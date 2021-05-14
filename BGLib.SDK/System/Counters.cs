namespace BGLib.SDK.System
{
    public class Counters
    {
        public Counters(byte txOK, byte txRetry, byte rxOK, byte rxFail, byte mbuf)
        {
            TXOK = txOK;
            TXRetry = txRetry;
            RXOK = rxOK;
            RXFail = rxFail;
            MBuf = mbuf;
        }

        /// <summary>
        /// Number of transmitted packets
        /// </summary>
        public byte TXOK { get; }
        /// <summary>
        /// Number of retransmitted packets
        /// </summary>
        public byte TXRetry { get; }
        /// <summary>
        /// Number of received packets where CRC was OK
        /// </summary>
        public byte RXOK { get; }
        /// <summary>
        /// Number of received packets with CRC error
        /// </summary>
        public byte RXFail { get; }
        /// <summary>
        /// Number of available packet buffers
        /// </summary>
        public byte MBuf { get; }
    }
}