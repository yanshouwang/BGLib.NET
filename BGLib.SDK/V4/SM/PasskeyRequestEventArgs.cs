using System;

namespace BGLib.SDK.V4.SM
{
    public class PasskeyRequestEventArgs : EventArgs
    {
        public PasskeyRequestEventArgs(byte handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Handle { get; }
    }
}