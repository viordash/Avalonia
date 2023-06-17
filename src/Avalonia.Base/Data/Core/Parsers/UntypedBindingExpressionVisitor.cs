using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Avalonia.Data.Core.Parsers;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class UntypedBindingExpressionVisitor<TIn> : ExpressionVisitor
{
    private static readonly PropertyInfo AvaloniaObjectIndexer;
    private static readonly string IndexerGetterName = "get_Item";
    private const string MultiDimensionalArrayGetterMethodName = "Get";
    private readonly LambdaExpression _rootExpression;
    private readonly List<ExpressionNode> _nodes = new();
    private Expression? _head;

    public UntypedBindingExpressionVisitor(LambdaExpression expression)
    {
        _rootExpression = expression;
    }

    static UntypedBindingExpressionVisitor()
    {
        AvaloniaObjectIndexer = typeof(AvaloniaObject).GetProperty("Item", new[] { typeof(AvaloniaProperty) })!;
    }

    public static ExpressionNode[] BuildNodes<TOut>(Expression<Func<TIn, TOut>> expression)
    {
        var visitor = new UntypedBindingExpressionVisitor<TIn>(expression);
        visitor.Visit(expression);
        return visitor._nodes.ToArray();
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Indexers require more work since the compiler doesn't generate IndexExpressions:
        // they weren't in System.Linq.Expressions v1 and so must be generated manually.
        if (node.NodeType == ExpressionType.ArrayIndex)
            return Visit(Expression.MakeIndex(node.Left, null, new[] { node.Right }));

        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node.Indexer == AvaloniaObjectIndexer)
        {
            var property = GetValue<AvaloniaProperty>(node.Arguments[0]);
            Add(node.Object, node, new AvaloniaPropertyAccessorNode(property));
            return node;
        }
        else
        {
            Add(node.Object, node, new ReflectionIndexerNode(node));
            return node;
        }
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        switch (node.Member.MemberType)
        {
            case MemberTypes.Property:
                Add(node.Expression, node, new PropertyAccessorNode(node.Member.Name));
                return node;
            default:
                throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
        }
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var method = node.Method;

        if (method.Name == IndexerGetterName && node.Object is not null)
        {
            var property = TryGetPropertyFromMethod(method);
            return Visit(Expression.MakeIndex(node.Object, property, node.Arguments));
        }
        else if (method.Name == MultiDimensionalArrayGetterMethodName &&
                 node.Object is not null)
        {
            var expression = Expression.MakeIndex(node.Object, null, node.Arguments);
            Add(node.Object, node, new ReflectionIndexerNode(expression));
            return node;
        }

        throw new ExpressionParseException(0, $"Invalid method call in binding expression: '{node.Method.DeclaringType!.AssemblyQualifiedName}.{node.Method.Name}'.");
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
    
    private void Add(Expression? instance, Expression expression, ExpressionNode node)
    {
        var visited = Visit(instance);
        if (visited != _head)
            throw new ExpressionParseException(
                0, 
                $"Unable to parse '{expression}': expected an instance of '{_head}' but got '{visited}'.");
        _nodes.Add(node);
        _head = expression;
    }

    private static T GetValue<T>(Expression expr)
    {
        if (expr is ConstantExpression constant)
            return (T)constant.Value!;
        return Expression.Lambda<Func<T>>(expr).Compile(preferInterpretation: true)();
    }

    private static PropertyInfo? TryGetPropertyFromMethod(MethodInfo method)
    {
        var type = method.DeclaringType;
        return type?.GetRuntimeProperties().FirstOrDefault(prop => prop.GetMethod == method);
    }
}
