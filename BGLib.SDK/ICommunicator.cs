using System;

namespace BGLib.SDK
{
    public interface ICommunicator
    {
        event EventHandler<ValueEventArgs> ValueChanged; 

        void Write(byte[] value);
    }
}
