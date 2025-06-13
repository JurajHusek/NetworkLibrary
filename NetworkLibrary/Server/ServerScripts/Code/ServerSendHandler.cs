using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;

/// <summary>
/// Contains all of the methods for packet sending according to their type.
/// </summary>
public class ServerSendHandler
{
    /// <summary>
    /// Adds HMAC to packet, returns packet with HMAC.
    /// </summary>
    public static Packet AddHmac(Packet sendPacket, int ClientNum)
    {
        /// <summary>
        /// Value of clients HMAC key.
        /// </summary>
        byte[] hmacKey = ServerLogic.clientsList[ClientNum].tcp.hmacKey;
        byte[] hmac = GenerateHmac(sendPacket, hmacKey);
        sendPacket.Write(hmac);
        return sendPacket;
    }
    /// <summary>
    /// Generates HMAC of the data with the key from argument.
    /// </summary>
    public static byte[] GenerateHmac(Packet packet, byte[] key)
    {
        byte[] packetData = packet.ConvertToArray();

        /// <summary>
        /// Uses HMACSHA256 for computing HMAC value.
        /// </summary>
        using (HMACSHA256 hmac = new HMACSHA256(key))
        {
            return hmac.ComputeHash(packetData);
        }
    }
    /// <summary>
    /// Sends a packet to all clients to inform them that a user has disconnected.
    /// </summary>
    public static void DisconnectedUser(int clientId)
    {
        Packet sendPacket = new Packet((int)Packet.ServerPackets.DisconnectUser);
        sendPacket.Write(clientId);
        if(ServerLogic.GetSettings().useHMAC == true)
        {
            sendPacket = AddHmac(sendPacket, clientId);
        }
        SendToAll(sendPacket);
    }
    /// <summary>
    /// Sends a latency response packet (ping reply) to a specific client.
    /// </summary>
    public static void LatencyToUser(int clientId, int pcktId)
    {
        Packet sendPacket = new Packet((int)Packet.ServerPackets.Latency);
        sendPacket.Write(clientId);
        sendPacket.Write(pcktId);
        UDPSendToClient(sendPacket, clientId);
    }
    /// <summary>
    /// Sends a welcome message to a newly connected client.
    /// </summary>
    public static void WelcomePacket(int clientId, string message)
    {
        Packet SendPacket = new Packet((int)Packet.ServerPackets.welcome);
        SendPacket.Write(message);
        SendPacket.Write(clientId);
        SendToClient(SendPacket, clientId);
    }
    /// <summary>
    /// Sends the HMAC key to a client for securing message integrity.
    /// </summary>
    public static void SendHmacKeyPacket(int clientID, byte[] hmacKey)
    {
        Packet sendPacket = new Packet((int)Packet.ServerPackets.HmacKey); 
        sendPacket.Write(hmacKey.Length); 
        sendPacket.Write(hmacKey);       
        SendToClient(sendPacket, clientID);
    }
    /// <summary>
    /// Sends an authentication token to the client.
    /// </summary>
    public static void SendAuthPacket(int clientID, string token)
    {
        Packet sendPacket = new Packet((int)Packet.ServerPackets.AuthToken);
        sendPacket.Write(token);
        sendPacket.Write(clientID);
        SendToClient(sendPacket, clientID);
    }
    /// <summary>
    /// Sends a packet to spawn a new user instance on the client side.
    /// </summary>
    public static void SpawnUser(int clientId, ServerSideClientInstance user)
    {
        Packet sendPacket = new Packet((int)Packet.ServerPackets.SpawnUser);
        sendPacket.Write(user.userID);
        sendPacket.Write(user.userName);
        if (ServerLogic.GetSettings().useHMAC == true)
        {
            sendPacket = AddHmac(sendPacket, clientId);
        }
        SendToClient(sendPacket, clientId);
    }
    /// <summary>
    /// Sends a bandwidth reply packet to the client after measurement.
    /// </summary>
    public static void SendBandwidthReply(int clientId)
    {
        Packet sendPacket = new Packet((int)Packet.ServerPackets.BandwidthReply);
        sendPacket.Write(clientId);
        if (ServerLogic.GetSettings().useHMAC == true)
        {
            sendPacket = AddHmac(sendPacket, clientId);
        }
        SendToClient(sendPacket, clientId);
    }
    /// <summary>
    /// Sends a throughput measurement result to the client.
    /// </summary>
    public static void SendThroughputReply(int clientId, int pcktId, int dataSize)
    {
        Packet sendPacket = new Packet((int)Packet.ServerPackets.ThroughputReply);
        sendPacket.Write(pcktId);
        sendPacket.Write(dataSize);
        UDPSendToClient(sendPacket, clientId);
    }
    /// <summary>
    /// Sends a private text message to a specific client.
    /// </summary>
    public static void SendMessage(int clientId, string message)
    {
        Packet sendPacket = new Packet((int)Packet.ServerPackets.Message);
        sendPacket.Write(message);
        sendPacket.Write(clientId);
        if (ServerLogic.GetSettings().useHMAC == true)
        {
            sendPacket = AddHmac(sendPacket, clientId);
        }
        SendToClient(sendPacket, clientId);
        
    }
    /// <summary>
    /// Sends a message to all clients except the sender.
    /// </summary>
    public static void SendMessageAll(int clientId, string message)
    {
        Packet sendPacket = new Packet((int)Packet.ServerPackets.MessageAll);
        sendPacket.Write(message);
        sendPacket.Write(clientId);
        SendToAllExc(sendPacket, clientId);

    }
    /// <summary>
    /// Sends a packet to all connected clients.
    /// </summary>
    public static void SendToAll(Packet sendPacket)
    {
        sendPacket.SizeOfDataAtStart();
        foreach (var user in ServerLogic.clientsList)
        {
            ServerSideClient ConnectedClient = user.Value;
            ConnectedClient.tcp.Send(sendPacket);
        }
    }
    /// <summary>
    /// Sends a packet to a specific client.
    /// </summary>
    public static void SendToClient(Packet sendPacket, int clientId)
    {
        sendPacket.SizeOfDataAtStart();
        ServerLogic.clientsList[clientId].tcp.Send(sendPacket);
    }
    /// <summary>
    /// Sends updated transform (position and rotation) to all clients.
    /// </summary>
    public static void SendUpdatedTransform(int clientId, Vector3 pos, Quaternion rot)
    {
        Packet sendPacket = new Packet((int)Packet.ServerPackets.TransformUpdate);
        sendPacket.Write(clientId);
        sendPacket.Write(pos);
        sendPacket.Write(rot);
        if(ServerLogic.GetSettings().allowUDP == true)
        {
            UDPSendToAll(sendPacket);
        } else
        {
            SendToAll(sendPacket);
        }
    }
    /// <summary>
    /// Sends updated animation data to all clients.
    /// </summary>
    public static void SendUpdatedAnimation(int clientId, int type, string name, float value)
    {
        Packet sendPacket = new Packet((int)Packet.ServerPackets.AnimationUpdate);
        sendPacket.Write(clientId);
        sendPacket.Write(type);
        sendPacket.Write(name);
        sendPacket.Write(value);
        SendToAll(sendPacket);
    }
    /// <summary>
    /// Sends a packet to all clients except one specified by ID.
    /// </summary>
    public static void SendToAllExc(Packet sendPacket, int exceptClient)
    {
        sendPacket.SizeOfDataAtStart();
        for (int i = 1; i <= ServerLogic.maxCCU; i++)
        {
            if (i != exceptClient)
            {
                ServerLogic.clientsList[i].tcp.Send(sendPacket);
            }
        }
    }
    /// <summary>
    /// Sends a packet to a specific client using UDP.
    /// </summary>
    private static void UDPSendToClient(Packet sendPacket, int clientId)
    {
        sendPacket.SizeOfDataAtStart();
        ServerLogic.clientsList[clientId].udp.SendUDPData(sendPacket);
    }
    /// <summary>
    /// Sends a packet to all clients using UDP.
    /// </summary>
    public static void UDPSendToAll(Packet sendPacket)
    {
        sendPacket.SizeOfDataAtStart();
        foreach (var user in ServerLogic.clientsList)
        {
            ServerSideClient ConnectedClient = user.Value;
            ConnectedClient.udp.SendUDPData(sendPacket);
        }
    }




}

