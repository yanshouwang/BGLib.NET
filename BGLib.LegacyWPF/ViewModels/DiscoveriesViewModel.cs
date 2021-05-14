using BGLib.LowEnergy;
using BGLib.LegacyWPF.Views;
using Prism.Commands;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Windows;

namespace BGLib.LegacyWPF.ViewModels
{
    class DiscoveriesViewModel : BaseViewModel
    {
        private Central _central;
        public Central Central
        {
            get => _central;
            set => SetProperty(ref _central, value);
        }

        public IList<DiscoveryViewModel> Discoveries { get; }
        public IList<string> PortNames { get; }

        public DiscoveriesViewModel(IRegionManager regionManager)
            : base(regionManager)
        {
            Discoveries = new SynchronizationObservableCollection<DiscoveryViewModel>();
            PortNames = SerialPort.GetPortNames();

            Test();
        }

        private async void Test()
        {
            var bgLib = new Bluegiga.BGLib();
            var bleModuleConnection = new BGLibExt.BleModuleConnection(bgLib);
            bleModuleConnection.Start("COM3");

            var bleDeviceManager = new BGLibExt.BleDeviceManager(bgLib, bleModuleConnection);
            var address = new byte[] { 0x0E, 0X64, 0XB4, 0X3A, 0X7D, 0X80 };
            var bleDevice = await bleDeviceManager.ConnectAsync(address, BGLibExt.BleAddressType.Public);
            var uuid = Guid.Parse("0000C305-0000-1000-8000-00805F9B34FB");
            bleDevice.CharacteristicsByUuid[uuid].ValueChanged += (sender, args) =>
            {
            };
            await bleDevice.CharacteristicsByUuid[uuid].WriteCccAsync(BGLibExt.BleCccValue.NotificationsEnabled);
            await System.Threading.Tasks.Task.Delay(10000);
            await bleDevice.DisconnectAsync();

            bleModuleConnection.Stop();
        }

        private DelegateCommand<string> _connectCommand;
        public DelegateCommand<string> ConnectCommand
            => _connectCommand ??= new DelegateCommand<string>(ExecuteConnectCommand);

        private void ExecuteConnectCommand(string portName)
        {
            try
            {
                Central = new Central(portName, 256000, Parity.None, 8, StopBits.One);
                Central.Discovered += OnDiscovered;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
            => _startDiscoveryCommand ??= new DelegateCommand(ExecuteStartDiscoveryCommand, CanExecuteStartDiscoveryCommand)
            .ObservesProperty(() => Central);

        private bool CanExecuteStartDiscoveryCommand()
        {
            return Central != null;
        }

        private async void ExecuteStartDiscoveryCommand()
        {
            try
            {
                await Central.StartDiscoveryAsync(Core.GAP.DiscoverMode.Observation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private DelegateCommand _stopDiscoveryCommand;
        public DelegateCommand StopDiscoveryCommand
            => _stopDiscoveryCommand ??= new DelegateCommand(ExecuteStopDiscoveryCommand, CanExecuteStopDiscoveryCommand)
            .ObservesProperty(() => Central);

        private bool CanExecuteStopDiscoveryCommand()
        {
            return Central != null;
        }

        private async void ExecuteStopDiscoveryCommand()
        {
            try
            {
                await Central.StopDiscoveryAsync();
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
            parameters.Add("Central", Central);
            parameters.Add("Address", discovery.Address);
            RegionManager.RequestNavigate(source, parameters);
        }
    }
}
