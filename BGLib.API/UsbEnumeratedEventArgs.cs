using System;

namespace BGLib.API
{
    public class UsbEnumeratedEventArgs : EventArgs
    {
        public UsbEnumeratedEventArgs(bool enumerated)
        {
            Enumerated = enumerated;
        }

        public bool Enumerated { get; }
    }
}