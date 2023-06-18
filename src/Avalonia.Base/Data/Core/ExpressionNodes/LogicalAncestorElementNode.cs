using System;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.ExpressionNodes;

internal class LogicalAncestorElementNode : ExpressionNode
{
    private readonly Type? _ancestorType;
    private readonly int _ancestorLevel;
    private IDisposable? _subscription;

    public LogicalAncestorElementNode(Type? ancestorType, int ancestorLevel)
    {
        _ancestorType = ancestorType;
        _ancestorLevel = ancestorLevel;
    }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        _subscription?.Dispose();
        _subscription = null;

        if (newSource is ILogical logical)
        {
            var locator = ControlLocator.Track(logical, _ancestorLevel, _ancestorType);
            _subscription = locator.Subscribe(SetValue);
        }
    }
}
