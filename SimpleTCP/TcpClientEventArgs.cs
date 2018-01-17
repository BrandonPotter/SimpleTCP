using System;
using System.Net.Sockets;

namespace SimpleTCP
{
    public class TcpClientEventArgs : EventArgs
    {
        public TcpClient TcpClient { get; set; }

        public TcpClientEventArgs(TcpClient tcpClient)
        {
            this.TcpClient = tcpClient;
        }
    }
}
