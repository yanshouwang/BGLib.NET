using System;
using System.Threading.Tasks;

namespace BGLib.SDK.V4.DFU
{
    /// <summary>
    /// <para>
    /// The commands and events in the DFU (Device firmware upgrade) can be used to perform a firmware upgrade
    /// to the local device for example over the UART interface.
    /// </para>
    /// <para>
    /// The commands in this class are only available when the module has been booted into DFU mode with the reset
    /// command.
    /// </para>
    /// <para>
    /// It is not possible to use other commands in DFU mode, bootloader can't parse commands not related with DFU.
    /// </para>
    /// </summary>
    public class MessageWorker : SDK.MessageWorker
    {
        internal MessageWorker(MessageHub messageHub)
            : base(0x09, messageHub)
        {
        }

        protected override void OnEventAnalyzed(byte id, byte[] eventValue)
        {
            switch (id)
            {
                case 0x00:
                    {
                        var version = BitConverter.ToUInt32(eventValue, 0);
                        var eventArgs = new BootEventArgs(version);
                        Boot?.Invoke(this, eventArgs);
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
        /// <param name="dfu">
        /// <para>Whether or not to boot into DFU mode:</para>
        /// <para>0: Reboot normally</para>
        /// <para>1: Reboot into DFU mode for communication with the currently installed boot loader
        /// (UART, USB or OTA)
        /// </para>
        /// </param>
        /// <returns></returns>
        public void Reset(byte dfu)
        {
            var commandValue = new[] { dfu };
            Write(0x00, commandValue);
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
        public async Task SetAddressAsync(uint address)
        {
            var commandValue = BitConverter.GetBytes(address);
            var responseValue = await WriteAsync(0x01, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
            var commandValue = new byte[1 + data.Length];
            commandValue[0] = data.GetByteLength();
            Array.Copy(data, 0, commandValue, 1, data.Length);
            var responseValue = await WriteAsync(0x02, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command tells to the device that the uploading of DFU data has finished. After this command the issue still
        /// Reset command to restart the Bluetooth module in normal mode.
        /// </summary>
        /// <returns></returns>
        public async Task UploadFinishAsync()
        {
            var responseValue = await WriteAsync(0x03);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Device has booted up in DFU mode and is ready to receive commands
        /// </summary>
        public event EventHandler<BootEventArgs> Boot;

        #endregion
    }
}
