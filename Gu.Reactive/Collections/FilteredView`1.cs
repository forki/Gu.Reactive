﻿namespace Gu.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Gu.Reactive.Collections;

    /// <summary>
    /// Typed CollectionView for intellisense in xaml
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public partial class FilteredView<T> : SynchronizedEditableView<T>, IFilteredView<T>, IReadOnlyFilteredView<T>
    {
        private readonly ObservableCollection<IObservable<object>> _triggers;
        private readonly IScheduler _scheduler;
        private readonly SerialDisposable _refreshSubscription = new SerialDisposable();
        private Func<T, bool> _filter;
        private bool _disposed = false;
        private TimeSpan _bufferTime;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">The collection to wrap</param>
        /// <param name="filter"></param>
        /// <param name="bufferTime">The time to defer updates, useful if many triggers fire in short time. Then it will be only one Reset</param>
        /// <param name="scheduler">The scheduler used when throttling. The collection changed events are raised on this scheduler</param>
        /// <param name="triggers">Triggers when to re evaluate the filter</param>
        public FilteredView(
            IList<T> source,
            Func<T, bool> filter,
            TimeSpan bufferTime,
            IScheduler scheduler,
            params IObservable<object>[] triggers)
            : base(source)
        {
            if (filter == null)
            {
                filter = x => true;
            }
            _scheduler = scheduler;
            _bufferTime = bufferTime;
            _filter = filter;
            if (triggers == null || triggers.Length == 0)
            {
                _triggers = new ObservableCollection<IObservable<object>>();
            }
            else
            {
                _triggers = new ObservableCollection<IObservable<object>>(triggers);
            }

            _refreshSubscription.Disposable = FilteredRefresher.Create(bufferTime, triggers, source, scheduler, false)
                                                               .Subscribe(Refresh);
            var observables = new IObservable<object>[]
                                            {
                                                this.Triggers.ObserveCollectionChanged(false),
                                                this.ObservePropertyChanged(x => x.Filter, false),
                                                this.ObservePropertyChanged(x => x.BufferTime, false)
                                            };
            observables.Merge()
                       .ThrottleOrDefault(bufferTime, scheduler)
                       .Subscribe(_ => _refreshSubscription.Disposable = FilteredRefresher.Create(bufferTime, triggers, source, scheduler, true)
                                                                                          .Subscribe(Refresh));
        }

        public Func<T, bool> Filter
        {
            get
            {
                VerifyDisposed();
                return _filter;
            }
            set
            {
                VerifyDisposed();
                if (Equals(value, _filter))
                {
                    return;
                }
                _filter = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<IObservable<object>> Triggers
        {
            get
            {
                VerifyDisposed();
                return _triggers;
            }
        }

        public TimeSpan BufferTime
        {
            get
            {
                VerifyDisposed();
                return _bufferTime;
            }
            set
            {
                VerifyDisposed();
                if (value.Equals(_bufferTime))
                {
                    return;
                }
                _bufferTime = value;
                OnPropertyChanged();
            }
        }

        public override void Refresh()
        {
            VerifyDisposed();
            Synchronized.Reset(this, Filtered(), _scheduler, PropertyChangedEventHandler, NotifyCollectionChangedEventHandler);
        }

        protected override void Refresh(IReadOnlyList<NotifyCollectionChangedEventArgs> changes)
        {
            Synchronized.Refresh(this, Filtered(), changes, _scheduler, PropertyChangedEventHandler, NotifyCollectionChangedEventHandler);
        }

        protected IEnumerable<T> Filtered()
        {
            if (Filter == null)
            {
                return Source;
            }
            return Source.Where(_filter);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern. 
        /// </summary>
        /// <param name="disposing">true: safe to free managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (disposing)
            {
                _refreshSubscription.Dispose();
            }
        }
    }
}
