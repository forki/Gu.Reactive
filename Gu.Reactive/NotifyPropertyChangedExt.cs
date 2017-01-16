﻿namespace Gu.Reactive
{
    using System;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Gu.Reactive.Internals;
    using Gu.Reactive.PropertyPathStuff;

    /// <summary>
    /// Extension methods for subscribing to property changes.
    /// </summary>
    public static class NotifyPropertyChangedExt
    {
        /// <summary>
        /// Extension method for listening to property changes.
        /// Handles nested x => x.Level1.Level2.Level3
        /// Unsubscribes &amp; subscribes when each level changes.
        /// Handles nulls.
        /// </summary>
        /// <param name="source">The source instance to track changes for. </param>
        /// <param name="property">
        /// An expression specifying the property path to track.
        /// Example x => x.Foo.Bar.Meh
        ///  </param>
        /// <param name="signalInitial">
        /// If true OnNext is called immediately on subscribe
        /// </param>
        public static IObservable<EventPattern<PropertyChangedEventArgs>> ObservePropertyChanged<TNotifier, TProperty>(
            this TNotifier source,
            Expression<Func<TNotifier, TProperty>> property,
            bool signalInitial = true)
            where TNotifier : INotifyPropertyChanged
        {
            var me = (MemberExpression)property.Body;
            var pe = me.Expression as ParameterExpression;
            if (pe == null)
            {
                var path = PropertyPath.GetOrCreate(property);
                return source.ObservePropertyChanged(path, signalInitial);
            }

            string name = me.Member.Name;
            return source.ObservePropertyChanged(name, signalInitial);
        }

        /// <summary>
        /// Prefer other overloads with x => x.PropertyName for refactor friendliness.
        /// This is faster though.
        /// </summary>
        /// <param name="source"> The source instance to track changes for. </param>
        /// <param name="name"> The name of the property to track. Note that nested properties are not allowed. </param>
        /// <param name="signalInitial"> If true OnNext is called immediately on subscribe </param>
        public static IObservable<EventPattern<PropertyChangedEventArgs>> ObservePropertyChanged(
            this INotifyPropertyChanged source,
            string name,
            bool signalInitial = true)
        {
            var observable = source.ObservePropertyChanged()
                                   .Where(e => IsPropertyName(e.EventArgs, name));
            if (signalInitial)
            {
                var wr = new WeakReference(source);
                return Observable.Defer(
                    () =>
                    {
                        var current = new EventPattern<PropertyChangedEventArgs>(wr.Target, new PropertyChangedEventArgs(name));
                        return Observable.Return(current)
                                         .Concat(observable);
                    });
            }

            return observable;
        }

        /// <summary>
        /// This is a faster version of ObservePropertyChanged. It returns only the <see cref="PropertyChangedEventArgs"/> from source and not the EventPattern
        /// </summary>
        /// <param name="source"> The source instance to track changes for. </param>
        /// <param name="name"> The name of the property to track. Note that nested properties are not allowed. </param>
        /// <param name="signalInitial"> If true OnNext is called immediately on subscribe </param>
        public static IObservable<PropertyChangedEventArgs> ObservePropertyChangedSlim(this INotifyPropertyChanged source, string name, bool signalInitial = true)
        {
            Ensure.NotNull(source, nameof(source));
            Ensure.NotNullOrEmpty(name, "name");
            if (source.GetType().GetProperty(name) == null)
            {
                throw new ArgumentException($"The type {source.GetType()} does not have a property named {name}", name);
            }

            var observable = source.ObservePropertyChangedSlim()
                                   .Where(e => IsPropertyName(e, name));
            if (signalInitial)
            {
                observable = observable.StartWith(new PropertyChangedEventArgs(name));
            }

            return observable;
        }

        /// <summary>
        /// Observe propertychanges with values.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="property">An expression specifying the property path.</param>
        /// <param name="signalInitial">If true OnNext is called immediately on subscribe.</param>
        /// <typeparam name="TNotifier">The type of <paramref name="source"/></typeparam>
        /// <typeparam name="TProperty">The type of the last property in the path.</typeparam>
        /// <returns>The <see cref="IObservable{T}"/> of type of type <see cref="EventPattern{TArgs}"/> of type <see cref="PropertyChangedAndValueEventArgs{TProperty}"/>.</returns>
        public static IObservable<EventPattern<PropertyChangedAndValueEventArgs<TProperty>>> ObservePropertyChangedWithValue<TNotifier, TProperty>(
            this TNotifier source,
            Expression<Func<TNotifier, TProperty>> property,
            bool signalInitial = true)
            where TNotifier : class, INotifyPropertyChanged
        {
            Ensure.NotNull(source, nameof(source));
            Ensure.NotNull(property, nameof(property));
            var propertyPath = PropertyPath.GetOrCreate(property);
            return source.ObservePropertyChangedWithValue(propertyPath, signalInitial);
        }

        /// <summary>
        /// Observe property changes for <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source.</param>
        public static IObservable<EventPattern<PropertyChangedEventArgs>> ObservePropertyChanged(this INotifyPropertyChanged source)
        {
            Ensure.NotNull(source, nameof(source));
            var wr = new WeakReference<INotifyPropertyChanged>(source);
            IObservable<EventPattern<PropertyChangedEventArgs>> observable =
                Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    x =>
                    {
                        INotifyPropertyChanged inpc;
                        if (wr.TryGetTarget(out inpc))
                        {
                            inpc.PropertyChanged += x;
                        }
                    },
                    x =>
                    {
                        INotifyPropertyChanged inpc;
                        if (wr.TryGetTarget(out inpc))
                        {
                            inpc.PropertyChanged -= x;
                        }
                    });
            return observable;
        }

        /// <summary>
        /// Observe property changes for <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source.</param>
        public static IObservable<PropertyChangedEventArgs> ObservePropertyChangedSlim(this INotifyPropertyChanged source)
        {
            Ensure.NotNull(source, nameof(source));

            var observable = Observable.Create<PropertyChangedEventArgs>(
                o =>
                    {
                        PropertyChangedEventHandler handler = (_, e) =>
                            {
                                o.OnNext(e);
                            };
                        source.PropertyChanged += handler;
                        return Disposable.Create(() => source.PropertyChanged -= handler);
                    });
            return observable;
        }

        /// <summary>
        /// Extension method for listening to property changes.
        /// Handles nested x => x.Level1.Level2.Level3
        /// Unsubscribes &amp; subscribes when each level changes.
        /// Handles nulls.
        /// </summary>
        /// <param name="source">The source instance to track changes for. </param>
        /// <param name="property">
        /// A cached property path. Creating the property path from Expression&lt;Func&lt;TNotifier, TProperty&gt;&gt; is a bit expensive so caching can make sense.
        ///  </param>
        /// <param name="signalInitial">
        /// If true OnNext is called immediately on subscribe
        /// </param>
        internal static IObservable<EventPattern<PropertyChangedEventArgs>> ObservePropertyChanged<TNotifier, TProperty>(
            this TNotifier source,
            PropertyPath<TNotifier, TProperty> property,
            bool signalInitial = true)
            where TNotifier : INotifyPropertyChanged
        {
            var observable = new PropertyPathObservable<TNotifier, TProperty>(source, property);
            if (signalInitial)
            {
                return Observable.Defer(
                    () =>
                    {
                        var current = new EventPattern<PropertyChangedEventArgs>(
                            observable.Sender,
                            observable.PropertyChangedEventArgs);
                        return Observable.Return(current).Concat(observable);
                    });
            }

            return observable;
        }

        internal static IObservable<EventPattern<PropertyChangedAndValueEventArgs<TProperty>>> ObservePropertyChangedWithValue<TNotifier, TProperty>(
            this TNotifier source,
            PropertyPath<TNotifier, TProperty> propertyPath,
            bool signalInitial = true)
            where TNotifier : INotifyPropertyChanged
        {
            var wr = new WeakReference(source);
            var observable = source.ObservePropertyChanged(propertyPath, false);
            return Observable.Defer(
                () =>
                    {
                        var withValues =
                            observable.Select(
                                x =>
                                    new EventPattern<PropertyChangedAndValueEventArgs<TProperty>>(
                                        x.Sender,
                                        new PropertyChangedAndValueEventArgs<TProperty>(
                                            x.EventArgs.PropertyName,
                                            propertyPath.GetValueFromRoot((TNotifier)wr.Target))));
                        if (signalInitial)
                        {
                            var valueAndSource = propertyPath.GetValueAndSender((TNotifier)wr.Target);
                            var current =
                                new EventPattern<PropertyChangedAndValueEventArgs<TProperty>>(
                                    valueAndSource.Source,
                                    new PropertyChangedAndValueEventArgs<TProperty>(
                                        propertyPath.Last.PropertyInfo.Name,
                                        valueAndSource.Value));
                            return Observable.Return(current).Concat(withValues);
                        }

                        return withValues;
                    });
        }

        private static bool IsPropertyName(this PropertyChangedEventArgs e, string propertyName)
        {
            return string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == propertyName;
        }
    }
}
