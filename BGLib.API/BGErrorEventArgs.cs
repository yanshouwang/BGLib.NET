using System;

namespace BGLib.API
{
    public class BGErrorEventArgs : EventArgs
    {
        public BGErrorEventArgs(ushort errorCode)
        {
            Message = errorCode.GetMessage();
        }

        public string Message { get; set; }
    }
}