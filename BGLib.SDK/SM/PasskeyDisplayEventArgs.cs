using System;

namespace BGLib.SDK.SM
{
    public class PasskeyDisplayEventArgs : EventArgs
    {
        public PasskeyDisplayEventArgs(byte handle, uint passkey)
        {
            Handle = handle;
            Passkey = passkey;
        }

        /// <summary>
        /// Bluetooth connection handle
        /// </summary>
        public byte Handle { get; }
        /// <summary>
        /// Passkey range: 000000-999999
        /// </summary>
        public uint Passkey { get; }
    }
}