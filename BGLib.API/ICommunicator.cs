using System;

namespace BGLib.API
{
    public interface ICommunicator
    {
        event EventHandler<ValueEventArgs> ValueChanged; 

        void Write(byte[] value);
    }
}
