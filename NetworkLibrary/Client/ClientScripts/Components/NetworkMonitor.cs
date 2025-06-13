using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// NetworkMonitor is responsible for actively monitoring network quality metrics such as latency, jitter,
/// packet loss, bandwidth, and throughput. It provides real-time updates to the UI.
/// </summary>
public class NetworkMonitor : MonoBehaviour
{
    /// <summary>
    /// Interval in seconds between pings sent to measure latency.
    /// </summary>
    [SerializeField] private float pingInterval = 1f;
    /// <summary>
    /// Timeout in seconds after which a ping is considered lost.
    /// </summary>
    [SerializeField] private float pingTimeout = 5f;
    /// <summary>
    /// UI Text element displaying packet loss, average latency, and jitter.
    /// </summary>
    [SerializeField] private Text MonitorText1;
    /// <summary>
    /// UI Text element displaying bandwidth test result.
    /// </summary>
    [SerializeField] private Text MonitorText2;
    /// <summary>
    /// UI Text element displaying throughput test result.
    /// </summary>
    [SerializeField] private Text MonitorText3;
    /// <summary>
    /// Total number of latency packets sent.
    /// </summary>
    private int sentPackets = 0;
    /// <summary>
    /// Total number of latency responses received.
    /// </summary>
    private int receivedResponses = 0;
    /// <summary>
    /// Number of latency packets sent during the current interval.
    /// </summary>
    private int sentPacketsInterval = 0;
    /// <summary>
    /// Number of responses received during the current interval.
    /// </summary>
    private int receivedResponsesInterval = 0;
    /// <summary>
    /// Set of IDs for which responses are still pending.
    /// </summary>
    private HashSet<int> awaitingResponses = new();
    /// <summary>
    /// Dictionary storing the send time for each packet ID.
    /// </summary>
    private Dictionary<int, float> sentTimes = new();
    /// <summary>
    /// List of recent round-trip time (RTT) samples for calculating average latency and jitter.
    /// </summary>
    private List<float> latencySamples = new();
    /// <summary>
    /// Last calculated packet loss percentage.
    /// </summary>
    private float lastLossPercent = 0f;
    /// <summary>
    /// Last calculated jitter value.
    /// </summary>
    private float lastJitter = 0f;
    /// <summary>
    /// Timestamp when the bandwidth test started.
    /// </summary>
    private float bandwidthClientStartTime;
    /// <summary>
    /// Size in bytes of the bandwidth test data.
    /// </summary>
    private int BandwidthDataSize;
    /// <summary>
    /// Coroutine reference for the ping routine.
    /// </summary>
    private Coroutine pingCoroutine;
    /// <summary>
    /// Coroutine reference for the throughput timeout logic.
    /// </summary>
    private Coroutine throughputTimeoutRoutine;

