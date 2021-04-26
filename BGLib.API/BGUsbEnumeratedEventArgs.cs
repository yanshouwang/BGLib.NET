using System;

namespace BGLib.API
{
    public class BGUsbEnumeratedEventArgs : EventArgs
    {
        public BGUsbEnumeratedEventArgs(bool enumerated)
        {
            Enumerated = enumerated;
        }

        public bool Enumerated { get; }
    }
}