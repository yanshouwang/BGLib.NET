using System;

namespace BGLib.SDK.System
{
    public class EndpointWatermarkTXEventArgs : EventArgs
    {
        public EndpointWatermarkTXEventArgs(Endpoint endpoint, byte data)
        {
            Endpoint = endpoint;
            Data = data;
        }

        /// <summary>
        /// Endpoint index where data was sent
        /// </summary>
        public Endpoint Endpoint { get; }
        /// <summary>
        /// Space available
        /// </summary>
        public byte Data { get; }
    }
}