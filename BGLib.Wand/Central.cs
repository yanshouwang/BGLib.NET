using BGLib.SDK.V4;
using BGLib.SDK.V4.AttributeClient;
using BGLib.SDK.V4.Connection;
using BGLib.SDK.V4.GAP;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;

namespace BGLib.Wand
{
    public class Central : IDisposable
    {
        public event EventHandler<DiscoveryEventArgs> Discovered;
        public event EventHandler<PeripheralEventArgs> ConnectionLost;
        public event EventHandler<GattCharacteristicValueEventArgs> CharacteristicValueChanged;

        private readonly MessageHub _messageHub;
        private readonly IDictionary<byte, Peripheral> _peripherals;

        public Central(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _messageHub = new MessageHub(portName, baudRate, parity, dataBits, stopBits);
            _peripherals = new Dictionary<byte, Peripheral>();
            _messageHub.GAP.ScanResponse += OnScanResponse;
            _messageHub.Connection.Disconnected += OnDisconnected;
            _messageHub.AttributeClient.AttributeValue += OnAttributeValue;
        }

        private void OnScanResponse(object sender, ScanResponseEventArgs e)
        {
            var type = (DiscoveryType)e.PacketType;
            var mac = new MAC(e.Sender);
            var macType = (MacType)e.AddressType;
            var eventArgs = new DiscoveryEventArgs(type, mac, macType, e.Data, e.RSSI);
            Discovered?.Invoke(this, eventArgs);
        }

        private void OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            var removed = _peripherals.Remove(e.Connection, out var peripheral);
            if (!removed || e.Reason == 0x0216) // We didn't hold the connection or disconnected by central.
                return;
            var eventArgs = new PeripheralEventArgs(peripheral);
            ConnectionLost?.Invoke(this, eventArgs);
        }

        private void OnAttributeValue(object sender, AttributeValueEventArgs e)
        {
            var notifyOrIndicate = e.Type == AttributeValueType.Notify || e.Type == AttributeValueType.Indicate;
            if (!notifyOrIndicate ||
                !_peripherals.TryGetValue(e.Connection, out var peripheral) ||
                !peripheral.Services.TryGetValue(e.AttHandle, out var service) ||
                !peripheral.Characteristics.TryGetValue(e.AttHandle, out var characteristic))
            {
                return;
            }
            var eventArgs = new GattCharacteristicValueEventArgs(peripheral, service, characteristic, e.Value);
            CharacteristicValueChanged?.Invoke(this, eventArgs);

        }

        public async Task StartDiscoveryAsync(DiscoverMode mode = DiscoverMode.Generic, DiscoverSettings settings = null)
        {
            if (settings != null)
            {
                var activeValue = settings.Active ? (byte)1 : (byte)0;
                await _messageHub.GAP.SetScanParametersAsync(settings.Interval, settings.Window, activeValue);
            }
            var modeV4 = (SDK.V4.GAP.DiscoverMode)mode;
            await _messageHub.GAP.DiscoverAsync(modeV4);
        }

        public async Task StopDiscoveryAsync()
        {
            await _messageHub.GAP.EndProcedureAsync();
        }

        public async Task<Peripheral> ConnectAsync(MAC mac, MacType macType)
        {
            var connectionTCS = new TaskCompletionSource<byte>();
            var onStatus = new EventHandler<StatusEventArgs>((s, e) =>
            {
                var eMAC = new MAC(e.Address);
                var eMacType = (MacType)e.AddressType;
                if (eMAC != mac || eMacType != macType)
                    return;
                connectionTCS.TrySetResult(e.Connection);
            });
            _messageHub.Connection.Status += onStatus;
            try
            {
                var value = mac.ToArray();
                var type = (AddressType)macType;
                await _messageHub.GAP.ConnectDirectAsync(value, type, 0x20, 0x30, 0x100, 0);
                var connection = await connectionTCS.Task;
                var peripheral = new Peripheral(connection, mac, macType);
                _peripherals[connection] = peripheral;
                return peripheral;
            }
            finally
            {
                _messageHub.Connection.Status -= onStatus;
            }
        }

