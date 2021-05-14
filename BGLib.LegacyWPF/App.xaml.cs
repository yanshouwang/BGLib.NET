using BGLib.LegacyWPF.Views;
using Prism.Ioc;
using Prism.Regions;
using Prism.Unity;
using System.Windows;

namespace BGLib.LegacyWPF
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<Shell>();
        }

        protected override void RegisterTypes(IContainerRegistry registry)
        {
            registry.RegisterForNavigation<DiscoveriesView>();
            registry.RegisterForNavigation<PeripheralView>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var regionManager = Container.Resolve<IRegionManager>();
            var source = nameof(DiscoveriesView);
            regionManager.RequestNavigate(source);
        }
    }
}
