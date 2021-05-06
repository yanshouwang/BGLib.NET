using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BGLib.API
{
    public class Discovery
    {
        /// <summary>
        /// <para>RSSI value (dBm)</para>
        /// <para>Range: -103 to -38</para>
        /// </summary>
        public sbyte RSSI { get; }
        /// <summary>
        /// Scan response header
        /// </summary>
        public DiscoveryType Type { get; }
        /// <summary>
        /// Advertisers Bluetooth address
        /// </summary>
        public Address Address { get; }
        /// <summary>
        /// Bond handle if there is known bond for this device, 0xff otherwise
        /// </summary>
        public byte Bond { get; set; }
        /// <summary>
        /// Scan response data
        /// </summary>
        public IList<Advertisement> Advertisements { get; }
        public string Name { get; }

        public Discovery(sbyte rssi, DiscoveryType type, Address address, byte bond, IList<Advertisement> advertisements)
        {
            RSSI = rssi;
            Type = type;
            Address = address;
            Bond = bond;
            Advertisements = advertisements;
            var advertisement = Advertisements.FirstOrDefault(i =>
                i.Type == AdvertisementType.ShortenedLocalName ||
                i.Type == AdvertisementType.CompleteLocalName);
            if (advertisement != null)
            {
                Name = Encoding.UTF8.GetString(advertisement.Value);
            }
        }
    }
}