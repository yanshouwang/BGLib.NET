using System;

namespace BGLib.Core.Hardware
{
    public class SoftTimerEventArgs : EventArgs
    {
        public SoftTimerEventArgs(byte handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// The software timer handle
        /// </summary>
        public byte Handle { get; }
    }
}