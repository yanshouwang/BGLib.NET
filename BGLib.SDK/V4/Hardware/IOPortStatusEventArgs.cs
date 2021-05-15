using System;

namespace BGLib.SDK.V4.Hardware
{
    public class IOPortStatusEventArgs : EventArgs
    {
        public IOPortStatusEventArgs(uint timestamp, byte port, byte irq, byte state)
        {
            Timestamp = timestamp;
            Port = port;
            Irq = irq;
            State = state;
        }

        /// <summary>
        /// <para>Value of internal timer</para>
        /// <para>Range : 0 to 2^24-1</para>
        /// </summary>
        public uint Timestamp { get; }
        /// <summary>
        /// I/O port
        /// </summary>
        public byte Port { get; }
        /// <summary>
        /// <para>I/O flags</para>
        /// <para>Tells which port caused interrupt(bitmask).</para>
        /// </summary>
        public byte Irq { get; }
        /// <summary>
        /// Current status of all I/Os in port(bitmask).
        /// </summary>
        public byte State { get; }
    }
}