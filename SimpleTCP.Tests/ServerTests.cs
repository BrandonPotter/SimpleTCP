using System;
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimpleTCP.Tests
{
	[TestClass]
	public class ServerTests
	{
		[TestMethod]
		public void Listening_port_opens_and_closes_when_server_starts_and_stops()
		{
			var serverPort = 8911;
			Assert.IsTrue(!IsTcpPortListening(serverPort), "Tcp port should be closed before test starts.");

			var server = new SimpleTcpServer().Start(serverPort);
			Assert.IsTrue(IsTcpPortListening(serverPort), "Tcp port should be open when server has started.");

			server.Stop();
			Assert.IsTrue(!IsTcpPortListening(serverPort), "Tcp port should be closed when server has stopped.");
		}



		public static bool IsTcpPortListening(int port)
		{
			return IPGlobalProperties.GetIPGlobalProperties()
				.GetActiveTcpListeners()
				.Where(x => x.Port == port)
				.Any();
		}
	}
}
