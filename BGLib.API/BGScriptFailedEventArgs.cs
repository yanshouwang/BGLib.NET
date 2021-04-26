using System;

namespace BGLib.API
{
    public class BGScriptFailureEventArgs : EventArgs
    {
        public BGScriptFailureEventArgs(ushort address, ushort errorCode)
        {
            Address = address;
            ErrorCode = errorCode;
        }

        public ushort Address { get; }
        public ushort ErrorCode { get; }
    }
}