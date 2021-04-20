namespace BGLib.API
{
    public class BGAdvertisement
    {
        public BGAdvertisementType Type { get; }
        public byte[] Value { get; }

        public BGAdvertisement(BGAdvertisementType type, byte[] value)
        {
            Type = type;
            Value = value;
        }
    }
}