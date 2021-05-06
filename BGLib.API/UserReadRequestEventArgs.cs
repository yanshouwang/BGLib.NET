using System;

namespace BGLib.API
{
    public class UserReadRequestEventArgs : EventArgs
    {
        public UserReadRequestEventArgs(byte connection, ushort attribute, ushort offset, byte maximum)
        {
            Connection = connection;
            Attribute = attribute;
            Offset = offset;
            Maximum = maximum;
        }

        /// <summary>
        /// Connection ID which requested attribute
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Attribute handle requested
        /// </summary>
        public ushort Attribute { get; }
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
        public byte Maximum { get; }
    }
}