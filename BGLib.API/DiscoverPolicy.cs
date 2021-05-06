namespace BGLib.API
{
    /// <summary>
    /// Discover policy
    /// </summary>
    public enum DiscoverPolicy : byte
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