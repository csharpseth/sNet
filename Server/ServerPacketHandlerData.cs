namespace sNet.Server
{
    public class ServerPacketHandlerData
    {
        public delegate void ReceiveCallback(int connectionID, INetworkData packet);
        private ReceiveCallback callback;
        private INetworkData packet;

        public ServerPacketHandlerData(INetworkData packet, ReceiveCallback callback = null)
        {
            this.packet = packet;
            this.callback = callback;
        }

        public void Subscribe(ReceiveCallback callback)
        {
            this.callback += callback;
        }

        public void UnSubscribe(ReceiveCallback callback)
        {
            this.callback -= callback;
        }

        public void Invoke(int connectionID, PacketBuffer buffer)
        {
            packet.Deserialize(buffer);

            callback?.Invoke(connectionID, packet);
            buffer.Dispose();
        }
    }
}