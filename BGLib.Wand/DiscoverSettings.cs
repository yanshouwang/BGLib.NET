namespace BGLib.Wand
{
    public class DiscoverSettings
    {
        public DiscoverSettings(
            byte interval = 0x4B,
            byte window = 0x32,
            bool active = false)
        {
            Interval = interval;
            Window = window;
            Active = active;
        }

        public byte Interval { get; }
        public byte Window { get; }
        public bool Active { get; }
    }
}
