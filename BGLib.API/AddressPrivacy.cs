namespace BGLib.API
{
    public enum AddressPrivacy : byte
    {
        /// <summary>
        /// Disable privacy
        /// </summary>
        Disable = 0,
        /// <summary>
        /// Enable privacy
        /// </summary>
        Enable = 1,
        /// <summary>
        /// Change private address on demand
        /// </summary>
        ChangeAddressOnDemand = 2,
        /// <summary>
        /// Enable privacy with non-resolvable address.
        /// </summary>
        EnableWithNonResolvableAddress = 3,
    }
}