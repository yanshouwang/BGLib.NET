namespace BGLib.API
{
    public class Advertisement
    {
        public AdvertisementType Type { get; }
        public byte[] Value { get; }

        public Advertisement(AdvertisementType type, byte[] value)
        {
            Type = type;
            Value = value;
        }
    }
}