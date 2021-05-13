using BGLib.LowEnergy;
using BGLib.WPF.Views;
using Prism.Commands;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Windows;

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
            var discovery = Discoveries.FirstOrDefault(i => Equals(i.Address, e.Address));
            if (discovery == null)
            {
                discovery = new DiscoveryViewModel(e.Type, e.Address, e.Name, e.Advertisements, e.RSSI);
                Discoveries.Add(discovery);
            }
            else
            {
                discovery.Type = e.Type;
                discovery.Name = e.Name;
                discovery.Advertisements = e.Advertisements;
                discovery.RSSI = e.RSSI;
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
            try
            {
                await _central.StartDiscoveryAsync(Core.GAP.DiscoverMode.Observation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private DelegateCommand _stopDiscoveryCommand;
        public DelegateCommand StopDiscoveryCommand
            => _stopDiscoveryCommand ??= new DelegateCommand(ExecuteStopDiscoveryCommand);

        private async void ExecuteStopDiscoveryCommand()
        {
            try
            {
                await _central.StopDiscoveryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private DelegateCommand _clearDiscoveriesCommand;
        public DelegateCommand ClearDiscoveriesCommand
            => _clearDiscoveriesCommand ??= new DelegateCommand(ExecuteClearDiscoveriesCommand);

        private void ExecuteClearDiscoveriesCommand()
        {
            Discoveries.Clear();
        }

        private DelegateCommand<DiscoveryViewModel> _shwoPeripheralViewCommand;
        public DelegateCommand<DiscoveryViewModel> ShowPeripheralViewCommand
            => _shwoPeripheralViewCommand ??= new DelegateCommand<DiscoveryViewModel>(ExecuteShowPeripheralViewCommand);

        private void ExecuteShowPeripheralViewCommand(DiscoveryViewModel discovery)
        {
            var source = $"{nameof(PeripheralView)}";
            var parameters = new NavigationParameters();
            parameters.Add("Central", _central);
            parameters.Add("Address", discovery.Address);
            RegionManager.RequestNavigate(source, parameters);
        }

        private DelegateCommand _connectCommand;
        public DelegateCommand ConnectCommand
            => _connectCommand ??= new DelegateCommand(ExecuteConnectCommand);

        private async void ExecuteConnectCommand()
        {
            var rawValue = new byte[] { 0xF5, 0xB8, 0xC4, 0x57, 0x0B, 0x00 };
            var address = new Address(Core.GAP.AddressType.Public, rawValue);
            try
            {
                var peripheral = await _central.ConnectAsync(address);
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
