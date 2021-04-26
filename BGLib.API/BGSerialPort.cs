using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;

namespace BGLib.API
{
    public class BGSerialPort : IDisposable
    {
        #region Fields

        private readonly SerialPort _serial;
        private readonly MessageAnalyzer _analyzer;
        private readonly IDictionary<ushort, string> _errors;

        #endregion

        #region Methods

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

        private string GetMessage(ushort errorCode)
        {
            return _errors.TryGetValue(errorCode, out var message)
                ? message
                : $"Unknown error with code: {errorCode}";
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

        #region Commands - System

        /// <summary>
        /// This command resets the local device immediately. The command does not have a response.
        /// </summary>
        /// <param name="mode">Selects the boot mode</param>
        public void Reset(BGBootMode mode)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.Reset;
            var payload = new[] { (byte)mode };
            var command = new Message(type, @class, id, payload);
            var data = command.ToBytes();
            _serial.Write(data, 0, data.Length);
        }

        /// <summary>
        /// This command can be used to test if the local device is functional. Similar to a typical "AT" -> "OK" test.
        /// </summary>
        /// <returns></returns>
        public async Task HelloAsync()
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.Hello;
            var command = new Message(type, @class, id);
            await WriteAsync(command);
        }

        /// <summary>
        /// This command reads the local device's public Bluetooth address.
        /// </summary>
        /// <returns></returns>
        public async Task<BGAddress> GetAddressAsync()
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.AddressGet;
            var command = new Message(type, @class, id);
            var response = await WriteAsync(command);
            var address = new BGAddress(BGAddressType.Public, response.Payload);
            return address;
        }

        /// <summary>
        /// Read packet counters and resets them, also returns available packet buffers.
        /// </summary>
        /// <returns></returns>
        public async Task<BGCounters> GetCountersAsync()
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.GetCounters;
            var command = new Message(type, @class, id);
            var response = await WriteAsync(command);
            var transmitted = response.Payload[0];
            var retransmitted = response.Payload[1];
            var receivedOK = response.Payload[2];
            var receivedError = response.Payload[3];
            var available = response.Payload[4];
            var counters = new BGCounters(transmitted, retransmitted, receivedOK, receivedError, available);
            return counters;
        }

        /// <summary>
        /// This command reads the number of supported connections from the local device.
        /// </summary>
        /// <returns>Max supported connections</returns>
        public async Task<byte> GetMaxConnectionsAsync()
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.GetConnections;
            var command = new Message(type, @class, id);
            var response = await WriteAsync(command);
            var maxConnections = response.Payload[0];
            return maxConnections;
        }

        /// <summary>
        /// This command reads the local devices software and hardware versions.
        /// </summary>
        /// <returns></returns>
        public async Task<BGVersion> GetVersionAsync()
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.GetInfo;
            var command = new Message(type, @class, id);
            var response = await WriteAsync(command);
            var major = BitConverter.ToUInt16(response.Payload, 0);
            var minor = BitConverter.ToUInt16(response.Payload, 2);
            var patch = BitConverter.ToUInt16(response.Payload, 4);
            var build = BitConverter.ToUInt16(response.Payload, 6);
            var linkLayer = BitConverter.ToUInt16(response.Payload, 8);
            var protocol = response.Payload[10];
            var hardware = response.Payload[11];
            var version = new BGVersion(major, minor, patch, build, linkLayer, protocol, hardware);
            return version;
        }

        /// <summary>
        /// Send data to endpoint, error is returned if endpoint does not have enough space
        /// </summary>
        /// <param name="endpoint">Endpoint index to send data to</param>
        /// <param name="data">data to send</param>
        /// <returns></returns>
        public async Task WriteEndPointAsync(BGEndpoint endpoint, byte[] data)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.EndpointTX;
            var length = data.GetByteLength();
            var payload = new byte[2 + length];
            payload[0] = (byte)endpoint;
            payload[1] = length;
            Array.Copy(data, 0, payload, 2, length);
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
        /// Add an entry to the running white list. By the white list you can define for example the remote devices which are
        /// allowed to establish a connection.See also Set Filtering and Connect Selective(if the white list is empty they
        /// will not be active). Do not use this command while advertising, scanning, or while being connected. The current
        /// list is discarded upon reset or power-cycle.
        /// </summary>
        /// <param name="address">
        /// <para>
        /// Bluetooth device address to add to the running white list
        /// </para>
        /// <para>
        /// Maximum of 8 can be stored before you must clear or remove entires
        /// </para>
        /// </param>
        /// <param name="type">Bluetooth address type</param>
        /// <returns></returns>
        public async Task AppendWhitelistAsync(BGAddress address, BGAddressType addressType)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.WhitelistAppend;
            var payload = new byte[7];
            Array.Copy(address.RawValue, payload, 6);
            payload[6] = (byte)addressType;
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
        /// <para>
        /// Remove an entry from the running white list.
        /// </para>
        /// <para>
        /// Do not use this command while advertising or while being connected.
        /// </para>
        /// </summary>
        /// <param name="address">
        /// <para>
        /// Bluetooth device address to add to the running white list
        /// </para>
        /// <para>
        /// Maximum of 8 can be stored before you must clear or remove entires
        /// </para>
        /// </param>
        /// <param name="type">Bluetooth address type</param>
        /// <returns></returns>
        public async Task RemoveWhitelistAsync(BGAddress address, BGAddressType addressType)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.WhitelistRemove;
            var payload = new byte[7];
            Array.Copy(address.RawValue, payload, 6);
            payload[6] = (byte)addressType;
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
        /// <para>
        /// Delete all entries on the white list at once.
        /// </para>
        /// <para>
        /// Do not use this command while advertising or while being connected.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public async Task ClearWhitelistAsync()
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.WhitelistClear;
            var command = new Message(type, @class, id);
            await WriteAsync(command);
        }

        /// <summary>
        /// Read data from an endpoint (i.e., data souce, e.g., UART), error is returned if endpoint does not have enough
        /// data.
        /// </summary>
        /// <param name="endpoint">Endpoint index to read data from</param>
        /// <param name="size">Size of data to read</param>
        /// <returns></returns>
        public async Task<byte[]> ReadEndpointAsync(BGEndpoint endpoint, byte size)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.EndpointRX;
            var payload = new byte[2];
            payload[0] = (byte)endpoint;
            payload[1] = size;
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
            var length = response.Payload[2];
            var data = new byte[length];
            Array.Copy(response.Payload, 3, data, 0, length);
            return data;
        }

        /// <summary>
        /// Set watermarks on both input and output sides of an endpoint. This is used to enable and disable the following
        /// events: Endpoint Watermark Tx and Endpoint Watermark Rx.
        /// </summary>
        /// <param name="endpoint">Endpoint index to set watermarks.</param>
        /// <param name="receive">
        /// <para>
        /// Watermark position on receive buffer
        /// </para>
        /// <para>
        /// 0xFF : watermark is not modified
        /// </para>
        /// <para>
        /// 0x00 : disables watermark
        /// </para>
        /// <para>
        /// 1-63 : sets watermark position
        /// </para>
        /// </param>
        /// <param name="transmit">
        /// <para>
        /// Watermark position on transmit buffer
        /// </para>
        /// <para>
        /// 0xFF : watermark is not modified
        /// </para>
        /// <para>
        /// 0x00 : disables watermark
        /// </para>
        /// <para>
        /// 1-63 : sets watermark position
        /// </para>
        /// </param>
        /// <returns></returns>
        public async Task SetEndpointWatermarksAsync(BGEndpoint endpoint, byte receive, byte transmit)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.EndpointSetWatermarks;
            var payload = new byte[3];
            payload[0] = (byte)endpoint;
            payload[1] = receive;
            payload[2] = transmit;
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
        /// This command defines the encryption key that will be used with the AES encrypt and decrypt commands.
        /// </summary>
        /// <param name="key">
        /// <para>
        /// Encryption key.
        /// </para>
        /// <para>
        /// Key size is 16 bytes, will be zero padded if less.
        /// </para>
        /// </param>
        /// <returns></returns>
        public async Task DefineEncryptionKeyAsync(byte[] key)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.AesSetKey;
            var length = key.GetByteLength();
            var payload = new byte[1 + length];
            payload[0] = length;
            Array.Copy(key, payload, length);
            var command = new Message(type, @class, id, null);
            await WriteAsync(command);
        }

        /// <summary>
        /// <para>
        /// This command encrypts the given data using the AES algorithm with the predefined with command Aes
        /// Setkey.
        /// </para>
        /// <para>
        /// This function uses CBC encryption mode.
        /// </para>
        /// </summary>
        /// <param name="data">
        /// <para>
        /// Data to be encrypted
        /// </para>
        /// <para>
        /// Maximum size is 16 bytes, will be zero padded if less.
        /// </para>
        /// </param>
        /// <returns></returns>
        public async Task<byte[]> EncryptAsync(byte[] data)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.AesEncrypt;
            var length = data.GetByteLength();
            var payload = new byte[1 + length];
            payload[0] = length;
            Array.Copy(data, payload, length);
            var command = new Message(type, @class, id, null);
            var response = await WriteAsync(command);
            var encryptedLength = response.Payload[0];
            var encrypted = new byte[encryptedLength];
            Array.Copy(response.Payload, 1, encrypted, 0, encryptedLength);
            return encrypted;
        }

        /// <summary>
        /// <para>
        /// This command decrypts the given data using the AES algorithm with the predefined key set with command Aes
        /// Setkey.
        /// </para>
        /// <para>
        /// This function uses CBC encryption mode.
        /// </para>
        /// </summary>
        /// <param name="data">
        /// <para>
        /// Data to be decrypted
        /// </para>
        /// <para>
        /// Maximum size is 16 bytes, will be zero padded if less.
        /// </para>
        /// </param>
        /// <returns></returns>
        public async Task<byte[]> DecryptAsync(byte[] data)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.AesDecrypt;
            var length = data.GetByteLength();
            var payload = new byte[1 + length];
            payload[0] = length;
            Array.Copy(data, payload, length);
            var command = new Message(type, @class, id, null);
            var response = await WriteAsync(command);
            var decryptedLength = response.Payload[0];
            var decrypted = new byte[decryptedLength];
            Array.Copy(response.Payload, 1, decrypted, 0, decryptedLength);
            return decrypted;
        }

        /// <summary>
        /// This command reads the enumeration status of USB device.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GetUsbEnumeratedAsync()
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.UsbEnumerationStatusGet;
            var command = new Message(type, @class, id);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
            var enumerated = response.Payload[2] == 1;
            return enumerated;
        }

        /// <summary>
        /// This command returns CRC-16 (polynomial X + X + X + 1) from bootloader. 
        /// </summary>
        /// <returns></returns>
        public async Task<ushort> GetBootloaderCrcAsync()
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.GetBootloader;
            var command = new Message(type, @class, id);
            var response = await WriteAsync(command);
            var crc = BitConverter.ToUInt16(response.Payload, 0);
            return crc;
        }

        /// <summary>
        /// <para>
        /// This command disables USB (if USB is enabled in module configuration), waits time delay in blocking mode and
        /// after that resets Bluetooth module.This command does not have a response, but the following event will be the
        /// normal boot event (system_boot) or the DFU boot event (dfu_boot) if the DFU option is used and UART
        /// bootloader is installed.
        /// </para>
        /// <para>
        /// There are three available bootloaders: USB for DFU upgrades using the USB-DFU protocol over the USB
        /// interface, UART for DFU upgrades using the BGAPI protocol over the UART interface, and OTA for the Overthe-Air upgrades.
        /// </para>
        /// </summary>
        /// <param name="mode">Whether or not to boot into DFU mode.</param>
        /// <param name="delay">Delay reset in milliseconds</param>
        public void DelayReset(BGBootMode mode, ushort delay)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.System;
            var id = (byte)SystemCommand.DelayReset;
            var delayArray = BitConverter.GetBytes(delay);
            var payload = new byte[3];
            payload[0] = (byte)mode;
            Array.Copy(delayArray, 0, payload, 1, 2);
            var command = new Message(type, @class, id, payload);
            var data = command.ToBytes();
            _serial.Write(data, 0, data.Length);
        }

        #endregion

        #region Commands - Flash

        #endregion

        #region Commands - Attribute Client

        /// <summary>
        /// <para>
        /// This command can be used to find specific attributes on a remote device based on their 16-bit UUID value and
        /// value.The search can be limited by a starting and ending handle values.
        /// </para>
        /// <para>
        /// The command returns the handles of all attributes matching the type (UUID) and value.
        /// </para>
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <param name="start">First requested handle number</param>
        /// <param name="end">Last requested handle number</param>
        /// <param name="uuid">2 octet UUID to find</param>
        /// <param name="value">Attribute value to find</param>
        /// <returns></returns>
        public async Task FindByTypeValueAsync(byte connection, ushort start, ushort end, ushort uuid, byte[] value)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.FindByTypeValue;
            var startArray = BitConverter.GetBytes(start);
            var endArray = BitConverter.GetBytes(end);
            var uuidArray = BitConverter.GetBytes(uuid);
            var payload = new byte[7 + value.Length];
            payload[0] = connection;
            Array.Copy(startArray, 0, payload, 1, 2);
            Array.Copy(endArray, 0, payload, 3, 2);
            Array.Copy(uuidArray, 0, payload, 5, 2);
            Array.Copy(value, 0, payload, 7, value.Length);
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// <para>
        /// This command reads the value of each attribute of a given type and in a given handle range.
        /// </para>
        /// <para>
        /// The command is typically used for primary (UUID: 0x2800) and secondary (UUID: 0x2801) service discovery.
        /// </para>
        /// <para>
        /// Discovered services are reported by Group Found event.
        /// </para>
        /// <para>
        /// Finally when the procedure is completed a Procedure Completed event is generated.
        /// </para>
        /// </summary>
        /// <param name="connection">Connection Handle</param>
        /// <param name="start">First requested handle number</param>
        /// <param name="end">Last requested handle number</param>
        /// <param name="uuid">Group UUID to find</param>
        /// <returns></returns>
        public async Task ReadByGroupTypeAsync(byte connection, ushort start, ushort end, byte[] uuid)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.ReadByGroupType;
            var startArray = BitConverter.GetBytes(start);
            var endArray = BitConverter.GetBytes(end);
            var payload = new byte[5 + uuid.Length];
            payload[0] = connection;
            Array.Copy(startArray, 0, payload, 1, 2);
            Array.Copy(endArray, 0, payload, 3, 2);
            Array.Copy(uuid, 0, payload, 5, uuid.Length);
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// <para>
        /// The command reads the value of each attribute of a given type (UUID) and in a given attribute handle range.
        /// </para>
        /// <para>
        /// The command can for example be used to discover the characteristic declarations (UUID: 0x2803) within a
        /// service.
        /// </para>
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <param name="start">First attribute handle</param>
        /// <param name="end">Last attribute handle</param>
        /// <param name="uuid">Attribute type (UUID)</param>
        /// <returns></returns>
        public async Task ReadByTypeAsync(byte connection, ushort start, ushort end, byte[] uuid)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.ReadByType;
            var startArray = BitConverter.GetBytes(start);
            var endArray = BitConverter.GetBytes(end);
            var payload = new byte[5 + uuid.Length];
            payload[0] = connection;
            Array.Copy(startArray, 0, payload, 1, 2);
            Array.Copy(endArray, 0, payload, 3, 2);
            Array.Copy(uuid, 0, payload, 5, uuid.Length);
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// This command is used to discover attribute handles and their types (UUIDs) in a given handle range.
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <param name="start">First attribute handle</param>
        /// <param name="end">Last attribute handle</param>
        /// <returns></returns>
        public async Task FindInformationAsync(byte connection, ushort start, ushort end)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.FindInformation;
            var startArray = BitConverter.GetBytes(start);
            var endArray = BitConverter.GetBytes(end);
            var payload = new byte[5];
            payload[0] = connection;
            Array.Copy(startArray, 0, payload, 1, 2);
            Array.Copy(endArray, 0, payload, 3, 2);
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// <para>
        /// This command reads a remote attribute's value with the given handle. Read by handle can be used to read
        /// attributes up to 22 bytes long.
        /// </para>
        /// <para>
        /// For longer attributes Read Long command must be used.
        /// </para>
        /// </summary>
        /// <param name="connection">Connection Handle</param>
        /// <param name="handle">Attribute handle</param>
        /// <returns></returns>
        public async Task ReadByHandleAsync(byte connection, ushort handle)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.ReadByHandle;
            var handleArray = BitConverter.GetBytes(handle);
            var payload = new byte[3];
            payload[0] = connection;
            Array.Copy(handleArray, 0, payload, 1, 2);
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// <para>
        /// This command can be used to write an attributes value on a remote device. In order to write the value of an
        /// attribute a Bluetooth connection must exists and you need to know the handle of the attribute you want to write.
        /// </para>
        /// <para>
        /// A successful attribute write will be acknowledged by the remote device and this will generate an event
        /// attclient_procedure_completed.The acknowledgement should happen within a 30 second window or otherwise
        /// the Bluetooth connection will be dropped.
        /// </para>
        /// <para>
        /// This command should be used for writing data to characteristic with property write="true".
        /// </para>
        /// <para>
        /// The data payload for the Attribute Write command can be up to 20 bytes.
        /// </para>
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <param name="handle">Attribute handle to write to</param>
        /// <param name="data">Attribute value</param>
        /// <returns></returns>
        public async Task AttributeWriteAsync(byte connection, ushort handle, byte[] data)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.AttributeWrite;
            var attributeArray = BitConverter.GetBytes(handle);
            var payload = new byte[3 + data.Length];
            payload[0] = connection;
            Array.Copy(attributeArray, 0, payload, 1, 2);
            Array.Copy(data, 0, payload, 3, data.Length);
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// <para>
        /// Writes the value of a remote devices attribute. The handle and the new value of the attribute are gives as
        /// parameters.
        /// </para>
        /// <para>
        /// Write command will not be acknowledged by the remote device unlike Attribute Write. This command
        /// should be used for writing data to characteristic with property write_no_response="true".
        /// </para>
        /// <para>
        /// The maximum data payload for Write Command is 20 bytes.
        /// </para>
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <param name="handle">Attribute handle to write</param>
        /// <param name="data">Value for the attribute</param>
        /// <returns></returns>
        public async Task WriteCommandAsync(byte connection, ushort handle, byte[] data)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.WriteCommand;
            var attributeArray = BitConverter.GetBytes(handle);
            var payload = new byte[3 + data.Length];
            payload[0] = connection;
            Array.Copy(attributeArray, 0, payload, 1, 2);
            Array.Copy(data, 0, payload, 3, data.Length);
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// <para>
        /// This command can be used to send a acknowledge a received indication from a remote device. This function
        /// allows the application to manually confirm the indicated values instead of the Bluetooth Low Energy stack
        /// automatically doing it.The benefit of this is extra reliability since the application can for example store the
        /// received value on the flash memory before confirming the indication to the remote device.
        /// </para>
        /// <para>
        /// In order to use this feature the manual indication acknowledgements must be enabled to the
        /// application configuration file(config.xml).
        /// </para>
        /// </summary>
        /// <param name="connection">Connection Handle</param>
        /// <returns></returns>
        public async Task IndicateConfirmAsync(byte connection)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.IndicateConfirm;
            var payload = new[] { connection };
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
        /// <para>
        /// This command can be used to read long attribute values, which are longer than 22 bytes and cannot be read
        /// with a simple Read by Handle command.
        /// </para>
        /// The command starts a procedure, where the client first sends a normal read command to the server and if the
        /// returned attribute value length is equal to MTU, the client will send further read long read requests until rest of
        /// the attribute is read.
        /// <para>
        /// </para>
        /// </summary>
        /// <param name="connection">Connection Handle</param>
        /// <param name="handle">Attribute handle</param>
        /// <returns></returns>
        public async Task ReadLongAsync(byte connection, ushort handle)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.ReadLong;
            var handleArray = BitConverter.GetBytes(handle);
            var payload = new byte[3];
            payload[0] = connection;
            Array.Copy(handleArray, 0, payload, 1, 2);
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// <para>
        /// This command will send a prepare write request to a remote device for queued writes. Queued writes can for
        /// example be used to write large attribute values by transmitting the data in chunks using prepare write
        /// command.
        /// </para>
        /// <para>
        /// Once the data has been transmitted with multiple prepare write commands the write must then be executed or
        /// canceled with Execute Write command, which if acknowledged by the remote device triggers a Procedure
        /// Completed event.
        /// </para>
        /// <para>
        /// The example below shows how this approach can be used to write a 30-byte characteristic value:
        /// </para>
        /// <para>1. attclient_prepare_write(...., partial data)</para>
        /// <para>2. wait for rsp_attclient_prepare_write</para>
        /// <para>3. wait for evt_attclient_procedure_completed</para>
        /// <para>4. attclient_prepare_write(...., partial data)</para>
        /// <para>5. wait for rsp_attclient_prepare_write</para>
        /// <para>6. wait for evt_attclient_procedure_completed</para>
        /// <para>7. attclient_execute_write(1)</para>
        /// <para>8. wait for rsp_attclient_prepare_write</para>
        /// <para>9. wait for evt_attclient_procedure_completed</para>
        /// <para>
        /// It is not mandatory for an ATT server to support this command. It is only recommended to use this
        /// command to write long-attributes which do not fit in single ATT packet.
        /// </para>
        /// </summary>
        /// <param name="connection">Connection Handle</param>
        /// <param name="handle">Attribute handle</param>
        /// <param name="offset">Offset to write to</param>
        /// <param name="data">
        /// <para>
        /// Data to write
        /// </para>
        /// <para>
        /// Maximum amount of data that can be sent in single command is 18 bytes.
        /// </para>
        /// </param>
        /// <returns></returns>
        public async Task PrepareWriteAsync(byte connection, ushort handle, ushort offset, byte[] data)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.PrepareWirte;
            var handleArray = BitConverter.GetBytes(handle);
            var offsetArray = BitConverter.GetBytes(offset);
            var payload = new byte[5 + data.Length];
            payload[0] = connection;
            Array.Copy(handleArray, 0, payload, 1, 2);
            Array.Copy(offsetArray, 0, payload, 3, 2);
            Array.Copy(data, 0, payload, 5, data.Length);
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// This command can be used to execute or cancel a previously queued prepare_write command on a remote
        /// device.
        /// </summary>
        /// <param name="connection">Connection Handle</param>
        /// <param name="commit">
        /// <para>1: commits queued writes</para>
        /// <para>0: cancels queued writes</para>
        /// </param>
        /// <returns></returns>
        public async Task ExecuteWriteAsync(byte connection, byte commit)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.ExecuteWrite;
            var payload = new[] { connection, commit };
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// This command can be used to read multiple attributes from a server.
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <param name="handles">List of attribute handles to read from the remote device</param>
        /// <returns></returns>
        public async Task ReadMultipleAsync(byte connection, byte[] handles)
        {
            var type = (byte)MessageType.Command;
            var @class = (byte)MessageClass.AttributeClient;
            var id = (byte)AttributeClientCommand.ReadMultiple;
            var payload = new byte[1 + handles.Length];
            payload[0] = connection;
            Array.Copy(handles, 0, payload, 1, handles.Length);
            var command = new Message(type, @class, id, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        #endregion

        #region Commands - Generic Access Profile

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
        public async Task SetDiscoveryParametersAsync(ushort interval, ushort window, bool active)
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
            var command = new Message(type, @class, id);
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
            var connection = response.Payload[2];
            return connection;
        }

        #endregion

        #region Events - System

        /// <summary>
        /// <para>
        /// This event is produced when the device boots up and is ready to receive commands
        /// </para>
        /// <para>
        /// This event is not sent over USB interface.
        /// </para>
        /// </summary>
        public event EventHandler<BGVersionEventArgs> SystemBoot;
        /// <summary>
        /// This event is generated if the receive (incoming) buffer of the endpoint has been filled with a number of bytes
        /// equal or higher than the value defined by the command Endpoint Set Watermarks. Data from the receive buffer
        /// can then be read(and consequently cleared) with the command Endpoint Rx.
        /// </summary>
        public event EventHandler<BGWatermarkEventArgs> EndpointWatermarkReceived;
        /// <summary>
        /// This event is generated when the transmit (outgoing) buffer of the endpoint has free space for a number of
        /// bytes equal or higher than the value defined by the command Endpoint Set Watermarks.When there is enough
        /// free space, data can be sent out of the endpoint by the command Endpoint Tx.
        /// </summary>
        public event EventHandler<BGWatermarkEventArgs> EndpointWatermarkWritten;
        public event EventHandler<BGScriptFailureEventArgs> ScriptFailed;
        /// <summary>
        /// <para>
        /// This error is produced when no valid license key found form the Bluetooth Low Energy hardware. When
        /// there is no valid license key the Bluetooth radio will not be operational.
        /// </para>
        /// <para>
        /// A new license key can be requested from the Bluegiga Technical Support.
        /// </para>
        /// </summary>
        public event EventHandler NoLicenseKey;
        public event EventHandler<BGErrorEventArgs> ProtocolError;
        /// <summary>
        /// Event is generated when USB enumeration status has changed. This event can be triggered by plugging
        /// module to USB host port or by USB device re-enumeration on host machine.
        /// </summary>
        public event EventHandler<BGUsbEnumeratedEventArgs> UsbEnumeratedChanged;

        private void OnSystemEventAnalyzed(Message message)
        {
            var @event = (SystemEvent)message.Id;
            switch (@event)
            {
                case SystemEvent.Boot:
                    {
                        var major = BitConverter.ToUInt16(message.Payload, 0);
                        var minor = BitConverter.ToUInt16(message.Payload, 2);
                        var patch = BitConverter.ToUInt16(message.Payload, 4);
                        var build = BitConverter.ToUInt16(message.Payload, 6);
                        var linkLayer = BitConverter.ToUInt16(message.Payload, 8);
                        var protocol = message.Payload[10];
                        var hardware = message.Payload[11];
                        var version = new BGVersion(major, minor, patch, build, linkLayer, protocol, hardware);
                        var eventArgs = new BGVersionEventArgs(version);
                        SystemBoot?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.EndpointWatermarkRX:
                    {
                        var endpoint = (BGEndpoint)message.Payload[0];
                        var size = message.Payload[1];
                        var eventArgs = new BGWatermarkEventArgs(endpoint, size);
                        EndpointWatermarkReceived?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.EndpointWatermarkTX:
                    {
                        var endpoint = (BGEndpoint)message.Payload[0];
                        var size = message.Payload[1];
                        var eventArgs = new BGWatermarkEventArgs(endpoint, size);
                        EndpointWatermarkWritten?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.ScriptFailure:
                    {
                        var address = BitConverter.ToUInt16(message.Payload, 0);
                        var errorCode = BitConverter.ToUInt16(message.Payload, 2);
                        var eventArgs = new BGScriptFailureEventArgs(address, errorCode);
                        ScriptFailed?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.NoLicenseKey:
                    {
                        NoLicenseKey?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                case SystemEvent.ProtocolError:
                    {
                        var errorCode = BitConverter.ToUInt16(message.Payload, 0);
                        var eventArgs = new BGErrorEventArgs(errorCode);
                        ProtocolError?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.UsbEnumerated:
                    {
                        var enumerated = message.Payload[0] == 1;
                        var eventArgs = new BGUsbEnumeratedEventArgs(enumerated);
                        UsbEnumeratedChanged?.Invoke(this, eventArgs);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        #endregion

        #region Events - Flash

        #endregion

        #region Events - Attribute Client

        /// <summary>
        /// <para>
        /// This event is produced at the GATT server side when an attribute is successfully indicated to the GATT client.
        /// </para>
        /// <para>
        /// This means the event is only produced at the GATT server if the indication is acknowledged by the GATT client
        /// (the remote device).
        /// </para>
        /// </summary>
        public event EventHandler<BGAttributeEventArgs> Indicated;
        /// <summary>
        /// <para>
        /// This event is produced at the GATT client when an attribute protocol event is completed a and new operation
        /// can be issued.
        /// </para>
        /// <para>
        /// This event is for example produced after an Attribute Write command is successfully used to write a value to a
        /// remote device.
        /// </para>
        /// </summary>
        public event EventHandler<BGProcedureCompleteEventArgs> ProcedureCompleted;
        /// <summary>
        /// This event is produced when an attribute group (a service) is found. Typically this event is produced after Read
        /// by Group Type command.
        /// </summary>
        public event EventHandler<GroupEventArgs> GroupFound;
        /// <summary>
        /// This event is generated when characteristics type mappings are found. This happens yypically after Find
        /// Information command has been issued to discover all attributes of a service.
        /// </summary>
        public event EventHandler<InformationEventArgs> FindInformationFound;
        /// <summary>
        /// This event is produced at the GATT client side when an attribute value is passed from the GATT server to the
        /// GATT client.This event is for example produced after a successful Read by Handle operation or when an
        /// attribute is indicated or notified by the remote device.
        /// </summary>
        public event EventHandler<AttributeValueEventArgs> AttributeValue;
        public event EventHandler<MultipleResponseEventArgs> ReadMultipleResopnse;

        private void OnAttributeClientEventAnalyzed(Message message)
        {
            var @event = (AttributeClientEvent)message.Id;
            switch (@event)
            {
                case AttributeClientEvent.Indicated:
                    {
                        var connection = message.Payload[0];
                        var attribute = BitConverter.ToUInt16(message.Payload, 1);
                        var eventArgs = new BGAttributeEventArgs(connection, attribute);
                        Indicated?.Invoke(this, eventArgs);
                        break;
                    }
                case AttributeClientEvent.ProcedureCompleted:
                    {
                        var connection = message.Payload[0];
                        var errorCode = BitConverter.ToUInt16(message.Payload, 1);
                        var attribute = BitConverter.ToUInt16(message.Payload, 3);
                        var eventArgs = new BGProcedureCompleteEventArgs(connection, errorCode, attribute);
                        ProcedureCompleted?.Invoke(this, eventArgs);
                        break;
                    }
                case AttributeClientEvent.GroupFound:
                    {
                        var connection = message.Payload[0];
                        var start = BitConverter.ToUInt16(message.Payload, 1);
                        var end = BitConverter.ToUInt16(message.Payload, 3);
                        var uuid = message.Payload.Skip(5).ToArray();
                        var eventArgs = new BGGroupEventArgs(connection, start, end, uuid);
                        GroupFound?.Invoke(this, eventArgs);
                        break;
                    }
                case AttributeClientEvent.AttributeFound:
                    {
                        var connection = message.Payload[0];
                        var chrdecl = BitConverter.ToUInt16(message.Payload, 1);
                        var value = BitConverter.ToUInt16(message.Payload, 3);
                        var properties = message.Payload[5];
                        var uuid = message.Payload.Skip(6).ToArray();
                        break;
                    }
                case AttributeClientEvent.FindInformationFound:
                    {
                        var connection = message.Payload[0];
                        var characteristic = BitConverter.ToUInt16(message.Payload, 1);
                        var uuid = message.Payload.Skip(3).ToArray();
                        break;
                    }
                case AttributeClientEvent.AttributeValue:
                    {
                        var connection = message.Payload[0];
                        var attribute = BitConverter.ToUInt16(message.Payload, 1);
                        var type = message.Payload[3];
                        var value = message.Payload.Skip(4).ToArray();
                        break;
                    }
                case AttributeClientEvent.ReadMultipleResponse:
                    {
                        var connection = message.Payload[0];
                        var handles = message.Payload.Skip(1).ToArray();
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        #endregion

        #region Events

        public event EventHandler<BGDiscoveryEventArgs> Discovered;

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
            var @event = (GenericAccessProfileEvent)message.Id;
            switch (@event)
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
                        var eventArgs = new BGDiscoveryEventArgs(discovery);
                        Discovered?.Invoke(this, eventArgs);
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
