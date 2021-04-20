using System;

namespace BGLib.API
{
    [Serializable]
    public class BGException : Exception
    {
        public BGException() { }
        public BGException(string message) : base(message) { }
        public BGException(string message, Exception inner) : base(message, inner) { }
        protected BGException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
