﻿namespace Gu.Reactive
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    /// <summary>
    /// Extension methods for <see cref="ICondition"/>
    /// </summary>
    public static class ConditionExt
    {
        /// <summary>
        /// Get an observable that notifies when ICondition.IsSatisfied changes.
        /// </summary>
        /// <param name="condition">The condition to track.</param>
        /// <returns>An observable that returns <paramref name="condition"/> every time ICondition.IsSatisfied changes.</returns>
        public static IObservable<T> ObserveIsSatisfiedChanged<T>(this T condition)
            where T : class, ISatisfied
        {
            return Observable.Create<T>(
                o =>
                {
                    PropertyChangedEventHandler handler = (_, e) =>
                    {
                        if (e.PropertyName == nameof(condition.IsSatisfied))
                        {
                            o.OnNext(condition);
                        }
                    };
                    condition.PropertyChanged += handler;
                    return Disposable.Create(() => condition.PropertyChanged -= handler);
                });
        }

        /// <summary>
        /// Returns true if history matches current state.
        /// </summary>
        public static bool IsInSync(this ICondition condition)
        {
            return condition.IsSatisfied == condition.History
                                                     .Last()
                                                     .State;
        }
    }
}
