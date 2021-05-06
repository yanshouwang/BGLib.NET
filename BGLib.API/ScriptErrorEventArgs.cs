namespace BGLib.API
{
    public class ScriptErrorEventArgs : BGErrorEventArgs
    {
        public ScriptErrorEventArgs(ushort address, ushort errorCode)
            : base(errorCode)
        {
            Address = address;
        }

        /// <summary>
        /// Address where failure was detected
        /// </summary>
        public ushort Address { get; }
    }
}