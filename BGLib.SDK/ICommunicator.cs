using System;

namespace BGLib.SDK
{
    internal interface ICommunicator
    {
        event EventHandler<ValueEventArgs> ValueChanged; 

        void Write(byte[] value);
    }
}
