using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BGLib.Core
{
    public class MessageHub
    {
        public event EventHandler<MessageEventArgs> Analyzed;

        private readonly ICommunicator _communicator;
        private readonly MessageAnalyzer _analyzer;

        public System.Messenger System { get; }
        public PS.Messenger PS { get; }
        public AttributeDatabase.Messenger AttributeDatabase { get; }
        public Connection.Messenger Connection { get; }
        public AttributeClient.Messenger AttributeClient { get; }
        public SM.Messenger SM { get; }
        public GAP.Messenger GAP { get; }
        public Hardware.Messenger Hardware { get; }
        public Testing.Messenger Testing { get; }
        public DFU.Messenger DFU { get; }

        public MessageHub(ICommunicator communicator)
        {
            _communicator = communicator;
            _analyzer = new MessageAnalyzer();

            System = new System.Messenger(this);
            PS = new PS.Messenger(this);
            AttributeDatabase = new AttributeDatabase.Messenger(this);
            Connection = new Connection.Messenger(this);
            AttributeClient = new AttributeClient.Messenger(this);
            SM = new SM.Messenger(this);
            GAP = new GAP.Messenger(this);
            Hardware = new Hardware.Messenger(this);
            Testing = new Testing.Messenger(this);
            DFU = new DFU.Messenger(this);

            _communicator.ValueChanged += OnValueChanged;
            _analyzer.Analyzed += OnAnalyzed;
        }

        private void OnValueChanged(object sender, ValueEventArgs e)
        {
            _analyzer.Analyze(e.Value);
        }

        private void OnAnalyzed(object sender, MessageEventArgs e)
        {
            Analyzed?.Invoke(this, e);
        }

        public void Write(Message command)
        {
            var value = command.ToArray();
            _communicator.Write(value);
        }
    }
}
