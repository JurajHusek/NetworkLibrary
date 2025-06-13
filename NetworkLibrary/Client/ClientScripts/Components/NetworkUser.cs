using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main network user-prefab component, must be part of the user prefab.
/// </summary>
public class NetworkUser : MonoBehaviour
{
    private int id;
    private string username;
    public bool isLocalUser = false;

    public void SetUser(int newId, string newUsername, bool isLocal)
    {
        id = newId;
        username = newUsername;
        isLocalUser = isLocal;
    }
    public int GetId()
    {
        return id;
    }

    public string GetUsername()
    {
        return username;
    }
}
