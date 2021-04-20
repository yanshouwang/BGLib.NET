using Prism.Regions;

namespace BGLib.WPF.ViewModels
{
    class DiscoveryViewModel : BaseViewModel
    {
        public DiscoveryViewModel(IRegionManager regionManager)
            : base(regionManager)
        {

        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
        }

        public override void Destroy()
        {
            base.Destroy();
        }
    }
}
