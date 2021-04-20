﻿using BGLib.WPF.Views;
using Prism.Ioc;
using Prism.Regions;
using Prism.Unity;
using System.Windows;

namespace BGLib.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<Shell>();
        }

        protected override void RegisterTypes(IContainerRegistry registry)
        {
            registry.RegisterForNavigation<DiscoveryView>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var regionManager = Container.Resolve<IRegionManager>();
            var source = nameof(DiscoveryView);
            regionManager.RequestNavigate(source);
        }
    }
}
