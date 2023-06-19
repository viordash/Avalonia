using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Input;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

[RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
internal class ReflectionIndexerNode : CollectionNodeBase
{
    private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
    private readonly IList _arguments;
    private PropertyInfo? _indexer;
    private List<object?>? _indexes;

    public ReflectionIndexerNode(IList arguments)
    {
        _arguments = arguments;
    }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        _indexes = null;
        _indexer = GetIndexer(newSource?.GetType());

        if (_indexer is not null)
        {
            _indexes = ConvertIndexes(_indexer.GetIndexParameters(), _arguments);
        }

        base.OnSourceChanged(oldSource, newSource);
    }

    protected override bool ShouldUpdate(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is null || e.PropertyName is null)
            return false;
        var typeInfo = sender.GetType().GetTypeInfo();
        return typeInfo.GetDeclaredProperty(e.PropertyName)?.GetIndexParameters().Any() ?? false;
    }

    protected override int? TryGetFirstArgumentAsInt()
    {
        if (TypeUtilities.TryConvert(typeof(int), _arguments[0], CultureInfo.InvariantCulture, out var value))
            return (int?)value;
        return null;
    }

    protected override void UpdateValue(object? source)
    {
        throw new NotImplementedException();
    }

    private static List<object?>? ConvertIndexes(ParameterInfo[] indexParameters, IList arguments)
    {
        var result = new List<object?>();

        for (var i = 0; i < indexParameters.Length; i++)
        {
            var type = indexParameters[i].ParameterType;
            var argument = arguments[i];

            if (TypeUtilities.TryConvert(type, argument, CultureInfo.InvariantCulture, out var value))
                result.Add(value);
            else
                throw new InvalidCastException(
                    $"Could not convert list index '{i}' of type '{argument}' to '{type}'.");
        }

        return result;
    }

    private static PropertyInfo? GetIndexer(Type? type)
    {
        for (; type != null; type = type.BaseType?.GetTypeInfo())
        {
            // Check for the default indexer name first to make this faster.
            // This will only be false when a class in VB has a custom indexer name.
            if (type.GetProperty(CommonPropertyNames.IndexerName, BindingFlags.Instance) is { } indexer)
                return indexer;

            foreach (var property in type.GetProperties(InstanceFlags))
            {
                if (property.GetIndexParameters().Length > 0)
                    return property;
            }
        }

        return null;
    }

}
