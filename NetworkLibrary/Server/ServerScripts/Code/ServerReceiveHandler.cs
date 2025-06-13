using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Contains all of the managing methods for received packets on server.
/// </summary>
public class ServerReceiveHandler
{
    /// <summary>
    /// Handles the initial welcome message from a client and completes handshake.
    /// </summary>
    public static void WelcomeReceived(int clientId, Packet receivedPacket)
    {
        int readClientId = receivedPacket.ReadInt();
        string clientUsername = receivedPacket.ReadString();
        ServerLogic.clientsList[clientId].clientUsername = clientUsername;
        Console.WriteLine($"{ServerLogic.clientsList[clientId].tcp.Socket.Client.RemoteEndPoint} [{clientUsername}] has connected successfully and is now Client{clientId}\n");
        if (clientId != readClientId)
        {
            Console.WriteLine($"Client \"{clientUsername}\" (ID: {clientId}) has assumed the wrong client ID ({readClientId})!");
            return;
        }
        ServerLogic.clientsList[clientId].SendUserToAction(clientUsername);
    }

    /// <summary>
    /// Handles client latency measurement request.
    /// </summary>
    public static void LatencyRequestReceived(int clientId, Packet receivedPacket)
    {
        int readClientId = receivedPacket.ReadInt();
        int pcktId = receivedPacket.ReadInt();
        if (clientId != readClientId)
        {
            Console.WriteLine($"Client (ID: {clientId}) has assumed the wrong client ID ({readClientId})!");
            return;
        }
        ServerLogic.clientsList[clientId].SendLatencyReply(pcktId);
    }

    /// <summary>
    /// Handles request to send a private message to another user.
    /// </summary>
    public static void MessageToUserRequestReceived(int clientId, Packet receivedPacket)
    {
        int readClientId = receivedPacket.ReadInt();
        if (ServerLogic.GetSettings().useAuthentificationTokens)
        {
            string token = receivedPacket.ReadString();
            if (token != ServerLogic.clientsList[clientId].GetToken())
            {
                Debug.LogError("Auth token is NOT valid");
            }
        }
        string message = receivedPacket.ReadString();
        ServerLogic.clientsList[readClientId].ResendMessage(message);
    }

    /// <summary>
    /// Handles request to broadcast a message to all clients.
    /// </summary>
    public static void MessageToAllRequestReceived(int clientId, Packet receivedPacket)
    {
        int readClientId = receivedPacket.ReadInt();
        if (ServerLogic.GetSettings().useAuthentificationTokens)
        {
            string token = receivedPacket.ReadString();
            if (token != ServerLogic.clientsList[clientId].GetToken())
            {
                Debug.LogError("Auth token is NOT valid");
            }
        }
        string message = receivedPacket.ReadString();
        if (clientId != readClientId)
        {
            Console.WriteLine($"Client (ID: {clientId}) has assumed the wrong client ID ({readClientId})!");
            return;
        }
        ServerLogic.clientsList[readClientId].ResendToAll(message);
    }

    /// <summary>
    /// Handles client disconnection request.
    /// </summary>
    public static void DisconnectRequestReceived(int clientId, Packet receivedPacket)
    {
        int readClientId = receivedPacket.ReadInt();
        if (clientId != readClientId)
        {
            Console.WriteLine($"Client (ID: {clientId}) has assumed the wrong client ID ({readClientId})!");
            return;
        }
        if (ServerLogic.GetSettings().useAuthentificationTokens)
        {
            string token = receivedPacket.ReadString();
            if (token != ServerLogic.clientsList[clientId].GetToken())
            {
                Debug.LogError("Auth token is NOT valid");
            }
        }
        ServerLogic.clientsList[clientId].DisconnectRequest();
    }

    /// <summary>
    /// Handles transform update from the client.
    /// </summary>
    public static void TransformChangeReceived(int clientId, Packet receivedPacket)
    {
        int readClientId = receivedPacket.ReadInt();
        if (ServerLogic.GetSettings().useAuthentificationTokens)
        {
            string token = receivedPacket.ReadString();
            if (token != ServerLogic.clientsList[clientId].GetToken())
            {
                Debug.LogError("Auth token is NOT valid");
            }
        }
        Vector3 pos = receivedPacket.ReadVector3();
        Quaternion rot = receivedPacket.ReadQuaternion();
        if (clientId != readClientId)
        {
            Console.WriteLine($"Client (ID: {clientId}) has assumed the wrong client ID ({readClientId})!");
            return;
        }
        ServerLogic.clientsList[clientId].UpdateTransform(pos, rot);
    }

    /// <summary>
    /// Handles animation change event sent from the client.
    /// </summary>
    public static void AnimationChangeReceived(int clientId, Packet receivedPacket)
    {
        int readClientId = receivedPacket.ReadInt();
        if (ServerLogic.GetSettings().useAuthentificationTokens)
        {
            string token = receivedPacket.ReadString();
            if (token != ServerLogic.clientsList[clientId].GetToken())
            {
                Debug.LogError("Auth token is NOT valid");
            }
        }
        int type = receivedPacket.ReadInt();
        string name = receivedPacket.ReadString();
        float value = receivedPacket.ReadFloat();
        if (clientId != readClientId)
        {
            Console.WriteLine($"Client (ID: {clientId}) has assumed the wrong client ID ({readClientId})!");
            return;
        }
        ServerLogic.clientsList[clientId].UpdateAnimation(type, name, value);
    }

    /// <summary>
    /// Handles request to measure network bandwidth.
    /// </summary>
    public static void BandwidthRequestReceived(int clientId, Packet receivedPacket)
    {
        int readClientId = receivedPacket.ReadInt();
        if (clientId != readClientId)
        {
            Console.WriteLine($"Client (ID: {clientId}) has assumed the wrong client ID ({readClientId})!");
            return;
        }
        if (ServerLogic.GetSettings().useAuthentificationTokens)
        {
            string token = receivedPacket.ReadString();
            if (token != ServerLogic.clientsList[clientId].GetToken())
            {
                Debug.LogError("Auth token is NOT valid");
            }
        }
        int dataLength = receivedPacket.ReadInt();
        byte[] data = receivedPacket.ReadBytes(dataLength);
        ServerSendHandler.SendBandwidthReply(clientId);
    }

    /// <summary>
    /// Handles request to measure network throughput.
    /// </summary>
    public static void ThroughputRequestReceived(int clientId, Packet receivedPacket)
    {
        int readClientId = receivedPacket.ReadInt();
        if (clientId != readClientId)
        {
            Console.WriteLine($"Client (ID: {clientId}) has assumed the wrong client ID ({readClientId})!");
            return;
        }
        int pcktId = receivedPacket.ReadInt();
        int dataLength = receivedPacket.ReadInt();
        byte[] data = receivedPacket.ReadBytes(dataLength);
        int sizeData = data.Length;
        ServerSendHandler.SendThroughputReply(clientId, pcktId, sizeData);
    }
}
