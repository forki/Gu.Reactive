﻿namespace Gu.Reactive
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reactive.Concurrency;
    using Gu.Reactive.Internals;

    /// <summary>
    /// A fixed size queue that notifies.
    /// </summary>
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [Serializable]
    public class ObservableFixedSizeQueue<T> : FixedSizedQueue<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly IScheduler scheduler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableFixedSizeQueue{T}"/> class.
        /// </summary>
        /// <param name="size">The maximum number of elements in the queue.</param>
        public ObservableFixedSizeQueue(int size)
            : this(size, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableFixedSizeQueue{T}"/> class.
        /// </summary>
        /// <param name="size">The maximum number of elements in the queue.</param>
        /// <param name="scheduler">The scheduler to notify collection changes on.</param>
        public ObservableFixedSizeQueue(int size, IScheduler scheduler)
            : base(size)
        {
            this.scheduler = scheduler;
        }

        /// <inheritdoc/>
        [field: NonSerialized]
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <inheritdoc/>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc/>
        public override void Enqueue(T item)
        {
            var count = this.Count;
            var overflow = default(T);
            if (this.Count >= this.Size)
            {
                while (this.Count >= this.Size && this.TryDequeue(out overflow))
                {
                }
            }

            base.Enqueue(item);
            if (this.Count != count)
            {
                this.OnPropertyChanged(CachedEventArgs.CountPropertyChanged);
            }

            if (count >= this.Size)
            {
                this.OnCollectionChanged(Diff.CreateRemoveEventArgs(overflow, 0));
            }

            this.OnCollectionChanged(Diff.CreateAddEventArgs(item, this.Count - 1));
        }

        /// <summary>
        /// Raise PropertyChanged event to any listeners.
        /// Properties/methods modifying this <see cref="ObservableFixedSizeQueue{T}"/> will raise
        /// a property changed event through this virtual method.
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            this.PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Raise CollectionChanged event to any listeners.
        /// Properties/methods modifying this <see cref="ObservableSet{T}"/> will raise
        /// a collection changed event through this virtual method.
        /// </summary>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.scheduler == null)
            {
                this.CollectionChanged?.Invoke(this, e);
                return;
            }

            var handler = this.CollectionChanged;
            if (handler != null)
            {
                this.scheduler.Schedule(() => handler.Invoke(this, e)).IgnoreReturnValue();
            }
        }
    }
}
