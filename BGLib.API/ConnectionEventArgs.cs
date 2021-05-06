using System;

namespace BGLib.API
{
    public class ConnectionEventArgs : EventArgs
    {
        public ConnectionEventArgs(byte connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// Connection handle
        /// </summary>
        public byte Connection { get; }
    }
}