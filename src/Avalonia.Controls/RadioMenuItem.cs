using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Controls;

/// <summary>
/// Represents a menu item that allows a user to select a single option from a group of options.
/// </summary>
public class RadioMenuItem : ToggleMenuItem, IGroupRadioButton
{
    /// <inheritdoc cref="RadioButton.GroupNameProperty"/>
    public static readonly StyledProperty<string?> GroupNameProperty =
        RadioButton.GroupNameProperty.AddOwner<RadioMenuItem>();

    private RadioButtonGroupManager? _groupManager;

    /// <inheritdoc cref="RadioButton.GroupName"/>
    public string? GroupName
    {
        get => GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    bool IGroupRadioButton.IsChecked
    {
        get => IsChecked;
        set => SetCurrentValue(IsCheckedProperty, value);
    }

    protected override void Toggle()
    {
        if (!IsChecked && !HasSubMenu)
        {
            SetCurrentValue(IsCheckedProperty, true);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (!string.IsNullOrEmpty(GroupName))
        {
            _groupManager?.Remove(this, GroupName);

            _groupManager = RadioButtonGroupManager.GetOrCreateForRoot(e.Root);

            _groupManager.Add(this);
        }
        base.OnAttachedToVisualTree(e);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (!string.IsNullOrEmpty(GroupName))
        {
            _groupManager?.Remove(this, GroupName);
        }
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
        {
            IsCheckedChanged(change.GetNewValue<bool>());
        }
        else if (change.Property == GroupNameProperty)
        {
            var (oldValue, newValue) = change.GetOldAndNewValue<string?>();
            OnGroupNameChanged(oldValue, newValue);
        }
    }

    private void OnGroupNameChanged(string? oldGroupName, string? newGroupName)
    {
        if (!string.IsNullOrEmpty(oldGroupName))
        {
            _groupManager?.Remove(this, oldGroupName);
        }

        if (!string.IsNullOrEmpty(newGroupName))
        {
            if (_groupManager == null)
            {
                _groupManager = RadioButtonGroupManager.GetOrCreateForRoot(this.GetVisualRoot());
            }

            _groupManager.Add(this);
        }
    }

    private void IsCheckedChanged(bool value)
    {
        var groupName = GroupName;
        if (string.IsNullOrEmpty(groupName))
        {
            var parent = this.GetVisualParent();

            if (value && parent != null)
            {
                var siblings = parent
                    .GetVisualChildren()
                    .OfType<RadioMenuItem>()
                    .Where(x => x != this && string.IsNullOrEmpty(x.GroupName));

                foreach (var sibling in siblings)
                {
                    if (sibling.IsChecked)
                        sibling.SetCurrentValue(IsCheckedProperty, false);
                }
            }
        }
        else
        {
            if (value && _groupManager != null)
            {
                _groupManager.SetChecked(this);
            }
        }

        if (value)
        {
            if (((IMenuItem)this).Parent is IGroupRadioButton parentRadioMenuItem)
            {
                parentRadioMenuItem.IsChecked = true;
            }
        }
    }
}
