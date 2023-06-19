using System;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

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
            Path.BuildExpression(nodes, out var isRooted);

            if (Source is null && !isRooted)
            {
                nodes.Insert(0, ExpressionNodeFactory.CreateDataContext(targetProperty));
            }

            var expression = new UntypedBindingExpression(
                Source ?? target,
                nodes,
                FallbackValue);

            return new InstancedBinding(expression, Mode, Priority);
        }

        [ConstructorArgument("path")]
        public CompiledBindingPath Path { get; set; }

        public object? Source { get; set; }

        public Type? DataType { get; set; }
    }
}
