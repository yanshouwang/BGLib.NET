using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;

namespace BGLib.WPF.ViewModels
{
    class SynchronizationObservableCollection<T> : ObservableCollection<T>
    {
        readonly SynchronizationContext _context;

        public SynchronizationObservableCollection()
        {
            _context = SynchronizationContext.Current;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_context == SynchronizationContext.Current)
            {
                base.OnCollectionChanged(e);
            }
            else
            {
                _context.Post(i =>
                {
                    var eventArgs = (NotifyCollectionChangedEventArgs)i;
                    base.OnCollectionChanged(eventArgs);
                }, e);
            }
        }
    }
}
