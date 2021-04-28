using System;

namespace BGLib.API
{
    public class WatermarkEventArgs : EventArgs
    {
        /// <summary>
        /// <para>
        /// Endpoint index where data was received.
        /// </para>
        /// <para>
        /// Endpoint index where data was sent.
        /// </para>
        /// </summary>
        public Endpoint Endpoint { get; }
        /// <summary>
        /// <para>
        /// Received data size
        /// </para>
        /// <para>
        /// Space available.
        /// </para>
        /// </summary>
        public byte Size { get; }

        public WatermarkEventArgs(Endpoint endpoint, byte size)
        {
            Endpoint = endpoint;
            Size = size;
        }
    }
}