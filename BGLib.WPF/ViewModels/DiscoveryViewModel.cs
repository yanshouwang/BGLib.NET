using BGLib.API;
using Prism.Mvvm;
using System.Collections.Generic;

namespace BGLib.WPF.ViewModels
{
    class DiscoveryViewModel : BindableBase
    {
        public Address Address { get; }

        string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        DiscoveryType _type;
        public DiscoveryType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        IList<Advertisement> _advertisements;
        public IList<Advertisement> Advertisements
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

        public DiscoveryViewModel(Discovery discovery)
        {
            Address = discovery.Address;
            Name = discovery.Name;
            Type = discovery.Type;
            Advertisements = discovery.Advertisements;
            RSSI = discovery.RSSI;
        }
    }
}
