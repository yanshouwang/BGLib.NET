using BGLib.Core;
using BGLib.Core.GAP;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace BGLib.LowEnergy
{
    public class Central : IDisposable
    {
        public event EventHandler<DiscoveryEventArgs> Discovered;

        private readonly SerialCommunicator _communicator;
        private readonly MessageHub _messgeHub;

        public Central(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _communicator = new SerialCommunicator(portName, baudRate, parity, dataBits, stopBits);
            _messgeHub = new MessageHub(_communicator);
            _messgeHub.GAP.ScanResponse += OnScanResponse;
        }

        private void OnScanResponse(object sender, ScanResponseEventArgs e)
        {
            var type = (DiscoveryType)e.PacketType;
            var address = new Address(e.AddressType, e.Sender);
            var eventArgs = new DiscoveryEventArgs(type, address, e.Bond, e.RSSI, e.Data, _messgeHub);
            Discovered?.Invoke(this, eventArgs);
        }

        public async Task StartDiscoveryAsync(DiscoverMode mode = DiscoverMode.Generic, DiscoverSettings settings = null)
        {
            if (settings != null)
            {
                var activeValue = settings.Active ? (byte)1 : (byte)0;
                await _messgeHub.GAP.SetScanParametersAsync(settings.Interval, settings.Window, activeValue);
            }
            await _messgeHub.GAP.DiscoverAsync(mode);
        }

        public async Task StopDiscoveryAsync()
        {
            await _messgeHub.GAP.EndProcedureAsync();
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
                    _communicator.Dispose();
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
