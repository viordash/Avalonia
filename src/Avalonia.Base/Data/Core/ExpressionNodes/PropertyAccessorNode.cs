using System;
using System.ComponentModel;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="UntypedBindingExpression"/> which uses an <see cref="IPropertyInfo"/>
/// instance to access a property.
/// </summary>
internal class PropertyAccessorNode : ExpressionNode, IPropertyAccessor, IPropertyAccessorNode
{
    private readonly IPropertyInfo _property;
    private Action<object?>? _listener;

    public PropertyAccessorNode(IPropertyInfo property)
    {
        _property = property;
    }

    public string PropertyName => _property.Name;
    IPropertyAccessor? IPropertyAccessorNode.Accessor => this;

    Type? IPropertyAccessor.PropertyType { get; }

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
        {
            var value = _property.Get(source);
            _listener?.Invoke(value);
            SetValue(value);
        }
        else
        {
            ClearValue();
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == _property.Name)
            UpdateValue(sender);
    }

    bool IPropertyAccessor.SetValue(object? value, BindingPriority priority)
    {
        return WriteValueToSource(value);
    }

    void IPropertyAccessor.Subscribe(Action<object?> listener) => _listener = listener;
    void IPropertyAccessor.Unsubscribe() => _listener = null;
    void IDisposable.Dispose() => _listener = null;
}
