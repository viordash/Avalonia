﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Avalonia.Data.Core.Parsers;
using Avalonia.Utilities;

namespace Avalonia.Data.Core;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class UntypedBindingExpression : IObservable<object?>, IDisposable
{
    private readonly WeakReference<object?> _source;
    private readonly ExpressionNode[] _nodes;
    private readonly Type _targetType;
    private IObserver<object?>? _observer;

    public UntypedBindingExpression(
        object? source,
        ExpressionNode[] nodes,
        Type targetType)
    {
        _source = new(source);
        _nodes = nodes;
        _targetType = targetType;
        foreach (var node in nodes)
            node.SetOwner(this);
    }

    public static UntypedBindingExpression Create<TIn, TOut>(
        TIn source,
        Expression<Func<TIn, TOut>> expression,
        Type targetType)
            where TIn : class?
    {
        var nodes = BindingExpressionVisitor<TIn>.BuildNodes(expression);
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
                $"A {nameof(UntypedBindingExpression)} may only have a single subscriber.");

        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        Start();
        return this;
    }

    private void Start()
    {
        if (_observer is null ||
            !_source.TryGetTarget(out var source) || 
            source is null)
            return;

        foreach (var node in _nodes)
        {
            node.SetSource(source);
            source = node.Value;
        }

        PublishValue();
    }

    private void Stop()
    {
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
}
