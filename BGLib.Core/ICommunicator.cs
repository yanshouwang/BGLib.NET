using System;

namespace BGLib.Core
{
    public interface ICommunicator
    {
        event EventHandler<ValueEventArgs> ValueChanged; 

        void Write(byte[] value);
    }
}
