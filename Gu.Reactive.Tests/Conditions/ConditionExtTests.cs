namespace Gu.Reactive.Tests.Conditions
{
    using System;
    using System.Reactive.Subjects;

    using Gu.Reactive.Tests.Helpers;

    using NUnit.Framework;

    public class ConditionExtTests
    {
        [Test]
        [Explicit("Not sure if we want caching")]
        public void Caches()
        {
            using (var observable = new Subject<object>())
            {
                using (var condition = new Condition(observable, () => true))
                {
                    var o1 = condition.ObserveIsSatisfiedChanged();
                    var o2 = condition.ObserveIsSatisfiedChanged();
                    Assert.AreSame(o1, o2);
                }
            }
        }

        [Test]
        public void Signals()
        {
            using (var source = new Subject<object>())
            {
                var isSatisfied = false;
                //// ReSharper disable once AccessToModifiedClosure
                using (var condition = new Condition(source, () => isSatisfied))
                {
                    ICondition result = null;
                    using (condition.ObserveIsSatisfiedChanged()
                                    .Subscribe(x => result = x))
                    {
                        isSatisfied = true;
                        source.OnNext(null);
                        Assert.AreSame(condition, result);
                    }
                }
            }
        }

        [Test]
        public void IsInSyncWhenSetupCorrectly()
        {
            var source = new Fake();
            using (var condition = new Condition(source.ObservePropertyChangedSlim(nameof(source.IsTrueOrNull)), () => source.IsTrueOrNull))
            {
                Assert.IsTrue(condition.IsInSync());
                source.IsTrueOrNull = true;
                Assert.IsTrue(condition.IsInSync());
            }
        }

        [Test]
        public void IsInSyncWhenSetupCorrectlyNagated()
        {
            var source = new Fake();
            using (var condition = new Condition(source.ObservePropertyChangedSlim(nameof(source.IsTrueOrNull)), () => source.IsTrueOrNull))
            {
                using (var negated = condition.Negate())
                {
                    Assert.IsTrue(negated.IsInSync());
                    source.IsTrueOrNull = true;
                    Assert.IsTrue(negated.IsInSync());
                }
            }
        }

        [Test]
        public void IsInSyncWhenError()
        {
            var source = new Fake();
            using (var condition = new Condition(source.ObservePropertyChangedSlim(nameof(source.Name)), () => source.IsTrueOrNull))
            {
                Assert.IsTrue(condition.IsInSync());
                source.IsTrueOrNull = true;
                Assert.IsFalse(condition.IsInSync());
            }
        }
    }
}