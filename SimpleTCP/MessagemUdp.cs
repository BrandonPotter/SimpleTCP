using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace SimpleTCP
{
    public class MessagemUdp
    {
        private UdpClient _udpClient;
        private Encoding _encoder;
        private byte _writeLineDelimiter;
        private bool _autoTrim;

        public byte[] Data { get; }

        public UdpClient TcpClient => _udpClient;

        internal MessagemUdp(byte[] data, UdpClient udpClient, Encoding stringEncoder, byte lineDelimiter, bool autoTrim)
        {
            Data = data;
            _udpClient = udpClient;
            _encoder = stringEncoder;
            _writeLineDelimiter = lineDelimiter;
            _autoTrim = autoTrim;
        }

        public string MessageString => _autoTrim ? _encoder.GetString(Data).Trim() : _encoder.GetString(Data);

        public void Reply(byte[] data)
        {
            _udpClient.Send(data, data.Length);
        }

        public void Reply(string data)
        {
            if (string.IsNullOrEmpty(data)) { return; }
            Reply(_encoder.GetBytes(data));
        }

        public void ReplyLine(string data)
        {
            if (string.IsNullOrEmpty(data)) { return; }
            if (data.LastOrDefault() != _writeLineDelimiter)
            {
                Reply(data + _encoder.GetString(new[] { _writeLineDelimiter }));
            }
            else
            {
                Reply(data);
            }
        }
    }
}