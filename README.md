[![image alt text](https://i.imgur.com/TmK23t0.png)](https://imgur.com/TmK23t0)
# sNet

Short for 'Seth Networking' because I am a very creative person like that.

This is simply a solution I made to use with my Unity projects that I decided was 
a good foundation to be built upon and removes a lot of the boiler plate code that comes
with networking. I will be building and adding to this as I need, feel free to do the same.


Created for use with Unity, but the code works in any C#/.Net environment.


## Based On These Wonderful Solutions

 - [Tom Weiland's Solution](https://github.com/tom-weiland/tcp-udp-networking)
 - [Kevin Kaymak's Solution](https://www.youtube.com/channel/UCThwyD-sY4PwFm7EM89shhQ)

  
## Unity Prerequisites
Built using Unity 2020 LTS, but I see no reason it wouldn't work on any other version.

If you are using this solution in Unity you must have a GameObject in your scene that
has the included ThreadManager.cs attached to it. This is required in both the Server
and Client scenes. If the ThreadManager is not present you wont receive any errors stuff
just won't work for seamingly no reason, and it is very frustrating. This is only necessary 
in Unity to the best of my knowledge.

## Logging
I added a static Log class the server & client use that essentially just wraps the Debug.Log methods. I did it this way to make it easy
for use in projects outside of Unity. This way you can simply go into the Log.cs and 
replace one or two instances of Debug.Log with your own solution.

This does however 
present an issue of debugging in Unity since whenever you double click on an error, it 
will simply point you to the Log file. The solution to this is to click once on the error,
then scroll past the Log.cs reference to the next reference and click on that.

[![image alt text](https://i.imgur.com/aKngCmJ.png)](https://imgur.com/aKngCmJ)


## Getting Started

#### To Start The Server:

```csharp
ServerPacketHandler.Init();
Server.Start(port, max players);
```

#### To Start The Client:

```csharp
ClientPacketHandler.Init();
Client.Connect(ip, port);
```

#### Alternatively You Can Connect Locally ( sets the IP to 127.0.0.1 ):

```csharp
ClientPacketHandler.Init();
Client.ConnectLocal(port);
```

## Packets
Packet Data is formated like this:
```csharp
public struct PacketName : INetworkData
{
    //Only thing you need to change is 'This Packets ID' to however you store your IDs
    public int PacketID { get { return (int)'This Packets ID'; } }

    public PacketBuffer Serialize()
    {
        PacketBuffer buffer = new PacketBuffer(PacketID);
        buffer.Write( 'Data To Write' ); //writes some data to the packet


        return buffer;
    }

    public void Deserialize(PacketBuffer buffer)
    {
        //MUST be read in the same order of writing
        'Data To Read' = buffer.ReadInt(); //Reads some data from the packet supports most C# Types
    }
}
```

The PacketID can use whatever system you want, I use Enums. I've also made sure to offset 
each packet type scope by some number to ensure no crossover between scopes.

#### Examples:
```csharp
ServerPacketTypes {
    first = 1 // <--- This is what Im talking about.
}

SharedPacketTypes {
    first = 500 // <--- This is what Im talking about.
}

ClientPacketTypes {
    first = 1000 // <--- This is what Im talking about.
}
```

If you wish to use a different system just know that I have placed a few hard coded checks
in the code to ensure that the packet being received is within the expected ranges. Those ranges
are 1-499 = Server, 500-999 = Shared, and 1000 & Up = Client

3 files are included to define and organize packets with their relevant types:

- ServerPackets.cs
- SharedPackets.cs
- ClientPackets.cs

## Sending Data

#### On The Server:

Using TCP Protocol
```csharp
ServerSend.SendTCP( recipient ID, your packet );
```

or

Using UDP Protocol
```csharp
ServerSend.SendUDP( recipient ID, your packet );
```

#### On The Client:

Using TCP Protocol
```csharp
ClientSend.SendTCP( your packet );
```

or

Using UDP Protocol
```csharp
ClientSend.SendUDP( your packet );
```

Alternatively you can just define a function within whichever relevant PacketHandler
for repetetive sending. In my case I used this for the Ping Packet so I can just go
```csharp
ClientSend.Ping();
```

## Receiving Data

#### On The Server:

```csharp
ServerPacketHandler.SubscribeTo< YourPacket >((int connectionID, INetworkData data) => 
{
    YourPacket packet = (YourPacket)data;
    //now you can access all the data that your packet contains
});
```

#### On The Client:

```csharp
ClientPacketHandler.SubscribeTo< YourPacket >((INetworkData data) => 
{
    YourPacket packet = (YourPacket)data;
    //now you can access all the data that your packet contains
});
```

These events are called whenever a packet with the PacketID specified 
in your packet is received. If you send a packet and there is no handler 
for it an error will be thrown.

Alternatively you could obviously just plug in a pre-existing function that meets 
the parameter requirements instead of using lambdas.
## Reasoning / Conclusion

So you may not care at all, but I feel it necessary to describe why I 
approached it this way. Packets are structs because they are just data, and that
 is what structs are for. I am using delegates for the packet handlers, because 
 in my experience they have no performance impact and have even been faster 
 on average than a plain method. They also make it extremely easy to build upon. 
 
For example, if I wanted to sync a players position with the server all 
I have to do is create a monobehaviour that subscribes to the 'PacketServer_PlayerPosition'
and updates the position whenever it receive that packet and the provided playerID matches the packets playerID.

Overall I find this approach way more readable and it seems like it will work better at 
scale. However, I haven't been in a circumstance to test this positive so I could be wrong. In general 
take all of this with a grain of salt and use it at your own risk, but have fun :D

  