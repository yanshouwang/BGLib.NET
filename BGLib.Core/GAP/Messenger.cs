using System;
using System.Threading.Tasks;

namespace BGLib.Core.GAP
{
    /// <summary>
    /// The Generic Access Profile (GAP) class provides methods to control the Bluetooth GAP level functionality of
    /// the local device. The GAP call for example allows remote device discovery, connection establishment and local
    /// devices connection and discovery modes. The GAP class also allows the control of local devices privacy
    /// modes.
    /// </summary>
    public class Messenger : BaseMessenger
    {
        internal Messenger(MessageHub messageHub)
            : base(messageHub)
        {
        }

        protected override byte Category => 0x06;

        protected override void OnEventAnalyzed(byte id, byte[] eventValue)
        {
            switch (id)
            {
                case 0x00:
                    {
                        var rssi = (sbyte)eventValue[0];
                        var packetType = eventValue[1];
                        var sender = new byte[6];
                        Array.Copy(eventValue, 2, sender, 0, 6);
                        var addressType = (AddressType)eventValue[8];
                        var bond = eventValue[9];
                        var dataLength = eventValue[10];
                        var data = new byte[dataLength];
                        Array.Copy(eventValue, 11, data, 0, data.Length);
                        var eventArgs = new ScanResponseEventArgs(rssi, packetType, sender, addressType, bond, data);
                        ScanResponse?.Invoke(this, eventArgs);
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
        /// <para>This command sets GAP central/peripheral privacy flags.</para>
        /// <para>
        /// By setting for example peripheral_privacy to 1, the Bluetooth stack will automatically generate a resolvable
        /// random private address for the advertising packets every time the Set Mode command is used to enter
        /// advertising mode.
        /// </para>
        /// <para>
        /// By setting privacy mode to 2, the Bluetooth stack will generate a resolvable random private address on
        /// demand.If peripherial_privacy is set to 2 additionally Set Mode is called with the current Discoverable and
        /// Connectable parameters.Setting up new mode by Set Mode command does not change generated address.
        /// </para>
        /// <para>
        /// By setting privacy mode to 3, the Bluetooth stack will use a non-resolvable random private address (set by Set
        /// Nonresolvable Address command). For example if peripheral_privacy is set to 3, the Bluetooth stack will get a
        /// non-resolvable random private address for the advertising packets every time the Set Mode command is used
        /// to enter advertising mode.
        /// </para>
        /// <para>
        /// It is not recommended to adjust peripheral privacy unless mandatory by the application, because not
        /// all Bluetooth implementations can decode resolvable private addresses.
        /// </para>
        /// </summary>
        /// <param name="peripheralPrivacy">
        /// <para>0: disable peripheral privacy</para>
        /// <para>1: enable peripheral privacy</para>
        /// <para>2: change peripheral private address on demand</para>
        /// <para>3: enable peripheral privacy with non-resolvable address</para>
        /// <para>Any other value will have no effect on flag</para>
        /// </param>
        /// <param name="centralPrivacy">
        /// <para>0: disable central privacy</para>
        /// <para>1: enable central privacy</para>
        /// <para>2: change central private address on demand</para>
        /// <para>3: enable central privacy with non-resolvable address</para>
        /// <para>Any other value will have no effect on flag</para>
        /// </param>
        /// <returns></returns>
        public async Task SetPrivacyFlagsAsync(byte peripheralPrivacy, byte centralPrivacy)
        {
            var commandValue = new[] { peripheralPrivacy, centralPrivacy };
            await WriteAsync(0x00, commandValue);
        }

        /// <summary>
        /// This command configures the current GAP discoverability and connectability modes. It can be used to enable
        /// advertisements and/or allow connection.The command is also meant to fully stop advertising, when using
        /// <see cref="DiscoverableMode.NoneDiscoverable"/> and <see cref="ConnectableMode.NonConnectable"/>.
        /// </summary>
        /// <param name="discover"></param>
        /// <param name="connect"></param>
        /// <returns></returns>
        public async Task SetModeAsync(DiscoverableMode discover, ConnectableMode connect)
        {
            var discoverValue = (byte)discover;
            var connectValue = (byte)connect;
            var commandValue = new[] { discoverValue, connectValue };
            var responseValue = await WriteAsync(0x01, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// This command starts the GAP discovery procedure to scan for advertising devices i.e. to perform a device
        /// discovery.
        /// </para>
        /// Scanning parameters can be configured with the <see cref="SetScanParametersAsync(ushort, ushort, bool)"/>
        /// <para>
        /// To cancel on an ongoing discovery process use the <see cref="EndProcedureAsync"/>
        /// </para>
        /// </summary>
        /// <param name="mode">GAP Discover modes</param>
        /// <returns></returns>
        public async Task DiscoverAsync(DiscoverMode mode)
        {
            var modeValue = (byte)mode;
            var commandValue = new[] { modeValue };
            var responseValue = await WriteAsync(0x02, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// This command will start the GAP direct connection establishment procedure to a dedicated Bluetooth Smart
        /// device.
        /// </para>
        /// <para>
        /// The Bluetooth module will enter a state where it continuously scans for the connectable advertisement packets
        /// from the remote device which matches the Bluetooth address gives as a parameter.Upon receiving the
        /// advertisement packet, the module will send a connection request packet to the target device to imitate a
        /// Bluetooth connection.A successful connection will bi indicated by a Status event.
        /// </para>
        /// <para>
        /// If the device is configured to support more than one connection, the smallest connection interval which is
        /// divisible by maximum_connections * 2.5ms will be selected. Thus, it is important to provide minimum and
        /// maximum connection intervals so that such a connection interval is available within the range.
        /// </para>
        /// <para>
        /// The connection establishment procedure can be cancelled with <see cref="EndProcedureAsync"/>.
        /// </para>
        /// </summary>
        /// <param name="address">Bluetooth address of the target device</param>
        /// <param name="connIntervalMin">
        /// <para>Minimum Connection Interval (in units of 1.25ms).</para>
        /// <para>Range: 6 - 3200</para>
        /// <para>
        /// The lowest possible Connection Interval is 7.50ms and the largest is
        /// 4000ms.
        /// </para>
        /// </param>
        /// <param name="connIntervalMax">
        /// <para>Maximum Connection Interval (in units of 1.25ms).</para>
        /// <para>Range: 6 - 3200</para>
        /// <para>
        /// Must be equal or bigger than minimum Connection Interval.
        /// </para>
        /// </param>
        /// <param name="timeout">
        /// <para>
        /// Supervision Timeout (in units of 10ms). The Supervision Timeout
        /// defines how long the devices can be out of range before the
        /// connection is closed.
        /// </para>
        /// <para>
        /// Range: 10 - 3200
        /// </para>
        /// <para>
        /// Minimum time for the Supervision Timeout is 100ms and maximum
        /// value is 32000ms.
        /// </para>
        /// <para>
        /// According to the specification, the Supervision Timeout in
        /// milliseconds shall be larger than(1 + latency) * conn_interval_max
        /// * 2, where conn_interval_max is given in milliseconds.
        /// </para>
        /// </param>
        /// <param name="latency">
        /// <para>
        /// This parameter configures the slave latency. Slave latency defines
        /// how many connection intervals a slave device can skip.
        /// Increasing slave latency will decrease the energy consumption of
        /// the slave in scenarios where slave does not have data to send at
        /// every connection interval.
        /// </para>
        /// <para>
        /// Range: 0 - 500
        /// </para>
        /// <para>
        /// 0 : Slave latency is disabled.
        /// </para>
        /// <para>
        /// Example:
        /// </para>
        /// <para>
        /// Connection interval is 10ms and slave latency is 9: this means that
        /// the slave is allowed to communicate every 100ms, but it can
        /// communicate every 10ms if needed.
        /// </para>
        /// </param>
        /// <returns>Connection handle that is reserved for new connection</returns>
        public async Task<byte> ConnectDirectAsync(byte[] address, AddressType addrType, ushort connIntervalMin, ushort connIntervalMax, ushort timeout, ushort latency)
        {
            var connIntervalMinValue = BitConverter.GetBytes(connIntervalMin);
            var connIntervalMaxValue = BitConverter.GetBytes(connIntervalMax);
            var timeoutValue = BitConverter.GetBytes(timeout);
            var latencyValue = BitConverter.GetBytes(latency);
            var commandValue = new byte[15];
            Array.Copy(address, 0, commandValue, 0, 6);
            commandValue[6] = (byte)addrType;
            Array.Copy(connIntervalMinValue, 0, commandValue, 7, 2);
            Array.Copy(connIntervalMaxValue, 0, commandValue, 9, 2);
            Array.Copy(timeoutValue, 0, commandValue, 11, 2);
            Array.Copy(latencyValue, 0, commandValue, 13, 2);
            var responseValue = await WriteAsync(0x03, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var connectionHandle = responseValue[2];
            return connectionHandle;
        }

        /// <summary>
        /// This command ends the current GAP discovery procedure and stop the scanning of advertising devices.
        /// </summary>
        /// <returns></returns>
        public async Task EndProcedureAsync()
        {
            var responseValue = await WriteAsync(0x04);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// This command will start the GAP direct connection establishment procedure to a set of dedicated Bluetooth
        /// Low Energy devices.
        /// </para>
        /// <para>When this command is issued the the Bluetooth module will enter a state where it scans connectable
        /// advertisement packets from the remote devices which are registered in the local white list. Upon receiving an
        /// advertisement packet from one of the registered devices, the module will send a connection request to this
        /// device, and a successful connection will produce a connection status event.</para>
        /// <para>The connect selective command can be cancelled with End Procedure command.</para>
        /// <para>When in Initiating State there are no scan response events.</para>
        /// </summary>
        /// <param name="connIntervalMin">
        /// <para>Minimum connection interval (in units of 1.25ms).</para>
        /// <para>Range: 6 - 3200</para>
        /// <para>
        /// The lowest possible connection interval is 7.50ms and the largest is
        /// 4000ms.
        /// </para>
        /// <para>
        /// When more then one connection is supported the connection interval
        /// values(minimum and maximum) used in all connection commands
        /// must be divisible by connection count* 2.5ms
        /// </para>
        /// </param>
        /// <param name="connIntervalMax">
        /// <para>Maximum connection interval (in units of 1.25ms).</para>
        /// <para>Range: 6 - 3200</para>
        /// <para>Must be equal or bigger than minimum connection interval.</para>
        /// </param>
        /// <param name="timeout">
        /// <para>
        /// Supervision timeout (in units of 10ms). The supervision timeout defines
        /// how long the devices can be out of range before the connection is
        /// closed.
        /// </para>
        /// <para>Range: 10 - 3200</para>
        /// <para>
        /// Minimum time for the supervision timeout is 100ms and maximum
        /// value: 32000ms.Supervision timeout must also be equal or grater than
        /// maximum connection interval.
        /// </para>
        /// </param>
        /// <param name="latency">
        /// <para>
        /// This parameter configures the slave latency. Slave latency defines
        /// how many connection intervals a slave device can skip.
        /// Increasing slave latency will decrease the energy consumption of the
        /// slave in scenarios where slave does not have data to send at every
        /// connection interval.
        /// </para>
        /// <para>Range: 0 - 500</para>
        /// <para>0 : Slave latency is disabled.</para>
        /// <para>Example:</para>
        /// <para>
        /// Connection interval is 10ms and slave latency is 9: this means that the
        /// slave is allowed to communicate every 100ms, but it can communicate
        /// every 10ms if needed.
        /// <para>Note:</para>
        /// <para>
        /// Slave Latency x Connection interval can NOT be higher than
        /// supervision timeout.
        /// </para>
        /// </para>
        /// </param>
        /// <returns>Connection handle reserved for connection</returns>
        public async Task<byte> ConnectSelectiveAsync(ushort connIntervalMin, ushort connIntervalMax, ushort timeout, ushort latency)
        {
            var connIntervalMinValue = BitConverter.GetBytes(connIntervalMin);
            var connIntervalMaxValue = BitConverter.GetBytes(connIntervalMax);
            var timeoutValue = BitConverter.GetBytes(timeout);
            var latencyValue = BitConverter.GetBytes(latency);
            var commandValue = new byte[8];
            Array.Copy(connIntervalMinValue, 0, commandValue, 0, 2);
            Array.Copy(connIntervalMaxValue, 0, commandValue, 2, 2);
            Array.Copy(timeoutValue, 0, commandValue, 4, 2);
            Array.Copy(latencyValue, 0, commandValue, 6, 2);
            var responseValue = await WriteAsync(0x05, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var connectionHandle = responseValue[2];
            return connectionHandle;
        }

        /// <summary>
        /// This command can be used to set scan, connection, and advertising filtering parameters based on the local
        /// devices white list.See also Whitelist Append command.
        /// </summary>
        /// <param name="scanPolicy"></param>
        /// <param name="advPolicy"></param>
        /// <param name="scanDuplicateFiltering">
        /// <para>0: Do not filter duplicate advertisers</para>
        /// <para>1: Filter duplicates</para>
        /// </param>
        /// <returns></returns>
        public async Task SetFilteringAsync(ScanPolicy scanPolicy, AdvertisingPolicy advPolicy, byte scanDuplicateFiltering)
        {
            var scanPolicyValue = (byte)scanPolicy;
            var advPolicyValue = (byte)advPolicy;
            var commandValue = new[] { scanPolicyValue, advPolicyValue, scanDuplicateFiltering };
            var responseValue = await WriteAsync(0x06, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// This command sets the scan parameters which affect how other Bluetooth Smart devices are discovered. See
        /// BLUETOOTH SPECIFICATION Version 4.0 [Vol 6 - Part B - Chapter 4.4.3].
        /// </para>
        /// <para>
        /// Keep in mind that when scan window value is equal to scan interval value, CPU may not have enough
        /// time to switch between speed of the system clock when using slow clock option and as a result the
        /// current consumption may not decrease.
        /// </para>
        /// </summary>
        /// <param name="scanInterval">
        /// <para>
        /// Scan interval defines the interval when scanning is re-started in units of
        /// 625us
        /// </para>
        /// <para>
        /// Range: 0x4 - 0x4000
        /// </para>
        /// <para>
        /// Default: 0x4B (46,875ms)
        /// </para>
        /// <para>
        /// After every scan interval the scanner will change the frequency it operates at
        /// at it will cycle through all the three advertisements channels in a round robin
        /// fashion.According to the Bluetooth specification all three channels must be
        /// used by a scanner.
        /// </para>
        /// </param>
        /// <param name="scanWindow">
        /// <para>
        /// Scan Window defines how long time the scanner will listen on a certain
        /// frequency and try to pick up advertisement packets.Scan window is defined
        /// as units of 625us
        /// </para>
        /// <para>
        /// Range: 0x4 - 0x4000
        /// </para>
        /// <para>
        /// Default: 0x32 (31,25 ms)
        /// </para>
        /// <para>
        /// Scan windows must be equal or smaller than scan interval
        /// If scan window is equal to the scan interval value, then the Bluetooth module
        /// will be scanning at a 100% duty cycle.
        /// If scan window is half of the scan interval value, then the Bluetooth module
        /// will be scanning at a 50% duty cycle.
        /// </para>
        /// </param>
        /// <param name="active">
        /// <para>0: Passive scanning is used. No scan request is made.</para>
        /// <para>
        /// 1: Active scanning is used. When an advertisement packet is received the
        /// Bluetooth stack will send a scan request packet to the advertiser to try and
        /// read the scan response data.
        /// </para>
        /// </param>
        /// <returns></returns>
        public async Task SetScanParametersAsync(ushort scanInterval, ushort scanWindow, byte active)
        {
            var scanIntervalValue = BitConverter.GetBytes(scanInterval);
            var scanWindowValue = BitConverter.GetBytes(scanWindow);
            var commandValue = new byte[5];
            Array.Copy(scanIntervalValue, 0, commandValue, 0, 2);
            Array.Copy(scanWindowValue, 0, commandValue, 2, 2);
            commandValue[4] = active;
            var responseValue = await WriteAsync(0x07, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>This command is used to set the advertising parameters.</para>
        /// <para>
        /// Example: If the minimum advertisement interval is 40ms and the maximum advertisement interval is 100ms
        /// then the real advertisement interval will be mostly the middle value(70ms) plus a randomly added 20ms delay,
        /// which needs to be added according to the Bluetooth specification.
        /// </para>
        /// <para>
        /// If you are currently advertising, then any changes set using this command will not take effect until you
        /// stop and re-start advertising.
        /// </para>
        /// </summary>
        /// <param name="advIntervalMin">
        /// <para>Minimum advertisement interval in units of 625us</para>
        /// <para>Range: 0x20 to 0x4000</para>
        /// <para>Default: 0x200 (320ms)</para>
        /// <para>Explanation:</para>
        /// <para>0x200 = 512</para>
        /// <para>512 * 625us = 320000us = 320ms</para>
        /// </param>
        /// <param name="advIntervalMax">
        /// <para>Maximum advertisement interval in units of 625us.</para>
        /// <para>Range: 0x20 to 0x4000</para>
        /// <para>Default: 0x200 (320ms)</para>
        /// </param>
        /// <param name="advChannels">
        /// <para>A bit mask to identify which of the three advertisement channels are used.</para>
        /// <para>Examples:</para>
        /// <para>0x07: All three channels are used</para>
        /// <para>0x03: Advertisement channels 37 and 38 are used.</para>
        /// <para>0x04: Only advertisement channel 39 is used</para>
        /// </param>
        /// <returns></returns>
        public async Task SetAdvParametersAsync(ushort advIntervalMin, ushort advIntervalMax, byte advChannels)
        {
            var advIntervalMinValue = BitConverter.GetBytes(advIntervalMin);
            var advIntervalMaxValue = BitConverter.GetBytes(advIntervalMax);
            var commandValue = new byte[5];
            Array.Copy(advIntervalMinValue, 0, commandValue, 0, 2);
            Array.Copy(advIntervalMaxValue, 0, commandValue, 2, 2);
            commandValue[4] = advChannels;
            var responseValue = await WriteAsync(0x08, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// This commands set advertisement or scan response data used in the advertisement and scan response
        /// packets.The command allows application specific data to be broadcasts either in advertisement or scan
        /// response packets.
        /// </para>
        /// <para>The data set with this command is only used when the GAP discoverable mode is set to gap_user_data.</para>
        /// <para>
        /// Notice that advertisement or scan response data must be formatted in accordance to the Bluetooth Core
        /// Specification.See BLUETOOTH SPECIFICATION Version 4.0 [Vol 3 - Part C - Chapter 11].
        /// </para>
        /// </summary>
        /// <param name="setScanrsp">
        /// <para>Advertisement data type</para>
        /// <para>0 : sets advertisement data</para>
        /// <para>1 : sets scan response data</para>
        /// </param>
        /// <param name="advData">Advertisement data to send</param>
        /// <returns></returns>
        public async Task SetAdvDataAsync(byte setScanrsp, byte[] advData)
        {
            var commandValue = new byte[2 + advData.Length];
            commandValue[0] = setScanrsp;
            commandValue[1] = advData.GetByteLength();
            Array.Copy(advData, 0, commandValue, 2, advData.Length);
            var responseValue = await WriteAsync(0x09, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>This command sets device to Directed Connectable mode.</para>
        /// <para>
        /// In this mode the device uses fast advertisement procedure for the first 1.28 seconds, after which the device
        /// enters a non-connectable mode.If the device implements the Peripheral Preferred Connection Parameters
        /// characteristic in its GAP service the parameters defined by this characteristic will be used for the connection.
        /// </para>
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task SetDirectedConnectableModeAsync(byte[] address, AddressType addrType)
        {
            var commandValue = new byte[7];
            Array.Copy(address, 0, commandValue, 0, 6);
            commandValue[6] = (byte)addrType;
            var responseValue = await WriteAsync(0x0A, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command sets the scan parameters for Initiating State which affect for establishing BLE connection. See
        /// BLUETOOTH SPECIFICATION Version 4.0 [Vol 6 - Part B - Chapter 4.4.4].
        /// </summary>
        /// <param name="scanInterval">
        /// <para>Scan interval defines the interval when scanning is re-started in units of 625us</para>
        /// <para>Range: 0x4 - 0x4000</para>
        /// <para>Default: 0x32 (31,25ms)</para>
        /// <para>
        /// After every scan interval the scanner will change the frequency it operates at
        /// at it will cycle through all the three advertisements channels in a round robin
        /// fashion.According to the Bluetooth specification all three channels must be
        /// used by a scanner.
        /// </para>
        /// </param>
        /// <param name="scanWindow">
        /// <para>
        /// Scan Window defines how long time the scanner will listen on a certain
        /// frequency and try to pick up advertisement packets.Scan window is defined
        /// as units of 625us
        /// </para>
        /// <para>Range: 0x4 - 0x4000</para>
        /// <para>Default: 0x32 (31,25ms)</para>
        /// <para>Scan windows must be equal or smaller than scan interval</para>
        /// <para>
        /// If scan window is equal to the scan interval value, then the Bluetooth module
        /// will be scanning at a 100% duty cycle.
        /// </para>
        /// <para>
        /// If scan window is half of the scan interval value, then the Bluetooth module
        /// will be scanning at a 50% duty cycle.
        /// </para>
        /// </param>
        /// <returns></returns>
        public async Task SetInitiatingConParametersAsync(ushort scanInterval, ushort scanWindow)
        {
            var scanIntervalValue = BitConverter.GetBytes(scanInterval);
            var scanWindowValue = BitConverter.GetBytes(scanWindow);
            var commandValue = new byte[4];
            Array.Copy(scanIntervalValue, 0, commandValue, 0, 2);
            Array.Copy(scanWindowValue, 0, commandValue, 2, 2);
            var responseValue = await WriteAsync(0x0B, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command set the local device's random Non-Resolvable Bluetooth address. Default local device's random
        /// Non-Resolvable Bluetooth address is 00:00:00:00:00:01.
        /// </summary>
        /// <param name="address">Bluetooth non-resolvable address of the local device</param>
        /// <returns></returns>
        public async Task SetNonresolvableAddressAsync(byte[] address)
        {
            var commandValue = address;
            var responseValue = await WriteAsync(0x0C, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// This is a scan response event. This event is normally received by a Master which is scanning for advertisement
        /// and scan response packets from Slaves.
        /// </summary>
        public event EventHandler<ScanResponseEventArgs> ScanResponse;

        #endregion
    }
}
