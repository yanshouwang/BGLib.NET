using System;
using System.Collections.Generic;
using System.Linq;

namespace BGLib.SDK
{
    internal class MessageAnalyzer : IMessageAnalyzer
    {
        public event EventHandler<MessageEventArgs> Analyzed;

        private readonly IList<byte> _value;

        private byte? _byet0;
        private byte? _byte1;
        private byte? _byte2;
        private byte? _byte3;

        private ushort? _length;

        public MessageAnalyzer()
        {
            _value = new List<byte>();
        }

        public void Analyze(byte[] value)
        {
            foreach (var byteValue in value)
            {
                if (_byet0 == null)
                {
                    _byet0 = byteValue;
                }
                else if (_byte1 == null)
                {
                    _byte1 = byteValue;
                    _length = (ushort)(_byet0 << 8 & 0x700 | _byte1);
                }
                else if (_byte2 == null)
                {
                    _byte2 = byteValue;
                }
                else if (_byte3 == null)
                {
                    _byte3 = byteValue;
                    if (_length > 0)
                        continue;
                    OnAnalyzed();
                }
                else
                {
                    _value.Add(byteValue);
                    if (_value.Count < _length)
                        continue;
                    OnAnalyzed();
                }
            }
        }

        private void OnAnalyzed()
        {
            var type = (byte)(_byet0 >> 7);
            var deviceType = (byte)(_byet0 >> 3 & 0x0F);
            var category = (byte)_byte2;
            var id = (byte)_byte3;
            var value = _value.ToArray();
            _byet0 = null;
            _byte1 = null;
            _byte2 = null;
            _byte3 = null;
            _length = null;
            _value.Clear();
            var message = new Message(type, deviceType, category, id, value);
            var eventArgs = new MessageEventArgs(message);
            Analyzed?.Invoke(this, eventArgs);
        }
    }
}
