using System;

namespace BGLib.SDK.V4
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