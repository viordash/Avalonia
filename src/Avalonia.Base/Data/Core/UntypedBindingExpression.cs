using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Avalonia.Data.Converters;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Parsers;
using Avalonia.Reactive;

namespace Avalonia.Data.Core;

/// <summary>
/// A binding expression which accepts and produces (possibly boxed) object values.
/// </summary>
/// <remarks>
/// A <see cref="UntypedBindingExpression"/> represents a untyped binding which has been
/// instantiated on an object.
/// </remarks>
internal class UntypedBindingExpression : IObservable<object?>,
    IObserver<object?>,
    IDisposable
{
    private readonly IObservable<object?>? _sourceObservable;
    private readonly WeakReference<object?>? _source;
    private readonly IReadOnlyList<ExpressionNode> _nodes;
    private readonly object? _fallbackValue;
    private readonly IValueConverter? _converter;
    private readonly object? _converterParameter;
    private readonly TargetTypeConverter? _targetTypeConverter;
    private IDisposable? _sourceSubscription;
    private IObserver<object?>? _observer;

    /// <summary>
    /// Initializes a new instance of the <see cref="UntypedBindingExpression"/> class.
    /// </summary>
    /// <param name="source">The source from which the value will be read.</param>
    /// <param name="nodes">The nodes representing the binding path.</param>
    /// <param name="fallbackValue">
    /// The fallback value. Pass <see cref="AvaloniaProperty.UnsetValue"/> for no fallback.
    /// </param>
    /// <param name="converter">The converter to use.</param>
    /// <param name="converterParameter">The converter parameter.</param>
    /// <param name="targetTypeConverter">
    /// A final type converter to be run on the produced value.
    /// </param>
    public UntypedBindingExpression(
        object? source,
        IReadOnlyList<ExpressionNode> nodes,
        object? fallbackValue,
        IValueConverter? converter = null,
        object? converterParameter = null,
        TargetTypeConverter? targetTypeConverter = null)
    {
        _source = new(source);
        _nodes = nodes;
        _fallbackValue = fallbackValue;
        _converter = converter;
        _converterParameter = converterParameter;
        _targetTypeConverter = targetTypeConverter;

        for (var i = 0; i < nodes.Count; ++i)
            nodes[i].SetOwner(this, i);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UntypedBindingExpression"/> class.
    /// </summary>
    /// <param name="source">An observable which produces the source from which the value will be read.</param>
    /// <param name="nodes">The nodes representing the binding path.</param>
    /// <param name="targetTypeConverter">
    /// A final type converter to be run on the produced value.
    /// </param>
    public UntypedBindingExpression(
        IObservable<object?> source,
        IReadOnlyList<ExpressionNode> nodes,
        TargetTypeConverter? targetTypeConverter = null)
    {
        _sourceObservable = source;
        _nodes = nodes;
        _targetTypeConverter = targetTypeConverter;

        for (var i = 0; i < nodes.Count; ++i)
            nodes[i].SetOwner(this, i);
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
        if (_nodes.Count == 0)
            return false;
        return _nodes[_nodes.Count - 1].WriteValueToSource(value);
    }

    /// <summary>
    /// Creates an <see cref="UntypedBindingExpression"/> from an expression tree.
    /// </summary>
    /// <typeparam name="TIn">The input type of the binding expression.</typeparam>
    /// <typeparam name="TOut">The output type of the binding expression.</typeparam>
    /// <param name="source">The source from which the binding value will be read.</param>
    /// <param name="expression">The expression representing the binding path.</param>
    /// <param name="fallbackValue">The fallback value.</param>
    /// <param name="targetType">The target type to convert to.</param>
    [RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
    public static UntypedBindingExpression Create<TIn, TOut>(
        TIn source,
        Expression<Func<TIn, TOut>> expression,
        Optional<object?> fallbackValue = default,
        Type? targetType = null)
            where TIn : class?
    {
        var nodes = UntypedBindingExpressionVisitor<TIn>.BuildNodes(expression);
        var fallback = fallbackValue.HasValue ? fallbackValue.Value : AvaloniaProperty.UnsetValue;
        var targetTypeConverter = targetType is not null ? new TargetTypeConverter(targetType) : null;

        return new UntypedBindingExpression(
            source,
            nodes,
            fallback,
            targetTypeConverter: targetTypeConverter);
    }

    /// <summary>
    /// Implements the disposable returned by <see cref="IObservable{T}.Subscribe(IObserver{T})"/>.
    /// </summary>
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

    void IObserver<object?>.OnCompleted() { }
    void IObserver<object?>.OnError(Exception error) { }
    void IObserver<object?>.OnNext(object? value) => SetValue(value);

    /// <summary>
    /// Called by an <see cref="ExpressionNode"/> belonging to this binding when its
    /// <see cref="ExpressionNode.Value"/> changes.
    /// </summary>
    /// <param name="nodeIndex">The <see cref="ExpressionNode.Index"/>.</param>
    /// <param name="value">The <see cref="ExpressionNode.Value"/>.</param>
    internal void OnNodeValueChanged(int nodeIndex, object? value)
    {
        if (nodeIndex == _nodes.Count - 1)
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
            if (_nodes.Count > 0)
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
            node.Reset();
    }

    private void PublishValue()
    {
        if (_observer is null)
            return;

        var value = _nodes.Count > 0 ? _nodes[_nodes.Count - 1].Value : null;

        if (_converter is not null)
        {
            value = _converter.Convert(
                value,
                _targetTypeConverter?.TargetType ?? typeof(object),
                _converterParameter,
                CultureInfo.InvariantCulture);
        }

        if (value == BindingOperations.DoNothing)
            return;

        if (_targetTypeConverter is not null && value is not null)
            value = BindingNotification.ExtractValue(_targetTypeConverter.ConvertFrom(value));

        if (_fallbackValue != AvaloniaProperty.UnsetValue && value == AvaloniaProperty.UnsetValue)
            value = _fallbackValue;

        _observer.OnNext(value);
    }

    private void OnSourceChanged(object? source)
    {
        if (_nodes.Count > 0)
            _nodes[0].SetSource(source);
    }
}
