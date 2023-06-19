using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Data.Core.Plugins;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class PluginPropertyAccessorNode : ExpressionNode
{
    private readonly Action<object?> _onValueChanged;
    private IPropertyAccessor? _accessor;

    public PluginPropertyAccessorNode(string propertyName)
    {
        PropertyName = propertyName;
        _onValueChanged = OnValueChanged;
    }

    public string PropertyName { get; }

    public override bool WriteValueToSource(object? value)
    {
        if (_accessor?.PropertyType is { } targetType &&
            TypeUtilities.TryConvert(targetType, value, CultureInfo.InvariantCulture, out var convertedValue))
        {
            return _accessor.SetValue(convertedValue, BindingPriority.LocalValue);
        }

        return false;
    }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        _accessor?.Dispose();
        _accessor = null;

        if (GetPlugin(newSource, PropertyName) is { } plugin &&
            plugin.Start(new(newSource), PropertyName) is { } accessor)
        {
            _accessor = accessor;
            _accessor.Subscribe(_onValueChanged);
        }
        else
        {
            SetValue(null);
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
