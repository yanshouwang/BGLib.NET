namespace BGLib.API
{
    public enum AdvertisementType : byte
    {
        /// <summary>
        /// Flags
        /// </summary>
        Flags = 0x01,
        /// <summary>
        /// Incomplete List of 16-bit Service Class UUIDs
        /// </summary>
        IncompleteService16BitUUIDs = 0x02,
        /// <summary>
        /// Complete List of 16-bit Service Class UUIDs
        /// </summary>
        CompleteService16BitUUIDs = 0x03,
        /// <summary>
        /// Incomplete List of 32-bit Service Class UUIDs
        /// </summary>
        IncompleteService32BitUUIDs = 0x04,
        /// <summary>
        /// Complete List of 32-bit Service Class UUIDs
        /// </summary>
        CompleteService32BitUUIDs = 0x05,
        /// <summary>
        /// Incomplete List of 128-bit Service Class UUIDs
        /// </summary>
        IncompleteService128BitUUIDs = 0x06,
        /// <summary>
        /// Complete List of 128-bit Service Class UUIDs
        /// </summary>
        CompleteService128BitUUIDs = 0x07,
        /// <summary>
        /// Shortened Local Name
        /// </summary>
        ShortenedLocalName = 0x08,
        /// <summary>
        /// Complete Local Name
        /// </summary>
        CompleteLocalName = 0x09,
        /// <summary>
        /// TX Power Level
        /// </summary>
        TXPowerLevel = 0x0A,
        /// <summary>
        /// Class of Device
        /// </summary>
        DeviceClass = 0x0D,
        /// <summary>
        /// Simple Pairing Hash C
        /// </summary>
        SimplePairingHashC = 0x0E,
        /// <summary>
        /// Simple Pairing Hash C-192
        /// </summary>
        SimplePairingHashC192 = SimplePairingHashC,
        /// <summary>
        /// Simple Pairing Randomizer R
        /// </summary>
        SimplePairingRandomizerR = 0x0F,
        /// <summary>
        /// Simple Pairing Randomizer R-192
        /// </summary>
        SimplePairingRandomizerR192 = SimplePairingRandomizerR,
        /// <summary>
        /// Device ID
        /// </summary>
        DeviceId = 0x10,
        /// <summary>
        /// Security Manager TK Value
        /// </summary>
        SecurityManagerTKValue = DeviceId,
        /// <summary>
        /// Security Manager Out of Band Flags
        /// </summary>
        SecurityManagerOutOfBandFlags = 0x11,
        /// <summary>
        /// Slave Connection Interval Range
        /// </summary>
        SlaveConnectionIntervalRange = 0x12,
        /// <summary>
        /// List of 16-bit Service Solicitation UUIDs
        /// </summary>
        ServiceSolicitationUUIDs16Bit = 0x14,
        /// <summary>
        /// List of 128-bit Service Solicitation UUIDs
        /// </summary>
        ServiceSolicitationUUIDs128Bit = 0x15,
        /// <summary>
        /// Service Data - 16-bit UUID
        /// </summary>
        ServiceData16BitUUID = 0x16,
        /// <summary>
        /// Service Data
        /// </summary>
        ServiceData = ServiceData16BitUUID,
        /// <summary>
        /// Public Target Address
        /// </summary>
        PublicTargetAddress = 0x17,
        /// <summary>
        /// Random Target Address
        /// </summary>
        RandomTargetAddress = 0x18,
        /// <summary>
        /// Appearance
        /// </summary>
        Appearance = 0x19,
        /// <summary>
        /// Advertising Interval
        /// </summary>
        AdvertisingInterval = 0x1A,
        /// <summary>
        /// LE Bluetooth Device Address
        /// </summary>
        DeviceAddress = 0x1B,
        /// <summary>
        /// LE Role
        /// </summary>
        Role = 0x1C,
        /// <summary>
        /// Simple Pairing Hash C-256
        /// </summary>
        SimplePairingHashC256 = 0x1D,
        /// <summary>
        /// Simple Pairing Randomizer R-256
        /// </summary>
        SimplePairingRandomizerR256 = 0x1E,
        /// <summary>
        /// List of 32-bit Service Solicitation UUIDs
        /// </summary>
        ServiceSolicitationUUIDs32Bit = 0x1F,
        /// <summary>
        /// ​Service Data - 32-bit UUID
        /// </summary>
        ServiceDataUUID32Bit = 0x20,
        /// <summary>
        /// ​Service Data - 128-bit UUID
        /// </summary>
        ServiceDataUUID128Bit = 0x21,
        /// <summary>
        /// LE Secure Connections Confirmation Value
        /// </summary>
        SecureConnectionsConfirmationValue = 0x22,
        /// <summary>
        /// ​​​LE Secure Connections Random Value
        /// </summary>
        SecureConnectionsRandomValue = 0x23,
        /// <summary>
        /// URI
        /// </summary>
        URI = 0x24,
        /// <summary>
        /// Indoor Positioning
        /// </summary>
        IndoorPositioning = 0x25,
        /// <summary>
        /// Transport Discovery Data
        /// </summary>
        TransportDiscoveryData = 0x26,
        /// <summary>
        /// LE Supported Features
        /// </summary>
        SupportedFeatures = 0x27,
        /// <summary>
        /// Channel Map Update Indication
        /// </summary>
        ChannelMapUpdateIndication = 0x28,
        /// <summary>
        /// PB-ADV
        /// </summary>
        PBADV = 0x29,
        /// <summary>
        /// Mesh Message
        /// </summary>
        MeshMessage = 0x2A,
        /// <summary>
        /// Mesh Beacon
        /// </summary>
        MeshBeacon = 0x2B,
        /// <summary>
        /// 3D Information Data
        /// </summary>
        InformationData3D = 0x3D,
        /// <summary>
        /// Manufacturer Specific Data
        /// </summary>
        ManufacturerSpecificData = 0xFF,
    }
}