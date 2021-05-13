using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BGLib.WPF.Controls
{
    class ListView2 : ListView
    {
        public event EventHandler<SelectionEventArgs> ItemSelected;

        private ListViewItem _listViewItem;

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            _listViewItem = ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
            _listViewItem.MouseEnter += OnListViewItemMouseEnter;
            _listViewItem.MouseLeave += OnListViewItemMouseLeave;
            VisualStateManager.GoToState(_listViewItem, "Pressed", true);
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            var olds = e.OldItems ?? new List<object>();
            var news = e.NewItems ?? new List<object>();
        }

        private void OnListViewItemMouseEnter(object sender, MouseEventArgs e)
        {
            VisualStateManager.GoToState(_listViewItem, "Pressed", true);
        }

        private void OnListViewItemMouseLeave(object sender, MouseEventArgs e)
        {
            VisualStateManager.GoToState(_listViewItem, "Normal", true);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            var listViewItem = ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
            if (listViewItem == _listViewItem)
            {
                var obj = listViewItem.DataContext ?? listViewItem;
                var eventArgs = new SelectionEventArgs(obj);
                ItemSelected?.Invoke(this, eventArgs);
            }
        }
    }
}
