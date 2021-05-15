using System;

namespace BGLib.SDK.V4.SM
{
    /// <summary>
    /// Bonding information stored
    /// </summary>
    [Flags]
    public enum BondingKey : byte
    {
        /// <summary>
        /// LTK saved in master
        /// </summary>
        LTK = 0x01,
        /// <summary>
        /// Public Address
        /// </summary>
        AddrPublic = 0x02,
        /// <summary>
        /// Static Address
        /// </summary>
        AddrStatic = 0x04,
        /// <summary>
        /// Identity resolving key for resolvable private addresses
        /// </summary>
        IRK = 0x08,
        /// <summary>
        /// EDIV+RAND received from slave
        /// </summary>
        EdivRAND = 0x10,
        /// <summary>
        /// Connection signature resolving key
        /// </summary>
        CSRK = 0x20,
        /// <summary>
        /// EDIV+RAND sent to master
        /// </summary>
        Masterid = 0x40,
    }
}