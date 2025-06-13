using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages network-related operations on the client side, including connection handling, user synchronization,
/// transform and animation updates, and messaging. It is part of EDA architecture of the library.
/// </summary>

public class NetworkManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the NetworkManager.
    /// </summary>
    public static NetworkManager networkManager;
    /// <summary>
    /// List of all currently active (connected) user GameObjects in the scene.
    /// </summary>
    public List<GameObject> onlineUsers;
    /// <summary>
    /// Configuration settings for the client, including user prefab and other preferences.
    /// </summary>
    [SerializeField] ClientSettings ClientSettings;
    /// <summary>
    /// Predefined spawn points used for placing newly connected users in the scene.
    /// </summary>
    public Transform[] spawnPoints;
    /// <summary>
    /// Initializes the singleton instance of the NetworkManager.
    /// </summary>

    private void Awake() 
    {
        if (networkManager == null)
        {
            networkManager = this;
        }
        else if (networkManager != this)
        {
            Destroy(this);
        }
    }
    /// <summary>
    /// Subscribes to client events for handling network state changes and data reception, can be easily extended of new events.
    /// </summary>
    void Start()
    {
        Client.instance.OnConnected += HandleConnected;
        Client.instance.OnConnectionFailed += HandleConnectionFailed;
        Client.instance.OnDisconnected += HandleDisconnected;
        Client.instance.OnMessageReceived += HandleMessage;
        Client.instance.OnTransformReceived += HandleTransformChange;
        Client.instance.OnAnimationChangeReceived += HandleAnimationChange;
    }
    /// <summary>
    /// Initiates a connection to the server.
    /// </summary>
    public void ConnectToServer()
    {
        Client.instance.ConnectToServer();
    }
    /// <summary>
    /// Sends a request to disconnect the client from the server.
    /// </summary>
    public void DisconnectFromServer()
    {
        Client.instance.DisconnectRequested();
    }
    /// <summary>
    /// Callback invoked when the client successfully connects to the server.
    /// </summary>
    private void HandleConnected()
    {
        //
    }
    /// <summary>
    /// Callback invoked when the connection to the server fails.
    /// </summary>
    private void HandleConnectionFailed(string reason)
    {
        Debug.LogError("NETWORKMANAGER: Connection failed: " + reason);
        // 
    }
    /// <summary>
    /// Callback for processing a received message from the server.
    /// </summary>
    private void HandleMessage(string message)
    {
        // 
    }
    /// <summary>
    /// Callback invoked when the client is disconnected from the server.
    /// Removes all user objects from the scene.
    /// </summary>
    private void HandleDisconnected()
    {
        Debug.Log("NETWORKMANAGER: Disconnected from server.");
        foreach (GameObject user in onlineUsers)
        {
            GameObject UserObject = user;
            onlineUsers.Remove(user);
            Destroy(UserObject);
        }
    }
    /// <summary>
    /// Applies a received position and rotation update to the corresponding remote user.
    /// </summary>
    private void HandleTransformChange(int id, Vector3 pos, Quaternion rot)
    {
        foreach(GameObject userObject in onlineUsers)
        {
            if(userObject.GetComponent<NetworkUser>().GetId() == id)
            {
                NetworkTransform nt = userObject.GetComponent<NetworkTransform>();
                if (nt != null && !nt.UserInfo.isLocalUser)
                {
                    nt.ApplyNetworkTransform(pos, rot);
                }
            }
        }
    }
    /// <summary>
    /// Applies a received animation parameter change to the corresponding remote user.
    /// </summary>
    private void HandleAnimationChange(int id, int type, string name, float value)
    {
        foreach (GameObject userObject in onlineUsers)
        {
            if (userObject.GetComponent<NetworkUser>().GetId() == id)
            {
                NetworkAnimator nt = userObject.GetComponent<NetworkAnimator>();
                if (nt != null && !nt.UserInfo.isLocalUser)
                {
                    nt.ApplyRemoteParameter(type, name, value);
                }
            }
        }
    }
    /// <summary>
    /// Adds a newly spawned user object to the list of online users.
    /// </summary>
    public void AddUser(GameObject userPrefab)
    {
        onlineUsers.Add(userPrefab);
    }
    /// <summary>
    /// Retrieves a user GameObject by username.
    /// </summary>
    public GameObject GetUserByName(string username)
    {
        foreach (GameObject user in onlineUsers)
        {
            if (user.GetComponent<NetworkUser>().GetUsername() == username)
            {
                return user;
            }
        }
        return null;
    }
    /// <summary>
    /// Retrieves a user GameObject by user ID.
    /// </summary>
    public GameObject GetUserById(int id)
    {
        foreach (GameObject user in onlineUsers)
        {
            if (user.GetComponent<NetworkUser>().GetId() == id)
            {
                return user;
            }
        }
        return null;
    }
    /// <summary>
    /// Instantiates and initializes a new user object in the scene.
    /// </summary>
    public void SpawnUser(int id, string userName, bool isLocal)
    {
        GameObject UserPrefab = Instantiate(ClientSettings.userPrefab[0], spawnPoints[0]);
        UserPrefab.GetComponent<NetworkUser>().SetUser(id, userName, isLocal);
        networkManager.AddUser(UserPrefab);
    }
    /// <summary>
    /// Removes a remote user object from the scene based on its ID.
    /// </summary>
    public void DisconnectForeignUser(int id)
    {
        foreach (GameObject user in onlineUsers)
        {
            if (user.GetComponent<NetworkUser>().GetId() == id)
            {
                GameObject UserObject = user;
                onlineUsers.Remove(user);
                Destroy(UserObject);
                break;
            }
        }
    }
    /// <summary>
    /// Sends a text message to either all users or a specific user.
    /// </summary>
    public void SendTextMessage(string message, int mode, int id)
    {
        if(mode == 1) { 
            Client.instance.MessageToAllRequest(message);
        } else
        {
            Client.instance.MessageToUserRequest(message, id);
        }
    }
    /// <summary>
    /// Sends a test broadcast message to the server.
    /// </summary>
    public void sendTestMessage()
    {
        SendTextMessage("Test message", 1, 0);
    }
    /// <summary>
    /// Sends the updated transform of the local user to the server.
    /// </summary>
    public void UpdateTransform(Transform objectTransform)
    {
        Client.instance.UpdateTransform(objectTransform);
    }
    /// <summary>
    /// Sends an animation parameter change to the server for synchronization.
    /// </summary>
    public void SendAnimatorParameter(int type, string name, float value)
    {
        Client.instance.AnimationUpdate(type,name,value);
    }
}
