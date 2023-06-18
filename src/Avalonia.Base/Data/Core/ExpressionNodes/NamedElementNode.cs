using System;
using Avalonia.Controls;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.ExpressionNodes;

internal class NamedElementNode : ExpressionNode
{
    private readonly WeakReference<INameScope?> _nameScope;
    private readonly string _name;
    private IDisposable? _subscription;

    public NamedElementNode(INameScope? nameScope, string name)
    {
        _nameScope = new(nameScope);
        _name = name;
    }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        if (newSource is not null && _nameScope.TryGetTarget(out var scope))
        {
            _subscription = NameScopeLocator.Track(scope, _name).Subscribe(SetValue);
        }
        else
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
