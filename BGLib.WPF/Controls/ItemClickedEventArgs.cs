using System.Windows;

namespace BGLib.WPF.Controls
{
    public class ItemClickedEventArgs : RoutedEventArgs
    {
        public ItemClickedEventArgs(RoutedEvent routedEvent, object item)
            : base(routedEvent)
        {
            Item = item;
        }

        public object Item { get; }
    }
}
