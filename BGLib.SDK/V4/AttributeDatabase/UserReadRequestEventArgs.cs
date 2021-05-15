using System;

namespace BGLib.SDK.V4.AttributeDatabase
{
    public class UserReadRequestEventArgs : EventArgs
    {
        public UserReadRequestEventArgs(byte connection, ushort handle, ushort offset, byte maxSize)
        {
            Connection = connection;
            Handle = handle;
            Offset = offset;
            MaxSize = maxSize;
        }

        /// <summary>
        /// Connection ID which requested attribute
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Attribute handle requested
        /// </summary>
        public ushort Handle { get; }
        /// <summary>
        /// Attribute offset to send data from
        /// </summary>
        public ushort Offset { get; }
        /// <summary>
        /// <para>Maximum data size to respond with</para>
        /// <para>
        /// If more data is sent than indicated by this parameter, the extra bytes will be
        /// ignored.
        /// </para>
        /// </summary>
        public byte MaxSize { get; }
    }
}