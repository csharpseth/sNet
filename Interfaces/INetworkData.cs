namespace sNet
{
    /// <summary>
    /// Every Packet struct must implement this interface inorder to be used in the network.
    /// </summary>
    public interface INetworkData
    {
        int PacketID { get; }

        PacketBuffer Serialize();

        void Deserialize(PacketBuffer buffer);
    }
}
