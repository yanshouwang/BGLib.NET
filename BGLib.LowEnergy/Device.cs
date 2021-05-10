using BGLib.Core;

namespace BGLib.LowEnergy
{
    public class Device
    {
        private readonly byte _bond;
        private readonly MessageHub _messageHub;

        public Device(Address address, string name, byte bond, MessageHub messageHub)
        {
            Address = address;
            Name = name;
            _bond = bond;
            _messageHub = messageHub;
        }

        public Address Address { get; }
        public string Name { get; }
        public bool Bonded => _bond != 0xFF;
    }
}