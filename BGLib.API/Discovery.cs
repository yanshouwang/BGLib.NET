using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BGLib.API
{
    public class Discovery
    {
        public sbyte RSSI { get; }
        public DiscoveryType Type { get; }
        public Address Address { get; }
        public IList<Advertisement> Advertisements { get; }
        public string Name { get; }

        public Discovery(sbyte rssi, DiscoveryType type, Address address, IList<Advertisement> advertisements)
        {
            RSSI = rssi;
            Type = type;
            Address = address;
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