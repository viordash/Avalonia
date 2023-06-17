using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Avalonia.Data.Core.Parsers;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class UntypedBindingExpressionVisitor<TIn> : ExpressionVisitor
{
    private static readonly string IndexerGetterName = "get_Item";
    private const string MultiDimensionalArrayGetterMethodName = "Get";
    private readonly LambdaExpression _rootExpression;
    private readonly List<ExpressionNode> _nodes = new();
    private Expression? _head;

    public UntypedBindingExpressionVisitor(LambdaExpression expression)
    {
        _rootExpression = expression;
    }

    public static ExpressionNode[] BuildNodes<TOut>(Expression<Func<TIn, TOut>> expression)
    {
        var visitor = new UntypedBindingExpressionVisitor<TIn>(expression);
        visitor.Visit(expression);
        return visitor._nodes.ToArray();
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var result = base.VisitBinary(node);

        EnsureHead(node.Left);
        _head = node;

        switch (node.NodeType)
        {
            case ExpressionType.ArrayIndex:
                var index = GetValue<int>(node.Right);
                _nodes.Add(new ListIndexerNode(index));
                break;
            default:
                throw new InvalidOperationException($"Unsupported binary expresion: {node.NodeType}.");
        }

        return result;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var result = base.VisitMember(node);

        EnsureHead(node.Expression);
        _head = node;

        if (node.Expression is not null)
        {
            switch (node.Member.MemberType)
            {
                case MemberTypes.Property:
                    _nodes.Add(new PropertyAccessorNode(node.Member.Name));
                    break;
                default:
                    throw new NotSupportedException($"Unsupported MemberExpression: {node}.");
            }
        }

        return result;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var result = base.VisitMethodCall(node);
        var method = node.Method;

        EnsureHead(node.Object);

        if (method.Name == IndexerGetterName)
        {
            if (method.DeclaringType == typeof(AvaloniaObject) &&
                method.GetParameters() is { } parameters &&
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(AvaloniaProperty) &&
                typeof(AvaloniaProperty).IsAssignableFrom(node.Arguments[0].Type))
            {
                var property = GetValue<AvaloniaProperty>(node.Arguments[0]);
                _nodes.Add(new AvaloniaPropertyAccessorNode(property));
            }
            else if (typeof(IList).IsAssignableFrom(method.DeclaringType) &&
                     node.Arguments.Count == 1 &&
                     node.Arguments[0].Type == typeof(int))
            {
                var index = GetValue<int>(node.Arguments[0]);
                _nodes.Add(new ListIndexerNode(index));
            }
        }
        else if (method.Name == MultiDimensionalArrayGetterMethodName &&
                 node.Object is not null)
        {
            var expression = Expression.MakeIndex(node.Object, null, node.Arguments);
            _nodes.Add(new ReflectionIndexerNode(expression));
        }

        return result;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == _rootExpression.Parameters[0])
            _head = node;
        return base.VisitParameter(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        var result = base.VisitUnary(node);
        if (node.Operand == _head)
            _head = node;
        return result;
    }

    private void EnsureHead(Expression? e)
    {
        if (e != _head)
            throw new NotSupportedException($"Unable to parse expression: {e}");
    }

    private static T GetValue<T>(Expression expr)
    {
        if (expr is ConstantExpression constant)
            return (T)constant.Value!;
        return Expression.Lambda<Func<T>>(expr).Compile(preferInterpretation: true)();
    }
}
