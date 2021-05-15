using System.Collections.Generic;

namespace BGLib.Wand
{
    public class Peripheral
    {
        public Peripheral(byte connection, MAC mac, MacType macType)
        {
            Services = new Dictionary<ushort, GattService>();
            Characteristics = new Dictionary<ushort, GattCharacteristic>();
            Connection = connection;
            MAC = mac;
            MacType = macType;
        }

        internal IDictionary<ushort, GattService> Services { get; set; }
        internal IDictionary<ushort, GattCharacteristic> Characteristics { get; set; }

        internal byte Connection { get; }
        public MAC MAC { get; }
        public MacType MacType { get; }

        public override string ToString()
        {
            return $"{MAC} - {MacType}";
        }
    }
}