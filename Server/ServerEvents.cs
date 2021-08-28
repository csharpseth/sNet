using System;

namespace sNet.Server
{
    public static class ServerEvents
    {
        public delegate void m_clientDisconnect(int connectionID);

        public static m_clientDisconnect OnClientDisconnect;
        public static Action OnServerStart;
        public static Action OnServerStop;
    }
}