using System;

namespace BGLib.Wand
{
    public class GattService
    {
        public GattService(byte connection, ushort start, ushort end, Guid uuid)
        {
            Connection = connection;
            Start = start;
            End = end;
            UUID = uuid;
        }

        internal byte Connection { get; }
        internal ushort Start { get; }
        internal ushort End { get; }

        public Guid UUID { get; }

        public override string ToString()
        {
            return $"{UUID}";
        }
    }
}
