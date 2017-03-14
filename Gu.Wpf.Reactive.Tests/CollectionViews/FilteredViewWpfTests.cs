namespace Gu.Wpf.Reactive.Tests.CollectionViews
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Windows.Controls;
    using System.Windows.Data;

    using Gu.Reactive;

    using NUnit.Framework;

    [Apartment(ApartmentState.STA)]
    public class FilteredViewWpfTests
    {
        [Test]
        public void TwoViewsNotSame()
        {
            var ints = new ObservableCollection<int>(new[] { 1, 2, 3 });
            using (var view1 = ints.AsFilteredView(x => true))
            {
                using (var view2 = ints.AsFilteredView(x => true))
                {
                    Assert.AreNotSame(view1, view2);

                    var colView1 = CollectionViewSource.GetDefaultView(view1);
                    var colView2 = CollectionViewSource.GetDefaultView(view2);
                    Assert.AreNotSame(colView1, colView2);
                }
            }
        }

        [Test]
        public void BindItemsSource()
        {
            var listBox = new ListBox();
            var ints = new List<int> { 1, 2, 3 };
            using (var view = ints.AsFilteredView(x => x == 2, new Subject<object>()))
            {
                var binding = new Binding { Source = view, };
                BindingOperations.SetBinding(listBox, ItemsControl.ItemsSourceProperty, binding);
                view.Refresh();
                CollectionAssert.AreEqual(new[] { 2 }, listBox.Items); // Filtered
            }
        }
    }
}