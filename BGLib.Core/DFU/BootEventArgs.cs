using System;

namespace BGLib.Core
{
    public class BootEventArgs : EventArgs
    {
        public BootEventArgs(uint version)
        {
            Version = version;
        }

        public uint Version { get; }
    }
}