using BGLib.SDK.GAP;
using System;
using System.Threading.Tasks;

namespace BGLib.SDK.System
{
    /// <summary>
    /// The System class provides access to the local device and contains functions for example to query the local
    /// Bluetooth address, read firmware version, read radio packet counters etc.
    /// </summary>
    public class Messenger : BaseMessenger
    {
        internal Messenger(MessageHub messageHub)
            : base(messageHub)
        {
        }

        protected override byte Category => 0x00;

        protected override void OnEventAnalyzed(byte id, byte[] eventValue)
        {
            switch (id)
            {
                case 0x00:
                    {
                        var major = BitConverter.ToUInt16(eventValue, 0);
                        var minor = BitConverter.ToUInt16(eventValue, 2);
                        var patch = BitConverter.ToUInt16(eventValue, 4);
                        var build = BitConverter.ToUInt16(eventValue, 6);
                        var llVersion = BitConverter.ToUInt16(eventValue, 8);
                        var protocolVersion = eventValue[10];
                        var hw = eventValue[11];
                        var eventArgs = new BootEventArgs(major, minor, patch, build, llVersion, protocolVersion, hw);
                        Boot?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x02:
                    {
                        var endpoint = (Endpoint)eventValue[0];
                        var data = eventValue[1];
                        var eventArgs = new EndpointWatermarkRXEventArgs(endpoint, data);
                        EndpointWatermarkRX?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x03:
                    {
                        var endpoint = (Endpoint)eventValue[0];
                        var data = eventValue[1];
                        var eventArgs = new EndpointWatermarkTXEventArgs(endpoint, data);
                        EndpointWatermarkTX?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x04:
                    {
                        var address = BitConverter.ToUInt16(eventValue, 0);
                        var errorCode = BitConverter.ToUInt16(eventValue, 2);
                        var eventArgs = new ScriptFailureEventArgs(address, errorCode);
                        ScriptFailure?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x05:
                    {
                        NoLicenseKey?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                case 0x06:
                    {
                        var errorCode = BitConverter.ToUInt16(eventValue, 0);
                        var eventArgs = new ProtocolErrorEventArgs(errorCode);
                        ProtocolError?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x07:
                    {
                        var state = eventValue[0];
                        var eventArgs = new UsbEnumeratedEventArgs(state);
                        UsbEnumerated?.Invoke(this, eventArgs);
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
        /// This command resets the local device immediately. The command does not have a response.
        /// </summary>
        /// <param name="bootInDFU">
        /// <para>Selects the boot mode</para>
        /// <para>0: boot to main program</para>
        /// <para>1: boot to DFU</para>
        /// </param>
        public void Reset(byte bootInDFU)
        {
            var commandValue = new[] { bootInDFU };
            Write(0x00, commandValue);
        }

        /// <summary>
        /// This command can be used to test if the local device is functional. Similar to a typical "AT" -> "OK" test.
        /// </summary>
        /// <returns></returns>
        public async Task HelloAsync()
        {
            await WriteAsync(0x01);
        }

        /// <summary>
        /// This command reads the local device's public Bluetooth address.
        /// </summary>
        /// <returns></returns>
        public async Task<byte[]> AddressGetAsync()
        {
            var address = await WriteAsync(0x02);
            return address;
        }

        /// <summary>
        /// Read packet counters and resets them, also returns available packet buffers.
        /// </summary>
        /// <returns></returns>
        public async Task<Counters> GetCountersAsync()
        {
            var responseValue = await WriteAsync(0x05);
            var txOK = responseValue[0];
            var txRetry = responseValue[1];
            var rxOK = responseValue[2];
            var rxFail = responseValue[3];
            var mBuf = responseValue[4];
            var counters = new Counters(txOK, txRetry, rxOK, rxFail, mBuf);
            return counters;
        }

        /// <summary>
        /// This command reads the number of supported connections from the local device.
        /// </summary>
        /// <returns>Max supported connections</returns>
        public async Task<byte> GetConnectionsAsync()
        {
            var responseValue = await WriteAsync(0x06);
            var maxConn = responseValue[0];
            return maxConn;
        }

        /// <summary>
        /// This command reads the local devices software and hardware versions.
        /// </summary>
        /// <returns></returns>
        public async Task<Info> GetInfoAsync()
        {
            var responseValue = await WriteAsync(0x08);
            var major = BitConverter.ToUInt16(responseValue, 0);
            var minor = BitConverter.ToUInt16(responseValue, 2);
            var patch = BitConverter.ToUInt16(responseValue, 4);
            var build = BitConverter.ToUInt16(responseValue, 6);
            var llVersion = BitConverter.ToUInt16(responseValue, 8);
            var protocolVersion = responseValue[10];
            var hw = responseValue[11];
            var info = new Info(major, minor, patch, build, llVersion, protocolVersion, hw);
            return info;
        }

        /// <summary>
        /// Send data to endpoint, error is returned if endpoint does not have enough space
        /// </summary>
        /// <param name="endpoint">Endpoint index to send data to</param>
        /// <param name="data">data to send</param>
        /// <returns></returns>
        public async Task EndPointTXAsync(Endpoint endpoint, byte[] data)
        {
            var commandValue = new byte[2 + data.Length];
            commandValue[0] = (byte)endpoint;
            commandValue[1] = data.GetByteLength();
            Array.Copy(data, 0, commandValue, 2, data.Length);
            var responseValue = await WriteAsync(0x09, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
        /// <param name="addressType">Bluetooth address type</param>
        /// <returns></returns>
        public async Task WhitelistAppendAsync(byte[] address, AddressType addressType)
        {
            var commandValue = new byte[7];
            Array.Copy(address, commandValue, 6);
            commandValue[6] = (byte)addressType;
            var responseValue = await WriteAsync(0x0A, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
        /// <param name="addressType">Bluetooth address type</param>
        /// <returns></returns>
        public async Task WhitelistRemoveAsync(byte[] address, AddressType addressType)
        {
            var commandValue = new byte[7];
            Array.Copy(address, commandValue, 6);
            commandValue[6] = (byte)addressType;
            var responseValue = await WriteAsync(0x0B, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
        public async Task WhitelistClearAsync()
        {
            await WriteAsync(0x0C);
        }

        /// <summary>
        /// Read data from an endpoint (i.e., data souce, e.g., UART), error is returned if endpoint does not have enough
        /// data.
        /// </summary>
        /// <param name="endpoint">Endpoint index to read data from</param>
        /// <param name="size">Size of data to read</param>
        /// <returns></returns>
        public async Task<byte[]> EndpointRXAsync(Endpoint endpoint, byte size)
        {
            var commandValue = new byte[2];
            commandValue[0] = (byte)endpoint;
            commandValue[1] = size;
            var responseValue = await WriteAsync(0x0D, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var dataLength = responseValue[2];
            var data = new byte[dataLength];
            Array.Copy(responseValue, 3, data, 0, data.Length);
            return data;
        }

        /// <summary>
        /// Set watermarks on both input and output sides of an endpoint. This is used to enable and disable the following
        /// events: Endpoint Watermark Tx and Endpoint Watermark Rx.
        /// </summary>
        /// <param name="endpoint">Endpoint index to set watermarks.</param>
        /// <param name="rx">
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
        /// <param name="tx">
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
        public async Task EndpointSetWatermarksAsync(Endpoint endpoint, byte rx, byte tx)
        {
            var commandValue = new byte[3];
            commandValue[0] = (byte)endpoint;
            commandValue[1] = rx;
            commandValue[2] = tx;
            var responseValue = await WriteAsync(0x0E, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
        public async Task AesSetKeyAsync(byte[] key)
        {
            var commandValue = new byte[1 + key.Length];
            commandValue[0] = key.GetByteLength();
            Array.Copy(key, commandValue, key.Length);
            await WriteAsync(0x0F, commandValue);
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
        public async Task<byte[]> AesEncryptAsync(byte[] data)
        {
            var commandValue = new byte[1 + data.Length];
            commandValue[0] = data.GetByteLength();
            Array.Copy(data, commandValue, data.Length);
            var responseValue = await WriteAsync(0x10, commandValue);
            var data1Length = responseValue[0];
            var data1 = new byte[data1Length];
            Array.Copy(responseValue, 1, data1, 0, data1.Length);
            return data1;
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
        public async Task<byte[]> AesDecryptAsync(byte[] data)
        {
            var commandValue = new byte[1 + data.Length];
            commandValue[0] = data.GetByteLength();
            Array.Copy(data, commandValue, data.Length);
            var responseValue = await WriteAsync(0x11, commandValue);
            var data1Length = responseValue[0];
            var data1 = new byte[data1Length];
            Array.Copy(responseValue, 1, data1, 0, data1.Length);
            return data1;
        }

        /// <summary>
        /// This command reads the enumeration status of USB device.
        /// </summary>
        /// <returns>
        /// <para>0: USB device is not enumerated</para>
        /// <para>1: USB device is enumerated</para>
        /// </returns>
        public async Task<byte> UsbEnumerationStatusGetAsync()
        {
            var responseValue = await WriteAsync(0x12);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var status = responseValue[2];
            return status;
        }

        /// <summary>
        /// This command returns CRC-16 (polynomial X + X + X + 1) from bootloader. 
        /// </summary>
        /// <returns></returns>
        public async Task<ushort> GetBootloaderCrcAsync()
        {
            var responseValue = await WriteAsync(0x13);
            var crc = BitConverter.ToUInt16(responseValue, 0);
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
        /// <param name="dfu">
        /// <para>Whether or not to boot into DFU mode.</para>
        /// <para>0: Reboot normally</para>
        /// <para>
        /// 1: Reboot into DFU mode for communication with the currently installed
        /// bootloader(UART, USB or OTA)
        /// </para>
        /// </param>
        /// <param name="delayMs">Delay reset in milliseconds</param>
        public void DelayReset(byte dfu, ushort delayMs)
        {
            var delayMsValue = BitConverter.GetBytes(delayMs);
            var commandValue = new byte[3];
            commandValue[0] = dfu;
            Array.Copy(delayMsValue, 0, commandValue, 1, 2);
            Write(0x14, commandValue);
        }

        #endregion

        #region Events

        /// <summary>
        /// <para>
        /// This event is produced when the device boots up and is ready to receive commands
        /// </para>
        /// <para>
        /// This event is not sent over USB interface.
        /// </para>
        /// </summary>
        public event EventHandler<BootEventArgs> Boot;
        /// <summary>
        /// This event is generated if the receive (incoming) buffer of the endpoint has been filled with a number of bytes
        /// equal or higher than the value defined by the command Endpoint Set Watermarks. Data from the receive buffer
        /// can then be read(and consequently cleared) with the command Endpoint Rx.
        /// </summary>
        public event EventHandler<EndpointWatermarkRXEventArgs> EndpointWatermarkRX;
        /// <summary>
        /// This event is generated when the transmit (outgoing) buffer of the endpoint has free space for a number of
        /// bytes equal or higher than the value defined by the command Endpoint Set Watermarks.When there is enough
        /// free space, data can be sent out of the endpoint by the command Endpoint Tx.
        /// </summary>
        public event EventHandler<EndpointWatermarkTXEventArgs> EndpointWatermarkTX;
        /// <summary>
        /// A BGScript failure has been detected and this event is raised.
        /// </summary>
        public event EventHandler<ScriptFailureEventArgs> ScriptFailure;
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
        public event EventHandler<ProtocolErrorEventArgs> ProtocolError;
        /// <summary>
        /// Event is generated when USB enumeration status has changed. This event can be triggered by plugging
        /// module to USB host port or by USB device re-enumeration on host machine.
        /// </summary>
        public event EventHandler<UsbEnumeratedEventArgs> UsbEnumerated;

        #endregion
    }
}
