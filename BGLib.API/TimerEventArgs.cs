using System;

namespace BGLib.API
{
    public class TimerEventArgs : EventArgs
    {
        public TimerEventArgs(byte timer)
        {
            Timer = timer;
        }

        /// <summary>
        /// The software timer handle
        /// </summary>
        public byte Timer { get; }
    }
}