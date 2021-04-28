using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;

namespace BGLib.API
{
    public class BGAPI
    {
        private readonly ICommunicator _communicator;
        private readonly MessageAnalyzer _analyzer;

        protected BGAPI(ICommunicator communicator)
        {
            _communicator = communicator;
            _analyzer = new MessageAnalyzer();

            _communicator.ValueChanged += OnValueChanged;
            _analyzer.Analyzed += OnAnalyzed;
        }

        private void OnValueChanged(object sender, ValueEventArgs e)
        {
            foreach (var value in e.Value)
            {
                _analyzer.Analyze(value);
            }
        }

        private void OnAnalyzed(object sender, MessageEventArgs e)
        {
            var type = (MessageType)e.Message.Type;
            if (type != MessageType.Event)
                return;
            var @class = (MessageClass)e.Message.Class;
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

        private void Write(Message command)
        {
            var value = command.ToArray();
            _communicator.Write(value);
        }

        private async Task<Message> WriteAsync(Message command)
        {
            var writeTCS = new TaskCompletionSource<Message>();
            var onAnalyzed = new EventHandler<MessageEventArgs>((s, e) =>
            {
                var type = (MessageType)e.Message.Type;
                if (type != MessageType.Response ||
                    e.Message.Class != command.Class ||
                    e.Message.Id != command.Id)
                {
                    return;
                }
                writeTCS.TrySetResult(e.Message);
            });
            _analyzer.Analyzed += onAnalyzed;
            try
            {
                var value = command.ToArray();
                _communicator.Write(value);
                return await writeTCS.Task;
            }
            finally
            {
                _analyzer.Analyzed -= onAnalyzed;
            }
        }

        private Message GetCommand(MessageClass @class, byte id, byte[] payload = null)
        {
            var type = (byte)MessageType.Command;
            var classValue = (byte)@class;
            return new Message(type, classValue, id, payload);
        }

        #region Commands - System

        private Message GetSystemCommand(SystemCommand command, byte[] payload = null)
        {
            var id = (byte)command;
            return GetCommand(MessageClass.System, id, payload);
        }

        /// <summary>
        /// This command resets the local device immediately. The command does not have a response.
        /// </summary>
        /// <param name="mode">Selects the boot mode</param>
        public void Reset(BootMode mode)
        {
            var modeValue = (byte)mode;
            var payload = new[] { modeValue };
            var command = GetSystemCommand(SystemCommand.Reset, payload);
            Write(command);
        }

        /// <summary>
        /// This command can be used to test if the local device is functional. Similar to a typical "AT" -> "OK" test.
        /// </summary>
        /// <returns></returns>
        public async Task HelloAsync()
        {
            var command = GetSystemCommand(SystemCommand.Hello);
            await WriteAsync(command);
        }

        /// <summary>
        /// This command reads the local device's public Bluetooth address.
        /// </summary>
        /// <returns></returns>
        public async Task<Address> GetAddressAsync()
        {
            var command = GetSystemCommand(SystemCommand.AddressGet);
            var response = await WriteAsync(command);
            var address = new Address(AddressType.Public, response.Payload);
            return address;
        }

        /// <summary>
        /// Read packet counters and resets them, also returns available packet buffers.
        /// </summary>
        /// <returns></returns>
        public async Task<Counters> GetCountersAsync()
        {
            var command = GetSystemCommand(SystemCommand.GetCounters);
            var response = await WriteAsync(command);
            var transmitted = response.Payload[0];
            var retransmitted = response.Payload[1];
            var receivedOK = response.Payload[2];
            var receivedError = response.Payload[3];
            var available = response.Payload[4];
            var counters = new Counters(transmitted, retransmitted, receivedOK, receivedError, available);
            return counters;
        }

        /// <summary>
        /// This command reads the number of supported connections from the local device.
        /// </summary>
        /// <returns>Max supported connections</returns>
        public async Task<byte> GetMaxConnectionsAsync()
        {
            var command = GetSystemCommand(SystemCommand.GetConnections);
            var response = await WriteAsync(command);
            var maxConnections = response.Payload[0];
            return maxConnections;
        }

        /// <summary>
        /// This command reads the local devices software and hardware versions.
        /// </summary>
        /// <returns></returns>
        public async Task<Version> GetVersionAsync()
        {
            var command = GetSystemCommand(SystemCommand.GetInfo);
            var response = await WriteAsync(command);
            var major = BitConverter.ToUInt16(response.Payload, 0);
            var minor = BitConverter.ToUInt16(response.Payload, 2);
            var patch = BitConverter.ToUInt16(response.Payload, 4);
            var build = BitConverter.ToUInt16(response.Payload, 6);
            var linkLayer = BitConverter.ToUInt16(response.Payload, 8);
            var protocol = response.Payload[10];
            var hardware = response.Payload[11];
            var version = new Version(major, minor, patch, build, linkLayer, protocol, hardware);
            return version;
        }

        /// <summary>
        /// Send data to endpoint, error is returned if endpoint does not have enough space
        /// </summary>
        /// <param name="endpoint">Endpoint index to send data to</param>
        /// <param name="data">data to send</param>
        /// <returns></returns>
        public async Task WriteEndPointAsync(Endpoint endpoint, byte[] data)
        {
            var length = data.GetByteLength();
            var payload = new byte[2 + length];
            payload[0] = (byte)endpoint;
            payload[1] = length;
            Array.Copy(data, 0, payload, 2, length);
            var command = GetSystemCommand(SystemCommand.EndpointTX, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
        public async Task AppendWhitelistAsync(Address address, AddressType addressType)
        {
            var payload = new byte[7];
            Array.Copy(address.RawValue, payload, 6);
            payload[6] = (byte)addressType;
            var command = GetSystemCommand(SystemCommand.WhitelistAppend, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
        public async Task RemoveWhitelistAsync(Address address, AddressType addressType)
        {
            var payload = new byte[7];
            Array.Copy(address.RawValue, payload, 6);
            payload[6] = (byte)addressType;
            var command = GetSystemCommand(SystemCommand.WhitelistRemove, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var command = GetSystemCommand(SystemCommand.WhitelistClear);
            await WriteAsync(command);
        }

        /// <summary>
        /// Read data from an endpoint (i.e., data souce, e.g., UART), error is returned if endpoint does not have enough
        /// data.
        /// </summary>
        /// <param name="endpoint">Endpoint index to read data from</param>
        /// <param name="size">Size of data to read</param>
        /// <returns></returns>
        public async Task<byte[]> ReadEndpointAsync(Endpoint endpoint, byte size)
        {
            var payload = new byte[2];
            payload[0] = (byte)endpoint;
            payload[1] = size;
            var command = GetSystemCommand(SystemCommand.EndpointRX, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
        public async Task SetEndpointWatermarksAsync(Endpoint endpoint, byte receive, byte transmit)
        {
            var payload = new byte[3];
            payload[0] = (byte)endpoint;
            payload[1] = receive;
            payload[2] = transmit;
            var command = GetSystemCommand(SystemCommand.EndpointSetWatermarks, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var length = key.GetByteLength();
            var payload = new byte[1 + length];
            payload[0] = length;
            Array.Copy(key, payload, length);
            var command = GetSystemCommand(SystemCommand.AesSetKey, payload);
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
            var length = data.GetByteLength();
            var payload = new byte[1 + length];
            payload[0] = length;
            Array.Copy(data, payload, length);
            var command = GetSystemCommand(SystemCommand.AesEncrypt, payload);
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
            var length = data.GetByteLength();
            var payload = new byte[1 + length];
            payload[0] = length;
            Array.Copy(data, payload, length);
            var command = GetSystemCommand(SystemCommand.AesDecrypt, payload);
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
            var command = GetSystemCommand(SystemCommand.UsbEnumerationStatusGet);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
                throw new BGException(message);
            }
            var enumerated = response.Payload[2] == 1;
            return enumerated;
        }

        /// <summary>
        /// This command returns CRC-16 (polynomial X + X + X + 1) from bootloader. 
        /// </summary>
        /// <returns></returns>
        public async Task<ushort> GetBootloaderAsync()
        {
            var command = GetSystemCommand(SystemCommand.GetBootloader);
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
        public void DelayReset(BootMode mode, ushort delay)
        {
            var delayValue = BitConverter.GetBytes(delay);
            var payload = new byte[3];
            payload[0] = (byte)mode;
            Array.Copy(delayValue, 0, payload, 1, 2);
            var command = GetSystemCommand(SystemCommand.DelayReset, payload);
            Write(command);
        }

        #endregion

        #region Commands - Flash

        private Message GetFlashCommand(PersistentStoreCommand command, byte[] payload = null)
        {
            var id = (byte)command;
            return GetCommand(MessageClass.PersistentStore, id, payload);
        }

        /// <summary>
        /// This command defragments the Persistent Store.
        /// </summary>
        /// <returns></returns>
        public async Task DefragAsync()
        {
            var command = GetFlashCommand(PersistentStoreCommand.PSDefrag);
            await WriteAsync(command);
        }

        /// <summary>
        /// This command dumps all Persistent Store keys.
        /// </summary>
        /// <returns></returns>
        public async Task DumpAsync()
        {
            var command = GetFlashCommand(PersistentStoreCommand.PSDump);
            await WriteAsync(command);
        }

        /// <summary>
        /// <para>
        /// This command erases all Persistent Store keys.
        /// </para>
        /// <para>
        /// The software needs to be restarted after using this command. During the reset the device will generate
        /// missing encryption keys and update bonding cache.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public async Task EraseAllAsync()
        {
            var command = GetFlashCommand(PersistentStoreCommand.PSEraseAll);
            await WriteAsync(command);
        }

        /// <summary>
        /// This command saves a Persistent Store (PS) key to the local device. The maximum size of a single PS-key is
        /// 32 bytes and a total of 128 keys are available.
        /// </summary>
        /// <param name="key">
        /// <para>Key to save.</para>
        /// <para>Values: 0x8000 to 0x807F can be used for persistent storage of user data</para>
        /// </param>
        /// <param name="value">Value of the key</param>
        /// <returns></returns>
        public async Task SaveAsync(ushort key, byte[] value)
        {
            if (key < 0x8000 || key > 0x807F)
            {
                var paramName = nameof(key);
                throw new ArgumentOutOfRangeException(paramName);
            }
            var keyValue = BitConverter.GetBytes(key);
            var length = value.GetByteLength();
            var payload = new byte[2 + length];
            Array.Copy(keyValue, payload, 2);
            Array.Copy(value, 0, payload, 2, length);
            var command = GetFlashCommand(PersistentStoreCommand.PSSave, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        public async Task LoadAsync(ushort key)
        {

        }

        public async Task EraseAsync() { }

        public async Task ErasePageAsync() { }

        public async Task WriteDataAsync() { }

        public async Task ReadDataAsync() { }

        #endregion

        #region Commands - Attribute Client

        private Message GetAttributeClientCommand(AttributeClientCommand command, byte[] payload = null)
        {
            var id = (byte)command;
            return GetCommand(MessageClass.AttributeClient, id, payload);
        }

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
            var startArray = BitConverter.GetBytes(start);
            var endArray = BitConverter.GetBytes(end);
            var uuidArray = BitConverter.GetBytes(uuid);
            var payload = new byte[7 + value.Length];
            payload[0] = connection;
            Array.Copy(startArray, 0, payload, 1, 2);
            Array.Copy(endArray, 0, payload, 3, 2);
            Array.Copy(uuidArray, 0, payload, 5, 2);
            Array.Copy(value, 0, payload, 7, value.Length);
            var command = GetAttributeClientCommand(AttributeClientCommand.FindByTypeValue, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var startArray = BitConverter.GetBytes(start);
            var endArray = BitConverter.GetBytes(end);
            var payload = new byte[5 + uuid.Length];
            payload[0] = connection;
            Array.Copy(startArray, 0, payload, 1, 2);
            Array.Copy(endArray, 0, payload, 3, 2);
            Array.Copy(uuid, 0, payload, 5, uuid.Length);
            var command = GetAttributeClientCommand(AttributeClientCommand.ReadByGroupType, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var startArray = BitConverter.GetBytes(start);
            var endArray = BitConverter.GetBytes(end);
            var payload = new byte[5 + uuid.Length];
            payload[0] = connection;
            Array.Copy(startArray, 0, payload, 1, 2);
            Array.Copy(endArray, 0, payload, 3, 2);
            Array.Copy(uuid, 0, payload, 5, uuid.Length);
            var command = GetAttributeClientCommand(AttributeClientCommand.ReadByType, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var startArray = BitConverter.GetBytes(start);
            var endArray = BitConverter.GetBytes(end);
            var payload = new byte[5];
            payload[0] = connection;
            Array.Copy(startArray, 0, payload, 1, 2);
            Array.Copy(endArray, 0, payload, 3, 2);
            var command = GetAttributeClientCommand(AttributeClientCommand.FindInformation, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var handleArray = BitConverter.GetBytes(handle);
            var payload = new byte[3];
            payload[0] = connection;
            Array.Copy(handleArray, 0, payload, 1, 2);
            var command = GetAttributeClientCommand(AttributeClientCommand.ReadByHandle, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var attributeArray = BitConverter.GetBytes(handle);
            var payload = new byte[3 + data.Length];
            payload[0] = connection;
            Array.Copy(attributeArray, 0, payload, 1, 2);
            Array.Copy(data, 0, payload, 3, data.Length);
            var command = GetAttributeClientCommand(AttributeClientCommand.AttributeWrite, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var attributeArray = BitConverter.GetBytes(handle);
            var payload = new byte[3 + data.Length];
            payload[0] = connection;
            Array.Copy(attributeArray, 0, payload, 1, 2);
            Array.Copy(data, 0, payload, 3, data.Length);
            var command = GetAttributeClientCommand(AttributeClientCommand.WriteCommand, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var payload = new[] { connection };
            var command = GetAttributeClientCommand(AttributeClientCommand.IndicateConfirm, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var handleArray = BitConverter.GetBytes(handle);
            var payload = new byte[3];
            payload[0] = connection;
            Array.Copy(handleArray, 0, payload, 1, 2);
            var command = GetAttributeClientCommand(AttributeClientCommand.ReadLong, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var handleArray = BitConverter.GetBytes(handle);
            var offsetArray = BitConverter.GetBytes(offset);
            var payload = new byte[5 + data.Length];
            payload[0] = connection;
            Array.Copy(handleArray, 0, payload, 1, 2);
            Array.Copy(offsetArray, 0, payload, 3, 2);
            Array.Copy(data, 0, payload, 5, data.Length);
            var command = GetAttributeClientCommand(AttributeClientCommand.PrepareWirte, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var payload = new[] { connection, commit };
            var command = GetAttributeClientCommand(AttributeClientCommand.ExecuteWrite, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
            var payload = new byte[1 + handles.Length];
            payload[0] = connection;
            Array.Copy(handles, 0, payload, 1, handles.Length);
            var command = GetAttributeClientCommand(AttributeClientCommand.ReadMultiple, payload);
            var response = await WriteAsync(command);
            //var connection = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        #endregion

        #region Commands - Generic Access Profile

        private Message GetGenericAccessProfileCommand(GenericAccessProfileCommand command, byte[] payload = null)
        {
            var id = (byte)command;
            return GetCommand(MessageClass.GenericAccessProfile, id, payload);
        }

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
            var intervalValue = BitConverter.GetBytes(interval);
            var windowValue = BitConverter.GetBytes(window);
            var payload = new byte[5];
            Array.Copy(intervalValue, 0, payload, 0, 2);
            Array.Copy(windowValue, 0, payload, 2, 2);
            payload[4] = active ? (byte)0x01 : (byte)0x00;
            var command = GetGenericAccessProfileCommand(GenericAccessProfileCommand.SetScanParameters, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// This command starts the GAP discovery procedure to scan for advertising devices i.e. to perform a device
        /// discovery.
        /// </summary>
        /// <param name="mode">GAP Discover modes</param>
        /// <returns></returns>
        public async Task StartDiscoveryAsync(DiscoverMode mode = DiscoverMode.Observation)
        {
            var modeValue = (byte)mode;
            var payload = new[] { modeValue };
            var command = GetGenericAccessProfileCommand(GenericAccessProfileCommand.Discover, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
                throw new BGException(message);
            }
        }

        /// <summary>
        /// This command ends the current GAP discovery procedure and stop the scanning of advertising devices.
        /// </summary>
        /// <returns></returns>
        public async Task StopDiscoveryAsync()
        {
            var command = GetGenericAccessProfileCommand(GenericAccessProfileCommand.EndProcedure);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
        public async Task<byte> ConnectAsync(Address address, ushort interval = 60, ushort timeout = 100, ushort latency = 0)
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
            var command = GetGenericAccessProfileCommand(GenericAccessProfileCommand.ConnectDirect, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                var message = BGUtil.GetMessage(errorCode);
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
        public event EventHandler<VersionEventArgs> SystemBoot;
        /// <summary>
        /// This event is generated if the receive (incoming) buffer of the endpoint has been filled with a number of bytes
        /// equal or higher than the value defined by the command Endpoint Set Watermarks. Data from the receive buffer
        /// can then be read(and consequently cleared) with the command Endpoint Rx.
        /// </summary>
        public event EventHandler<WatermarkEventArgs> EndpointWatermarkReceived;
        /// <summary>
        /// This event is generated when the transmit (outgoing) buffer of the endpoint has free space for a number of
        /// bytes equal or higher than the value defined by the command Endpoint Set Watermarks.When there is enough
        /// free space, data can be sent out of the endpoint by the command Endpoint Tx.
        /// </summary>
        public event EventHandler<WatermarkEventArgs> EndpointWatermarkWritten;
        public event EventHandler<ScriptFailureEventArgs> ScriptFailed;
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
        public event EventHandler<ErrorEventArgs> ProtocolError;
        /// <summary>
        /// Event is generated when USB enumeration status has changed. This event can be triggered by plugging
        /// module to USB host port or by USB device re-enumeration on host machine.
        /// </summary>
        public event EventHandler<UsbEnumeratedEventArgs> UsbEnumeratedChanged;

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
                        var version = new Version(major, minor, patch, build, linkLayer, protocol, hardware);
                        var eventArgs = new VersionEventArgs(version);
                        SystemBoot?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.EndpointWatermarkRX:
                    {
                        var endpoint = (Endpoint)message.Payload[0];
                        var size = message.Payload[1];
                        var eventArgs = new WatermarkEventArgs(endpoint, size);
                        EndpointWatermarkReceived?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.EndpointWatermarkTX:
                    {
                        var endpoint = (Endpoint)message.Payload[0];
                        var size = message.Payload[1];
                        var eventArgs = new WatermarkEventArgs(endpoint, size);
                        EndpointWatermarkWritten?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.ScriptFailure:
                    {
                        var address = BitConverter.ToUInt16(message.Payload, 0);
                        var errorCode = BitConverter.ToUInt16(message.Payload, 2);
                        var eventArgs = new ScriptFailureEventArgs(address, errorCode);
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
                        var eventArgs = new ErrorEventArgs(errorCode);
                        ProtocolError?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.UsbEnumerated:
                    {
                        var enumerated = message.Payload[0] == 1;
                        var eventArgs = new UsbEnumeratedEventArgs(enumerated);
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
        public event EventHandler<AttributeEventArgs> Indicated;
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
                        var eventArgs = new AttributeEventArgs(connection, attribute);
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

        public event EventHandler<DiscoveryEventArgs> Discovered;

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
                        var type = (DiscoveryType)message.Payload[1];
                        //var rawValue = new byte[6];
                        //Array.Copy(message.Payload, 2, rawValue, 0, rawValue.Length);
                        var rawValue = message.Payload.Skip(2).Take(6).ToArray();
                        var addressType = (AddressType)message.Payload[8];
                        var address = new Address(addressType, rawValue);
                        //var bond = message.Payload[9];
                        var dataLength = message.Payload[10];
                        var data = message.Payload.Skip(11).Take(dataLength).ToArray();
                        var advertisements = new List<Advertisement>();
                        for (int i = 0; i < data.Length; i++)
                        {
                            var advertisementLength = data[i];
                            var advertisementType = (AdvertisementType)data[i + 1];
                            var advertisementValue = data.Skip(i + 2).Take(advertisementLength - 1).ToArray();
                            var advertisement = new Advertisement(advertisementType, advertisementValue);
                            advertisements.Add(advertisement);
                            i += advertisementLength;
                        }
                        var discovery = new Discovery(rssi, type, address, advertisements);
                        var eventArgs = new DiscoveryEventArgs(discovery);
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

        #endregion
    }
}
