using System;

namespace BGLib.API
{
    public class BGAttributeEventArgs : EventArgs
    {
        public byte Connection { get; }
        public ushort Attribute { get; }

        public BGAttributeEventArgs(byte connection, ushort attribute)
        {
            Connection = connection;
            Attribute = attribute;
        }
    }
}