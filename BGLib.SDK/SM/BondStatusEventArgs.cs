using System;

namespace BGLib.SDK.SM
{
    public class BondStatusEventArgs : EventArgs
    {
        public BondStatusEventArgs(byte bond, byte keySize, byte mitm, BondingKey keys)
        {
            Bond = bond;
            KeySize = keySize;
            MITM = mitm;
            Keys = keys;
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
        public byte MITM { get; }
        /// <summary>
        /// Keys stored for bonding
        /// </summary>
        public BondingKey Keys { get; }
    }
}