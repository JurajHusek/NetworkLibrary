using UnityEngine;

[CreateAssetMenu(fileName = "ServerSettings", menuName = "NetworkLibrary/Server/Settings")]
public class ServerSettings : ScriptableObject
{
    [Header("General Server settings")]
    public int maxCCU = 10;
    [Header("Additional Server settings")]
    public GameObject serverSideUserPrefab;
    public bool sendWelcomeToServerMesage = true;
    public string welcomeToServerMessage = "Welcome to server!";
}
