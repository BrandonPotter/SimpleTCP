using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SimpleTCP.Server;

namespace SimpleTCP
{
    public class SimpleTcpServer
    {
        private readonly List<ServerListener> _listeners = new List<Server.ServerListener>();

        public byte Delimiter { get; set; }
        public Encoding StringEncoder { get; set; }
        public bool AutoTrimStrings { get; set; }

        public int ConnectedClientsCount => _listeners.Sum(l => l.ConnectedClientsCount);
        public bool IsStarted => _listeners.Any(l => l.Listener.Active);

        public event EventHandler<TcpClient> ClientConnected;
        public event EventHandler<TcpClient> ClientDisconnected;
        public event EventHandler<Message> DelimiterDataReceived;
        public event EventHandler<Message> DataReceived;


        public SimpleTcpServer()
        {
            Delimiter = 0x13;
            StringEncoder = Encoding.UTF8;
        }

        public IEnumerable<IPAddress> GetIpAddresses() 
            => NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .SelectMany(i => i.GetIPProperties().UnicastAddresses.Select(a => a.Address))
                .Distinct()
                .OrderByDescending(RankIpAddress);

        public IEnumerable<IPAddress> GetListeningIPs() 
            => _listeners
                .Select(l => l.IpAddress)
                .Distinct()
                .OrderByDescending(RankIpAddress);

        public void Broadcast(byte[] data)
        {
            foreach (var client in _listeners.SelectMany(x => x.ConnectedClients))
            {
                client.GetStream().Write(data, 0, data.Length);
            }
        }

        public void Broadcast(string data)
        {
            if (data == null)
                return; 

            Broadcast(StringEncoder.GetBytes(data));
        }

        public void BroadcastLine(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            if (data.LastOrDefault() != Delimiter)
            {
                Broadcast(data + StringEncoder.GetString(new[] { Delimiter }));
                return;
            }

            Broadcast(data);
        }

        public int RankIpAddress(IPAddress addr)
        {
            var rankScore = 1000;

            if (IPAddress.IsLoopback(addr))
            {
                // rank loopback below others, even though their routing metrics may be better
                rankScore = 300;
            }
            else if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                rankScore += 100;
                // except...
                if (addr.GetAddressBytes().Take(2).SequenceEqual(new byte[] { 169, 254 }))
                {
                    // APIPA generated address - no router or DHCP server - to the bottom of the pile
                    rankScore = 0;
                }
            }

            if (rankScore <= 500)
                return rankScore;

            foreach (var nic in TryGetCurrentNetworkInterfaces())
            {
                var ipProps = nic.GetIPProperties();
                if (!ipProps.GatewayAddresses.Any())
                    continue;

                if (ipProps.UnicastAddresses.Any(u => u.Address.Equals(addr)))
                {
                    // if the preferred NIC has multiple addresses, boost all equally
                    // (justifies not bothering to differentiate... IOW YAGNI)
                    rankScore += 1000;
                }

                // only considering the first NIC that is UP and has a gateway defined
                break;
            }

            return rankScore;
        }

        public SimpleTcpServer Start(int port, bool ignoreNicsWithOccupiedPorts = true)
        {
            var ipSorted = GetIpAddresses();
			var anyNicFailed = false;
            foreach (var ipAddr in ipSorted)
            {
				try
				{
					Start(ipAddr, port);
				}
				catch (SocketException ex)
				{
					DebugInfo(ex.ToString());
					anyNicFailed = true;
				}
            }

			if (!IsStarted)
				throw new InvalidOperationException("Port was already occupied for all network interfaces");

            if (!anyNicFailed || ignoreNicsWithOccupiedPorts)
                return this;

            Stop();
            throw new InvalidOperationException("Port was already occupied for one or more network interfaces.");
        }

        public SimpleTcpServer Start(int port, AddressFamily addressFamilyFilter)
        {
            var ipSorted = GetIpAddresses()
                .Where(ip => ip.AddressFamily == addressFamilyFilter);

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
            var listener = new ServerListener(this, ipAddress, port);
            _listeners.Add(listener);

            return this;
        }

        public void Stop()
        {
			while (_listeners.Any(l => l.Listener.Active))
            {
				Thread.Sleep(100);
			}
            _listeners.Clear();
        }

        internal void NotifyDelimiterMessageRx(ServerListener listener, TcpClient client, byte[] msg)
        {
            if (DelimiterDataReceived == null)
                return;

            var message = new Message(msg, client, StringEncoder, Delimiter, AutoTrimStrings);
            DelimiterDataReceived(this, message);
        }

        internal void NotifyEndTransmissionRx(ServerListener listener, TcpClient client, byte[] msg)
        {
            if (DataReceived == null)
                return;

            var message = new Message(msg, client, StringEncoder, Delimiter, AutoTrimStrings);
            DataReceived(this, message);
        }

        internal void NotifyClientConnected(ServerListener listener, TcpClient newClient) 
            => ClientConnected?.Invoke(this, newClient);

        internal void NotifyClientDisconnected(ServerListener listener, TcpClient disconnectedClient) 
            => ClientDisconnected?.Invoke(this, disconnectedClient);

        private static IEnumerable<NetworkInterface> TryGetCurrentNetworkInterfaces()
        {
            try
            {
                return NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up);
            }
            catch (NetworkInformationException)
            {
                return Enumerable.Empty<NetworkInterface>();
            }
        }

        #region Debug logging

        private Stopwatch _debugInfoTime;

        [Conditional("DEBUG")]
        private void DebugInfo(string format, params object[] args)
		{
			if (_debugInfoTime == null)
			{
				_debugInfoTime = new Stopwatch();
				_debugInfoTime.Start();
			}
			Debug.WriteLine(_debugInfoTime.ElapsedMilliseconds + ": " + format, args);
		}

        #endregion Debug logging
	}
}
