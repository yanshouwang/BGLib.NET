using System;

namespace BGLib.SDK
{
    internal interface IMessageAnalyzer
    {
        event EventHandler<MessageEventArgs> Analyzed;

        void Analyze(byte[] value);
    }
}
