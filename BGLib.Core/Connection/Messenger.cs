using BGLib.Core.GAP;
using System;
using System.Threading.Tasks;

namespace BGLib.Core.Connection
{
    /// <summary>
    /// The Connection class provides methods to manage Bluetooth connections and query their statuses.
    /// </summary>
    public class Messenger : BaseMessenger
    {
        internal Messenger(MessageHub messageHub)
            : base(messageHub)
        {
        }

        protected override byte Category => 0x03;

        protected override void OnEventAnalyzed(byte id, byte[] eventValue)
        {
            switch (id)
            {
                case 0x00:
                    {
                        var connection = eventValue[0];
                        var flags = (ConnectionStatus)eventValue[1];
                        var address = new byte[6];
                        Array.Copy(eventValue, 2, address, 0, 6);
                        var addressType = (AddressType)eventValue[8];
                        var connInterval = BitConverter.ToUInt16(eventValue, 9);
                        var timeout = BitConverter.ToUInt16(eventValue, 11);
                        var latency = BitConverter.ToUInt16(eventValue, 13);
                        var bonding = eventValue[15];
                        var eventArgs = new StatusEventArgs(connection, flags, address, addressType, connInterval, timeout, latency, bonding);
                        Status?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x01:
                    {
                        var connection = eventValue[0];
                        var versNr = eventValue[1];
                        var compId = BitConverter.ToUInt16(eventValue, 2);
                        var subVersNr = BitConverter.ToUInt16(eventValue, 4);
                        var eventArgs = new VersionIndEventArgs(connection, versNr, compId, subVersNr);
                        VersionInd?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x02:
                    {
                        var connection = eventValue[0];
                        var featuresLength = eventValue[1];
                        var features = new byte[featuresLength];
                        Array.Copy(eventValue, 2, features, 0, features.Length);
                        var eventArgs = new FeatureIndEventArgs(connection, features);
                        FeatureInd.Invoke(this, eventArgs);
                        break;
                    }
                case 0x04:
                    {
                        var connection = eventValue[0];
                        var reason = BitConverter.ToUInt16(eventValue, 1);
                        var eventArgs = new DisconnectedEventArgs(connection, reason);
                        Disconnected?.Invoke(this, eventArgs);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        #region Commands

        /// <summary>
        /// <para>This command disconnects an active Bluetooth connection.</para>
        /// <para>When link is disconnected a Disconnected event is produced.</para>
        /// </summary>
        /// <param name="connection">Connection handle to close</param>
        /// <returns></returns>
        public async Task DisconnectAsync(byte connection)
        {
            var commandValue = new[] { connection };
            var responseValue = await WriteAsync(0x00, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// This command returns the Receiver Signal Strength Indication (RSSI) related to the connection referred to by
        /// the connection handle parameter.If the connection is not open, then the RSSI value returned in the response
        /// packet will be 0x00, while if the connection is active, then it will be some negative value (2's complement form
        /// between 0x80 and 0xFF and never 0x00). Note that this command also returns an RSSI of 0x7F if you request
        /// RSSI on an invalid/unsupported handle.
        /// </para>
        /// <para>
        /// At -38 dBm the BLE112 receiver is saturated. The measurement value may depend on the used
        /// hardware and design.
        /// </para>
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <returns>
        /// <para>RSSI value of the connection in dBm.</para>
        /// <para>Range: -103 to -38</para>
        /// </returns>
        public async Task<sbyte> GetRssiAysnc(byte connection)
        {
            var commandValue = new[] { connection };
            var responseValue = await WriteAsync(0x01, commandValue);
            var rssi = (sbyte)responseValue[1];
            return rssi;
        }

        /// <summary>
        /// <para>
        /// This command updates the connection parameters of a given connection. The parameters have the same
        /// meaning and follow the same rules as for the GAP class command : Connect Direct.
        /// </para>
        /// <para>
        /// If this command is issued at a master device, it will send parameter update request to the Bluetooth link layer.
        /// </para>
        /// <para>
        /// On the other hand if this command is issued at a slave device, it will send L2CAP connection parameter update
        /// request to the master, which may either accept or reject it.
        /// </para>
        /// <para>
        /// It will take an amount of time corresponding to at least six times the current connection interval before the new
        /// connection parameters will become active.
        /// </para>
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <param name="intervalMin">Minimum connection interval (units of 1.25ms)</param>
        /// <param name="intervalMax">Maximum connection interval (units of 1.25ms)</param>
        /// <param name="latency">
        /// Slave latency which defines how many connections intervals a slave may
        /// skip.
        /// </param>
        /// <param name="timeout">Supervision timeout (units of 10ms)</param>
        /// <returns></returns>
        public async Task UpdateAysnc(byte connection, ushort intervalMin, ushort intervalMax, ushort latency, ushort timeout)
        {
            var intervalMinValue = BitConverter.GetBytes(intervalMin);
            var intervalMaxValue = BitConverter.GetBytes(intervalMax);
            var latencyValue = BitConverter.GetBytes(latency);
            var timeoutValue = BitConverter.GetBytes(timeout);
            var commandValue = new byte[9];
            commandValue[0] = connection;
            Array.Copy(intervalMinValue, 0, commandValue, 1, 2);
            Array.Copy(intervalMaxValue, 0, commandValue, 3, 2);
            Array.Copy(timeoutValue, 0, commandValue, 5, 2);
            Array.Copy(latencyValue, 0, commandValue, 7, 2);
            var responseValue = await WriteAsync(0x02, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command requests a version exchange of a given connection.
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <returns></returns>
        public async Task VersionUpdateAysnc(byte connection)
        {
            var commandValue = new[] { connection };
            var responseValue = await WriteAsync(0x03, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command can be used to read the current Channel Map.
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <returns>
        /// <para>
        /// Current Channel Map. Each bit corresponds to one channel. 0-bit
        /// corresponds to 0 channel.Size of Channel Map is 5 bytes.
        /// </para>
        /// <para>Channel range: 0-36</para>
        /// </returns>
        public async Task<byte[]> ChannelMapGetAysnc(byte connection)
        {
            var commandValue = new[] { connection };
            var responseValue = await WriteAsync(0x04, commandValue);
            var mapLength = responseValue[1];
            var map = new byte[mapLength];
            Array.Copy(responseValue, 2, map, 0, mapLength);
            return map;
        }

        /// <summary>
        /// This command can be used to set the new Channel Map.
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <param name="map">
        /// <para>
        /// New Channel Map. Channel Map is 5 bytes array. Each bit corresponds to
        /// one channel. 0-bit corresponds to 0 channel.
        /// </para>
        /// <para>Channel range: 0-36</para>
        /// </param>
        /// <returns></returns>
        public async Task ChannelMapSetAysnc(byte connection, byte[] map)
        {
            var commandValue = new byte[2 + map.Length];
            commandValue[0] = connection;
            commandValue[1] = map.GetByteLength();
            Array.Copy(map, 0, commandValue, 2, map.Length);
            var responseValue = await WriteAsync(0x05, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>This command returns the status of the given connection.</para>
        /// <para>Status is returned in a Status event.</para>
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <returns></returns>
        public async Task GetStatusAysnc(byte connection)
        {
            var commandValue = new[] { connection };
            await WriteAsync(0x07, commandValue);
        }

        /// <summary>
        /// This command temporarily enables or disables slave latency.
        /// </summary>
        /// <param name="disable">
        /// <para>0: enables slave latency</para>
        /// <para>1: disables slave latency</para>
        /// </param>
        /// <returns></returns>
        public async Task SlaveLatencyDisableAysnc(byte disable)
        {
            var commandValue = new[] { disable };
            var responseValue = await WriteAsync(0x09, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// This event indicates the connection status and parameters.
        /// </summary>
        public event EventHandler<StatusEventArgs> Status;
        /// <summary>
        /// This event indicates the remote devices version.
        /// </summary>
        public event EventHandler<VersionIndEventArgs> VersionInd;
        /// <summary>
        /// This event indicates the remote devices features.
        /// </summary>
        public event EventHandler<FeatureIndEventArgs> FeatureInd;
        /// <summary>
        /// This event is produced when a Bluetooth connection is disconnected.
        /// </summary>
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        #endregion
    }
}
