using UnityEngine;

[CreateAssetMenu(fileName = "ClientSettings", menuName = "NetworkLibrary/Client/ClientSettings")]
public class ClientSettings : ScriptableObject
{
    [Header("General settings")]
    public GameObject[] userPrefab;
}
