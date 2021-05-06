using BGLib.API;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BGLib.WPF.ViewModels
{
    class SerialCommunicator : ICommunicator
    {
        private readonly SerialPort _serial;

        public SerialCommunicator(SerialPort serial)
        {
            _serial = serial;
            _serial.DataReceived += OnDataReceived;
            _serial.Open();
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var value = new byte[_serial.BytesToRead];
            _serial.Read(value, 0, value.Length);
            var eventArgs = new ValueEventArgs(value);
            ValueChanged?.Invoke(this, eventArgs);
        }

        public event EventHandler<ValueEventArgs> ValueChanged;

        public void Write(byte[] value)
        {
            _serial.Write(value, 0, value.Length);
        }
    }
}
