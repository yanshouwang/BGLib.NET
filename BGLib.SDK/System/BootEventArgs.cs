using System;

namespace BGLib.SDK.System
{
    public class BootEventArgs : EventArgs
    {
        public BootEventArgs(ushort major, ushort minor, ushort patch, ushort build, ushort llVersion, byte protocolVersion, byte hw)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Build = build;
            LLVersion = llVersion;
            ProtocolVersion = protocolVersion;
            HW = hw;
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
        public ushort LLVersion { get; }
        /// <summary>
        /// BGAPI protocol version
        /// </summary>
        public byte ProtocolVersion { get; }
        /// <summary>
        /// Hardware version
        /// </summary>
        public byte HW { get; }
    }
}