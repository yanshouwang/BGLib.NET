using System;
using System.IO.Ports;

namespace BGLib.SDK
{
    public abstract class MessageHub : IDisposable
    {
        public event EventHandler<MessageEventArgs> Analyzed;

        private readonly SerialCommunicator _communicator;
        private readonly MessageAnalyzer _analyzer;

        internal byte Type { get; }

        public MessageHub(byte type, string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            Type = type;
            _communicator = new SerialCommunicator(portName, baudRate, parity, dataBits, stopBits);
            _analyzer = new MessageAnalyzer();

            _communicator.ValueChanged += OnValueChanged;
            _analyzer.Analyzed += OnAnalyzed;
        }

        private void OnValueChanged(object sender, ValueEventArgs e)
        {
            _analyzer.Analyze(e.Value);
        }

        private void OnAnalyzed(object sender, MessageEventArgs e)
        {
            Analyzed?.Invoke(this, e);
        }

        internal void Write(Message command)
        {
            var value = command.ToArray();
            _communicator.Write(value);
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
        // ~MessageHub()
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
