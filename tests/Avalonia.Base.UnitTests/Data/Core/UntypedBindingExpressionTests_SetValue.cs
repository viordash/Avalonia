using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class UntypedBindingExpressionTests_SetValue
    {
        [Fact]
        public void Should_Set_Simple_Property_Value()
        {
            var data = new Person { Name = "Frank" };
            var target = UntypedBindingExpression.Create(data, o => o.Name, typeof(object));

            using (target.Subscribe(_ => { }))
            {
                target.SetValue("Kups");
            }

            Assert.Equal("Kups", data.Name);
        }

        [Fact]
        public void Should_Set_Attached_Property_Value()
        {
            var data = new AvaloniaObject();
            var target = UntypedBindingExpression.Create(data, o => o[DockPanel.DockProperty], typeof(object));

            using (target.Subscribe(_ => { }))
            {
                target.SetValue(Dock.Right);
            }

            Assert.Equal(Dock.Right, data[DockPanel.DockProperty]);
        }

        [Fact]
        public void Should_Set_Value_On_Simple_Property_Chain()
        {
            var data = new Person { Pet = new Dog { Name = "Fido" } };
            var target = UntypedBindingExpression.Create(data, o => o.Pet.Name, typeof(object));

            using (target.Subscribe(_ => { }))
            {
                target.SetValue("Rover");
            }

            Assert.Equal("Rover", data.Pet.Name);
        }

        [Fact]
        public void Should_Not_Try_To_Set_Value_On_Broken_Chain()
        {
            var data = new Person { Pet = new Dog { Name = "Fido" } };
            var target = UntypedBindingExpression.Create(data, o => o.Pet.Name, typeof(object));

            // Ensure the UntypedBindingExpression's subscriptions are kept active.
            using (target.OfType<string>().Subscribe(x => { }))
            {
                data.Pet = null;
                Assert.False(target.SetValue("Rover"));
            }
        }

        [Fact]
        public void SetValue_Should_Return_False_For_Missing_Property()
        {
            var data = new Person { Pet = new Cat() };
            var target = UntypedBindingExpression.Create(data, o => (o.Pet as Dog).IsBarky, typeof(object));

            using (target.Subscribe(_ => { }))
            {
                Assert.False(target.SetValue("baz"));
            }

            GC.KeepAlive(data);
        }

        ////[Fact]
        ////public void SetValue_Should_Notify_New_Value_With_Inpc()
        ////{
        ////    var data = new Class1();
        ////    var target = UntypedBindingExpression.Create(data, o => o.Foo);
        ////    var result = new List<object>();

        ////    target.Subscribe(x => result.Add(x));
        ////    target.SetValue("bar");

        ////    Assert.Equal(new[] { null, "bar" }, result);

        ////    GC.KeepAlive(data);
        ////}

        ////[Fact]
        ////public void SetValue_Should_Notify_New_Value_Without_Inpc()
        ////{
        ////    var data = new Class1();
        ////    var target = UntypedBindingExpression.Create(data, o => o.Bar);
        ////    var result = new List<object>();

        ////    target.Subscribe(x => result.Add(x));
        ////    target.SetValue("bar");

        ////    Assert.Equal(new[] { null, "bar" }, result);

        ////    GC.KeepAlive(data);
        ////}

        ////[Fact]
        ////public void SetValue_Should_Return_False_For_Missing_Object()
        ////{
        ////    var data = new Class1();
        ////    var target = UntypedBindingExpression.Create(data, o => (o.Next as Class2).Bar);

        ////    using (target.Subscribe(_ => { }))
        ////    {
        ////        Assert.False(target.SetValue("baz"));
        ////    }

        ////    GC.KeepAlive(data);
        ////}

        /// <summary>
        /// Test for #831 - Bound properties are incorrectly updated when changing tab items.
        /// </summary>
        /// <remarks>
        /// There was a bug whereby pushing a null as the source didn't update the leaf node,
        /// causing a subsequent SetValue to update an object that should have become unbound.
        /// </remarks>
        [Fact]
        public void Pushing_Null_To_RootObservable_Updates_Leaf_Node()
        {
            var data = new Person { Pet = new Dog { Name = "Fido" } };
            var rootObservable = new BehaviorSubject<Person>(data);
            var target = UntypedBindingExpression.Create(rootObservable, o => o.Pet.Name, typeof(object));

            using (target.Subscribe(_ => { }))
            {
                rootObservable.OnNext(null);
                target.SetValue("Rover");
                Assert.Equal("Fido", data.Pet.Name);
            }
        }

        private interface IAnimal
        {
            string Name { get; }
            int PropertyChangedSubscriptionCount { get; }
        }

        private class Person : NotifyingBase
        {
            private string _name;
            private IAnimal _pet;

            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
                    RaisePropertyChanged(nameof(Name));
                }
            }

            public IAnimal Pet
            {
                get => _pet;
                set
                {
                    _pet = value;
                    RaisePropertyChanged(nameof(Pet));
                }
            }
        }

        private class Animal : NotifyingBase, IAnimal
        {
            private string _name;

            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

        private class Dog : Animal
        {
            public bool IsBarky { get; set; }
        }

        private class Cat : Animal
        {
        }
    }
}
