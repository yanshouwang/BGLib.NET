using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BGLib.LowEnergy
{
    internal static class ArrayX
    {
        public static Guid ToGuid(this byte[] value)
        {
            uint a;
            ushort b, c;
            byte d, e, f, g, h, i, j, k;
            if (value.Length == 2)
            {
                a = BitConverter.ToUInt16(value, 0);
                b = 0x0000;
                c = 0x1000;
                d = 0x80;
                e = 0x00;
                f = 0x00;
                g = 0x80;
                h = 0x5F;
                i = 0x9B;
                j = 0x34;
                k = 0xFB;
            }
            else
            {
                a = BitConverter.ToUInt32(value, 12);
                b = BitConverter.ToUInt16(value, 10);
                c = BitConverter.ToUInt16(value, 8);
                d = value[7];
                e = value[6];
                f = value[5];
                g = value[4];
                h = value[3];
                i = value[2];
                j = value[1];
                k = value[0];
            }
            return new Guid(a, b, c, d, e, f, g, h, i, j, k);
        }
    }
}
