namespace BGLib.SDK.V4.AttributeClient
{
    /// <summary>
    /// These enumerations are in the Attribute Client class
    /// </summary>
    public enum AttributeValueType : byte
    {
        /// <summary>
        /// Value was read
        /// </summary>
        Read = 0,
        /// <summary>
        /// Value was notified
        /// </summary>
        Notify = 1,
        /// <summary>
        /// Value was indicated
        /// </summary>
        Indicate = 2,
        /// <summary>
        /// Value was read
        /// </summary>
        ReadByType = 3,
        /// <summary>
        /// Value was part of a long attribute
        /// </summary>
        ReadBlob = 4,
        /// <summary>
        /// <para>
        /// Value was indicated and the remote device is
        /// waiting for a confirmation.
        /// </para>
        /// <para>
        /// Indicate Confirm command can be used to send a
        /// confirmation.
        /// </para>
        /// </summary>
        IndicateRspReq = 5,
    }
}