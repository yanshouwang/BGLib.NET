using BGLib.API;
using Prism.Mvvm;
using System.Collections.Generic;

namespace BGLib.WPF.ViewModels
{
    class DiscoveryViewModel : BindableBase
    {
        public BGAddress Address { get; }

        string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        BGDiscoveryType _type;
        public BGDiscoveryType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        IList<BGAdvertisement> _advertisements;
        public IList<BGAdvertisement> Advertisements
        {
            get => _advertisements;
            set => SetProperty(ref _advertisements, value);
        }

        sbyte _rssi;
        public sbyte RSSI
        {
            get => _rssi;
            set => SetProperty(ref _rssi, value);
        }

        public DiscoveryViewModel(BGDiscovery discovery)
        {
            Address = discovery.Address;
            Name = discovery.Name;
            Type = discovery.Type;
            Advertisements = discovery.Advertisements;
            RSSI = discovery.RSSI;
        }
    }
}
