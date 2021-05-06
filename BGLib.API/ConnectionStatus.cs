using System;

namespace BGLib.API
{
    /// <summary>
    /// The possible connection status flags are described in the table below. The flags field is a bit mask, so multiple
    /// flags can be set at a time.If the bit is 1 the flag is active and if the bit is 0 the flag is inactive.
    /// </summary>
    [Flags]
    public enum ConnectionStatus : byte
    {
        /// <summary>
        /// This status flag tells the connection exists to a remote device.
        /// </summary>
        Connected = 1,
        /// <summary>
        /// This flag tells the connection is encrypted.
        /// </summary>
        Encrypted = 2,
        /// <summary>
        /// Connection completed flag, which is used to tell a new connection
        /// has been created.
        /// </summary>
        Completed = 4,
        /// <summary>
        /// This flag tells that connection parameters have changed and. It is
        /// set when connection parameters have changed due to a link layer
        /// operation.
        /// </summary>
        ParametersChanged = 8,
    }
}