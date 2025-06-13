using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that initializes and manages the network server logic in a Unity scene.
/// </summary>
public class NetworkServer : MonoBehaviour
{
    [SerializeField] DefaultNetworkSettings defaultSettings;
    [SerializeField] ServerSettings serverSettings;
    public static NetworkServer instance;
    private GameObject userPrefab;
    private int ccu;
    private int port;
    /// <summary>
    /// Unity Awake method that ensures only one instance of the NetworkServer exists (Singleton pattern).
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }
    /// <summary>
    /// Initializes server settings, disables VSync, sets target frame rate,
    /// and starts the server with specified configuration.
    /// </summary>
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        userPrefab = serverSettings.serverSideUserPrefab;
        ccu = serverSettings.maxCCU;
        port = defaultSettings.port;
        ServerLogic.StartServer(ccu, port, defaultSettings, serverSettings);
    }
    /// <summary>
    /// Ensures the server is stopped when the application quits.
    /// </summary>
    private void OnApplicationQuit()
    {
        ServerLogic.StopServer();
    }
    /// <summary>
    /// Instantiates a new server-side user prefab and returns its ServerSideClientInstance component.
    /// </summary>
    public ServerSideClientInstance NewUser()
    {
        return Instantiate(userPrefab).GetComponent<ServerSideClientInstance>();
    }
}
