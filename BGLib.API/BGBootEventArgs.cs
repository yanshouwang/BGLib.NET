using System;

namespace BGLib.API
{
    public class BGVersionEventArgs : EventArgs
    {
        public BGVersion Version { get; }

        public BGVersionEventArgs(BGVersion version)
        {
            Version = version;
        }
    }
}