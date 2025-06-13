using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Chat demo class, manages UI.
/// </summary>
public class NetworkChatDemo : MonoBehaviour
{
    public Text sendText;
    public Transform container;
    public GameObject networkChatDemoEntry;

    private void Start()
    {
        Client.instance.OnMessageReceived += SpawnMessageEntry;
    }

    private void SpawnMessageEntry(string message)
    {
        GameObject newEntry = Instantiate(networkChatDemoEntry, container);
        newEntry.GetComponent<Text>().text = message;
    }
    public void SendMessageToAll()
    {
        string username = Client.instance.GetUsername();
        NetworkManager.networkManager.SendTextMessage(username + ": " + sendText.text, 1, Client.instance.GetId());
        GameObject newEntry = Instantiate(networkChatDemoEntry, container);
        newEntry.GetComponent<Text>().text = username + ": " + sendText.text;
    }
}
