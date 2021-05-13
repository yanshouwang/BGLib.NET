using BGLib.Core.GAP;
using System;
using System.Linq;

namespace BGLib.LowEnergy
{
    public class Address
    {
        public AddressType Type { get; }
        public byte[] RawValue { get; }
        public string Value { get; }

        public Address(AddressType type, byte[] rawValue)
        {
            Type = type;
            RawValue = rawValue;
            var reversed = rawValue.Reverse().ToArray();
            Value = BitConverter.ToString(reversed).Replace('-', ':');
        }

        public override string ToString()
        {
            return $"{Type} - {Value}";
        }

        public override bool Equals(object obj)
        {
            return obj is Address address &&
                   address.Type == Type &&
                   address.Value == Value;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() ^ Value.GetHashCode();
        }
    }
}
