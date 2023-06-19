using System;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="UntypedBindingExpression"/> which accesses an array with integer
/// indexers.
/// </summary>
internal class ArrayIndexerNode : ExpressionNode
{
    private readonly int[] _indexes;

    public ArrayIndexerNode(int[] indexes)
    {
        _indexes = indexes;
    }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        if (newSource is Array array)
            SetValue(array.GetValue(_indexes));
        else
            ClearValue();
    }
}
