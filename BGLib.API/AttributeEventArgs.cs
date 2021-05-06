using System;

namespace BGLib.API
{
    public class AttributeEventArgs : EventArgs
    {
        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Attribute handle
        /// </summary>
        public ushort Attribute { get; }

        public AttributeEventArgs(byte connection, ushort attribute)
        {
            Connection = connection;
            Attribute = attribute;
        }
    }
}