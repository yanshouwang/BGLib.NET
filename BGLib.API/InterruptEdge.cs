namespace BGLib.API
{
    /// <summary>
    /// Interrupt sense for port.
    /// </summary>
    public enum InterruptEdge : byte
    {
        /// <summary>
        /// rising edge
        /// </summary>
        Rising = 0,
        /// <summary>
        /// falling edge
        /// </summary>
        Falling = 1,
    }
}