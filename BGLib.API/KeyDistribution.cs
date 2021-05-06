using System;

namespace BGLib.API
{
    /// <summary>
    /// Key Distribution
    /// </summary>
    [Flags]
    public enum KeyDistribution : byte
    {
        None = 0x00,
        /// <summary>
        ///  EncKey (LTK)
        /// </summary>
        LTK = 0x01,
        /// <summary>
        ///  IdKey (IRK)
        /// </summary>
        IRK = 0x02,
        /// <summary>
        /// Sign (CSRK)
        /// </summary>
        CSRK = 0x04,
        All = LTK | IRK | CSRK,
    }
}