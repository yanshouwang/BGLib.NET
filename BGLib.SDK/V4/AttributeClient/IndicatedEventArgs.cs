using System;

namespace BGLib.SDK.V4.AttributeClient
{
    public class IndicatedEventArgs : EventArgs
    {
        public IndicatedEventArgs(byte connection, ushort attrHandle)
        {
            Connection = connection;
            AttrHandle = attrHandle;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Attribute handle
        /// </summary>
        public ushort AttrHandle { get; }
    }
}