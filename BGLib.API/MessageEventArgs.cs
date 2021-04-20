using System;

namespace BGLib.API
{
    internal class MessageEventArgs : EventArgs
    {
        public Message Message { get; }

        public MessageEventArgs(Message message)
        {
            Message = message;
        }
    }
}