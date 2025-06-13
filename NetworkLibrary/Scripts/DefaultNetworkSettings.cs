using UnityEngine;

[CreateAssetMenu(fileName = "DefaultNetworkSettings", menuName = "NetworkLibrary/DefaultSettings")]
public class DefaultNetworkSettings : ScriptableObject
{
    [Header("General settings")]
    public string ipAddress = "127.0.0.1";
    public int port = 7777;
    public bool allowUDP = true;
    [Header("Security options")]
    public bool useTLS = true;
    public string certificatePath = "";         
    public string certificatePassword = "";      
    public bool useHMAC = true;
    public bool useAuthentificationTokens = true;
    [Header("Settings for tunneling")]
    public bool useTunneling = false;
    public int TunnelPort = 7777;
}
