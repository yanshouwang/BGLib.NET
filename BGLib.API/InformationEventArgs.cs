using System;

namespace BGLib.API
{
    public class InformationEventArgs : EventArgs
    {
        public InformationEventArgs(byte connection, ushort characteristic, byte[] uuid)
        {
            Connection = connection;
            Characteristic = characteristic;
            UUID = uuid;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Characteristics handle
        /// </summary>
        public ushort Characteristic { get; }
        /// <summary>
        /// Characteristics type (UUID)
        /// </summary>
        public byte[] UUID { get; }
    }
}