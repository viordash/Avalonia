using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Avalonia.Data.Converters;

[RequiresUnreferencedCode(TrimmingMessages.TypeConversionRequiresUnreferencedCodeMessage)]
internal class TargetTypeConverter : TypeConverter
{
    private readonly Type _targetType;

    public TargetTypeConverter(Type targetType) => _targetType = targetType;

    public static TypeConverter? Create(AvaloniaProperty? targetProperty)
    {
        if (targetProperty is null)
            return null;
        return new TargetTypeConverter(targetProperty.PropertyType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return DefaultValueConverter.Instance.Convert(value, _targetType, null, CultureInfo.InvariantCulture);
    }
}
