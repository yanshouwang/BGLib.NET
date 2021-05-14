using System;

namespace BGLib.SDK.SM
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