using System;

namespace BGLib.API
{
    /// <summary>
    /// BGAPI packet format
    /// </summary>
    internal class Message
    {
        /// <summary>
        /// Message Type (MT)
        /// </summary>
        public byte Type { get; set; }
        /// <summary>
        /// Class ID (CID)
        /// </summary>
        public byte Class { get; }
        /// <summary>
        /// Command ID (CMD)
        /// </summary>
        public byte Id { get; }
        /// <summary>
        /// Payload (PL)
        /// </summary>
        public byte[] Payload { get; }

        public Message(byte type, byte @class, byte id, byte[] payload)
        {
            if (payload == null)
            {
                payload = Array.Empty<byte>();
            }
            else if (payload.Length > 60)
            {
                var message = "The maximum payload size of `Bluetooth Smart` is 60 bytes.";
                var paramName = nameof(payload);
                throw new ArgumentException(message, paramName);
            }
            Type = type;
            Class = @class;
            Id = id;
            Payload = payload;
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[Payload.Length + 4];
            // Bluetooth Smart is 0x0000 and `LENGTH_HIGH` is always 0x000.
            bytes[0] = (byte)(Type << 7);
            bytes[1] = (byte)Payload.Length;
            bytes[2] = Class;
            bytes[3] = Id;
            Array.Copy(Payload, 0, bytes, 4, Payload.Length);
            return bytes;
        }
    }
}
