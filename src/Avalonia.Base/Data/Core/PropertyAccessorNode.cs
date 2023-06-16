using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class PropertyAccessorNode : ExpressionNode
{
    private readonly Action<object?> _onValueChanged;
    private IPropertyAccessor? _accessor;

    public PropertyAccessorNode(string propertyName)
    {
        PropertyName = propertyName;
        _onValueChanged = OnValueChanged;
    }

    public string PropertyName { get; }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        _accessor?.Dispose();
        _accessor = null;

        if (GetPlugin(newSource, PropertyName) is { } plugin &&
            plugin.Start(new(newSource), PropertyName) is { } accessor)
        {
            _accessor = accessor;
            _accessor.Subscribe(_onValueChanged);
            SetValue(_accessor.Value);
        }
    }

    private void OnValueChanged(object? newValue)
    {
        SetValue(newValue);
    }

    private static IPropertyAccessorPlugin? GetPlugin(object? source, string propertyName)
    {
        if (source is null)
            return null;

        foreach (var plugin in BindingPlugins.PropertyAccessors)
        {
            if (plugin.Match(source, propertyName))
                return plugin;
        }

        return null;
    }
}
