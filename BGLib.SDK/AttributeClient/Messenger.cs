using System;
using System.Threading.Tasks;

namespace BGLib.SDK.AttributeClient
{
    /// <summary>
    /// The Attribute Client class implements the Bluetooth Low Energy Attribute Protocol (ATT) and provides access
    /// to the ATT protocol methods. The Attribute Client class can be used to discover services and characteristics
    /// from the ATT server, read and write values and manage indications and notifications.
    /// </summary>
    public class Messenger : BaseMessenger
    {
        internal Messenger(MessageHub messageHub)
            : base(messageHub)
        {
        }

        protected override byte Category => 0x04;

        protected override void OnEventAnalyzed(byte id, byte[] eventValue)
        {
            switch (id)
            {
                case 0x00:
                    {
                        var connection = eventValue[0];
                        var attrHandle = BitConverter.ToUInt16(eventValue, 1);
                        var eventArgs = new IndicatedEventArgs(connection, attrHandle);
                        Indicated?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x01:
                    {
                        var connection = eventValue[0];
                        var errorCode = BitConverter.ToUInt16(eventValue, 1);
                        var chrHandle = BitConverter.ToUInt16(eventValue, 3);
                        var eventArgs = new ProcedureCompletedEventArgs(connection, errorCode, chrHandle);
                        ProcedureCompleted?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x02:
                    {
                        var connection = eventValue[0];
                        var start = BitConverter.ToUInt16(eventValue, 1);
                        var end = BitConverter.ToUInt16(eventValue, 3);
                        var uuidLength = eventValue[5];
                        var uuid = new byte[uuidLength];
                        Array.Copy(eventValue, 6, uuid, 0, uuid.Length);
                        var eventArgs = new GroupFoundEventArgs(connection, start, end, uuid);
                        GroupFound?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x04:
                    {
                        var connection = eventValue[0];
                        var chrHandle = BitConverter.ToUInt16(eventValue, 1);
                        var uuidLength = eventValue[3];
                        var uuid = new byte[uuidLength];
                        Array.Copy(eventValue, 4, uuid, 0, uuid.Length);
                        var eventArgs = new FindInformationFoundEventArgs(connection, chrHandle, uuid);
                        FindInformationFound?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x05:
                    {
                        var connection = eventValue[0];
                        var attHandle = BitConverter.ToUInt16(eventValue, 1);
                        var type = (AttributeValueType)eventValue[3];
                        var valueLength = eventValue[4];
                        var value = new byte[valueLength];
                        Array.Copy(eventValue, 5, value, 0, value.Length);
                        var eventArgs = new AttributeValueEventArgs(connection, attHandle, type, value);
                        AttributeValue?.Invoke(this, eventArgs);
                        break;
                    }
                case 0x06:
                    {
                        var connection = eventValue[0];
                        var handlesLength = eventValue[1];
                        var handles = new byte[handlesLength];
                        Array.Copy(eventValue, 2, handles, 0, handles.Length);
                        var eventArgs = new ReadMultipleResponseEventArgs(connection, handles);
                        ReadMultipleResopnse?.Invoke(this, eventArgs);
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
            var startValue = BitConverter.GetBytes(start);
            var endValue = BitConverter.GetBytes(end);
            var uuidValue = BitConverter.GetBytes(uuid);
            var commandValue = new byte[8 + value.Length];
            commandValue[0] = connection;
            Array.Copy(startValue, 0, commandValue, 1, 2);
            Array.Copy(endValue, 0, commandValue, 3, 2);
            Array.Copy(uuidValue, 0, commandValue, 5, 2);
            commandValue[7] = value.GetByteLength();
            Array.Copy(value, 0, commandValue, 8, value.Length);
            var responseValue = await WriteAsync(0x00, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
            var commandValue = new byte[6 + uuid.Length];
            commandValue[0] = connection;
            Array.Copy(startValue, 0, commandValue, 1, 2);
            Array.Copy(endValue, 0, commandValue, 3, 2);
            commandValue[5] = uuid.GetByteLength();
            Array.Copy(uuid, 0, commandValue, 6, uuid.Length);
            var responseValue = await WriteAsync(0x01, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
            var commandValue = new byte[6 + uuid.Length];
            commandValue[0] = connection;
            Array.Copy(startValue, 0, commandValue, 1, 2);
            Array.Copy(endValue, 0, commandValue, 3, 2);
            commandValue[5] = uuid.GetByteLength();
            Array.Copy(uuid, 0, commandValue, 6, uuid.Length);
            var responseValue = await WriteAsync(0x02, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
            var commandValue = new byte[5];
            commandValue[0] = connection;
            Array.Copy(startValue, 0, commandValue, 1, 2);
            Array.Copy(endValue, 0, commandValue, 3, 2);
            var responseValue = await WriteAsync(0x03, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
        /// <param name="chrHandle">Attribute handle</param>
        /// <returns></returns>
        public async Task ReadByHandleAsync(byte connection, ushort chrHandle)
        {
            var chrHandleValue = BitConverter.GetBytes(chrHandle);
            var commandValue = new byte[3];
            commandValue[0] = connection;
            Array.Copy(chrHandleValue, 0, commandValue, 1, 2);
            var responseValue = await WriteAsync(0x04, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
        /// <param name="attHandle">Attribute handle to write to</param>
        /// <param name="data">Attribute value</param>
        /// <returns></returns>
        public async Task AttributeWriteAsync(byte connection, ushort attHandle, byte[] data)
        {
            var attHandleValue = BitConverter.GetBytes(attHandle);
            var commandValue = new byte[4 + data.Length];
            commandValue[0] = connection;
            Array.Copy(attHandleValue, 0, commandValue, 1, 2);
            commandValue[3] = data.GetByteLength();
            Array.Copy(data, 0, commandValue, 4, data.Length);
            var responseValue = await WriteAsync(0x05, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
        /// <param name="attHandle">Attribute handle to write</param>
        /// <param name="data">Value for the attribute</param>
        /// <returns></returns>
        public async Task WriteCommandAsync(byte connection, ushort attHandle, byte[] data)
        {
            var attHandleValue = BitConverter.GetBytes(attHandle);
            var commandValue = new byte[4 + data.Length];
            commandValue[0] = connection;
            Array.Copy(attHandleValue, 0, commandValue, 1, 2);
            commandValue[3] = data.GetByteLength();
            Array.Copy(data, 0, commandValue, 4, data.Length);
            var responseValue = await WriteAsync(0x06, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
            var commandValue = new[] { connection };
            var responseValue = await WriteAsync(0x07, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 0);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
        /// <param name="chrHandle">Attribute handle</param>
        /// <returns></returns>
        public async Task ReadLongAsync(byte connection, ushort chrHandle)
        {
            var chrHandleValue = BitConverter.GetBytes(chrHandle);
            var commandValue = new byte[3];
            commandValue[0] = connection;
            Array.Copy(chrHandleValue, 0, commandValue, 1, 2);
            var responseValue = await WriteAsync(0x08, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
        /// <param name="attHandle">Attribute handle</param>
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
        public async Task PrepareWriteAsync(byte connection, ushort attHandle, ushort offset, byte[] data)
        {
            var attHandleValue = BitConverter.GetBytes(attHandle);
            var offsetValue = BitConverter.GetBytes(offset);
            var commandValue = new byte[6 + data.Length];
            commandValue[0] = connection;
            Array.Copy(attHandleValue, 0, commandValue, 1, 2);
            Array.Copy(offsetValue, 0, commandValue, 3, 2);
            commandValue[5] = data.GetByteLength();
            Array.Copy(data, 0, commandValue, 6, data.Length);
            var responseValue = await WriteAsync(0x09, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        /// <summary>
        /// This command can be used to execute or cancel a previously queued prepare_write command on a remote
        /// device.
        /// </summary>
        /// <param name="connection">Connection Handle</param>
        /// <param name="commit">
        /// <para>0: cancels queued writes</para>
        /// <para>1: commits queued writes</para>
        /// </param>
        /// <returns></returns>
        public async Task ExecuteWriteAsync(byte connection, byte commit)
        {
            var commandValue = new[] { connection, commit };
            var responseValue = await WriteAsync(0x0A, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
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
            var commandValue = new byte[2 + handles.Length];
            commandValue[0] = connection;
            commandValue[1] = handles.GetByteLength();
            Array.Copy(handles, 0, commandValue, 2, handles.Length);
            var responseValue = await WriteAsync(0x0B, commandValue);
            var errorCode = BitConverter.ToUInt16(responseValue, 1);
            if (errorCode != 0)
            {
                throw new ErrorException(errorCode);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// <para>
        /// This event is produced at the GATT server side when an attribute is successfully indicated to the GATT client.
        /// </para>
        /// <para>
        /// This means the event is only produced at the GATT server if the indication is acknowledged by the GATT client
        /// (the remote device).
        /// </para>
        /// </summary>
        public event EventHandler<IndicatedEventArgs> Indicated;
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
        public event EventHandler<ProcedureCompletedEventArgs> ProcedureCompleted;
        /// <summary>
        /// This event is produced when an attribute group (a service) is found. Typically this event is produced after Read
        /// by Group Type command.
        /// </summary>
        public event EventHandler<GroupFoundEventArgs> GroupFound;
        /// <summary>
        /// This event is generated when characteristics type mappings are found. This happens yypically after Find
        /// Information command has been issued to discover all attributes of a service.
        /// </summary>
        public event EventHandler<FindInformationFoundEventArgs> FindInformationFound;
        /// <summary>
        /// This event is produced at the GATT client side when an attribute value is passed from the GATT server to the
        /// GATT client.This event is for example produced after a successful Read by Handle operation or when an
        /// attribute is indicated or notified by the remote device.
        /// </summary>
        public event EventHandler<AttributeValueEventArgs> AttributeValue;
        /// <summary>
        /// This event is a response to a Read Multiple request.
        /// </summary>
        public event EventHandler<ReadMultipleResponseEventArgs> ReadMultipleResopnse;
        #endregion
    }
}
