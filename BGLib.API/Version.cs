namespace BGLib.API
{
    public class Version
    {
        public Version(ushort major, ushort minor, ushort patch, ushort build, ushort linkLayer, byte protocol, byte hardware)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Build = build;
            LinkLayer = linkLayer;
            Protocol = protocol;
            Hardware = hardware;
        }

        /// <summary>
        /// Major software version
        /// </summary>
        public ushort Major { get; }
        /// <summary>
        /// Minor software version
        /// </summary>
        public ushort Minor { get; }
        /// <summary>
        /// Patch ID
        /// </summary>
        public ushort Patch { get; }
        /// <summary>
        /// Build version
        /// </summary>
        public ushort Build { get; }
        /// <summary>
        /// Link layer version
        /// </summary>
        public ushort LinkLayer { get; }
        /// <summary>
        /// BGAPI protocol version
        /// </summary>
        public byte Protocol { get; }
        /// <summary>
        /// Hardware version
        /// </summary>
        public byte Hardware { get; }
    }
}