﻿namespace Gu.Reactive.Tests.Trackers
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    using JetBrains.Annotations;

    using NUnit.Framework;

    public partial class MinTrackerTests
    {
        public class Nested
        {
            [TestCase(true)]
            [TestCase(false)]
            public void InitializesWithValues(bool trackItemChanges)
            {
                var source = new ObservableCollection<Dummy>(new[] { new Dummy(1), new Dummy(2), new Dummy(3) });
                using (var tracker = source.TrackMin(x => x.Value))
                {
                    Assert.AreEqual(1, tracker.Value);
                }
            }

            [Test]
            public void InitializesWhenEmpty()
            {
                var source = new ObservableCollection<Dummy>(new Dummy[0]);
                using (var tracker = source.TrackMin(x => x.Value))
                {
                    Assert.AreEqual(null, tracker.Value);
                }
            }

            [Test]
            public void ReactsAndNotifiesOnSourceCollectionChanges()
            {
                var source = new ObservableCollection<Dummy>(new[] { new Dummy(1), new Dummy(2), new Dummy(3) });
                using (var tracker = source.TrackMin(x => x.Value))
                {
                    Assert.AreEqual(1, tracker.Value);
                    int count = 0;
                    using (tracker.ObservePropertyChanged(x => x.Value, false)
                                  .Subscribe(_ => count++))
                    {
                        source.RemoveAt(1);
                        Assert.AreEqual(0, count);
                        Assert.AreEqual(1, tracker.Value);

                        source.RemoveAt(0);
                        Assert.AreEqual(1, count);
                        Assert.AreEqual(3, tracker.Value);

                        source.RemoveAt(0);
                        Assert.AreEqual(2, count);
                        Assert.AreEqual(null, tracker.Value);

                        source.Add(new Dummy(4));
                        Assert.AreEqual(3, count);
                        Assert.AreEqual(4, tracker.Value);
                    }
                }
            }

            [Test]
            public void ReactsAndNotifiesOnItemChanges()
            {
                var source = new ObservableCollection<Dummy>(new[] { new Dummy(1), new Dummy(2), new Dummy(3) });
                using (var tracker = source.TrackMin(x => x.Value))
                {
                    Assert.AreEqual(1, tracker.Value);
                    int count = 0;
                    using (tracker.ObservePropertyChanged(x => x.Value, false)
                                  .Subscribe(_ => count++))
                    {
                        source[1].Value = -3;
                        Assert.AreEqual(1, count);
                        Assert.AreEqual(-3, tracker.Value);
                    }
                }
            }

            public class Dummy : INotifyPropertyChanged
            {
                private int value;

                public Dummy(int value)
                {
                    this.value = value;
                }

                public event PropertyChangedEventHandler PropertyChanged;

                public int Value
                {
                    get
                    {
                        return this.value;
                    }

                    set
                    {
                        if (value == this.value)
                        {
                            return;
                        }

                        this.value = value;
                        this.OnPropertyChanged();
                    }
                }

                [NotifyPropertyChangedInvocator]
                protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
                {
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
}