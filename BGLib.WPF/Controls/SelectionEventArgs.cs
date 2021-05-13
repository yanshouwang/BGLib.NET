using System;

namespace BGLib.WPF.Controls
{
    class SelectionEventArgs : EventArgs
    {
        public SelectionEventArgs(object item)
        {
            Item = item;
        }

        public object Item { get; }
    }
}