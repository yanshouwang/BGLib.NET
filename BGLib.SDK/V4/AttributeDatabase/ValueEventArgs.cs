using System;

namespace BGLib.SDK.V4.AttributeDatabase
{
    public class ValueEventArgs : EventArgs
    {
        public ValueEventArgs(byte connection, AttributeChangeReason reason, ushort handle, ushort offset, byte[] value)
        {
            Connection = connection;
            Reason = reason;
            Handle = handle;
            Offset = offset;
            Value = value;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Reason why value has changed
        /// </summary>
        public AttributeChangeReason Reason { get; }
        /// <summary>
        /// Attribute handle, which was changed
        /// </summary>
        public ushort Handle { get; }
        /// <summary>
        /// Offset into attribute value where data starts
        /// </summary>
        public ushort Offset { get; }
        /// <summary>
        /// Attribute value
        /// </summary>
        public byte[] Value { get; }
    }
}