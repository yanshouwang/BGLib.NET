﻿using System;

namespace BGLib.API
{
    public class AnalogComparatorEventArgs : EventArgs
    {
        public AnalogComparatorEventArgs(uint timestamp, byte output)
        {
            Timestamp = timestamp;
            Output = output;
        }

        /// <summary>
        /// <para>Value of internal timer</para>
        /// <para>Range: 0 to 2^24-1</para>
        /// </summary>
        public uint Timestamp { get; }
        /// <summary>
        /// <para>Analog comparator output</para>
        /// <para>1: if V+ > V-</para>
        /// <para>0: if V+ &lt; V-</para>
        /// </summary>
        public byte Output { get; }
    }
}