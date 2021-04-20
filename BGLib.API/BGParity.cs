namespace BGLib.API
{
    /// <summary>
    /// Specifies the parity bit for a BGSerialPort object.
    /// </summary>
    public enum BGParity
    {
        /// <summary>
        /// No parity check occurs.
        /// </summary>
        None = 0,
        /// <summary>
        /// Sets the parity bit so that the count of bits set is an odd number.
        /// </summary>
        Odd = 1,
        /// <summary>
        /// Sets the parity bit so that the count of bits set is an even number.
        /// </summary>
        Even = 2,
        /// <summary>
        /// Leaves the parity bit set to 1.
        /// </summary>
        Mark = 3,
        /// <summary>
        /// Leaves the parity bit set to 0.
        /// </summary>
        Space = 4
    }
}
