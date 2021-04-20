using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BGLib.API
{
    public class BGDiscovery
    {
        public sbyte RSSI { get; }
        public BGDiscoveryType Type { get; }
        public BGAddress Address { get; }
        public IList<BGAdvertisement> Advertisements { get; }
        public string Name { get; }

        public BGDiscovery(sbyte rssi, BGDiscoveryType type, BGAddress address, IList<BGAdvertisement> advertisements)
        {
            RSSI = rssi;
            Type = type;
            Address = address;
            Advertisements = advertisements;
            var advertisement = Advertisements.FirstOrDefault(i =>
                i.Type == BGAdvertisementType.ShortenedLocalName ||
                i.Type == BGAdvertisementType.CompleteLocalName);
            if (advertisement != null)
            {
                Name = Encoding.UTF8.GetString(advertisement.Value);
            }
        }
    }
}