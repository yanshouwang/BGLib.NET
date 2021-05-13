using System;

namespace BGLib.LowEnergy
{
    public class GattCharacteristicValueEventArgs : EventArgs
    {
        public GattCharacteristicValueEventArgs(Peripheral peripheral, GattService service, GattCharacteristic characteristic, byte[] value)
        {
            Peripheral = peripheral;
            Service = service;
            Characteristic = characteristic;
            Value = value;
        }

        public Peripheral Peripheral { get; }
        public GattService Service { get; }
        public GattCharacteristic Characteristic { get; }
        public byte[] Value { get; }
    }
}