namespace sNet
{
    /// <summary>
    /// These are packets that are sent and received by the Server and Client. Client <---> Server
    /// </summary>
    public enum SharedPacketID
    {
        ping = 500
    }
    public struct PacketShared_Ping : INetworkData
    {
        //Used to Distinguish Between Packets To Call The Appropriate Functions
        public int PacketID { get { return (int)SharedPacketID.ping; } }

        //More Packet Data HERE
        public float TimeSent;

        //Translates This Packets Variables To Bytes
        //Use buffer.Write('Some Packet Data'); to translate
        public PacketBuffer Serialize()
        {
            PacketBuffer buffer = new PacketBuffer(PacketID);
            buffer.Write(TimeSent);

            return buffer;
        }

        //Translates The Bytes Received To This Packets Variables
        //Variables Must Be Read In The Same Order Of Writing
        //Use buffer.Read'Some Type'(); to get the data
        public void Deserialize(PacketBuffer buffer)
        {
            TimeSent = buffer.ReadFloat();
        }
    }
}