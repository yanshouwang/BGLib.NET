namespace BGLib.API
{
    /// <summary>
    /// BGAPI command classes
    /// </summary>
    internal enum MessageClass : byte
    {
        /// <summary>
        /// The System class provides access to the local device and contains functions for example to query the local
        /// Bluetooth address, read firmware version, read radio packet counters etc.
        /// </summary>
        System = 0x00,
        /// <summary>
        /// The Persistent Store (PS) class provides methods to read write and dump the local devices parameters (PS
        /// keys). The persistent store is an abstract data storage on the local devices flash where an application can store
        /// data for future use.
        /// </summary>
        PS = 0x01,
        /// <summary>
        /// The Attribute Database class provides methods to read and write attributes to the local devices Attribute
        /// Database. This class is usually only needed on sensor devices(Attribute server) for example to update
        /// attribute values to the local database based on the sensor readings.A remote device then can access the
        /// GATT database and these values over a Bluetooth connection.
        /// </summary>
        AttributeDatabase = 0x02,
        /// <summary>
        /// The Connection class provides methods to manage Bluetooth connections and query their statuses.
        /// </summary>
        Connection = 0x03,
        /// <summary>
        /// The Attribute Client class implements the Bluetooth Low Energy Attribute Protocol (ATT) and provides access
        /// to the ATT protocol methods. The Attribute Client class can be used to discover services and characteristics
        /// from the ATT server, read and write values and manage indications and notifications.
        /// </summary>
        AttributeClient = 0x04,
        /// <summary>
        /// The Security Manager (SM) class provides access to the Bluetooth low energy Security Manager and methods
        /// such as : bonding management and modes and encryption control.
        /// </summary>
        SM = 0x05,
        /// <summary>
        /// The Generic Access Profile (GAP) class provides methods to control the Bluetooth GAP level functionality of
        /// the local device. The GAP call for example allows remote device discovery, connection establishment and local
        /// devices connection and discovery modes. The GAP class also allows the control of local devices privacy
        /// modes.
        /// </summary>
        GAP = 0x06,
        /// <summary>
        /// The Hardware class provides methods to access the local devices hardware interfaces such as : A/D
        /// converters, IO and timers, I2C interface etc.
        /// </summary>
        Hardware = 0x07,
        /// <summary>
        /// The Testing API provides access to functions which can be used to put the local device into a test mode
        /// required for Bluetooth conformance testing.
        /// </summary>
        Testing = 0x08,
        /// <summary>
        /// <para>
        /// The commands and events in the DFU (Device firmware upgrade) can be used to perform a firmware upgrade
        /// to the local device for example over the UART interface.
        /// </para>
        /// <para>
        /// The commands in this class are only available when the module has been booted into DFU mode with the reset
        /// command.
        /// </para>
        /// <para>
        /// It is not possible to use other commands in DFU mode, bootloader can't parse commands not related with DFU.
        /// </para>
        /// </summary>
        DFU = 0x09,
    }
}
