using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Regions;

namespace BGLib.WPF.ViewModels
{
    class BaseViewModel : BindableBase, INavigationAware, IDestructible
    {
        protected IRegionManager RegionManager { get; }

        public BaseViewModel(IRegionManager regionManager)
        {
            RegionManager = regionManager;
        }

        public virtual bool IsNavigationTarget(NavigationContext context)
        {
            return true;
        }

        public virtual void OnNavigatedFrom(NavigationContext context)
        {

        }

        public virtual void OnNavigatedTo(NavigationContext context)
        {

        }

        public virtual void Destroy()
        {

        }

        private DelegateCommand _goBackCommand;
        public DelegateCommand GoBackCommand
            => _goBackCommand ??= new DelegateCommand(ExecuteGoBackCommand);

        private void ExecuteGoBackCommand()
        {
            var journal = RegionManager.Regions["Shell"].NavigationService.Journal;
            if (!journal.CanGoBack)
                return;
            journal.GoBack();
        }

        private DelegateCommand _goForwardCommand;
        public DelegateCommand GoForwardCommand
            => _goForwardCommand ??= new DelegateCommand(ExecuteGoForwardCommand);

        private void ExecuteGoForwardCommand()
        {
            var journal = RegionManager.Regions["Shell"].NavigationService.Journal;
            if (!journal.CanGoForward)
                return;
            journal.GoForward();
        }
    }
}
