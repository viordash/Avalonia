using System;

namespace Avalonia.Data.Core;

/// <summary>
/// A node in the binding path of an <see cref="UntypedBindingExpression"/>.
/// </summary>
internal abstract class ExpressionNode
{
    private WeakReference<object?>? _source;
    private WeakReference<object?>? _value;

    /// <summary>
    /// Gets the index of the node in the binding path.
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// Gets the owning <see cref="UntypedBindingExpression"/>.
    /// </summary>
    public UntypedBindingExpression? Owner { get; private set; }
    
    /// <summary>
    /// Gets the source object from which the node will read its value.
    /// </summary>
    public object? Source
    {
        get
        {
            if (_source?.TryGetTarget(out var source) == true)
                return source;
            return null;
        }
    }

    /// <summary>
    /// Gets the current value of the node.
    /// </summary>
    public object? Value
    {
        get
        {
            if (_value is null)
                return AvaloniaProperty.UnsetValue;
            _value.TryGetTarget(out var value);
            return value;
        }
    }

    /// <summary>
    /// Resets the node to its uninitialized state when the <see cref="Owner"/> is unsubscribed.
    /// </summary>
    public void Reset()
    {
        SetSource(null);
        _source = _value = null;
    }

    /// <summary>
    /// Sets the owner binding.
    /// </summary>
    /// <param name="owner">The owner binding.</param>
    /// <param name="index">The index of the node in the binding path.</param>
    /// <exception cref="InvalidOperationException">
    /// The node already has an owner.
    /// </exception>
    public void SetOwner(UntypedBindingExpression owner, int index)
    {
        if (Owner is not null)
            throw new InvalidOperationException($"{this} already has an owner.");
        Owner = owner;
        Index = index;
    }

    /// <summary>
    /// Sets the <see cref="Source"/> from which the node will read its value and updates
    /// the current <see cref="Value"/>, notifying the <see cref="Owner"/> if the value
    /// changes.
    /// </summary>
    /// <param name="source">
    /// The new source from which the node will read its value. May be 
    /// <see cref="AvaloniaProperty.UnsetValue"/> in which case the source will be considered
    /// to be null.
    /// </param>
    public void SetSource(object? source)
    {
        var oldSource = Source;

        if (source == AvaloniaProperty.UnsetValue)
            _source = null;
        
        if (source != oldSource)
        {
            _source = new(source);
            try { OnSourceChanged(oldSource, source); }
            catch (Exception e) { SetError(e); }
        }
    }

    /// <summary>
    /// Tries to write the specified value to the source.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <returns>True if the value was written sucessfully; otherwise false.</returns>
    public virtual bool WriteValueToSource(object? value) => false;

    /// <summary>
    /// Sets the current value to <see cref="AvaloniaProperty.UnsetValue"/> and notifies the
    /// <see cref="Owner"/> of the error.
    /// </summary>
    /// <param name="e">The error.</param>
    protected void SetError(Exception e)
    {
        SetValue(AvaloniaProperty.UnsetValue);
    }

    /// <summary>
    /// Sets the current <see cref="Value"/>, notifying the <see cref="Owner"/> if the value
    /// has changed.
    /// </summary>
    /// <param name="value">The new value.</param>
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

    /// <summary>
    /// When implemented in a derived class, unsubscribes from the previous source, subscribes
    /// to the new source, and updates the current <see cref="Value"/>.
    /// </summary>
    /// <param name="oldSource">The old source.</param>
    /// <param name="newSource">The new source.</param>
    protected abstract void OnSourceChanged(object? oldSource, object? newSource);
}
