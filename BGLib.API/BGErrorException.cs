using System;

namespace BGLib.API
{
    [Serializable]
    public class BGErrorException : Exception
    {
        public BGErrorException(ushort errorCode)
            : base(errorCode.GetMessage()) { }
        public BGErrorException(ushort errorCode, Exception inner)
            : base(errorCode.GetMessage(), inner) { }
        protected BGErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
