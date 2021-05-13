using System;

namespace BGLib.Core
{
    internal static class CoreX
    {
        public static string GetMessage(this ushort errorCode)
        {
            return Util.GetMessage(errorCode);
        }

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

        public static byte[] ToArray(this Message message)
        {
            var value = new byte[message.Value.Length + 4];
            // Bluetooth Smart is 0x0000 and `LENGTH_HIGH` is always 0x000.
            value[0] = (byte)(message.Type << 7);
            value[1] = (byte)message.Value.Length;
            value[2] = message.Category;
            value[3] = message.Id;
            Array.Copy(message.Value, 0, value, 4, message.Value.Length);
            return value;
        }
    }
}
