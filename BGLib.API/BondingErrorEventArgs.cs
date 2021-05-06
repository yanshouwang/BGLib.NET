namespace BGLib.API
{
    public class BondingErrorEventArgs : BGErrorEventArgs
    {
        public BondingErrorEventArgs(byte connection, ushort errorCode)
            : base(errorCode)
        {
            Connection = connection;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
    }
}