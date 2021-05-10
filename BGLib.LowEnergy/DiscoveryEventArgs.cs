using BGLib.Core;
using BGLib.Core.GAP;
using System;
using System.Collections.Generic;
using System.Text;

namespace BGLib.LowEnergy
{
    public class DiscoveryEventArgs : EventArgs
    {
        public DiscoveryType Type { get; }
        public sbyte RSSI { get; }
        public byte[] RawAdvertisements { get; }
        public IDictionary<byte, byte[]> Advertisements { get; }
        public Device Device { get; }

        public DiscoveryEventArgs(DiscoveryType type, Address address, byte bond, sbyte rssi, byte[] rawAdvertisement, MessageHub messageHub)
        {
            Type = type;
            RSSI = rssi;
            RawAdvertisements = rawAdvertisement;
            Advertisements = new Dictionary<byte, byte[]>();

            var i = 0;
            while (i < rawAdvertisement.Length)
            {
                // Notice that advertisement or scan response data must be formatted in accordance to the Bluetooth Core
                // Specification.See BLUETOOTH SPECIFICATION Version 4.0[Vol 3 - Part C - Chapter 11].
                var length = rawAdvertisement[i++];
                var key = rawAdvertisement[i++];
                var value = new byte[length - 1];
                Array.Copy(rawAdvertisement, i, value, 0, value.Length);
                Advertisements[key] = value;
                i += value.Length;
            }

            var name = Advertisements.TryGetValue(0x08, out var nameValue) ||
                       Advertisements.TryGetValue(0x09, out nameValue)
                       ? Encoding.UTF8.GetString(nameValue)
                       : null;
            Device = new Device(address, name, bond, messageHub);
        }
    }
}