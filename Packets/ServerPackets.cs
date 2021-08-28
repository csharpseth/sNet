namespace sNet
{
    /// <summary>
    /// These are packets that are sent by the Server and received by the Client. Server ---> Client
    /// </summary>
    public enum ServerPacketID
    {
        connection_status = 1
    }

    public struct PacketServer_ConnectionStatus : INetworkData
    {
        //Used to Distinguish Between Packets To Call The Appropriate Functions
        public int PacketID { get { return (int)ServerPacketID.connection_status; } }

        public ClientConnectionStatus status;
        public int connectionID;

        //Translates This Packets Variables To Bytes
        //Use buffer.Write('Some Packet Data'); to translate
        public PacketBuffer Serialize()
        {
            PacketBuffer buffer = new PacketBuffer(PacketID);

            //Write Other Variables HERE
            buffer.Write(connectionID);
            buffer.Write((byte)status); // I am type casting to a byte to lower the size of the packet, because in this case I know that the connection status will never be < 0 or > 255

            return buffer;
        }

        //Translates The Bytes Received To This Packets Variables
        //Variables Must Be Read In The Same Order Of Writing
        //Use buffer.Read'Some Type'(); to get the data
        public void Deserialize(PacketBuffer buffer)
        {
            connectionID = buffer.ReadInt();
            status = (ClientConnectionStatus)buffer.ReadByte();
            //Read Other Variables HERE
        }
    }
}