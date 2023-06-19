using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using System.Collections.Generic;
using Avalonia.Data.Core.ExpressionNodes;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class CompiledBindingExtension : BindingBase
    {
        public CompiledBindingExtension()
        {
            Path = new CompiledBindingPath();
        }

        public CompiledBindingExtension(CompiledBindingPath path)
        {
            Path = path;
        }

        public CompiledBindingExtension ProvideValue(IServiceProvider provider)
        {
            return new CompiledBindingExtension
            {
                Path = Path,
                Converter = Converter,
                ConverterParameter = ConverterParameter,
                TargetNullValue = TargetNullValue,
                FallbackValue = FallbackValue,
                Mode = Mode,
                Priority = Priority,
                StringFormat = StringFormat,
                Source = Source,
                DefaultAnchor = new WeakReference(provider.GetDefaultAnchor())
            };
        }

        public override InstancedBinding? Initiate(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            var nodes = new List<ExpressionNode>();
            Path.BuildExpression(false, nodes);

            if (Source is null)
                nodes.Insert(0, new DataContextNode());

            var expression = new UntypedBindingExpression(
                Source ?? target,
                nodes,
                targetProperty?.PropertyType ?? typeof(object));

            return new InstancedBinding(expression, Mode, Priority);
        }

        [ConstructorArgument("path")]
        public CompiledBindingPath Path { get; set; }

        public object? Source { get; set; }

        public Type? DataType { get; set; }
    }
}
