using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTCP
{
    public class ConnectedClient
    {
        public IPAddress ServerIP { get; internal set; }
        public TcpClient Client { get; internal set; }
    }
}
