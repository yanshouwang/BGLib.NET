using System;

namespace BGLib.API
{
    public class BGWatermarkEventArgs : EventArgs
    {
        /// <summary>
        /// <para>
        /// Endpoint index where data was received.
        /// </para>
        /// <para>
        /// Endpoint index where data was sent.
        /// </para>
        /// </summary>
        public BGEndpoint Endpoint { get; }
        /// <summary>
        /// <para>
        /// Received data size
        /// </para>
        /// <para>
        /// Space available.
        /// </para>
        /// </summary>
        public byte Size { get; }

        public BGWatermarkEventArgs(BGEndpoint endpoint, byte size)
        {
            Endpoint = endpoint;
            Size = size;
        }
    }
}