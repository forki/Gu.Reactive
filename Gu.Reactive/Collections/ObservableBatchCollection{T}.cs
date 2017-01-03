﻿// ReSharper disable StaticMemberInGenericType
namespace Gu.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;

    /// <summary>
    /// An <see cref="ObservableCollection{T}"/> with support for AddRange and RemoveRange
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    [Serializable]
    public class ObservableBatchCollection<T> : ObservableCollection<T>
    {
        private static readonly PropertyChangedEventArgs CountPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Count));
        private static readonly PropertyChangedEventArgs IndexerPropertyChangedEventArgs = new PropertyChangedEventArgs("Item[]");
        private static readonly NotifyCollectionChangedEventArgs NotifyCollectionResetEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableBatchCollection{T}"/> class.
        /// It is empty and has default initial capacity.
        /// </summary>
        public ObservableBatchCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableBatchCollection{T}"/> class.
        /// It contains elements copied from the specified list
        /// </summary>
        /// <param name="list">The list whose elements are copied to the new list.</param>
        /// <remarks>
        /// The elements are copied onto the ObservableCollection in the
        /// same order they are read by the enumerator of the list.
        /// </remarks>
        /// <exception cref="ArgumentNullException"> list is a null reference </exception>
        public ObservableBatchCollection(IList<T> list)
            : base(list)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableBatchCollection{T}"/> class.
        /// It contains the elements copied from the specified collection and has sufficient capacity
        /// to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <remarks>
        /// The elements are copied onto the ObservableCollection in the
        /// same order they are read by the enumerator of the collection.
        /// </remarks>
        /// <exception cref="ArgumentNullException"> collection is a null reference </exception>
        public ObservableBatchCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }

        /// <summary>
        /// Add a range of elements. If the count is greater than 1 one reset event is raised when done.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            this.CheckReentrancy();
            var count = 0;
            var added = default(T);
            foreach (var item in items)
            {
                if (count > 0)
                {
                    this.Items.Add(item);
                }
                else
                {
                    added = item;
                    this.Items.Add(item);
                }

                count++;
            }

            if (count == 0)
            {
                return;
            }

            if (count == 1)
            {
                this.OnPropertyChanged(CountPropertyChangedEventArgs);
                this.OnPropertyChanged(IndexerPropertyChangedEventArgs);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, added, this.Items.Count - 1));
            }
            else
            {
                this.RaiseReset();
            }
        }

        /// <summary>
        /// Remove a range of elements. If the count is greater than 1 one reset event is raised when done.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void RemoveRange(IEnumerable<T> items)
        {
            this.CheckReentrancy();
            var count = 0;
            var index = -1;
            var removed = default(T);
            foreach (var item in items)
            {
                if (count > 0)
                {
                    this.Items.Remove(item);
                }
                else
                {
                    removed = item;
                    index = this.Items.IndexOf(item);
                    this.Items.RemoveAt(index);
                }

                count++;
            }

            if (count == 0)
            {
                return;
            }

            if (count == 1)
            {
                this.OnPropertyChanged(CountPropertyChangedEventArgs);
                this.OnPropertyChanged(IndexerPropertyChangedEventArgs);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, index));
            }
            else
            {
                this.RaiseReset();
            }
        }

        private void RaiseReset()
        {
            this.OnPropertyChanged(CountPropertyChangedEventArgs);
            this.OnPropertyChanged(IndexerPropertyChangedEventArgs);
            this.OnCollectionChanged(NotifyCollectionResetEventArgs);
        }
    }
}
