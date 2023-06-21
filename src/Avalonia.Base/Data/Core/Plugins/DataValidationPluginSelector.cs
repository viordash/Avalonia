using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Data.Core.Plugins;

[RequiresUnreferencedCode(TrimmingMessages.DataValidationPluginRequiresUnreferencedCodeMessage)]
internal class DataValidationPluginSelector : IDataValidationFactory
{
    public IPropertyAccessor? TryCreate(object? o, string propertyName, IPropertyAccessor accessor)
    {
        var reference = new WeakReference<object?>(o);

        foreach (var plugin in BindingPlugins.DataValidators)
        {
            if (plugin.Match(reference, propertyName))
                return plugin.Start(reference, propertyName, accessor);
        }

        return null;
    }
}
