using System;
using System.IO.Ports;

namespace BGLib.SDK
{
    internal class SerialCommunicator : ICommunicator, IDisposable
    {
        private readonly SerialPort _serial;

        public event EventHandler<ValueEventArgs> ValueChanged;

        public void Write(byte[] value)
        {
            _serial.Write(value, 0, value.Length);
        }

        public SerialCommunicator(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _serial = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _serial.DataReceived += OnDataReceived;
            _serial.Open();
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var value = new byte[_serial.BytesToRead];
            _serial.Read(value, 0, value.Length);
            var eventArgs = new ValueEventArgs(value);
            ValueChanged?.Invoke(this, eventArgs);
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
                    _serial.Close();
                    _serial.Dispose();
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
