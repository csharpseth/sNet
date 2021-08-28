using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace sNet.Server
{
    /// <summary>
    /// A way to encapsulate client state
    /// </summary>
    public struct ServerClientData
    {
        private int m_id;
        private bool m_empty;
        private bool m_verified;

        public bool isEmpty { get { return m_empty; } set { m_empty = value; } }
        public bool isVerified { get { return m_verified; } set { m_verified = value; } }

        public ServerClientData(int id)
        {
            m_id = id;
            m_empty = true;
            m_verified = false;
        }

        public void Disconnect()
        {
            m_empty = true;
            m_verified = false;
        }
    }

    /// <summary>
    /// Handles all connected Clients' data/state
    /// </summary>
    public class ClientManager
    {
        // Separated into individual systems to make iterating them faster( maybe? ). Also just a more organized approach IMO
        private ServerClientData[] m_clientDatas;
        private TCP[] m_clientTcpConnections;
        private UDP[] m_clientUdpConnections;

        // Since data is stored in arrays and some slots may be empty clients in those arrays this is how I chose to keep track of the connected client indexes, and prevent iteration on empty clients.
        private List<int> m_connectedClients;
        private int m_maxClients;
        
        public int NumConnected { get { return m_connectedClients.Count; } }


        //Initializer
        public ClientManager(int maxNumClients)
        {
            m_maxClients = maxNumClients;

            //Initialize Arrays
            m_clientDatas = new ServerClientData[m_maxClients];
            m_clientTcpConnections = new TCP[m_maxClients];
            m_clientUdpConnections = new UDP[m_maxClients];

            //Initialize List :(
            m_connectedClients = new List<int>();

            //Fill Arrays
            for (int i = 0; i < m_maxClients; i++)
            {
                m_clientDatas[i] = new ServerClientData(i);
                m_clientTcpConnections[i] = new TCP(i);
                m_clientUdpConnections[i] = new UDP(i);
            }
        }


        // Im not going to annotate these because this is pretty low level networking for C# and I also only understand like 85% of it.
        public class TCP
        {
            private static int m_dataBufferSize = 4096;

            private TcpClient m_socket;
            private int m_id;

            private NetworkStream m_stream;
            private PacketBuffer m_receivePacketBuffer;
            private byte[] m_receiveBuffer;

            public TcpClient Socket
            {
                get { return m_socket; }
            }

            public TCP(int id)
            {
                m_id = id;
            }

            public void Connect(TcpClient socket)
            {
                if(m_socket != null)
                {
                    Log.Err("Attempting to Create New Client When Existing Socket is In Use!");
                    return;
                }

                //Initialization
                m_socket = socket;
                m_socket.ReceiveBufferSize = m_dataBufferSize;
                m_socket.SendBufferSize = m_dataBufferSize;

                m_stream = socket.GetStream();

                m_receiveBuffer = new byte[m_dataBufferSize];
                m_receivePacketBuffer = new PacketBuffer();

                BeginRead();

                ServerSend.ConnectionStatus(m_id, ClientConnectionStatus.ConnectedTCP);
            }

            private void BeginRead()
            {
                m_stream.BeginRead(m_receiveBuffer, 0, m_dataBufferSize, ReceiveCallback, null);
            }

            private void ReceiveCallback(IAsyncResult ar)
            {
                try
                {
                    if (m_stream == null) return;

                    int byteLength = m_stream.EndRead(ar);
                    if(byteLength <= 0)
                    {
                        Server.Clients.TryDisconnectClient(m_id);
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(m_receiveBuffer, data, byteLength);

                    m_receivePacketBuffer.Reset(HandleData(data));

                    BeginRead();
                }
                catch
                {
                    Server.Clients.TryDisconnectClient(m_id);
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
                    ThreadManager.ExecOnMain(() =>
                    {
                        using (PacketBuffer packet = new PacketBuffer(packetBytes))
                        {
                            int packetID = packet.ReadInt();
                            ServerPacketHandler.Handle(m_id, packetID, packet);
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

            public void SendData(byte[] data)
            {
                try
                {
                    if (m_socket == null) { Log.Err("Cannot Send TCP Data For Client: {0}, No Socket Exists.", m_id); return; }

                    m_stream.BeginWrite(data, 0, data.Length, null, null);
                }
                catch (Exception e)
                {
                    Log.Err("Uknown Exception: {0}", e);
                }
            }

            public void Disconnect()
            {
                m_socket.Close();
                m_stream = null;
                m_receiveBuffer = null;
                m_receivePacketBuffer = null;
                m_socket = null;
            }
        }

        public class UDP
        {
            private IPEndPoint m_endPoint;
            private int m_id;

            public bool Open { get { return m_endPoint == null; } }
            public string EndPointString { get { return m_endPoint.ToString(); } }

            public UDP(int id)
            {
                m_id = id;
            }

            public void Connect(IPEndPoint endPoint)
            {
                m_endPoint = endPoint;
            }

            public void SendData(PacketBuffer buffer)
            {
                try
                {
                    Server.SendUDP(m_endPoint, buffer);
                }
                catch (Exception e)
                {
                    Log.Err(e);
                }
            }
            public void HandleData(PacketBuffer buffer)
            {
                int packetLength = buffer.ReadInt();
                byte[] packetBytes = buffer.ReadBytes(packetLength);

                ThreadManager.ExecOnMain(() =>
                {
                    using (PacketBuffer bufferData = new PacketBuffer(packetBytes))
                    {
                        int packetID = bufferData.ReadInt();
                        ServerPacketHandler.Handle(m_id, packetID, bufferData);
                    }
                });
            }

            public void Disconnect()
            {
                m_endPoint = null;
            }
        }

        //All farely self explanatory sending functions. Send either TCP or UDP. Send to one client, all clients, or all but one client.
        public void SendDataTCP(int id, byte[] data)
        {
            if(ValidID(id) == false)
            {
                Log.Err("Failed To Send TCP Data. Client ID: '{0}' is Invalid.");
                return;
            }

            m_clientTcpConnections[id].SendData(data);
        }
        public void SendDataTCPAll(byte[] data)
        {
            for (int i = 0; i < m_maxClients; i++)
            {
                m_clientTcpConnections[i].SendData(data);
            }
        }
        public void SendDataTCPAllExcept(int exceptID, byte[] data)
        {
            for (int i = 0; i < m_maxClients; i++)
            {
                if (i == exceptID) continue;

                m_clientTcpConnections[i].SendData(data);
            }
        }
        public void SendDataUDP(int toClient, PacketBuffer buffer)
        {
            if (ValidID(toClient) == false) return;

            m_clientUdpConnections[toClient].SendData(buffer);
        }
        public void SendDataUDPAll(PacketBuffer buffer)
        {
            for (int i = 0; i < m_clientUdpConnections.Length; i++)
            {
                if (m_clientUdpConnections[i].Open == true) continue;

                SendDataUDP(i, buffer);
            }
        }
        public void SendDataUDPAllExcept(int exceptClient, PacketBuffer buffer)
        {
            for (int i = 0; i < m_clientUdpConnections.Length; i++)
            {
                if (i == exceptClient || m_clientUdpConnections[i].Open == true) continue;

                SendDataUDP(i, buffer);
            }
        }

        /// <summary>
        /// Searches for an open Client slot/id and if one exists it will return true, otherwise returns false.
        /// </summary>
        /// <param name="client">Client attempting to connect</param>
        /// <returns></returns>
        public bool TryConnectNewClient(TcpClient client)
        {
            int id = FindEmptyClient();
            if(id == -1)
            {
                return false;
            }

            SetClient(id, client);
            ServerSend.ConnectionStatus(id, ClientConnectionStatus.ConnectedTCP);

            return true;
        }
        /// <summary>
        /// Ensures the provided ID exists and if it does the server will disconnect the specifed client.
        /// </summary>
        /// <param name="connectionID">Client to disconnect</param>
        public void TryDisconnectClient(int connectionID)
        {
            if (ValidID(connectionID) == false) return;

            DisconnectClient(connectionID);
        }

        // DisconnectAll() should only be called when the server is stopping or in some edge cases like an Admin KickAll command or something
        /// <summary>
        /// Disconnects all connected clients.
        /// </summary>
        public void DisconnectAll()
        {
            for (int i = 0; i < m_connectedClients.Count; i++)
            {
                DisconnectClient(m_connectedClients[i]);
            }
        }
        
        private void DisconnectClient(int connectionID)
        {
            //Disconnect And Reset This Client Slot To Be Reused
            m_connectedClients.Remove(connectionID);
            m_clientDatas[connectionID].Disconnect();
            m_clientTcpConnections[connectionID].Disconnect();
            m_clientUdpConnections[connectionID].Disconnect();

            Log.Msg("SERVER - Player[ '{0}' ] has disconnected.", connectionID);
            ServerEvents.OnClientDisconnect?.Invoke(connectionID);
        }

        //This stores a newly connected and confirmed client with the provided ID to be referenced later when necessary
        private void SetClient(int id, TcpClient client)
        {
            m_connectedClients.Add(id);
            m_clientDatas[id].isEmpty = false;
            m_clientTcpConnections[id].Connect(client);
        }
        //Finds and empty client slot if one exists it will return the index of that slot to be used as the client's ID, if one doesn't exist returns -1
        private int FindEmptyClient()
        {
            for (int i = 0; i < m_maxClients; i++)
            {
                if(m_clientDatas[i].isEmpty)
                {
                    return i;
                }
            }

            return -1;
        }
        //Ensures that a connectionID is valid i.e. connected and not out of range
        public bool ValidID(int id)
        {
            if (id < 0) return false;

            return m_connectedClients.Contains(id);
        }

        /// <summary>
        /// Returns true if the UDP client's endpoint has not been established.
        /// </summary>
        /// <param name="connectionID">Client to check</param>
        /// <returns></returns>
        public bool UdpClientIsOpen(int connectionID)
        {
            return m_clientUdpConnections[connectionID].Open;
        }
        /// <summary>
        /// Sets up all data needed to communicate with the specified client using the UDP protocol
        /// </summary>
        /// <param name="connectionID">Client to establish</param>
        /// <param name="endPoint">The Client's endpoint</param>
        public void UdpEstablishClientConnection(int connectionID, IPEndPoint endPoint)
        {
            m_clientUdpConnections[connectionID].Connect(endPoint);
        }
        /// <summary>
        /// Ensures the connectionID provided in the received UDP packet is the same ID of the endpoint that the packet was received from.
        /// </summary>
        /// <param name="connectionID">Client in question</param>
        /// <param name="packetEndPoint">EndPoint the packet was received from</param>
        /// <returns></returns>
        public bool UdpPacketIsValid(int connectionID, IPEndPoint packetEndPoint)
        {
            return m_clientUdpConnections[connectionID].EndPointString == packetEndPoint.ToString();
        }
        /// <summary>
        /// Handles data received using the UDP protocol.
        /// </summary>
        /// <param name="connectionID">Client who sent the data</param>
        /// <param name="buffer">The data</param>
        public void UdpHandleData(int connectionID, PacketBuffer buffer)
        {
            m_clientUdpConnections[connectionID].HandleData(buffer);
        }
        /// <summary>
        /// Sets a clients state to verified so the server can start sending/receiving its usually data to/from the client.
        /// </summary>
        /// <param name="connectionID">Client to verify</param>
        public void VerifyClient(int connectionID)
        {
            m_clientDatas[connectionID].isVerified = true;
        }
        /// <summary>
        /// Checks if a client is verified.
        /// </summary>
        /// <param name="connectionID">Client to check</param>
        /// <returns></returns>
        public bool IsClientVerified(int connectionID)
        {
            return m_clientDatas[connectionID].isVerified;
        }
    }
}
