using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data;
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
        public static void CreateFromAst(
            IEnumerable<BindingExpressionGrammar.INode> astNodes,
            Func<string?, string, Type>? typeResolver,
            INameScope? nameScope,
            List<ExpressionNode> result)
        {
            foreach (var astNode in astNodes)
            {
                ExpressionNode? node = astNode switch
                {
                    BindingExpressionGrammar.AncestorNode ancestor => LogicalAncestorNode(typeResolver, ancestor),
                    BindingExpressionGrammar.AttachedPropertyNameNode attached => AttachedPropertyNode(typeResolver, attached),
                    BindingExpressionGrammar.NameNode name => new NamedElementNode(nameScope, name.Name),
                    BindingExpressionGrammar.NotNode => new LogicalNotNode(),
                    BindingExpressionGrammar.PropertyNameNode propName => new PluginPropertyAccessorNode(propName.PropertyName),
                    BindingExpressionGrammar.SelfNode => null,
                    _ => throw new NotSupportedException($"Unsupported binding expression: {astNode}."),
                };

                if (node is not null)
                    result.Add(node);
            }
        }

        public static ExpressionNode? CreateRelativeSource(RelativeSource source, Func<string?, string, Type>? typeResolver)
        {
            return source.Mode switch
            {
                RelativeSourceMode.DataContext => new DataContextNode(),
                RelativeSourceMode.TemplatedParent => new TemplatedParentNode(),
                RelativeSourceMode.Self => null,
                RelativeSourceMode.FindAncestor when source.Tree == TreeType.Logical =>
                    new LogicalAncestorElementNode(source.AncestorType, source.AncestorLevel),
                RelativeSourceMode.FindAncestor when source.Tree == TreeType.Visual =>
                    new VisualAncestorElementNode(source.AncestorType, source.AncestorLevel),
            };
        }

        private static AvaloniaPropertyAccessorNode AttachedPropertyNode(
            Func<string?, string, Type>? typeResolver,
            BindingExpressionGrammar.AttachedPropertyNameNode attached)
        {
            var type = LookupType(typeResolver, attached.Namespace, attached.TypeName);
            var property = AvaloniaPropertyRegistry.Instance.FindRegistered(type, attached.PropertyName) ??
                throw new InvalidOperationException($"Cannot find property {type}.{attached.PropertyName}.");
            return new AvaloniaPropertyAccessorNode(property);
        }

        private static LogicalAncestorElementNode LogicalAncestorNode(
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
