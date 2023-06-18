using System;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Data.Core.ExpressionNodes;

internal class VisualAncestorElementNode : ExpressionNode
{
    private readonly Type? _ancestorType;
    private readonly int _ancestorLevel;
    private IDisposable? _subscription;

    public VisualAncestorElementNode(Type? ancestorType, int ancestorLevel)
    {
        _ancestorType = ancestorType;
        _ancestorLevel = ancestorLevel;
    }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        _subscription?.Dispose();
        _subscription = null;

        if (newSource is Visual visual)
        {
            var locator = VisualLocator.Track(visual, _ancestorLevel, _ancestorType);
            _subscription = locator.Subscribe(SetValue);
        }
    }
}
