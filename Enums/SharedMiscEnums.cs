namespace sNet
{
    public enum ClientConnectionStatus
    {
        ConnectedTCP = 1,
        ConnectedUDP,
        Connection_Successfull,

        FailedToConnectTCP,
        FailedToConnectUDP,
        FailedToConnect,

        Disconnected,
        DisconnectedByServer
    }
}