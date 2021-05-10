namespace BGLib.Core.System
{
    public class ProtocolErrorEventArgs : ErrorEventArgs
    {
        public ProtocolErrorEventArgs(ushort errorCode)
            : base(errorCode)
        {
        }
    }
}
