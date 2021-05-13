using System.Collections.Generic;

namespace BGLib.LowEnergy
{
    internal class AddressEqualityComparer : IEqualityComparer<Peripheral>
    {
        public AddressEqualityComparer()
        {
        }

        public bool Equals(Peripheral x, Peripheral y)
        {
            throw new System.NotImplementedException();
        }

        public int GetHashCode(Peripheral obj)
        {
            throw new System.NotImplementedException();
        }
    }
}