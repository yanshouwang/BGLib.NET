using System;

namespace BGLib.API
{
    public class MultipleResponseEventArgs : EventArgs
    {
        public MultipleResponseEventArgs(byte connection, byte[] attributes)
        {
            Connection = connection;
            Attributes = attributes;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// This array contains the concatenated data from the multiple attributes that
        /// have been read, up to 22 bytes.
        /// </summary>
        public byte[] Attributes { get; }
    }
}