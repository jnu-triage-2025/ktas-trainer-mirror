using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerInfrastructure.Session
{
  public class LanDiscoveryService : MonoBehaviour
  {
    public static LanDiscoveryService Instance { get; private set; }

    [Header("LAN Discovery")]
    [SerializeField] private int discoveryPort = 47777;
    [SerializeField] private int heartbeatIntervalMs = 1000;
    [SerializeField] private int entryTtlMs = 5000;

    private readonly object _lock = new object();
    private readonly Dictionary<string, SessionInformationModel> _sessions = new Dictionary<string, SessionInformationModel>();

    private CancellationTokenSource _broadcastCts;
    private CancellationTokenSource _listenCts;

    private bool _pendingUpdate;
    private List<SessionInformationModel> _lastSnapshot = new List<SessionInformationModel>();

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }
      Instance = this;
      DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
      bool removed = false;
      lock (_lock)
      {
        var now = DateTime.UtcNow;
        var toRemove = new List<string>();
        foreach (var kv in _sessions)
        {
          if ((now - kv.Value.LastSeenUtc).TotalMilliseconds > entryTtlMs)
            toRemove.Add(kv.Key);
        }
        foreach (var key in toRemove)
        {
          _sessions.Remove(key);
          removed = true;
        }
      }
      if (removed)
      {
        _pendingUpdate = true;
      }
    }

    public List<SessionInformationModel> GetDiscoveredSessions()
    {
      lock (_lock)
      {
        _lastSnapshot = new List<SessionInformationModel>(_sessions.Values);
        _pendingUpdate = false;
        return _lastSnapshot;
      }
    }

    public bool HasPendingUpdate() => _pendingUpdate;

    public void StartBroadcast(string sessionName, int gamePort)
    {
      StopBroadcast();
      _broadcastCts = new CancellationTokenSource();
      _ = BroadcastLoop(sessionName, gamePort, _broadcastCts.Token);
    }

    public void StopBroadcast()
    {
      _broadcastCts?.Cancel();
      _broadcastCts?.Dispose();
      _broadcastCts = null;
    }

    public void StartDiscovery()
    {
      StopDiscovery();
      ClearDiscovered();
      _listenCts = new CancellationTokenSource();
      _ = ListenLoop(_listenCts.Token);
    }

    public void StopDiscovery()
    {
      _listenCts?.Cancel();
      _listenCts?.Dispose();
      _listenCts = null;
    }

    public void ClearDiscovered()
    {
      lock (_lock)
      {
        _sessions.Clear();
        _pendingUpdate = true;
      }
    }

    private async Task BroadcastLoop(string sessionName, int gamePort, CancellationToken token)
    {
      using var client = new UdpClient();
      client.EnableBroadcast = true;
      client.MulticastLoopback = false;

      string addr = GetLocalIPv4() ?? "127.0.0.1";
      IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, discoveryPort);

      while (!token.IsCancellationRequested)
      {
        try
        {
          string payload = $"FISHNET_DISCOVERY|{sessionName}|{addr}|{gamePort}";
          byte[] data = Encoding.UTF8.GetBytes(payload);
          await client.SendAsync(data, data.Length, ep);
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[LanDiscovery] Broadcast error: {ex.Message}");
        }

        try
        {
          await Task.Delay(heartbeatIntervalMs, token);
        }
        catch (TaskCanceledException) { }
      }
    }

    private async Task ListenLoop(CancellationToken token)
    {
      using var client = new UdpClient();
      client.EnableBroadcast = true;
      client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      client.Client.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));

      // 취소 시 소켓을 닫아 대기 중인 ReceiveAsync 를 즉시 깨운다.
      // (취소 토큰 없이 대기하면 StopDiscovery 후에도 다음 패킷이 올 때까지
      //  루프가 살아 있고 포트가 해제되지 않는 버그가 발생한다.)
      using var registration = token.Register(() =>
      {
        try
        { client.Close(); }
        catch { /* ignore */ }
      });

      while (!token.IsCancellationRequested)
      {
        UdpReceiveResult result;
        try
        {
          result = await client.ReceiveAsync();
          if (token.IsCancellationRequested)
            break;
        }
        catch (OperationCanceledException)
        {
          break;
        }
        catch (ObjectDisposedException)
        {
          break; // 취소로 소켓이 닫힘.
        }
        catch (SocketException) when (token.IsCancellationRequested)
        {
          break; // 취소로 소켓이 닫힘.
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[LanDiscovery] Listen error: {ex.Message}");
          continue;
        }

        ParseAndStore(result.Buffer, result.RemoteEndPoint);
      }
    }

    private void ParseAndStore(byte[] data, IPEndPoint remote)
    {
      string msg;
      try
      {
        msg = Encoding.UTF8.GetString(data);
      }
      catch
      {
        return;
      }

      var parts = msg.Split('|');
      if (parts.Length != 4 || parts[0] != "FISHNET_DISCOVERY")
        return;

      string name = parts[1];
      string addr = parts[2];
      if (!ushort.TryParse(parts[3], out ushort port))
        return;

      string key = $"{addr}:{port}";
      lock (_lock)
      {
        if (!_sessions.TryGetValue(key, out var si))
        {
          si = new SessionInformationModel(addr, port, name, DateTime.UtcNow);
          _sessions[key] = si;
        }
        else
        {
          si.LastSeenUtc = DateTime.UtcNow;
          si.Name = name;
        }
        _pendingUpdate = true;
      }
    }

    private string GetLocalIPv4()
    {
      string fallback = null;
      foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
      {
        if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
          continue;
        var ipProps = ni.GetIPProperties();
        foreach (var ua in ipProps.UnicastAddresses)
        {
          if (ua.Address.AddressFamily == AddressFamily.InterNetwork &&
              !IPAddress.IsLoopback(ua.Address))
          {
            return ua.Address.ToString();
          }
          if (fallback == null && ua.Address.AddressFamily == AddressFamily.InterNetwork)
            fallback = ua.Address.ToString();
        }
      }
      return fallback;
    }

    private void OnDestroy()
    {
      StopBroadcast();
      StopDiscovery();
    }
  }
}
