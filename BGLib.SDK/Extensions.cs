using System;

namespace BGLib.SDK
{
    internal static class Extensions
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
            var length = message.Value.Length & 0x7FF;
            value[0] = (byte)(message.Type << 7 | message.DeviceType << 3 | length >> 8);
            value[1] = (byte)length;
            value[2] = message.Category;
            value[3] = message.Id;
            Array.Copy(message.Value, 0, value, 4, length);
            return value;
        }
    }
}
