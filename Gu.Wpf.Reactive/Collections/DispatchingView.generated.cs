﻿namespace Gu.Wpf.Reactive
{
    using System;
    using System.Collections.ObjectModel;

    using Gu.Reactive;

    /// <summary>
    /// Factory methods for creating <see cref="DispatchingView{T}"/>
    /// </summary>
    public static partial class DispatchingView
    {
        /// <summary>
        /// Create a <see cref="DispatchingView{T}"/> for <paramref name="collection"/>
        /// </summary>
        /// <typeparam name="TItem">The type of the items in <paramref name="collection"/></typeparam>
        /// <param name="collection">The source collection.</param>
        /// <returns>A <see cref="DispatchingView{T}"/></returns>
        public static DispatchingView<TItem> AsDispatchingView<TItem>(this IObservableCollection<TItem> collection)
        {
            return new DispatchingView<TItem>(collection, TimeSpan.Zero);
        }

        /// <summary>
        /// Create a <see cref="DispatchingView{T}"/> for <paramref name="collection"/>
        /// </summary>
        /// <typeparam name="TItem">The type of the items in <paramref name="collection"/></typeparam>
        /// <param name="collection">The source collection.</param>
        /// <param name="bufferTime">The time to buffer changes before notifying.</param>
        /// <returns>A <see cref="DispatchingView{T}"/></returns>
        public static DispatchingView<TItem> AsDispatchingView<TItem>(this IObservableCollection<TItem> collection, TimeSpan bufferTime)
        {
            return new DispatchingView<TItem>(collection, bufferTime);
        }

        /// <summary>
        /// Create a <see cref="DispatchingView{T}"/> for <paramref name="collection"/>
        /// </summary>
        /// <typeparam name="TItem">The type of the items in <paramref name="collection"/></typeparam>
        /// <param name="collection">The source collection.</param>
        /// <returns>A <see cref="DispatchingView{T}"/></returns>
        public static DispatchingView<TItem> AsDispatchingView<TItem>(this ObservableCollection<TItem> collection)
        {
            return new DispatchingView<TItem>(collection, TimeSpan.Zero);
        }

        /// <summary>
        /// Create a <see cref="DispatchingView{T}"/> for <paramref name="collection"/>
        /// </summary>
        /// <typeparam name="TItem">The type of the items in <paramref name="collection"/></typeparam>
        /// <param name="collection">The source collection.</param>
        /// <param name="bufferTime">The time to buffer changes before notifying.</param>
        /// <returns>A <see cref="DispatchingView{T}"/></returns>
        public static DispatchingView<TItem> AsDispatchingView<TItem>(this ObservableCollection<TItem> collection, TimeSpan bufferTime)
        {
            return new DispatchingView<TItem>(collection, bufferTime);
        }

        /// <summary>
        /// Create a <see cref="ReadOnlyDispatchingView{T}"/> for <paramref name="collection"/>
        /// </summary>
        /// <typeparam name="TItem">The type of the items in <paramref name="collection"/></typeparam>
        /// <param name="collection">The source collection.</param>
        /// <returns>A <see cref="DispatchingView{T}"/></returns>
        public static ReadOnlyDispatchingView<TItem> AsDispatchingView<TItem>(this ReadOnlyObservableCollection<TItem> collection)
        {
            return new ReadOnlyDispatchingView<TItem>(collection, TimeSpan.Zero);
        }

        /// <summary>
        /// Create a <see cref="ReadOnlyDispatchingView{T}"/> for <paramref name="collection"/>
        /// </summary>
        /// <typeparam name="TItem">The type of the items in <paramref name="collection"/></typeparam>
        /// <param name="collection">The source collection.</param>
        /// <param name="bufferTime">The time to buffer changes before notifying.</param>
        /// <returns>A <see cref="DispatchingView{T}"/></returns>
        public static ReadOnlyDispatchingView<TItem> AsDispatchingView<TItem>(this ReadOnlyObservableCollection<TItem> collection, TimeSpan bufferTime)
        {
            return new ReadOnlyDispatchingView<TItem>(collection, bufferTime);
        }

        /// <summary>
        /// Create a <see cref="ReadOnlyDispatchingView{T}"/> for <paramref name="collection"/>
        /// </summary>
        /// <typeparam name="TItem">The type of the items in <paramref name="collection"/></typeparam>
        /// <param name="collection">The source collection.</param>
        /// <returns>A <see cref="DispatchingView{T}"/></returns>
        public static ReadOnlyDispatchingView<TItem> AsDispatchingView<TItem>(this IReadOnlyObservableCollection<TItem> collection)
        {
            return new ReadOnlyDispatchingView<TItem>(collection, TimeSpan.Zero);
        }

        /// <summary>
        /// Create a <see cref="ReadOnlyDispatchingView{T}"/> for <paramref name="collection"/>
        /// </summary>
        /// <typeparam name="TItem">The type of the items in <paramref name="collection"/></typeparam>
        /// <param name="collection">The source collection.</param>
        /// <param name="bufferTime">The time to buffer changes before notifying.</param>
        /// <returns>A <see cref="DispatchingView{T}"/></returns>
        public static ReadOnlyDispatchingView<TItem> AsDispatchingView<TItem>(this IReadOnlyObservableCollection<TItem> collection, TimeSpan bufferTime)
        {
            return new ReadOnlyDispatchingView<TItem>(collection, bufferTime);
        }
    }
}