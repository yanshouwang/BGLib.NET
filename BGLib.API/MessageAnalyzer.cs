using System;
using System.Collections.Generic;
using System.Linq;

namespace BGLib.API
{
    internal class MessageAnalyzer
    {
        public event EventHandler<MessageEventArgs> Analyzed;

        private const ushort MAXIMUM = 64;

        private readonly IList<byte> _payload;

        private byte? _type;
        private byte? _length;
        private byte? _class;
        private byte? _id;

        public MessageAnalyzer()
        {
            _payload = new List<byte>();
        }

        public void Analyze(byte value)
        {
            if (_type == null)
            {
                AnalyzeType(value);
            }
            else if (_length == null)
            {
                AnalyzeLength(value);
            }
            else if (_class == null)
            {
                AnalyzeClass(value);
            }
            else if (_id == null)
            {
                AnalyzeId(value);
            }
            else
            {
                AnalyzePayload(value);
            }
        }

        private void AnalyzeType(byte value)
        {
            // X0000000, `LENGTH_HIGH` is always zero.
            var masked = value & 0x7F;
            if (masked != 0x00)
                return;
            _type = (byte)(value >> 7);
        }

        private void AnalyzeLength(byte value)
        {
            if (value > MAXIMUM)
            {
                _type = null;
            }
            else
            {
                _length = value;
            }
        }

        private void AnalyzeClass(byte value)
        {
            var type = typeof(MessageClass);
            var defined = Enum.IsDefined(type, value);
            if (defined)
            {
                _class = value;
            }
            else
            {
                _type = null;
                _length = null;
            }
        }

        private void AnalyzeId(byte value)
        {
            var type = (MessageType)_type;
            var @class = (MessageClass)_class;
            var enumType = default(Type);
            switch (type)
            {
                case MessageType.Response:
                    {
                        switch (@class)
                        {
                            case MessageClass.System:
                                {
                                    enumType = typeof(SystemCommand);
                                    break;
                                }
                            case MessageClass.PersistentStore:
                                {
                                    enumType = typeof(PersistentStoreCommand);
                                    break;
                                }
                            case MessageClass.AttributeDatabase:
                                {
                                    enumType = typeof(AttributeDatabaseCommand);
                                    break;
                                }
                            case MessageClass.Connection:
                                {
                                    enumType = typeof(ConnectionCommand);
                                    break;
                                }
                            case MessageClass.AttributeClient:
                                {
                                    enumType = typeof(AttributeClientCommand);
                                    break;
                                }
                            case MessageClass.SecurityManager:
                                {
                                    enumType = typeof(SecurityManagerCommand);
                                    break;
                                }
                            case MessageClass.GenericAccessProfile:
                                {
                                    enumType = typeof(GenericAccessProfileCommand);
                                    break;
                                }
                            case MessageClass.Hardware:
                                {
                                    enumType = typeof(HardwareCommand);
                                    break;
                                }
                            case MessageClass.Testing:
                                {
                                    enumType = typeof(TestingCommand);
                                    break;
                                }
                            case MessageClass.DeviceFirmwareUpgrade:
                                {
                                    enumType = typeof(DeviceFirmwareUpgradeCommand);
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                        break;
                    }
                case MessageType.Event:
                    {
                        switch (@class)
                        {
                            case MessageClass.System:
                                {
                                    enumType = typeof(SystemEvent);
                                    break;
                                }
                            case MessageClass.PersistentStore:
                                {
                                    enumType = typeof(PersistentStoreEvent);
                                    break;
                                }
                            case MessageClass.AttributeDatabase:
                                {
                                    enumType = typeof(AttributeDatabaseEvent);
                                    break;
                                }
                            case MessageClass.Connection:
                                {
                                    enumType = typeof(ConnectionEvent);
                                    break;
                                }
                            case MessageClass.AttributeClient:
                                {
                                    enumType = typeof(AttributeClientEvent);
                                    break;
                                }
                            case MessageClass.SecurityManager:
                                {
                                    enumType = typeof(SecurityManagerEvent);
                                    break;
                                }
                            case MessageClass.GenericAccessProfile:
                                {
                                    enumType = typeof(GenericAccessProfileEvent);
                                    break;
                                }
                            case MessageClass.Hardware:
                                {
                                    enumType = typeof(HardwareEvent);
                                    break;
                                }
                            case MessageClass.Testing:
                                {
                                    // Testing doesn't have events.
                                    break;
                                }
                            case MessageClass.DeviceFirmwareUpgrade:
                                {
                                    enumType = typeof(DeviceFirmwareUpgradeEvent);
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            var defined = enumType != default && Enum.IsDefined(enumType, value);
            if (defined)
            {
                _id = value;
            }
            else
            {
                _type = null;
                _length = null;
                _class = null;
            }
        }

        private void AnalyzePayload(byte value)
        {
            _payload.Add(value);
            if (_payload.Count < _length)
                return;
            var type = (byte)_type;
            var @class = (byte)_class;
            var id = (byte)_id;
            var payload = _payload.ToArray();
            _type = null;
            _length = null;
            _class = null;
            _id = null;
            _payload.Clear();
            Message message;
            if (type == (byte)MessageType.Response)
            {
                message = new Response(@class, id, payload);
            }
            else
            {
                message = new Event(@class, id, payload);
            }
            var e = new MessageEventArgs(message);
            Analyzed?.Invoke(this, e);
        }
    }
}
