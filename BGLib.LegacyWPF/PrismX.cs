using Prism.Regions;
using System;

namespace BGLib.LegacyWPF
{
    internal static class PrismX
    {
        public static void RequestNavigate(this IRegionManager regionManager, Uri source, Action<NavigationResult> navigationCallback)
        {
            var regionName = nameof(Shell);
            regionManager.RequestNavigate(regionName, source, navigationCallback);
        }

        public static void RequestNavigate(this IRegionManager regionManager, Uri source)
        {
            var regionName = nameof(Shell);
            regionManager.RequestNavigate(regionName, source);
        }

        public static void RequestNavigate(this IRegionManager regionManager, string source, Action<NavigationResult> navigationCallback)
        {
            var regionName = nameof(Shell);
            regionManager.RequestNavigate(regionName, source, navigationCallback);
        }

        public static void RequestNavigate(this IRegionManager regionManager, string source)
        {
            var regionName = nameof(Shell);
            regionManager.RequestNavigate(regionName, source);
        }

        public static void RequestNavigate(this IRegionManager regionManager, Uri source, Action<NavigationResult> navigationCallback, NavigationParameters navigationParameters)
        {
            var regionName = nameof(Shell);
            regionManager.RequestNavigate(regionName, source, navigationCallback, navigationParameters);
        }

        public static void RequestNavigate(this IRegionManager regionManager, string source, Action<NavigationResult> navigationCallback, NavigationParameters navigationParameters)
        {
            var regionName = nameof(Shell);
            regionManager.RequestNavigate(regionName, source, navigationCallback, navigationParameters);
        }

        public static void RequestNavigate(this IRegionManager regionManager, Uri source, NavigationParameters navigationParameters)
        {
            var regionName = nameof(Shell);
            regionManager.RequestNavigate(regionName, source, navigationParameters);
        }

        public static void RequestNavigate(this IRegionManager regionManager, string source, NavigationParameters navigationParameters)
        {
            var regionName = nameof(Shell);
            regionManager.RequestNavigate(regionName, source, navigationParameters);
        }
    }
}
