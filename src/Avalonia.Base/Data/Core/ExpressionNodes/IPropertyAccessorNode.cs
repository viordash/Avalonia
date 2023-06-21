using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// Indicates that a <see cref="ExpressionNode"/> accesses a property on an object. Used by data
/// validatation plugins to iden.
/// </summary>
internal interface IPropertyAccessorNode
{
    string PropertyName { get; }
    IPropertyAccessor? Accessor { get; }
}
