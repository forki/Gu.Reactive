namespace Gu.Reactive
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    using Gu.Reactive.Internals;

    /// <summary>
    /// A base class for swapping out an <see cref="IEnumerable{T}"/> source and get notifications.
    /// </summary>
    [Serializable]
    public abstract class ReadonlySerialViewBase<TSourceItem, TITem> : IRefreshAble, IList, IReadOnlyList<TITem>, IDisposable, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private static readonly IReadOnlyList<TSourceItem> EmptySource = new TSourceItem[0];

        private readonly Func<IEnumerable<TSourceItem>, IEnumerable<TITem>> mapper;
        private readonly CollectionSynchronizer<TITem> tracker;

        private bool disposed;
        private IEnumerable<TSourceItem> source;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadonlySerialViewBase{TSourceItem, TITem}"/> class.
        /// </summary>
        protected ReadonlySerialViewBase(IEnumerable<TSourceItem> source, Func<IEnumerable<TSourceItem>, IEnumerable<TITem>> mapper, bool starteEmpty = false)
        {
            Ensure.NotNull(mapper, nameof(mapper));
            this.mapper = mapper;
            this.source = source ?? EmptySource;
            this.tracker = new CollectionSynchronizer<TITem>(starteEmpty ? mapper(EmptySource) : mapper(source));
        }

        /// <inheritdoc/>
        [field: NonSerialized]
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <inheritdoc/>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc/>
        public bool IsReadOnly => true;

        /// <inheritdoc/>
        public bool IsFixedSize => true;

        /// <inheritdoc/>
        public int Count => this.ThrowIfDisposed(() => this.tracker.Count);

        /// <inheritdoc/>
        object ICollection.SyncRoot => ((ICollection)this.tracker).SyncRoot;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => false;

        /// <summary>
        /// Returns true if there are any subscribers to the <see cref="PropertyChanged"/> or <see cref="CollectionChanged"/> events.
        /// </summary>
        protected bool HasListeners => this.PropertyChanged != null || this.CollectionChanged != null;

        /// <summary>
        /// The collection synchronizer.
        /// </summary>
        protected CollectionSynchronizer<TITem> Tracker => this.tracker;

        /// <summary>
        /// The source collection.
        /// </summary>
        protected IEnumerable<TSourceItem> Source
        {
            get
            {
                return this.source;
            }

            private set
            {
                if (ReferenceEquals(value, this.source))
                {
                    return;
                }

                lock (this.source.SyncRootOrDefault(this.SyncRoot()))
                {
                    if (ReferenceEquals(value, this.source))
                    {
                        return;
                    }

                    this.source = value;
                    this.OnPropertyChanged();
                }
            }
        }

        /// <inheritdoc/>
        public TITem this[int index] => this.ThrowIfDisposed(() => this.tracker[index]);

        /// <inheritdoc/>
        [SuppressMessage("ReSharper", "ValueParameterNotUsed")]
        object IList.this[int index]
        {
            get { return this[index]; }
            set { ThrowHelper.ThrowCollectionIsReadonly(); }
        }

        /// <inheritdoc/>
        public IEnumerator<TITem> GetEnumerator() => this.ThrowIfDisposed(() => this.tracker.GetEnumerator());

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <inheritdoc/>
        int IList.Add(object value) => ThrowHelper.ThrowCollectionIsReadonly<int>();

        /// <inheritdoc/>
        bool IList.Contains(object value) => this.tracker.Contains(value);

        /// <inheritdoc/>
        void IList.Clear() => ThrowHelper.ThrowCollectionIsReadonly();

        /// <inheritdoc/>
        int IList.IndexOf(object value) => this.tracker.IndexOf(value);

        /// <inheritdoc/>
        void IList.Insert(int index, object value) => ThrowHelper.ThrowCollectionIsReadonly();

        /// <inheritdoc/>
        void IList.Remove(object value) => ThrowHelper.ThrowCollectionIsReadonly();

        /// <inheritdoc/>
        void IList.RemoveAt(int index) => ThrowHelper.ThrowCollectionIsReadonly();

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index) => ((ICollection)this.tracker).CopyTo(array, index);

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Refresh this collection from <see cref="Source"/> and notify changes.
        /// </summary>
        public virtual void Refresh()
        {
            this.ThrowIfDisposed();
            lock (this.source.SyncRootOrDefault(this.SyncRoot()))
            {
                (this.Source as IRefreshAble)?.Refresh();
                if (this.HasListeners)
                {
                    this.tracker.Reset(this.mapper(this.source ?? EmptySource), this.OnPropertyChanged, this.OnCollectionChanged);
                }
                else
                {
                    this.tracker.Reset(this.mapper(this.Source ?? EmptySource));
                }
            }
        }

        /// <summary>
        /// Update the source collection and notify about changes.
        /// </summary>
        protected virtual void SetSource(IEnumerable<TSourceItem> newSource)
        {
            this.SetSourceCore(newSource);
        }

        /// <summary>
        /// Update the source collection and notify about changes.
        /// </summary>
        protected void SetSourceCore(IEnumerable<TSourceItem> newSource)
        {
            this.ThrowIfDisposed();
            this.Source = newSource ?? EmptySource;
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">true: safe to free managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.Source = EmptySource;
                this.tracker.Clear();
            }
        }

        /// <summary>
        /// Set <see cref="Source"/> to empty and notify about changes.
        /// </summary>
        protected virtual void ClearSource()
        {
            this.ThrowIfDisposed();
            this.Source = EmptySource;
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the instance is disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the instance is disposed.
        /// Invokes <paramref name="action"/> if not disposed.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        protected void ThrowIfDisposed(Action action)
        {
            this.ThrowIfDisposed();
            action();
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the instance is disposed.
        /// Returns <paramref name="result"/> if not disposed.
        /// </summary>
        /// <param name="result">The action to invoke.</param>
        protected TResult ThrowIfDisposed<TResult>(Func<TResult> result)
        {
            this.ThrowIfDisposed();
            return result();
        }

        /// <summary>
        /// Synchronize with source and notify about changes.
        /// </summary>
        protected virtual void Refresh(IReadOnlyList<NotifyCollectionChangedEventArgs> changes)
        {
            lock (this.source.SyncRootOrDefault(this.SyncRoot()))
            {
                if (this.HasListeners)
                {
                    this.tracker.Refresh(this.mapper(this.source ?? EmptySource), changes, this.OnPropertyChanged, this.OnCollectionChanged);
                }
                else
                {
                    this.tracker.Refresh(this.mapper(this.source ?? EmptySource), changes);
                }
            }
        }

        /// <summary>
        /// Raise CollectionChanged event to any listeners.
        /// Properties/methods modifying this <see cref="EditableListView{T}"/> will raise
        /// a collection changed event through this virtual method.
        /// </summary>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this.CollectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Raise PropertyChanged event to any listeners.
        /// Properties/methods modifying this <see cref="ReadonlySerialViewBase{TSource,TMapped}"/> will raise
        /// a property changed event through this virtual method.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raise PropertyChanged event to any listeners.
        /// Properties/methods modifying this <see cref="EditableListView{T}"/> will raise
        /// a property changed event through this virtual method.
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            this.PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Notify about the changes [PropertyChanged("Count"), PropertyChanged("Item[]", CollectionChanged].
        /// Count is not signalled for move and replace.
        ///  If the list has more than one change a reste notification is performed.
        /// </summary>
        protected virtual void Notify(IReadOnlyList<NotifyCollectionChangedEventArgs> changes)
        {
            if (changes == null ||
                changes.Count == 0)
            {
                return;
            }

            if (changes.Count == 1)
            {
                var change = changes[0];
                this.Notify(change);
            }
            else
            {
                this.Notify(CachedEventArgs.NotifyCollectionReset);
            }
        }

        /// <summary>
        /// Notify about the changes [PropertyChanged("Count"), PropertyChanged("Item[]", CollectionChanged].
        /// Count is not signalled for move and replace.
        /// </summary>
        protected virtual void Notify(NotifyCollectionChangedEventArgs change)
        {
            if (change == null)
            {
                return;
            }

            switch (change.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    this.OnPropertyChanged(CachedEventArgs.CountPropertyChanged);
                    this.OnPropertyChanged(CachedEventArgs.IndexerPropertyChanged);
                    this.OnCollectionChanged(change);
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    this.OnPropertyChanged(CachedEventArgs.IndexerPropertyChanged);
                    this.OnCollectionChanged(change);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    this.OnPropertyChanged(CachedEventArgs.CountPropertyChanged);
                    this.OnPropertyChanged(CachedEventArgs.IndexerPropertyChanged);
                    this.OnCollectionChanged(change);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}