        public async Task<IList<GattService>> GetServicesAsync(Peripheral peripheral)
        {
            var connection = peripheral.Connection;
            var itemsTCS = new TaskCompletionSource<IList<GroupFoundEventArgs>>();
            var items = new List<GroupFoundEventArgs>();
            var onGroupFound = new EventHandler<GroupFoundEventArgs>((s, e) =>
            {
                if (e.Connection != connection)
                    return;
                items.Add(e);
            });
            var onProcedureCompleted = new EventHandler<ProcedureCompletedEventArgs>((s, e) =>
            {
                if (e.Connection != connection)
                    return;
                //if (e.ErrorCode == 0)
                //{
                //    itemsTCS.TrySetResult(items);
                //}
                //else
                //{
                //    var error = new ErrorException(e.ErrorCode);
                //    itemsTCS.TrySetException(error);
                //}
                itemsTCS.TrySetResult(items);
            });
            _messageHub.AttributeClient.GroupFound += onGroupFound;
            _messageHub.AttributeClient.ProcedureCompleted += onProcedureCompleted;
            try
            {
                var start = (ushort)0x0001;
                var end = (ushort)0xFFFF;
                var uuid = (ushort)0x2800;
                var uuidValue = BitConverter.GetBytes(uuid);
                await _messageHub.AttributeClient.ReadByGroupTypeAsync(connection, start, end, uuidValue);
                await itemsTCS.Task;
            }
            finally
            {
                _messageHub.AttributeClient.GroupFound -= onGroupFound;
                _messageHub.AttributeClient.ProcedureCompleted -= onProcedureCompleted;
            }
            var services = new List<GattService>();
            foreach (var item in items)
            {
                var uuid = item.UUID.ToUUID();
                var service = new GattService(connection, item.Start, item.End, uuid);
                services.Add(service);
            }
            return services;
        }

