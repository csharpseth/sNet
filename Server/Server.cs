using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;


namespace sNet.Server
{
	public static class Server
	{
		//Server Stuffz
		private static int m_port;
		private static TcpListener m_tcpListener;
		private static UdpClient m_udpListener;

		private static bool m_listening;

		//Client Management
		private static ClientManager m_clientManager;

		//Public Relations :P
		public static int Port { get { return m_port; } }
		public static ClientManager Clients { get { return m_clientManager; } }
		public static int ClientCount { get { return (m_clientManager != null) ? m_clientManager.NumConnected : 0; } }

		/// <summary>
		/// Starts the server.
		/// </summary>
		/// <param name="port">Port to listen on</param>
		/// <param name="maxClients">Maximum clients that can connect to the server</param>
		public static void Start(int port, int maxClients = 25)
		{
			m_port = port;
			m_clientManager = new ClientManager(maxClients);

			m_tcpListener = new TcpListener(IPAddress.Any, port);
			m_tcpListener.Start();
			m_tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

			m_udpListener = new UdpClient(port);
			m_udpListener.BeginReceive(UdpReceiveCallback, null);

			m_listening = true;
			Log.Msg("Started Successfully");
			ServerEvents.OnServerStart?.Invoke();
		}
		/// <summary>
		/// Stops the server.
		/// </summary>
		public static void Stop()
        {
			if (m_listening == false) return;

			m_clientManager.DisconnectAll();

			if(m_tcpListener != null)
            {
				m_tcpListener.Stop();
				m_tcpListener = null;
			}
			if(m_udpListener != null)
            {
				m_udpListener.Close();
				m_udpListener = null;
			}

			m_clientManager = null;

			m_listening = false;
			ServerEvents.OnServerStop?.Invoke();
        }

		// Converts the data received using the UDP protocol to a byte array that can be read and translated by the PacketBuffer.
		private static void UdpReceiveCallback(IAsyncResult ar)
		{
            try
            {
				if (m_udpListener == null)
					return;

				IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
				byte[] data = m_udpListener.EndReceive(ar, ref clientEndPoint);
				m_udpListener.BeginReceive(UdpReceiveCallback, null);

				if(data.Length < 4)
                {
					return;
                }

                using (PacketBuffer buffer = new PacketBuffer(data))
                {
					int connectionID = buffer.ReadInt();

					if (m_clientManager.ValidID(connectionID) == false)
					{
						Log.Err("Connection ID[ '{0}' ] Validation Error in UdpReceiveCallback Check. Index Exceed Range.");
						return;
					}

					if(m_clientManager.UdpClientIsOpen(connectionID) == true)
                    {
						m_clientManager.UdpEstablishClientConnection(connectionID, clientEndPoint);
						ServerSend.ConnectionStatus(connectionID, ClientConnectionStatus.ConnectedUDP);
						return;
                    }

					if(m_clientManager.UdpPacketIsValid(connectionID, clientEndPoint))
                    {
						m_clientManager.UdpHandleData(connectionID, buffer);
                    }
                }
			}
            catch (Exception e)
            {
				Log.Err(e);
            }
		}

		/// <summary>
		/// Sends data using the UDP protcol to an IPEndPoint
		/// </summary>
		/// <param name="clientEndPoint">Clients IPEndPoint</param>
		/// <param name="buffer">Data to send</param>
		public static void SendUDP(IPEndPoint clientEndPoint, PacketBuffer buffer)
        {
            try
            {
				if(clientEndPoint != null)
                {
					m_udpListener.BeginSend(buffer.ToArray(), buffer.Length(), clientEndPoint, null, null);
				}
            }
            catch (Exception e)
            {
				Log.Err(e);
			}
        }

		//Handles any clients trying to connect to the server
		private static void TCPConnectCallback(IAsyncResult ar)
		{
			TcpClient client = m_tcpListener.EndAcceptTcpClient(ar);
			m_tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);

			if(m_clientManager.TryConnectNewClient(client) != false)
			{
				Log.Msg("Client Has Connected Successfully");
				return;
			}

			Log.Err("Client Connection Attempt Has Failed( SERVER_FULL )");
		}
	}
}