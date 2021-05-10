using System;

namespace BGLib.Core.SM
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