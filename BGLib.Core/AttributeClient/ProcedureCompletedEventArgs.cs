namespace BGLib.Core.AttributeClient
{
    public class ProcedureCompletedEventArgs : ErrorEventArgs
    {
        public ProcedureCompletedEventArgs(byte connection, ushort errorCode, ushort chrHandle)
            : base(errorCode)
        {
            Connection = connection;
            ChrHandle = chrHandle;
        }

        /// <summary>
        /// Object Handle
        /// </summary>
        public byte Connection { get; }
        /// <summary>
        /// Characteristic handle at which the event ended
        /// </summary>
        public ushort ChrHandle { get; }
    }
}