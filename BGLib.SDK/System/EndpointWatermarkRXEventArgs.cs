using System;

namespace BGLib.SDK.System
{
    public class EndpointWatermarkRXEventArgs : EventArgs
    {
        public EndpointWatermarkRXEventArgs(Endpoint endpoint, byte data)
        {
            Endpoint = endpoint;
            Data = data;
        }

        /// <summary>
        /// Endpoint index where data was received
        /// </summary>
        public Endpoint Endpoint { get; }
        /// <summary>
        /// Received data size
        /// </summary>
        public byte Data { get; }
    }
}