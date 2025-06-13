using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component for GameObject representing client in server-scene, used only for testing id Unity
/// </summary>
public class ServerSideClientInstance : MonoBehaviour
{
    public int userID;
    public string userName;
    private int actionId = 0;
    public void UserInitialization(int Id, string Name, int actId)
    {
        userID = Id;
        userName = Name;
        actionId = actId;
    }
}
