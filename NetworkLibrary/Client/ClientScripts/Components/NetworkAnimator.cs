using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Synchronizes animator parameters (bools, floats, triggers) over the network.
/// Sends changes from the local user to the server and applies received parameters on remote clients.
/// </summary>
public class NetworkAnimator : MonoBehaviour
{
    /// <summary>
    /// Information about the network user used to check local or remote status.
    /// </summary>
    public NetworkUser UserInfo;
    /// <summary>
    /// Reference to the Animator component used to control animations.
    /// </summary>
    [SerializeField] private Animator animator;
    /// <summary>
    /// Tracks last known values of boolean parameters to detect changes.
    /// </summary>
    private Dictionary<string, bool> boolStates = new Dictionary<string, bool>();
    /// <summary>
    /// Tracks last known values of float parameters to detect changes.
    /// </summary>
    private Dictionary<string, float> floatStates = new Dictionary<string, float>();
    /// <summary>
    /// Initializes the animator reference and caches all initial animator parameter states.
    /// </summary>
    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        CacheInitialStates();
    }
    /// <summary>
    /// Checks for any changed animator parameters if the object is controlled by the local user.
    /// Sends updated parameters to the server.
    /// </summary>
    void Update()
    {
        if (!UserInfo.isLocalUser) return;

        CheckAndSendBoolChanges();
        CheckAndSendFloatChanges();
    }
    /// <summary>
    /// Stores initial states of all bool and float animator parameters for change tracking.
    /// </summary>
    private void CacheInitialStates()
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            switch (param.type) {
                case AnimatorControllerParameterType.Bool:
                    boolStates[param.name] = animator.GetBool(param.name);
                    break;
                case AnimatorControllerParameterType.Float:
                    floatStates[param.name] = animator.GetFloat(param.name);
                    break;
            }
        }
    }
    /// <summary>
    /// Detects and sends changed boolean parameters to the server.
    /// </summary>
    private void CheckAndSendBoolChanges()
    {
        List<string> changedKeys = new List<string>();
    
        foreach (var entry in boolStates) {
            bool current = animator.GetBool(entry.Key);
            if (current != entry.Value) {
                changedKeys.Add(entry.Key); 
            }
        }

        foreach (string key in changedKeys) {
        bool newValue = animator.GetBool(key);
        boolStates[key] = newValue;
        NetworkManager.networkManager.SendAnimatorParameter(0, key, newValue ? 1f : 0f);
        }
    }

    /// <summary>
    /// Detects and sends changed float parameters to the server.
    /// </summary>
    private void CheckAndSendFloatChanges()
    {
        List<string> changedKeys = new List<string>();

        foreach (var kvp in floatStates)
        {
            float current = animator.GetFloat(kvp.Key);
            if (Mathf.Abs(current - kvp.Value) > 0.01f)
            {
                changedKeys.Add(kvp.Key);
            }
        }

        foreach (string key in changedKeys)
        {
            float newValue = animator.GetFloat(key);
            floatStates[key] = newValue;
            NetworkManager.networkManager.SendAnimatorParameter(1, key, newValue);
        }
    }
    /// <summary>
    /// Triggers an animation parameter both locally and across the network if controlled by local user.
    /// </summary>
    public void Trigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
        if (UserInfo.isLocalUser)
        {
            NetworkManager.networkManager.SendAnimatorParameter(2, triggerName, 0f);
        }
    }
    /// <summary>
    /// Applies an animation parameter update received from a remote user.
    /// </summary>
    public void ApplyRemoteParameter(int type, string name, float value)
    {
        switch (type)
        {
            case 0: animator.SetBool(name, value > 0.5f); break;
            case 1: animator.SetFloat(name, value); break;
            case 2: animator.SetTrigger(name); break;
        }
    }
}
