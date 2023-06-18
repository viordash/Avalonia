using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Data.Core.ExpressionNodes;

[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class AvaloniaPropertyAccessorNode : ExpressionNode
{
    private readonly EventHandler<AvaloniaPropertyChangedEventArgs> _onValueChanged;

    public AvaloniaPropertyAccessorNode(AvaloniaProperty property)
    {
        Property = property;
        _onValueChanged = OnValueChanged;
    }

    public AvaloniaProperty Property { get; }

    public override bool WriteValueToSource(object? value)
    {
        if (Source is AvaloniaObject o)
        {
            o.SetValue(Property, value);
            return true;
        }

        return false;
    }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        if (oldSource is AvaloniaObject oldObject)
            oldObject.PropertyChanged -= _onValueChanged;

        if (newSource is AvaloniaObject newObject)
        {
            newObject.PropertyChanged += _onValueChanged;
            SetValue(newObject.GetValue(Property));
        }
    }

    private void OnValueChanged(object? source, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Property && source is AvaloniaObject o)
            SetValue(o.GetValue(Property));
    }
}
