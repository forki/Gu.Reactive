﻿// ReSharper disable UnusedVariable
namespace Gu.Reactive.Tests.Sandbox
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    using Gu.Reactive.Internals;
    using Gu.Reactive.Tests.Helpers;

    using NUnit.Framework;

    [Explicit("Sandbox")]
    public class ItemsObservableBoxTests
    {
        [TestCase(1000)]
        public void AddNested(int n)
        {
            var source = new ObservableCollection<Fake>();
            var path = NotifyingPath.GetOrCreate<Fake, int>(x => x.Next.Next.Value);
            using (var view = source.AsMappingView(x => x.ObservePropertyChanged(path, signalInitial: true)))
            {
                var sw = Stopwatch.StartNew();
                for (var i = 0; i < n; i++)
                {
                    var fake = new Fake();
                    source.Add(fake);
                }

                sw.Stop();
                Console.WriteLine("// source.ObserveItemPropertyChanged(x => x.Next.Next.Value): {0} Adds took {1} ms {2:F3} ms each. {3}", n, sw.ElapsedMilliseconds, sw.Elapsed.TotalMilliseconds / n, DateTime.Now.ToShortDateString());
            }
        }

        [TestCase(1000)]
        public void AddNestedMerge(int n)
        {
            var source = new ObservableCollection<Fake>();
            var path = NotifyingPath.GetOrCreate<Fake, int>(x => x.Next.Next.Value);
            using (var view = source.AsMappingView(x => x.ObservePropertyChanged(path, signalInitial: true)))
            {
                var sw = Stopwatch.StartNew();
                using (var subject = new Subject<IObservable<EventPattern<PropertyChangedEventArgs>>>())
                {
                    using (subject.Switch()
                                  .Publish()
                                  .RefCount()
                                  .Subscribe(_ => { }))
                    {
                        for (var i = 0; i < n; i++)
                        {
                            var fake = new Fake();
                            source.Add(fake);
                            subject.OnNext(view.Merge());
                        }
                    }
                }

                sw.Stop();
                Console.WriteLine("// source.ObserveItemPropertyChanged(x => x.Next.Next.Value): {0} Adds took {1} ms {2:F3} ms each. {3}", n, sw.ElapsedMilliseconds, sw.Elapsed.TotalMilliseconds / n, DateTime.Now.ToShortDateString());
            }
        }

        [TestCase(1000)]
        public void AddNestedThatUpdates(int n)
        {
            var source = new ObservableCollection<Fake>();
            var path = NotifyingPath.GetOrCreate<Fake, int>(x => x.Next.Next.Value);
            using (var view = source.AsMappingView(x => x.ObservePropertyChanged(path, signalInitial: true)))
            {
                var sw = Stopwatch.StartNew();
                for (var i = 0; i < n; i++)
                {
                    var fake = new Fake();
                    source.Add(fake);
                    fake.Next = new Level { Next = new Level { Value = 1 } };
                }

                sw.Stop();
                Console.WriteLine("// source.ObserveItemPropertyChanged(x => x.Next.Next.Value): {0} Adds took {1} ms {2:F3} ms each. {3}", n, sw.ElapsedMilliseconds, sw.Elapsed.TotalMilliseconds / n, DateTime.Now.ToShortDateString());
            }
        }
    }
}
