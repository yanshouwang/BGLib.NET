namespace BGLib.Core.GAP
{
    /// <summary>
    /// GAP discoverable modes
    /// </summary>
    public enum DiscoverableMode : byte
    {
        /// <summary>
        /// Non-discoverable mode: the LE Limited Discoverable Mode and the
        /// LE General Discoverable Mode bits are NOT set in the Flags AD
        /// type. A master can still connect to the advertising slave in this mode.
        /// </summary>
        NoneDiscoverable = 0,
        /// <summary>
        /// Discoverable using limited scanning mode: the advertisement
        /// packets will carry the LE Limited Discoverable Mode bit set in the
        /// Flags AD type.
        /// </summary>
        LimitedDiscoverable = 1,
        /// <summary>
        /// Discoverable using general scanning mode: the advertisement
        /// packets will carry the LE General Discoverable Mode bit set in the
        /// Flags AD type.
        /// </summary>
        GeneralDiscoverable = 2,
        /// <summary>
        /// Same as <see cref="NoneDiscoverable"/> above.
        /// </summary>
        Broadcast = 3,
        /// <summary>
        /// In this advertisement the advertisement and scan response data
        /// defined by user will be used. The user is responsible of building the
        /// advertisement data so that it also contains the appropriate desired
        /// Flags AD type.
        /// </summary>
        UserData = 4,
        /// <summary>
        /// When turning the most highest bit on in GAP discoverable mode, the
        /// remote devices that send scan request packets to the advertiser are
        /// reported back to the application through Scan Response event.
        /// This is so called Enhanced Broadcasting mode.
        /// </summary>
        EnhancedBroadcasting = 0x80,
    }
}