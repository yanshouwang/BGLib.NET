namespace BGLib.API
{
    public class DiscoveryEventArgs
    {
        public Discovery Discovery { get; }

        public DiscoveryEventArgs(Discovery discovery)
        {
            Discovery = discovery;
        }
    }
}