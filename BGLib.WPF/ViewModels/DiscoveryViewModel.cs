﻿using BGLib.Wand;
using Prism.Mvvm;
using System.Collections.Generic;

namespace BGLib.WPF.ViewModels
{
    class DiscoveryViewModel : BindableBase
    {
        private DiscoveryType _type;
        public DiscoveryType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public MAC MAC { get; }
        public MacType MacType { get; }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private IDictionary<byte, byte[]> _advertisements;
        public IDictionary<byte, byte[]> Advertisements
        {
            get => _advertisements;
            set => SetProperty(ref _advertisements, value);
        }

        private sbyte _rssi;
        public sbyte RSSI
        {
            get => _rssi;
            set => SetProperty(ref _rssi, value);
        }

        public DiscoveryViewModel(DiscoveryType type, MAC mac,MacType macType, string name, IDictionary<byte, byte[]> advertisements, sbyte rssi)
        {
            Type = type;
            MAC = mac;
            MacType = macType;
            Name = name;
            Advertisements = advertisements;
            RSSI = rssi;
        }
    }
}
