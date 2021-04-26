using System;
using System.Linq;

namespace BGLib.API
{
    public class BGAddress
    {
        public BGAddressType Type { get; }
        public byte[] RawValue { get; }
        public string Value { get; set; }

        public BGAddress(BGAddressType type, byte[] rawValue)
        {
            if (rawValue == null || rawValue.Length != 6)
            {
                var message = "A MAC address must be an array with 6 bytes.";
                var paramName = nameof(rawValue);
                throw new ArgumentException(message, paramName);
            }
            Type = type;
            RawValue = rawValue;
            // Bluetooth address in little endian format
            var reversed = rawValue.Reverse().ToArray();
            Value = BitConverter.ToString(reversed).Replace('-', ':');
        }

        public override string ToString()
        {
            return Value;
        }

        public override bool Equals(object obj)
        {
            return obj is BGAddress address && address.Value == Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}