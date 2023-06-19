using System;

namespace Avalonia.Data.Core.ExpressionNodes;

internal class DataContextNode : ExpressionNode
{
    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        if (oldSource is StyledElement oldElement)
            oldElement.PropertyChanged -= OnPropertyChanged;
        
        if (newSource is IDataContextProvider && newSource is AvaloniaObject ao)
        {
            ao.PropertyChanged += OnPropertyChanged;
            SetValue(ao.GetValue(StyledElement.DataContextProperty));
        }
        else if (newSource is null)
        {
            SetValue(null);
        }
        else
        {
            SetError(new InvalidCastException($"Unable to read DataContext from '{newSource.GetType()}'."));
        }
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender == Source && e.Property == StyledElement.DataContextProperty)
            SetValue(e.NewValue);
    }
}
