using System;
using System.Threading.Tasks;

namespace BGLib.SDK.V4.AttributeDatabase
{
    /// <summary>
    /// The Attribute Database class provides methods to read and write attributes to the local devices Attribute
    /// Database. This class is usually only needed on sensor devices(Attribute server) for example to update
    /// attribute values to the local database based on the sensor readings.A remote device then can access the
    /// GATT database and these values over a Bluetooth connection.
    /// </summary>
    public class MessageWorker : SDK.MessageWorker
    {
        internal MessageWorker(MessageHub messageHub)
            : base(0x02, messageHub)
        {
        }

        protected override void OnEventAnalyzed(byte id, byte[] eventValue)
        {
            switch (id)
            {
                case 0x00:
                    {
                        var connection = eventValue[0];
                        var reason = (AttributeChangeReason)eventValue[1];
                        var handle = BitConverter.ToUInt16(eventValue, 2);
                        var offset = BitConverter.ToUInt16(eventValue, 4);
                        var valueLength = eventValue[6];
                        var value = new byte[valueLength];
                        Array.Copy(eventValue, 7, value, 0, value.Length);
                        var eventArgs = new ValueEventArgs(connection, reason, handle, offset, value);
                        Value?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x01:
                    {
                        var connection = eventValue[0];
                        var handle = BitConverter.ToUInt16(eventValue, 1);
                        var offset = BitConverter.ToUInt16(eventValue, 3);
                        var maxSize = eventValue[5];
                        var eventArgs = new UserReadRequestEventArgs(connection, handle, offset, maxSize);
                        UserReadRequest?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x02:
                    {
                        var handle = BitConverter.ToUInt16(eventValue, 0);
                        var flags = (AttributeStatus)eventValue[2];
                        var eventArgs = new StatusEventArgs(handle, flags);
                        Status?.Invoke(this, eventArgs);
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
        /// This command writes an attribute's value to the local database.
        /// </summary>
        /// <param name="handle">Handle of the attribute to write</param>
        /// <param name="offset">Attribute offset to write data</param>
        /// <param name="value">Value of the attribute to write</param>
        /// <returns></returns>
        public async Task WriteAttributeAsync(ushort handle, byte offset, byte[] value)
        {
            var handleValue = BitConverter.GetBytes(handle);
            var commandValue = new byte[4 + value.Length];
            Array.Copy(handleValue, commandValue, 2);
            commandValue[2] = offset;
            commandValue[3] = value.GetByteLength();
            Array.Copy(value, 0, commandValue, 4, value.Length);
            var responseValue = await WriteAsync(0x00, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
        /// <param name="handle">Handle of the attribute to read</param>
        /// <param name="offset">
        /// <para>Offset to read from.</para>
        /// <para>Maximum of 32 bytes can be read at a time.</para>
        /// </param>
        /// <returns>Value of the attribute</returns>
        public async Task<byte[]> ReadAsync(ushort handle, ushort offset)
        {
            var handleValue = BitConverter.GetBytes(handle);
            var offsetValue = BitConverter.GetBytes(offset);
            var commandValue = new byte[4];
            Array.Copy(handleValue, commandValue, 2);
            Array.Copy(offsetValue, 0, commandValue, 2, 2);
            var responseValue = await WriteAsync(0x01, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 4);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var valueLength = responseValue[6];
            var value = new byte[valueLength];
            Array.Copy(responseValue, 7, value, 0, value.Length);
            return value;
        }

        /// <summary>
        /// This command reads the given attribute's type (UUID) from the local database.
        /// </summary>
        /// <param name="handle">Handle of the attribute to read</param>
        /// <returns></returns>
        public async Task<byte[]> ReadTypeAsync(ushort handle)
        {
            var commandValue = BitConverter.GetBytes(handle);
            var responseValue = await WriteAsync(0x02, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 2);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
            var valueLength = responseValue[4];
            var value = new byte[valueLength];
            Array.Copy(responseValue, 5, value, 0, value.Length);
            return value;
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
        /// <param name="attError">
        /// <para>0: User Read Request is responded with data.</para>
        /// <para>In case of an error an application specific error code can be sent.</para>
        /// </param>
        /// <param name="value">Data to send</param>
        /// <returns></returns>
        public async Task UserReadResponseAsync(byte connection, byte attError, byte[] value)
        {
            var commandValue = new byte[3 + value.Length];
            commandValue[0] = connection;
            commandValue[1] = attError;
            commandValue[2] = value.GetByteLength();
            Array.Copy(value, 0, commandValue, 3, value.Length);
            await WriteAsync(0x03, commandValue);
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
        /// <param name="attError">
        /// <para>Attribute error code to send if an error occurs.</para>
        /// <para>0x0: Write was accepted</para>
        /// <para>0x80-0x9F: Reserved for user defined error codes</para>
        /// </param>
        /// <returns></returns>
        public async Task UserWriteResponseAsync(byte connection, byte attError)
        {
            var commandValue = new[] { connection, attError };
            await WriteAsync(0x04, commandValue);
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
        /// <param name="handle">Attribute handle to send.</param>
        /// <param name="value">Data to send.</param>
        /// <returns></returns>
        public async Task SendAsync(byte connection, ushort handle, byte[] value)
        {
            var handleValue = BitConverter.GetBytes(handle);
            var commandValue = new byte[4 + value.Length];
            commandValue[0] = connection;
            Array.Copy(handleValue, 0, commandValue, 1, 2);
            commandValue[3] = value.GetByteLength();
            Array.Copy(value, 0, commandValue, 4, value.Length);
            var responseValue = await WriteAsync(0x05, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// This event is produced at the GATT server when a local attribute value was written by a remote device.
        /// </summary>
        public event EventHandler<ValueEventArgs> Value;
        /// <summary>
        /// This event is generated when a remote device tries to read an attribute which has the user property enabled.
        /// This event should be responded within 30 seconds with User Read Response command either containing the
        /// data or an error code.
        /// </summary>
        public event EventHandler<UserReadRequestEventArgs> UserReadRequest;
        /// <summary>
        /// This event indicates attribute status flags have changed. For example, this even is generated at the module
        /// acting as the GATT Server whenever the remote GATT Client changes the Client Characteristic Configuration
        /// to start or stop notification or indications from the Server.
        /// </summary>
        public event EventHandler<StatusEventArgs> Status;

        #endregion
    }
}
