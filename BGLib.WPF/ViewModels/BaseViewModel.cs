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

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {

        }

        public virtual void Destroy()
        {

        }
    }
}
