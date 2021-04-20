namespace BGLib.API
{
    public class BGDiscoveryEventArgs
    {
        public BGDiscovery Discovery { get; }

        public BGDiscoveryEventArgs(BGDiscovery discovery)
        {
            Discovery = discovery;
        }
    }
}