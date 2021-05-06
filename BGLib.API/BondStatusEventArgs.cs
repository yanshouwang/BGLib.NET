using System;

namespace BGLib.API
{
    public class BondStatusEventArgs : EventArgs
    {
        public BondStatusEventArgs(byte bond, byte keySize, bool mitm, BondingKey key)
        {
            Bond = bond;
            KeySize = keySize;
            MITM = mitm;
            Key = key;
        }

        /// <summary>
        /// Bonding handle
        /// </summary>
        public byte Bond { get; }
        /// <summary>
        /// Encryption key size used in long-term key
        /// </summary>
        public byte KeySize { get; }
        /// <summary>
        /// <para>Was Man-in-the-Middle mode was used in pairing</para>
        /// <para>0: No MITM used</para>
        /// <para>1: MITM was used</para>
        /// </summary>
        public bool MITM { get; }
        /// <summary>
        /// Keys stored for bonding
        /// </summary>
        public BondingKey Key { get; }
    }
}