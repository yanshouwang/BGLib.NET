namespace BGLib.Core.System
{
    public class ScriptFailureEventArgs : ErrorEventArgs
    {
        public ScriptFailureEventArgs(ushort address, ushort errorCode)
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