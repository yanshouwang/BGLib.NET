using System;

namespace BGLib.SDK
{
    /// <summary>
    /// BGAPI Message
    /// </summary>
    public class Message
    {
        /// <summary>
        /// <para>Type</para>
        /// <para>0x00: The message is a command or a response to command.</para>
        /// <para>0x01: The message is an event.</para>
        /// </summary>
        public byte Type { get; set; }
        /// <summary>
        /// <para>Device type</para>
        /// <para>0x00: Bluetooth Low Energy</para>
        /// <para>0x01: WiFi</para>
        /// <para>0x04: Bluetooth LE</para>
        /// <para>0x05: Bluetooth Mesh</para>
        /// </summary>
        public byte DeviceType { get; }
        /// <summary>
        /// Category
        /// </summary>
        public byte Category { get; }
        /// <summary>
        /// Id
        /// </summary>
        public byte Id { get; }
        /// <summary>
        /// Value
        /// </summary>
        public byte[] Value { get; }

        public Message(byte type, byte deviceType, byte category, byte id, byte[] value = null)
        {
            Type = type;
            DeviceType = deviceType;
            Category = category;
            Id = id;
            Value = value ?? Array.Empty<byte>();
        }
    }
}
