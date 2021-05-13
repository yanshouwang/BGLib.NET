using System;

namespace BGLib.LowEnergy
{
    public class PeripheralEventArgs : EventArgs
    {
        public PeripheralEventArgs(Peripheral peripheral)
        {
            Peripheral = peripheral;
        }

        public Peripheral Peripheral { get; }
    }
}