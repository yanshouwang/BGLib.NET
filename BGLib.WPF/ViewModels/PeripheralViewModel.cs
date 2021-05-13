using BGLib.LowEnergy;
using Prism.Commands;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BGLib.WPF.ViewModels
{
    class PeripheralViewModel : BaseViewModel
    {
        private Central _central;
        private Peripheral _peripheral;

        private Address _address;
        public Address Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public IList<byte[]> Values { get; }

        public IList<TreeNode> ServiceNodes { get; }

        private GattCharacteristic _characteristic;
        public GattCharacteristic Characteristic
        {
            get => _characteristic;
            set => SetProperty(ref _characteristic, value);
        }

        public PeripheralViewModel(IRegionManager regionManager)
            : base(regionManager)
        {
            ServiceNodes = new ObservableCollection<TreeNode>();
            Values = new SynchronizationObservableCollection<byte[]>();
        }

        public override bool IsNavigationTarget(NavigationContext context)
        {
            context.Parameters.TryGetValue<Address>("Address", out var address);
            return Equals(address, Address);
        }

        public override void OnNavigatedTo(NavigationContext context)
        {
            base.OnNavigatedTo(context);

            context.Parameters.TryGetValue("Central", out _central);
            context.Parameters.TryGetValue("Address", out _address);

            _central.ConnectionLost += OnConnectioinLost;
            _central.CharacteristicValueChanged += OnCharacteristicValueChanged;
        }

        private void OnConnectioinLost(object sender, PeripheralEventArgs e)
        {

        }

        private void OnCharacteristicValueChanged(object sender, GattCharacteristicValueEventArgs e)
        {
            if (e.Characteristic != Characteristic)
                return;
            Values.Add(e.Value);
        }

        public override void OnNavigatedFrom(NavigationContext context)
        {
            base.OnNavigatedFrom(context);

            _central.ConnectionLost -= OnConnectioinLost;
            _central.CharacteristicValueChanged -= OnCharacteristicValueChanged;
        }

        private DelegateCommand _connectCommand;
        public DelegateCommand ConnectCommand
            => _connectCommand ??= new DelegateCommand(ExecuteConnectCommand);

        private async void ExecuteConnectCommand()
        {
            try
            {
                var peripheral = await _central.ConnectAsync(_address);
                _peripheral = peripheral;
                var services = await _central.GetServicesAsync(peripheral);
                foreach (var service in services)
                {
                    var characteristics = await _central.GetCharacteristicsAsync(service);
                    var characteristicNodes = new List<TreeNode>();
                    foreach (var characteristic in characteristics)
                    {
                        var characteristicNode = new TreeNode(characteristic, null);
                        characteristicNodes.Add(characteristicNode);
                    }
                    var serviceNode = new TreeNode(service, characteristicNodes);
                    ServiceNodes.Add(serviceNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private DelegateCommand _disconnectCommand;
        public DelegateCommand DisconnectCommand
            => _disconnectCommand ??= new DelegateCommand(ExecuteDisconnectCommand);

        private async void ExecuteDisconnectCommand()
        {
            try
            {
                await _central.DisconnectAsync(_peripheral);
                ServiceNodes.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private DelegateCommand _notifyCommand;
        public DelegateCommand NotifyCommand
            => _notifyCommand ??= new DelegateCommand(ExecuteNotifyCommand);

        private async void ExecuteNotifyCommand()
        {
            await _central.ConfigAsync(Characteristic, GattCharacteristicSettings.Notify);
        }

        private DelegateCommand<string> _writeCommand;
        public DelegateCommand<string> WriteCommand
            => _writeCommand ??= new DelegateCommand<string>(ExecuteWriteCommand);

        private async void ExecuteWriteCommand(string value)
        {
            var data = Encoding.UTF8.GetBytes($"{value}\r\n");
            await _central.WriteAsync(Characteristic, data, GattCharacteristicWriteType.Default);
        }

        private DelegateCommand<object> _selectCommand;
        public DelegateCommand<object> SelectCommand
            => _selectCommand ??= new DelegateCommand<object>(ExecuteSelectCommand);

        private void ExecuteSelectCommand(object @object)
        {
            if (@object is TreeNode node &&
                node.Object is GattCharacteristic characteristic)
            {
                Characteristic = characteristic;
            }
            else
            {
                Characteristic = null;
            }
        }

        private DelegateCommand _readCommand;
        public DelegateCommand ReadCommand
            => _readCommand ??= new DelegateCommand(ExecuteReadCommand);

        private async void ExecuteReadCommand()
        {
            var value = await _central.ReadAsync(Characteristic);
            var str = Encoding.UTF8.GetString(value);
        }
    }
}
