using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;

namespace BGLib.API
{
    public class BGSerialPort : IDisposable
    {
        private readonly SerialPort _serial;
        private readonly MessageAnalyzer _analyzer;
        private readonly IDictionary<ushort, string> _errors;

        public BGSerialPort(
            string portName,
            int baudRate = 256000,
            BGParity parity = BGParity.None,
            int dataBits = 8,
            BGStopBits stopBits = BGStopBits.One)
        {
            _serial = new SerialPort(portName, baudRate, (Parity)parity, dataBits, (StopBits)stopBits);
            _analyzer = new MessageAnalyzer();
            _errors = new Dictionary<ushort, string>()
            {
                // BGAPI Errors
                [0x0180] = "Command contained invalid parameter.",
                [0x0181] = "Device is in wrong state to receive command.",
                [0x0182] = "Device has run out of memory.",
                [0x0183] = "Feature is not implemented.",
                [0x0184] = "Command was not recognized.",
                [0x0185] = "Command or Procedure failed due to timeout.",
                [0x0186] = "Connection handle passed is to command is not a valid handle.",
                [0x0187] = "Command would cause either underflow or overflow error.",
                [0x0188] = "User attribute was accessed through API which is not supported.",
                [0x0189] = "No valid license key found.",
                [0x018A] = "Command maximum length exceeded.",
                [0x018B] = "Bonding procedure can't be started because device has no space left for bond.",
                [0x018C] = "Module was reset due to script stack overflow.",
                // Bluetooth Errors
                [0x0205] = "Pairing or authentication failed due to incorrect results in the pairing or authentication procedure. This could be due to an incorrect PIN or Link Key.",
                [0x0206] = "Pairing failed because of missing PIN, or authentication failed because of missing Key.",
                [0x0207] = "Controller is out of memory.",
                [0x0208] = "Link supervision timeout has expired.",
                [0x0209] = "Controller is at limit of connections it can support.",
                [0x020C] = "Command requested cannot be executed because the Controller is in a state where it cannot process this command at this time.",
                [0x0212] = "Command contained invalid parameters.",
                [0x0213] = "User on the remote device terminated the connection.",
                [0x0216] = "Local device terminated the connection.",
                [0x0222] = "Connection terminated due to link-layer procedure timeout.",
                [0x0228] = "Received link-layer control packet where instant was in the past.",
                [0x023A] = "Operation was rejected because the controller is busy and unable to process the request.",
                [0x023B] = "The Unacceptable Connection Interval error code indicates that the remote device terminated the connection because of an unacceptable connection interval.",
                [0x023C] = "Directed advertising completed without a connection being created.",
                [0x023D] = "Connection was terminated because the Message Integrity Check (MIC) failed on a received packet.",
                [0x023E] = "LL initiated a connection but the connection has failed to be established. Controller did not receive any packets from remote end.",
                // Security Manager Protocol Errors
                [0x0301] = "The user input of passkey failed, for example, the user cancelled the operation.",
                [0x0302] = "Out of Band data is not available for authentication.",
                [0x0303] = "The pairing procedure cannot be performed as authentication requirements cannot be met due to IO capabilities of one or both devices.",
                [0x0304] = "The confirm value does not match the calculated compare value.",
                [0x0305] = "Pairing is not supported by the device.",
                [0x0306] = "The resultant encryption key size is insufficient for the security requirements of this device.",
                [0x0307] = "The SMP command received is not supported on this device.",
                [0x0308] = "Pairing failed due to an unspecified reason.",
                [0x0309] = "Pairing or authentication procedure is disallowed because too little time has elapsed since last pairing request or security request.",
                [0x030A] = "The Invalid Parameters error code indicates: the command length is invalid or a parameter is outside of the specified range.",
                // Attribute Protocol Errors
                [0x0401] = "The attribute handle given was not valid on this server.",
                [0x0402] = "The attribute cannot be read.",
                [0x0403] = "The attribute cannot be written.",
                [0x0404] = "The attribute PDU was invalid.",
                [0x0405] = "The attribute requires authentication before it can be read or written.",
                [0x0406] = "Attribute Server does not support the request received from the client.",
                [0x0407] = "Offset specified was past the end of the attribute.",
                [0x0408] = "The attribute requires authorization before it can be read or written.",
                [0x0409] = "Too many prepare writes have been queueud.",
                [0x040A] = "No attribute found within the given attribute handle range.",
                [0x040B] = "The attribute cannot be read or written using the Read Blob Request.",
                [0x040C] = "The Encryption Key Size used for encrypting this link is insufficient.",
                [0x040D] = "The attribute value length is invalid for the operation.",
                [0x040E] = "The attribute request that was requested has encountered an error that was unlikely, and therefore could not be completed as requested.",
                [0x040F] = "The attribute requires encryption before it can be read or written.",
                [0x0410] = "The attribute type is not a supported grouping attribute as defined by a higher layer specification.",
                [0x0411] = "Insufficient Resources to complete the request.",
                [0x0480] = "Application error code defined by a higher layer specification.",
            };

            _serial.DataReceived += OnDataReceived;
            _serial.ErrorReceived += OnErrorReceived;
            _serial.PinChanged += OnPinChanged;
            _analyzer.MessageAnalyzed += OnMessageAnalyzed;
        }

