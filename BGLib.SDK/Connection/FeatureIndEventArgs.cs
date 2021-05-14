using System;

namespace BGLib.SDK.Connection
{
    public class FeatureIndEventArgs : EventArgs
    {
        public FeatureIndEventArgs(byte connection, byte[] features)
        {
            Connection = connection;
            Features = features;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// CtrData field from LL_FEATURE_RSP - packet
        /// </summary>
        public byte[] Features { get; }
    }
}