    /*
    private string pingLog = Application.dataPath + "/ping_log.txt";
    private string jitterLog = Application.dataPath + "/jitter_log.txt";
    private string packetLossLog = Application.dataPath + "/packet_loss_log.txt";
    private string bandwidthLog = Application.dataPath + "/bandwidth_log.txt";
    private string throughputLog = Application.dataPath + "/throughput_log.txt"; */
    /// <summary>
    /// Subscribes to client events and starts monitoring after connection.
    /// </summary>
    private void Start()
    {
        Client.instance.OnPingReply += OnPingResponse;
        Client.instance.OnConnected += StartMonitor;
        Client.instance.OnBandwidthReply += OnBandwidthResponse;
        Client.instance.OnThroughputReply += OnThroughputResposne;
        Client.instance.OnDisconnected += StopMonitoring;
    }
    /// <summary>
    /// Starts the latency monitor coroutine.
    /// </summary>
    private void StartMonitor()
    {
        pingCoroutine = StartCoroutine(PingCoroutine());
    }
    /// <summary>
    /// Stops all monitoring coroutines and cleanups.
    /// </summary>
    private void StopMonitoring()
    {
        StopCoroutine(pingCoroutine);
        if(throughputTimeoutRoutine != null)
        StopCoroutine(throughputTimeoutRoutine);
    }
    /// <summary>
    /// Coroutine that repeatedly sends latency packets and calculate packet loss.
    /// </summary>
    private IEnumerator PingCoroutine()
    {
        while (true)
        {
            CleanOldPings();
            SendPing();
            yield return new WaitForSeconds(pingInterval);
            CalculateLoss();
        }
    }
    /// <summary>
    /// Sends a latency packet and tracks its send time.
    /// </summary>
    private void SendPing()
    {
        int id = sentPackets++;
        float timeSent = Time.time;
        awaitingResponses.Add(id);
        sentTimes[id] = timeSent;
        sentPacketsInterval++;
        Client.instance.MeassureRequest(id);
    }
    /// <summary>
    /// Handles a received ping response, calculates latency and jitter in ms.
    /// </summary>
    public void OnPingResponse(int id)
    {
        if (awaitingResponses.Contains(id) && sentTimes.ContainsKey(id))
        {
            float sentTime = sentTimes[id];
            float rtt = (Time.time - sentTime) * 1000f; 
            latencySamples.Add(rtt);
            if (latencySamples.Count > 10)
                latencySamples.RemoveAt(0);
            receivedResponses++;
            receivedResponsesInterval++;
            awaitingResponses.Remove(id);
            sentTimes.Remove(id);
            lastJitter = CalculateJitter();
        }
    }
    /// <summary>
    /// Calculates packet loss percentage and updates the UI.
    /// </summary>
    private void CalculateLoss()
    {
        int lost = sentPacketsInterval - receivedResponsesInterval;
        if (sentPacketsInterval > 0) {
            lastLossPercent = (float)lost / sentPacketsInterval * 100f;
        } else {
            lastLossPercent = 0f;
        }

        float avgRtt;
        if (latencySamples.Count > 0) {
            avgRtt = Average(latencySamples);
        } else {
            avgRtt = 0f;
        }
        MonitorText1.text = ($"[NetworkMonitor] Packet Loss: {lastLossPercent:F2}% | Avg RTT: {avgRtt:F2} ms | Jitter: {lastJitter:F2} ms");
        sentPacketsInterval = 0;
        receivedResponsesInterval = 0;

        /*
        File.AppendAllText(pingLog, avgRtt.ToString() + "\n");
        File.AppendAllText(jitterLog, lastJitter.ToString() + "\n");
        File.AppendAllText(packetLossLog, lastLossPercent.ToString() + "\n"); */
    }
    /// <summary>
    /// Removes pings that have exceeded the timeout.
    /// </summary>
    private void CleanOldPings()
    {
        float now = Time.time;
        List<int> expired = new();
        foreach (var sent in sentTimes) {
            if (now - sent.Value > pingTimeout)
                expired.Add(sent.Key);
        }
        foreach (int id in expired) {
            awaitingResponses.Remove(id);
            sentTimes.Remove(id);
        }
    }
    /// <summary>
    /// Calculates the average of values in float list.
    /// </summary>
    private float Average(List<float> list)
    {
        float sum = 0f;
        foreach (float entry in list)
            sum += entry;
        return sum / list.Count;
    }
    /// <summary>
    /// Calculates jitter based on differences in RTT samples.
    /// </summary>
    private float CalculateJitter()
    {
        if (latencySamples.Count < 2) return 0f;

        float totalDiff = 0f;
        for (int i = 1; i < latencySamples.Count; i++)
            totalDiff += Mathf.Abs(latencySamples[i] - latencySamples[i - 1]);
        return totalDiff / (latencySamples.Count - 1);
    }
    /// <summary>
    /// Initiates a bandwidth test by sending a large byte array.
    /// </summary>
    public void StartBandwidthTest()
    {
        int sizeKB = 512;
        byte[] data = new byte[sizeKB * 1024];
        new System.Random().NextBytes(data);
        bandwidthClientStartTime = Time.realtimeSinceStartup;
        BandwidthDataSize = data.Length;
        Client.instance.BandwidthRequest(data);
    }
    /// <summary>
    /// Called when the server replies to a bandwidth test; calculates and displays bandwidth.
    /// </summary>
    private void OnBandwidthResponse()
    {
        float TimeReply = Time.realtimeSinceStartup;
        float duration = TimeReply - bandwidthClientStartTime;
        float bandwidthMbps = (BandwidthDataSize * 8f) / duration / 1_000_000f;
        MonitorText2.text = ($"[BandwidthTest] {bandwidthMbps:F2} Mbps");
       // File.AppendAllText(bandwidthLog, bandwidthMbps.ToString() + "\n");
    }
    private float ThroughputStartTime;
    private int ThroughputSentSize = 0;
    private int ThroughputReceivedSize = 0;
    private bool isThroughputRunning = false;
    private int ThroughputPacketsReceived = 0;
    /// <summary>
    /// Starts a throughput test by sending 100 UDP packets.
    /// </summary>
    public void StartThroughputTest()
    {
        if (isThroughputRunning)
        {
            Debug.LogWarning("[ThroughputTester] Test already running.");
            return;
        }
        ThroughputPacketsReceived = 0;
        ThroughputSentSize = 0;
        ThroughputReceivedSize = 0;
        isThroughputRunning = true;
        ThroughputStartTime = Time.realtimeSinceStartup;
        throughputTimeoutRoutine = StartCoroutine(stopThroughput());
        for(int i = 1; i<=100; i++)
        { 
            if(!isThroughputRunning)
            {
                ThroughputCalculate();
                break;     
            }
            byte[] data = new byte[1024];
            new System.Random().NextBytes(data);
            ThroughputSentSize += data.Length;
            Client.instance.SendThroughputData(i, data);
        }
    }
    /// <summary>
    /// Called when a response for a throughput packet is received.
    /// </summary>
    public void OnThroughputResposne(int pcktId, int DataSize)
    {
        ThroughputPacketsReceived++;
        ThroughputReceivedSize += DataSize;
        if(ThroughputPacketsReceived == 100)
        {
            isThroughputRunning = false;
            StopCoroutine(throughputTimeoutRoutine);
            ThroughputCalculate();
        }
    }
    /// <summary>
    /// Coroutine that stops the throughput test after a fixed time - 5s if not completed.
    /// </summary>
    private IEnumerator stopThroughput()
    {
        yield return new WaitForSeconds(5);
        isThroughputRunning = false;
        ThroughputCalculate();

    }
    /// <summary>
    /// Finalizes throughput test and updates the UI with results.
    /// </summary>
    private void ThroughputCalculate()
    {
        StopCoroutine(throughputTimeoutRoutine);
        float duration = Time.realtimeSinceStartup - ThroughputStartTime;
        float throughputMbps = (ThroughputReceivedSize * 8f) / duration / 1_000_000f;
        MonitorText3.text = ($"[ThroughputTest] {throughputMbps:F2} Mbps, received {ThroughputPacketsReceived} / 100 packets with size 1024B");
       /* File.AppendAllText(throughputLog, throughputMbps.ToString() + "\n");
        File.AppendAllText("throughput.csv", $"{throughputMbps:F2};{ThroughputReceivedSize};{duration:F3}\n"); */
    }
    public float GetLossPercent()
    {
        return lastLossPercent;
    }
    public float GetAverageRTT()
    {
        float avgRtt;
        if (latencySamples.Count > 0) {
            avgRtt = Average(latencySamples);
        } else {
            avgRtt = 0f;
        }
        return avgRtt;
    }
    public float GetJitter() {
        return lastJitter ;
    }
}
