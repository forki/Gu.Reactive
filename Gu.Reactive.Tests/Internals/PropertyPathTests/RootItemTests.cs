﻿namespace Gu.Reactive.Tests.Internals.PropertyPathTests
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reactive;

    using Gu.Reactive.Internals;

    using NUnit.Framework;

    public class RootItemTests
    {
        [Test]
        public void SignalsValue()
        {
            using (var item = new RootPropertyTracker(null))
            {
                var changes = new List<EventPattern<PropertyChangedEventArgs>>();
                item.ObservePropertyChanged(x => x.Value, false)
                    .Subscribe(changes.Add);
                Assert.AreEqual(0, changes.Count);
                item.Value = new object();
                Assert.AreEqual(1, changes.Count);
            }
        }

        [Test]
        public void Signals()
        {
            int count = 0;
            using (var item = new RootPropertyTracker(null))
            {
                item.ObservePropertyChanged(x => x.Source, false)
                    .Subscribe(_ => count++);
                Assert.AreEqual(0, count);
                item.Value = new object();
                Assert.AreEqual(1, count);
            }
        }
    }
}