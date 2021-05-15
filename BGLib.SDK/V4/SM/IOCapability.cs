namespace BGLib.SDK.V4.SM
{
    /// <summary>
    /// Security Manager I/O Capabilities
    /// </summary>
    public enum IOCapability : byte
    {
        /// <summary>
        /// Display Only
        /// </summary>
        DisplayOnly = 0,
        /// <summary>
        /// Display with Yes/No-buttons
        /// </summary>
        DisplayYesNo = 1,
        /// <summary>
        /// Keyboard Only
        /// </summary>
        KeyboardOnly = 2,
        /// <summary>
        /// No Input and No Output
        /// </summary>
        NoInputNoOutput = 3,
        /// <summary>
        /// Display with Keyboard
        /// </summary>
        KeyboardDisplay = 4,
    }
}