using System;

namespace BGLib.API
{
    public class DeviceVersionEventArgs : EventArgs
    {
        public DeviceVersionEventArgs(byte connection, byte version, ushort vendorId, ushort subVersion)
        {
            Connection = connection;
            Version = version;
            VendorId = vendorId;
            SubVersion = subVersion;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Bluetooth controller specification version
        /// </summary>
        public byte Version { get; }
        /// <summary>
        /// Manufacturer of the Bluetooth controller
        /// </summary>
        public ushort VendorId { get; }
        /// <summary>
        /// Bluetooth controller version
        /// </summary>
        public ushort SubVersion { get; }
    }
}