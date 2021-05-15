using System;

namespace BGLib.Wand
{
    /// <summary>
    /// The Characteristic Properties bit field determines how the Characteristic Value 
    /// can be used, or how the characteristic descriptors(see Section 3.3.3) can be
    /// accessed.If the bits defined in Table 3.5 are set, the action described is 
    /// permitted.Multiple characteristic properties can be set.
    /// </summary>
    [Flags]
    public enum GattCharacteristicProperty : byte
    {
        /// <summary>
        /// If set, permits broadcasts of the Characteristic Value using 
        /// Server Characteristic Configuration Descriptor. If set, the Server
        /// Characteristic Configuration Descriptor shall exist.
        /// </summary>
        Broadcast = 0x01,
        /// <summary>
        /// If set, permits reads of the Characteristic Value using procedures 
        /// defined in Section 4.8
        /// </summary>
        Read = 0x02,
        /// <summary>
        /// If set, permit writes of the Characteristic Value without response 
        /// using procedures defined in Section 4.9.1.
        /// </summary>
        WriteWithoutResponse = 0x04,
        /// <summary>
        /// If set, permits writes of the Characteristic Value with response 
        /// using procedures defined in Section 4.9.3 or Section 4.9.4.
        /// </summary>
        Write = 0x08,
        /// <summary>
        /// If set, permits notifications of a Characteristic Value without
        /// acknowledgment using the procedure defined in Section 4.10. If
        /// set, the Client Characteristic Configuration Descriptor shall exist.
        /// </summary>
        Notify = 0x10,
        /// <summary>
        /// If set, permits indications of a Characteristic Value with acknowl-
        /// edgment using the procedure defined in Section 4.11. If set, the 
        /// Client Characteristic Configuration Descriptor shall exist.
        /// </summary>
        Indicate = 0x20,
        /// <summary>
        /// If set, permits signed writes to the Characteristic Value using the 
        /// procedure defined in Section 4.9.2.
        /// </summary>
        AuthenticatedSignedWrites = 0x40,
        /// <summary>
        /// If set, additional characteristic properties are defined in the Char-
        /// acteristic Extended Properties Descriptor defined in Section 
        /// 3.3.3.1. If set, the Characteristic Extended Properties Descriptor
        /// shall exist.
        /// </summary>
        ExtendedProperties = 0x80,
    }
}