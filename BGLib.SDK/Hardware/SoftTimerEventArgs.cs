using System;

namespace BGLib.SDK.Hardware
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