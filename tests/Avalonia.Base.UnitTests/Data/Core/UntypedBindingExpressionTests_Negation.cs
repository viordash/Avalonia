using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Data.Core;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class UntypedBindingExpressionTests_Negation
    {
        [Fact]
        public async Task Should_Negate_Boolean_Value()
        {
            var data = new { Foo = true };
            var target = UntypedBindingExpression.Create(data, o => !o.Foo);
            var result = await target.Take(1);

            Assert.False((bool)result);

            GC.KeepAlive(data);
        }

        ////[Fact]
        ////public void Can_SetValue_For_Valid_Value()
        ////{
        ////    var data = new Test { Foo = true };
        ////    var target = UntypedBindingExpression.Create(data, o => !o.Foo, typeof(object));
        ////    target.Subscribe(_ => { });

        ////    Assert.True(target.SetValue(true));

        ////    Assert.False(data.Foo);
        ////}

        private class Test
        {
            public bool Foo { get; set; }
        }
    }
}
