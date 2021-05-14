using System;

namespace BGLib.SDK.Connection
{
    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectedEventArgs(byte connection, ushort reason)
        {
            Connection = connection;
            Reason = reason;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// <para>Disconnection reason code</para>
        /// <para>0 : disconnected by local user</para>
        /// </summary>
        public ushort Reason { get; }
    }
}