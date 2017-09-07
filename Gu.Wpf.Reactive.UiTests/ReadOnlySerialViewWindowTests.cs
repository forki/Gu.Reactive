namespace Gu.Wpf.Reactive.UiTests
{
    using System.Collections.Generic;
    using System.Linq;
    using Gu.Wpf.UiAutomation;
    using NUnit.Framework;

    public class ReadOnlySerialViewWindowTests : WindowTests
    {
        protected override string WindowName { get; } = "ReadOnlySerialViewWindow";

        private ListBox ListBox => this.Window
                                    .FindFirstDescendant(x => x.ByText("ListBox"))
                                    .FindListBox();

        private DataGrid DataGrid => this.Window
                                     .FindFirstDescendant(x => x.ByText("DataGrid"))
                                     .FindDataGrid();

        private Button ClearButton => this.Window.FindButton("Clear");

        private TextBox ItemsTextBox => this.Window.FindTextBox("Items");

        private Button UpdateButton => this.Window.FindButton("Update");

        private Button ClearSourceButton => this.Window.FindButton("ClearSource");

        private IEnumerable<Label> ViewChanges => this.Window
                                                      .FindFirstDescendant(x => x.ByText("ViewChanges"))
                                                      .FindAllChildren()
                                                      .Skip(1)
                                                      .Select(x => x.AsLabel());

        [SetUp]
        public void SetUp()
        {
            this.ClearButton.Click();
            this.ClearButton.Click();
        }

        [Test]
        public void Initializes()
        {
            this.Restart();
            CollectionAssert.IsEmpty(this.ListBox.Items.Select(x => x.FindTextBlock().Text));
            CollectionAssert.IsEmpty(this.DataGrid.ColumnValues(0));
            CollectionAssert.IsEmpty(this.ViewChanges.Select(x => x.Text));
        }

        [Test]
        public void UpdateWithOne()
        {
            this.ItemsTextBox.Enter("1");
            this.UpdateButton.Click();
            CollectionAssert.AreEqual(new[] { "1" }, this.ListBox.Items.Select(x => x.FindTextBlock().Text));
            CollectionAssert.AreEqual(new[] { "1" }, this.DataGrid.ColumnValues(0));
            CollectionAssert.AreEqual(new[] { "Add" }, this.ViewChanges.Select(x => x.Text).ToArray());
        }

        [Test]
        public void UpdateWithTwo()
        {
            this.ItemsTextBox.Enter("1, 2");
            this.UpdateButton.Click();
            CollectionAssert.AreEqual(new[] { "1", "2" }, this.ListBox.Items.Select(x => x.FindTextBlock().Text));
            CollectionAssert.AreEqual(new[] { "1", "2" }, this.DataGrid.ColumnValues(0));
            CollectionAssert.AreEqual(new[] { "Reset" }, this.ViewChanges.Select(x => x.Text));
        }

        [Test]
        public void UpdateWithOneThenClearSourceThenUpdateOne()
        {
            this.ItemsTextBox.Enter("1");
            this.UpdateButton.Click();
            CollectionAssert.AreEqual(new[] { "1" }, this.ListBox.Items.Select(x => x.FindTextBlock().Text));
            CollectionAssert.AreEqual(new[] { "1" }, this.DataGrid.ColumnValues(0));
            CollectionAssert.AreEqual(new[] { "Add" }, this.ViewChanges.Select(x => x.Text));

            this.ClearSourceButton.Click();
            CollectionAssert.IsEmpty(this.ListBox.Items.Select(x => x.FindTextBlock().Text));
            CollectionAssert.IsEmpty(this.DataGrid.ColumnValues(0));
            CollectionAssert.AreEqual(new[] { "Add", "Remove" }, this.ViewChanges.Select(x => x.Text));

            this.ItemsTextBox.Enter("2");
            this.UpdateButton.Click();
            CollectionAssert.AreEqual(new[] { "2" }, this.ListBox.Items.Select(x => x.FindTextBlock().Text));
            CollectionAssert.AreEqual(new[] { "2" }, this.DataGrid.ColumnValues(0));
            CollectionAssert.AreEqual(new[] { "Add", "Remove", "Add" }, this.ViewChanges.Select(x => x.Text));
        }
    }
}