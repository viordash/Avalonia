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
    private readonly WeakReference<object?>? _source;
    private readonly IReadOnlyList<ExpressionNode> _nodes;
    private readonly TargetTypeConverter? _targetTypeConverter;
    private IObserver<object?>? _observer;
    private UncommonFields? _uncommon;

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
    /// <param name="stringFormat">The format string to use.</param>
    /// <param name="targetTypeConverter">
    /// A final type converter to be run on the produced value.
    /// </param>
    public UntypedBindingExpression(
        object? source,
        IReadOnlyList<ExpressionNode> nodes,
        object? fallbackValue,
        IValueConverter? converter = null,
        object? converterParameter = null,
        string? stringFormat = null,
        TargetTypeConverter? targetTypeConverter = null)
    {
        _source = new(source);
        _nodes = nodes;
        _targetTypeConverter = targetTypeConverter;

        if (fallbackValue != AvaloniaProperty.UnsetValue ||
            converter is not null ||
            converterParameter is not null ||
            !string.IsNullOrWhiteSpace(stringFormat))
        {
            _uncommon = new()
            {
                _fallbackValue = fallbackValue,
                _converter = converter,
                _converterParameter = converterParameter,
                _stringFormat = stringFormat switch
                {
                    string s when string.IsNullOrWhiteSpace(s) => null,
                    string s when !s.Contains('{') => $"{{0:{stringFormat}}}",
                    _ => stringFormat,
                },
            };
        }

        for (var i = 0; i < nodes.Count; ++i)
            nodes[i].SetOwner(this, i);
    }

    private Type TargetType => _targetTypeConverter?.TargetType ?? typeof(object);
    private IValueConverter? Converter => _uncommon?._converter;
    private object? ConverterParameter => _uncommon?._converterParameter;
    private string? StringFormat => _uncommon?._stringFormat;

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
        foreach (var node in _nodes)
            node.Reset();
    }

    private void PublishValue()
    {
        if (_observer is null)
            return;

        var value = _nodes.Count > 0 ? _nodes[_nodes.Count - 1].Value : null;

        if (Converter is { } converter)
        {
            value = converter.Convert(
                value,
                _targetTypeConverter?.TargetType ?? typeof(object),
                ConverterParameter,
                CultureInfo.InvariantCulture);
        }

        if (value == BindingOperations.DoNothing)
            return;

        if (value != AvaloniaProperty.UnsetValue)
        {
            if (StringFormat is { } stringFormat && (TargetType == typeof(object) || TargetType == typeof(string)))
                value = string.Format(CultureInfo.CurrentCulture, stringFormat, value);
            else if (_targetTypeConverter is not null && value is not null)
                value = BindingNotification.ExtractValue(_targetTypeConverter.ConvertFrom(value));
            }

        if (_uncommon is not null &&
            _uncommon._fallbackValue != AvaloniaProperty.UnsetValue &&
            value == AvaloniaProperty.UnsetValue)
        {
            value = _uncommon._fallbackValue;
        }

        _observer.OnNext(value);
    }

    private void OnSourceChanged(object? source)
    {
        if (_nodes.Count > 0)
            _nodes[0].SetSource(source);
    }

    private class UncommonFields
    {
        public object? _fallbackValue;
        public IValueConverter? _converter;
        public object? _converterParameter;
        public string? _stringFormat;
    }
}
