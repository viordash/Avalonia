using System;
using System.Collections;

namespace Avalonia.Data.Core;

internal class ListIndexerNode : ExpressionNode
{
    private int _index;

    public ListIndexerNode(int index)
    {
        _index = index;
    }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        UpdateValue(newSource);
    }

    private void UpdateValue(object? source) 
    {
        try
        {
            object? value = null;
            if (source is IList list)
                value = list[_index];
            SetValue(value);
        }
        catch (Exception e)
        {
            SetError(e);
        }
    }
}
