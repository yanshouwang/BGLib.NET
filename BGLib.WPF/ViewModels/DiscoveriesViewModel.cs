using BGLib.API;
using Prism.Commands;
using Prism.Regions;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace BGLib.WPF.ViewModels
{
    class DiscoveriesViewModel : BaseViewModel
    {
        private readonly BGAPI _bgAPI;

        public IList<DiscoveryViewModel> Discoveries { get; }

        public DiscoveriesViewModel(IRegionManager regionManager)
            : base(regionManager)
        {
            Discoveries = new SynchronizationObservableCollection<DiscoveryViewModel>();
            var serial = new SerialPort("COM3", 256000, Parity.None, 8, StopBits.One);
            var communicator = new SerialCommunicator(serial);
            _bgAPI = new BGAPI(communicator);
            _bgAPI.Discovered += OnDiscovered;
        }

        private void OnDiscovered(object sender, DiscoveryEventArgs e)
        {
            var discovery = Discoveries.FirstOrDefault(i => i.Address.Value == e.Discovery.Address.Value);
            if (discovery == null)
            {
                discovery = new DiscoveryViewModel(e.Discovery);
                Discoveries.Add(discovery);
            }
            else
            {
                discovery.Name = e.Discovery.Name;
                discovery.Type = e.Discovery.Type;
                discovery.Advertisements = e.Discovery.Advertisements;
                discovery.RSSI = e.Discovery.RSSI;
            }
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
            await _bgAPI.DiscoverAsync();
        }

        private DelegateCommand _stopDiscoveryCommand;
        public DelegateCommand StopDiscoveryCommand
            => _stopDiscoveryCommand ??= new DelegateCommand(ExecuteStopDiscoveryCommand);

        private async void ExecuteStopDiscoveryCommand()
        {
            await _bgAPI.EndAsync();
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
