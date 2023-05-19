using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Primitives;
using Avalonia.Rendering;

namespace Avalonia.Controls;

internal interface IGroupRadioButton
{
    string? GroupName { get; }
    bool IsChecked { get; set; }
}

internal class RadioButtonGroupManager
{
    public static readonly RadioButtonGroupManager Default = new RadioButtonGroupManager();
    static readonly ConditionalWeakTable<IRenderRoot, RadioButtonGroupManager> s_registeredVisualRoots
        = new ConditionalWeakTable<IRenderRoot, RadioButtonGroupManager>();

    readonly Dictionary<string, List<WeakReference<IGroupRadioButton>>> s_registeredGroups
        = new Dictionary<string, List<WeakReference<IGroupRadioButton>>>();

    public static RadioButtonGroupManager GetOrCreateForRoot(IRenderRoot? root)
    {
        if (root == null)
            return Default;
        return s_registeredVisualRoots.GetValue(root, key => new RadioButtonGroupManager());
    }

    public void Add(IGroupRadioButton radioButton)
    {
        lock (s_registeredGroups)
        {
            string groupName = radioButton.GroupName!;
            if (!s_registeredGroups.TryGetValue(groupName, out var group))
            {
                group = new List<WeakReference<IGroupRadioButton>>();
                s_registeredGroups.Add(groupName, group);
            }
            group.Add(new WeakReference<IGroupRadioButton>(radioButton));
        }
    }

    public void Remove(IGroupRadioButton radioButton, string oldGroupName)
    {
        lock (s_registeredGroups)
        {
            if (!string.IsNullOrEmpty(oldGroupName) && s_registeredGroups.TryGetValue(oldGroupName, out var group))
            {
                int i = 0;
                while (i < group.Count)
                {
                    if (!group[i].TryGetTarget(out var button) || button == radioButton)
                    {
                        group.RemoveAt(i);
                        continue;
                    }
                    i++;
                }
                if (group.Count == 0)
                {
                    s_registeredGroups.Remove(oldGroupName);
                }
            }
        }
    }

    public void SetChecked(IGroupRadioButton radioButton)
    {
        lock (s_registeredGroups)
        {
            string groupName = radioButton.GroupName!;
            if (s_registeredGroups.TryGetValue(groupName, out var group))
            {
                int i = 0;
                while (i < group.Count)
                {
                    if (!group[i].TryGetTarget(out var current))
                    {
                        group.RemoveAt(i);
                        continue;
                    }
                    if (current != radioButton && current.IsChecked)
                        current.IsChecked = false;
                    i++;
                }
                if (group.Count == 0)
                {
                    s_registeredGroups.Remove(groupName);
                }
            }
        }
    }
}
