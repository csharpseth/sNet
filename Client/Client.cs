using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace sNet.Client
{
	public static class Client
	{
		public const int DataBufferSize = 4096;

		private static string m_ip;
		private static int m_port;
		private static int m_id;

		private static TCP m_tcp;
		private static UDP m_udp;

		private static bool m_connected = false;
		private static bool m_initialized = false;

		public static int ID { get { return m_id; } }
		public static bool Connected { get { return m_connected; } }

		/// <summary>
		/// Used to handle connection and data over the TCP Protocol
		/// </summary>
		public class TCP
		{
			public TcpClient socket;

			private NetworkStream m_stream;
			private PacketBuffer m_receivePacketBuffer;
			private byte[] m_receiveBuffer;

			public EndPoint LocalEndPoint { get { return socket.Client.LocalEndPoint; } }

			//Connection Methods
			public void Connect()
			{
				socket = new TcpClient
				{
					ReceiveBufferSize = DataBufferSize,
					SendBufferSize = DataBufferSize
				};


				m_receiveBuffer = new byte[DataBufferSize];
				socket.BeginConnect(m_ip, m_port, ConnectCallback, socket);
			}
			private void ConnectCallback(IAsyncResult ar)
			{
				socket.EndConnect(ar);



				if (!socket.Connected)
				{
					Log.Err("Failed To Connect To [ '{0}:{1}' ]", m_ip, m_port);
					return;
				}


				m_stream = socket.GetStream();

				m_receivePacketBuffer = new PacketBuffer();

				BeginRead();
			}


			//Data Receiving Methods
			private void BeginRead()
			{
				m_stream.BeginRead(m_receiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
			}
			private void ReceiveCallback(IAsyncResult ar)
			{
				try
				{
					int byteLength = m_stream.EndRead(ar);
					if (byteLength <= 0)
					{
						Client.Disconnect();
						return;
					}

					byte[] data = new byte[byteLength];
					Array.Copy(m_receiveBuffer, data, byteLength);

					m_receivePacketBuffer.Reset(HandleData(data));
					BeginRead();
				}
				catch
				{
					Disconnect();
				}
			}

			private bool HandleData(byte[] data)
			{
				int packetLength = 0;

				m_receivePacketBuffer.SetBytes(data);

				if (m_receivePacketBuffer.UnreadLength() >= 4)
				{
					packetLength = m_receivePacketBuffer.ReadInt();
					if (packetLength <= 0)
					{
						return true;
					}
				}

				while (packetLength > 0 && packetLength <= m_receivePacketBuffer.UnreadLength())
				{
					byte[] packetBytes = m_receivePacketBuffer.ReadBytes(packetLength);

					//This is a unity necessary Function but it can be executed without ThreadManager if you are out side of Unity Engine
					ThreadManager.ExecOnMain(() =>
					{
						using (PacketBuffer packet = new PacketBuffer(packetBytes))
						{
							int packetID = packet.ReadInt();
							Debug.Log("Handling Data");
							ClientPacketHandler.Handle(packetID, packet);
						};
					});

					packetLength = 0;
					if (m_receivePacketBuffer.UnreadLength() >= 4)
					{
						packetLength = m_receivePacketBuffer.ReadInt();
						if (packetLength <= 0)
						{
							return true;
						}
					}
				}

				//If No Remaining Data Return True To Invoke The Reset Of The PacketBuffer
				if (packetLength <= 1) return true;


				//There is Still Data Remaining So Don't Reset PacketBuffer
				return false;
			}

			public void SendData(PacketBuffer buffer)
			{
				try
				{
					if (socket == null) { Log.Err("Cannot Send TCP Data, No Socket Exists."); return; }

					m_stream.BeginWrite(buffer.ToArray(), 0, buffer.Length(), null, null);
				}
				catch (Exception e)
				{
					Log.Err("Uknown Network Stream Exception: {0}", e);
				}
			}

			public void Disconnect()
            {
				Client.Disconnect();

				m_stream = null;
				m_receiveBuffer = null;
				m_receivePacketBuffer = null;
				socket = null;
            }

		}
		/// <summary>
		/// Used to handle connection and data over the UDP Protocol
		/// </summary>
		public class UDP
		{
			public UdpClient socket;
			private IPEndPoint m_endPoint;

			public UDP()
			{
				m_endPoint = new IPEndPoint(IPAddress.Parse(m_ip), m_port);
			}

			public void Connect(int localPort)
			{
				socket = new UdpClient(localPort);
				socket.Connect(m_endPoint);

				socket.BeginReceive(ReceiveCallback, null);

				using (PacketBuffer buffer = new PacketBuffer())
				{
					SendData(buffer);
				}
			}

			private void ReceiveCallback(IAsyncResult ar)
			{
				try
				{
					if (socket == null) return;

					byte[] data = socket.EndReceive(ar, ref m_endPoint);
					socket.BeginReceive(ReceiveCallback, null);

					if (data.Length < 4)
					{
						Client.Disconnect();
						return;
					}

					HandleData(data);
				}
				catch
				{
					Disconnect();					
				}
			}


			public void SendData(PacketBuffer buffer)
			{
				try
				{
					buffer.InsertInt(m_id);


					if (socket != null)
					{
						socket.BeginSend(buffer.ToArray(), buffer.Length(), null, null);
					}
				}
				catch (Exception e)
				{
					Log.Err(e);
				}
			}
			private void HandleData(byte[] data)
			{
				using (PacketBuffer buffer = new PacketBuffer(data))
				{
					int packetLength = buffer.ReadInt();
					data = buffer.ReadBytes(packetLength);
				}

				ThreadManager.ExecOnMain(() =>
				{
					using (PacketBuffer buffer = new PacketBuffer(data))
					{
						int packetID = buffer.ReadInt();
						ClientPacketHandler.Handle(packetID, buffer);
					}
				});
			}

			public void Disconnect()
            {
				Client.Disconnect();

				m_endPoint = null;
				socket = null;
            }

		}

		/// <summary>
		/// This is where the client is made aware of their ID so they can use it to communicate with the server
		/// </summary>
		/// <param name="connectionID">The ID the Server Provided</param>
		public static void Init(int connectionID)
		{
			if (m_initialized)
			{
				return;
			}

			m_id = connectionID;
			m_initialized = true;
			Log.Msg("Successfully Established TCP Connection w/ Server. Attempting To Establish UDP Connection...");

			m_udp.Connect(((IPEndPoint)m_tcp.LocalEndPoint).Port);
		}

		/// <summary>
		/// This is where the client determines whether it has successfully connected or not. This function is called in the ClientPacketHandler when a successfull connection status is received from the server.
		/// </summary>
		/// <param name="serverProvidedID"></param>
		/// <param name="connectionStatus"></param>
		public static void Verify(int serverProvidedID, ClientConnectionStatus connectionStatus)
		{
			if (m_connected)
				return;

			m_connected = (serverProvidedID == m_id && connectionStatus == ClientConnectionStatus.Connection_Successfull);
			if (m_connected)
            {
				ClientEvents.OnClientConnected?.Invoke();
				Log.Msg("Verification Successfull, Connection with Server Active.");
			}
			else
				Log.Err("Verification Failed...");
		}

		private static void NewSockets()
		{
			m_udp = new UDP();
			m_tcp = new TCP();
		}

		/// <summary>
		/// Connects to the local address 127.0.0.1 with the provided port
		/// </summary>
		/// <param name="port">Port to connect to</param>
		public static void ConnectLocal(int port)
		{
			m_ip = "127.0.0.1";
			m_port = port;
			Connect();
		}
		/// <summary>
		/// Connects to the provided IP Address & Port
		/// </summary>
		/// <param name="ip">Address to Connect to</param>
		/// <param name="port">Port to connect to</param>
		public static void Connect(string ip, int port)
		{
			m_ip = ip;
			m_port = port;
			Connect();
		}
		/// <summary>
		/// Connects to the m_ip & m_port variables in the Client Class
		/// </summary>
		private static void Connect()
		{
			Log.Msg("Attempting To Connect To [ '{0} : {1}' ]", m_ip, m_port);
			NewSockets();

			m_tcp.Connect();
		}

		/// <summary>
		/// Sends data over the TCP protocol to the server.
		/// </summary>
		/// <param name="buffer">The data to send</param>
		public static void SendDataTCP(PacketBuffer buffer)
		{
			m_tcp.SendData(buffer);
		}
		/// <summary>
		/// Sends data over the UDP protocol to the server.
		/// </summary>
		/// <param name="buffer">The data to send</param>
		public static void SendDataUDP(PacketBuffer buffer)
		{
			m_udp.SendData(buffer);
		}

		/// <summary>
		/// Disconnects from the server if you are connected to one.
		/// </summary>
		public static void Disconnect()
        {
			if (m_connected == false) return;

			m_id = -1;
			m_connected = false;
			m_initialized = false;
			

			m_tcp.socket.Close();
			m_udp.socket.Close();

			ClientEvents.OnClientDisconnected?.Invoke();
			Log.Msg("Disconnected From Server.");
        }
	}
}