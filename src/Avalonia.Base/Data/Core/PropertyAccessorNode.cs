using System.Diagnostics.CodeAnalysis;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class PropertyAccessorNode : ExpressionNode
{
    private IPropertyAccessor? _accessor;

    public PropertyAccessorNode(string propertyName)
    {
        PropertyName = propertyName;
    }

    public string PropertyName { get; }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        _accessor?.Dispose();
        _accessor = null;

        if (GetPlugin(newSource) is { } plugin &&
            plugin.Start(new(newSource), PropertyName) is { } accessor)
        {
            _accessor = accessor;
            SetValue(_accessor.Value);
        }
    }

    private IPropertyAccessorPlugin? GetPlugin(object? source)
    {
        if (source is null)
            return null;

        foreach (var plugin in BindingPlugins.PropertyAccessors)
        {
            if (plugin.Match(source, PropertyName))
                return plugin;
        }

        return null;
    }
}
