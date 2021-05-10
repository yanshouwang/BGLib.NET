using System;

namespace BGLib.Core.PS
{
    public class KeyEventArgs : EventArgs
    {
        public KeyEventArgs(ushort key, byte[] value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// <para>Persistent Store key ID</para>
        /// <para>Values: 0x8000 to 0x807F</para>
        /// <para>0xFFFF: All keys have been dumped</para>
        /// </summary>
        public ushort Key { get; }
        /// <summary>
        /// Key value
        /// </summary>
        public byte[] Value { get; }
    }
}