using System;
using System.ComponentModel;

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
        if (oldSource is INotifyPropertyChanged oldInpc)
            oldInpc.PropertyChanged -= OnPropertyChanged;
        if (newSource is INotifyPropertyChanged newInpc)
            newInpc.PropertyChanged += OnPropertyChanged;

        UpdateValue(newSource);
    }

    private void UpdateValue(object? source)
    {
        if (source is not null)
            SetValue(_property.Get(source));
        else
            ClearValue();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == _property.Name)
            UpdateValue(sender);
    }
}
