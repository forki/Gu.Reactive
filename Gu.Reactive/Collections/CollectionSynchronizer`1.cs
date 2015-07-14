namespace Gu.Reactive
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Concurrency;

    public class CollectionSynchronizer<T>
    {
        internal static readonly IReadOnlyList<NotifyCollectionChangedEventArgs> EmptyArgs = new NotifyCollectionChangedEventArgs[0];
        internal static readonly IReadOnlyList<NotifyCollectionChangedEventArgs> ResetArgs = new[] { Diff.NotifyCollectionResetEventArgs };
        private readonly List<T> _current = new List<T>();
        public CollectionSynchronizer(IEnumerable<T> source)
        {
            _current.AddRange(source);
        }

        internal IReadOnlyList<T> Current
        {
            get
            {
                lock (_current)
                {
                    return _current;
                }
            }
        }

        internal void Reset(
            object sender,
            IReadOnlyList<T> updated,
            IScheduler scheduler,
            PropertyChangedEventHandler propertyChanged,
            NotifyCollectionChangedEventHandler collectionChanged)
        {
            Refresh(sender, updated, EmptyArgs, scheduler, propertyChanged, collectionChanged);
        }

        internal void Refresh(
            object sender,
            IReadOnlyList<T> updated,
            IReadOnlyList<NotifyCollectionChangedEventArgs> collectionChanges,
            IScheduler scheduler,
            PropertyChangedEventHandler propertyChanged,
            NotifyCollectionChangedEventHandler collectionChanged)
        {
            lock (_current)
            {
                var change = Update(updated, collectionChanges, collectionChanged != null);
                Notifier.Notify(sender, change, scheduler, propertyChanged, collectionChanged);
            }
        }

        private NotifyCollectionChangedEventArgs Update(IReadOnlyList<T> updated, IReadOnlyList<NotifyCollectionChangedEventArgs> collectionChanges, bool calculateDiff)
        {
            NotifyCollectionChangedEventArgs change = calculateDiff
                                                          ? Diff.CollectionChange(_current, updated, collectionChanges)
                                                          : null;
            if (change != null)
            {
                _current.Clear();
                _current.AddRange(updated);
            }
            return change;
        }

        #region Ilist & IList<T>

        public int IndexOf(T value)
        {
            lock (_current)
            {
                return _current.IndexOf(value);
            }
        }

        public int IndexOf(object value)
        {
            lock (_current)
            {
                return ((IList)_current).IndexOf(value);
            }
        }

        public int LastIndexOf(T value)
        {
            lock (_current)
            {
                return _current.LastIndexOf(value);
            }
        }

        public bool Contains(T value)
        {
            lock (_current)
            {
                return _current.Contains(value);
            }
        }

        public bool Contains(object value)
        {
            lock (_current)
            {
                return ((IList)_current).Contains(value);
            }
        }

        public void CopyTo(Array array, int index)
        {
            lock (_current)
            {
                ((IList)_current).CopyTo(array, index);
            }
        }

        #endregion Ilist & IList<T>
    }
}