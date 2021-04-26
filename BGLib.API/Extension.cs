using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BGLib.API
{
    internal static class Extension
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
