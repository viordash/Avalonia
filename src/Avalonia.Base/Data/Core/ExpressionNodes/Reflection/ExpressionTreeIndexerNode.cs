using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal sealed class ExpressionTreeIndexerNode : CollectionNodeBase
{
    private readonly ParameterExpression _parameter;
    private readonly IndexExpression _expression;
    private readonly Delegate _setDelegate;
    private readonly Delegate _getDelegate;
    private readonly Delegate _firstArgumentDelegate;

    public ExpressionTreeIndexerNode(IndexExpression expression)
    {
        var valueParameter = Expression.Parameter(expression.Type);
        _parameter = Expression.Parameter(expression.Object!.Type);
        _expression = expression.Update(_parameter, expression.Arguments);
        _getDelegate = Expression.Lambda(_expression, _parameter).Compile();
        _setDelegate = Expression.Lambda(Expression.Assign(_expression, valueParameter), _parameter, valueParameter).Compile();
        _firstArgumentDelegate = Expression.Lambda(_expression.Arguments[0], _parameter).Compile();
    }

    protected override bool ShouldUpdate(object? sender, PropertyChangedEventArgs e)
    {
        return _expression.Indexer == null || _expression.Indexer.Name == e.PropertyName;
    }

    protected override int? TryGetFirstArgumentAsInt()
    {
        var source = Source;
        if (source is null)
            return null;
        return _firstArgumentDelegate.DynamicInvoke(source) as int?;
    }

    protected override void UpdateValue(object? source)
    {
        try
        {
            if (source is not null)
                SetValue(_getDelegate.DynamicInvoke(source));
            else
                SetValue(null);
        }
        catch (Exception e)
        {
            SetError(e);
        }
    }

    public override bool WriteValueToSource(object? value)
    {
        if (Source is null)
            return false;
        _setDelegate.DynamicInvoke(Source, value);
        return true;
    }
}