        public async Task<IList<GattCharacteristic>> GetCharacteristicsAsync(GattService service)
        {
            var connection = service.Connection;
            var itemsTCS = new TaskCompletionSource<IList<AttributeValueEventArgs>>();
            var items = new List<AttributeValueEventArgs>();
            var onAttributeValue = new EventHandler<AttributeValueEventArgs>((s, e) =>
            {
                if (e.Connection != connection ||
                    e.Type != AttributeValueType.ReadByType)
                    return;
                items.Add(e);
            });
            var onProcedureCompleted = new EventHandler<ProcedureCompletedEventArgs>((s, e) =>
            {
                if (e.Connection != connection)
                    return;
                //if (e.ErrorCode == 0)
                //{
                //    itemsTCS.TrySetResult(items);
                //}
                //else
                //{
                //    var error = new ErrorException(e.ErrorCode);
                //    itemsTCS.TrySetException(error);
                //}
                itemsTCS.TrySetResult(items);
            });
            _messageHub.AttributeClient.AttributeValue += onAttributeValue;
            _messageHub.AttributeClient.ProcedureCompleted += onProcedureCompleted;
            try
            {
                var start = service.Start;
                var end = service.End;
                var uuid = (ushort)0x2803;
                var uuidValue = BitConverter.GetBytes(uuid);
                await _messageHub.AttributeClient.ReadByTypeAsync(connection, start, end, uuidValue);
                await itemsTCS.Task;
            }
            finally
            {
                _messageHub.AttributeClient.AttributeValue -= onAttributeValue;
                _messageHub.AttributeClient.ProcedureCompleted -= onProcedureCompleted;
            }
            var peripheral = _peripherals[connection];
            var characteristics = new List<GattCharacteristic>();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var start = item.AttHandle;
                var end = i < items.Count - 1
                    ? (ushort)(items[i + 1].AttHandle - 1)
                    : service.End;
                // BLUETOOTH CORE SPECIFICATION Version 5.2 | Vol 3, Part G, Page 1551
                var properties = (GattCharacteristicProperty)item.Value[0];
                var value = BitConverter.ToUInt16(item.Value, 1);
                var uuidValue = new byte[item.Value.Length - 3];
                Array.Copy(item.Value, 3, uuidValue, 0, uuidValue.Length);
                var uuid = uuidValue.ToUUID();
                var characteristic = new GattCharacteristic(connection, start, end, value, uuid, properties);
                peripheral.Services[value] = service;
                peripheral.Characteristics[value] = characteristic;
                characteristics.Add(characteristic);
            }
            return characteristics;
        }

        public async Task<byte[]> ReadAsync(GattCharacteristic characteristic)
        {
            var connection = characteristic.Connection;
            var handle = characteristic.Start;
            var valueTCS = new TaskCompletionSource<byte[]>();
            var onAttributeValue = new EventHandler<AttributeValueEventArgs>((s, e) =>
            {
                if (e.Connection != connection ||
                    e.AttHandle != handle ||
                    e.Type != AttributeValueType.Read)
                    return;
                valueTCS.TrySetResult(e.Value);
            });
            _messageHub.AttributeClient.AttributeValue += onAttributeValue;
            try
            {
                await _messageHub.AttributeClient.ReadByHandleAsync(connection, handle);
                return await valueTCS.Task;
            }
            finally
            {
                _messageHub.AttributeClient.AttributeValue -= onAttributeValue;
            }
        }

        public async Task WriteAsync(GattCharacteristic characteristic, byte[] value, GattCharacteristicWriteType type)
        {
            var connection = characteristic.Connection;
            var handle = characteristic.Value;
            switch (type)
            {
                case GattCharacteristicWriteType.Default:
                    await WriteAttributeAsync(connection, handle, value);
                    break;
                case GattCharacteristicWriteType.NoResponse:
                    await _messageHub.AttributeClient.WriteCommandAsync(connection, handle, value);
                    break;
                default:
                    break;
            }
        }

        private async Task WriteAttributeAsync(byte connection, ushort handle, byte[] value)
        {
            var writeTCS = new TaskCompletionSource<bool>();
            var onProcedureCompleted = new EventHandler<ProcedureCompletedEventArgs>((s, e) =>
            {
                if (e.Connection != connection || e.ChrHandle != handle)
                    return;
                if (e.ErrorCode == 0)
                {
                    writeTCS.TrySetResult(true);
                }
                else
                {
                    var error = new SDK.ErrorException(e.ErrorCode);
                    writeTCS.TrySetException(error);
                }
            });
            _messageHub.AttributeClient.ProcedureCompleted += onProcedureCompleted;
            try
            {
                await _messageHub.AttributeClient.AttributeWriteAsync(connection, handle, value);
                await writeTCS.Task;
            }
            finally
            {
                _messageHub.AttributeClient.ProcedureCompleted += onProcedureCompleted;
            }
        }

        public async Task ConfigAsync(GattCharacteristic characteristic, GattCharacteristicSettings settings)
        {
            var connection = characteristic.Connection;
            var itemsTCS = new TaskCompletionSource<IList<FindInformationFoundEventArgs>>();
            var items = new List<FindInformationFoundEventArgs>();
            var onFindInformationFound = new EventHandler<FindInformationFoundEventArgs>((s, e) =>
            {
                if (e.Connection != connection)
                    return;
                items.Add(e);
            });
            var onProcedureCompleted = new EventHandler<ProcedureCompletedEventArgs>((s, e) =>
            {
                if (e.Connection != connection)
                    return;
                if (e.ErrorCode == 0)
                {
                    itemsTCS.TrySetResult(items);
                }
                else
                {
                    var error = new SDK.ErrorException(e.ErrorCode);
                    itemsTCS.TrySetException(error);
                }
            });
            _messageHub.AttributeClient.FindInformationFound += onFindInformationFound;
            _messageHub.AttributeClient.ProcedureCompleted += onProcedureCompleted;
            try
            {
                var start = characteristic.Start;
                var end = characteristic.End;
                await _messageHub.AttributeClient.FindInformationAsync(connection, start, end);
                await itemsTCS.Task;
            }
            finally
            {
                _messageHub.AttributeClient.FindInformationFound -= onFindInformationFound;
                _messageHub.AttributeClient.ProcedureCompleted -= onProcedureCompleted;
            }
            var item = items.First(i =>
            {
                var uuid = BitConverter.ToUInt16(i.UUID, 0);
                return uuid == 0x2902;
            });
            var handle = item.ChrHandle;
            var settingsValue = (ushort)settings;
            var value = BitConverter.GetBytes(settingsValue);
            await _messageHub.AttributeClient.WriteCommandAsync(connection, handle, value);
        }

        public async Task DisconnectAsync(Peripheral peripheral)
        {
            var connection = peripheral.Connection;
            await _messageHub.Connection.DisconnectAsync(connection);
        }

        #region IDisposable

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    _messageHub.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并替代终结器
                // TODO: 将大型字段设置为 null
                _disposed = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~BGCentral()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