        public void Open()
        {
            _serial.Open();
        }

        public void Close()
        {
            _serial.Close();
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var data = new byte[_serial.BytesToRead];
            _serial.Read(data, 0, data.Length);
            foreach (var value in data)
            {
                _analyzer.Analyze(value);
            }
        }

        private string GetMessage(ushort errorCode)
        {
            return _errors.TryGetValue(errorCode, out var message)
                ? message
                : $"Unknown error with code: {errorCode}";
        }

        #region Commands

        /// <summary>
        /// This command sets the scan parameters which affect how other Bluetooth Smart devices are discovered. See
        /// BLUETOOTH SPECIFICATION Version 4.0 [Vol 6 - Part B - Chapter 4.4.3].
        /// </summary>
        /// <param name="interval">
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
        /// <param name="window">
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
        /// <para>
        /// 1: Active scanning is used. When an advertisement packet is received the
        /// Bluetooth stack will send a scan request packet to the advertiser to try and
        /// read the scan response data.
        /// </para>
        /// <para>
        /// 0: Passive scanning is used.No scan request is made.
        /// </para>
        /// </param>
        /// <returns></returns>
        public async Task SetDiscoveryParameters(ushort interval, ushort window, bool active)
        {
            if (interval < 0x4 || interval > 0x4000)
            {
                var paramName = nameof(interval);
                throw new ArgumentOutOfRangeException(paramName);
            }
            if (window < 0x4 || window > 0x4000)
            {
                var paramName = nameof(window);
                throw new ArgumentOutOfRangeException(paramName);
            }
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.GenericAccessProfile;
            var id = (byte)GenericAccessProfileCommand.SetScanParameters;
            var intervalBytes = BitConverter.GetBytes(interval);
            var windowBytes = BitConverter.GetBytes(window);
            var payload = new byte[5];
            Array.Copy(intervalBytes, 0, payload, 0, 2);
            Array.Copy(windowBytes, 0, payload, 2, 2);
            payload[4] = active ? (byte)0x01 : (byte)0x00;
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// This command starts the GAP discovery procedure to scan for advertising devices i.e. to perform a device
        /// discovery.
        /// </summary>
        /// <param name="mode">GAP Discover modes</param>
        /// <returns></returns>
        public async Task StartDiscoveryAsync(BGDiscoverMode mode = BGDiscoverMode.Observation)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.GenericAccessProfile;
            var id = (byte)GenericAccessProfileCommand.Discover;
            var payload = new[] { (byte)mode };
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// This command ends the current GAP discovery procedure and stop the scanning of advertising devices.
        /// </summary>
        /// <returns></returns>
        public async Task StopDiscoveryAsync()
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.GenericAccessProfile;
            var id = (byte)GenericAccessProfileCommand.EndProcedure;
            var command = new Message(type, @class, id, null);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
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
        /// The connection establishment procedure can be cancelled with End Procedure command.
        /// </para>
        /// </summary>
        /// <param name="address">Bluetooth address of the target device</param>
        /// <param name="interval">
        /// <para>
        /// Connection Interval (in units of 1.25ms).
        /// </para>
        /// <para>
        /// Range: 6 - 3200
        /// </para>
        /// <para>
        /// The lowest possible Connection Interval is 7.50ms and the largest is
        /// 4000ms.
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
        /// <returns></returns>
        public async Task<byte> ConnectAsync(BGAddress address, ushort interval = 60, ushort timeout = 100, ushort latency = 0)
        {
            if (interval < 6 || interval > 3200)
            {
                var paramName = nameof(interval);
                throw new ArgumentOutOfRangeException(paramName);
            }
            if (timeout < 10 || timeout > 3200)
            {
                var paramName = nameof(timeout);
                throw new ArgumentOutOfRangeException(paramName);
            }
            if (latency < 0 || latency > 500)
            {
                var paramName = nameof(latency);
                throw new ArgumentOutOfRangeException(paramName);
            }
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.GenericAccessProfile;
            var id = (byte)GenericAccessProfileCommand.ConnectDirect;
            var addressBytes = address.RawValue;
            var intervalBytes = BitConverter.GetBytes(interval);
            var timeoutBytes = BitConverter.GetBytes(timeout);
            var latencyBytes = BitConverter.GetBytes(latency);
            var payload = new byte[15];
            Array.Copy(addressBytes, 0, payload, 0, 6);
            payload[6] = (byte)address.Type;
            Array.Copy(intervalBytes, 0, payload, 7, 2);
            Array.Copy(intervalBytes, 0, payload, 9, 2);
            Array.Copy(timeoutBytes, 0, payload, 11, 2);
            Array.Copy(latencyBytes, 0, payload, 13, 2);
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
            var connectionHandle = response.Payload[2];
            return connectionHandle;
        }

        private async Task<Message> WriteAsync(Message command)
        {
            var writeTCS = new TaskCompletionSource<Message>();
            var onMessageAnalyzed = new EventHandler<MessageEventArgs>((s, e) =>
            {
                var type = (byte)MessageType.Response;
                if (e.Message.Type != type ||
                    e.Message.Class != command.Class ||
                    e.Message.Id != command.Id)
                {
                    return;
                }
                writeTCS.TrySetResult(e.Message);
            });
            _analyzer.MessageAnalyzed += onMessageAnalyzed;
            try
            {
                var data = command.ToBytes();
                _serial.Write(data, 0, data.Length);
                return await writeTCS.Task;
            }
            finally
            {
                _analyzer.MessageAnalyzed -= onMessageAnalyzed;
            }
        }
        #endregion

        #region Events

        public event EventHandler<BGDiscoveryEventArgs> Discovered;

        private void OnMessageAnalyzed(object sender, MessageEventArgs e)
        {
            var type = (byte)MessageType.Event;
            if (e.Message.Type != type)
                return;
            var @class = (MessageClass)e.Message.Class;
            try
            {
                switch (@class)
                {
                    case MessageClass.System:
                        OnSystemEventAnalyzed(e.Message);
                        break;
                    case MessageClass.PersistentStore:
                        OnPersistentStoreEventAnalyzed(e.Message);
                        break;
                    case MessageClass.AttributeDatabase:
                        OnAttributeDatabaseEventAnalyzed(e.Message);
                        break;
                    case MessageClass.Connection:
                        OnConnectionEventAnalyzed(e.Message);
                        break;
                    case MessageClass.AttributeClient:
                        OnAttributeClientEventAnalyzed(e.Message);
                        break;
                    case MessageClass.SecurityManager:
                        OnSecurityManagerEventAnalyzed(e.Message);
                        break;
                    case MessageClass.GenericAccessProfile:
                        OnGenericAccessProfileEventAnalyzed(e.Message);
                        break;
                    case MessageClass.Hardware:
                        OnHardwareEventAnalyzed(e.Message);
                        break;
                    case MessageClass.DeviceFirmwareUpgrade:
                        OnDeviceFirmwareUpgradeEventAnalyzed(e.Message);
                        break;
                    case MessageClass.Testing:  // Testing doesn't have events.
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                // Analyze event failed, just skip.
#if DEBUG
                System.Diagnostics.Debug.WriteLine(ex.Message);
#endif
            }
        }

        private void OnDeviceFirmwareUpgradeEventAnalyzed(Message message)
        {
            throw new NotImplementedException();
        }

        private void OnHardwareEventAnalyzed(Message message)
        {
            throw new NotImplementedException();
        }

        private void OnGenericAccessProfileEventAnalyzed(Message message)
        {
            var id = (GenericAccessProfileEvent)message.Id;
            switch (id)
            {
                case GenericAccessProfileEvent.ScanResponse:
                    {
                        var rssi = (sbyte)message.Payload[0];
                        var type = (BGDiscoveryType)message.Payload[1];
                        //var rawValue = new byte[6];
                        //Array.Copy(message.Payload, 2, rawValue, 0, rawValue.Length);
                        var rawValue = message.Payload.Skip(2).Take(6).ToArray();
                        var addressType = (BGAddressType)message.Payload[8];
                        var address = new BGAddress(addressType, rawValue);
                        //var bond = message.Payload[9];
                        var dataLength = message.Payload[10];
                        var data = message.Payload.Skip(11).Take(dataLength).ToArray();
                        var advertisements = new List<BGAdvertisement>();
                        for (int i = 0; i < data.Length; i++)
                        {
                            var advertisementLength = data[i];
                            var advertisementType = (BGAdvertisementType)data[i + 1];
                            var advertisementValue = data.Skip(i + 2).Take(advertisementLength - 1).ToArray();
                            var advertisement = new BGAdvertisement(advertisementType, advertisementValue);
                            advertisements.Add(advertisement);
                            i += advertisementLength;
                        }
                        var discovery = new BGDiscovery(rssi, type, address, advertisements);
                        var e = new BGDiscoveryEventArgs(discovery);
                        Discovered?.Invoke(this, e);
                        break;
                    }
                case GenericAccessProfileEvent.ModeChanged:
                    {
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        private void OnSecurityManagerEventAnalyzed(Message message)
        {
            throw new NotImplementedException();
        }

        private void OnAttributeClientEventAnalyzed(Message message)
        {
            throw new NotImplementedException();
        }

        private void OnConnectionEventAnalyzed(Message message)
        {
            throw new NotImplementedException();
        }

        private void OnAttributeDatabaseEventAnalyzed(Message message)
        {
            throw new NotImplementedException();
        }

        private void OnPersistentStoreEventAnalyzed(Message message)
        {
            throw new NotImplementedException();
        }

        private void OnSystemEventAnalyzed(Message message)
        {
            throw new NotImplementedException();
        }

        private void OnPinChanged(object sender, SerialPinChangedEventArgs e)
        {

        }

        private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {

        }

        #endregion

        #region IDisposable

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    _serial.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并替代终结器
                // TODO: 将大型字段设置为 null
                _disposed = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~BGAPI()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
