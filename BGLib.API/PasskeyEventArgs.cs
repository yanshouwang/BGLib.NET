using System;

namespace BGLib.API
{
    public class PasskeyEventArgs : EventArgs
    {
        public PasskeyEventArgs(byte connectioin, uint passkey)
        {
            Connectioin = connectioin;
            Passkey = passkey;
        }

        /// <summary>
        /// Bluetooth connection handle
        /// </summary>
        public byte Connectioin { get; set; }
        /// <summary>
        /// Passkey range: 000000-999999
        /// </summary>
        public uint Passkey { get; set; }
    }
}