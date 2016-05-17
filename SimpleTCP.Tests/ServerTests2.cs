using System;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimpleTCP.Tests
{
	[TestClass]
	public class ServerTests2
	{
		readonly int _serverPort = 8911;

		[TestMethod]
		public void Start_passes_if_at_all_nics_passed()
		{
			var server = new SimpleTcpServer().Start(_serverPort, false);
			Assert.IsTrue(server.IsStarted, "Server should have started");
			server.Stop();
		}

		[TestMethod]
		public void Start_passes_if_at_least_one_nic_is_free()
		{
			var listener = new System.Net.Sockets.TcpListener(new IPAddress(new byte[] { 127, 0, 0, 1 }), _serverPort);
			listener.Start();
			try
			{
				var server = new SimpleTcpServer().Start(_serverPort);
				Assert.IsTrue(server.IsStarted, "Server should have started on free nics");
				server.Stop();
			}
			finally
			{
				listener.Stop();
			}
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void Start_fails_if_at_all_nics_free_is_required()
		{
			var listener = new System.Net.Sockets.TcpListener(new IPAddress(new byte[] { 127, 0, 0, 1 }), _serverPort);
			listener.Start();
			try
			{
				var server = new SimpleTcpServer().Start(_serverPort, false);
				Assert.IsTrue(server.IsStarted, "Server should have started on free nics");
				server.Stop();
			}
			finally
			{
				listener.Stop();
			}
		}
	}
}
