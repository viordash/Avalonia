using System;

namespace Avalonia.Data.Core;

internal abstract class ExpressionNode
{
    private WeakReference<object?>? _source;
    private WeakReference<object?>? _value;

    public int Index { get; private set; }
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

    public void SetOwner(UntypedBindingExpression owner, int index)
    {
        if (Owner is not null)
            throw new InvalidOperationException($"{this} already has an owner.");
        Owner = owner;
        Index = index;
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

    public virtual bool WriteValueToSource(object? value) => false;

    protected void SetValue(object? value)
    {
        // We raise a change notification if:
        //
        // - This is the initial value (_value is null)
        // - The old value has been GC'd - in this case we don't know if the new value is different
        // - The new value is different to the old value
        if (_value is null ||
            _value.TryGetTarget(out var oldValue) == false ||
            !Equals(oldValue, value))
        {
            _value = new(value);
            Owner?.OnNodeValueChanged(Index, value);
        }
    }

    protected abstract void OnSourceChanged(object? oldSource, object? newSource);
}
