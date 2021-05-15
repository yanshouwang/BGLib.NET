using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BGLib.Wand
{
    public struct MAC
    {
        private readonly byte _a;
        private readonly byte _b;
        private readonly byte _c;
        private readonly byte _d;
        private readonly byte _e;
        private readonly byte _f;

        public MAC(string value)
        {
            var pattern = @"^[0-9A-F]{2}(:[0-9A-F]{2}){5}$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            if (!regex.IsMatch(value))
            {
                throw new ArgumentException();
            }
            var items = value.Split(':').Select(i => byte.Parse(i)).ToArray();
            _a = items[0];
            _b = items[1];
            _c = items[2];
            _d = items[3];
            _e = items[4];
            _f = items[5];
        }

        public MAC(byte[] value)
        {
            if (value == null || value.Length != 6)
            {
                throw new ArgumentException();
            }
            _a = value[5];
            _b = value[4];
            _c = value[3];
            _d = value[2];
            _e = value[1];
            _f = value[0];
        }

        public byte[] ToArray()
        {
            return new[] { _f, _e, _d, _c, _b, _a };
        }

        public override string ToString()
        {
            return $"{_a:X2}:{_b:X2}:{_c:X2}:{_d:X2}:{_e:X2}:{_f:X2}";
        }

        public override bool Equals(object obj)
        {
            return obj is MAC mac &&
                   mac._a == _a &&
                   mac._b == _b &&
                   mac._c == _c &&
                   mac._d == _d &&
                   mac._e == _e &&
                   mac._f == _f;
        }

        public override int GetHashCode()
        {
            return _a.GetHashCode() ^
                   _b.GetHashCode() ^
                   _c.GetHashCode() ^
                   _d.GetHashCode() ^
                   _e.GetHashCode() ^
                   _f.GetHashCode();
        }

        public static bool operator ==(MAC a, MAC b) => Equals(a, b);

        public static bool operator !=(MAC a, MAC b) => !Equals(a, b);
    }
}
