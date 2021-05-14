using System;

namespace BGLib.SDK.AttributeDatabase
{
    public class StatusEventArgs : EventArgs
    {
        public StatusEventArgs(ushort handle, AttributeStatus flags)
        {
            Handle = handle;
            Flags = flags;
        }

        /// <summary>
        /// Attribute handle
        /// </summary>
        public ushort Handle { get; set; }
        /// <summary>
        /// Attribute status flags
        /// </summary>
        public AttributeStatus Flags { get; }
    }
}