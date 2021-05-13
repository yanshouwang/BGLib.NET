using System.Collections.Generic;

namespace BGLib.LowEnergy
{
    public class Peripheral
    {
        public Peripheral(byte connection, Address address)
        {
            Services = new Dictionary<ushort, GattService>();
            Characteristics = new Dictionary<ushort, GattCharacteristic>();
            Connection = connection;
            Address = address;
        }

        internal IDictionary<ushort, GattService> Services { get; set; }
        internal IDictionary<ushort, GattCharacteristic> Characteristics { get; set; }

        internal byte Connection { get; }
        public Address Address { get; }

        public override string ToString()
        {
            return $"{Address}";
        }
    }
}