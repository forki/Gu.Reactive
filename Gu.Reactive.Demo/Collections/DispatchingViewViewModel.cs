﻿#pragma warning disable 618
namespace Gu.Reactive.Demo
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using System.Windows.Threading;
    using Gu.Wpf.Reactive;

    public sealed class DispatchingViewViewModel : IDisposable
    {
        private readonly System.Reactive.Disposables.CompositeDisposable disposable;
        private bool disposed;

        public DispatchingViewViewModel()
        {
            this.Add(3);
            this.View = this.Source.AsDispatchingView();
            this.AddOneCommand = new RelayCommand(this.AddOne, () => true);
            this.AddOneToViewCommand = new RelayCommand(this.AddOneToView, () => true);
            this.AddFourCommand = new RelayCommand(() => this.Add(4), () => true);
            this.AddOneOnOtherThreadCommand = new RelayCommand(() => Task.Run(() => this.AddOne()), () => true);
            this.ClearCommand = new RelayCommand(this.Source.Clear);
            this.ResetCommand = new RelayCommand(this.Reset);
            this.disposable = new System.Reactive.Disposables.CompositeDisposable
                              {
                                  this.Source
                                      .ObserveCollectionChanged(signalInitial: false)
                                      .ObserveOnDispatcher()
                                      .Subscribe(x => this.SourceChanges.Add(x.EventArgs)),
                                  this.View
                                      .ObserveCollectionChanged(signalInitial: false)
                                      .ObserveOnDispatcher()
                                      .Subscribe(x => this.ViewChanges.Add(x.EventArgs)),
                              };
        }

        public IObservableCollection<DummyItem> View { get; }

        public ICommand AddOneCommand { get; }

        public ICommand AddOneToViewCommand { get; }

        public ICommand AddFourCommand { get; }

        public ICommand AddOneOnOtherThreadCommand { get; }

        public RelayCommand ClearCommand { get; }

        public RelayCommand ResetCommand { get; }

        public ObservableCollection<NotifyCollectionChangedEventArgs> SourceChanges { get; } = new ObservableCollection<NotifyCollectionChangedEventArgs>();

        public ObservableCollection<NotifyCollectionChangedEventArgs> ViewChanges { get; } = new ObservableCollection<NotifyCollectionChangedEventArgs>();

        public ObservableCollection<DummyItem> Source { get; } = new ObservableCollection<DummyItem>();

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            (this.View as IDisposable)?.Dispose();
            this.disposable.Dispose();
        }

        private void AddOne()
        {
            this.Source.Add(new DummyItem(this.Source.Count + 1));
        }

        private void AddOneToView()
        {
            this.View.Add(new DummyItem(this.Source.Count + 1));
        }

        private void Add(int n)
        {
            for (var i = 0; i < n; i++)
            {
                this.AddOne();
            }
        }

        private async void Reset()
        {
            this.Source.Clear();
            await Dispatcher.Yield(DispatcherPriority.Background);
            this.SourceChanges.Clear();
            this.ViewChanges.Clear();
        }
    }
}
