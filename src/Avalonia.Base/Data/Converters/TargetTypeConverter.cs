using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Avalonia.Data.Converters;

[RequiresUnreferencedCode(TrimmingMessages.TypeConversionRequiresUnreferencedCodeMessage)]
internal class TargetTypeConverter : TypeConverter
{
    public TargetTypeConverter(Type targetType) => TargetType = targetType;

    public Type TargetType { get; }

    public static TargetTypeConverter? Create(AvaloniaProperty? targetProperty)
    {
        if (targetProperty is null)
            return null;
        return new TargetTypeConverter(targetProperty.PropertyType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return DefaultValueConverter.Instance.Convert(value, TargetType, null, CultureInfo.InvariantCulture);
    }
}
