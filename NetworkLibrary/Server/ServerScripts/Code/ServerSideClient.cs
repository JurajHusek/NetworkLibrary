using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// kniznice na pracu s tcp a ip protokolom
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Diagnostics;

/// <summary>
/// Represents a server-side connected client, including TCP and UDP communication handlers,
/// user state, and connection security such as HMAC and authentication tokens.
/// </summary>
public class ServerSideClient 
{
    /// <summary>Unique client ID on the server.</summary>
    public int clientID;

    /// <summary>Username of the connected client.</summary>
    public string clientUsername;

    /// <summary>TCP connection handler.</summary>
    public TCP tcp;

    /// <summary>UDP connection handler.</summary>
    public UDP udp;

    /// <summary>Size of the data buffer used for transmission.</summary>
    public static int BufferSize = 4096;

    /// <summary>Instance of the client's representation in the game world.</summary>
    public ServerSideClientInstance user;

    private byte[] hmacKey;
    private string auth_token;
    public DefaultNetworkSettings DefaultSettings;
    private bool isDisconnected = true;

    /// <summary>
    /// Generates a random alphanumeric authentication token.
    /// </summary>
    public static string GenerateRandomToken(int length = 32)
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; 
        byte[] randomBytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = validChars[randomBytes[i] % validChars.Length];
        }
        return new string(chars);
    }
    /// <summary>Assigns a new authentication token to the client.</summary>
    private void SetToken()
    {
        auth_token = GenerateRandomToken();
    }
    /// <summary>Gets the client's current authentication token.</summary>
    public string GetToken()
    {
        return auth_token;
    }

    /// <summary>
    /// Initializes a new client instance and sets up its TCP and UDP handlers.
    /// </summary>
    public ServerSideClient(int newClientID) 
    {
        isDisconnected = false;
        clientID = newClientID;
        DefaultSettings = ServerLogic.GetSettings();
        tcp = new TCP(clientID);
        udp = new UDP(clientID);

    }
    /// <summary>
    /// Disconnects the client from the server if not already disconnected.
    /// Cleans up the user object and resets the client slot.
    /// </summary>
    public void Disconnect()
    {
        if (isDisconnected)
        {
            return;
        }
        if (tcp != null && tcp.Socket != null)
        {
            ConsoleLog($"{tcp.Socket.Client.RemoteEndPoint} [{clientUsername}] has disconnected from the Server");
            ServerSendHandler.DisconnectedUser(clientID);
            tcp.Disconnect();
            udp.Disconnect();
            ThreadsController.StartOnMainThread(() =>
            {
                UnityEngine.Object.Destroy(user.gameObject);
                user = null;
                
            });
            isDisconnected = true;
            ServerLogic.clientsList[clientID] = new ServerSideClient(clientID);
        }
        else
        {
            ConsoleLog($"Client {clientUsername} has already been disconnected.");
            return;
        }
    }
    /// <summary>
    /// Handles a user-initiated disconnect request and broadcasts it to others.
    /// </summary>
    public void DisconnectRequest()
    {
        if (isDisconnected)
        {
            ConsoleLog($"[Disconnect] Already disconnected {clientUsername}");
            return;
        }
        ConsoleLog($"{tcp.Socket.Client.RemoteEndPoint} [{clientUsername}] wants to disconnect");
        string message = "Server message: " + clientUsername + " has disconnected";
        ServerSendHandler.SendMessageAll(clientID, message);
        Disconnect();
    }
    /// <summary>
    /// Sends a latency reply (pong) to the client.
    /// </summary>
    public void SendLatencyReply(int pcktId)
    {
        ServerSendHandler.LatencyToUser(clientID, pcktId);
    }
    /// <summary>
    /// Re-sends a private message back to the same client.
    /// </summary>
    public void ResendMessage(string message)
    {
        ServerSendHandler.SendMessage(clientID, message);
    }
    /// <summary>
    /// Re-sends a message to all connected clients except the sender.
    /// </summary>
    public void ResendToAll(string Message)
    {
        ServerSendHandler.SendMessageAll(clientID, Message);
    }
    /// <summary>
    /// Sends updated transform (position and rotation) to all clients.
    /// </summary>
    public void UpdateTransform(Vector3 pos, Quaternion rot)
    {
        ServerSendHandler.SendUpdatedTransform(clientID, pos, rot);
    }
    /// <summary>
    /// Sends updated animation parameters to all clients.
    /// </summary>
    public void UpdateAnimation(int type, string name, float value)
    {
        ServerSendHandler.SendUpdatedAnimation(clientID, type, name, value);
    }
    /// <summary>
    /// Initializes a new user and synchronizes it with all other clients.
    /// Sends spawn info to self and others.
    /// </summary>
    public void SendUserToAction(string username)
    {
        user = NetworkServer.instance.NewUser();
        user.UserInitialization(clientID, username, 1);
        foreach (ServerSideClient client in ServerLogic.clientsList.Values)
        {
            if (client.user != null)
            {
                if (client.clientID != clientID)
                {
                    ServerSendHandler.SpawnUser(clientID, client.user);
                }
            }
        }
        foreach (ServerSideClient client in ServerLogic.clientsList.Values)
        {
            if (client.user != null)
            {
                ServerSendHandler.SpawnUser(client.clientID, user);
            }   
        }
        string message = "Server message: " + username + " has connected";
        ServerSendHandler.SendMessageAll(clientID, message);
    }
    /// <summary>
    /// Generates a secure random 256-bit HMAC key.
    /// </summary>
    private static byte[] GenerateHmacKey()
    {
        using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
        {
            byte[] key = new byte[32]; // 256-bitový HMAC k¾úè
            rng.GetBytes(key);
            return key;
        }
    }
    /// <summary>
    /// Class for managing TCP connection.
    /// </summary>
    public class TCP 
    {
        private Stream Stream; 
        private byte[] receiveBuffer; 
        public TcpClient Socket; 
        private readonly int Id; 
        private Packet receivedPacket;
        public byte[] hmacKey;
        /// <summary>
        /// Initializes a new TCP object with the given client ID.
        /// </summary>
        public TCP(int id) 
        {
            Id = id;
        }
        /// <summary>
        /// Connects a TCP client and starts listening for data. Optionally initializes TLS, HMAC, and auth token.
        /// </summary>
        public void Connect(TcpClient TcpSocket)
        {
            Socket = TcpSocket; 
            Socket.ReceiveBufferSize = BufferSize;
            Socket.SendBufferSize = BufferSize;
            if (ServerLogic.GetSettings().useTLS == true)
            {
                SslStream SslStream = new SslStream(Socket.GetStream(), false); 
                try
                {
                    SslStream.AuthenticateAsServer(
                        ServerLogic.serverCertificate, 
                        false,                        
                        SslProtocols.Tls12,            
                        false                          
                    );
                    Stream = SslStream;
                }
                catch (Exception ex)
                {
                    ServerLogic.clientsList[Id].DisconnectRequest();
                    return;
                }
            } else
            {
                Stream = Socket.GetStream();
            }
            receiveBuffer = new byte[BufferSize]; 
            receivedPacket = new Packet();
            hmacKey = GenerateHmacKey();
            ServerLogic.clientsList[Id].hmacKey = hmacKey;
            ServerLogic.clientsList[Id].SetToken();
            Stream.BeginRead(receiveBuffer, 0, BufferSize, ReceiveCallback, null); 
            if(ServerLogic.GetServerSettings().sendWelcomeToServerMesage == true)
            {
                ServerSendHandler.WelcomePacket(Id, ServerLogic.GetServerSettings().welcomeToServerMessage);
            } else
            {
                ServerSendHandler.WelcomePacket(Id, "");
            }
            
            if(ServerLogic.GetSettings().useHMAC == true)
            {
                ServerSendHandler.SendHmacKeyPacket(Id, hmacKey);
            }
            if(ServerLogic.GetSettings().useAuthentificationTokens == true)
            {
                Console.WriteLine($"Sending auth token {ServerLogic.clientsList[Id].auth_token}");
                ServerSendHandler.SendAuthPacket(Id, ServerLogic.clientsList[Id].GetToken());
            }
        }
        /// <summary>
        /// Sends a packet over the TCP stream to the connected client.
        /// </summary>
        public void Send(Packet sendPacket)
        {
            try
            {
                if (Socket != null && Stream != null)
                {
                    Stream.Write(sendPacket.ConvertToArray());
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"Error, cannot send data to client {Id}: {ex}");
            }
        }
        /// <summary>
        /// Handles received raw data and calls PacketAction() to manage packet type.
        /// </summary>
        private bool HandleData(byte[] Data)
        {
            if (ServerLogic.clientsList[Id].isDisconnected)
            {
                return false;
            }
            int packetLength = 0;
            receivedPacket.AddBytesToBuffers(Data);
            if (receivedPacket.UnreadLength() >= 4)
            {
                packetLength = receivedPacket.ReadInt();
                if (packetLength <= 0)
                {
                    return true;
                }
            }
            while (packetLength > 0 && packetLength <= receivedPacket.UnreadLength())
            {
                byte[] packetBytes = receivedPacket.ReadBytes(packetLength);
                ThreadsController.StartOnMainThread(() =>
                {
                    Packet packet = new Packet(packetBytes);
                    int packetId = packet.ReadInt();
                    ServerLogic.PacketAction(packetId, packet, Id);
                });
                packetLength = 0;
                if (receivedPacket.UnreadLength() >= 4)
                {
                    packetLength = receivedPacket.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }
            }
            if (packetLength <= 1)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Closes the TCP connection and releases related resources.
        /// </summary>
        public void Disconnect()
        {
            Socket.Close();
            Stream = null;
            receivedPacket = null;
            receiveBuffer = null;
            Socket = null;
        }
        /// <summary>
        /// Callback function for receiving TCP data asynchronously.
        /// </summary>
        private void ReceiveCallback(IAsyncResult result) 
        {
            try
            {
                if (Stream == null)
                {
                    return;
                }
                int ByteLength = Stream.EndRead(result); 
                if (ByteLength <= 0) 
                {
                    ServerLogic.clientsList[Id].Disconnect();
                    return;
                }
                byte[] Data = new byte[ByteLength];
                Array.Copy(receiveBuffer, Data, ByteLength);                                         
                receivedPacket.ClearPacket(HandleData(Data));
                Stream.BeginRead(receiveBuffer, 0, BufferSize, ReceiveCallback, null);
            }
            catch (Exception ex) 
            {
                ConsoleLog($"Error receiving TCP {ex}");
                ServerLogic.clientsList[Id].Disconnect();
            }
        }
    }
    /// <summary>
    /// Class for managing UDP connection.
    /// </summary>
    public class UDP
    {
        public IPEndPoint EndPoint;
        private int clientId;
        /// <summary>
        /// Initializes a new UDP object with the given client ID.
        /// </summary>
        public UDP(int id)
        {
            clientId = id;
        }
        /// <summary>
        /// Sets the remote endpoint for the UDP client.
        /// </summary>
        public void ConnectUDP(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }
        /// <summary>
        /// Sends a UDP packet to the client using the configured endpoint.
        /// </summary>
        public void SendUDPData(Packet SendPacket)
        {
            ServerLogic.SendUDPData(EndPoint, SendPacket);
        }
        /// <summary>
        /// Handles incoming UDP data and calls PacketAction() on the main thread.
        /// </summary>
        public void UdpDataHandler(Packet data)
        {
            int dataLength = data.ReadInt();
            byte[] packetBytes = data.ReadBytes(dataLength);
            ThreadsController.StartOnMainThread(() => {
                Packet newPacket = new Packet(packetBytes);
                int packetId = newPacket.ReadInt();
                ServerLogic.PacketAction(packetId, newPacket, clientId);
            });
        }
        /// <summary>
        /// Disconnects the UDP client by clearing its endpoint.
        /// </summary>
        public void Disconnect()
        {
            EndPoint = null;
        }
    }

    public static void ConsoleLog(string message)
    {
        Console.WriteLine(message + "\n");
    }
}

