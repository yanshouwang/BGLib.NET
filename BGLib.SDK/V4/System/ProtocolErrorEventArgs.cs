namespace BGLib.SDK.V4.System
{
    public class ProtocolErrorEventArgs : ErrorEventArgs
    {
        public ProtocolErrorEventArgs(ushort errorCode)
            : base(errorCode)
        {
        }
    }
}
