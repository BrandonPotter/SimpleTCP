﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleTCP
{
	public class SimpleTcpClient : IDisposable
	{
		public SimpleTcpClient()
		{
			StringEncoder = System.Text.Encoding.UTF8;
			ReadLoopIntervalMs = 10;
			Delimiter = 0x13;
		}

		private Thread _rxThread = null;
		private List<byte> _queuedMsg = new List<byte>();
		public byte Delimiter { get; set; }
		public System.Text.Encoding StringEncoder { get; set; }
		private TcpClient _client = null;

		public event EventHandler<Message> DelimiterDataReceived;
		public event EventHandler<Message> DataReceived;

		internal bool QueueStop { get; set; }
		internal int ReadLoopIntervalMs { get; set; }
		public bool AutoTrimStrings { get; set; }

		public SimpleTcpClient Connect(string hostNameOrIpAddress, int port)
		{
			if (string.IsNullOrEmpty(hostNameOrIpAddress))
			{
				throw new ArgumentNullException("hostNameOrIpAddress");
			}

			_client = new TcpClient();
			_client.Connect(hostNameOrIpAddress, port);

			StartRxThread();

			return this;
		}

		private void StartRxThread()
		{
			if (_rxThread != null) { return; }

			_rxThread = new Thread(ListenerLoop);
			_rxThread.IsBackground = true;
			_rxThread.Start();
		}

		public SimpleTcpClient Disconnect()
		{
			if (_client == null) { return this; }
			_client.Close();
			_client = null;
			return this;
		}

		public TcpClient TcpClient { get { return _client; } }

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

				System.Threading.Thread.Sleep(ReadLoopIntervalMs);
			}

			_rxThread = null;
		}

		private void RunLoopStep()
		{
			if (_client == null) { return; }
			if (_client.Connected == false) { return; }

			var delimiter = this.Delimiter;
			var c = _client;

			int bytesAvailable = c.Available;
			if (bytesAvailable == 0)
			{
				System.Threading.Thread.Sleep(10);
				return;
			}

			List<byte> bytesReceived = new List<byte>();

			while (c.Available > 0 && c.Connected)
			{
				byte[] nextByte = new byte[1];
				c.Client.Receive(nextByte, 0, 1, SocketFlags.None);
				bytesReceived.AddRange(nextByte);
				if (nextByte[0] == delimiter)
				{
					byte[] msg = _queuedMsg.ToArray();
					_queuedMsg.Clear();
					NotifyDelimiterMessageRx(c, msg);
				}
				else
				{
					_queuedMsg.AddRange(nextByte);
				}
			}

			if (bytesReceived.Count > 0)
			{
				NotifyEndTransmissionRx(c, bytesReceived.ToArray());
			}
		}

		private void NotifyDelimiterMessageRx(TcpClient client, byte[] msg)
		{
			if (DelimiterDataReceived != null)
			{
				Message m = new Message(msg, client, StringEncoder, Delimiter, AutoTrimStrings);
				DelimiterDataReceived(this, m);
			}
		}

		private void NotifyEndTransmissionRx(TcpClient client, byte[] msg)
		{
			if (DataReceived != null)
			{
				Message m = new Message(msg, client, StringEncoder, Delimiter, AutoTrimStrings);
				DataReceived(this, m);
			}
		}

		public void Write(byte[] data)
		{
			if (_client == null) { throw new Exception("Cannot send data to a null TcpClient (check to see if Connect was called)"); }
			_client.GetStream().Write(data, 0, data.Length);
		}

		public void Write(string data)
		{
			if (data == null) { return; }
			Write(StringEncoder.GetBytes(data));
		}

		public void WriteLine(string data)
		{
			if (string.IsNullOrEmpty(data)) { return; }
			if (data.LastOrDefault() != Delimiter)
			{
				Write(data + StringEncoder.GetString(new byte[] { Delimiter }));
			}
			else
			{
				Write(data);
			}
		}

		public Message WriteLineAndGetReply(string data, TimeSpan timeout)
		{
			Message mReply = null;
			this.DataReceived += (s, e) => { mReply = e; };
			WriteLine(data);

			Stopwatch sw = new Stopwatch();
			sw.Start();

			while (mReply == null && sw.Elapsed < timeout)
			{
				System.Threading.Thread.Sleep(10);
			}

			return mReply;
		}


		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).

				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.
				QueueStop = true;
				if (_client != null)
				{
					try
					{
						_client.Close();
					}
					catch { }
					_client = null;
				}

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~SimpleTcpClient() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}