using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="UntypedBindingExpression"/> which uses a function to transform its
/// value.
/// </summary>
internal class FuncTransformNode : ExpressionNode
{
    private readonly Func<object?, object?> _transform;

    public FuncTransformNode(Func<object?, object?> transform)
    {
        _transform = transform;
    }

    protected override void OnSourceChanged(object? oldSource, object? newSource)
    {
        SetValue(_transform(newSource));
    }
}
