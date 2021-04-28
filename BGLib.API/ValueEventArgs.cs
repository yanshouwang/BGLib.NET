using System;

namespace BGLib.API
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