namespace BGLib.SDK.V4.AttributeDatabase
{
    /// <summary>
    /// This enumeration contains the reason for an attribute value change.
    /// </summary>
    public enum AttributeChangeReason : byte
    {
        /// <summary>
        /// Value was written by remote device using write request
        /// </summary>
        WriteRequest = 0,
        /// <summary>
        /// Value was written by remote device using write command
        /// </summary>
        WriteCommand = 1,
        /// <summary>
        /// <para>
        /// Local attribute value was written by the remote device, but the
        /// Bluetooth Low Energy stack is waiting for the write to be confirmed
        /// by the application.
        /// </para>
        /// <para>
        /// User Write Response command should be used to send the confirmation.
        /// </para>
        /// <para>
        /// For this reason to appear the attribute in the GATT database must have
        /// the user property enabled.
        /// </para>
        /// <para>
        /// See Profile Toolkit Developer Guide for more information how to enable
        /// the user property for an attribute.
        /// </para>
        /// </summary>
        WriteRequestUser = 2,
    }
}