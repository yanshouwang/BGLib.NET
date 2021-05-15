namespace BGLib.Wand
{
    /// <summary>
    /// GAP Discover modes
    /// </summary>
    public enum DiscoverMode : byte
    {
        /// <summary>
        /// Discover only limited discoverable devices, that is, Slaves which have
        /// the LE Limited Discoverable Mode bit set in the Flags AD type of their
        /// advertisement packets.
        /// </summary>
        Limited,
        /// <summary>
        /// Discover limited and generic discoverable devices, that is, Slaves which
        /// have the LE Limited Discoverable Mode or the LE General
        /// Discoverable Mode bit set in the Flags AD type of their advertisement
        /// packets.
        /// </summary>
        Generic,
        /// <summary>
        /// Discover all devices regardless of the Flags AD type, so also devices in
        /// non-discoverable mode will be reported to host.
        /// </summary>
        Observation,
    }
}
