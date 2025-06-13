using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Online list demo class, manages UI.
/// </summary>
public class NetworkOnlineList : MonoBehaviour
{
    public Transform listContent;
    public GameObject listEntry;
    public GameObject listCanvas;

    public void ShowList()
    {
        listCanvas.SetActive(true);
        foreach(Transform child in listContent)
        {
            Destroy(child.gameObject);
        }
        foreach(GameObject user in NetworkManager.networkManager.onlineUsers)
        {
            GameObject newEntry = Instantiate(listEntry, listContent);
            newEntry.GetComponent<Text>().text = user.GetComponent<NetworkUser>().GetUsername();
        }
    }

    public void HideList()
    {
        listCanvas.SetActive(false);
    }
}
