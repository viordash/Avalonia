namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="UntypedBindingExpression"/> which uses an <see cref="IPropertyInfo"/>
/// instance to access a property.
/// </summary>
internal class PropertyAccessorNode : ExpressionNode
{
    private readonly IPropertyInfo _property;

    public PropertyAccessorNode(IPropertyInfo property)
    {
        _property = property;
    }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        if (newSource is not null)
            SetValue(_property.Get(newSource));
        else
            ClearValue();
    }
}
