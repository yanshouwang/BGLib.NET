namespace BGLib.Core.GAP
{
    /// <summary>
    /// Scan Policy
    /// </summary>
    public enum ScanPolicy : byte
    {
        /// <summary>
        /// Accept All advertisement Packets (default)
        /// </summary>
        All = 0,
        /// <summary>
        /// Ignore advertisement packets from remote slaves not in the running
        /// whitelist
        /// </summary>
        Whitelist = 1,
    }
}