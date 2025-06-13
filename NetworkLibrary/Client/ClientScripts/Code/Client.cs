using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Main MonoBehaviour singleton class handling the client-side networking logic (TCP, UDP) in Unity.
/// </summary>
public class Client : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the client.
    /// </summary>
    public static Client instance;
    /// <summary>
    /// Default buffer size used for sending and receiving data (in bytes).
    /// </summary>
    private static int DefaultBufferSize = 4096;
    /// <summary>
    /// Network settings shared between client and server (IP, port, security, ...).
    /// </summary>
    [SerializeField] private DefaultNetworkSettings defaultNetworkSettings;
    /// <summary>
    /// Client-specific configuration used during runtime.
    /// </summary>
    [SerializeField] ClientSettings ClientSettings;
    /// <summary>
    /// The unique client id assigned by the server.
    /// </summary>
    private int id = 0;
    /// <summary>
    /// The current username of the client.
    /// </summary>
    private string username = "";
    /// <summary>
    /// Handles TCP communication with the server.
    /// </summary>
    public TCP tcp;
    /// <summary>
    /// Handles UDP communication with the server.
    /// </summary>
    public UDP udp;
    /// <summary>
    /// Indicates whether the client is currently connected to the server.
    /// </summary>
    public bool isConnected = false;
    /// <summary>
    /// Reference to the coroutine responsible for measuring latency.
    /// </summary>
    private Coroutine latencyMeassure;
    /// <summary>
    /// HMAC key used for message authentication.
    /// </summary>
    private byte[] hmacKey;
    /// <summary>
    /// Authentication token used to verify client identity.
    /// </summary>
    private string authToken;
    /// <summary>
    /// Event triggered when the client successfully connects to the server.
    /// </summary>
    public event Action OnConnected;
    /// <summary>
    /// Event triggered when the client fails to connect, providing an error message.
    /// </summary>
    public event Action<string> OnConnectionFailed;
    /// <summary>
    /// Event triggered when the client is disconnected from the server.
    /// </summary>
    public event Action OnDisconnected;
    /// <summary>
    /// Event triggered when a text message is received from the server.
    /// </summary>
    public event Action<string> OnMessageReceived;
    /// <summary>
    /// Event triggered when a transform update is received.
    /// </summary>
    public event Action<int,Vector3,Quaternion> OnTransformReceived;
    /// <summary>
    /// Event triggered when an animation update is received.
    /// </summary>
    public event Action<int,int, string, float> OnAnimationChangeReceived;
    /// <summary>
    /// Event triggered when a ping response is received.
    /// </summary>
    public event Action<int> OnPingReply;
    /// <summary>
    /// Event triggered when the bandwidth test result is received.
    /// </summary>
    public event Action OnBandwidthReply;
    /// <summary>
    /// Event triggered when throughput test results are received.
    /// </summary>
    public event Action<int,int> OnThroughputReply;
    /// <summary>
    /// Ensures only one instance of the client exists (Singleton pattern).
    /// </summary>
    private void Awake() 
    {
        if(instance == null)
        {
            instance = this;
        } 
        else if(instance != this)
        {
            Destroy(this);
        }
    }
    /// <summary>
    /// Checks if the specified ID belongs to this local client.
    /// </summary>
    public bool IsLocal(int checkId)
    {
        if(id == checkId) {
            return true;
        } else {
            return false;
        }
    }
    /// <summary>
    /// Sets the authentication token.
    /// </summary>
    public void SetToken(string token)
    {
        authToken = token;
    }
    /// <summary>
    /// Gets the authentication token.
    /// </summary>
    public string GetToken()
    {
        return authToken;
    }
    /// <summary>
    /// Gets the HMAC key used for secure communication.
    /// </summary>
    public byte[] GetHmacKey()
    {
        return hmacKey;
    }
    /// <summary>
    /// Sets the HMAC key for secure communication.
    /// </summary>
    public void SetHmacKey(byte[] newHmacKey)
    {
        hmacKey = newHmacKey;
    }
    /// <summary>
    /// Gets the current username.
    /// </summary>
    public string GetUsername()
    {
        return username;
    }
    /// <summary>
    /// Sets the current username.
    /// </summary>
    public void SetUsername(string newUsername)
    {
        username = newUsername;
    }
    /// <summary>
    /// Gets the current client ID.
    /// </summary>
    public int GetId()
    {
        return id;
    }
    /// <summary>
    /// Sets the client ID.
    /// </summary>
    public void SetId(int newId)
    {
        id = newId;
    }
    /// <summary>
    /// Gets default network settings used for connection.
    /// </summary>
    public DefaultNetworkSettings GetDefaultSettings()
    {
        return defaultNetworkSettings;
    }
    /// <summary>
    /// Disconnects the client when the application quits.
    /// </summary>
    private void OnApplicationQuit()
    {
        Disconnect();
    }
    /// <summary>
    /// Initializes TCP and UDP communication handlers for this client.
    /// </summary>
    private void SetClient()
    {
        if (tcp != null)
        {
            tcp = null;
        }
        if (udp != null)
        {
            udp = null;
        }
        tcp = new TCP();
        udp = new UDP();
    }
    /// <summary>
    /// Handles the received packet by type by calling methods in ClientReceiveHandler.
    /// </summary>
    public static void PacketAction(int packetId, Packet packet)
    {
        if (!Enum.IsDefined(typeof(Packet.ServerPackets), packetId))
        {
            Debug.LogWarning($"Unknown packet ID: {packetId}");
            return;
        }

        Packet.ServerPackets packetType = (Packet.ServerPackets)packetId;

        switch (packetType)
        {
            case Packet.ServerPackets.welcome:
                ClientReceiveHandler.Welcome(packet);
                break;

            case Packet.ServerPackets.SpawnUser:
                ClientReceiveHandler.SpawnUser(packet);
                break;

            case Packet.ServerPackets.DisconnectUser:
                ClientReceiveHandler.DisconnectUser(packet);
                break;

            case Packet.ServerPackets.Latency:
                ClientReceiveHandler.Latency(packet);
                break;

            case Packet.ServerPackets.Message:
                ClientReceiveHandler.ReceiveMessage(packet);
                break;

            case Packet.ServerPackets.HmacKey:
                ClientReceiveHandler.ReceiveHmacKey(packet);
                break;

            case Packet.ServerPackets.AuthToken:
                ClientReceiveHandler.ReceiveToken(packet);
                break;

            case Packet.ServerPackets.MessageAll:
                ClientReceiveHandler.ReceiveMessageAll(packet);
                break;

            case Packet.ServerPackets.TransformUpdate:
                ClientReceiveHandler.TransformUpdate(packet);
                break;

            case Packet.ServerPackets.AnimationUpdate:
                ClientReceiveHandler.AnimationUpdate(packet);
                break;

            case Packet.ServerPackets.BandwidthReply:
                ClientReceiveHandler.BandwidthReply(packet);
                break;

            case Packet.ServerPackets.ThroughputReply:
                ClientReceiveHandler.ThroughputReply(packet);
                break;

            default:
                Debug.LogWarning($"Unhandled packet type: {packetType}");
                break;
        }
    }
    /// <summary>
    /// Starts the connection process to the server.
    /// </summary>
    public void ConnectToServer()
    {
        SetClient();
        tcp.Connect();
    }
    /// <summary>
    /// Handles TCP connection.
    /// </summary>
    public class TCP 
    {
        public TcpClient Socket; 
        private Stream Stream; 
        private byte[] receiveBuffer; 
        private Packet receivedPacket;
        /// <summary>
        /// Establishes a TCP connection to the server and begins receiving data.
        /// </summary>
        public void Connect() 
        {
            if(Client.instance.GetUsername() == "")
            {
                System.Random rng = new System.Random();
                int rand = rng.Next(1000); 
                Client.instance.SetUsername("Guest" + rand.ToString());
            }
            Socket = new TcpClient 
            {
                ReceiveBufferSize = DefaultBufferSize,
                SendBufferSize = DefaultBufferSize
            };
            receiveBuffer = new byte[DefaultBufferSize]; 
            Socket.BeginConnect(Client.instance.defaultNetworkSettings.ipAddress, Client.instance.defaultNetworkSettings.port, ConnectCallback, Socket); // zacne asynchronne pripojenie k serveru, ak uspesne da callback
        }
        /// <summary>
        /// Sends a packet to the server using TCP.
        /// </summary>
        public void Send(Packet Packet)
        {
            try
            {
                if (Socket != null)
                {
                    Stream.BeginWrite(Packet.ConvertToArray(), 0, Packet.DataLength(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.LogError($"Error sending data to server via TCP: {_ex}");
            }
        }
        /// <summary>
        /// Disconnects the TCP connection and clears the buffers.
        /// </summary>
        public void Disconnect()
        {
            Stream = null;
            receivedPacket = null;
            receiveBuffer = null;
            Socket = null;
        }
        /// <summary>
        /// Validates the server's SSL/TLS certificate. Returns true if the certificate is valid; else writes error and returns false.
        /// </summary>
        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true; 
            }
            Debug.LogError($"SSL/TLS validation error: {sslPolicyErrors}");
            return false; 
        }
        /// <summary>
        /// Handles received raw byte data from the server.
        /// </summary>
        private bool Handler(byte[] data)
        {
            int packetLength = 0;
            receivedPacket.AddBytesToBuffers(data);
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
                PacketAction(packetId, packet); 
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
        /// Callback triggered after attempting to connect to the server by TCP.
        /// Initializes TLS if enabled, sets up the stream, and begins reading data.
        /// </summary>
        private void ConnectCallback(IAsyncResult result) 
        {
            try
            {
                Socket.EndConnect(result); 
                if (!Socket.Connected)
                {
                    return;
                }
                if (Client.instance.defaultNetworkSettings.useTLS == true)
                {
                    SslStream ScStream = new SslStream(Socket.GetStream(), false, ValidateServerCertificate);
                    ScStream.AuthenticateAsClient(Client.instance.defaultNetworkSettings.ipAddress); 

                    receiveBuffer = new byte[Client.DefaultBufferSize];
                    receivedPacket = new Packet();
                    Stream = ScStream;
                } else
                {
                    Stream = Socket.GetStream();
                    receiveBuffer = new byte[Client.DefaultBufferSize];
                    receivedPacket = new Packet();
                }
                Stream.BeginRead(receiveBuffer, 0, Client.DefaultBufferSize, ReceiveCallback, null); 
                Client.instance.isConnected = true;
                ThreadsController.StartOnMainThread(() =>
                {
                    Client.instance.OnConnected?.Invoke();
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during SSL/TLS handshake: {ex.Message}");
                ThreadsController.StartOnMainThread(() =>
                {
                    Client.instance.OnConnectionFailed?.Invoke($"Error during SSL/TLS handshake: {ex.Message}");
                });
                Disconnect();
            }
        }
        /// <summary>
        /// Callback for receiving data over the TCP.
        /// Reads data into the data buffer, manages it by Handler and continues reading.
        /// </summary>
        private void ReceiveCallback(IAsyncResult result) 
        {
            try
            {
                int dataLength = Stream.EndRead(result); 
                if (dataLength <= 0) 
                {
                    instance.Disconnect();
                    return;
                }
                byte[] data = new byte[dataLength];
                Array.Copy(receiveBuffer, data, dataLength); 
                receivedPacket.ClearPacket(Handler(data));
                Stream.BeginRead(receiveBuffer, 0, DefaultBufferSize, ReceiveCallback, null);
            }
            catch (Exception ex) {
                Debug.LogError($"Error connecting {ex}");
            }
        }
    }
    /// <summary>
    /// Handles UDP connection and communication for the client.
    /// </summary>
    public class UDP
    {
        public UdpClient Socket;
        public IPEndPoint EndPoint;
        public UDP()
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(instance.defaultNetworkSettings.ipAddress), instance.defaultNetworkSettings.port);
        }
        /// <summary>
        /// Initializes and connects the UDP socket to the server.
        /// </summary>
        public void Connect(int Port)
        {
            Socket = new UdpClient(Port);
            Socket.Connect(EndPoint);
            Socket.BeginReceive(UdpReceiveCallback, null);
            Packet SendPacket = new Packet();
            SendUdpData(SendPacket);
        }
        /// <summary>
        /// Handles incoming UDP data and calls PacketAction() on the main thread.
        /// </summary>
        private void UdpDataHandler(byte[] Data)
        {
            Packet ReceivedPacket = new Packet(Data);
            int DataLength = ReceivedPacket.ReadInt();
            Data = ReceivedPacket.ReadBytes(DataLength);
            ThreadsController.StartOnMainThread(() => {
                Packet nPacket = new Packet(Data);
                int PId = nPacket.ReadInt();
                PacketAction(PId, nPacket);
            }) ;
        }
        /// <summary>
        /// Sends UDP data packet to the server.
        /// </summary>
        public void SendUdpData(Packet SendPacket)
        {
            try
            {
                if (Socket == null || !Client.instance.isConnected) return;

                SendPacket.AddIntAtStart(instance.id);
                if(Socket != null)
                {
                    Socket.BeginSend(SendPacket.ConvertToArray(), SendPacket.DataLength(), null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending UDP data {ex}");
            }
        }
        /// <summary>
        /// Disconnects the UDP client from the server.
        /// </summary>
        public void Disconnect()
        {
            instance.Disconnect();
            EndPoint = null;
            Socket = null;
        }
        /// <summary>
        /// Callback for receiving data over the UDP.
        /// Reads data into the data buffer, manages it by Handler and continues reading.
        /// </summary>
        private void UdpReceiveCallback(IAsyncResult Result)
        {
            try
            {
                byte[] data = Socket.EndReceive(Result, ref EndPoint);
                Socket.BeginReceive(UdpReceiveCallback, null);
                if(data.Length < 4)
                {
                    instance.Disconnect();
                    return;
                }
                UdpDataHandler(data);
            } catch
            {
                Disconnect();
            }
        }
    }
    /// <summary>
    /// Disconnects the client from the server.
    /// </summary>
    public void Disconnect()
    {
        if (isConnected == true)
        {
            isConnected = false;
            tcp.Socket.Close();
            udp.Socket.Close();
            ThreadsController.StartOnMainThread(() =>
            {
                ResetNetwork();
            });
            instance.OnDisconnected?.Invoke();
        }
    }
    /// <summary>
    /// Resets client network components and reloads the initial scene.
    /// </summary>
    public void ResetNetwork()
    {
        Disconnect();
        if (latencyMeassure != null)
        {
            StopCoroutine(latencyMeassure);
            latencyMeassure = null;
        }
        SetClient();
        /// <summary>
        /// This can be changed.
        /// </summary>
        SceneManager.LoadScene(0);
    }
    /// <summary>
    /// Sends a ping measurement request to the server.
    /// </summary>
    public void MeassureRequest(int pcktId)
    {
        if(isConnected == true)
        {
            ClientSendHandler.MeassureRequest(pcktId);
        } 
    }
    /// <summary>
    /// Handles the server’s ping reply and triggers the ping event.
    /// </summary>
    public void MeassureAnswer(int pcktId)
    {
        instance.OnPingReply?.Invoke(pcktId);
    }
    /// <summary>
    /// Sends a disconnect request to the server.
    /// </summary>
    public void DisconnectRequested()
    {
        if(isConnected == true)
        {
            ClientSendHandler.DisconnectRequest();
        }
    }
    /// <summary>
    /// Disconnects another user with the given ID from the scene.
    /// </summary>
    public void DisconnectOther(int clientid)
    {
        NetworkManager.networkManager.DisconnectForeignUser(clientid);
    }
    /// <summary>
    /// Sends a message to a specific user.
    /// </summary>
    public void MessageToUserRequest(string message, int clientId)
    {
        if(isConnected == true)
        {
            ClientSendHandler.MessageToUserRequest(message, clientId);
        }
    }
    /// <summary>
    /// Sends a message to all users in the session.
    /// </summary>
    public void MessageToAllRequest(string message)
    {
        if(isConnected == true)
        {
            ClientSendHandler.MessageToAllRequest(message);
        }
    }
    /// <summary>
    /// Spawns the user prefab locally or remotely.
    /// </summary>
    public void SpawnUserPrefab(int id, string username)
    {
        if (id == instance.GetId())
        {
            NetworkManager.networkManager.SpawnUser(id,username,true);
        } else
        {
            NetworkManager.networkManager.SpawnUser(id, username, false);
        }
    }
    /// <summary>
    /// Triggers message-received event with the given message.
    /// </summary>
    public void ReceivedMessage(string message)
    {
        instance.OnMessageReceived?.Invoke(message);
    }
    /// <summary>
    /// Sends current transform of a game object to the server.
    /// </summary>
    public void UpdateTransform(Transform objectTransform)
    {
        ClientSendHandler.SendTransform(instance.GetId(), objectTransform);
    }
    /// <summary>
    /// Updates the transform of a remote player.
    /// </summary>
    public void ChangeTransform(int id, Vector3 pos, Quaternion rot)
    {
        instance.OnTransformReceived?.Invoke(id, pos, rot);
    }
    /// <summary>
    /// Sends animation update to the server.
    /// </summary>
    public void AnimationUpdate(int type, string name, float value)
    {
        ClientSendHandler.SendAnimationUpdate(type, name, value);
    }
    /// <summary>
    /// Applies received animation state from the server.
    /// </summary>
    public void ChangeAnimatorState(int id, int type, string name, float value)
    {
        instance.OnAnimationChangeReceived?.Invoke(id, type, name, value);
    }
    /// <summary>
    /// Sends request to measure bandwidth to the server.
    /// </summary>
    public void BandwidthRequest(byte[] data)
    {
        ClientSendHandler.SendBandwidthRequest(data);
    }
    /// <summary>
    /// Sends a packet of data to test throughput.
    /// </summary>
    public void SendThroughputData(int pcktId, byte[] data)
    {
        ClientSendHandler.SendThroughputRequest(pcktId, data);
    }
    /// <summary>
    /// Called when bandwidth reply is received from the server.
    /// </summary>
    public void ReceivedBandwidth()
    {
        instance.OnBandwidthReply?.Invoke();
    }
    /// <summary>
    /// Called when throughput test result is received.
    /// </summary>
    public void ReceivedThroughput(int pcktId, int dataSize)
    {
        instance.OnThroughputReply?.Invoke(pcktId, dataSize);
    }
}
