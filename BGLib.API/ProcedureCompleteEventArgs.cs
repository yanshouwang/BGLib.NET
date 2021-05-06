using System;

namespace BGLib.API
{
    public class ProcedureCompleteEventArgs : EventArgs
    {
        public ProcedureCompleteEventArgs(byte connection, ushort errorCode, ushort characteristic)
        {
            Connection = connection;
            ErrorCode = errorCode;
            Characteristic = characteristic;
        }

        /// <summary>
        /// Object Handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// <para>0: The operation was successful</para>
        /// <para>Otherwise: attribute protocol error code returned by remote device</para>
        /// </summary>
        public ushort ErrorCode { get; }
        /// <summary>
        /// Characteristic handle at which the event ended
        /// </summary>
        public ushort Characteristic { get; }
    }
}