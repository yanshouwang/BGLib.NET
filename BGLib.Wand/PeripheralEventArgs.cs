using System;

namespace BGLib.Wand
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