using System;

namespace BGLib.API
{
    public class DeviceDisconnectedEventArgs : EventArgs
    {
        public DeviceDisconnectedEventArgs(byte connection, ushort reason)
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