namespace BGLib.API
{
    /// <summary>
    /// BGAPI message types
    /// </summary>
    internal enum MessageType : byte
    {
        /// <summary>
        /// Command from host to the stack
        /// </summary>
        Command = 0x00,
        /// <summary>
        /// Response from stack to the host
        /// </summary>
        Response = Command,
        /// <summary>
        /// Event from stack to the host
        /// </summary>
        Event = 0x01,
    }
}
