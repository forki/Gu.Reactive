#pragma warning disable CS0618 // Type or member is obsolete
namespace Gu.Wpf.Reactive.Tests.Collections.MutableViews
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Reactive.Subjects;

    using Gu.Reactive;
    using Gu.Reactive.Tests.Helpers;
    using Gu.Wpf.Reactive.Tests.FakesAndHelpers;
    using Microsoft.Reactive.Testing;

    using NUnit.Framework;

    public class FilteredViewTriggersTests
    {
        [SetUp]
        public void SetUp()
        {
            App.Start();
        }

        [Test]
        public void ManyOnNextsOneReset()
        {
            var source = new ObservableCollection<int> { 1, 2, 3 };
            var scheduler = new TestScheduler();
            using (var trigger = new Subject<object>())
            {
                using (var view = new FilteredView<int>(source, x => true, TimeSpan.FromMilliseconds(10), scheduler, trigger))
                {
                    using (var actualChanges = view.SubscribeAll())
                    {
                        source.Clear();
                        for (var i = 0; i < 10; i++)
                        {
                            trigger.OnNext(null);
                        }

                        CollectionAssert.IsEmpty(actualChanges);
                        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, view);
                        scheduler.Start();

                        var expected = new EventArgs[]
                        {
                            CachedEventArgs.CountPropertyChanged,
                            CachedEventArgs.IndexerPropertyChanged,
                            CachedEventArgs.NotifyCollectionReset
                        };
                        CollectionAssert.AreEqual(expected, actualChanges, EventArgsComparer.Default);
                        CollectionAssert.IsEmpty(view);
                    }
                }
            }
        }

        [Test]
        public void ManyOnNextsOneAdd()
        {
            using (var trigger = new Subject<object>())
            {
                var source = new ObservableCollection<int> { 1, 2, 3 };
                var scheduler = new TestScheduler();
                using (var view = new FilteredView<int>(source, x => true, TimeSpan.FromMilliseconds(10), scheduler, trigger))
                {
                    using (var actualChanges = view.SubscribeAll())
                    {
                        source.Add(4);
                        for (var i = 0; i < 10; i++)
                        {
                            trigger.OnNext(null);
                        }

                        CollectionAssert.IsEmpty(actualChanges);
                        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, view);

                        scheduler.Start();

                        var expected = new EventArgs[]
                        {
                            CachedEventArgs.CountPropertyChanged,
                            CachedEventArgs.IndexerPropertyChanged,
                            Diff.CreateAddEventArgs(4, 3)
                        };
                        CollectionAssert.AreEqual(expected, actualChanges, EventArgsComparer.Default);
                        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, view);
                    }
                }
            }
        }

        [Test]
        public void AddTriggerThenManyOnNextsOneAdd()
        {
            var source = new ObservableCollection<int> { 1, 2, 3 };
            var scheduler = new TestScheduler();
            using (var trigger = new Subject<object>())
            {
                using (var view = new FilteredView<int>(source, x => true, TimeSpan.FromMilliseconds(10), scheduler, trigger))
                {
                    using (var actualChanges = view.SubscribeAll())
                    {
                        source.Add(4);
                        for (var i = 0; i < 10; i++)
                        {
                            trigger.OnNext(null);
                        }

                        CollectionAssert.IsEmpty(actualChanges);
                        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, view);

                        scheduler.Start();

                        var expected = new EventArgs[]
                        {
                            CachedEventArgs.CountPropertyChanged,
                            CachedEventArgs.IndexerPropertyChanged,
                            Diff.CreateAddEventArgs(4, 3)
                        };
                        CollectionAssert.AreEqual(expected, actualChanges, EventArgsComparer.Default);
                        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, view);
                    }
                }
            }
        }

        [Test]
        public void UpdatesAndNotifiesOnCollectionChanged()
        {
            var ints = new ObservableCollection<int> { 1, 2, 3 };
            using (var expected = ints.SubscribeAll())
            {
                var scheduler = new TestScheduler();
                using (var view = new FilteredView<int>(ints, x => true, TimeSpan.FromMilliseconds(10), scheduler))
                {
                    using (var actual = view.SubscribeAll())
                    {
                        ints.Add(4);
                        CollectionAssert.IsEmpty(actual);
                        scheduler.Start();
                        CollectionAssert.AreEqual(expected, actual, EventArgsComparer.Default);
                    }
                }
            }
        }

        [Test]
        public void UpdatesAndNotifiesOnObservableCollectionChangedWhenFiltered()
        {
            var source = new ObservableCollection<int> { 1, 2 };
            using (var view = source.AsFilteredView(x => true))
            {
                source.Add(1);
                using (var actual = view.SubscribeAll())
                {
                    view.Filter = x => x < 2;
                    view.Refresh();
                    var expected = new EventArgs[]
                                       {
                                           CachedEventArgs.GetOrCreatePropertyChangedEventArgs("Filter"),
                                           CachedEventArgs.CountPropertyChanged,
                                           CachedEventArgs.IndexerPropertyChanged,
                                           Diff.CreateRemoveEventArgs(2, 1),
                                       };
                    CollectionAssert.AreEqual(expected, actual, EventArgsComparer.Default);
                    CollectionAssert.AreEqual(new[] { 1, 1 }, view);
                }
            }
        }
    }
}