using System;

namespace BGLib.API
{
    public class AttributeEventArgs : EventArgs
    {
        public byte Connection { get; }
        public ushort Attribute { get; }

        public AttributeEventArgs(byte connection, ushort attribute)
        {
            Connection = connection;
            Attribute = attribute;
        }
    }
}