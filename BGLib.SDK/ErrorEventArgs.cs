using System;

namespace BGLib.SDK
{
    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(ushort errorCode)
        {
            ErrorCode = errorCode;
            Message = errorCode.GetMessage();
        }

        public ushort ErrorCode { get; }
        public string Message { get; set; }
    }
}