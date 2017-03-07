namespace Gu.Reactive.Tests.Helpers
{
    using System.ComponentModel;
    using System.Reactive;

    using NUnit.Framework;

    public static class EventPatternAssert
    {
        public static void AreEqual(object sender, string propertyName, EventPattern<PropertyChangedEventArgs> pattern)
        {
            Assert.AreSame(sender, pattern.Sender);
            Assert.AreEqual(propertyName, pattern.EventArgs.PropertyName);
        }

        public static void AreEqual<T>(object sender, string propertyName, Maybe<T> value, EventPattern<PropertyChangedAndValueEventArgs<T>> pattern)
        {
            Assert.AreSame(sender, pattern.Sender);
            Assert.AreEqual(propertyName, pattern.EventArgs.PropertyName);
            if (value.HasValue)
            {
                Assert.AreEqual(true, pattern.EventArgs.HasValue);
                Assert.AreEqual(value.Value, pattern.EventArgs.Value);
            }
            else
            {
                Assert.AreEqual(false, pattern.EventArgs.HasValue);
                Assert.AreEqual(default(T), pattern.EventArgs.Value);
            }
        }
    }
}