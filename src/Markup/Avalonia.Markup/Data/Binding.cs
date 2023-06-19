using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipes;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Markup.Parsers;
using Avalonia.Utilities;

namespace Avalonia.Data
{
    /// <summary>
    /// A XAML binding.
    /// </summary>
    [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
    public class Binding : BindingBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Binding"/> class.
        /// </summary>
        public Binding()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Binding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        /// <param name="mode">The binding mode.</param>
        public Binding(string path, BindingMode mode = BindingMode.Default)
            : base(mode)
        {
            Path = path;
        }

        /// <summary>
        /// Gets or sets the name of the element to use as the binding source.
        /// </summary>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets the relative source for the binding.
        /// </summary>
        public RelativeSource? RelativeSource { get; set; }

        /// <summary>
        /// Gets or sets the source for the binding.
        /// </summary>
        public object? Source { get; set; }

        /// <summary>
        /// Gets or sets the binding path.
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Gets or sets a function used to resolve types from names in the binding path.
        /// </summary>
        public Func<string?, string, Type>? TypeResolver { get; set; }

        public override InstancedBinding? Initiate(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            var nodes = new List<ExpressionNode>();

            if (!string.IsNullOrEmpty(Path))
            {
                var reader = new CharacterReader(Path.AsSpan());
                var (astNodes, sourceMode) = BindingExpressionGrammar.Parse(ref reader);
                ExpressionNodeFactory.CreateFromAst(astNodes, TypeResolver, GetNameScope(), nodes);
            }

            if (CreateSourceNode(targetProperty) is { } sourceNode)
                nodes.Insert(0, sourceNode);

            var expression = new UntypedBindingExpression(
                Source ?? target,
                nodes,
                FallbackValue,
                converter: Converter,
                converterParameter: ConverterParameter,
                targetTypeConverter: TargetTypeConverter.Create(targetProperty));
            return new InstancedBinding(expression, Mode, Priority);
        }

        private INameScope? GetNameScope()
        {
            INameScope? result = null;
            NameScope?.TryGetTarget(out result);
            return result;
        }

        private ExpressionNode? CreateSourceNode(AvaloniaProperty? targetProperty)
        {
            if (Source is not null)
                return null;

            if (!string.IsNullOrEmpty(ElementName))
            {
                var nameScope = GetNameScope() ?? throw new InvalidOperationException(
                    "Cannot create ElementName binding when NameScope is null");
                return new NamedElementNode(nameScope, ElementName);
            }

            if (RelativeSource is not null)
                return ExpressionNodeFactory.CreateRelativeSource(RelativeSource);

            if (targetProperty == StyledElement.DataContextProperty)
                return new ParentDataContextNode();

            return new DataContextNode();
        }
    }
}
