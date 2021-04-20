namespace BGLib.API
{
    /// <summary>
    /// BGAPI command classes
    /// </summary>
    internal enum MessageClass : byte
    {
        /// <summary>
        /// Provides access to system functions
        /// </summary>
        System = 0x00,
        /// <summary>
        /// Provides access the persistence store (parameters)
        /// </summary>
        PersistentStore = 0x01,
        /// <summary>
        /// Provides access to local GATT database
        /// </summary>
        AttributeDatabase = 0x02,
        /// <summary>
        /// Provides access to connection management functions
        /// </summary>
        Connection = 0x03,
        /// <summary>
        /// Functions to access remote devices GATT database
        /// </summary>
        AttributeClient = 0x04,
        /// <summary>
        /// Bluetooth low energy security functions
        /// </summary>
        SecurityManager = 0x05,
        /// <summary>
        /// GAP functions
        /// </summary>
        GenericAccessProfile = 0x06,
        /// <summary>
        /// Provides access to hardware such as timers and ADC
        /// </summary>
        Hardware = 0x07,
        Testing = 0x08,
        DeviceFirmwareUpgrade = 0x09,
    }
}
