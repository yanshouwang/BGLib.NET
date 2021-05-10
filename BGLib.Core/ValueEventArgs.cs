using System;

namespace BGLib.Core
{
    public class ValueEventArgs : EventArgs
    {
        public byte[] Value { get; }

        public ValueEventArgs(byte[] value)
        {
            Value = value;
        }
    }
}