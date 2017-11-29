using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SimpleTCP.Server
{
    internal class ServerListener
    {
        private readonly List<TcpClient> _connectedClients = new List<TcpClient>();
        private readonly List<TcpClient> _disconnectedClients = new List<TcpClient>();
        private readonly SimpleTcpServer _parent;
        private readonly List<byte> _queuedMsg = new List<byte>();
        private byte _delimiter = 0x13;
        private Thread _rxThread;

        public int ConnectedClientsCount => _connectedClients.Count;
        public IEnumerable<TcpClient> ConnectedClients => _connectedClients;

        internal bool QueueStop { get; set; }
        internal IPAddress IpAddress { get; }
        internal int Port { get; }
        internal int ReadLoopIntervalMs { get; set; }
        internal TcpListenerEx Listener { get; }

        internal ServerListener(SimpleTcpServer parentServer, IPAddress ipAddress, int port)
        {
            QueueStop = false;
            _parent = parentServer;
            IpAddress = ipAddress;
            Port = port;
            ReadLoopIntervalMs = 10;

            Listener = new TcpListenerEx(ipAddress, port);
            Listener.Start();

            ThreadPool.QueueUserWorkItem(ListenerLoop);
        }

        private void ListenerLoop(object state)
        {
            while (!QueueStop)
            {
                try
                {
                    RunLoopStep();
                }
                catch
                {

                }

                Thread.Sleep(ReadLoopIntervalMs);
            }
            Listener.Stop();
        }

        private static bool IsSocketConnected(Socket s)
        {
            // https://stackoverflow.com/questions/2661764/how-to-check-if-a-socket-is-connected-disconnected-in-c
            var part1 = s.Poll(1000, SelectMode.SelectRead);
            var part2 = (s.Available == 0);
            return (!part1 || !part2) && s.Connected;
        }

        private void RunLoopStep()
        {
            if (_disconnectedClients.Count > 0)
            {
                var disconnectedClients = _disconnectedClients.ToArray();
                _disconnectedClients.Clear();

                foreach (var disC in disconnectedClients)
                {
                    _connectedClients.Remove(disC);
                    _parent.NotifyClientDisconnected(this, disC);
                }
            }

            if (Listener.Pending())
            {
                var newClient = Listener.AcceptTcpClient();
                _connectedClients.Add(newClient);
                _parent.NotifyClientConnected(this, newClient);
            }

            _delimiter = _parent.Delimiter;

            foreach (var client in _connectedClients)
            {

                if (IsSocketConnected(client.Client) == false)
                {
                    _disconnectedClients.Add(client);
                }

                var bytesAvailable = client.Available;
                if (bytesAvailable == 0)
                {
                    //Thread.Sleep(10);
                    continue;
                }

                var bytesReceived = new List<byte>();

                while (client.Available > 0 && client.Connected)
                {
                    var nextByte = new byte[1];
                    client.Client.Receive(nextByte, 0, 1, SocketFlags.None);
                    bytesReceived.AddRange(nextByte);

                    if (nextByte[0] == _delimiter)
                    {
                        var msg = _queuedMsg.ToArray();
                        _queuedMsg.Clear();
                        _parent.NotifyDelimiterMessageRx(this, client, msg);
                    }
                    else
                    {
                        _queuedMsg.AddRange(nextByte);
                    }
                }

                if (bytesReceived.Count > 0)
                {
                    _parent.NotifyEndTransmissionRx(this, client, bytesReceived.ToArray());
                }
            }
        }

        //Think this method should be deleted
        private void StartThread()
        {
            if (_rxThread != null) { return; }
            _rxThread = new Thread(ListenerLoop) { IsBackground = true };
            _rxThread.Start();
        }
    }
}
