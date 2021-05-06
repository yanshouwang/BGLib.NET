using System;

namespace BGLib.API
{
    public class ConnectionStateEventArgs : EventArgs
    {
        public ConnectionStateEventArgs(byte connection, ConnectionState status, Address address, ushort interval, ushort timeout, ushort latency, byte bonding)
        {
            Connection = connection;
            Status = status;
            Address = address;
            Interval = interval;
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
        public ConnectionState Status { get; }
        /// <summary>
        /// Remote devices Bluetooth address
        /// </summary>
        public Address Address { get; }
        /// <summary>
        /// Current connection interval (units of 1.25ms)
        /// </summary>
        public ushort Interval { get; }
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