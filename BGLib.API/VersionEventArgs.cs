using System;

namespace BGLib.API
{
    public class VersionEventArgs : EventArgs
    {
        public Version Version { get; }

        public VersionEventArgs(Version version)
        {
            Version = version;
        }
    }
}