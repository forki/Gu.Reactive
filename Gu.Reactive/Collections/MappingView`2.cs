﻿namespace Gu.Reactive
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Gu.Reactive.Internals;

    public class MappingView<TSource, TResult> : IReadOnlyObservableCollection<TResult>, IDisposable
        where TResult : class
    {
        private readonly IEnumerable<TSource> _source;
        private readonly IScheduler _scheduler;
        private readonly List<TResult> _mapped;
        private readonly IDisposable _changeSubscription;
        private readonly IMappingFactory<TSource, TResult> _factory;
        private bool _disposed;

        public MappingView(ObservableCollection<TSource> source, Func<TSource, TResult> selector, IScheduler scheduler)
            : this(source, scheduler, selector)
        {
        }

        public MappingView(IObservableCollection<TSource> source, Func<TSource, TResult> selector, IScheduler scheduler)
            : this(source, scheduler, selector)
        {
        }

        public MappingView(ReadOnlyObservableCollection<TSource> source, Func<TSource, TResult> selector, IScheduler scheduler)
            : this(source, scheduler, selector)
        {
        }

        public MappingView(IReadOnlyObservableCollection<TSource> source, Func<TSource, TResult> selector, IScheduler scheduler)
            : this(source, scheduler, selector)
        {
        }

        private MappingView(IEnumerable<TSource> source, IScheduler scheduler, Func<TSource, TResult> selector)
        {
            Ensure.NotNull(source, "source");
            Ensure.NotNull(source as INotifyCollectionChanged, "source");
            Ensure.NotNull(selector, "selector");
            _source = source;
            _scheduler = scheduler;
            _factory = MappingFactory.Create(selector);
            _mapped = source.Select(GetOrCreateValue)
                            .ToList();
            var incc = source as INotifyCollectionChanged;
            var observable = Observable.Create<NotifyCollectionChangedEventArgs>(o =>
            {
                NotifyCollectionChangedEventHandler fsHandler = (_, e) =>
                {
                    o.OnNext(e);
                };
                incc.CollectionChanged += fsHandler;
                return Disposable.Create(() => incc.CollectionChanged -= fsHandler);
            });
            _changeSubscription = observable.Subscribe(OnSourceCollectionChanged);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count
        {
            get { return _mapped.Count; }
        }

        public TResult this[int index]
        {
            get { return _mapped[index]; }
        }

        public void Refresh()
        {
            VerifyDisposed();
            var mapped = _source.Select(GetOrCreateValue)
                                .ToArray();
            var change = Diff.CollectionChange(_mapped, mapped);
            _mapped.Clear();
            _mapped.AddRange(mapped);
            Notifier.Notify(this, change, _scheduler, PropertyChanged, CollectionChanged);
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            VerifyDisposed();
            return _mapped.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _changeSubscription.Dispose();
            Dispose(true);
        }

        protected void VerifyDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        protected virtual TResult GetOrCreateValue(TSource key)
        {
            return _factory.GetOrCreateValue(key);
        }

        protected virtual void OnSourceCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            var index = e.NewStartingIndex;
                            var value = GetOrCreateValue(_source.ElementAt(index));
                            _mapped.Insert(index, value);
                            var change = Diff.CreateAddEventArgs(value, index);
                            Notifier.Notify(this, change, _scheduler, PropertyChanged, CollectionChanged);
                            break;
                        }
                    case NotifyCollectionChangedAction.Remove:
                        {
                            var index = e.OldStartingIndex;
                            var value = _mapped[index];
                            _mapped.RemoveAt(index);
                            var change = Diff.CreateRemoveEventArgs(value, index);
                            Notifier.Notify(this, change, _scheduler, PropertyChanged, CollectionChanged);
                            break;
                        }
                    case NotifyCollectionChangedAction.Replace:
                        {
                            var index = e.NewStartingIndex;
                            var value = GetOrCreateValue(_source.ElementAt(index));
                            var oldValue = _mapped[e.OldStartingIndex];
                            _mapped[index] = value;
                            var change = Diff.CreateReplaceEventArgs(value, oldValue, index);
                            Notifier.Notify(this, change, _scheduler, PropertyChanged, CollectionChanged);
                            break;
                        }

                    case NotifyCollectionChangedAction.Move:
                        {
                            var value = _mapped[e.OldStartingIndex];
                            _mapped.RemoveAt(e.OldStartingIndex);
                            _mapped.Insert(e.NewStartingIndex, value);
                            var change = Diff.CreateMoveEventArgs(value, e.NewStartingIndex, e.OldStartingIndex);
                            Notifier.Notify(this, change, _scheduler, PropertyChanged, CollectionChanged);
                            break;
                        }

                    case NotifyCollectionChangedAction.Reset:
                        Refresh();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception)
            {
                Refresh(); // Resetting
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _changeSubscription.Dispose();
                _factory.Dispose();
            }
        }
    }
}
