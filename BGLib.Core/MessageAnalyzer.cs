using System;
using System.Collections.Generic;
using System.Linq;

namespace BGLib.Core
{
    internal class MessageAnalyzer
    {
        public event EventHandler<MessageEventArgs> Analyzed;

        private const ushort MAXIMUM = 64;

        private readonly IList<byte> _payload;

        private byte? _type;
        private byte? _length;
        private byte? _class;
        private byte? _id;

        public MessageAnalyzer()
        {
            _payload = new List<byte>();
        }

        public void Analyze(byte[] value)
        {
            foreach (var byteValue in value)
            {
                if (_type == null)
                {
                    AnalyzeType(byteValue);
                }
                else if (_length == null)
                {
                    AnalyzeLength(byteValue);
                }
                else if (_class == null)
                {
                    AnalyzeClass(byteValue);
                }
                else if (_id == null)
                {
                    AnalyzeId(byteValue);
                }
                else
                {
                    AnalyzePayload(byteValue);
                }
            }
        }

        private void AnalyzeType(byte value)
        {
            // X0000000, `LENGTH_HIGH` is always zero.
            var masked = value & 0x7F;
            if (masked != 0x00)
                return;
            _type = (byte)(value >> 7);
        }

        private void AnalyzeLength(byte value)
        {
            if (value > MAXIMUM)
            {
                _type = null;
            }
            else
            {
                _length = value;
            }
        }

        private void AnalyzeClass(byte value)
        {
            _class = value;
        }

        private void AnalyzeId(byte value)
        {
            _id = value;
            if (_length > 0)
                return;
            OnAnalyzed();
        }

        private void AnalyzePayload(byte value)
        {
            _payload.Add(value);
            if (_payload.Count < _length)
                return;
            OnAnalyzed();
        }

        private void OnAnalyzed()
        {
            var type = (byte)_type;
            var category = (byte)_class;
            var id = (byte)_id;
            var payload = _payload.ToArray();
            _type = null;
            _length = null;
            _class = null;
            _id = null;
            _payload.Clear();
            var message = new Message(type, category, id, payload);
            var eventArgs = new MessageEventArgs(message);
            Analyzed?.Invoke(this, eventArgs);
        }
    }
}
