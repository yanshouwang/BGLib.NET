namespace BGLib.API
{
    /// <summary>
    /// Specifies the number of stop bits used on the BGSerialPort object.
    /// </summary>
    public enum BGStopBits
    {
        /// <summary>
        /// No stop bits are used.
        /// </summary>
        None = 0,
        /// <summary>
        /// One stop bit is used.
        /// </summary>
        One = 1,
        /// <summary>
        /// Two stop bits are used.
        /// </summary>
        Two = 2,
        /// <summary>
        /// 1.5 stop bits are used.
        /// </summary>
        OnePointFive = 3
    }
}
