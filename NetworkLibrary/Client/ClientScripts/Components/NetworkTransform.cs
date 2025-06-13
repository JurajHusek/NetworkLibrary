using UnityEngine;
/// <summary>
/// Handles synchronization of position and rotation over the network.
/// For local users sends updates in time interval, for remote users interpolates transform.
/// </summary>
public class NetworkTransform : MonoBehaviour
{
    /// <summary>
    /// Time interval between transform updates for local users.
    /// </summary>
    public float sendRate = 0.05f;
    /// <summary>
    /// Timer to track when the next transform should be sent.
    /// </summary>
    private float sendTimer = 0f;
    /// <summary>
    /// Target position to interpolate to (for remote users).
    /// </summary>
    private Vector3 targetPosition;
    /// <summary>
    /// Target rotation to interpolate to (for remote users).
    /// </summary>
    private Quaternion targetRotation;
    /// <summary>
    /// UserInfo class used for checking if the object belongs to the local user or another player.
    /// </summary>
    public NetworkUser UserInfo;
    /// <summary>
    /// Initializes the transform targets and adjusts send rate based on UDP setting.
    /// </summary>
    private void Start()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
        if(Client.instance.GetDefaultSettings().allowUDP == false)
        {
            sendRate = 1f;
        }
    }
    /// <summary>
    /// For local users, sends transform updates periodically.
    /// For remote users, smoothly interpolates transform to the target.
    /// </summary>
    private void Update()
    {
        if (UserInfo.isLocalUser)
        {
            sendTimer += Time.deltaTime;
            if (sendTimer >= sendRate)
            {
                NetworkManager.networkManager.UpdateTransform(this.transform);
                sendTimer = 0f;
            }
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
    /// <summary>
    /// Applies the received position and rotation from the network to be used for interpolation.
    /// </summary>
    public void ApplyNetworkTransform(Vector3 pos, Quaternion rot)
    {
        targetPosition = pos;
        targetRotation = rot;
    }
}