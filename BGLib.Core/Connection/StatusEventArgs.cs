using BGLib.Core.GAP;
using System;

namespace BGLib.Core.Connection
{
    public class StatusEventArgs : EventArgs
    {
        public StatusEventArgs(byte connection, ConnectionStatus flags, byte[] address, AddressType addressType, ushort connInterval, ushort timeout, ushort latency, byte bonding)
        {
            Connection = connection;
            Flags = flags;
            Address = address;
            AddressType = addressType;
            ConnInterval = connInterval;
            Timeout = timeout;
            Latency = latency;
            Bonding = bonding;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Connection status flags use connstatus-enumerator
        /// </summary>
        public ConnectionStatus Flags { get; }
        /// <summary>
        /// Remote devices Bluetooth address
        /// </summary>
        public byte[] Address { get; }
        /// <summary>
        /// Remote address type see: Bluetooth Address Types--gap
        /// </summary>
        public AddressType AddressType { get; }
        /// <summary>
        /// Current connection interval (units of 1.25ms)
        /// </summary>
        public ushort ConnInterval { get; }
        /// <summary>
        /// Current supervision timeout (units of 10ms)
        /// </summary>
        public ushort Timeout { get; }
        /// <summary>
        /// Slave latency which tells how many connection intervals the slave may
        /// skip.
        /// </summary>
        public ushort Latency { get; }
        /// <summary>
        /// <para>Bonding handle if the device has been bonded with.</para>
        /// <para>Otherwise: 0xFF</para>
        /// </summary>
        public byte Bonding { get; }
    }
}