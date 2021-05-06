using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BGLib.API
{
    public class BGAPI
    {
        #region Fields

        private readonly ICommunicator _communicator;
        private readonly MessageAnalyzer _analyzer;

        #endregion

        #region Methods

        public BGAPI(ICommunicator communicator)
        {
            _communicator = communicator;
            _analyzer = new MessageAnalyzer();

            _communicator.ValueChanged += OnValueChanged;
            _analyzer.Analyzed += OnAnalyzed;
        }

        private void OnValueChanged(object sender, ValueEventArgs e)
        {
            _analyzer.Analyze(e.Value);
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
                case MessageClass.PS:
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
                case MessageClass.SM:
                    OnSMEventAnalyzed(e.Message);
                    break;
                case MessageClass.GAP:
                    OnGapEventAnalyzed(e.Message);
                    break;
                case MessageClass.Hardware:
                    OnHardwareEventAnalyzed(e.Message);
                    break;
                case MessageClass.DFU:
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

        #endregion

        #region Commands - System

        /// <summary>
        /// This command resets the local device immediately. The command does not have a response.
        /// </summary>
        /// <param name="mode">Selects the boot mode</param>
        public void Reset(BootMode mode)
        {
            var modeValue = (byte)mode;
            var payload = new[] { modeValue };
            var command = Util.GetSystemCommand(SystemCommand.Reset, payload);
            Write(command);
        }

        /// <summary>
        /// This command can be used to test if the local device is functional. Similar to a typical "AT" -> "OK" test.
        /// </summary>
        /// <returns></returns>
        public async Task HelloAsync()
        {
            var command = Util.GetSystemCommand(SystemCommand.Hello);
            await WriteAsync(command);
        }

        /// <summary>
        /// This command reads the local device's public Bluetooth address.
        /// </summary>
        /// <returns></returns>
        public async Task<Address> GetAddressAsync()
        {
            var command = Util.GetSystemCommand(SystemCommand.AddressGet);
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
            var command = Util.GetSystemCommand(SystemCommand.GetCounters);
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
            var command = Util.GetSystemCommand(SystemCommand.GetConnections);
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
            var command = Util.GetSystemCommand(SystemCommand.GetInfo);
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
            var payload = new byte[2 + data.Length];
            payload[0] = (byte)endpoint;
            payload[1] = data.GetByteLength();
            Array.Copy(data, 0, payload, 2, data.Length);
            var command = Util.GetSystemCommand(SystemCommand.EndpointTX, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        /// <returns></returns>
        public async Task AppendWhitelistAsync(Address address)
        {
            var payload = new byte[7];
            Array.Copy(address.RawValue, payload, 6);
            payload[6] = (byte)address.Type;
            var command = Util.GetSystemCommand(SystemCommand.WhitelistAppend, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        /// <returns></returns>
        public async Task RemoveWhitelistAsync(Address address)
        {
            var payload = new byte[7];
            Array.Copy(address.RawValue, payload, 6);
            payload[6] = (byte)address.Type;
            var command = Util.GetSystemCommand(SystemCommand.WhitelistRemove, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
            var command = Util.GetSystemCommand(SystemCommand.WhitelistClear);
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
            var command = Util.GetSystemCommand(SystemCommand.EndpointRX, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
            var command = Util.GetSystemCommand(SystemCommand.EndpointSetWatermarks, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
            var payload = new byte[1 + key.Length];
            payload[0] = key.GetByteLength();
            Array.Copy(key, payload, key.Length);
            var command = Util.GetSystemCommand(SystemCommand.AesSetKey, payload);
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
            var payload = new byte[1 + data.Length];
            payload[0] = data.GetByteLength();
            Array.Copy(data, payload, data.Length);
            var command = Util.GetSystemCommand(SystemCommand.AesEncrypt, payload);
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
            var payload = new byte[1 + data.Length];
            payload[0] = data.GetByteLength();
            Array.Copy(data, payload, data.Length);
            var command = Util.GetSystemCommand(SystemCommand.AesDecrypt, payload);
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
            var command = Util.GetSystemCommand(SystemCommand.UsbEnumerationStatusGet);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
            var command = Util.GetSystemCommand(SystemCommand.GetBootloader);
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
            var command = Util.GetSystemCommand(SystemCommand.DelayReset, payload);
            Write(command);
        }

        #endregion

        #region Commands - PS

        /// <summary>
        /// This command defragments the Persistent Store.
        /// </summary>
        /// <returns></returns>
        public async Task DefragAsync()
        {
            var command = Util.GetPSCommand(PSCommand.PSDefrag);
            await WriteAsync(command);
        }

        /// <summary>
        /// This command dumps all Persistent Store keys.
        /// </summary>
        /// <returns></returns>
        public async Task DumpAsync()
        {
            var command = Util.GetPSCommand(PSCommand.PSDump);
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
            var command = Util.GetPSCommand(PSCommand.PSEraseAll);
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
            var keyValue = BitConverter.GetBytes(key);
            var payload = new byte[3 + value.Length];
            Array.Copy(keyValue, payload, 2);
            payload[2] = value.GetByteLength();
            Array.Copy(value, 0, payload, 3, value.Length);
            var command = Util.GetPSCommand(PSCommand.PSSave, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command reads a Persistent Store key from the local device.
        /// </summary>
        /// <param name="key">
        /// <para>Key to load</para>
        /// <para>Values: 0x8000 to 0x807F</para>
        /// </param>
        /// <returns>Key's value</returns>
        public async Task<byte[]> LoadAsync(ushort key)
        {
            var payload = BitConverter.GetBytes(key);
            var command = Util.GetPSCommand(PSCommand.PSLoad, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
            var length = response.Payload[2];
            var value = new byte[length];
            Array.Copy(response.Payload, 3, value, 0, length);
            return value;
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
        public async Task EraseAsync()
        {
            var command = Util.GetPSCommand(PSCommand.PSEraseAll);
            await WriteAsync(command);
        }

        /// <summary>
        /// <para>
        /// The command erases a flash page which is allocated for user-data. Every page on the flash is 2kB in size
        /// starting from the first page indexed as 0.
        /// <para>
        /// When flash page is erased all bytes inside that page are set to 0xFF.
        /// </para>
        /// </para>
        /// </summary>
        /// <param name="index">
        /// <para>Index of memory page to erase</para>
        /// <para>0: First 2kB flash page</para>
        /// <para>1: Next 2kB flash page</para>
        /// <para>etc.</para>
        /// </param>
        /// <returns></returns>
        public async Task ErasePageAsync(byte index)
        {
            var payload = new[] { index };
            var command = Util.GetPSCommand(PSCommand.ErasePage, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>This command can be used to write data to user data area.</para>
        /// <para>
        /// Bits on the flash can only be turned from 1 to 0. To turn the bits from 0 to 1 the Erase Page command
        /// must be used.Notice that the erase page will erase the full 2kB flash page.
        /// </para>
        /// <para>
        /// The amount of flash reserved for the user data needs to be defined in the application configuration file
        /// (config.xml).
        /// </para>
        /// <para>
        /// The amount of available user flash depends on the hardware version and whether it has 128kbB or
        /// 256kB flash and also how much flash is left after the Bluetooth Low Energy stack, BGScript application
        /// and the GATT database.The BGBuild xompiler will show the flash consumption in it's output.
        /// </para>
        /// </summary>
        /// <param name="address">
        /// <para>Offset in the user data space to write in bytes</para>
        /// <para>0: 1st byte</para>
        /// </param>
        /// <param name="data">Data to write</param>
        /// <returns></returns>
        public async Task WriteDataAsync(uint address, byte[] data)
        {
            var addressValue = BitConverter.GetBytes(address);
            var payload = new byte[5 + data.Length];
            Array.Copy(addressValue, payload, 4);
            payload[4] = data.GetByteLength();
            Array.Copy(data, 0, payload, 5, data.Length);
            var command = Util.GetPSCommand(PSCommand.WriteData, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command can be used to read data from user data area.
        /// </summary>
        /// <param name="address">
        /// <para>Offset in the user data space to start reading from in bytes.</para>
        /// <para>0: 1st byte</para>
        /// </param>
        /// <param name="length">Length to read in bytes</param>
        /// <returns>
        /// <para>Data read from flash.</para>
        /// <para>length is set to 0 if read address was invalid</para>
        /// </returns>
        public async Task<byte[]> ReadDataAsync(uint address, byte length)
        {
            var addressValue = BitConverter.GetBytes(address);
            var payload = new byte[5];
            Array.Copy(addressValue, payload, 4);
            payload[4] = length;
            var command = Util.GetPSCommand(PSCommand.ReadData, payload);
            var response = await WriteAsync(command);
            var dataLength = response.Payload[0];
            var data = new byte[dataLength];
            if (data.Length > 0)
            {
                Array.Copy(response.Payload, 1, data, 0, data.Length);
            }
            return data;

        }

        #endregion

        #region Commands - Attribute Database

        /// <summary>
        /// This command writes an attribute's value to the local database.
        /// </summary>
        /// <param name="attribute">Handle of the attribute to write</param>
        /// <param name="offset">Attribute offset to write data</param>
        /// <param name="value">Value of the attribute to write</param>
        /// <returns></returns>
        public async Task WriteAttributeAsync(ushort attribute, byte offset, byte[] value)
        {
            var attributeValue = BitConverter.GetBytes(attribute);
            var payload = new byte[4 + value.Length];
            Array.Copy(attributeValue, payload, 2);
            payload[2] = offset;
            payload[3] = value.GetByteLength();
            Array.Copy(value, 0, payload, 4, value.Length);
            var command = Util.GetAttributeDatabaseCommand(AttributeDatabaseCommand.Write, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// The command reads the given attribute's value from the local database. There is a 32-byte limit in the amount
        /// of data that can be read at a time.In order to read larger values multiple read commands must be used with the
        /// offset properly used.
        /// </para>
        /// <para>For example to read a 64 bytes attribute:</para>
        /// <para>1. Read first 32 bytes using offset 0</para>
        /// <para>2. Read second 32 bytes using offset 32</para>
        /// </summary>
        /// <param name="attribute">Handle of the attribute to read</param>
        /// <param name="offset">
        /// <para>Offset to read from.</para>
        /// <para>Maximum of 32 bytes can be read at a time.</para>
        /// </param>
        /// <returns>Value of the attribute</returns>
        public async Task<byte[]> ReadAttributeAsync(ushort attribute, ushort offset)
        {
            var attributeValue = BitConverter.GetBytes(attribute);
            var offsetValue = BitConverter.GetBytes(offset);
            var payload = new byte[4];
            Array.Copy(attributeValue, payload, 2);
            Array.Copy(offsetValue, 0, payload, 2, 2);
            var command = Util.GetAttributeDatabaseCommand(AttributeDatabaseCommand.Read, payload);
            var response = await WriteAsync(command);
            //var attribute1 = BitConverter.ToUInt16(response.Payload, 0);
            //var offset1 = BitConverter.ToUInt16(response.Payload, 2);
            var errorCode = BitConverter.ToUInt16(response.Payload, 4);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
            var valueLength = response.Payload[6];
            var value = new byte[valueLength];
            Array.Copy(response.Payload, 7, value, 0, value.Length);
            return value;
        }

        /// <summary>
        /// This command reads the given attribute's type (UUID) from the local database.
        /// </summary>
        /// <param name="attribute">Handle of the attribute to read</param>
        /// <returns></returns>
        public async Task<byte[]> ReadAttributeUuidAsync(ushort attribute)
        {
            var payload = BitConverter.GetBytes(attribute);
            var command = Util.GetAttributeDatabaseCommand(AttributeDatabaseCommand.ReadType, payload);
            var response = await WriteAsync(command);
            //var attribute1 = BitConverter.ToUInt16(response.Payload, 0);
            var errorCode = BitConverter.ToUInt16(response.Payload, 2);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
            var uuidLength = response.Payload[4];
            var uuid = new byte[uuidLength];
            Array.Copy(response.Payload, 5, uuid, 0, uuid.Length);
            return uuid;
        }

        /// <summary>
        /// <para>
        /// This command is used to respond to an attribute Read request by a remote device, but only for attributes which
        /// have been configured with the user property.Attributes which have the user property enabled allow the attribute
        /// value to be requested from the application instead of the Bluetooth Low Energy stack automatically responding
        /// with the data in it's local GATT database.
        /// </para>
        /// <para>
        /// This command is normally used in response to a User Read Request event, which is generated when a remote
        /// device tries to read an attribute with a user property enabled.
        /// </para>
        /// <para>
        /// The response to User Read Request events must happen within 30 seconds or otherwise a timeout will occur.
        /// </para>
        /// </summary>
        /// <param name="connection">Connection handle to respond to</param>
        /// <param name="error">
        /// <para>0: User Read Request is responded with data.</para>
        /// <para>In case of an error an application specific error code can be sent.</para>
        /// </param>
        /// <param name="data">Data to send</param>
        /// <returns></returns>
        public async Task UserReadResponseAsync(byte connection, byte error, byte[] data)
        {
            var payload = new byte[3 + data.Length];
            payload[0] = connection;
            payload[1] = error;
            payload[2] = data.GetByteLength();
            Array.Copy(data, 0, payload, 3, data.Length);
            var command = Util.GetAttributeDatabaseCommand(AttributeDatabaseCommand.UserReadResponse, payload);
            await WriteAsync(command);
        }

        /// <summary>
        /// <para>
        /// This command is used by the GATT server to acknowledge to the remote device that the attribute's value was
        /// written.This feature again allows the user application to acknowledged the attribute write operations instead of
        /// the Bluetooth Low Energy stack doing it automatically.
        /// </para>
        /// <para>
        /// The command should be used when a Value event is received where the reason why value has changed
        /// corresponds to attributes_attribute_change_reason_write_request_user.
        /// </para>
        /// <para>
        /// This response must be sent within 30 seconds or otherwise a timeout will occur.
        /// </para>
        /// </summary>
        /// <param name="connection">Connection handle to respond to</param>
        /// <param name="error">
        /// <para>Attribute error code to send if an error occurs.</para>
        /// <para>0x0: Write was accepted</para>
        /// <para>0x80-0x9F: Reserved for user defined error codes</para>
        /// </param>
        /// <returns></returns>
        public async Task UserWriteResponseAsync(byte connection, byte error)
        {
            var payload = new[] { connection, error };
            var command = Util.GetAttributeDatabaseCommand(AttributeDatabaseCommand.UserWriteResponse, payload);
            await WriteAsync(command);
        }

        /// <summary>
        /// <para>
        /// This command will send an attribute value, identified by handle, via a notification or an indication to a remote
        /// device, but does not modify the current corresponding value in the local GATT database.
        /// </para>
        /// <para>
        /// If this attribute, identified by handle, does not have notification or indication property, or no remote device has
        /// registered for notifications or indications of this attribute, then an error will be returned.
        /// </para>
        /// </summary>
        /// <param name="connection">
        /// <para>Connection handle to send to.</para>
        /// <para>
        /// Use 0xFF to send to all connected clients which have subscribed to
        /// receive the notifications or indications.
        /// </para>
        /// <para>An error is returned as soon as the first failed transmission occurs.</para>
        /// </param>
        /// <param name="attribute">Attribute handle to send.</param>
        /// <param name="data">Data to send.</param>
        /// <returns></returns>
        public async Task NotifyAsync(byte connection, ushort attribute, byte[] data)
        {
            var attributeValue = BitConverter.GetBytes(attribute);
            var payload = new byte[4 + data.Length];
            payload[0] = connection;
            Array.Copy(attributeValue, 0, payload, 1, 2);
            payload[3] = data.GetByteLength();
            Array.Copy(data, 0, payload, 4, data.Length);
            var command = Util.GetAttributeDatabaseCommand(AttributeDatabaseCommand.Send, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }
        #endregion

        #region Commands - Connection

        /// <summary>
        /// <para>This command disconnects an active Bluetooth connection.</para>
        /// <para>When link is disconnected a Disconnected event is produced.</para>
        /// </summary>
        /// <param name="connection">Connection handle to close</param>
        /// <returns></returns>
        public async Task DisconnectAsync(byte connection)
        {
            var payload = new[] { connection };
            var command = Util.GetConnectionCommand(ConnectionCommand.Disconnect, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
            var payload = new[] { connection };
            var command = Util.GetConnectionCommand(ConnectionCommand.GetRSSI, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var rssi = (sbyte)response.Payload[1];
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
        /// <param name="minimum">Minimum connection interval (units of 1.25ms)</param>
        /// <param name="maximum">Maximum connection interval (units of 1.25ms)</param>
        /// <param name="latency">
        /// Slave latency which defines how many connections intervals a slave may
        /// skip.
        /// </param>
        /// <param name="timeout">Supervision timeout (units of 10ms)</param>
        /// <returns></returns>
        public async Task UpdateAysnc(byte connection, ushort minimum = 60, ushort maximum = 60, ushort latency = 0, ushort timeout = 100)
        {
            var minimumValue = BitConverter.GetBytes(minimum);
            var maximumValue = BitConverter.GetBytes(minimum);
            var latencyValue = BitConverter.GetBytes(latency);
            var timeoutValue = BitConverter.GetBytes(timeout);
            var payload = new byte[9];
            payload[0] = connection;
            Array.Copy(minimumValue, 0, payload, 1, 2);
            Array.Copy(maximumValue, 0, payload, 3, 2);
            Array.Copy(timeoutValue, 0, payload, 5, 2);
            Array.Copy(latencyValue, 0, payload, 7, 2);
            var command = Util.GetConnectionCommand(ConnectionCommand.Update, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command requests a version exchange of a given connection.
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <returns></returns>
        public async Task UpdateVersionAysnc(byte connection)
        {
            var payload = new[] { connection };
            var command = Util.GetConnectionCommand(ConnectionCommand.VersionUpdate, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        public async Task<byte[]> GetChannelMapAysnc(byte connection)
        {
            var payload = new[] { connection };
            var command = Util.GetConnectionCommand(ConnectionCommand.ChannelMapGet, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var mapLength = response.Payload[1];
            var map = new byte[mapLength];
            Array.Copy(response.Payload, 2, map, 0, mapLength);
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
        public async Task SetChannelMapAysnc(byte connection, byte[] map)
        {
            var payload = new byte[2 + map.Length];
            payload[0] = connection;
            payload[1] = map.GetByteLength();
            Array.Copy(map, 0, payload, 2, map.Length);
            var command = Util.GetConnectionCommand(ConnectionCommand.VersionUpdate, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
            var payload = new[] { connection };
            var command = Util.GetConnectionCommand(ConnectionCommand.GetStatus, payload);
            await WriteAsync(command);
        }

        /// <summary>
        /// This command temporarily enables or disables slave latency.
        /// </summary>
        /// <param name="enabled">
        /// <para>0: enables slave latency</para>
        /// <para>1: disables slave latency</para>
        /// </param>
        /// <returns></returns>
        public async Task UpdateSlaveLatencyAysnc(bool enabled)
        {
            var value = enabled ? (byte)0 : (byte)1;
            var payload = new[] { value };
            var command = Util.GetConnectionCommand(ConnectionCommand.SlaveLatencyDisable, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

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
        public async Task FindByUuidAndValueAsync(byte connection, ushort start, ushort end, ushort uuid, byte[] value)
        {
            var startValue = BitConverter.GetBytes(start);
            var endValue = BitConverter.GetBytes(end);
            var uuidValue = BitConverter.GetBytes(uuid);
            var payload = new byte[8 + value.Length];
            payload[0] = connection;
            Array.Copy(startValue, 0, payload, 1, 2);
            Array.Copy(endValue, 0, payload, 3, 2);
            Array.Copy(uuidValue, 0, payload, 5, 2);
            payload[7] = value.GetByteLength();
            Array.Copy(value, 0, payload, 8, value.Length);
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.FindByTypeValue, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
            var startValue = BitConverter.GetBytes(start);
            var endValue = BitConverter.GetBytes(end);
            var payload = new byte[6 + uuid.Length];
            payload[0] = connection;
            Array.Copy(startValue, 0, payload, 1, 2);
            Array.Copy(endValue, 0, payload, 3, 2);
            payload[5] = uuid.GetByteLength();
            Array.Copy(uuid, 0, payload, 6, uuid.Length);
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.ReadByGroupType, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
            var startValue = BitConverter.GetBytes(start);
            var endValue = BitConverter.GetBytes(end);
            var payload = new byte[6 + uuid.Length];
            payload[0] = connection;
            Array.Copy(startValue, 0, payload, 1, 2);
            Array.Copy(endValue, 0, payload, 3, 2);
            payload[5] = uuid.GetByteLength();
            Array.Copy(uuid, 0, payload, 6, uuid.Length);
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.ReadByType, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
            var startValue = BitConverter.GetBytes(start);
            var endValue = BitConverter.GetBytes(end);
            var payload = new byte[5];
            payload[0] = connection;
            Array.Copy(startValue, 0, payload, 1, 2);
            Array.Copy(endValue, 0, payload, 3, 2);
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.FindInformation, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        /// <param name="attribute">Attribute handle</param>
        /// <returns></returns>
        public async Task ReadByHandleAsync(byte connection, ushort attribute)
        {
            var attributeValue = BitConverter.GetBytes(attribute);
            var payload = new byte[3];
            payload[0] = connection;
            Array.Copy(attributeValue, 0, payload, 1, 2);
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.ReadByHandle, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        /// <param name="attribute">Attribute handle to write to</param>
        /// <param name="data">Attribute value</param>
        /// <returns></returns>
        public async Task AttributeWriteAsync(byte connection, ushort attribute, byte[] data)
        {
            var attributeValue = BitConverter.GetBytes(attribute);
            var payload = new byte[4 + data.Length];
            payload[0] = connection;
            Array.Copy(attributeValue, 0, payload, 1, 2);
            payload[3] = data.GetByteLength();
            Array.Copy(data, 0, payload, 4, data.Length);
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.AttributeWrite, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        /// <param name="attribute">Attribute handle to write</param>
        /// <param name="data">Value for the attribute</param>
        /// <returns></returns>
        public async Task WriteCommandAsync(byte connection, ushort attribute, byte[] data)
        {
            var attributeValue = BitConverter.GetBytes(attribute);
            var payload = new byte[4 + data.Length];
            payload[0] = connection;
            Array.Copy(attributeValue, 0, payload, 1, 2);
            payload[3] = data.GetByteLength();
            Array.Copy(data, 0, payload, 4, data.Length);
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.WriteCommand, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.IndicateConfirm, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        /// <param name="attribute">Attribute handle</param>
        /// <returns></returns>
        public async Task ReadLongAsync(byte connection, ushort attribute)
        {
            var attributeValue = BitConverter.GetBytes(attribute);
            var payload = new byte[3];
            payload[0] = connection;
            Array.Copy(attributeValue, 0, payload, 1, 2);
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.ReadLong, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        /// <param name="attribute">Attribute handle</param>
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
        public async Task PrepareWriteAsync(byte connection, ushort attribute, ushort offset, byte[] data)
        {
            var attributeValue = BitConverter.GetBytes(attribute);
            var offsetValue = BitConverter.GetBytes(offset);
            var payload = new byte[6 + data.Length];
            payload[0] = connection;
            Array.Copy(attributeValue, 0, payload, 1, 2);
            Array.Copy(offsetValue, 0, payload, 3, 2);
            payload[5] = data.GetByteLength();
            Array.Copy(data, 0, payload, 6, data.Length);
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.PrepareWirte, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        public async Task ExecuteWriteAsync(byte connection, bool commit)
        {
            var commitValue = commit ? (byte)1 : (byte)0;
            var payload = new[] { connection, commitValue };
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.ExecuteWrite, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command can be used to read multiple attributes from a server.
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <param name="attributes">List of attribute handles to read from the remote device</param>
        /// <returns></returns>
        public async Task ReadMultipleAsync(byte connection, byte[] attributes)
        {
            var payload = new byte[2 + attributes.Length];
            payload[0] = connection;
            payload[1] = attributes.GetByteLength();
            Array.Copy(attributes, 0, payload, 2, attributes.Length);
            var command = Util.GetAttributeClientCommand(AttributeClientCommand.ReadMultiple, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        #endregion

        #region Commands - SM

        /// <summary>
        /// <para>This command starts the encryption for a given connection.</para>
        /// <para>
        /// Since iOS 9.1 update pairing without bonding is not any more supported by iOS. Calling this APIcommand without being in bondable mode, will cause the connection to fail with devices running iOS
        /// 9.1 or newer.
        /// </para>
        /// <para>
        /// Before using this API command with iOS9.1 or newer you must enable bonding mode with command
        /// Set Bondable Mode and you must also set then bonding parameter in this API command to 1 (Create
        /// bonding).
        /// </para>
        /// </summary>
        /// <param name="connection">Connection handle</param>
        /// <param name="create">
        /// <para>Create bonding if devices are not already bonded</para>
        /// <para>0: Do not create bonding</para>
        /// <para>1: Creating bonding</para>
        /// </param>
        /// <returns></returns>
        public async Task StartEncryptAsync(byte connection, bool create)
        {
            var createValue = create ? (byte)1 : (byte)0;
            var payload = new[] { connection, createValue };
            var command = Util.GetSMCommand(SMCommand.EncryptStart, payload);
            var response = await WriteAsync(command);
            //var connection1 = response.Payload[0];
            var errorCode = BitConverter.ToUInt16(response.Payload, 1);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// Set device to bondable mode
        /// </summary>
        /// <param name="bondable">
        /// <para>Enables or disables bonding mode</para>
        /// <para>0 : the device is not bondable</para>
        /// <para>1 : the device is bondable</para>
        /// </param>
        /// <returns></returns>
        public async Task SetBondableModeAsync(bool bondable)
        {
            var bondableValue = bondable ? (byte)1 : (byte)0;
            var payload = new[] { bondableValue };
            var command = Util.GetSMCommand(SMCommand.SetBondableMode, payload);
            await WriteAsync(command);
        }

        /// <summary>
        /// This command deletes a bonding from the local security database. There can be a maximum of 8 bonded
        /// devices stored at the same time, and one of them must be deleted if you need bonding with a 9th device.
        /// </summary>
        /// <param name="bonding">
        /// <para>Bonding handle of a device.</para>
        /// <para>This handle can be obtained for example from events like:</para>
        /// <para>Scan Response</para>
        /// <para>Status</para>
        /// <para>If handle is 0xFF, all bondings will be deleted</para>
        /// </param>
        /// <returns></returns>
        public async Task DeleteBondingAsync(byte bonding)
        {
            var payload = new[] { bonding };
            var command = Util.GetSMCommand(SMCommand.DeleteBonding, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command is used to configure the local Security Manager and its features.
        /// </summary>
        /// <param name="mitm">
        /// <para>1: Man-in-the-middle protection required</para>
        /// <para>0: No Man-in-the-middle protection</para>
        /// <para>Default: 0</para>
        /// </param>
        /// <param name="minimim">
        /// <para>Minimum key size in Bytes</para>
        /// <para>Range: 7-16</para>
        /// <para>Default: 7 (56bits)</para>
        /// </param>
        /// <param name="capabilities">
        /// <para>Configures the local devices I/O capabilities.</para>
        /// <para>See: SMP IO Capabilities for options.</para>
        /// <para>Default: No Input and No Output</para>
        /// </param>
        /// <returns></returns>
        public async Task SetParametersAsync(bool mitm = false, byte minimim = 7, IOCapability capabilities = IOCapability.NoInputNoOutput)
        {
            var mitmValue = mitm ? (byte)1 : (byte)0;
            var capabilitiesValue = (byte)capabilities;
            var payload = new[] { mitmValue, minimim, capabilitiesValue };
            var command = Util.GetSMCommand(SMCommand.SetParameters, payload);
            await WriteAsync(command);
        }

        /// <summary>
        /// This command is used to enter a passkey required for Man-in-the-Middle pairing. It should be sent as a
        /// response to Passkey Request event.
        /// </summary>
        /// <param name="connection">Connection Handle</param>
        /// <param name="passkey">
        /// <para>Passkey</para>
        /// <para>Range: 000000-999999</para>
        /// </param>
        /// <returns></returns>
        public async Task EnterPasskeyAsync(byte connection, uint passkey)
        {
            var passkeyValue = BitConverter.GetBytes(passkey);
            var payload = new byte[5];
            payload[0] = connection;
            Array.Copy(passkeyValue, 0, payload, 1, 4);
            var command = Util.GetSMCommand(SMCommand.PasskeyEntry, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command lists all bonded devices. There can be a maximum of 8 bonded devices. The information related
        /// to the bonded devices is stored in the Flash memory, so it is persistent across resets and power-cycles.
        /// </summary>
        /// <returns>Num of currently bonded devices</returns>
        public async Task<byte> GetBondsAsync()
        {
            var command = Util.GetSMCommand(SMCommand.GetBonds);
            var response = await WriteAsync(command);
            var bonds = response.Payload[0];
            return bonds;
        }

        /// <summary>
        /// <para>This commands sets the Out-of-Band encryption data for a device.</para>
        /// <para>Device does not allow any other kind of pairing except OoB if the OoB data is set.</para>
        /// </summary>
        /// <param name="oob">
        /// <para>The OoB data to set, which must be 16 or 0 octets long.</para>
        /// <para>If the data is empty it clears the previous OoB data.</para>
        /// </param>
        /// <returns></returns>
        public async Task SetOobDataAsync(byte[] oob)
        {
            var payload = new byte[1 + oob.Length];
            payload[0] = oob.GetByteLength();
            Array.Copy(oob, 0, payload, 1, oob.Length);
            var command = Util.GetSMCommand(SMCommand.SetOobData, payload);
            await WriteAsync(command);
        }

        /// <summary>
        /// <para>
        /// This command will add all bonded devices with a known public or static address to the local devices white list.
        /// Previous entries in the white list will be first cleared.
        /// </para>
        /// <para>
        /// This command can't be used while advertising, scanning or being connected.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public async Task<byte> WhitelistBondsAsync()
        {
            var command = Util.GetSMCommand(SMCommand.WhitelistBonds);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
            var count = response.Payload[2];
            return count;
        }

        /// <summary>
        /// Change keys distribution fields in pairing request and response. By default all keys are distributed.
        /// </summary>
        /// <param name="initiator">Initiator Key Distribution</param>
        /// <param name="responder">Responder Key Distribution</param>
        /// <returns></returns>
        public async Task SetPairingDistributionKeysAsync(
            KeyDistribution initiator = KeyDistribution.All,
            KeyDistribution responder = KeyDistribution.All)
        {
            var initiatorValue = (byte)initiator;
            var responderValue = (byte)responder;
            var payload = new[] { initiatorValue, responderValue };
            var command = Util.GetSMCommand(SMCommand.SetPairingDistributionKeys, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        #endregion

        #region Commands - GAP

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
        /// <param name="peripheral"></param>
        /// <param name="central"></param>
        /// <returns></returns>
        public async Task SetPrivacyFlagsAsync(AddressPrivacy peripheral, AddressPrivacy central)
        {
            var peripheralValue = (byte)peripheral;
            var centralValue = (byte)central;
            var payload = new[] { peripheralValue, centralValue };
            var command = Util.GetGapCommand(GapCommand.SetPrivacyFlags, payload);
            await WriteAsync(command);
        }

        /// <summary>
        /// This command configures the current GAP discoverability and connectability modes. It can be used to enable
        /// advertisements and/or allow connection.The command is also meant to fully stop advertising, when using
        /// <see cref="DiscoverableMode.None"/> and <see cref="ConnectableMode.NonConnectable"/>.
        /// </summary>
        /// <param name="discoverableMode"></param>
        /// <param name="connectableMode"></param>
        /// <returns></returns>
        public async Task SetModeAsync(DiscoverableMode discoverableMode, ConnectableMode connectableMode)
        {
            var discoverableModeValue = (byte)discoverableMode;
            var connectableModeValue = (byte)connectableMode;
            var payload = new[] { discoverableModeValue, connectableModeValue };
            var command = Util.GetGapCommand(GapCommand.SetMode, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// This command starts the GAP discovery procedure to scan for advertising devices i.e. to perform a device
        /// discovery.
        /// </para>
        /// Scanning parameters can be configured with the <see cref="SetDiscoveryParametersAsync(ushort, ushort, bool)"/>
        /// <para>
        /// To cancel on an ongoing discovery process use the <see cref="EndAsync"/>
        /// </para>
        /// </summary>
        /// <param name="mode">GAP Discover modes</param>
        /// <returns></returns>
        public async Task DiscoverAsync(DiscoverMode mode = DiscoverMode.Observation)
        {
            var modeValue = (byte)mode;
            var payload = new[] { modeValue };
            var command = Util.GetGapCommand(GapCommand.Discover, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        /// The connection establishment procedure can be cancelled with <see cref="EndAsync"/>.
        /// </para>
        /// </summary>
        /// <param name="address">Bluetooth address of the target device</param>
        /// <param name="minimum">
        /// <para>Minimum Connection Interval (in units of 1.25ms).</para>
        /// <para>Range: 6 - 3200</para>
        /// <para>
        /// The lowest possible Connection Interval is 7.50ms and the largest is
        /// 4000ms.
        /// </para>
        /// </param>
        /// <param name="maximum">
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
        public async Task<byte> ConnectAsync(Address address, ushort minimum = 60, ushort maximum = 60, ushort timeout = 100, ushort latency = 0)
        {
            var minimumValue = BitConverter.GetBytes(minimum);
            var maximumValue = BitConverter.GetBytes(minimum);
            var timeoutValue = BitConverter.GetBytes(timeout);
            var latencyValue = BitConverter.GetBytes(latency);
            var payload = new byte[15];
            Array.Copy(address.RawValue, 0, payload, 0, 6);
            payload[6] = (byte)address.Type;
            Array.Copy(minimumValue, 0, payload, 7, 2);
            Array.Copy(maximumValue, 0, payload, 9, 2);
            Array.Copy(timeoutValue, 0, payload, 11, 2);
            Array.Copy(latencyValue, 0, payload, 13, 2);
            var command = Util.GetGapCommand(GapCommand.ConnectDirect, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
            var connection = response.Payload[2];
            return connection;
        }

        /// <summary>
        /// This command ends the current GAP discovery procedure and stop the scanning of advertising devices.
        /// </summary>
        /// <returns></returns>
        public async Task EndAsync()
        {
            var command = Util.GetGapCommand(GapCommand.EndProcedure);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        /// <param name="minimum">
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
        /// <param name="maximum">
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
        public async Task<byte> ConnectSelectiveAsync(ushort minimum, ushort maximum, ushort timeout, ushort latency)
        {
            var minimumValue = BitConverter.GetBytes(minimum);
            var maximumValue = BitConverter.GetBytes(maximum);
            var timeoutValue = BitConverter.GetBytes(timeout);
            var latencyValue = BitConverter.GetBytes(latency);
            var payload = new byte[8];
            Array.Copy(minimumValue, 0, payload, 0, 2);
            Array.Copy(maximumValue, 0, payload, 2, 2);
            Array.Copy(timeoutValue, 0, payload, 4, 2);
            Array.Copy(latencyValue, 0, payload, 6, 2);
            var command = Util.GetGapCommand(GapCommand.ConnectSelective, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
            var connection = response.Payload[2];
            return connection;
        }

        /// <summary>
        /// This command can be used to set scan, connection, and advertising filtering parameters based on the local
        /// devices white list.See also Whitelist Append command.
        /// </summary>
        /// <param name="discoverPolicy"></param>
        /// <param name="advertisingPolicy"></param>
        /// <param name="filterDuplicates">
        /// <para>0: Do not filter duplicate advertisers</para>
        /// <para>1: Filter duplicates</para>
        /// </param>
        /// <returns></returns>
        public async Task SetFilteringAsync(DiscoverPolicy discoverPolicy, AdvertisingPolicy advertisingPolicy, bool filterDuplicates)
        {
            var discoverPolicyValue = (byte)discoverPolicy;
            var advertisingPolicyValue = (byte)advertisingPolicy;
            var filterDuplicatesValue = filterDuplicates ? (byte)1 : (byte)0;
            var payload = new[] { discoverPolicyValue, advertisingPolicyValue, filterDuplicatesValue };
            var command = Util.GetGapCommand(GapCommand.SetFiltering, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
            var intervalValue = BitConverter.GetBytes(interval);
            var windowValue = BitConverter.GetBytes(window);
            var activeValue = active ? (byte)0x01 : (byte)0x00;
            var payload = new byte[5];
            Array.Copy(intervalValue, 0, payload, 0, 2);
            Array.Copy(windowValue, 0, payload, 2, 2);
            payload[4] = activeValue;
            var command = Util.GetGapCommand(GapCommand.SetScanParameters, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        /// <param name="minimum">
        /// <para>Minimum advertisement interval in units of 625us</para>
        /// <para>Range: 0x20 to 0x4000</para>
        /// <para>Default: 0x200 (320ms)</para>
        /// <para>Explanation:</para>
        /// <para>0x200 = 512</para>
        /// <para>512 * 625us = 320000us = 320ms</para>
        /// </param>
        /// <param name="maximum">
        /// <para>Maximum advertisement interval in units of 625us.</para>
        /// <para>Range: 0x20 to 0x4000</para>
        /// <para>Default: 0x200 (320ms)</para>
        /// </param>
        /// <param name="channels">
        /// <para>A bit mask to identify which of the three advertisement channels are used.</para>
        /// <para>Examples:</para>
        /// <para>0x07: All three channels are used</para>
        /// <para>0x03: Advertisement channels 37 and 38 are used.</para>
        /// <para>0x04: Only advertisement channel 39 is used</para>
        /// </param>
        /// <returns></returns>
        public async Task SetAdvertiseParametersAsync(ushort minimum, ushort maximum, byte channels)
        {
            var minimumValue = BitConverter.GetBytes(minimum);
            var maximumValue = BitConverter.GetBytes(maximum);
            var payload = new byte[5];
            Array.Copy(minimumValue, 0, payload, 0, 2);
            Array.Copy(maximumValue, 0, payload, 2, 2);
            payload[4] = channels;
            var command = Util.GetGapCommand(GapCommand.SetAdvParameters, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        /// <param name="mode">Advertisement data type</param>
        /// <param name="advertisements">Advertisement data to send</param>
        /// <returns></returns>
        public async Task SetAdvertisementsAsync(AdvertisementMode mode, IList<Advertisement> advertisements)
        {
            var modeValue = (byte)mode;
            var length = advertisements.Sum(i => 1 + i.Value.Length);
            var advertisementsValue = new byte[length];
            var i = 0;
            foreach (var advertisement in advertisements)
            {
                advertisementsValue[i] = (byte)advertisement.Type;
                Array.Copy(advertisement.Value, 0, advertisementsValue, ++i, advertisement.Value.Length);
                i += advertisement.Value.Length;
            }
            var payload = new byte[2 + advertisementsValue.Length];
            payload[0] = modeValue;
            payload[1] = advertisementsValue.GetByteLength();
            Array.Copy(advertisementsValue, 0, payload, 2, advertisementsValue.Length);
            var command = Util.GetGapCommand(GapCommand.SetAdvData, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
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
        public async Task SetDirectedConnectableModeAsync(Address address)
        {
            var payload = new byte[7];
            Array.Copy(address.RawValue, 0, payload, 0, 6);
            payload[6] = (byte)address.Type;
            var command = Util.GetGapCommand(GapCommand.SetDirectedConnectableMode, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command sets the scan parameters for Initiating State which affect for establishing BLE connection. See
        /// BLUETOOTH SPECIFICATION Version 4.0 [Vol 6 - Part B - Chapter 4.4.4].
        /// </summary>
        /// <param name="interval">
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
        /// <param name="window">
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
        public async Task SetInitialDiscoverParametersAsync(ushort interval, ushort window)
        {
            var intervalValue = BitConverter.GetBytes(interval);
            var windowValue = BitConverter.GetBytes(window);
            var payload = new byte[4];
            Array.Copy(intervalValue, 0, payload, 0, 2);
            Array.Copy(windowValue, 0, payload, 2, 2);
            var command = Util.GetGapCommand(GapCommand.SetInitialingConParameters, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command set the local device's random Non-Resolvable Bluetooth address. Default local device's random
        /// Non-Resolvable Bluetooth address is 00:00:00:00:00:01.
        /// </summary>
        /// <param name="address">Bluetooth non-resolvable address of the local device</param>
        /// <returns></returns>
        public async Task SetNonresolvableAddress(byte[] address)
        {
            var payload = address;
            var command = Util.GetGapCommand(GapCommand.SetNonresolvableAddress, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        #endregion

        #region Commands - Hardware

        /// <summary>
        /// <para>This command configures the locals I/O-port interrupts.</para>
        /// <para>
        /// Interrupts on I/O-port 2 can be enabled only for BLE113 and BLE121LR chip. In this case P2_0 and
        /// P2_1 pins are available.
        /// </para>
        /// </summary>
        /// <param name="port">
        /// <para>I/O port selection</para>
        /// <para>Values: 0 - 2</para>
        /// </param>
        /// <param name="interrupt">
        /// <para>A bit mask which tells which I/O generate an interrupt</para>
        /// <para>bit 0: Interrupt is enabled</para>
        /// <para>bit 1: Interrupt is disabled</para>
        /// </param>
        /// <param name="edge">
        /// <para>Interrupt sense for port.</para>
        /// <para>Note: affects all IRQ enabled pins on the port</para>
        /// </param>
        /// <returns></returns>
        [Obsolete("This command is deprecated in and Io Port Irq Enable and Io Port Irq Direction commands should beused instead.")]
        public async Task ConfigIOPortInterruptsAsync(byte port, byte interrupt, InterruptEdge edge)
        {
            var edgeValue = (byte)edge;
            var payload = new[] { port, interrupt, edgeValue };
            var command = Util.GetHardwareCommand(HardwareCommand.IOPortConfigIRQ, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command configures the local software timer. The timer is 22 bits so the maximum value with BLE112 is
        /// 2^22 = 4194304/32768Hz = 128 seconds.With BLED112 USB dongle the maximum value is 2^22 = 4194304
        /// /32000Hz = 131 seconds.
        /// </summary>
        /// <param name="time">
        /// <para>Timer interrupt period in units of local crystal frequency.</para>
        /// <para>time : 1/32768 seconds for modules where the external sleep oscillator must be enabled.</para>
        /// <para>time : 1/32000 seconds for the dongle where internal RC oscillator is used. If time is 0, scheduled timer is removed.</para>
        /// </param>
        /// <param name="handle">Handle that is sent back within triggered event at timeout</param>
        /// <param name="mode">Timer mode.</param>
        /// <returns></returns>
        public async Task SetTimerAsync(uint time, byte handle, TimeoutMode mode)
        {
            var timeValue = BitConverter.GetBytes(time);
            var modeValue = (byte)mode;
            var payload = new byte[6];
            Array.Copy(timeValue, payload, 4);
            payload[4] = handle;
            payload[5] = modeValue;
            var command = Util.GetHardwareCommand(HardwareCommand.SetSoftTimer, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command reads the devices local A/D converter. Only a single channel may be read at a time, and each
        /// conversion must complete before another one is requested.The completion of each conversion is indicated by
        /// the hardware_adc_result event.
        /// </summary>
        /// <param name="input">
        /// <para>Selects the ADC input.</para>
        /// <para>0x0: AIN0 (pin 0 of port P0, denoted as A0 in the ADC row of datasheet's table 3)</para>
        /// <para>0x1: AIN1</para>
        /// <para>0x2: AIN2</para>
        /// <para>0x3: AIN3</para>
        /// <para>0x4: AIN4</para>
        /// <para>0x5: AIN5</para>
        /// <para>0x6: AIN6</para>
        /// <para>0x7: AIN7</para>
        /// <para>0x8: AIN0--AIN1 differential</para>
        /// <para>0x9: AIN2--AIN3 differential</para>
        /// <para>0xA: AIN4--AIN5 differential</para>
        /// <para>0xB: AIN6--AIN7 differential</para>
        /// <para>0xC: GND</para>
        /// <para>0xD: Reserved</para>
        /// <para>0xE: Temperature sensor</para>
        /// <para>0xF: VDD/3</para>
        /// </param>
        /// <param name="decimation">
        /// <para>Select resolution and conversion rate for conversion, result is always stored in MSB bits.</para>
        /// <para>0: 7 effective bits</para>
        /// <para>1: 9 effective bits</para>
        /// <para>2: 10 effective bits</para>
        /// <para>3: 12 effective bits</para>
        /// </param>
        /// <param name="reference">
        /// <para>Selects the reference for the ADC. Reference corresponds to the maximum allowed input value.</para>
        /// <para>0: Internal reference (1.24V)</para>
        /// <para>1: External reference on AIN7 pin</para>
        /// <para>2: AVDD pin</para>
        /// <para>3: External reference on AIN6--AIN7 differential input</para>
        /// </param>
        /// <returns></returns>
        public async Task ReadADConverterAsync(byte input, byte decimation, byte reference)
        {
            var payload = new[] { input, decimation, reference };
            var command = Util.GetHardwareCommand(HardwareCommand.AdcRead, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// The command configiures I/O-port directions
        /// </summary>
        /// <param name="port">I/0 PORT index: 0, 1 or 2</param>
        /// <param name="direction">
        /// <para>Bitmask for each individual pin direction</para>
        /// <para>bit0 means input (default)</para>
        /// <para>bit1 means output</para>
        /// <para>Example:</para>
        /// <para>To configure all port's pins as output use 0xff</para>
        /// </param>
        /// <returns></returns>
        public async Task ConfigIOPortDirectionsAsync(byte port, byte direction)
        {
            var payload = new[] { port, direction };
            var command = Util.GetHardwareCommand(HardwareCommand.IOPortConfigDirection, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>This command configures the I/O-ports function.</para>
        /// <para>
        /// If bit is set in function parameter then the corresponding I/O port is set to peripheral function, otherwise it is
        /// general purpose I/O pin.
        /// </para>
        /// </summary>
        /// <param name="port">I/O port: 0,1 or 2</param>
        /// <param name="function">peripheral selection bit for pins</param>
        /// <returns></returns>
        public async Task ConfigIOPortFunctionAsync(byte port, byte function)
        {
            var payload = new[] { port, function };
            var command = Util.GetHardwareCommand(HardwareCommand.IOPortConfigFunction, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>Configure I/O-port pull-up/pull-down</para>
        /// <para>Pins P1_0 and P1_1 do not have pull-up/pull-down.</para>
        /// </summary>
        /// <param name="port">I/O port select: 0, 1 or 2</param>
        /// <param name="disabled">If this bit is set, disabled pull on pin</param>
        /// <param name="up">
        /// <para>1: pull all port's pins up</para>
        /// <para>0: pull all port's pins down</para>
        /// </param>
        /// <returns></returns>
        public async Task ConfigIOPortPullAsync(byte port, byte disabled, bool up)
        {
            var upValue = up ? (byte)1 : (byte)0;
            var payload = new[] { port, disabled, upValue };
            var command = Util.GetHardwareCommand(HardwareCommand.IOPortConfigPull, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// Write I/O-port statuses
        /// </summary>
        /// <param name="port">
        /// <para>I/O port to write to</para>
        /// <para>Values: 0,1 or 2</para>
        /// </param>
        /// <param name="mask">
        /// <para>Bit mask to tell which I/O pins to write</para>
        /// <para>Example:</para>
        /// <para>To write the status of all IO pins use 0xFF</para>
        /// </param>
        /// <param name="data">
        /// <para>Bit mask to tell which state to write</para>
        /// <para>bit0: I/O is disabled</para>
        /// <para>bit1: I/O is enabled</para>
        /// <para>Example:</para>
        /// <para>To enable all IO pins use 0xFF</para>
        /// </param>
        /// <returns></returns>
        public async Task WriteIOPortAsync(byte port, byte mask, byte data)
        {
            var payload = new[] { port, mask, data };
            var command = Util.GetHardwareCommand(HardwareCommand.IOPortWrite, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// Read I/O-port
        /// </summary>
        /// <param name="port">
        /// <para>I/O port to read</para>
        /// <para>Values: 0,1 or 2</para>
        /// </param>
        /// <param name="mask">
        /// <para>Bit mask to tell which I/O pins to read</para>
        /// <para>Example:</para>
        /// <para>To read the status of all IO pins use 0xFF</para>
        /// </param>
        /// <returns>I/O port pin state</returns>
        public async Task<byte> ReadIOPortAsync(byte port, byte mask)
        {
            var payload = new[] { port, mask };
            var command = Util.GetHardwareCommand(HardwareCommand.IOPortRead, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
            //var port1 = response.Payload[2];
            var data = response.Payload[3];
            return data;
        }

        /// <summary>
        /// The command configures the SPI interface
        /// </summary>
        /// <param name="channel">
        /// <para>USART channel</para>
        /// <para>Values: 0 or 1</para>
        /// </param>
        /// <param name="polarity">
        /// <para>Clock polarity</para>
        /// <para>Values: 0 or 1</para>
        /// </param>
        /// <param name="phase">
        /// <para>Clock phase</para>
        /// <para>Values: 0 or 1</para>
        /// </param>
        /// <param name="endianness">
        /// <para>Endianness</para>
        /// <para>0: LSB</para>
        /// <para>1: MSB</para>
        /// </param>
        /// <param name="baudExponent">baud rate exponent value</param>
        /// <param name="baudMantissa">baud rate mantissa value</param>
        /// <returns></returns>
        public async Task ConfigSpiAsync(byte channel, byte polarity, byte phase, byte endianness, byte baudExponent, byte baudMantissa)
        {
            var payload = new[] { channel, polarity, phase, endianness, baudExponent, baudMantissa };
            var command = Util.GetHardwareCommand(HardwareCommand.SpiConfig, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// This command is used to transfer SPI data when in master mode. Maximum of 64 bytes can be transferred at a
        /// time.
        /// </para>
        /// <para>
        /// Slave select pin is not controlled automatically when transferring data while in SPI master mode, so it
        /// must be controlled by the application using normal GPIO control commands like IO Port Write
        /// command.
        /// </para>
        /// </summary>
        /// <param name="channel">
        /// <para>SPI channel</para>
        /// <para>Value: 0 or 1</para>
        /// </param>
        /// <param name="data">
        /// <para>Data to transmit</para>
        /// <para>Maximum length is 64 bytes</para>
        /// </param>
        /// <returns>data received from SPI</returns>
        public async Task<byte[]> TransferSpiAsync(byte channel, byte[] data)
        {
            var payload = new byte[2 + data.Length];
            payload[0] = channel;
            payload[1] = data.GetByteLength();
            Array.Copy(data, 0, payload, 2, data.Length);
            var command = Util.GetHardwareCommand(HardwareCommand.SpiTransfer, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
            //var channel1 = response.Payload[2];
            var receivedLength = response.Payload[3];
            var received = new byte[receivedLength];
            Array.Copy(response.Payload, 4, received, 0, received.Length);
            return received;
        }

        /// <summary>
        /// <para>The command reads data from I2C bus.</para>
        /// <para>
        /// BLE112 module: uses bit-bang method and only master-mode is supported in current firmwares, I2C CLK is
        /// fixed to P1_7 and I2C DATA to P1_6(pull - up must be enabled on both pins), the clock rate is approximately 20 -
        /// 25 kHz and it does vary slightly because other functionality has higher interrupt priority, such as the BLE radio.
        /// </para>
        /// <para>
        /// BLE113/BLE121LR modules: only master-mode is supported in current firmwares, I2C pins are 14/24 (I2C CLK)
        /// and 15/25 (I2C DATA) as seen in the datasheet, operates at 267kHz.
        /// </para>
        /// <para>
        /// To convert a 7-bit I2C address to an 8-bit one, shift left by one bit. For example, a 7-bit address of
        /// 0x40 (dec 64) would be used as 0x80 (dec 128).
        /// </para>
        /// <para>
        /// I2C commands got a timeout of about 250 ms. If the read operation is timeouted then the
        /// corresponding command result is returned.
        /// </para>
        /// </summary>
        /// <param name="address">
        /// I2C's 8-bit slave address according to the note above. Keep read/write bit (LSB) set
        /// to zero, as the firmware will set it automatically.
        /// </param>
        /// <param name="stop">If nonzero Send I2C stop condition after transmission</param>
        /// <param name="length">Number of bytes to read</param>
        /// <returns>Data read</returns>
        public async Task<byte[]> ReadI2cAsync(byte address, byte stop, byte length)
        {
            var payload = new[] { address, stop, length };
            var command = Util.GetHardwareCommand(HardwareCommand.I2cRead, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
            var dataLength = response.Payload[2];
            var data = new byte[dataLength];
            Array.Copy(response.Payload, 3, data, 0, data.Length);
            return data;
        }

        /// <summary>
        /// <para>Write data to I2C bus.</para>
        /// <para>
        /// BLE112: uses bit-bang method, only master-mode is supported in current firmwares, I2C CLK is fixed to P1_7
        /// and I2C DATA to P1_6(pull-up must be enabled on both pins), the clock rate is approximately 20-25 kHz and it
        /// does vary slightly because other functionality has higher interrupt priority, such as the BLE radio.
        /// </para>
        /// <para>
        /// BLE113/BLE121LR: only master-mode is supported in current firmwares, I2C pins are 14/24 (I2C CLK) and 15
        /// /25 (I2C DATA) as seen in the datasheet, operates at 267kHz.
        /// </para>
        /// <para>
        /// To convert a 7-bit address to an 8-bit one, shift left by one bit. For example, a 7-bit address of 0x40
        /// (dec 64) would be used as 0x80 (dec 128).
        /// </para>
        /// <para>
        /// I2C commands got a timeout of about 250 ms. If the write operation is timeouted then the written bytes
        /// value is 0.
        /// </para>
        /// </summary>
        /// <param name="address">
        /// I2C's 8-bit slave address according to the note above. Keep read/write bit
        /// (LSB) set to zero, as the firmware will set it automatically.
        /// </param>
        /// <param name="stop">If nonzero Send I2C stop condition after transmission</param>
        /// <param name="data">Data to write</param>
        /// <returns>Bytes written</returns>
        public async Task<byte> WriteI2cAsync(byte address, byte stop, byte[] data)
        {
            var payload = new byte[3 + data.Length];
            payload[0] = address;
            payload[1] = stop;
            payload[2] = data.GetByteLength();
            Array.Copy(data, 0, payload, 3, data.Length);
            var command = Util.GetHardwareCommand(HardwareCommand.I2cWrite, payload);
            var response = await WriteAsync(command);
            var length = response.Payload[0];
            return length;
        }

        /// <summary>
        /// Re-configure TX output power.
        /// </summary>
        /// <param name="power">
        /// <para>TX output power level to use</para>
        /// <para>Range:</para>
        /// <para>0 to 15 with the BLE112 and the BLED112</para>
        /// <para>0 to 14 with the BLE113</para>
        /// <para>0 to 9 with the BLE121LR</para>
        /// <para>For more information, refer to the <txpower> tag in the hardware.xml configuration file.</para>
        /// </param>
        /// <returns></returns>
        public async Task SetTXPowerAsync(byte power)
        {
            var payload = new[] { power };
            var command = Util.GetHardwareCommand(HardwareCommand.SetTXPower, payload);
            await WriteAsync(command);
        }

        /// <summary>
        /// <para>Set comparator for timer channel.</para>
        /// <para>
        /// This command may be used to generate e.g. PWM signals with hardware timer. More information on different
        /// comparator modes and their usage may be found from Texas Instruments CC2540 User's Guide (SWRU191B),
        /// section 9.8 Output Compare Mode.
        /// </para>
        /// </summary>
        /// <param name="timer">Timer</param>
        /// <param name="channel">Timer channel</param>
        /// <param name="mode">Comparator mode</param>
        /// <param name="comparatorValue">Comparator value</param>
        /// <returns></returns>
        public async Task SetTimerComparatorAsync(byte timer, byte channel, byte mode, ushort comparatorValue)
        {
            var value = BitConverter.GetBytes(comparatorValue);
            var payload = new byte[5];
            payload[0] = timer;
            payload[1] = channel;
            payload[2] = mode;
            Array.Copy(value, 0, payload, 3, 2);
            var command = Util.GetHardwareCommand(HardwareCommand.TimerComparator, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// Enable I/O-port interrupts. When enabled, I/O-port interrupts are triggered on either rising or falling edge. The
        /// direction when the interrupt occurs may be configured with IO Port Irq Direction command.
        /// </para>
        /// <para>
        /// Interrupts on I/O-port 2 can be enabled only for BLE113 and BLE121LR chip. In this case P2_0 and
        /// P2_1 pins are available.
        /// </para>
        /// </summary>
        /// <param name="port">
        /// <para>I/O Port</para>
        /// <para>Value: 0 - 2</para>
        /// </param>
        /// <param name="mask">
        /// <para>Interrupt enable mask for pins</para>
        /// <para>bit0 means interrupt is disabled</para>
        /// <para>bit1 means interrupt is enabled</para>
        /// <para>Example:</para>
        /// <para>To enable interrupts an all pins use 0xFF</para>
        /// </param>
        /// <returns></returns>
        public async Task EnableIOPortInterruptsAsync(byte port, byte mask)
        {
            var payload = new[] { port, mask };
            var command = Util.GetHardwareCommand(HardwareCommand.IOPortIrqEnable, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>Set I/O-port interrupt direction. The direction applies for every pin in the given I/O-port.</para>
        /// <para>
        /// Interrupts on I/O-port 2 can be enabled only for BLE113 and BLE121LR chip. In this case P2_0 and
        /// P2_1 pins are available.
        /// </para>
        /// </summary>
        /// <param name="port">
        /// <para>I/O Port</para>
        /// <para>Values: 0 - 2</para>
        /// </param>
        /// <param name="edge">
        /// <para>Interrupt edge direction for port</para>
        /// <para>0: rising edge</para>
        /// <para>1: falling edge</para>
        /// </param>
        /// <returns></returns>
        public async Task SetIOPortInterruptDirectionAsync(byte port, InterruptEdge edge)
        {
            var edgeValue = (byte)edge;
            var payload = new[] { port, edgeValue };
            var command = Util.GetHardwareCommand(HardwareCommand.IOPortIrqDirection, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// Enables or disables the analog comparator. Analog comparator has to be enabled prior using any other analog
        /// comparator commands.
        /// </summary>
        /// <param name="enable">
        /// <para>1: enable</para>
        /// <para>0: disable</para>
        /// </param>
        /// <returns></returns>
        public async Task ControlAnalogComparatorAsync(bool enable)
        {
            var enableValue = enable ? (byte)1 : (byte)0;
            var payload = new[] { enableValue };
            var command = Util.GetHardwareCommand(HardwareCommand.AnalogComparatorEnable, payload);
            await WriteAsync(command);
        }

        /// <summary>
        /// The command reads analog comparator output. Before using this command, analog comparator has to be
        /// enabled with <see cref="ControlAnalogComparatorAsync(bool)"/> command.
        /// </summary>
        /// <returns>
        /// <para>Analog comparator output</para>
        /// <para>1: V+ > V-</para>
        /// <para>0: V+ &lt; V-</para>
        /// </returns>
        public async Task<byte> ReadAnalogComparatorAsync()
        {
            var command = Util.GetHardwareCommand(HardwareCommand.AnalogComparatorRead);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
            var output = response.Payload[2];
            return output;
        }

        /// <summary>
        /// <para>
        /// This command configures analog comparator interrupts. Before enabling this interrupt, analog comparator has
        /// to be first enabled with Analog Comparator Enable command.
        /// </para>
        /// <para>
        /// Analog comparator interrupts are generated by default on rising edge, i.e. when condition V+ > V- becomes
        /// true. It is also possible to configure the opposite functionality, i.e.interrupts are generated on falling edge when
        /// V+ &lt; V- becomes true. The interrupt direction may be configured with Io Port Irq Direction command, by setting I
        /// /O-port 0 direction.Please note that this configuration affects both analog comparator interrupt direction and all I
        /// /O-port 0 pin interrupt directions.
        /// </para>
        /// <para>
        /// Analog comparator interrupts are automatically disabled once triggered , so that a high frequency signal doesn't
        /// cause unintended consequences.Continuous operation may be achieved by re-enabling the interrupt as soon
        /// as the Analog Comparator Status event has been received.
        /// </para>
        /// </summary>
        /// <param name="enable">
        /// <para>1: enable interrupts</para>
        /// <para>0: disable interrupts</para>
        /// </param>
        /// <returns></returns>
        public async Task ConfigAnalogComparatorInterruptsAsync(bool enable)
        {
            var enableValue = enable ? (byte)1 : (byte)0;
            var payload = new[] { enableValue };
            var command = Util.GetHardwareCommand(HardwareCommand.AnalogComparatorConfigIRQ, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command sets the radio receiver (RX) sensitivity to either high (default) or standard. The exact sensitivity
        /// value is dependent on the used hardware(refer to the appropriate data sheet).
        /// </summary>
        /// <param name="sensitivity"></param>
        /// <returns></returns>
        public async Task SetRXSensitivityAsync(Sensitivity sensitivity)
        {
            var sensitivityValue = (byte)sensitivity;
            var payload = new[] { sensitivityValue };
            var command = Util.GetHardwareCommand(HardwareCommand.SetRXGain, payload);
            await WriteAsync(command);
        }

        /// <summary>
        /// This command activates (enable) or deactivates USB controller on the BLE112 Bluetooth Low Energy module.
        /// The USB controller is activated by default when USB is set on in the hardware configuration.On the other
        /// hand, the USB controller cannot be activated if the USB is not set on in the hardware configuration.
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        public async Task ControlUsbAsync(bool enable)
        {
            var enableValue = enable ? (byte)1 : (byte)0;
            var payload = new[] { enableValue };
            var command = Util.GetHardwareCommand(HardwareCommand.UsbEnable, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command enables or disables sleep mode.
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        public async Task ControlSleepAsync(bool enable)
        {
            var enableValue = enable ? (byte)1 : (byte)0;
            var payload = new[] { enableValue };
            var command = Util.GetHardwareCommand(HardwareCommand.SleepEnable, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>This command returns value of hardware Sleep Timer count.</para>
        /// <para>
        /// It can be used (e. g.) for the estimation of statement execution time, as a timestamp, or in code termination after
        /// a timeout.Value of timestamp isn't incremented when the module is in PM3 power mode.
        /// </para>
        /// </summary>
        /// <returns>Sleep Timer count value</returns>
        public async Task<uint> GetTimestampAsync()
        {
            var command = Util.GetHardwareCommand(HardwareCommand.GetTimestamp);
            var response = await WriteAsync(command);
            var timestamp = BitConverter.ToUInt32(response.Payload, 0);
            return timestamp;
        }

        #endregion

        #region Commands - Testing

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
            var payload = new[] { channel, length, type };
            var command = Util.GetTestingCommand(TestingCommand.PhyTX, payload);
            await WriteAsync(command);
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
            var payload = new[] { channel };
            var command = Util.GetTestingCommand(TestingCommand.PhyRX, payload);
            await WriteAsync(command);
        }

        /// <summary>
        /// <para>This command ends a PHY test and report received packets.</para>
        /// <para>PHY - testing commands implement Direct test mode from Bluetooth Core Specification, Volume 6, Part F.</para>
        /// <para>These commands are meant to be used when testing against separate Bluetooth tester.</para>
        /// </summary>
        /// <returns>Received packet counter</returns>
        public async Task<ushort> PhyEndAsync()
        {
            var command = Util.GetTestingCommand(TestingCommand.PhyEnd);
            var response = await WriteAsync(command);
            var counter = BitConverter.ToUInt16(response.Payload, 0);
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
            var command = Util.GetTestingCommand(TestingCommand.GetChannelMap);
            var response = await WriteAsync(command);
            var channelMapLength = response.Payload[0];
            var channelMap = new byte[channelMapLength];
            Array.Copy(response.Payload, 1, channelMap, 0, channelMap.Length);
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
        public async Task SetChannelModeAsync(byte mode)
        {
            var payload = new[] { mode };
            var command = Util.GetTestingCommand(TestingCommand.ChannelMode, payload);
            await WriteAsync(command);
        }

        #endregion

        #region Commands - DFU

        /// <summary>
        /// <para>
        /// This command resets the Bluetooth module or the dongle. This command does not have a response, but the
        /// consequent following event will be the normal boot event (system_boot) or the DFU boot event (dfu_boot) if
        /// the DFU option is used and UART boot loader is installed.
        /// </para>
        /// <para>
        /// There are three available boot loaders: USB for DFU upgrades using the USB-DFU protocol over the USB
        /// interface, UART for DFU upgrades using the BGAPI protocol over the UART interface, and OTA for the
        /// Over-the-Air upgrades.
        /// </para>
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public void Reset1(BootMode mode)
        {
            var modeValue = (byte)mode;
            var payload = new[] { modeValue };
            var command = Util.GetDfuCommand(DfuCommand.Reset, payload);
            Write(command);
        }

        /// <summary>
        /// <para>
        /// After the device has been boot into DFU mode, and if the UART bootloader is used (defined in project
        /// configuration file), this command can be used to start the DFU firmware upgrade.
        /// </para>
        /// <para>The UART DFU process:</para>
        /// <para>1. Boot device to DFU mode with : Reset command.</para>
        /// <para>2. Wait for DFU Boot event</para>
        /// <para>3. Send command Flash Set Address to start the firmware update.</para>
        /// <para>4. Upload the firmware with Flash Upload commands until all the data has been uploaded. Use data
        /// contained in the firmware image.hex file starting from byte offset 0x1000: everything before this offset is
        /// bootloader data which cannot be written using DFU; also, the last 2kB are skipped because they contain
        /// the hardware page and other configuration data that cannot be changed over DFU.</para>
        /// <para>5. Send Flash Upload Finish to when all the data has been uploaded.</para>
        /// <para>6. Finalize the DFU firmware update with command: Reset.</para>
        /// </summary>
        /// <param name="address">
        /// <para>The offset in the flash where to start flashing.</para>
        /// <para>Always use: 0x1000</para>
        /// </param>
        /// <returns></returns>
        public async Task SetFlashAddressAsync(uint address)
        {
            var payload = BitConverter.GetBytes(address);
            var command = Util.GetDfuCommand(DfuCommand.FlashSetAddress, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// <para>
        /// This command is used repeatedly to upload the new binary firmware image to module over the UART interface.
        /// The address on the flash will be updated automatically.
        /// </para>
        /// <para>When all data is uploaded finalize the upload with command: Flash Upload Finish.</para>
        /// </summary>
        /// <param name="data">
        /// <para>An array of data which will be written into the flash.</para>
        /// <para>
        /// The amount of data in the array MUST be 1, 2, 4, 8, 16, 32 or 64 bytes or
        /// otherwise the firmware update will fail.
        /// </para>
        /// </param>
        /// <returns></returns>
        public async Task UploadAsync(byte[] data)
        {
            var payload = new byte[1 + data.Length];
            payload[0] = data.GetByteLength();
            Array.Copy(data, 0, payload, 1, data.Length);
            var command = Util.GetDfuCommand(DfuCommand.FlashUpload, payload);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command tells to the device that the uploading of DFU data has finished. After this command the issue still
        /// Reset command to restart the Bluetooth module in normal mode.
        /// </summary>
        /// <returns></returns>
        public async Task FinishUploadAsync()
        {
            var command = Util.GetDfuCommand(DfuCommand.FlashUploadFinish);
            var response = await WriteAsync(command);
            var errorCode = BitConverter.ToUInt16(response.Payload, 0);
            if (errorCode != 0)
            {
                throw new BGErrorException(errorCode);
            }
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
        public event EventHandler<VersionEventArgs> SystemBooted;
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
        /// <summary>
        /// A BGScript failure has been detected and this event is raised.
        /// </summary>
        public event EventHandler<ScriptErrorEventArgs> ScriptFailed;
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
        /// <summary>
        /// A protocol error was detected in BGAPI command parser. This event is triggered if a BGAPI command from the
        /// host contains syntax error(s), or if a command is only partially sent.Then the BGAPI parser has a 1 second
        /// command timeout and if a valid command is not transmitted within this timeout an error is raised and the partial
        /// or wrong command will be ignored.
        /// </summary>
        public event EventHandler<BGErrorEventArgs> ProtocolErrorDetected;
        /// <summary>
        /// Event is generated when USB enumeration status has changed. This event can be triggered by plugging
        /// module to USB host port or by USB device re-enumeration on host machine.
        /// </summary>
        public event EventHandler<UsbEnumeratedEventArgs> UsbEnumeratedChanged;

        private void OnSystemEventAnalyzed(Message @event)
        {
            var id = (SystemEvent)@event.Id;
            switch (id)
            {
                case SystemEvent.Boot:
                    {
                        var major = BitConverter.ToUInt16(@event.Payload, 0);
                        var minor = BitConverter.ToUInt16(@event.Payload, 2);
                        var patch = BitConverter.ToUInt16(@event.Payload, 4);
                        var build = BitConverter.ToUInt16(@event.Payload, 6);
                        var linkLayer = BitConverter.ToUInt16(@event.Payload, 8);
                        var protocol = @event.Payload[10];
                        var hardware = @event.Payload[11];
                        var version = new Version(major, minor, patch, build, linkLayer, protocol, hardware);
                        var eventArgs = new VersionEventArgs(version);
                        SystemBooted?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.EndpointWatermarkRX:
                    {
                        var endpoint = (Endpoint)@event.Payload[0];
                        var size = @event.Payload[1];
                        var eventArgs = new WatermarkEventArgs(endpoint, size);
                        EndpointWatermarkReceived?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.EndpointWatermarkTX:
                    {
                        var endpoint = (Endpoint)@event.Payload[0];
                        var size = @event.Payload[1];
                        var eventArgs = new WatermarkEventArgs(endpoint, size);
                        EndpointWatermarkWritten?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.ScriptFailure:
                    {
                        var address = BitConverter.ToUInt16(@event.Payload, 0);
                        var errorCode = BitConverter.ToUInt16(@event.Payload, 2);
                        var eventArgs = new ScriptErrorEventArgs(address, errorCode);
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
                        var errorCode = BitConverter.ToUInt16(@event.Payload, 0);
                        var eventArgs = new BGErrorEventArgs(errorCode);
                        ProtocolErrorDetected?.Invoke(this, eventArgs);
                        break;
                    }
                case SystemEvent.UsbEnumerated:
                    {
                        var enumerated = @event.Payload[0] == 1;
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

        #region Events - PS

        /// <summary>
        /// <para>This event is produced during a Persistent Store key dump which in launched with command PS Dump.</para>
        /// <para>
        /// The event reporting a PS Key with address of 0xFFFF and empty value is always sent: it is meant to indicate
        /// that all existing PS Keys have been read.
        /// </para>
        /// </summary>
        public event EventHandler<PSEntryEventArgs> PSDumped;

        private void OnPersistentStoreEventAnalyzed(Message @event)
        {
            var id = (PSEvent)@event.Id;
            switch (id)
            {
                case PSEvent.PSKey:
                    {
                        var key = BitConverter.ToUInt16(@event.Payload, 0);
                        var length = @event.Payload[2];
                        var value = new byte[length];
                        Array.Copy(@event.Payload, 3, value, 0, value.Length);
                        var eventArgs = new PSEntryEventArgs(key, value);
                        PSDumped?.Invoke(this, eventArgs);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        #endregion

        #region Events - Attribute Database

        /// <summary>
        /// This event is produced at the GATT server when a local attribute value was written by a remote device.
        /// </summary>
        public event EventHandler<AttributeValueWrittenEventArgs> AttributeValueWritten;
        /// <summary>
        /// This event is generated when a remote device tries to read an attribute which has the user property enabled.
        /// This event should be responded within 30 seconds with User Read Response command either containing the
        /// data or an error code.
        /// </summary>
        public event EventHandler<UserReadRequestEventArgs> UserReadRequested;
        /// <summary>
        /// This event indicates attribute status flags have changed. For example, this even is generated at the module
        /// acting as the GATT Server whenever the remote GATT Client changes the Client Characteristic Configuration
        /// to start or stop notification or indications from the Server.
        /// </summary>
        public event EventHandler<AttributeStatusEventArgs> AttributeStatusChanged;

        private void OnAttributeDatabaseEventAnalyzed(Message @event)
        {
            var id = (AttributeDatabaseEvent)@event.Id;
            switch (id)
            {
                case AttributeDatabaseEvent.Value:
                    {
                        var connection = @event.Payload[0];
                        var reason = (AttributeValueChangeReason)@event.Payload[1];
                        var attribute = BitConverter.ToUInt16(@event.Payload, 2);
                        var offset = BitConverter.ToUInt16(@event.Payload, 4);
                        var valueLength = @event.Payload[6];
                        var value = new byte[valueLength];
                        Array.Copy(@event.Payload, 7, value, 0, value.Length);
                        var eventArgs = new AttributeValueWrittenEventArgs(connection, reason, attribute, offset, value);
                        AttributeValueWritten?.Invoke(this, eventArgs);
                        break;
                    }
                case AttributeDatabaseEvent.UserReadRequest:
                    {
                        var connection = @event.Payload[0];
                        var attribute = BitConverter.ToUInt16(@event.Payload, 1);
                        var offset = BitConverter.ToUInt16(@event.Payload, 3);
                        var maximum = @event.Payload[5];
                        var eventArgs = new UserReadRequestEventArgs(connection, attribute, offset, maximum);
                        UserReadRequested?.Invoke(this, eventArgs);
                        break;
                    }
                case AttributeDatabaseEvent.Status:
                    {
                        var attribute = BitConverter.ToUInt16(@event.Payload, 0);
                        var status = (AttributeStatus)@event.Payload[2];
                        var eventArgs = new AttributeStatusEventArgs(attribute, status);
                        AttributeStatusChanged?.Invoke(this, eventArgs);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        #endregion

        #region Events - Connection

        /// <summary>
        /// This event indicates the connection status and parameters.
        /// </summary>
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusNotified;
        /// <summary>
        /// This event indicates the remote devices version.
        /// </summary>
        public event EventHandler<DeviceVersionEventArgs> DeviceVersionNotified;
        /// <summary>
        /// This event indicates the remote devices features.
        /// </summary>
        public event EventHandler<DeviceFeatureEventArgs> DeviceFeatureNotified;
        /// <summary>
        /// This event is produced when a Bluetooth connection is disconnected.
        /// </summary>
        public event EventHandler<DeviceDisconnectedEventArgs> Disconnected;

        private void OnConnectionEventAnalyzed(Message @event)
        {
            var id = (ConnectionEvent)@event.Id;
            switch (id)
            {
                case ConnectionEvent.Status:
                    {
                        var connection = @event.Payload[0];
                        var status = (ConnectionStatus)@event.Payload[1];
                        var rawValue = new byte[6];
                        Array.Copy(@event.Payload, 2, rawValue, 0, 6);
                        var type = (AddressType)@event.Payload[8];
                        var interval = BitConverter.ToUInt16(@event.Payload, 9);
                        var timeout = BitConverter.ToUInt16(@event.Payload, 11);
                        var latency = BitConverter.ToUInt16(@event.Payload, 13);
                        var bonding = @event.Payload[15];
                        var address = new Address(type, rawValue);
                        var eventArgs = new ConnectionStatusEventArgs(connection, status, address, interval, timeout, latency, bonding);
                        ConnectionStatusNotified?.Invoke(this, eventArgs);
                        break;
                    }
                case ConnectionEvent.VersionInd:
                    {
                        var connection = @event.Payload[0];
                        var version = @event.Payload[1];
                        var vendorId = BitConverter.ToUInt16(@event.Payload, 2);
                        var subVersion = BitConverter.ToUInt16(@event.Payload, 4);
                        var eventArgs = new DeviceVersionEventArgs(connection, version, vendorId, subVersion);
                        DeviceVersionNotified?.Invoke(this, eventArgs);
                        break;
                    }
                case ConnectionEvent.FeatureInd:
                    {
                        var connection = @event.Payload[0];
                        var featuresLength = @event.Payload[1];
                        var features = new byte[featuresLength];
                        Array.Copy(@event.Payload, 2, features, 0, features.Length);
                        var eventArgs = new DeviceFeatureEventArgs(connection, features);
                        DeviceFeatureNotified.Invoke(this, eventArgs);
                        break;
                    }
                case ConnectionEvent.Disconnected:
                    {
                        var connection = @event.Payload[0];
                        var reason = BitConverter.ToUInt16(@event.Payload, 1);
                        var eventArgs = new DeviceDisconnectedEventArgs(connection, reason);
                        Disconnected?.Invoke(this, eventArgs);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

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
        public event EventHandler<ProcedureCompleteEventArgs> ProcedureCompleted;
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
        public event EventHandler<AttributeValueEventArgs> AttributeValueChanged;
        /// <summary>
        /// This event is a response to a Read Multiple request.
        /// </summary>
        public event EventHandler<MultipleResponseEventArgs> MultipleResopnseRead;

        private void OnAttributeClientEventAnalyzed(Message @event)
        {
            var id = (AttributeClientEvent)@event.Id;
            switch (id)
            {
                case AttributeClientEvent.Indicated:
                    {
                        var connection = @event.Payload[0];
                        var attribute = BitConverter.ToUInt16(@event.Payload, 1);
                        var eventArgs = new AttributeEventArgs(connection, attribute);
                        Indicated?.Invoke(this, eventArgs);
                        break;
                    }
                case AttributeClientEvent.ProcedureCompleted:
                    {
                        var connection = @event.Payload[0];
                        var errorCode = BitConverter.ToUInt16(@event.Payload, 1);
                        var characteristic = BitConverter.ToUInt16(@event.Payload, 3);
                        var eventArgs = new ProcedureCompleteEventArgs(connection, errorCode, characteristic);
                        ProcedureCompleted?.Invoke(this, eventArgs);
                        break;
                    }
                case AttributeClientEvent.GroupFound:
                    {
                        var connection = @event.Payload[0];
                        var start = BitConverter.ToUInt16(@event.Payload, 1);
                        var end = BitConverter.ToUInt16(@event.Payload, 3);
                        var uuidLength = @event.Payload[5];
                        var uuid = new byte[uuidLength];
                        Array.Copy(@event.Payload, 6, uuid, 0, uuid.Length);
                        var eventArgs = new GroupEventArgs(connection, start, end, uuid);
                        GroupFound?.Invoke(this, eventArgs);
                        break;
                    }
                case AttributeClientEvent.FindInformationFound:
                    {
                        var connection = @event.Payload[0];
                        var characteristic = BitConverter.ToUInt16(@event.Payload, 1);
                        var uuidLength = @event.Payload[3];
                        var uuid = new byte[uuidLength];
                        Array.Copy(@event.Payload, 4, uuid, 0, uuid.Length);
                        var eventArgs = new InformationEventArgs(connection, characteristic, uuid);
                        FindInformationFound?.Invoke(this, eventArgs);
                        break;
                    }
                case AttributeClientEvent.AttributeValue:
                    {
                        var connection = @event.Payload[0];
                        var attribute = BitConverter.ToUInt16(@event.Payload, 1);
                        var type = (AttributeType)@event.Payload[3];
                        var valueLength = @event.Payload[4];
                        var value = new byte[valueLength];
                        Array.Copy(@event.Payload, 5, value, 0, value.Length);
                        var eventArgs = new AttributeValueEventArgs(connection, attribute, type, value);
                        AttributeValueChanged?.Invoke(this, eventArgs);
                        break;
                    }
                case AttributeClientEvent.ReadMultipleResponse:
                    {
                        var connection = @event.Payload[0];
                        var attributesLength = @event.Payload[1];
                        var attributes = new byte[attributesLength];
                        Array.Copy(@event.Payload, 2, attributes, 0, attributes.Length);
                        var eventArgs = new MultipleResponseEventArgs(connection, attributes);
                        MultipleResopnseRead?.Invoke(this, eventArgs);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        #endregion

        #region Events - SM

        /// <summary>
        /// This event indicates the bonding has failed for a connection.
        /// </summary>
        public event EventHandler<BondingErrorEventArgs> BondingFailed;
        /// <summary>
        /// This event tells a passkey should be printed to the user for bonding. This passkey must be entered in the
        /// remote device for bonding to be successful.
        /// </summary>
        public event EventHandler<PasskeyEventArgs> PasskeyDisplayed;
        /// <summary>
        /// <para>
        /// This event indicates the Security Manager requests the user to enter passkey. The passkey the user needs to
        /// enter is displayed by the remote device.
        /// </para>
        /// <para>Use Passkey Entry command to respond to request</para>
        /// </summary>
        public event EventHandler<ConnectionEventArgs> PasskeyRequested;
        /// <summary>
        /// This event outputs bonding status information.
        /// </summary>
        public event EventHandler<BondStatusEventArgs> BondStatusChanged;

        private void OnSMEventAnalyzed(Message @event)
        {
            var id = (SMEvent)@event.Id;
            switch (id)
            {
                case SMEvent.BondingFail:
                    {
                        var connection = @event.Payload[0];
                        var errorCode = BitConverter.ToUInt16(@event.Payload, 1);
                        var eventArgs = new BondingErrorEventArgs(connection, errorCode);
                        BondingFailed?.Invoke(this, eventArgs);
                        break;
                    }
                case SMEvent.PassKeyDisplay:
                    {
                        var connection = @event.Payload[0];
                        var passkey = BitConverter.ToUInt32(@event.Payload, 1);
                        var eventArgs = new PasskeyEventArgs(connection, passkey);
                        PasskeyDisplayed?.Invoke(this, eventArgs);
                        break;
                    }
                case SMEvent.PasskeyRequest:
                    {
                        var connection = @event.Payload[0];
                        var eventArgs = new ConnectionEventArgs(connection);
                        PasskeyRequested?.Invoke(this, eventArgs);
                        break;
                    }
                case SMEvent.BondStatus:
                    {
                        var bond = @event.Payload[0];
                        var keySize = @event.Payload[1];
                        var mitm = @event.Payload[2] == 0 ? false : true;
                        var key = (BondingKey)@event.Payload[3];
                        var eventArgs = new BondStatusEventArgs(bond, keySize, mitm, key);
                        BondStatusChanged?.Invoke(this, eventArgs);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        #endregion

        #region Events - GAP

        /// <summary>
        /// This is a scan response event. This event is normally received by a Master which is scanning for advertisement
        /// and scan response packets from Slaves.
        /// </summary>
        public event EventHandler<DiscoveryEventArgs> Discovered;

        private void OnGapEventAnalyzed(Message @event)
        {
            var id = (GapEvent)@event.Id;
            switch (id)
            {
                case GapEvent.ScanResponse:
                    {
                        var rssi = (sbyte)@event.Payload[0];
                        var type = (DiscoveryType)@event.Payload[1];
                        var rawValue = new byte[6];
                        Array.Copy(@event.Payload, 2, rawValue, 0, 6);
                        var addressType = (AddressType)@event.Payload[8];
                        var address = new Address(addressType, rawValue);
                        var bond = @event.Payload[9];
                        var discoveryLength = @event.Payload[10];
                        var discoveryValue = new byte[discoveryLength];
                        Array.Copy(@event.Payload, 11, discoveryValue, 0, discoveryValue.Length);
                        var advertisements = new List<Advertisement>();
                        var i = 0;
                        while (i < discoveryValue.Length)
                        {
                            // Notice that advertisement or scan response data must be formatted in accordance to the Bluetooth Core
                            // Specification.See BLUETOOTH SPECIFICATION Version 4.0[Vol 3 - Part C - Chapter 11].
                            var advertisementLength = discoveryValue[i++];
                            var advertisementType = (AdvertisementType)discoveryValue[i++];
                            var advertisementValue = new byte[advertisementLength - 1];
                            Array.Copy(discoveryValue, i, advertisementValue, 0, advertisementValue.Length);
                            var advertisement = new Advertisement(advertisementType, advertisementValue);
                            advertisements.Add(advertisement);
                            i += advertisementValue.Length;
                        }
                        var discovery = new Discovery(rssi, type, address, bond, advertisements);
                        var eventArgs = new DiscoveryEventArgs(discovery);
                        Discovered?.Invoke(this, eventArgs);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        #endregion

        #region Events - Hardware

        /// <summary>
        /// <para>This event is produced when I/O port status changes.</para>
        /// <para>
        /// The timestamp is only valid if the module doesn't go to PM3 because in that mode the low frequency
        /// oscillator is turned off.Example of such situation is the module in master mode, but not connected to
        /// any slave. If module wakes up from an IO interrupt, then the timestamp in the event will not be
        /// accurate.
        /// </para>
        /// <para>
        /// Setting up the timer by the Set Soft Timer command prevents the module from going to PM3 and
        /// makes timestamps be valid all the time.
        /// </para>
        /// </summary>
        public event EventHandler<IOPortStateEventArgs> IOPortStateChanged;
        /// <summary>
        /// This event is produced when software timer interrupt is generated.
        /// </summary>
        public event EventHandler<TimerEventArgs> TimerGenerated;
        /// <summary>
        /// This events is produced when an A/D converter result is received.
        /// </summary>
        public event EventHandler<ADConverterEventArgs> ADConverterReceived;
        /// <summary>
        /// <para>This event is produced when analog comparator output changes in the configured direction.</para>
        /// <para>
        /// The timestamp is only valid if the module doesn't go to PM3 because in that mode the low frequency
        /// oscillator is turned off.Example of such situation is the module in master mode, but not connected to
        /// any slave. If module wakes up from an analog comparator interrupt, then the timestamp in the event
        /// will not be accurate.
        /// </para>
        /// <para>
        /// Setting up the timer by the Set Soft Timer command prevents the module from going to PM3 and
        /// makes timestamps be valid all the time.
        /// </para>
        /// </summary>
        public event EventHandler<AnalogComparatorEventArgs> AnalogComparatorChanged;
        /// <summary>
        /// This event is produced when the radio hardware error appears. The radio hardware error is caused by an
        /// incorrect state of the radio receiver that reports wrong values of length of packets.The FIFO queue of thereceiver is then wrongly read and as a result the device stops responding. After receiving such event the device
        /// must be restarted in order to recover.
        /// </summary>
        public event EventHandler RadioErrorAppeared;

        private void OnHardwareEventAnalyzed(Message @event)
        {
            var id = (HardwareEvent)@event.Id;
            switch (id)
            {
                case HardwareEvent.IOPortStatus:
                    {
                        var timestamp = BitConverter.ToUInt32(@event.Payload, 0);
                        var port = @event.Payload[4];
                        var interrupt = @event.Payload[5];
                        var state = @event.Payload[6];
                        var eventArgs = new IOPortStateEventArgs(timestamp, port, interrupt, state);
                        IOPortStateChanged?.Invoke(this, eventArgs);
                        break;
                    }
                case HardwareEvent.SoftTimer:
                    {
                        var timer = @event.Payload[0];
                        var eventArgs = new TimerEventArgs(timer);
                        TimerGenerated?.Invoke(this, eventArgs);
                        break;
                    }
                case HardwareEvent.AdcResult:
                    {
                        var input = @event.Payload[0];
                        var value = BitConverter.ToUInt16(@event.Payload, 1);
                        var eventArgs = new ADConverterEventArgs(input, value);
                        ADConverterReceived?.Invoke(this, eventArgs);
                        break;
                    }
                case HardwareEvent.AnalogComparatorStatus:
                    {
                        var timestamp = BitConverter.ToUInt32(@event.Payload, 0);
                        var output = @event.Payload[4];
                        var eventArgs = new AnalogComparatorEventArgs(timestamp, output);
                        AnalogComparatorChanged?.Invoke(this, eventArgs);
                        break;
                    }
                case HardwareEvent.RadioError:
                    {
                        RadioErrorAppeared?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        #endregion

        #region Events - DFU

        /// <summary>
        /// Device has booted up in DFU mode and is ready to receive commands
        /// </summary>
        public event EventHandler<DfuVersionEventArgs> DfuBooted;

        private void OnDeviceFirmwareUpgradeEventAnalyzed(Message @event)
        {
            var id = (DfuEvent)@event.Id;
            switch (id)
            {
                case DfuEvent.Boot:
                    {
                        var version = BitConverter.ToUInt32(@event.Payload, 0);
                        var eventArgs = new DfuVersionEventArgs(version);
                        DfuBooted?.Invoke(this, eventArgs);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        #endregion
    }
}
