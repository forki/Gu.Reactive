namespace Gu.Reactive.Tests.Reflection
{
    using System;

    using Gu.Reactive.Internals;
    using Gu.Reactive.Tests.Helpers;

    using NUnit.Framework;

    public class GetterTests
    {
        [Test]
        public void FakeIsTrue()
        {
            var pathProperty = Getter.GetOrCreate(typeof(Fake).GetProperty(nameof(Fake.IsTrue)));
            Assert.IsInstanceOf<NotifyingGetter<Fake, bool>>(pathProperty);
        }

        [Test]
        public void FakeOfIntNext()
        {
            var pathProperty = Getter.GetOrCreate(typeof(Fake<int>).GetProperty(nameof(Fake<int>.Next)));
            Assert.IsInstanceOf<NotifyingGetter<Fake<int>, Level<int>>>(pathProperty);
        }

        [Test]
        public void ThrowsOnWriteOnly()
        {
            var propertyInfo = typeof(Fake).GetProperty("WriteOnly");
            Assert.NotNull(propertyInfo);
            //// ReSharper disable once ObjectCreationAsStatement
            var exception = Assert.Throws<ArgumentException>(() => Getter.GetOrCreate(propertyInfo));
            var expected = "Property cannot be write only.\r\n" +
                           "The property Gu.Reactive.Tests.Helpers.Fake.WriteOnly does not have a getter.\r\n" +
                           "Parameter name: property";
            Assert.AreEqual(expected, exception.Message);
        }

        [Test]
        public void ThrowsOnNullProp()
        {
            // ReSharper disable once ObjectCreationAsStatement
            var exception = Assert.Throws<ArgumentNullException>(() => Getter.GetOrCreate(null));
            Assert.AreEqual("Value cannot be null.\r\nParameter name: property", exception.Message);
        }

        [Test]
        public void Caching()
        {
            var getter1 = Getter.GetOrCreate(typeof(Fake).GetProperty("Name"));
            var getter2 = Getter.GetOrCreate(typeof(Fake).GetProperty("Name"));
            Assert.AreSame(getter1, getter2);

            var getter3 = Getter.GetOrCreate(typeof(Level).GetProperty("Name"));
            var getter4 = Getter.GetOrCreate(typeof(Level).GetProperty("Name"));
            Assert.AreSame(getter3, getter4);

            Assert.AreNotSame(getter1, getter3);
        }

        [Test]
        public void GetValue()
        {
            var source = new Fake { Name = "meh" };
            var getter = Getter.GetOrCreate(typeof(Fake).GetProperty("Name"));
            Assert.AreEqual("meh", getter.GetValue(source));
            var genericGetter = (Getter<Fake, string>)getter;
            Assert.AreEqual("meh", genericGetter.GetValue(source));
        }

        [Test]
        public void GetValueStruct()
        {
            var source = new StructLevel { Name = "meh" };
            var getter = Getter.GetOrCreate(typeof(StructLevel).GetProperty("Name"));
            Assert.AreEqual("meh", getter.GetValue(source));
            var genericGetter = (Getter<StructLevel, string>)getter;
            Assert.AreEqual("meh", genericGetter.GetValue(source));
        }

        [Test]
        public void GetValueGeneric()
        {
            var source = new Fake<int> { Value = 1 };
            var getter = Getter.GetOrCreate(typeof(Fake<int>).GetProperty("Value"));
            Assert.AreEqual(1, getter.GetValue(source));
            var genericGetter = (Getter<Fake<int>, int>)getter;
            Assert.AreEqual(1, genericGetter.GetValue(source));
        }

        [Test]
        public void GetValueGenerics()
        {
            var intFake = new Fake<int> { Value = 1 };
            var intgetter = Getter.GetOrCreate(typeof(Fake<int>).GetProperty("Value"));
            Assert.AreEqual(1, intgetter.GetValue(intFake));
            var genericintGetter = (Getter<Fake<int>, int>)intgetter;
            Assert.AreEqual(1, genericintGetter.GetValue(intFake));

            var doubleFake = new Fake<double> { Value = 1 };
            var doublegetter = Getter.GetOrCreate(typeof(Fake<double>).GetProperty("Value"));
            Assert.AreEqual(1, doublegetter.GetValue(doubleFake));
            var genericdoubleGetter = (Getter<Fake<double>, double>)doublegetter;
            Assert.AreEqual(1, genericdoubleGetter.GetValue(doubleFake));
        }

        [Test]
        public void GetValueGenericStruct()
        {
            var source = new StructLevel { Name = "meh" };
            var getter = (Getter<StructLevel, string>)Getter.GetOrCreate(typeof(StructLevel).GetProperty("Name"));
            Assert.AreEqual("meh", getter.GetValue(source));
        }

        [Test]
        public void GetValueViaDelegate()
        {
            var source = new Fake { Name = "meh" };
            var propertyInfo = typeof(Fake).GetProperty("Name");
            Assert.AreEqual("meh", propertyInfo.GetValueViaDelegate(source));
        }
    }
}