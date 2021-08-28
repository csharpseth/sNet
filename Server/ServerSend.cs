namespace sNet.Server
{
    public static class ServerSend
    {
        #region TCP
        /// <summary>
        /// Sends Data (of INetworkPacket type) as bytes to ONE connected client via TCP protocol.
        /// </summary>
        /// <param name="toClient">Client To Send To</param>
        /// <param name="packet">The Data To Send</param>
        public static void SendTCP(int toClient, INetworkData packet)
        {
            using (PacketBuffer buffer = packet.Serialize())
            {
                buffer.WriteLength();
                Server.Clients.SendDataTCP(toClient, buffer.ToArray());
            }
        }
        /// <summary>
        /// Sends Data (of INetworkPacket type) as bytes to ALL connected clients via TCP protocol.
        /// </summary>
        /// <param name="packet">A Struct Storing Data To Be Easily Converted To/From byte[].</param>
        public static void SendTCPAll(INetworkData packet)
        {
            using (PacketBuffer buffer = packet.Serialize())
            {
                buffer.WriteLength();
                Server.Clients.SendDataTCPAll(buffer.ToArray());
            }

        }
        /// <summary>
        /// Sends Data (of INetworkPacket type) as bytes to ALL connected clients EXCEPT one specified client via TCP protocol.
        /// </summary>
        /// <param name="connectionID">The Client To Negate Sending The Packet To.</param>
        /// <param name="packet">A Struct Storing Data To Be Easily Converted To/From byte[].</param>
        public static void SendTCPAllExcept(int connectionID, INetworkData packet)
        {
            using (PacketBuffer buffer = packet.Serialize())
            {
                buffer.WriteLength();
                Server.Clients.SendDataTCPAllExcept(connectionID, buffer.ToArray());
            }
        }
        #endregion

        #region UDP
        /// <summary>
        /// Sends Data From Server To Client Via UDP Protocol
        /// </summary>
        /// <param name="toClient">Client To Send To</param>
        /// <param name="packet">Data To Send The Client</param>
        public static void SendUDP(int toClient, INetworkData packet)
        {
            using (PacketBuffer buffer = packet.Serialize())
            {
                buffer.WriteLength();
                Server.Clients.SendDataUDP(toClient, buffer);
            }
        }
        /// <summary>
        /// Sends Data From Server To ALL Clients Via UDP Protocol
        /// </summary>
        /// <param name="packet">Data To Send The Clients</param>
        public static void SendUDPAll(INetworkData packet)
        {
            using (PacketBuffer buffer = packet.Serialize())
            {
                buffer.WriteLength();
                Server.Clients.SendDataUDPAll(buffer);
            }
        }
        /// <summary>
        /// Sends Data From Server To ALL Clients Except One Specified Client Via UDP Protocol
        /// </summary>
        /// <param name="exceptClient">Client Excluded From Receiving Data</param>
        /// <param name="packet">Data To Send The Clients</param>
        public static void SendUDPAllExcept(int exceptClient, INetworkData packet)
        {
            using (PacketBuffer buffer = packet.Serialize())
            {
                buffer.WriteLength();
                Server.Clients.SendDataUDPAllExcept(exceptClient, buffer);
            }
        }
        #endregion

        /// <summary>
        /// Used to send a connection status update to a specific client. Uses TCP protocl unless the status is 'ConnectedUDP' then it uses UDP just to ensure its working properly.
        /// </summary>
        /// <param name="connectionID">Client to send status to</param>
        /// <param name="status">Client's connection status</param>
        public static void ConnectionStatus(int connectionID, ClientConnectionStatus status)
        {
            PacketServer_ConnectionStatus packet = new PacketServer_ConnectionStatus()
            {
                connectionID = connectionID,
                status = status
            };

            if(status == ClientConnectionStatus.ConnectedUDP)
            {
                SendUDP(connectionID, packet);
            }else
            {
                SendTCP(connectionID, packet);
            }
        }
    }
}