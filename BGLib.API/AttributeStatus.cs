namespace BGLib.API
{
    /// <summary>
    /// Attribute status flags
    /// </summary>
    public enum AttributeStatus : byte
    {
        /// <summary>
        /// Notifications are enabled
        /// </summary>
        Notify = 1,
        /// <summary>
        ///  Indications are enabled
        /// </summary>
        Indicate = 2,
    }
}