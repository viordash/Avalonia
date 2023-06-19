using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data.Core.Plugins;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class PluginStreamNode : ExpressionNode
{
    private IDisposable? _subscription;

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        _subscription?.Dispose();
        _subscription = null;
        
        var reference = new WeakReference<object?>(newSource);

        if (newSource is not null &&
            GetPlugin(reference) is { } plugin &&
            plugin.Start(reference) is { } accessor)
        {
            _subscription = accessor.Subscribe(SetValue);
        }
        else
        {
            SetValue(null);
        }
    }

    private static IStreamPlugin? GetPlugin(WeakReference<object?> source)
    {
        if (source is null)
            return null;

        foreach (var plugin in BindingPlugins.StreamHandlers)
        {
            if (plugin.Match(source))
                return plugin;
        }

        return null;
    }
}
