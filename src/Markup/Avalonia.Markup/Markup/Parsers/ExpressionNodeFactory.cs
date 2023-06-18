using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.ExpressionNodes.Reflection;

namespace Avalonia.Markup.Parsers
{
    /// <summary>
    /// Creates <see cref="ExpressionNode"/>s from a <see cref="BindingExpressionGrammar"/>.
    /// </summary>
    internal static class ExpressionNodeFactory
    {
        [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
        public static ExpressionNode[] Create(
            IEnumerable<BindingExpressionGrammar.INode> astNodes,
            Func<string?, string, Type>? typeResolver,
            INameScope? nameScope)
        {
            var result = new List<ExpressionNode>();

            foreach (var astNode in astNodes)
            {
                ExpressionNode? node = astNode switch
                {
                    BindingExpressionGrammar.AncestorNode ancestor => AncestorNode(typeResolver, ancestor),
                    BindingExpressionGrammar.NameNode name => new NamedElementNode(nameScope, name.Name),
                    BindingExpressionGrammar.NotNode => new LogicalNotNode(),
                    BindingExpressionGrammar.PropertyNameNode propName => new PluginPropertyAccessorNode(propName.PropertyName),
                    BindingExpressionGrammar.SelfNode => null,
                    _ => throw new NotSupportedException($"Unsupported binding expression: {astNode}."),
                };

                if (node is not null)
                    result.Add(node);
            }

            return result.ToArray();
        }

        private static LogicalAncestorElementNode AncestorNode(
            Func<string?, string, Type>? typeResolver,
            BindingExpressionGrammar.AncestorNode ancestor)
        {
            Type? type = null;

            if (!string.IsNullOrEmpty(ancestor.TypeName))
            {
                type = LookupType(typeResolver, ancestor.Namespace, ancestor.TypeName);
            }

            return new LogicalAncestorElementNode(type, ancestor.Level);
        }

        private static Type LookupType(
            Func<string?, string, Type>? typeResolver,
            string? @namespace,
            string? name)
        {
            if (name is null)
                throw new InvalidOperationException($"Unable to resolve unnamed type from namespace '{@namespace}'.");
            return typeResolver?.Invoke(@namespace, name) ??
                throw new InvalidOperationException($"Unable to resolve type '{@namespace}:{name}'.");
        }
    }
}
