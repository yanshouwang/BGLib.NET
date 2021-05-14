namespace BGLib.SDK.GAP
{
    /// <summary>
    /// Scan header flags
    /// </summary>
    public enum AdFlag : byte
    {
        /// <summary>
        /// Limited discoverability
        /// </summary>
        LimitedDiscoverable = 0x01,
        /// <summary>
        /// General discoverability
        /// </summary>
        GeneralDiscoverable = 0x02,
        /// <summary>
        /// BR/EDR not supported
        /// </summary>
        BredrNotSupported = 0x04,
        /// <summary>
        /// BR/EDR controller
        /// </summary>
        SimultaneousLebredrCtrl = 0x10,
        /// <summary>
        /// BE/EDR host
        /// </summary>
        SimultaneousLebredrHost = 0x20,
        /// <summary>
        /// -
        /// </summary>
        Mask = 0x1F,
    }
}
