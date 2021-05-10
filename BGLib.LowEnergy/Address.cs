using BGLib.Core.GAP;
using System;

namespace BGLib.LowEnergy
{
    public class Address
    {
        public AddressType Type { get; }
        public string Value { get; }

        public Address(AddressType type, byte[] value)
        {
            Type = type;
            Value = BitConverter.ToString(value).Replace('-', ':');
        }

        public override string ToString()
        {
            return $"{Type} - {Value}";
        }
    }
}
