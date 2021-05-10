using BGLib.LowEnergy;
using Prism.Commands;
using Prism.Regions;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace BGLib.WPF.ViewModels
{
    class DiscoveriesViewModel : BaseViewModel
    {
        private readonly Central _central;

        public IList<DiscoveryViewModel> Discoveries { get; }

        public DiscoveriesViewModel(IRegionManager regionManager)
            : base(regionManager)
        {
            Discoveries = new SynchronizationObservableCollection<DiscoveryViewModel>();
            _central = new Central("COM3", 256000, Parity.None, 8, StopBits.One);
            _central.Discovered += OnDiscovered;
        }

        private void OnDiscovered(object sender, DiscoveryEventArgs e)
        {
            var discovery = Discoveries.FirstOrDefault(i => i.Address.Value == e.Device.Address.Value);
            if (discovery == null)
            {
                discovery = new DiscoveryViewModel(e.Type, e.Device, e.Advertisements, e.RSSI);
                Discoveries.Add(discovery);
            }
            else
            {
                discovery.Type = e.Type;
                discovery.Name = e.Device.Name;
                discovery.Advertisements = e.Advertisements;
                discovery.RSSI = e.RSSI;
            }
            //_bgAPI.ConnectionStateChanged += OnConnectionStateChanged;
            //await _bgAPI.ConnectAsync(discovery.Address);
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

        private DelegateCommand _startDiscoveryCommand;
        public DelegateCommand StartDiscoveryCommand
            => _startDiscoveryCommand ??= new DelegateCommand(ExecuteStartDiscoveryCommand);

        private async void ExecuteStartDiscoveryCommand()
        {
            await _central.StartDiscoveryAsync();
        }

        private DelegateCommand _stopDiscoveryCommand;
        public DelegateCommand StopDiscoveryCommand
            => _stopDiscoveryCommand ??= new DelegateCommand(ExecuteStopDiscoveryCommand);

        private async void ExecuteStopDiscoveryCommand()
        {
            await _central.StopDiscoveryAsync();
        }

        private DelegateCommand _clearDiscoveriesCommand;
        public DelegateCommand ClearDiscoveriesCommand
            => _clearDiscoveriesCommand ??= new DelegateCommand(ExecuteClearDiscoveriesCommand);

        private void ExecuteClearDiscoveriesCommand()
        {
            Discoveries.Clear();
        }
    }
}
