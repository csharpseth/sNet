using sNet.Server;
using UnityEngine;
using UnityEngine.UI;

public class TestServer : MonoBehaviour
{
    public int port = 4226;
    public int maxConnections = 20;

    [Header("UI:")]
    public Text connectedText;
    public Button stopButton;

    public void StartServer()
    {
        //This must be called before you start the Server
        ServerPacketHandler.Init();

        // I suggest subscribing to events before starting the server otherwise none of the initial events,
        // like OnServerStart will be executed. This isn't needed just an easy way for you to add functionality
        Subscribe();

        //Max Connections is necessary because of the way clients are accessed and stored
        Server.Start(port, maxConnections);
    }

    private void Subscribe()
    {
        ServerEvents.OnServerStart += () =>
        {
            stopButton.interactable = true;
        };

        ServerEvents.OnServerStop += () =>
        {
            stopButton.interactable = false;
        };
    }

    public void StopServer()
    {
        Server.Stop();
    }

    // This is not necessary however you may run into issues like not being allowed to start a new server
    // because the previous one was never 'disposed', you can get around this during testing by just changing
    // the port, but in release I highly recommend something like this
    private void OnApplicationQuit()
    {
        StopServer();
    }

    // UI updates are only done this way because I am using Unity's built in text and it's ass and won't update
    // unless I do it this way
    private void LateUpdate()
    {
        connectedText.text = Server.ClientCount.ToString();
    }
}
