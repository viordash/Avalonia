using Avalonia.Controls.Metadata;

namespace Avalonia.Controls;

[PseudoClasses(":checked", ":unchecked")]
public class ToggleMenuItem : MenuItem
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<ToggleMenuItem, bool>(nameof(IsChecked));

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    protected override void OnClick()
    {
        Toggle();
        base.OnClick();
    }

    protected virtual void Toggle()
    {
        if (!HasSubMenu)
        {
            var newValue = !IsChecked;

            SetCurrentValue(IsCheckedProperty, newValue);
        }
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
        {
            var newValue = change.GetNewValue<bool>();
            UpdatePseudoClasses(newValue);
        }
    }
    
    private void UpdatePseudoClasses(bool isChecked)
    {
        PseudoClasses.Set(":checked", isChecked);
        PseudoClasses.Set(":unchecked", !isChecked);
    }
}
