using System;

namespace BGLib.SDK
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