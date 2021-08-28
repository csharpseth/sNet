namespace sNet
{
	/// <summary>
	/// These are packets that are sent by the Client and received by the Server. Client ---> Server
	/// </summary>
	public enum ClientPacket
	{
		request_verification = 1000
	}
	public struct PacketClient_VerificationRequest : INetworkData
	{
		public int PacketID { get { return (int)ClientPacket.request_verification; } }
		public int connectionID;

		//Packet Data Below


		public PacketBuffer Serialize()
		{
			PacketBuffer buffer = new PacketBuffer(PacketID);
			buffer.Write(connectionID);


			return buffer;
		}

		public void Deserialize(PacketBuffer buffer)
		{
			connectionID = buffer.ReadInt();
		}
	}

}