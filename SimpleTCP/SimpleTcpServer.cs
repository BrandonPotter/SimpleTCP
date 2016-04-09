using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTCP
{
    public class SimpleTcpServer
    {
        public SimpleTcpServer()
        {
            Delimiter = 0x13;
            StringEncoder = System.Text.ASCIIEncoding.UTF8;
        }

        private List<Server.ServerListener> _listeners = new List<Server.ServerListener>();
        public byte Delimiter { get; set; }
        public System.Text.Encoding StringEncoder { get; set; }
        public bool AutoTrimStrings { get; set; }

        public event EventHandler<TcpClient> ClientConnected;
        public event EventHandler<TcpClient> ClientDisconnected;
        public event EventHandler<Message> DelimiterDataReceived;
        public event EventHandler<Message> DataReceived;

        public IEnumerable<IPAddress> GetIPAddresses()
        {
            List<IPAddress> ipAddresses = new List<IPAddress>();

            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                {
                    if (!ipAddresses.Contains(addr.Address))
                    {
                        ipAddresses.Add(addr.Address);
                    }
                }
            }

            var ipSorted = ipAddresses.OrderByDescending(ip => RankIpAddress(ip)).ToList();
            return ipSorted;
        }

        public List<IPAddress> GetListeningIPs()
        {
            List<IPAddress> listenIps = new List<IPAddress>();
            foreach (var l in _listeners)
            {
                if (!listenIps.Contains(l.IPAddress))
                {
                    listenIps.Add(l.IPAddress);
                }
            }

            return listenIps.OrderByDescending(ip => RankIpAddress(ip)).ToList();
        }
        
        public void Broadcast(byte[] data)
        {
            foreach(var client in _listeners.SelectMany(x => x.ConnectedClients))
            {
                client.GetStream().Write(data, 0, data.Length);
            }
        }

        public void Broadcast(string data)
        {
            if (data == null) { return; }
            Broadcast(StringEncoder.GetBytes(data));
        }

        public void BroadcastLine(string data)
        {
            if (string.IsNullOrEmpty(data)) { return; }
            if (data.LastOrDefault() != Delimiter)
            {
                Broadcast(data + StringEncoder.GetString(new byte[] { Delimiter }));
            }
            else
            {
                Broadcast(data);
            }
        }

        private int RankIpAddress(IPAddress addr)
        {
            int rankScore = 1000;

            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                rankScore += 100;
            }

            // class A
            if (addr.ToString().StartsWith("10."))
            {
                rankScore += 100;
            }
            
            // class B
            if (addr.ToString().StartsWith("172.30."))
            {
                rankScore += 100;
            }

            // class C
            if (addr.ToString().StartsWith("192.168.1."))
            {
                rankScore += 100;
            }

            // local sucks
            if (addr.ToString().StartsWith("169."))
            {
                rankScore = 0;
            }

            return rankScore;
        }

        public SimpleTcpServer Start(int port)
        {
            var ipSorted = GetIPAddresses();
            foreach (var ipAddr in ipSorted)
            {
                try
                {
                    Start(ipAddr, port);
                }
                catch { }
            }

            return this;
        }

        public SimpleTcpServer Start(int port, AddressFamily addressFamilyFilter)
        {
            var ipSorted = GetIPAddresses().Where(ip => ip.AddressFamily == addressFamilyFilter);
            foreach (var ipAddr in ipSorted)
            {
                try
                {
                    Start(ipAddr, port);
                }
                catch { }
            }

            return this;
        }

        public SimpleTcpServer Start(IPAddress ipAddress, int port)
        {
            Server.ServerListener listener = new Server.ServerListener(this, ipAddress, port);
            _listeners.Add(listener);

            return this;
        }

        public void Stop()
        {
            foreach (var listener in _listeners)
            {
                try
                {
                    listener.QueueStop = true;
                }
                catch { }
            }

            _listeners.Clear();
        }

        public int ConnectedClientsCount
        {
            get {
                return _listeners.Sum(l => l.ConnectedClientsCount);
            }
        }

        internal void NotifyDelimiterMessageRx(Server.ServerListener listener, TcpClient client, byte[] msg)
        {
            if (DelimiterDataReceived != null)
            {
                Message m = new Message(msg, client, StringEncoder, Delimiter, AutoTrimStrings);
                DelimiterDataReceived(this, m);
            }
        }

        internal void NotifyEndTransmissionRx(Server.ServerListener listener, TcpClient client, byte[] msg)
        {
            if (DataReceived != null)
            {
                Message m = new Message(msg, client, StringEncoder, Delimiter, AutoTrimStrings);
                DataReceived(this, m);
            }
        }

        internal void NotifyClientConnected(Server.ServerListener listener, TcpClient newClient)
        {
            if (ClientConnected != null)
            {
                ClientConnected(this, newClient);
            }
        }

        internal void NotifyClientDisconnected(Server.ServerListener listener, TcpClient disconnectedClient)
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected(this, disconnectedClient);
            }
        }
    }
}
