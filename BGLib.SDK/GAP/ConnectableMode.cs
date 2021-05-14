namespace BGLib.SDK.GAP
{
    /// <summary>
    /// GAP connectable modes
    /// </summary>
    public enum ConnectableMode : byte
    {
        /// <summary>
        /// Not connectable
        /// </summary>
        NonConnectable = 0,
        /// <summary>
        /// Directed Connectable
        /// </summary>
        DirectedConnectable = 1,
        /// <summary>
        /// Undirected connectable
        /// </summary>
        UndirectedConnectable = 2,
        /// <summary>
        /// Same as non-connectable, but also supports ADV_SCAN_IND
        /// packets. Device accepts scan requests (active scanning) but is
        /// not connectable.
        /// </summary>
        ScannableNonConnectable = 3,
    }
}