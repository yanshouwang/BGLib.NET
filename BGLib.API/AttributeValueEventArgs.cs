using System;

namespace BGLib.API
{
    public class AttributeValueEventArgs : EventArgs
    {
        public AttributeValueEventArgs(byte connection, ushort attribute, AttributeType type, byte[] value)
        {
            Connection = connection;
            Attribute = attribute;
            Type = type;
            Value = value;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Attribute handle
        /// </summary>
        public ushort Attribute { get; }
        /// <summary>
        /// Attribute type
        /// </summary>
        public AttributeType Type { get; }
        /// <summary>
        /// Attribute value (data)
        /// </summary>
        public byte[] Value { get; }
    }
}