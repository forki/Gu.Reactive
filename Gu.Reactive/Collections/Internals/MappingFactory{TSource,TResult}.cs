﻿#pragma warning disable SA1600 // Elements must be documented, internal
namespace Gu.Reactive
{
    using System;

    internal class MappingFactory<TSource, TResult> : IMappingFactory<TSource, TResult>
    {
        private readonly Func<TSource, int, TResult> indexSelector;
        private readonly Func<TSource, TResult> selector;
        private readonly WeakCompositeDisposable itemDisposables = new WeakCompositeDisposable();
        private bool disposed;

        internal MappingFactory(Func<TSource, TResult> selector)
        {
            this.selector = selector;
        }

        internal MappingFactory(Func<TSource, int, TResult> indexSelector)
        {
            this.indexSelector = indexSelector;
        }

        public bool CanUpdateIndex => this.indexSelector != null;

        public TResult GetOrCreateValue(TSource key, int index)
        {
            this.ThrowIfDisposed();
            TResult mapped;
            if (this.indexSelector != null)
            {
                mapped = this.indexSelector(key, index);
            }
            else
            {
                mapped = this.selector(key);
            }

            var disposable = mapped as IDisposable;
            if (disposable != null)
            {
                this.itemDisposables.Add(disposable);
            }

            return mapped;
        }

        public TResult UpdateIndex(TSource key, int index)
        {
            return this.GetOrCreateValue(key, index);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.itemDisposables.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
