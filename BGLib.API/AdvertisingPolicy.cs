namespace BGLib.API
{
    /// <summary>
    /// Advertising policy
    /// </summary>
    public enum AdvertisingPolicy : byte
    {
        /// <summary>
        /// Respond to scan requests from any master, allow connection
        /// from any master (default)
        /// </summary>
        All,
        /// <summary>
        /// Respond to scan requests from whitelist only, allow connection
        /// from any
        /// </summary>
        WhitelistDiscover,
        /// <summary>
        /// Respond to scan requests from any, allow connection from
        /// whitelist only
        /// </summary>
        WhitelistConnect,
        /// <summary>
        /// Respond to scan requests from whitelist only, allow connection
        /// from whitelist only
        /// </summary>
        WhitelistAll,
    }
}
