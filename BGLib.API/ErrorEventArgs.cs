using System;

namespace BGLib.API
{
    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(ushort errorCode)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Reason for failure
        /// </summary>
        public ushort ErrorCode { get; }
    }
}