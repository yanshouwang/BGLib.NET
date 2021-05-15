using System;

namespace BGLib.SDK.V4.AttributeClient
{
    public class FindInformationFoundEventArgs : EventArgs
    {
        public FindInformationFoundEventArgs(byte connection, ushort chrHandle, byte[] uuid)
        {
            Connection = connection;
            ChrHandle = chrHandle;
            UUID = uuid;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Characteristics handle
        /// </summary>
        public ushort ChrHandle { get; }
        /// <summary>
        /// Characteristics type (UUID)
        /// </summary>
        public byte[] UUID { get; }
    }
}