using System;

namespace BGLib.API
{
    public class BGErrorEventArgs : EventArgs
    {
        public BGErrorEventArgs(ushort errorCode)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Reason for failure
        /// </summary>
        public ushort ErrorCode { get; }
    }
}