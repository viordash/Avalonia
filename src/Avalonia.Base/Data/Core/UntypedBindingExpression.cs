using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Avalonia.Data.Core.Parsers;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Data.Core;

/// <summary>
/// A binding expression which accepts and produces (possibly boxed) object values.
/// </summary>
/// <remarks>
/// A <see cref="UntypedBindingExpression"/> represents a untyped binding which has been
/// instantiated on an object.
/// </remarks>
[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class UntypedBindingExpression : IObservable<object?>, IDisposable
{
    private readonly IObservable<object?>? _sourceObservable;
    private readonly WeakReference<object?>? _source;
    private readonly ExpressionNode[] _nodes;
    private readonly Type _targetType;
    private IDisposable? _sourceSubscription;
    private IObserver<object?>? _observer;

    /// <summary>
    /// Initializes a new instance of the <see cref="UntypedBindingExpression"/> class.
    /// </summary>
    /// <param name="source">The source from which the value will be read.</param>
    /// <param name="nodes">The nodes representing the binding path.</param>
    /// <param name="targetType">The type to which produced values should be converted.</param>
    public UntypedBindingExpression(
        object? source,
        ExpressionNode[] nodes,
        Type targetType)
    {
        _source = new(source);
        _nodes = nodes;
        _targetType = targetType;

        var i = 0;
        foreach (var node in nodes)
            node.SetOwner(this, i++);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UntypedBindingExpression"/> class.
    /// </summary>
    /// <param name="source">An observable which produces the source from which the value will be read.</param>
    /// <param name="nodes">The nodes representing the binding path.</param>
    /// <param name="targetType">The type to which produced values should be converted.</param>
    public UntypedBindingExpression(
        IObservable<object?> source,
        ExpressionNode[] nodes,
        Type targetType)
    {
        _sourceObservable = source;
        _nodes = nodes;
        _targetType = targetType;

        var i = 0;
        foreach (var node in nodes)
            node.SetOwner(this, i++);
    }

    /// <summary>
    /// Writes the specified value to the binding source if possible.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <returns>
    /// True if the value could be written to the binding source; otherwise false.
    /// </returns>
    public bool SetValue(object? value)
    {
        if (_nodes.Length == 0)
            return false;
        return _nodes[_nodes.Length - 1].WriteValueToSource(value);
    }

    /// <summary>
    /// Creates an <see cref="UntypedBindingExpression"/> from an expression tree.
    /// </summary>
    /// <typeparam name="TIn">The input type of the binding expression.</typeparam>
    /// <typeparam name="TOut">The output type of the binding expression.</typeparam>
    /// <param name="source">The source from which the binding value will be read.</param>
    /// <param name="expression">The expression representing the binding path.</param>
    /// <param name="targetType">The type to which produced values should be converted.</param>
    public static UntypedBindingExpression Create<TIn, TOut>(
        TIn source,
        Expression<Func<TIn, TOut>> expression,
        Type targetType)
            where TIn : class?
    {
        var nodes = UntypedBindingExpressionVisitor<TIn>.BuildNodes(expression);
        return new UntypedBindingExpression(source, nodes, targetType);
    }

    /// <summary>
    /// Creates an <see cref="UntypedBindingExpression"/> from an expression tree.
    /// </summary>
    /// <typeparam name="TIn">The input type of the binding expression.</typeparam>
    /// <typeparam name="TOut">The output type of the binding expression.</typeparam>
    /// <param name="source">An observable which produces the source from which the value will be read.</param>
    /// <param name="expression">The expression representing the binding path.</param>
    /// <param name="targetType">The type to which produced values should be converted.</param>
    public static UntypedBindingExpression Create<TIn, TOut>(
        IObservable<TIn> source,
        Expression<Func<TIn, TOut>> expression,
        Type targetType)
            where TIn : class?
    {
        var nodes = UntypedBindingExpressionVisitor<TIn>.BuildNodes(expression);
        return new UntypedBindingExpression(source, nodes, targetType);
    }

    void IDisposable.Dispose()
    {
        if (_observer is null)
            return;
        _observer = null;
        Stop();
    }

    IDisposable IObservable<object?>.Subscribe(IObserver<object?> observer)
    {
        if (_observer is not null)
            throw new InvalidOperationException(
                $"An {nameof(UntypedBindingExpression)} may only have a single subscriber.");

        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        Start();
        return this;
    }

    internal void OnNodeValueChanged(int nodeIndex, object? value)
    {
        if (nodeIndex == _nodes.Length - 1)
            PublishValue();
        else
            _nodes[nodeIndex + 1].SetSource(value);
    }

    private void Start()
    {
        if (_observer is null)
            return;

        if (_sourceSubscription is not null)
            throw new InvalidOperationException(
                $"The {nameof(UntypedBindingExpression)} has already been started.");

        if (_sourceObservable is not null)
            _sourceSubscription = _sourceObservable.Subscribe(OnSourceChanged);
        
        if (_source?.TryGetTarget(out var source) == true)
        {
            if (_nodes.Length > 0)
                _nodes[0].SetSource(source);
            else
                _observer.OnNext(source);
        }
        else
        {
            _observer.OnNext(null);
        }
    }

    private void Stop()
    {
        _sourceSubscription?.Dispose();
        _sourceSubscription = null;

        foreach (var node in _nodes)
            node.SetSource(null);
    }

    private void PublishValue()
    {
        if (_observer is null)
            return;

        var value = _nodes.Length > 0 ? _nodes[_nodes.Length - 1].Value : null;
        
        if (TypeUtilities.TryConvert(_targetType, value, CultureInfo.InvariantCulture, out var convertedValue))
        {
            _observer.OnNext(convertedValue);
        }
    }

    private void OnSourceChanged(object? source)
    {
        if (_nodes.Length > 0)
            _nodes[0].SetSource(source);
    }
}
