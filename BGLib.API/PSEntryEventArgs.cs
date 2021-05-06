using System;

namespace BGLib.API
{
    public class PSEntryEventArgs : EventArgs
    {
        public PSEntryEventArgs(ushort key, byte[] value)
        {
            Key = key;
            Value = value;
        }

        public ushort Key { get; }
        public byte[] Value { get; }
    }
}