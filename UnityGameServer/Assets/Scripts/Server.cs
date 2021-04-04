using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server
{
    public static int MaxPlayers { get; private set; }
    public static int Port { get; private set; }
    public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
    public delegate void PacketHandler(int _fromClient, Packet _packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    private static TcpListener tcpListener;
    private static UdpClient udpListener;

    public static bool isActive = false;

    public static Convar tickrate = new Convar("tickrate", 32, "Ticks per second", Flags.NETWORK, 1, 128);

    static Command serverstop = new Command("serverstop", "Stops server", Flags.NETWORK, () => Server.Stop());
    static Command serverstart = new Command("serverstart", "Starts server with max players and port number", Flags.NETWORK, (string[] a) =>
    {
        if (a.Length <= 1)
            return;

        if (!Command.IsNumber(a[0]) || !Command.IsNumber(a[1]))
            return;

        Server.Start(int.Parse(a[0]), int.Parse(a[1]));
    });

    /// <summary>Starts the server.</summary>
    /// <param name="_maxPlayers">The maximum players that can be connected simultaneously.</param>
    /// <param name="_port">The port to start the server on.</param>
    public static void Start(int _maxPlayers, int _port)
    {
        Stop();

        MaxPlayers = _maxPlayers;
        Port = _port;

        Debug.Log("Starting server...");
        InitializeServerData();
        LagCompensation.Start(_maxPlayers);

        tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);

        udpListener = new UdpClient(Port);
        udpListener.BeginReceive(UDPReceiveCallback, null);

        isActive = true;

        Debug.Log($"Server started on port {Port}.");
    }

    /// <summary>Handles new TCP connections.</summary>
    private static void TCPConnectCallback(IAsyncResult _result)
    {
        TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
        tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
        Debug.Log($"Incoming connection from {_client.Client.RemoteEndPoint}...");

        for (int i = 1; i <= MaxPlayers; i++)
        {
            if (clients[i].tcp.socket == null)
            {
                clients[i].tcp.Connect(_client);
                return;
            }
        }
        Debug.Log($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
    }

    /// <summary>Receives incoming UDP data.</summary>
    private static void UDPReceiveCallback(IAsyncResult _result)
    {
        try
        {
            IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            if (_data.Length < 4)
            {
                return;
            }

            using (Packet _packet = new Packet(_data))
            {
                int _clientId = _packet.ReadInt();

                if (_clientId == 0)
                {
                    return;
                }

                if (clients[_clientId].udp.endPoint == null)
                {
                    // If this is a new connection
                    clients[_clientId].udp.Connect(_clientEndPoint);
                    return;
                }

                if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                {
                    // Ensures that the client is not being impersonated by another by sending a false clientID
                    clients[_clientId].udp.HandleData(_packet);
                }
            }
        }
        catch (Exception _ex)
        {
            Debug.Log($"Error receiving UDP data: {_ex}");
        }
    }

    /// <summary>Sends a packet to the specified endpoint via UDP.</summary>
    /// <param name="_clientEndPoint">The endpoint to send the packet to.</param>
    /// <param name="_packet">The packet to send.</param>
    public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
    {
        try
        {
            if (_clientEndPoint != null)
            {
                udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
            }
        }
        catch (Exception _ex)
        {
            Debug.Log($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
        }
    }

    /// <summary>Initializes all necessary server data.</summary>
    private static void InitializeServerData()
    {
        clients = new Dictionary<int, Client>();

        for (int i = 1; i <= MaxPlayers; i++)
        {
            clients.Add(i, new Client(i));
        }

        packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                { (int)ClientPackets.playerInput, ServerHandle.PlayerInput },
                { (int)ClientPackets.playerConvar, ServerHandle.PlayerConvar },
            };
        Debug.Log("Initialized packets.");
    }
    public static void Stop()
    {
        if(isActive)
        {
            tcpListener.Stop();
            udpListener.Close();
            LagCompensation.Stop();
            isActive = false;
        }
    }
}
