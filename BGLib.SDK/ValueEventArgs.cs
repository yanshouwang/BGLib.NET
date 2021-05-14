using System;

namespace BGLib.SDK
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