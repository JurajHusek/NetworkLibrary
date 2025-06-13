using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System;
using System.Security.Cryptography;
/// <summary>
/// Handles incoming packets from the server and performs client-side actions accordingly.
/// </summary>
public class ClientReceiveHandler : MonoBehaviour
{
    /// <summary>
    /// Validates the HMAC of a received packet using the client's stored key.
    /// </summary>
    public static bool ValidateHmac(Packet receivedPacket)
    {
        byte[] dataWithoutHmac = receivedPacket.GetDataWithoutHmac(); 
        byte[] receivedHmac = receivedPacket.GetHmac(); 
        byte[] expectedHmac = GenerateHmac(dataWithoutHmac, Client.instance.GetHmacKey());
        /// <summary>
        /// Validity check of HMAC.
        /// </summary>
        return StructuralComparisons.StructuralEqualityComparer.Equals(receivedHmac, expectedHmac);
    }
    /// <summary>
    /// Generates a HMAC-SHA256 hash for the provided data using the given key.
    /// </summary>
    public static byte[] GenerateHmac(byte[] data, byte[] key)
    {
        using (HMACSHA256 hmac = new HMACSHA256(key))
        {
            return hmac.ComputeHash(data);
        }
    }
    /// <summary>
    /// Handles the welcome packet from the server, sets the client ID, and initiates UDP connection.
    /// </summary>
    public static void Welcome(Packet receivedPacket)
    {
        string ReceivedMessage = receivedPacket.ReadString();
        int id = receivedPacket.ReadInt();
        Client.instance.SetId(id);
        if(ReceivedMessage != "")
        {
            Client.instance.ReceivedMessage($"Server message: {ReceivedMessage} [{Client.instance.GetUsername()}]");
        }
        ClientSendHandler.WelcomeReceived();
        Client.instance.isConnected = true;
        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.Socket.Client.LocalEndPoint).Port);
    }
    /// <summary>
    /// Handles receiving the HMAC key from the server and stores it on the client.
    /// </summary>
    public static void ReceiveHmacKey(Packet receivedPacket)
    {
        int keyLength = receivedPacket.ReadInt(); 
        byte[] hmacKey = receivedPacket.ReadBytes(keyLength); 
        Client.instance.SetHmacKey(hmacKey);
    }
    /// <summary>
    /// Handles receiving an authentication token from the server and stores it on the client.
    /// </summary>
    public static void ReceiveToken(Packet receivedPacket)
    {
        string token = receivedPacket.ReadString();
        Client.instance.SetToken(token);
    }
    /// <summary>
    /// Handles a latency response packet from the server.
    /// </summary>
    public static void Latency(Packet receivedPacket)
    {
        int clientId = receivedPacket.ReadInt();
        int pcktId = receivedPacket.ReadInt();
        Client.instance.MeassureAnswer(pcktId);
    }
    /// <summary>
    /// Spawns a user prefab on the client.
    /// </summary>
    public static void SpawnUser(Packet receivedPacket)
    {
        if (Client.instance.GetDefaultSettings().useHMAC == true)
        {
            if (!ValidateHmac(receivedPacket))
            {
                Debug.LogError("Invalid HMAC received for packet. Dropping packet.");
                return;
            }
        }
        int clientId = receivedPacket.ReadInt();
        string userName = receivedPacket.ReadString();
        Client.instance.SpawnUserPrefab(clientId, userName);
    }
    /// <summary>
    /// Handles a bandwidth test response from the server.
    /// </summary>
    public static void BandwidthReply(Packet receivedPacket)
    {
        if (Client.instance.GetDefaultSettings().useHMAC == true)
        {
            if (!ValidateHmac(receivedPacket))
            {
                Debug.LogError("Invalid HMAC received for packet. Dropping packet.");
                return;
            }
        }
        Client.instance.ReceivedBandwidth();
    }
    /// <summary>
    /// Handles a throughput test response from the server.
    /// </summary>
    public static void ThroughputReply(Packet receivedPacket)
    {
        int pcktId = receivedPacket.ReadInt();
        int dataSize = receivedPacket.ReadInt();
        Client.instance.ReceivedThroughput(pcktId, dataSize);
    }
    /// <summary>
    /// Receives a private text message from server.
    /// </summary>
    public static void ReceiveMessage(Packet receivedPacket)
    {
        if (Client.instance.GetDefaultSettings().useHMAC == true)
        {
            if (!ValidateHmac(receivedPacket))
            {
                Debug.LogError("Invalid HMAC received for packet. Dropping packet.");
                return;
            }
        }
        string message = receivedPacket.ReadString();
        Client.instance.ReceivedMessage(message);
    }
    /// <summary>
    /// Receives a broadcast message sent to all clients by the server.
    /// </summary>
    public static void ReceiveMessageAll(Packet receivePacket)
    {
        string message = receivePacket.ReadString();
        Client.instance.ReceivedMessage(message);
    }
    /// <summary>
    /// Handles disconnection of a specific client.
    /// </summary>
    public static void DisconnectUser(Packet receivedPacket)
    {
        int clientId = receivedPacket.ReadInt();
        if(clientId == Client.instance.GetId())
        {
            Client.instance.Disconnect();
        } else
        {
            Client.instance.DisconnectOther(clientId);
        }
    }
    /// <summary>
    /// Receives and applies a position and rotation update for a user in the scene.
    /// </summary>
    public static void TransformUpdate(Packet receivedPacket)
    {
        int id = receivedPacket.ReadInt();
        Vector3 pos = receivedPacket.ReadVector3();
        Quaternion rot = receivedPacket.ReadQuaternion();
        Client.instance.ChangeTransform(id, pos, rot);
    }
    /// <summary>
    /// Receives and applies an animation update from the server for a specific user.
    /// </summary>
    public static void AnimationUpdate(Packet receivedPacket)
    {
        int id = receivedPacket.ReadInt();
        int type = receivedPacket.ReadInt();
        string paramName = receivedPacket.ReadString();
        float value;
        if (type != 2)
        {
            value = receivedPacket.ReadFloat();
        } else {
            value = 0f;
        }
        Client.instance.ChangeAnimatorState(id,type,paramName,value);
    }
}
