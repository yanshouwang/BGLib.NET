namespace BGLib.SDK.System
{
    public class ProtocolErrorEventArgs : ErrorEventArgs
    {
        public ProtocolErrorEventArgs(ushort errorCode)
            : base(errorCode)
        {
        }
    }
}
