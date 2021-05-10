using System;
using System.Runtime.Serialization;

namespace BGLib.Core
{
    [Serializable]
    public class ErrorException : Exception
    {
        public ErrorException(ushort errorCode)
            : base(errorCode.GetMessage()) { }
        public ErrorException(ushort errorCode, Exception inner)
            : base(errorCode.GetMessage(), inner) { }
        protected ErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
