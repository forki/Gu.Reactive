﻿namespace Gu.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Gu.Reactive.Internals;

    /// <summary>
    /// A view of a collection that maps the values.
    /// </summary>
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {this.Count}")]
    public class MappingView<TSource, TResult> : ReadonlySerialViewBase<TSource, TResult>, IReadOnlyObservableCollection<TResult>, IUpdater
    {
        private readonly CompositeDisposable updateSubscription = new CompositeDisposable();
        private readonly IMapper<TSource, TResult> factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(ObservableCollection<TSource> source, Func<TSource, TResult> selector, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, triggers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(IObservableCollection<TSource> source, Func<TSource, TResult> selector, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, triggers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(ReadOnlyObservableCollection<TSource> source, Func<TSource, TResult> selector, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, triggers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(IReadOnlyObservableCollection<TSource> source, Func<TSource, TResult> selector, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, triggers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(ObservableCollection<TSource> source, Func<TSource, int, TResult> selector, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, null, triggers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(IObservableCollection<TSource> source, Func<TSource, int, TResult> selector, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, null, triggers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(ReadOnlyObservableCollection<TSource> source, Func<TSource, int, TResult> selector, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, null, triggers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(IReadOnlyObservableCollection<TSource> source, Func<TSource, int, TResult> selector, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, null, triggers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(ObservableCollection<TSource> source, Func<TSource, int, TResult> selector, Func<TResult, int, TResult> updater, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, updater, triggers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(IObservableCollection<TSource> source, Func<TSource, int, TResult> selector, Func<TResult, int, TResult> updater, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, updater, triggers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(ReadOnlyObservableCollection<TSource> source, Func<TSource, int, TResult> selector, Func<TResult, int, TResult> updater, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, updater, triggers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingView{TSource, TResult}"/> class.
        /// </summary>
        public MappingView(IReadOnlyObservableCollection<TSource> source, Func<TSource, int, TResult> selector, Func<TResult, int, TResult> updater, IScheduler scheduler, params IObservable<object>[] triggers)
            : this(source, scheduler, selector, updater, triggers)
        {
        }

        private MappingView(IEnumerable<TSource> source, IScheduler scheduler, Func<TSource, TResult> selector, params IObservable<object>[] triggers)
           : this(source, scheduler, MappingFactory.Create(selector), triggers)
        {
        }

        private MappingView(IEnumerable<TSource> source, IScheduler scheduler, Func<TSource, int, TResult> selector, Func<TResult, int, TResult> updater, params IObservable<object>[] triggers)
            : this(source, scheduler, MappingFactory.Create(selector, updater), triggers)
        {
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private MappingView(IEnumerable<TSource> source, IScheduler scheduler, IMapper<TSource, TResult> factory, params IObservable<object>[] triggers)
            : base(source, s => s.Select(factory.GetOrCreateValue))
        {
            Ensure.NotNull(source as INotifyCollectionChanged, nameof(source));
            Ensure.NotNull(factory, nameof(factory));

            this.factory = factory;
            this.updateSubscription.Add(ThrottledRefresher.Create(this, source, TimeSpan.Zero, scheduler, false)
                                                          .ObserveOn(scheduler ?? Scheduler.Immediate)
                                                          .Subscribe(this.OnSourceCollectionChanged));
            if (triggers != null && triggers.Any(t => t != null))
            {
                var triggerSubscription = triggers.Merge()
                                                  .ObserveOn(scheduler ?? Scheduler.Immediate)
                                                  .Subscribe(_ => this.Refresh());
                this.updateSubscription.Add(triggerSubscription);
            }
        }

        /// <inheritdoc/>
        object IUpdater.CurrentlyUpdatingSourceItem => null;

        /// <summary>
        /// Delegates creation to mapping factory.
        /// </summary>
        protected virtual TResult GetOrCreateValue(TSource key, int index) => this.factory.GetOrCreateValue(key, index);

        /// <summary>
        /// Delegates updating of item at index to mapping factory.
        /// </summary>
        /// <param name="index">The index to update the item for.</param>
        /// <param name="createEventArgOnUpdate">If a <see cref="NotifyCollectionChangedEventArgs"/> for the update should be created.</param>
        /// <returns>
        /// The <see cref="NotifyCollectionChangedEventArgs"/> update causes or null.
        /// If the updated instance is the same reference null is returned.
        /// </returns>
        protected virtual NotifyCollectionChangedEventArgs UpdateAt(int index, bool createEventArgOnUpdate)
        {
            var old = this.Tracker[index];
            var updated = this.factory.UpdateIndex(this.Source.ElementAt(index), old, index);
            if (ReferenceEquals(updated, old))
            {
                return null;
            }

            this.Tracker[index] = updated;
            return createEventArgOnUpdate
                ? Diff.CreateReplaceEventArgs(updated, old, index)
                : null;
        }

        /// <summary>
        /// Delegates updating of items at and above index to mapping factory.
        /// This happens after an item is inserted, removed or moved.
        /// </summary>
        /// <param name="from">The index to start update of the item for.</param>
        /// <param name="to">The index to end update of the item for.</param>
        /// <returns>The collection changed args the update causes.</returns>
        protected virtual List<NotifyCollectionChangedEventArgs> UpdateRange(int @from, int to)
        {
            var changes = new List<NotifyCollectionChangedEventArgs>();
            for (var i = @from; i < Math.Min(this.Count, to + 1); i++)
            {
                var change = this.UpdateAt(i, changes.Count < 2);
                if (change != null)
                {
                    changes.Add(change);
                }
            }

            return changes;
        }

        /// <summary>
        /// Called when the source collection changed.
        /// </summary>
        /// <param name="changeCollection">The changes accumulated during the buffer time.</param>
        protected virtual void OnSourceCollectionChanged(IReadOnlyList<NotifyCollectionChangedEventArgs> changeCollection)
        {
            if (changeCollection == null || changeCollection.Count == 0)
            {
                return;
            }

            if (changeCollection.Count > 1)
            {
                this.Refresh();
                return;
            }

            var singleChange = changeCollection[0];
            switch (singleChange.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var index = singleChange.NewStartingIndex;
                        var value = this.GetOrCreateValue(this.Source.ElementAt(index), index);
                        this.Tracker.Insert(index, value);
                        var changes = this.UpdateRange(index + 1, this.Count - 1);
                        changes.Add(Diff.CreateAddEventArgs(value, index));
                        this.Notify(changes);
                        break;
                    }

                case NotifyCollectionChangedAction.Remove:
                    {
                        var index = singleChange.OldStartingIndex;
                        var value = this.Tracker[index];
                        this.Tracker.RemoveAt(index);
                        var changes = this.UpdateRange(index, this.Count - 1);
                        changes.Add(Diff.CreateRemoveEventArgs(value, index));
                        this.Notify(changes);
                        break;
                    }

                case NotifyCollectionChangedAction.Replace:
                    {
                        var index = singleChange.NewStartingIndex;
                        var value = this.GetOrCreateValue(this.Source.ElementAt(index), index);
                        var oldValue = this.Tracker[singleChange.OldStartingIndex];
                        this.Tracker[index] = value;
                        var change = Diff.CreateReplaceEventArgs(value, oldValue, index);
                        this.Notify(change);
                        break;
                    }

                case NotifyCollectionChangedAction.Move:
                    {
                        var value = this.Tracker[singleChange.OldStartingIndex];
                        this.Tracker.RemoveAt(singleChange.OldStartingIndex);
                        this.Tracker.Insert(singleChange.NewStartingIndex, value);
                        this.UpdateAt(singleChange.OldStartingIndex, false);
                        this.UpdateAt(singleChange.NewStartingIndex, false);
                        var changes = this.UpdateRange(Math.Min(singleChange.OldStartingIndex, singleChange.NewStartingIndex), Math.Min(singleChange.OldStartingIndex, singleChange.NewStartingIndex));
                        changes.Add(Diff.CreateMoveEventArgs(value, singleChange.NewStartingIndex, singleChange.OldStartingIndex));
                        this.Notify(changes);
                        break;
                    }

                case NotifyCollectionChangedAction.Reset:
                    this.Refresh();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this.factory.Refresh(this.Source, this.Tracker, e);
            base.OnCollectionChanged(e);
        }

        /// <summary>
        /// Disposes of a <see cref="MappingView{TSource,TResult}"/>.
        /// </summary>
        /// <remarks>
        /// Called from Dispose() with disposing=true.
        /// Guidelines:
        /// 1. We may be called more than once: do nothing after the first call.
        /// 2. Avoid throwing exceptions if disposing is false, i.e. if we're being finalized.
        /// </remarks>
        /// <param name="disposing">True if called from Dispose(), false if called from the finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.updateSubscription.Dispose();
                this.factory.Dispose();
                foreach (var item in this)
                {
                    (item as IDisposable)?.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
