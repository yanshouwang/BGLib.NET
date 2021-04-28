using System;

namespace BGLib.API
{
    internal static class Extensions
    {
        public static byte GetByteLength(this Array array)
        {
            if (array.Length > byte.MaxValue)
            {
                var message = $"Array is too large. The maximum with a BGLib array is {byte.MaxValue}";
                var paramName = nameof(array);
                throw new ArgumentException(message, paramName);
            }
            return (byte)array.Length;
        }
    }
}
