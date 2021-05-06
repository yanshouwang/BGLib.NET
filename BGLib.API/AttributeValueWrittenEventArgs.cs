using System;

namespace BGLib.API
{
    public class AttributeValueWrittenEventArgs : EventArgs
    {
        public AttributeValueWrittenEventArgs(byte connection, AttributeValueChangeReason reason, ushort attribute, ushort offset, byte[] value)
        {
            Connection = connection;
            Reason = reason;
            Attribute = attribute;
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
        public AttributeValueChangeReason Reason { get; }
        /// <summary>
        /// Attribute handle, which was changed
        /// </summary>
        public ushort Attribute { get; }
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