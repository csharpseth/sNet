using System;

namespace sNet.Client
{
	/// <summary>
	/// Static Class Used To Wrap Some Of The Low Level Functions Into an Easy To Use API
	/// </summary>
	public static class ClientSend
	{
		/// <summary>
		/// Sends Some Data, Using The TCP Protocol, To The Server.
		/// </summary>
		/// <param name="packet">The Data To Send The Server</param>
		public static void SendTCP(INetworkData packet)
		{
			using (PacketBuffer buffer = packet.Serialize())
			{
				buffer.WriteLength();
				Client.SendDataTCP(buffer);
			}
		}

		/// <summary>
		/// Sends Some Data, Using The UDP Protocol, To The Server.
		/// </summary>
		/// <param name="packet">The Data To Send The Server</param>
		public static void SendUDP(INetworkData packet)
		{
			using (PacketBuffer buffer = packet.Serialize())
			{
				buffer.WriteLength();
				Client.SendDataUDP(buffer);
			}
		}

		/// <summary>
		/// Sends A Packet Containing The Current Time Of Being Sent, The Server Then Returns This Packet.
		/// You Can Listen For The Packet To Be Returned An Use That Information To Calculate The Delay Between
		/// Client and Server.
		/// </summary>
		public static void Ping()
		{
			PacketShared_Ping packet = new PacketShared_Ping()
			{
				TimeSent = UnityEngine.Time.realtimeSinceStartup
			};

			SendUDP(packet);
		}

		/// <summary>
		/// Send The Packet That Holds The Information To Request That The Client Side Starting
		/// Information is Valid and Thus Can Be Verified By The Server.
		/// </summary>
		public static void RequestVerification()
		{
			PacketClient_VerificationRequest req = new PacketClient_VerificationRequest()
			{
				connectionID = Client.ID
			};

			SendTCP(req);
		}

	}
}