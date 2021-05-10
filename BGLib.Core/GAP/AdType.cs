namespace BGLib.Core.GAP
{
    public enum AdType : byte
    {
        None = 0,
        Flags = 1,
        Services16BitMor = 2,
        Services16BitAll = 3,
        Services32BitMore = 4,
        Services32BitAll = 5,
        Services128BitMore = 6,
        Services128BitAll = 7,
        LocalnameShort = 8,
        LocalnameComplete = 9,
        Txpower = 10,
    }
}
