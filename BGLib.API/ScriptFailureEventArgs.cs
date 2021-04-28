using System;

namespace BGLib.API
{
    public class ScriptFailureEventArgs : EventArgs
    {
        public ScriptFailureEventArgs(ushort address, ushort errorCode)
        {
            Address = address;
            ErrorCode = errorCode;
        }

        public ushort Address { get; }
        public ushort ErrorCode { get; }
    }
}