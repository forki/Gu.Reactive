﻿// ReSharper disable VirtualMemberNeverOverridden.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Gu.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Runtime.CompilerServices;
    using Gu.Reactive.Internals;

    /// <summary>
    /// To be used standalone or derived from. Conditions really starts to sing when you subclass them and use an IoC container to build trees.
    /// </summary>
    public class Condition : ICondition
    {
        private static readonly IReadOnlyList<ICondition> EmptyPrerequisites = new ICondition[0];
        private readonly Func<bool?> criteria;
        private readonly IDisposable subscription;
        private readonly FixedSizedQueue<ConditionHistoryPoint> history = new FixedSizedQueue<ConditionHistoryPoint>(100);
        private bool? isSatisfied;
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Condition"/> class.
        /// </summary>
        /// <param name="observable">
        /// The observable that triggers updates of <see cref="IsSatisfied"/>
        /// </param>
        /// <param name="criteria">
        /// The criteria that is evaluated to give IsSatisfied.
        /// </param>
        /// <param name="observables">
        /// More observables that triggers updates of <see cref="IsSatisfied"/>
        /// </param>
        public Condition(Func<bool?> criteria, IObservable<object> observable, params IObservable<object>[] observables)
            : this(Observable.Merge(observables.Concat(new[] { observable })), criteria)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Condition"/> class.
        /// </summary>
        /// <param name="observable">
        /// The observable that triggers updates of <see cref="IsSatisfied"/>
        /// </param>
        /// <param name="criteria">
        /// The criteria that is evaluated to give IsSatisfied.
        /// </param>
        public Condition(IObservable<object> observable, Func<bool?> criteria)
        {
            Ensure.NotNull(observable, nameof(observable));
            Ensure.NotNull(criteria, nameof(criteria));

            this.criteria = criteria;
            this.name = this.GetType().PrettyName();
            this.subscription = observable.StartWith((object)null)
                                          .Subscribe(x => this.UpdateIsSatisfied());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Condition"/> class.
        /// </summary>
        protected Condition(ObservableAndCriteria observableAndCriteria)
            : this(observableAndCriteria.Observable, observableAndCriteria.Criteria)
        {
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// A log of the last 100 times the condition has signaled. Use for debugging.
        /// </summary>
        public IEnumerable<ConditionHistoryPoint> History => this.history;

        /// <summary>
        /// The sub conditions for this condition
        /// </summary>
        public virtual IReadOnlyList<ICondition> Prerequisites
        {
            get
            {
                this.ThrowIfDisposed();
                return EmptyPrerequisites;
            }
        }

        /// <summary>
        /// Evaluates the criteria and returns if it is satisfied.
        /// Notifies via PropertyChanged when it changes.
        /// </summary>
        public bool? IsSatisfied
        {
            get
            {
                this.ThrowIfDisposed();
                return this.criteria(); // No caching
            }

            private set
            {
                // The cached value is only for not raising PropertyChanged if the value has not changed.
                // The getter calculates the value.
                if (this.isSatisfied == value)
                {
                    if (this.history.Count == 0)
                    {
                        this.history.Enqueue(new ConditionHistoryPoint(DateTime.UtcNow, this.isSatisfied));
                    }

                    return;
                }

                this.isSatisfied = value;
                this.history.Enqueue(new ConditionHistoryPoint(DateTime.UtcNow, this.isSatisfied));
                this.OnPropertyChanged(CachedEventArgs.IsSatisfiedChanged);
            }
        }

        /// <summary>
        /// Gets or sets the name. The default name is .GetType().PrettyName()
        /// </summary>
        public string Name
        {
            get
            {
                this.ThrowIfDisposed();
                return this.name;
            }

            set
            {
                this.ThrowIfDisposed();
                if (value == this.name)
                {
                    return;
                }

                this.name = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// True if this instance is disposed.
        /// </summary>
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// Negates the condition. Calling Negate does not mutate the condition it is called on.
        /// Calling Negate on a negated condition returns the original condition.
        /// </summary>
        /// <returns>
        /// A new condition.
        /// </returns>
        public virtual ICondition Negate()
        {
            this.ThrowIfDisposed();
            return new NegatedCondition(this);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public override string ToString() => $"Name: {(string.IsNullOrEmpty(this.Name) ? this.GetType().PrettyName() : this.Name)}, IsSatisfied: {this.IsSatisfied?.ToString() ?? "null"}";

        /// <summary>
        /// Create an <see cref="ObservableAndCriteria"/> to be passed in as constructor argument.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="path">The property path to listen to changes for on source.</param>
        /// <param name="value">The value when satisfied.</param>
        protected static ObservableAndCriteria For<TSource, TValue>(
            TSource source,
            Expression<Func<TSource, TValue>> path,
            TValue value)
            where TSource : class, INotifyPropertyChanged
        {
            Ensure.NotNull(source, nameof(source));
            Ensure.NotNull(path, nameof(path));
            return For(source, path, value, (x, y) => Maybe.Equals(x, y, EqualityComparer<TValue>.Default.Equals));
        }

        /// <summary>
        /// Create an <see cref="ObservableAndCriteria"/> to be passed in as constructor argument.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="path">The property path to listen to changes for on source.</param>
        /// <param name="value">The value when satisfied.</param>
        /// <param name="comparer">How to compare actual value and <paramref name="value"/>.</param>
        protected static ObservableAndCriteria For<TSource, TValue>(
            TSource source,
            Expression<Func<TSource, TValue>> path,
            TValue value,
            EqualityComparer<TValue> comparer)
            where TSource : class, INotifyPropertyChanged
        {
            Ensure.NotNull(source, nameof(source));
            Ensure.NotNull(path, nameof(path));
            return For(source, path, value, (x, y) => Maybe.Equals(x, y, comparer.Equals));
        }

        /// <summary>
        /// Create an <see cref="ObservableAndCriteria"/> to be passed in as constructor argument.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="path">The property path to listen to changes for on source.</param>
        /// <param name="value">The value when satisfied.</param>
        /// <param name="equals">How to compare actual value and <paramref name="value"/>.</param>
        protected static ObservableAndCriteria For<TSource, TValue>(
            TSource source,
            Expression<Func<TSource, TValue>> path,
            TValue value,
            Func<TValue, TValue, bool> @equals)
            where TSource : class, INotifyPropertyChanged
        {
            Ensure.NotNull(source, nameof(source));
            Ensure.NotNull(path, nameof(path));
            return For(source, path, value, (x, y) => Maybe.Equals(x, y, @equals));
        }

        /// <summary>
        /// Create an <see cref="ObservableAndCriteria"/> to be passed in as constructor argument.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="path">The property path to listen to changes for on source.</param>
        /// <param name="value">The value when satisfied.</param>
        /// <param name="compare">How to compare actual value and <paramref name="value"/>.</param>
        protected static ObservableAndCriteria For<TSource, TValue>(
            TSource source,
            Expression<Func<TSource, TValue>> path,
            TValue value,
            Func<Maybe<TValue>, TValue, bool?> compare)
            where TSource : class, INotifyPropertyChanged
        {
            Ensure.NotNull(source, nameof(source));
            Ensure.NotNull(path, nameof(path));
            var notifyingPath = NotifyingPath.GetOrCreate(path);
            return new ObservableAndCriteria(
                Observable.Create<object>(o =>
                {
                    var tracker = notifyingPath.CreateTracker(source);
                    void Handler(IPropertyTracker _, object __, PropertyChangedEventArgs args, SourceAndValue<INotifyPropertyChanged, TValue> ___) => o.OnNext(args);
                    tracker.TrackedPropertyChanged += Handler;
                    return Disposable.Create(() =>
                    {
                        tracker.TrackedPropertyChanged -= Handler;
                        tracker.Dispose();
                    });
                }),
                () => compare(notifyingPath.SourceAndValue(source).Value, value));
        }

        /// <summary>
        /// Disposes of a <see cref="Condition"/>.
        /// </summary>
        /// <remarks>
        /// Called from Dispose() with disposing=true.
        /// Guidelines:
        /// 1. We may be called more than once: do nothing after the first call.
        /// 2. Avoid throwing exceptions if disposing is false, i.e. if we're being finalized.
        /// </remarks>
        /// <param name="disposing">True if called from Dispose(), false if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsDisposed = true;
            if (disposing)
            {
                this.subscription.Dispose();
            }
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the instance is disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        /// <summary>
        /// Update <see cref="IsSatisfied"/> with <see cref="criteria"/>
        /// </summary>
        protected void UpdateIsSatisfied()
        {
            this.IsSatisfied = this.criteria();
        }

        /// <summary>
        /// Raise PropertyChanged event to any listeners.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raise PropertyChanged event to any listeners.
        /// Properties/methods modifying this <see cref="Condition"/> will raise
        /// a property changed event through this virtual method.
        /// </summary>
        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            this.PropertyChanged?.Invoke(this, e);
        }
    }
}
