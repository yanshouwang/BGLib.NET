﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BGLib.Wand
{
    public class DiscoveryEventArgs : EventArgs
    {
        public DiscoveryType Type { get; }
        public MAC MAC { get; }
        public MacType MacType { get; }
        public string Name { get; }
        public sbyte RSSI { get; }
        public byte[] RawAdvertisements { get; }
        public IDictionary<byte, byte[]> Advertisements { get; }

        public DiscoveryEventArgs(DiscoveryType type, MAC mac, MacType macType, byte[] rawAdvertisement, sbyte rssi)
        {
            Type = type;
            MAC = mac;
            MacType = macType;
            RawAdvertisements = rawAdvertisement;
            Advertisements = new Dictionary<byte, byte[]>();
            var i = 0;
            while (i < rawAdvertisement.Length)
            {
                // Notice that advertisement or scan response data must be formatted in accordance to the Bluetooth Core
                // Specification.See BLUETOOTH SPECIFICATION Version 4.0[Vol 3 - Part C - Chapter 11].
                var length = rawAdvertisement[i++];
                if (length == 0)
                    break;
                var key = rawAdvertisement[i++];
                var value = new byte[length - 1];
                Array.Copy(rawAdvertisement, i, value, 0, value.Length);
                Advertisements[key] = value;
                i += value.Length;
            }
            Name = Advertisements.TryGetValue(0x08, out var nameValue) || Advertisements.TryGetValue(0x09, out nameValue)
                 ? Encoding.UTF8.GetString(nameValue)
                 : null;
            RSSI = rssi;
        }
    }
}