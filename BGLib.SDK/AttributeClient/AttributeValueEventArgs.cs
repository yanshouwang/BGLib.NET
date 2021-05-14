using System;

namespace BGLib.SDK.AttributeClient
{
    public class AttributeValueEventArgs : EventArgs
    {
        public AttributeValueEventArgs(byte connection, ushort attHandle, AttributeValueType type, byte[] value)
        {
            Connection = connection;
            AttHandle = attHandle;
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
        public ushort AttHandle { get; }
        /// <summary>
        /// Attribute type
        /// </summary>
        public AttributeValueType Type { get; }
        /// <summary>
        /// Attribute value (data)
        /// </summary>
        public byte[] Value { get; }
    }
}