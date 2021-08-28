using System;
using System.Collections.Generic;

namespace sNet.Server
{
	public static class ServerPacketHandler
	{
		private static Dictionary<int, ServerPacketHandlerData> m_handlers;

		private static bool m_initialized = false;

		public static void Init()
		{
			m_handlers = new Dictionary<int, ServerPacketHandlerData>();
			EstablishHandlers();
			m_initialized = true;
		}
		private static bool isValidPacketID(int packetID)
		{
			return (packetID >= 500);
		}

		/// <summary>
		/// Creates default handlers( Events ) to handle incoming network packets.
		/// </summary>
		private static void EstablishHandlers()
		{
			SubscribeTo<PacketShared_Ping>((int connectionID, INetworkData data) =>
			{
				ServerSend.SendUDP(connectionID, data);
			});

			SubscribeTo<PacketClient_VerificationRequest>((int connectionID, INetworkData data) =>
			{
				PacketClient_VerificationRequest packet = (PacketClient_VerificationRequest)data;

				if(packet.connectionID != connectionID)
				{
					ServerSend.ConnectionStatus(connectionID, ClientConnectionStatus.FailedToConnect);
					Log.Err("Failed To Verified Client[ '{0}' ]!", connectionID);
					return;
				}

				if(Server.Clients.IsClientVerified(connectionID) == false)
				{
					Log.Msg("Successfully Verified Client[ '{0}' ]!", connectionID);
					ServerSend.ConnectionStatus(connectionID, ClientConnectionStatus.Connection_Successfull);
					Server.Clients.VerifyClient(connectionID);
				}
			});

		}

		#region BackEnd
		/// <summary>
		/// Uses the supplied parameters to determine what to do with a network packet that was received.
		/// </summary>
		/// <param name="connectionID">The ID of the Client who sent the data.</param>
		/// <param name="packetID">The ID the received packet provides.</param>
		/// <param name="buffer">The remaining data to process.</param>
		public static void Handle(int connectionID, int packetID, PacketBuffer buffer)
		{
			if (isValidPacketID(packetID) == false)
			{
				Log.Err("Failed To Handle Packet: PacketID[ '{0}' ] was not a valid 'CLIENT' or 'SHARED' packet", packetID);
				return;
			}

			if (m_initialized == false)
			{
				Log.Err("Failed To Handle Packet: ServerPacketHandler Has Not Been Initialized!");
				return;
			}

			if (m_handlers.ContainsKey(packetID) == false)
			{
				Log.Err("Failed To Handle Packet: No Handler For Packet: {0}!", packetID);
				return;
			}

			m_handlers[packetID].Invoke(connectionID, buffer);
		}

		/// <summary>
		/// Wrapper function to make it easier for outside systems to subscribe to packets
		/// </summary>
		/// <typeparam name="T">The packet to subscribe to, must implement 'INetworkPacket'</typeparam>
		/// <param name="callback">The function to execute when the specified packet is received.</param>
		public static void SubscribeTo<T>(ServerPacketHandlerData.ReceiveCallback callback) where T : INetworkData
		{
			T instance = Activator.CreateInstance<T>();
			
			if (m_handlers.ContainsKey(instance.PacketID) == false) {
				CreateHandler(instance.PacketID, instance, callback);
			};

			m_handlers[instance.PacketID].Subscribe(callback);
		}

		/// <summary>
		/// Wrapper function to make it easier for outside systems to unsubscribe to packets
		/// </summary>
		/// <param name="packetID">The ID of packet to unsubscribe from.</param>
		/// <param name="callback">The function to be unsubscribed.</param>
		public static void UnSubscribeFrom(int packetID, ServerPacketHandlerData.ReceiveCallback callback)
		{
			if (m_handlers.ContainsKey(packetID) == false) return;

			m_handlers[packetID].UnSubscribe(callback);
		}

		private static void CreateHandler(int packetID, INetworkData type, ServerPacketHandlerData.ReceiveCallback callback = null)
		{
			ServerPacketHandlerData phData = new ServerPacketHandlerData(type, callback);
			m_handlers.Add(packetID, phData);
		}
		#endregion
	}
}