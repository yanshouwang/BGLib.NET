using System;

namespace BGLib.SDK.Connection
{
    public class VersionIndEventArgs : EventArgs
    {
        public VersionIndEventArgs(byte connection, byte versNr, ushort compId, ushort subVersNr)
        {
            Connection = connection;
            VersNr = versNr;
            CompId = compId;
            SubVersNr = subVersNr;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Bluetooth controller specification version
        /// </summary>
        public byte VersNr { get; }
        /// <summary>
        /// Manufacturer of the Bluetooth controller
        /// </summary>
        public ushort CompId { get; }
        /// <summary>
        /// Bluetooth controller version
        /// </summary>
        public ushort SubVersNr { get; }
    }
}