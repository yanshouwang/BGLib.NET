using BGLib.API;
using Prism.Commands;
using Prism.Regions;
using System.Collections.Generic;
using System.Linq;

namespace BGLib.WPF.ViewModels
{
    class DiscoveriesViewModel : BaseViewModel
    {
        private readonly BGSerialPort _serial;

        public IList<DiscoveryViewModel> Discoveries { get; }

        public DiscoveriesViewModel(IRegionManager regionManager)
            : base(regionManager)
        {
            //string portName,
            //int baudRate = 256000,
            //BGParity parity = BGParity.None,
            //int dataBits = 8,
            //BGStopBits stopBits = BGStopBits.One
            _serial = new BGSerialPort("COM6");
            _serial.Discovered += OnDiscovered;

            Discoveries = new SynchronizationObservableCollection<DiscoveryViewModel>();

            _serial.Open();
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
            _serial.Close();
            _serial.Dispose();

            base.Destroy();
        }

        private DelegateCommand _startDiscoveryCommand;
        public DelegateCommand StartDiscoveryCommand
            => _startDiscoveryCommand ??= new DelegateCommand(ExecuteStartDiscoveryCommand);

        private async void ExecuteStartDiscoveryCommand()
        {
            await _serial.StartDiscoveryAsync();
        }

        private DelegateCommand _stopDiscoveryCommand;
        public DelegateCommand StopDiscoveryCommand
            => _stopDiscoveryCommand ??= new DelegateCommand(ExecuteStopDiscoveryCommand);

        private async void ExecuteStopDiscoveryCommand()
        {
            await _serial.StopDiscoveryAsync();
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
