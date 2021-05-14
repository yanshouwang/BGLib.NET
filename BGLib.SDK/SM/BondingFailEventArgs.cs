namespace BGLib.SDK.SM
{
    public class BondingFailEventArgs : ErrorEventArgs
    {
        public BondingFailEventArgs(byte handle, ushort errorCode)
            : base(errorCode)
        {
            Handle = handle;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Handle { get; }
    }
}