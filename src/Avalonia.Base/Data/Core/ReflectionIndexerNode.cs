using System;
using System.Linq.Expressions;

namespace Avalonia.Data.Core;

internal class ReflectionIndexerNode : ExpressionNode
{
    private readonly ParameterExpression _parameter;
    private readonly IndexExpression _expression;
    private readonly Delegate _setDelegate;
    private readonly Delegate _getDelegate;
    private readonly Delegate _firstArgumentDelegate;

    public ReflectionIndexerNode(IndexExpression expression)
    {
        var valueParameter = Expression.Parameter(expression.Type);
        _parameter = Expression.Parameter(expression.Object!.Type);
        _expression = expression.Update(_parameter, expression.Arguments);
        _getDelegate = Expression.Lambda(_expression, _parameter).Compile();
        _setDelegate = Expression.Lambda(Expression.Assign(_expression, valueParameter), _parameter, valueParameter).Compile();
        _firstArgumentDelegate = Expression.Lambda(_expression.Arguments[0], _parameter).Compile();
    }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        UpdateValue(newSource);
    }

    public override bool WriteValueToSource(object? value)
    {
        if (Source is null)
            return false;
        _setDelegate.DynamicInvoke(value);
        return true;
    }

    private void UpdateValue(object? source)
    {
        if (source is not null)
            SetValue(_getDelegate.DynamicInvoke(source));
        else
            SetValue(null);
    }
}
