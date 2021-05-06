namespace BGLib.API
{
    /// <summary>
    /// Scan response header
    /// </summary>
    public enum DiscoveryType : byte
    {
        /// <summary>
        /// Connectable Advertisement packet
        /// </summary>
        Connectable = 0,
        /// <summary>
        /// Non Connectable Advertisement packet
        /// </summary>
        NonConnectable = 2,
        /// <summary>
        /// Scan response packet
        /// </summary>
        ScanResponse = 4,
        /// <summary>
        /// Discoverable advertisement packet
        /// </summary>
        Discoverable = 6,
    }
}