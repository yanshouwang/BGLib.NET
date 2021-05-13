using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Windows.Data;

namespace BGLib.WPF.ViewModels
{
    internal class SynchronizationObservableCollection<T> : ObservableCollection<T>
    {
        //private readonly SynchronizationContext _context;
        private static object _lockObject = new object();

        public SynchronizationObservableCollection()
        {
            //_context = SynchronizationContext.Current;
            BindingOperations.EnableCollectionSynchronization(this, _lockObject);
        }

        //protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        //{
        //    if (_context == SynchronizationContext.Current)
        //    {
        //        base.OnCollectionChanged(e);
        //    }
        //    else
        //    {
        //        _context.Send(
        //            state => base.OnCollectionChanged(state as NotifyCollectionChangedEventArgs),
        //            e);
        //    }
        //}
    }
}
