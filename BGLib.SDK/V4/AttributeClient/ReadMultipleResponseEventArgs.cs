using System;

namespace BGLib.SDK.V4.AttributeClient
{
    public class ReadMultipleResponseEventArgs : EventArgs
    {
        public ReadMultipleResponseEventArgs(byte connection, byte[] handles)
        {
            Connection = connection;
            Handles = handles;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// This array contains the concatenated data from the multiple attributes that
        /// have been read, up to 22 bytes.
        /// </summary>
        public byte[] Handles { get; }
    }
}