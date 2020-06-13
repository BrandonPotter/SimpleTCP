using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimpleTCP
{
    public class SimpleUdpClient : IDisposable
    {
        private Thread _thread;

        internal bool QueueStop { get; set; }

        public bool Connected { get; private set; }

        public byte Delimiter { get; set; }

        public UdpClient UdpClient { get; private set; }

        public IPEndPoint EndPoint { get; set; }

        public SimpleUdpClient()
        {
            Connected = false;
            Delimiter = 0x13;
        }

        public event EventHandler<MessagemUdp> DataReceived;

        public void Connect(string hostName, int port)
        {
            try
            {
                EndPoint = new IPEndPoint(IPAddress.Parse(hostName), port);

                UdpClient = new UdpClient(hostName, port);
                UdpClient.Connect(hostName, port);

                Connected = UdpClient.Client.Connected;

                if (!Connected) return;
                if (_thread != null) return;

                _thread = new Thread(ListLoop) { IsBackground = true };
                _thread.Start();

            }
            catch (Exception erro)
            {
                throw new Exception(erro.Message);
            }
        }

        public void Write(byte[] dados)
        {
            try
            {
                if (UdpClient?.Client == null) return;
                if (!Connected) return;
                if (!UdpClient.Client.Connected) return;
                UdpClient?.Send(dados, dados.Length);
            }
            catch (Exception erro)
            {
                throw new Exception(erro.Message);
            }
        }

        public void Disconnect()
        {
            try
            {
                QueueStop = false;
                Connected = false;
                UdpClient?.Close();
            }
            catch (Exception erro)
            {
                throw new Exception(erro.Message);
            }
        }

        private void ListLoop()
        {
            while (!QueueStop)
            {
                try
                {
                    RunLoopStep();
                }
                catch
                {
                    //
                }

                Thread.Sleep(10);
            }

            _thread = null;
        }

        private void RunLoopStep()
        {
            if (UdpClient == null) { return; }
            if (UdpClient.Client.Connected == false) { return; }
            var c = UdpClient;

            var bytesAvailable = c.Available;
            if (bytesAvailable == 0)
            {
                Thread.Sleep(10);
                return;
            }

            var bytesReceived = new List<byte>();

            var rec = EndPoint;

            while (c.Available > 0 && UdpClient.Client.Connected)
            {
                bytesReceived.AddRange(c.Receive(ref rec));
            }

            if (bytesReceived.Count > 0)
            {
                NotifyEndTransmissionRx(c, bytesReceived.ToArray());
            }
        }

        private void NotifyEndTransmissionRx(UdpClient client, byte[] msg)
        {
            if (DataReceived == null) return;
            var m = new MessagemUdp(msg, client, Encoding.ASCII, Delimiter, false);
            DataReceived(this, m);
        }

        public void Dispose()
        {
            QueueStop = false;
            UdpClient.Close();
            ((IDisposable)UdpClient)?.Dispose();
        }

        protected virtual void OnDataReceived(MessagemUdp e)
        {
            DataReceived?.Invoke(this, e);
        }
    }
}
