namespace BGLib.API
{
    public class BGCounters
    {
        public BGCounters(byte transmitted, byte retransmitted, byte receivedOK, byte receivedError, byte available)
        {
            Transmitted = transmitted;
            Retransmitted = retransmitted;
            ReceivedOK = receivedOK;
            ReceivedError = receivedError;
            Available = available;
        }

        /// <summary>
        /// Number of transmitted packets
        /// </summary>
        public byte Transmitted { get; }
        /// <summary>
        /// Number of retransmitted packets
        /// </summary>
        public byte Retransmitted { get; }
        /// <summary>
        /// Number of received packets where CRC was OK
        /// </summary>
        public byte ReceivedOK { get; }
        /// <summary>
        /// Number of received packets with CRC error
        /// </summary>
        public byte ReceivedError { get; }
        /// <summary>
        /// Number of available packet buffers
        /// </summary>
        public byte Available { get; }
    }
}