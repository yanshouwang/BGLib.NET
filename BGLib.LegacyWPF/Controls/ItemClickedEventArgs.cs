using System.Windows;

namespace BGLib.LegacyWPF.Controls
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
