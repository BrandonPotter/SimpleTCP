using System.Net;
using System.Net.Sockets;

namespace SimpleTCP
{
    public class ConnectedClient
    {
        public IPAddress ServerIP { get; internal set; }
        public TcpClient Client { get; internal set; }
    }
}
