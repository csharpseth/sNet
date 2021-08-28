using System;
using System.Collections.Generic;

namespace sNet.Client
{
	/// <summary>
	/// A static slass used by the client as an API for handling received network data.
	/// </summary>
	public static class ClientPacketHandler
	{
		private static Dictionary<int, ClientPacketHandlerData> m_handlers;

		private static bool m_initialized = false;

		/// <summary>
		/// Initializes any instances that need to be created, and calls EstablishHandlers()
		/// </summary>
		public static void Init()
		{
			m_handlers = new Dictionary<int, ClientPacketHandlerData>();
			EstablishHandlers();
			m_initialized = true;
		}

		/// <summary>
		/// Creates default handlers( Events ) to handle incoming network packets.
		/// </summary>
		private static void EstablishHandlers()
		{
			SubscribeTo<PacketServer_ConnectionStatus>((INetworkData p) =>
			{
				PacketServer_ConnectionStatus packet = (PacketServer_ConnectionStatus)p;
				if (packet.status == ClientConnectionStatus.ConnectedTCP)
				{
					Log.Msg("Successfully Established TCP Connection with The Server. Attempting To Verify Connection...");
					Client.Init(packet.connectionID);
				}
				else if (packet.status == ClientConnectionStatus.ConnectedUDP)
				{
					Log.Msg("Successfully Established UDP Connection with The Server. Attempting To Verify Connection...");
					ClientSend.RequestVerification();
				}
				else if (packet.status == ClientConnectionStatus.Connection_Successfull)
				{
					Client.Verify(packet.connectionID, packet.status);
				}
			});
		}

		#region BackEnd
		/// <summary>
		/// Enusures the supplied integer is within the expected range. In this case 1 - 999( Packet IDs The Client Expects ).
		/// </summary>
		/// <param name="packetID">ID to Validate</param>
		/// <returns></returns>
		private static bool isValidPacketID(int packetID)
		{
			return (packetID >= 1 && packetID < 1000);
		}

		/// <summary>
		/// Uses the supplied parameters to determine what to do with a network packet that was received.
		/// </summary>
		/// <param name="packetID">The ID the received packet provides.</param>
		/// <param name="buffer">The remaining data to process.</param>
		public static void Handle(int packetID, PacketBuffer buffer)
		{
			//Ensures The ClientPacketHandler is prepared to process data
			if (m_initialized == false)
			{
				Log.Err("Failed To Handle Packet, ClientPacketHandler Has Not Been Initialized!");
				return;
			}

			//Validates ID
			if (isValidPacketID(packetID) == false)
			{
				Log.Err("Failed To Handle Packet: PacketID[ '{0}' ] was not a valid 'SERVER' or 'SHARED' packet", packetID);
				return;
			}

			//Checks if there are any handlers that can process a packet of this type
			if (m_handlers.ContainsKey(packetID) == false)
			{
				Log.Err("Failed To Handle Packet, No Handler For Packet: {0}!", packetID);
				return;
			}

			//Finally the delegate function correlated with this 'packetID' is executed and passed the data received.
			m_handlers[packetID].Invoke(buffer);
		}


		/// <summary>
		/// Wrapper function to make it easier for outside systems to subscribe to packets
		/// </summary>
		/// <typeparam name="T">The packet to subscribe to, must implement 'INetworkPacket'</typeparam>
		/// <param name="callback">The function to execute when the specified packet is received.</param>
		public static void SubscribeTo<T>(ClientPacketHandlerData.ReceiveCallback callback) where T : INetworkData
		{
			T instance = Activator.CreateInstance<T>();

			if (m_handlers.ContainsKey(instance.PacketID) == false)
			{
				CreateHandler(instance.PacketID, instance, callback);
			};

			m_handlers[instance.PacketID].Subscribe(callback);
		}

		/// <summary>
		/// Wrapper function to make it easier for outside systems to unsubscribe to packets
		/// </summary>
		/// <param name="packetID">The ID of packet to unsubscribe from.</param>
		/// <param name="callback">The function to be unsubscribed.</param>
		public static void UnSubscribeFrom(int packetID, ClientPacketHandlerData.ReceiveCallback callback)
		{
			if (m_handlers.ContainsKey(packetID) == false) return;

			m_handlers[packetID].UnSubscribe(callback);
		}

		private static void CreateHandler(int packetID, INetworkData packetInstance, ClientPacketHandlerData.ReceiveCallback callback = null)
		{
			ClientPacketHandlerData phData = new ClientPacketHandlerData(packetInstance, callback);
			m_handlers.Add(packetID, phData);
		}
		#endregion
	}
}