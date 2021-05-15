using System;
using System.Threading.Tasks;

namespace BGLib.SDK.V4.Testing
{
    /// <summary>
    /// The Testing API provides access to functions which can be used to put the local device into a test mode
    /// required for Bluetooth conformance testing.
    /// </summary>
    public class MessageWorker : SDK.MessageWorker
    {
        internal MessageWorker(MessageHub messageHub)
            : base(0x08, messageHub)
        {
        }

        protected override void OnEventAnalyzed(byte id, byte[] eventValue)
        {
            // Testing doesn't have an event.
        }

        #region Commands

        /// <summary>
        /// <para>
        /// This command start PHY packet transmission and the radio starts to send one packet at every 625us. If a
        /// carrier wave is specified as type then the radio just broadcasts continuous carrier wave.
        /// </para>
        /// <para>Sleep mode shall be disabled for BLE121LR-m256k module due to hardware limitation.</para>
        /// </summary>
        /// <param name="channel">
        /// <para>RF channel to use</para>
        /// <para>Values: 0x00 - 0x27</para>
        /// <para>channel is (Frequency-2402)/2</para>
        /// <para>Frequency Range 2402 MHz to 2480 MHz</para>
        /// </param>
        /// <param name="length">
        /// <para>Payload data length as octetes</para>
        /// <para>Values: 0x00 - 0x25</para>
        /// </param>
        /// <param name="type">
        /// <para>Packet Payload data contents</para>
        /// <para>0: PRBS9 pseudo-random data</para>
        /// <para>1: 11110000 sequence</para>
        /// <para>2: 10101010 sequence</para>
        /// <para>3: broadcast carrier wave</para>
        /// </param>
        /// <returns></returns>
        public async Task PhyTXAsync(byte channel, byte length, byte type)
        {
            var commandValue = new[] { channel, length, type };
            await WriteAsync(0x00, commandValue);
        }

        /// <summary>
        /// This commands starts a PHY receive test. Valid packets received can be read by Phy End command.
        /// </summary>
        /// <param name="channel">
        /// <para>Bluetooth channel to use</para>
        /// <para>Values: 0x00 - 0x27</para>
        /// <para>Channel is (Frequency-2402)/2</para>
        /// <para>Frequency Range 2402 MHz to 2480 MHz</para>
        /// <para>Examples:</para>
        /// <para>0x00: 2402MHz</para>
        /// <para>0x13: 2441MHz</para>
        /// <para>0x27: 2480MHz</para>
        /// </param>
        /// <returns></returns>
        public async Task PhyRXAsync(byte channel)
        {
            var commandValue = new[] { channel };
            await WriteAsync(0x01, commandValue);
        }

        /// <summary>
        /// <para>This command ends a PHY test and report received packets.</para>
        /// <para>PHY - testing commands implement Direct test mode from Bluetooth Core Specification, Volume 6, Part F.</para>
        /// <para>These commands are meant to be used when testing against separate Bluetooth tester.</para>
        /// </summary>
        /// <returns>Received packet counter</returns>
        public async Task<ushort> PhyEndAsync()
        {
            var responseValue = await WriteAsync(0x02);
            var counter = BitConverter.ToUInt16(responseValue, 0);
            return counter;
        }

        /// <summary>
        /// This command can be used to read the Channel Quality Map. Channel Quality Map is cleared after the
        /// response to this command is sent.Measurements are entered into the Channel Quality Map as packets are
        /// received over the different channels during a normal connection.
        /// </summary>
        /// <returns>
        /// <para>Channel quality map measurements.</para>
        /// <para>
        /// The 37 bytes reported by this response, one per each channel, carry the
        /// information defined via the Channel Mode configuration command.
        /// </para>
        /// </returns>
        public async Task<byte[]> GetChannelMapAsync()
        {
            var responseValue = await WriteAsync(0x04);
            var channelMapLength = responseValue[0];
            var channelMap = new byte[channelMapLength];
            Array.Copy(responseValue, 1, channelMap, 0, channelMap.Length);
            return channelMap;
        }

        /// <summary>
        /// Set channel quality measurement mode. This command defines the kind of information reported by the
        /// response to the command Get Channel Map.
        /// </summary>
        /// <param name="mode">
        /// <para>0: RSSI of next packet sent on channel after Get Channel Map is issued</para>
        /// <para>
        /// When this mode is selected, the command Get Channel Map must be issued while
        /// a connection exists.Response will be ready when packets have been sent on all
        /// the 37 channels.Returned value minus an offset of 103 will give the approximate
        /// RSSI in dBm.
        /// </para>
        /// <para>1: Accumulate error counter</para>
        /// <para>
        /// When this mode is selected, the command Get Channel Map must be issued while
        /// a connection exists.After the command is issued the counter will be reset.
        /// </para>
        /// <para>2: Fast channel Sweep</para>
        /// <para>
        /// When this mode is selected, the command Get Channel Map must be issued while
        /// no connection exists.Returned value is of the same kind as in mode 0, but refers to
        /// the measured background noise.
        /// </para>
        /// </param>
        /// <returns></returns>
        public async Task ChannelModeAsync(byte mode)
        {
            var commandValue = new[] { mode };
            await WriteAsync(0x06, commandValue);
        }

        #endregion
    }
}
