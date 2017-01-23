using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTCP
{
    public class Message
    {
        private TcpClient _tcpClient;
        private System.Text.Encoding _encoder = null;
        private byte _writeLineDelimiter;
        private bool _autoTrim = false;
        internal Message(byte[] data, TcpClient tcpClient, System.Text.Encoding stringEncoder, byte lineDelimiter)
        {
            Data = data;
            _tcpClient = tcpClient;
            _encoder = stringEncoder;
            _writeLineDelimiter = lineDelimiter;
        }

        internal Message(byte[] data, TcpClient tcpClient, System.Text.Encoding stringEncoder, byte lineDelimiter, bool autoTrim)
        {
            Data = data;
            _tcpClient = tcpClient;
            _encoder = stringEncoder;
            _writeLineDelimiter = lineDelimiter;
            _autoTrim = autoTrim;
        }

        public byte[] Data { get; private set; }
        public string MessageString
        {
            get
            {
                if (_autoTrim)
                {
                    return _encoder.GetString(Data).Trim();
                }

                return _encoder.GetString(Data);
            }
        }

        public void Reply(byte[] data)
        {
            _tcpClient.GetStream().Write(data, 0, data.Length);
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
                Reply(data + _encoder.GetString(new byte[] { _writeLineDelimiter }));
            } else
            {
                Reply(data);
            }
        }

        public TcpClient TcpClient {  get { return _tcpClient; } }
    }
}
