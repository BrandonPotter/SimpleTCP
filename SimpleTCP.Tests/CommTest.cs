using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleTCP.Tests
{
    [TestClass]
    public class CommTest
    {
        private List<string> _clientTx = new List<string>();
        private List<string> _clientRx = new List<string>();
        private List<string> _serverRx = new List<string>();
        private List<string> _serverTx = new List<string>();


        [TestMethod]
        public void SimpleCommTest()
        {
            SimpleTcpServer server = new SimpleTcpServer().Start(8910);
            SimpleTcpClient client = new SimpleTcpClient().Connect(server.GetListeningIPs().FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString(), 8910);

            server.DelimiterDataReceived += (sender, msg) => {
                _serverRx.Add(msg.MessageString);
                string serverReply = Guid.NewGuid().ToString();
                msg.ReplyLine(serverReply);
                _serverTx.Add(serverReply);
            };

            client.DelimiterDataReceived += (sender, msg) => {
                _clientRx.Add(msg.MessageString);
            };

            System.Threading.Thread.Sleep(1000);

            if (server.ConnectedClientsCount == 0)
            {
                Assert.Fail("Server did not register connected client");
            }

            for (int i = 0; i < 10; i++)
            {
                string clientTxMsg = Guid.NewGuid().ToString();
                _clientTx.Add(clientTxMsg);
                client.WriteLine(clientTxMsg);
                System.Threading.Thread.Sleep(100);
            }

            System.Threading.Thread.Sleep(1000);

            for (int i = 0; i < 10; i++)
            {
                if (_clientTx[i] != _serverRx[i])
                {
                    Assert.Fail("Client TX " + i.ToString() + " did not match server RX " + i.ToString());
                }

                if (_serverTx[i] != _clientRx[i])
                {
                    Assert.Fail("Client RX " + i.ToString() + " did not match server TX " + i.ToString());
                }
            }

            var reply = client.WriteLineAndGetReply("TEST", TimeSpan.FromSeconds(1));
            if (reply == null)
            {
                Assert.Fail("WriteLineAndGetReply returned null");
            }

            Assert.IsTrue(true);


        }


        [TestMethod]
        public void MultipleClientsTransmittingToSameServerTest()
        {
            SimpleTcpServer server = new SimpleTcpServer().Start(8911);
            server.Delimiter = Encoding.UTF8.GetBytes("0")[0];
            SimpleTcpClient client1 = new SimpleTcpClient().Connect(server.GetListeningIPs().FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString(), 8911);
            SimpleTcpClient client2 = new SimpleTcpClient().Connect(server.GetListeningIPs().FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString(), 8911);

            System.Threading.Thread.Sleep(1000);
            if (server.ConnectedClientsCount < 2)
            {
                Assert.Fail("Server did not register 2 connected clients");
            }

            string dataRecievedByServer = "";
            server.DelimiterDataReceived += (sender, msg) =>
            {
                dataRecievedByServer += msg.MessageString;
            };

            client1.Write("1111");
            System.Threading.Thread.Sleep(100);
            client2.Write("2222");
            System.Threading.Thread.Sleep(100);
            client1.Write("0");

            System.Threading.Thread.Sleep(1000);

            Assert.AreEqual("1111", dataRecievedByServer);

            server.Stop();
        }
    }
}
