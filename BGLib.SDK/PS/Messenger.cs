using System;
using System.Threading.Tasks;

namespace BGLib.SDK.PS
{
    /// <summary>
    /// The Persistent Store (PS) class provides methods to read write and dump the local devices parameters (PS
    /// keys). The persistent store is an abstract data storage on the local devices flash where an application can store
    /// data for future use.
    /// </summary>
    public class Messenger : BaseMessenger
    {
        internal Messenger(MessageHub messageHub)
            : base(messageHub)
        {
        }

        protected override byte Category => 0x01;

        protected override void OnEventAnalyzed(byte id, byte[] eventValue)
        {
            switch (id)
            {
                case 0x00:
                    {
                        var key = BitConverter.ToUInt16(eventValue, 0);
                        var valueLength = eventValue[2];
                        var value = new byte[valueLength];
                        Array.Copy(eventValue, 3, value, 0, value.Length);
                        var eventArgs = new KeyEventArgs(key, value);
                        Key?.Invoke(this, eventArgs);
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
        /// This command defragments the Persistent Store.
        /// </summary>
        /// <returns></returns>
        public async Task DefragAsync()
        {
            await WriteAsync(0x00);
        }

        /// <summary>
        /// This command dumps all Persistent Store keys.
        /// </summary>
        /// <returns></returns>
        public async Task DumpAsync()
        {
            await WriteAsync(0x01);
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
            await WriteAsync(0x02);
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
            var commandValue = new byte[3 + value.Length];
            Array.Copy(keyValue, commandValue, 2);
            commandValue[2] = value.GetByteLength();
            Array.Copy(value, 0, commandValue, 3, value.Length);
            var responseValue = await WriteAsync(0x03, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
            var commandValue = BitConverter.GetBytes(key);
            var responseValue = await WriteAsync(0x04, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var length = responseValue[2];
            var value = new byte[length];
            Array.Copy(responseValue, 3, value, 0, length);
            return value;
        }

        /// <summary>
        /// This command erases a Persistent Store key given as parameter.
        /// </summary>
        /// <param name="key">
        /// <para>Key to erase</para>
        /// <para>Values: 0x8000 to 0x807F</para>
        /// </param>
        /// <returns></returns>
        public async Task EraseAsync(ushort key)
        {
            var commandValue = BitConverter.GetBytes(key);
            await WriteAsync(0x05, commandValue);
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
        /// <param name="page">
        /// <para>Index of memory page to erase</para>
        /// <para>0: First 2kB flash page</para>
        /// <para>1: Next 2kB flash page</para>
        /// <para>etc.</para>
        /// </param>
        /// <returns></returns>
        public async Task ErasePageAsync(byte page)
        {
            var commandValue = new[] { page };
            var responseValue = await WriteAsync(0x06, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
            var commandValue = new byte[5 + data.Length];
            Array.Copy(addressValue, commandValue, 4);
            commandValue[4] = data.GetByteLength();
            Array.Copy(data, 0, commandValue, 5, data.Length);
            var responseValue = await WriteAsync(0x07, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
            var commandValue = new byte[5];
            Array.Copy(addressValue, commandValue, 4);
            commandValue[4] = length;
            var responseValue = await WriteAsync(0x08, commandValue);
            var dataLength = responseValue[0];
            var data = new byte[dataLength];
            if (data.Length > 0)
            {
                Array.Copy(responseValue, 1, data, 0, data.Length);
            }
            return data;
        }

        #endregion

        #region Events

        /// <summary>
        /// <para>This event is produced during a Persistent Store key dump which in launched with command PS Dump.</para>
        /// <para>
        /// The event reporting a PS Key with address of 0xFFFF and empty value is always sent: it is meant to indicate
        /// that all existing PS Keys have been read.
        /// </para>
        /// </summary>
        public event EventHandler<KeyEventArgs> Key;

        #endregion
    }
}
