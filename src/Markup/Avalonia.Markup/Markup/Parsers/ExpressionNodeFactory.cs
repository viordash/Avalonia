using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data.Core;

namespace Avalonia.Markup.Parsers
{
    /// <summary>
    /// Creates <see cref="ExpressionNode"/>s from a <see cref="BindingExpressionGrammar"/>.
    /// </summary>
    internal static class ExpressionNodeFactory
    {
        [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
        public static ExpressionNode[] Create(IEnumerable<BindingExpressionGrammar.INode> astNodes)
        {
            var result = new List<ExpressionNode>();

            foreach (var astNode in astNodes)
            {
                ExpressionNode node = astNode switch
                {
                    BindingExpressionGrammar.NotNode => new LogicalNotNode(),
                    BindingExpressionGrammar.PropertyNameNode propName => new PropertyAccessorNode(propName.PropertyName),
                    _ => throw new NotSupportedException($"Unsupported binding expression grammar: {astNode}."),
                };

                result.Add(node);
            }

            return result.ToArray();
        }
    }
}
