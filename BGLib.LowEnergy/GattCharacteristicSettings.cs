using System;

namespace BGLib.LowEnergy
{
    [Flags]
    public enum GattCharacteristicSettings : ushort
    {
        None = 0x0000,
        Notify = 0x0001,
        Indicate = 0x0002,
    }
}