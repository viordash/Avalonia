using System;
using System.Globalization;

namespace Avalonia.Data.Core;

internal class LogicalNotNode : ExpressionNode
{
    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        UpdateValue(newSource);
    }

    private void UpdateValue(object? value)
    {
        if (value is null)
            SetValue(AvaloniaProperty.UnsetValue);
        else if (TryConvert(value, out var boolValue))
            SetValue(!boolValue);
        else
            SetError(new InvalidCastException($"Unable to convert '{value}' to bool."));
    }

    private static bool TryConvert(object? value,  out bool result)
    {
        if (value is string s)
        {
            // Special case string for performance.
            if (bool.TryParse(s, out result))
                return true;
        }
        else
        {
            try
            {
                result = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch { }
        }

        result = false;
        return false;
    }
}
