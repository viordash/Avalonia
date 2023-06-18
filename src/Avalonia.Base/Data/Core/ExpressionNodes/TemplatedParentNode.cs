using System;

namespace Avalonia.Data.Core.ExpressionNodes;

internal class TemplatedParentNode : ExpressionNode
{
    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        if (oldSource is StyledElement oldElement)
            oldElement.PropertyChanged -= OnPropertyChanged;
        
        if (newSource is StyledElement newElement)
        {
            newElement.PropertyChanged += OnPropertyChanged;
            SetValue(newElement.TemplatedParent);
        }
        else if (newSource is null)
        {
            SetValue(null);
        }
        else
        {
            SetError(new InvalidCastException($"Unable to read TemplatedParent from '{newSource.GetType()}'."));
        }
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender == Source && e.Property == StyledElement.TemplatedParentProperty)
            SetValue(e.NewValue);
    }
}
