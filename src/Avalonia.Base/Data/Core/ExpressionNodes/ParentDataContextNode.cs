using System;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="UntypedBindingExpression"/> which selects the value of the visual
/// parent's DataContext.
/// </summary>
internal class ParentDataContextNode : ExpressionNode
{
    private AvaloniaObject? _parent;

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        if (oldSource is AvaloniaObject oldElement)
            oldElement.PropertyChanged -= OnPropertyChanged;
        if (newSource is AvaloniaObject newElement)
            newElement.PropertyChanged += OnPropertyChanged;

        if (newSource is Visual v)
            SetParent(v.GetValue(Visual.VisualParentProperty));
        else
            SetParent(null);
    }

    private void SetParent(AvaloniaObject? parent)
    {
        if (parent == _parent)
            return;

        Unsubscribe();
        _parent = parent;

        if (_parent is IDataContextProvider)
        {
            _parent.PropertyChanged += OnParentPropertyChanged;
            SetValue(_parent.GetValue(StyledElement.DataContextProperty));
        }
    }

    private void Unsubscribe()
    {
        if (_parent is not null)
            _parent.PropertyChanged -= OnParentPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.VisualParentProperty)
            SetParent(e.NewValue as AvaloniaObject);
    }

    private void OnParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == StyledElement.DataContextProperty)
            SetValue(e.NewValue);
    }
}
