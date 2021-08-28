using System;

namespace sNet.Client
{
    public static class ClientEvents
    {
        /// <summary>
        /// Called whenever the Client is verified and successfully connected to the server.
        /// </summary>
        public static Action OnClientConnected;
        /// <summary>
        /// Called whenever the Client is disconnected or disconnects from the server.
        /// </summary>
        public static Action OnClientDisconnected;
    }
}