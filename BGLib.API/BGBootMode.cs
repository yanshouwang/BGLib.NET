namespace BGLib.API
{
    /// <summary>
    /// Boot mode
    /// </summary>
    public enum BGBootMode : byte
    {
        /// <summary>
        /// boot to main program
        /// </summary>
        Main = 0,
        /// <summary>
        /// boot to DFU
        /// </summary>
        DFU = 1,
    }
}