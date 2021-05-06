using System;

namespace BGLib.API
{
    public class DfuVersionEventArgs : EventArgs
    {
        public uint Version { get; }

        public DfuVersionEventArgs(uint version)
        {
            Version = version;
        }
    }
}