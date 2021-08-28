using sNet;
using sNet.Client;
using UnityEngine;
using UnityEngine.UI;

public class TestClient : MonoBehaviour
{
    public int port = 4226;
    public float checkPingInterval = 0.3f;

    [Header("UI:")]
    public Text pingText;
    public Text statusText;
    public Button disconnectButton;


    private int pingMs = 0;
    private string status = "~";

    private void Awake()
    {
        Log.Err("Test Error");
    }
    public void ConnectLocal()
    {
        status = "Connecting...";

        //This must be called before you try to connect otherwise packet data will not be handled properly if at all.
        ClientPacketHandler.Init();

        // I suggest subscribing to events before connecting to the server otherwise none of the initial events,
        // like OnClientConnect will be executed. This isn't needed just an easy way to add functionality
        Subscribe();

        Client.ConnectLocal(port);
    }

    private void Subscribe()
    {
        // The ClientPacketHandler is used to create functions that are called the the specified packet is
        // Received. In this case we are listening for 'PacketShared_Ping', defined in sNet/Packets/SharedPackets.cs
        ClientPacketHandler.SubscribeTo<PacketShared_Ping>((INetworkData data) =>
        {
            PacketShared_Ping packet = (PacketShared_Ping)data;
            float timeDifference = (Time.realtimeSinceStartup - packet.TimeSent);
            pingMs = (int)(timeDifference * 1000f);
        });

        // ClientEvents is a separate script organizing a bunch of actions that are called under certain circumstances by the Client
        // This system isn't necessary and could potentially be improved however this is just an easy way for me to( in this case ) update the UI
        ClientEvents.OnClientConnected += () =>
        {
            disconnectButton.interactable = true;
            status = "Connected";
        };

        ClientEvents.OnClientDisconnected += () =>
        {
            disconnectButton.interactable = false;
            status = "Disconnected";
        };
    }

    //This is how you send a ping. It would be way safer to have the server sending the Ping Request, but I am just realizing this now.
    float timer = 0f;
    private void Update()
    {
        if (Client.Connected == false) return;

        timer += Time.deltaTime;
        if(timer >= checkPingInterval)
        {
            ClientSend.Ping();
            timer = 0f;
        }
    }

    public void Disconnect()
    {
        Client.Disconnect();
    }

    // Again this is not necessary and as far as I know not doing this doesn't cause any issues however its just cleaner to do it this way
    private void OnApplicationQuit()
    {
        Disconnect();
    }

    // UI updates are only done this way because I am using Unity's built in text and it's ass and won't update
    // unless I do it this way
    private void LateUpdate()
    {
        pingText.text = pingMs + "ms";
        statusText.text = status;
    }

}
