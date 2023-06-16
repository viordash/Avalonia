using System;

namespace Avalonia.Data.Core;

internal abstract class ExpressionNode
{
    private WeakReference<object?>? _source;
    private WeakReference<object?>? _value;

    public UntypedBindingExpression? Owner { get; private set; }
    
    public object? Source
    {
        get
        {
            if (_source?.TryGetTarget(out var source) == true)
                return source;
            return null;
        }
    }

    public object? Value
    {
        get
        {
            if (_value?.TryGetTarget(out var value) == true)
                return value;
            return null;
        }
    }

    public void SetOwner(UntypedBindingExpression owner)
    {
        if (Owner is not null)
            throw new InvalidOperationException($"{this} already has an owner.");
        Owner = owner;
    }

    public void SetSource(object? source)
    {
        var oldSource = Source;

        if (source != oldSource)
        {
            _source = new(source);
            OnSourceChanged(oldSource, source);
        }
    }

    protected void SetValue(object? value)
    {
        _value = new(value);
    }

    protected abstract void OnSourceChanged(object? oldSource, object? newSource);
}
