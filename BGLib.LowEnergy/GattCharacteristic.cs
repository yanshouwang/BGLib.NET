using System;

namespace BGLib.LowEnergy
{
    public class GattCharacteristic
    {
        public GattCharacteristic(byte connection, ushort start, ushort end, ushort value, Guid uuid, GattCharacteristicProperty properties)
        {
            Connection = connection;
            Start = start;
            End = end;
            Value = value;
            UUID = uuid;
            Properties = properties;
        }

        internal byte Connection { get; }
        internal ushort Start { get; }
        internal ushort End { get; }
        internal ushort Value { get; }

        public Guid UUID { get; }
        public GattCharacteristicProperty Properties { get; }

        public override string ToString()
        {
            return $"{UUID} - {Properties}";
        }
    }
}
