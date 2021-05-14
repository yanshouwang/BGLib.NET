using System;
using System.Threading.Tasks;

namespace BGLib.SDK
{
    public abstract class BaseMessenger
    {
        private const byte COMMAND = 0x00;
        private const byte RESPONSE = 0x00;
        private const byte EVENT = 0x01;

        private readonly MessageHub _messageHub;

        protected BaseMessenger(MessageHub messageHub)
        {
            _messageHub = messageHub;
            _messageHub.Analyzed += OnAnalyzed;
        }

        private void OnAnalyzed(object sender, MessageEventArgs e)
        {
            if (e.Message.Type != EVENT ||
                e.Message.Category != Category)
            {
                return;
            }
            global::System.Diagnostics.Debug.WriteLine($"[EVENT] {e.Message.Category}, {e.Message.Id}: {BitConverter.ToString(e.Message.Value)}");
            OnEventAnalyzed(e.Message.Id, e.Message.Value);
        }

        protected void Write(byte id, byte[] commandValue = null)
        {
            var command = new Message(COMMAND, Category, id, commandValue);
            _messageHub.Write(command);
            global::System.Diagnostics.Debug.WriteLine($"[COMMAND] {command.Category}, {command.Id}: {BitConverter.ToString(command.Value)}");
        }

        protected async Task<byte[]> WriteAsync(byte id, byte[] commandValue = null)
        {
            var writeTCS = new TaskCompletionSource<byte[]>();
            var onAnalyzed = new EventHandler<MessageEventArgs>((s, e) =>
            {
                if (e.Message.Type != RESPONSE ||
                    e.Message.Category != Category ||
                    e.Message.Id != id)
                {
                    return;
                }
                writeTCS.TrySetResult(e.Message.Value);
            });
            _messageHub.Analyzed += onAnalyzed;
            try
            {
                Write(id, commandValue);
                return await writeTCS.Task;
            }
            finally
            {
                _messageHub.Analyzed -= onAnalyzed;
            }
        }

        protected abstract byte Category { get; }
        protected abstract void OnEventAnalyzed(byte id, byte[] eventValue);
    }
}
