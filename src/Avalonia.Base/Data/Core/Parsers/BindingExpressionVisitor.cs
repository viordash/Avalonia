using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Avalonia.Data.Core.Parsers;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class BindingExpressionVisitor<TIn> : ExpressionVisitor
{
    private static readonly PropertyInfo AvaloniaObjectIndexer;
    private static readonly string IndexerGetterName = "get_Item";
    private readonly LambdaExpression _rootExpression;
    private readonly List<ExpressionNode> _nodes = new();
    private Expression? _head;

    public BindingExpressionVisitor(LambdaExpression expression)
    {
        _rootExpression = expression;
    }

    static BindingExpressionVisitor()
    {
        AvaloniaObjectIndexer = typeof(AvaloniaObject).GetProperty("Item", new[] { typeof(AvaloniaProperty) })!;
    }

    public static ExpressionNode[] BuildNodes<TOut>(Expression<Func<TIn, TOut>> expression)
    {
        var visitor = new BindingExpressionVisitor<TIn>(expression);
        visitor.Visit(expression);
        return visitor._nodes.ToArray();
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var result = base.VisitBinary(node);
        if (node.Left == _head)
            _head = node;
        return result;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var result = base.VisitMember(node);

        if (node.Expression is not null &&
            node.Expression == _head &&
            node.Expression.Type.IsValueType == false &&
            node.Member.MemberType == MemberTypes.Property)
        {
            _nodes.Add(new PropertyAccessorNode(node.Member.Name));
            _head = node;
        }

        return result;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var result = base.VisitMethodCall(node);
        var method = node.Method;

        if (method.Name == IndexerGetterName &&
            method.DeclaringType == typeof(AvaloniaObject) &&
            method.GetParameters() is { } parameters &&
            parameters.Length == 1 &&
            parameters[0].ParameterType == typeof(AvaloniaProperty) &&
            typeof(AvaloniaProperty).IsAssignableFrom(node.Arguments[0].Type))
        {
            var property = GetValue<AvaloniaProperty>(node.Arguments[0]);
            _nodes.Add(new AvaloniaPropertyAccessorNode(property));
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

    private static T GetValue<T>(Expression expr)
    {
        try
        {
            return Expression.Lambda<Func<T>>(expr).Compile(preferInterpretation: true)();
        }
        catch (InvalidOperationException ex)
        {
            throw new ExpressionParseException(0, "Unable to parse indexer value.", ex);
        }
    }
}
