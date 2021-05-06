using System;

namespace BGLib.API
{
    public class AttributeStatusEventArgs : EventArgs
    {
        public AttributeStatusEventArgs(ushort attribute, AttributeStatus status)
        {
            Attribute = attribute;
            Status = status;
        }

        /// <summary>
        /// Attribute handle
        /// </summary>
        public ushort Attribute { get; set; }
        /// <summary>
        /// Attribute status flags
        /// </summary>
        public AttributeStatus Status { get; }
    }
}