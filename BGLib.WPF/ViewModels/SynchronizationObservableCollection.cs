using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;

namespace BGLib.WPF.ViewModels
{
    internal class SynchronizationObservableCollection<T> : ObservableCollection<T>
    {
        private readonly SynchronizationContext _context;

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
                _context.Post(
                    state => base.OnCollectionChanged(state as NotifyCollectionChangedEventArgs),
                    e);
            }
        }
    }
}
