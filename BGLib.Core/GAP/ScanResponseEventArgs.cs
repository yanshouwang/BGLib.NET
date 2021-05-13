using System;

namespace BGLib.Core.GAP
{
    public class ScanResponseEventArgs : EventArgs
    {
        public ScanResponseEventArgs(sbyte rssi, byte packetType, byte[] sender, AddressType addressType, byte bond, byte[] data)
        {
            RSSI = rssi;
            PacketType = packetType;
            Sender = sender;
            AddressType = addressType;
            Bond = bond;
            Data = data;
        }

        /// <summary>
        /// <para>RSSI value (dBm)</para>
        /// <para>Range: -103 to -38</para>
        /// </summary>
        public sbyte RSSI { get; }
        /// <summary>
        /// <para>Scan response header</para>
        /// <para>0: Connectable Advertisement packet</para>
        /// <para>2: Non Connectable Advertisement packet</para>
        /// <para>4: Scan response packet</para>
        /// <para>6: Discoverable advertisement packet</para>
        /// </summary>
        public byte PacketType { get; }
        /// <summary>
        /// Advertisers Bluetooth address
        /// </summary>
        public byte[] Sender { get; }
        /// <summary>
        /// Advertiser address type
        /// </summary>
        public AddressType AddressType { get; }
        /// <summary>
        /// Bond handle if there is known bond for this device, 0xff otherwise
        /// </summary>
        public byte Bond { get; }
        /// <summary>
        /// Scan response data
        /// </summary>
        public byte[] Data { get; }
    }
}