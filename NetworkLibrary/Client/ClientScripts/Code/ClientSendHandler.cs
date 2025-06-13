using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles sending various network requests from the client to the server using TCP or UDP.
/// </summary>
public class ClientSendHandler : MonoBehaviour
{
    /// <summary>
    /// Sends a response to the server confirming the getting of the welcome packet.
    /// </summary>
    public static void WelcomeReceived()
    {
        Packet newPacket = new Packet((int)Packet.ClientPackets.welcomeReceived);
        newPacket.Write(Client.instance.GetId());
        newPacket.Write(Client.instance.GetUsername());
        SendTCPData(newPacket);
    }
    /// <summary>
    /// Sends a UDP latency packet to the server.
    /// </summary>
    public static void MeassureRequest(int pcktId)
    {
        Packet newPacket = new Packet((int)Packet.ClientPackets.MeassureRequest);
        newPacket.Write(Client.instance.GetId());
        newPacket.Write(pcktId);
        SendUDPData(newPacket);
    }
    /// <summary>
    /// Sends a disconnect request to the server. Includes an authentication token if enabled.
    /// </summary>
    public static void DisconnectRequest()
    {
        Packet newPacket = new Packet((int)Packet.ClientPackets.DisconnectRequest);
        newPacket.Write(Client.instance.GetId());
        if (Client.instance.GetDefaultSettings().useAuthentificationTokens == true)
        {
            newPacket.Write(Client.instance.GetToken());
        }
        SendTCPData(newPacket);
    }
    /// <summary>
    /// Sends a private text message to a specific user by server.
    /// </summary>
    public static void MessageToUserRequest(string message, int toSendId)
    {
        Packet newPacket = new Packet((int)Packet.ClientPackets.MessageToUserRequest); 
        newPacket.Write(toSendId);
        if (Client.instance.GetDefaultSettings().useAuthentificationTokens == true)
        {
            newPacket.Write(Client.instance.GetToken());
        }
        newPacket.Write(message);
        SendTCPData(newPacket);
    }
    /// <summary>
    /// Sends a broadcast message to all users by server.
    /// </summary>
    public static void MessageToAllRequest(string message)
    {
        Packet newPacket = new Packet((int)Packet.ClientPackets.MessageToAllRequest);
        newPacket.Write(Client.instance.GetId());
        if (Client.instance.GetDefaultSettings().useAuthentificationTokens == true)
        {
            newPacket.Write(Client.instance.GetToken());
        }
        newPacket.Write(message);
        SendTCPData(newPacket);
    }
    /// <summary>
    /// Sends the updated position and rotation of an object to the server.
    /// Uses UDP or TCP depending on the current settings.
    /// </summary>
    public static void SendTransform(int id, Transform objectTransform)
    {
        Packet newPacket = new Packet((int)Packet.ClientPackets.TransformChange);
        newPacket.Write(id);
        if (Client.instance.GetDefaultSettings().useAuthentificationTokens == true)
        {
            newPacket.Write(Client.instance.GetToken());
        }
        newPacket.Write(objectTransform.position);
        newPacket.Write(objectTransform.rotation);
        if(Client.instance.GetDefaultSettings().allowUDP == true)
        {
            SendUDPData(newPacket);
        } else
        {
            SendTCPData(newPacket);
        }    
    }
    /// <summary>
    /// Sends an animation parameter update to the server.
    /// </summary>
    public static void SendAnimationUpdate(int type, string name, float value)
    {
        Packet packet = new Packet((int)Packet.ClientPackets.AnimationChange);
        packet.Write(Client.instance.GetId());
        if (Client.instance.GetDefaultSettings().useAuthentificationTokens == true)
        {
            packet.Write(Client.instance.GetToken());
        }
        packet.Write(type);       // 0=bool, 1=float, 2=trigger
        packet.Write(name);
        if (type != 2) packet.Write(value);
        SendTCPData(packet); 
    }
    /// <summary>
    /// Sends a packet with raw data to the server for bandwidth measurement.
    /// </summary>
    public static void SendBandwidthRequest(byte[] data)
    {
        Packet packet = new Packet((int)Packet.ClientPackets.BandwidthRequest);
        packet.Write(Client.instance.GetId());
        if (Client.instance.GetDefaultSettings().useAuthentificationTokens == true)
        {
            packet.Write(Client.instance.GetToken());
        }
        packet.Write(data.Length);
        packet.Write(data);
        SendTCPData(packet);
    }
    /// <summary>
    /// Sends a UDP packet to the server to test network throughput.
    /// </summary>
    public static void SendThroughputRequest(int pcktId, byte[] data)
    {
        Packet packet = new Packet((int)Packet.ClientPackets.ThroughputRequest);
        packet.Write(Client.instance.GetId());
        packet.Write(pcktId);
        packet.Write(data.Length);
        packet.Write(data);
        SendUDPData(packet);
    }

    /// <summary>
    /// Sends a packet to the server using TCP.
    /// </summary>
    private static void SendTCPData(Packet packet)
    {
        packet.SizeOfDataAtStart();
        Client.instance.tcp.Send(packet);
    }
    /// <summary>
    /// Sends a packet to the server using UDP if the client is connected and UDP is initialized.
    /// </summary>
    private static void SendUDPData(Packet packet)
    {
        if (Client.instance == null || !Client.instance.isConnected || Client.instance.udp == null) return;
        packet.SizeOfDataAtStart();
        Client.instance.udp.SendUdpData(packet);
    }
}
