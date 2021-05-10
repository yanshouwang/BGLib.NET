using System;

namespace BGLib.Core
{
    /// <summary>
    /// BGAPI packet format
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Message Type (MT)
        /// </summary>
        public byte Type { get; set; }
        /// <summary>
        /// Class ID (CID)
        /// </summary>
        public byte Category { get; }
        /// <summary>
        /// Command ID (CMD)
        /// </summary>
        public byte Id { get; }
        /// <summary>
        /// Payload (PL)
        /// </summary>
        public byte[] Value { get; }

        public Message(byte type, byte category, byte id, byte[] value = null)
        {
            Type = type;
            Category = category;
            Id = id;
            Value = value ?? Array.Empty<byte>();
        }
    }
}
