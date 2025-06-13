using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Main server logic class, responsible for all server-managing actions.
/// </summary>
public class ServerLogic
{
    /// <summary>
    /// A dictionary holding all connected clients, indexed by their client ID.
    /// </summary>
    public static Dictionary<int, ServerSideClient> clientsList = new Dictionary<int, ServerSideClient>();
    /// <summary>
    /// Maximum number of concurrent users allowed on the server.
    /// </summary>
    public static int maxCCU;
    /// <summary>
    /// Port number the server listens on for incoming connections.
    /// </summary>
    public static int listeningPort;
    /// <summary>
    /// TCP listener for handling incoming TCP connections.
    /// </summary>
    private static TcpListener tcpListener;
    /// <summary>
    /// UDP listener for handling incoming UDP packets.
    /// </summary>
    private static UdpClient udpListener;
    /// <summary>
    /// TLS certificate used for secure communication, if enabled.
    /// </summary>
    public static X509Certificate2 serverCertificate;
    /// <summary>
    /// Default network configuration settings for the server.
    /// </summary>
    static DefaultNetworkSettings DefaultSettings;
    /// <summary>
    /// Advanced server-side settings used during initialization.
    /// </summary>
    static ServerSettings ServerSettings;

    /// <summary>
    /// Initializes and starts the server, including TCP/UDP listeners and packet handlers.
    /// </summary>
    public static void StartServer(int CCU, int port, DefaultNetworkSettings defaultSettings, ServerSettings serverSettings) 
    {
        DefaultSettings = defaultSettings;
        ServerSettings = serverSettings;

        if (DefaultSettings.useTLS == true)
        {
            try
            {
                serverCertificate = new X509Certificate2(DefaultSettings.certificatePath, DefaultSettings.certificatePassword);
                Debug.Log($"Certificate loaded successfully");
                Console.WriteLine($"Certificate loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Problem with certificate: {ex.Message}");
                Console.WriteLine($"Problem with certificate: {ex.Message}");
            }
        }

        ConsoleLog("Starting Server");
        listeningPort = port;
        /// <summary>
        /// Ability to use tunelling alternative to webserver (for example playit.gg service)
        /// </summary>
        if (defaultSettings.useTunneling == true)
        {
            listeningPort = defaultSettings.TunnelPort;
        }
        maxCCU = CCU;
        InitializeClientsList();
        tcpListener = new TcpListener(IPAddress.Any, listeningPort); 
        tcpListener.Start(); 
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallback), null); 

        udpListener = new UdpClient(listeningPort);
        udpListener.BeginReceive(UdpReceiveCallback, null);

        ConsoleLog($"Server started on Port {listeningPort}");
    }

    /// <summary>
    /// Stops the server and closes all TCP and UDP connections.
    /// </summary>
    public static void StopServer()
    {
        tcpListener.Stop();
        udpListener.Close();
        ConsoleLog("TCP and UDP sockets closed, server is down");
    }

    /// <summary>
    /// Initializes the client list.
    /// </summary>
    private static void InitializeClientsList() 
    {
        for (int i = 1; i <= maxCCU; i++)
        {
            clientsList.Add(i, new ServerSideClient(i));
        }
    }
    /// <summary>
    /// Method responsible for managing received Packet.ClientPackets actions that are written in ServerReceiveHandler class. Can be easily extended for new packet types.
    /// </summary>
    public static void PacketAction(int packetId, Packet packet, int fromClient)
    {
        if (!Enum.IsDefined(typeof(Packet.ClientPackets), packetId))
        {
            Debug.LogWarning($"Unknown packet ID: {packetId}");
            return;
        }

        Packet.ClientPackets packetType = (Packet.ClientPackets)packetId;

        switch (packetType)
        {
            case Packet.ClientPackets.welcomeReceived:
                ServerReceiveHandler.WelcomeReceived(fromClient, packet);
                break;

            case Packet.ClientPackets.MeassureRequest:
                ServerReceiveHandler.LatencyRequestReceived(fromClient, packet);
                break;

            case Packet.ClientPackets.DisconnectRequest:
                ServerReceiveHandler.DisconnectRequestReceived(fromClient, packet);
                break;

            case Packet.ClientPackets.MessageToUserRequest:
                ServerReceiveHandler.MessageToUserRequestReceived(fromClient, packet);
                break;

            case Packet.ClientPackets.MessageToAllRequest:
                ServerReceiveHandler.MessageToAllRequestReceived(fromClient, packet);
                break;

            case Packet.ClientPackets.TransformChange:
                ServerReceiveHandler.TransformChangeReceived(fromClient, packet);
                break;

            case Packet.ClientPackets.AnimationChange:
                ServerReceiveHandler.AnimationChangeReceived(fromClient, packet);
                break;

            case Packet.ClientPackets.BandwidthRequest:
                ServerReceiveHandler.BandwidthRequestReceived(fromClient, packet);
                break;

            case Packet.ClientPackets.ThroughputRequest:
                ServerReceiveHandler.ThroughputRequestReceived(fromClient, packet);
                break;

            default:
                Debug.LogWarning($"Unhandled client packet type: {packetType}");
                break;
        }
    }
    /// <summary>
    /// Callback for handling new TCP client connections asynchronously.
    /// Assigns the client to the first available slot.
    /// </summary>
    private static void TcpConnectCallback(IAsyncResult result) 
    {
        TcpClient Client = tcpListener.EndAcceptTcpClient(result);
       tcpListener.BeginAcceptTcpClient(new AsyncCallback(TcpConnectCallback), null); 
        ConsoleLog($"Client on {Client.Client.RemoteEndPoint} is connecting");
        for (int i = 1; i <= maxCCU; i++)
        {
            if (clientsList[i].tcp.Socket == null)
            {
                clientsList[i].tcp.Connect(Client);
                ConsoleLog($"Client {clientsList[i].tcp.Socket.Client.RemoteEndPoint} has connected");            
                return;
            }
        }
        ConsoleLog("Max server CCU exceeded!");
    }

    /// <summary>
    /// Callback for processing incoming UDP data packets from clients.
    /// </summary>
    private static void UdpReceiveCallback(IAsyncResult result)
    {
        try
        {
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpListener.EndReceive(result, ref clientEndPoint);
            udpListener.BeginReceive(UdpReceiveCallback, null);
            if (data.Length < 4)
            {
                return;
            }
            Packet packet = new Packet(data);
            int clientId = packet.ReadInt();
            if (clientId == 0) return;
            if (clientsList[clientId].udp.EndPoint == null)
            {
                clientsList[clientId].udp.ConnectUDP(clientEndPoint);
                return;
            }
            if (clientsList[clientId].udp.EndPoint.ToString() == clientEndPoint.ToString())
            {
                clientsList[clientId].udp.UdpDataHandler(packet);
            }
        }
        catch (Exception ex)
        {
            ConsoleLog($"Error receiving UDP {ex}");
        }
    }
    /// <summary>
    /// Sends a UDP packet to the specified client endpoint.
    /// </summary>
    public static void SendUDPData(IPEndPoint clientEndPoint, Packet sendPacket)
    {
        try
        {
            if (clientEndPoint != null)
            {
                udpListener.BeginSend(sendPacket.ConvertToArray(), sendPacket.DataLength(), clientEndPoint, null, null);
            }
        }
        catch (Exception ex)
        {
            ConsoleLog($"Error sending via UDP {ex}");
        }
    }

    public static DefaultNetworkSettings GetSettings()
    {
        return DefaultSettings;
    }

    public static ServerSettings GetServerSettings()
    {
        return ServerSettings;
    }
    /// <summary>
    /// Logs a message to the console with a newline.
    /// </summary>
    public static void ConsoleLog(string message)
    {
        Console.WriteLine(message + "\n");
    }
}

