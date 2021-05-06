using System;

namespace BGLib.API
{
    public class GroupEventArgs : EventArgs
    {
        public GroupEventArgs(byte connection, ushort start, ushort end, byte[] uuid)
        {
            Connection = connection;
            Start = start;
            End = end;
            UUID = uuid;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Starting handle
        /// </summary>
        public ushort Start { get; }
        /// <summary>
        /// Ending handle
        /// </summary>
        public ushort End { get; }
        /// <summary>
        /// <para>UUID of a service</para>
        /// <para>Length is 0 if no services are found.</para>
        /// </summary>
        public byte[] UUID { get; }
    }
}