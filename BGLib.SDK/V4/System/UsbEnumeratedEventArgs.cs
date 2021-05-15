using System;

namespace BGLib.SDK.V4.System
{
    public class UsbEnumeratedEventArgs : EventArgs
    {
        public UsbEnumeratedEventArgs(byte state)
        {
            State = state;
        }

        /// <summary>
        /// <para>0: device is not enumerated</para>
        /// <para>1: device is enumerated</para>
        /// </summary>
        public byte State { get; }
    }
}