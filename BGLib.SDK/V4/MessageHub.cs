using System.IO.Ports;

namespace BGLib.SDK.V4
{
    public class MessageHub : SDK.MessageHub
    {
        public System.MessageWorker System { get; }
        public PS.MessageWorker PS { get; }
        public AttributeDatabase.MessageWorker AttributeDatabase { get; }
        public Connection.MessageWorker Connection { get; }
        public AttributeClient.MessageWorker AttributeClient { get; }
        public SM.MessageWorker SM { get; }
        public GAP.MessageWorker GAP { get; }
        public Hardware.MessageWorker Hardware { get; }
        public Testing.MessageWorker Testing { get; }
        public DFU.MessageWorker DFU { get; }

        public MessageHub(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
            : base(0x00, portName, baudRate, parity, dataBits, stopBits)
        {
            System = new System.MessageWorker(this);
            PS = new PS.MessageWorker(this);
            AttributeDatabase = new AttributeDatabase.MessageWorker(this);
            Connection = new Connection.MessageWorker(this);
            AttributeClient = new AttributeClient.MessageWorker(this);
            SM = new SM.MessageWorker(this);
            GAP = new GAP.MessageWorker(this);
            Hardware = new Hardware.MessageWorker(this);
            Testing = new Testing.MessageWorker(this);
            DFU = new DFU.MessageWorker(this);
        }
    }
}
