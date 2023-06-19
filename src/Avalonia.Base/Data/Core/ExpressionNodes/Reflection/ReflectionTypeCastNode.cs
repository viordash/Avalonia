using System;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

/// <summary>
/// A node in an <see cref="UntypedBindingExpression"/> which casts a value using reflection.
/// </summary>
internal class ReflectionTypeCastNode : ExpressionNode
{
    private readonly Type _targetType;

    public ReflectionTypeCastNode(Type targetType) => _targetType = targetType;

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        if (_targetType.IsInstanceOfType(newSource))
            SetValue(newSource);
        else
            ClearValue();
    }
}
