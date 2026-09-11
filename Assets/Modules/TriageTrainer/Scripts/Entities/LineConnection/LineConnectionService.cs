using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using TriageTrainer.Entity.AEDLine;
using TriageTrainer.Entity.CentralLine;
using TriageTrainer.Entity.ElectricalLine;
using TriageTrainer.Entity.IntravenousLine;
using TriageTrainer.Entity.OxyLine;
using TriageTrainer.Entity.SuctionLine;
using UnityEngine;

namespace TriageTrainer.Entity.LineConnection
{
  public class LineConnectionService : MonoBehaviour
  {
    [Serializable]
    private sealed class PendingConnectionContext
    {
      public PlayerController Player;
      public LineConnectionPoint StartPoint;
      public LineConnectionPoint EndPoint;
      public int RequestId;
      /// <summary>서버 응답을 기다리는 동안 미리 소비해 둔 요구 아이템. 거부되면 되돌린다.</summary>
      public PlayerController.ItemUseConsumptionReceipt ReservedRequirement;
    }

    private static void SettleReservedRequirement(PendingConnectionContext context, bool accepted)
    {
      if (context?.ReservedRequirement == null)
        return;
      var receipt = context.ReservedRequirement;
      context.ReservedRequirement = null;
      context.Player?.CompleteConsumedItemUse(receipt, accepted);
    }

    private readonly struct ActiveConnectionPair
    {
      public ActiveConnectionPair(LineConnectionPoint first, LineConnectionPoint second)
      {
        First = first;
        Second = second;
      }

      public LineConnectionPoint First { get; }
      public LineConnectionPoint Second { get; }
    }

    private readonly struct PendingTopologyChange
    {
      public PendingTopologyChange(long version, string first, string second, bool connected)
      {
        Version = version;
        First = first;
        Second = second;
        Connected = connected;
        QueuedAt = Time.unscaledTime;
      }

      public long Version { get; }
      public string First { get; }
      public string Second { get; }
      public bool Connected { get; }
      public float QueuedAt { get; }
    }

    [Header("Hierarchy")]
    [SerializeField] private Transform _linesRoot;

    [Header("Line Visual")]
    [SerializeField] private Color _lineColor = new Color(0.94f, 0.98f, 1f, 0.18f);

    [Header("Line Shape")]
    [SerializeField, Min(2)] private int _lineSegments = 18;
    [SerializeField, Min(0f)] private float _lineSagAmount = 0.015f;

    [Header("Line Simulation")]
    [SerializeField] private bool _usePhysicsSimulation = true;
    [SerializeField, Min(1)] private int _simulationStepsPerFrame = 2;
    [SerializeField, Min(1)] private int _solverIterations = 8;
    [SerializeField, Range(0f, 1f)] private float _gravityScale = 0.18f;
    [SerializeField, Range(0f, 1f)] private float _velocityDamping = 0.1f;
    [SerializeField, Min(0f)] private float _slackLength = 0.06f;

    [Header("Line Collision")]
    [SerializeField] private bool _collideWithWorld = true;
    [SerializeField, Min(0.0005f)] private float _collisionRadius = 0.006f;
    [SerializeField] private LayerMask _collisionMask = ~0;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

    private readonly Dictionary<int, PendingConnectionContext> _pendingConnections = new();
    private readonly List<ActiveConnectionPair> _activeAuthoritativePairs = new();
    private readonly List<ActiveConnectionPair> _automaticPairs = new();
    private readonly List<ActiveConnectionPair> _snapshotStalePairs = new();
    private readonly List<PendingTopologyChange> _pendingTopologyChanges = new();
    private readonly List<PendingTopologyChange> _deferredTopologyChanges = new();
    private bool _applyingReplicatedSnapshot;
    private long _lastAppliedTopologyVersion = -1;
    private long _snapshotTopologyVersion = -1;
    private static LineConnectionService _topologyService;
    private static readonly List<LineConnectionService> TopologyServices = new();
    private static int _nextTopologyRequestId;
    private bool _subscribedToTopology;

    public static LineConnectionService TopologyService =>
      _topologyService != null && _topologyService.isActiveAndEnabled ? _topologyService : null;

    private void OnEnable()
    {
      if (!TopologyServices.Contains(this))
        TopologyServices.Add(this);
      TryBecomeTopologyService();
    }

    private void TryBecomeTopologyService()
    {
      if (_topologyService != null || !isActiveAndEnabled)
        return;
      _topologyService = this;
      _subscribedToTopology = true;
      ScenarioNetworkRelay.LineTopologyRequestReceived += HandleTopologyRequest;
      ScenarioNetworkRelay.LineTopologyMirrored += HandleTopologyMirror;
      ScenarioNetworkRelay.LineTopologyRequestCompleted += HandleTopologyRequestCompleted;
      ScenarioNetworkRelay.LineTopologySnapshotBegan += BeginReplicatedTopologySnapshot;
      ScenarioNetworkRelay.LineTopologySnapshotEnded += EndReplicatedTopologySnapshot;
      ScenarioNetworkRelay.LineTopologySnapshotRequested += GetTopologySnapshot;
    }

    private void OnDisable()
    {
      if (!_subscribedToTopology)
      {
        TopologyServices.Remove(this);
        return;
      }
      ScenarioNetworkRelay.LineTopologyRequestReceived -= HandleTopologyRequest;
      ScenarioNetworkRelay.LineTopologyMirrored -= HandleTopologyMirror;
      ScenarioNetworkRelay.LineTopologyRequestCompleted -= HandleTopologyRequestCompleted;
      ScenarioNetworkRelay.LineTopologySnapshotBegan -= BeginReplicatedTopologySnapshot;
      ScenarioNetworkRelay.LineTopologySnapshotEnded -= EndReplicatedTopologySnapshot;
      ScenarioNetworkRelay.LineTopologySnapshotRequested -= GetTopologySnapshot;
      _subscribedToTopology = false;
      if (_topologyService == this)
      {
        _topologyService = null;
        for (int i = 0; i < TopologyServices.Count; i++)
          TopologyServices[i]?.TryBecomeTopologyService();
      }
      TopologyServices.Remove(this);
    }

    private void Awake()
    {
      EnsureLinesRoot();
    }

    private bool HandleTopologyRequest(
      string firstIdentifier, string secondIdentifier, bool connected, NetworkConnection sender)
    {
      if (!TryFindConnectionPoint(firstIdentifier, out var first))
        return false;

      if (!connected && string.Equals(firstIdentifier, secondIdentifier, StringComparison.Ordinal))
        return DisconnectFromPointOnServer(first, sender, broadcast: false);

      if (!TryFindConnectionPoint(secondIdentifier, out var second))
        return false;

      return connected
        ? TryCompleteConnectionOnServer(first, second, sender, broadcast: false)
        : DisconnectPairOnServer(first, second, sender, broadcast: false);
    }

    private void HandleTopologyMirror(long version, string firstIdentifier, string secondIdentifier, bool connected)
    {
      if (_applyingReplicatedSnapshot)
      {
        if (version < _snapshotTopologyVersion)
          return;
        if (version > _snapshotTopologyVersion)
        {
          QueueDeferredTopologyChange(version, firstIdentifier, secondIdentifier, connected);
          return;
        }
      }
      else if (version < _lastAppliedTopologyVersion)
        return;

      if (!TryFindConnectionPoint(firstIdentifier, out var first))
      {
        QueuePendingTopologyChange(version, firstIdentifier, secondIdentifier, connected);
        return;
      }
      if (!connected && string.Equals(firstIdentifier, secondIdentifier, StringComparison.Ordinal))
      {
        while (first.TryGetAnyConnectedLineObject(out var line) && line != null)
          DestroyLineObject(line, notifyEndpoints: false);
        if (!_applyingReplicatedSnapshot)
          _lastAppliedTopologyVersion = version;
        return;
      }
      if (!TryFindConnectionPoint(secondIdentifier, out var second))
      {
        QueuePendingTopologyChange(version, firstIdentifier, secondIdentifier, connected);
        return;
      }

      if (connected)
      {
        if (_applyingReplicatedSnapshot)
          ApplyReplicatedSnapshotPair(first, second);
        else
          ApplyReplicatedConnection(first, second);
      }
      else
        ApplyReplicatedDisconnect(first, second);

      if (!_applyingReplicatedSnapshot)
        _lastAppliedTopologyVersion = version;
    }

    private void HandleTopologyRequestCompleted(
      int requestId, string firstIdentifier, string secondIdentifier, bool connected, bool accepted)
    {
      // 서버가 거부해도 보류 요청은 아래에서 함께 정리된다. 다만 거부 사실 자체가 어디에도
      // 드러나지 않으면 "연결 모드만 풀리고 선은 생기지 않는" 현상의 원인을 추적할 수 없다.
      if (!accepted)
      {
        Debug.LogWarning(
          $"[LineConnectionService] 서버가 라인 토폴로지 요청을 거부했습니다: "
          + $"'{firstIdentifier}' -> '{secondIdentifier}' (connected={connected}, requestId={requestId}). "
          + "식별자가 유효하지 않거나 요청 빈도 제한에 걸렸을 수 있습니다.");
      }

      var completedKeys = new List<int>();
      foreach (var pair in _pendingConnections)
      {
        var context = pair.Value;
        if (context?.Player == null || context.StartPoint == null
            || !string.Equals(context.StartPoint.ConnectionIdentifier, firstIdentifier, StringComparison.Ordinal)
            || context.EndPoint == null
            || !string.Equals(context.EndPoint.ConnectionIdentifier, secondIdentifier, StringComparison.Ordinal)
            || !connected || context.RequestId != requestId)
          continue;
        completedKeys.Add(pair.Key);
        // 요청 전에 미리 소비한 요구 아이템은 서버 판정에 따라 확정하거나 되돌린다.
        SettleReservedRequirement(context, accepted);
        context.Player.SetLineConnectionMode(false, false);
        context.Player.RefreshInteractableHintsNow();
      }
      for (int i = 0; i < completedKeys.Count; i++)
        _pendingConnections.Remove(completedKeys[i]);
    }

    private void Update()
    {
      CleanupInvalidPendingConnections();
      for (int i = _pendingTopologyChanges.Count - 1; i >= 0; i--)
      {
        var pending = _pendingTopologyChanges[i];
        if (Time.unscaledTime - pending.QueuedAt > 10f)
        {
          Debug.LogWarning(
            $"[LineConnectionService] 서버가 보낸 라인 변경을 10초 안에 반영하지 못해 폐기합니다: "
            + $"'{pending.First}' -> '{pending.Second}' (connected={pending.Connected}, "
            + $"version={pending.Version}). 해당 연결 지점을 씬에서 찾지 못했습니다.");
          _pendingTopologyChanges.RemoveAt(i);
          continue;
        }
        if (!TryFindConnectionPoint(pending.First, out _)
            || (!string.Equals(pending.First, pending.Second, StringComparison.Ordinal)
                && !TryFindConnectionPoint(pending.Second, out _)))
          continue;
        _pendingTopologyChanges.RemoveAt(i);
        HandleTopologyMirror(pending.Version, pending.First, pending.Second, pending.Connected);
      }
    }

    private void QueuePendingTopologyChange(long version, string first, string second, bool connected)
    {
      for (int i = 0; i < _pendingTopologyChanges.Count; i++)
      {
        var pending = _pendingTopologyChanges[i];
        if (pending.Version == version && pending.First == first
            && pending.Second == second && pending.Connected == connected)
          return;
      }
      if (_pendingTopologyChanges.Count >= 128)
      {
        Debug.LogWarning(
          $"[LineConnectionService] 보류 라인 변경 대기열이 상한(128)에 도달해 변경을 폐기합니다: "
          + $"'{first}' -> '{second}' (connected={connected}, version={version}).");
        return;
      }

      _pendingTopologyChanges.Add(new PendingTopologyChange(version, first, second, connected));
    }

    private void QueueDeferredTopologyChange(long version, string first, string second, bool connected)
    {
      for (int i = 0; i < _deferredTopologyChanges.Count; i++)
      {
        var pending = _deferredTopologyChanges[i];
        if (pending.Version == version && pending.First == first
            && pending.Second == second && pending.Connected == connected)
          return;
      }
      if (_deferredTopologyChanges.Count >= 128)
      {
        Debug.LogWarning(
          $"[LineConnectionService] 지연 라인 변경 대기열이 상한(128)에 도달해 변경을 폐기합니다: "
          + $"'{first}' -> '{second}' (connected={connected}, version={version}).");
        return;
      }

      _deferredTopologyChanges.Add(new PendingTopologyChange(version, first, second, connected));
    }

    private IEnumerable<ScenarioNetworkRelay.LineTopologyPair> GetTopologySnapshot()
    {
      PruneInvalidAuthoritativePairs();
      for (int i = 0; i < _activeAuthoritativePairs.Count; i++)
      {
        var pair = _activeAuthoritativePairs[i];
        if (pair.First != null && pair.Second != null)
          yield return new ScenarioNetworkRelay.LineTopologyPair(
            pair.First.ConnectionIdentifier, pair.Second.ConnectionIdentifier);
      }
    }

    private static bool TryFindConnectionPoint(string identifier, out LineConnectionPoint point)
    {
      point = null;
      if (string.IsNullOrWhiteSpace(identifier))
        return false;
      var points = FindObjectsByType<LineConnectionPoint>(
        FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
      LineConnectionPoint match = null;
      for (int i = 0; i < points.Length; i++)
      {
        if (points[i] != null && string.Equals(points[i].ConnectionIdentifier, identifier, StringComparison.Ordinal))
        {
          if (match != null)
          {
            Debug.LogError(
              $"[LineConnectionService] 연결 지점 식별자 '{identifier}'가 중복되어 토폴로지 변경을 거부합니다. "
              + $"첫 번째='{GetHierarchyPath(match.transform)}', 두 번째='{GetHierarchyPath(points[i].transform)}'.");
            return false;
          }
          match = points[i];
        }
      }
      point = match;
      return point != null;
    }

    private static string GetHierarchyPath(Transform target)
    {
      if (target == null)
        return "<null>";
      string path = target.name;
      for (Transform parent = target.parent; parent != null; parent = parent.parent)
        path = parent.name + "/" + path;
      return target.gameObject.scene.name + "/" + path;
    }

    private void OnValidate()
    {
      EnsureLinesRoot();
      _lineSegments = Mathf.Max(2, _lineSegments);
      _lineSagAmount = Mathf.Max(0f, _lineSagAmount);
      _simulationStepsPerFrame = Mathf.Max(1, _simulationStepsPerFrame);
      _solverIterations = Mathf.Max(1, _solverIterations);
      _gravityScale = Mathf.Clamp01(_gravityScale);
      _velocityDamping = Mathf.Clamp01(_velocityDamping);
      _slackLength = Mathf.Max(0f, _slackLength);
      _collisionRadius = Mathf.Max(0.0005f, _collisionRadius);
    }

    public void BeginConnectionMode(PlayerController player, LineConnectionPoint startPoint)
    {
      if (player == null || startPoint == null)
        return;

      int key = player.GetInstanceID();

      _pendingConnections[key] = new PendingConnectionContext
      {
        Player = player,
        StartPoint = startPoint,
      };

      player.SetLineConnectionMode(true);
      player.RefreshInteractableHintsNow();

      startPoint.NotifyConnectionStarted();
    }

    public bool HasPendingStartPoint(PlayerController player, out LineConnectionPoint startPoint)
    {
      startPoint = null;

      if (player == null)
        return false;

      if (!_pendingConnections.TryGetValue(player.GetInstanceID(), out var context))
        return false;

      if (context == null || context.StartPoint == null)
        return false;

      startPoint = context.StartPoint;
      return true;
    }

    /// <summary>
    /// 시작점이 정의한 포인트 고유의 요구사항을 소모한 뒤 범용 라인 연결을 완료한다.
    /// </summary>
    public bool TryCompleteConnection(PlayerController player, LineConnectionPoint endPoint)
    {
      if (player == null || endPoint == null)
        return false;

      if (!HasPendingStartPoint(player, out var startPoint) || startPoint == null)
        return false;

      if (!player.IsLineConnectionMode)
      {
        ClearPendingFor(player);
        player.RefreshInteractableHintsNow();
        return false;
      }

      if (ReferenceEquals(startPoint, endPoint))
        return false;

      if (!startPoint.CanAcceptAdditionalConnection || !endPoint.CanAcceptAdditionalConnection)
        return false;

      if (!startPoint.CanConnectTo(endPoint) || !endPoint.CanConnectTo(startPoint))
        return false;

      if (!CanConnectBetweenDifferentOwners(startPoint, endPoint, out _))
        return false;

      if (!startPoint.CanPlayerCompleteConnection(player, endPoint)
          || !endPoint.CanPlayerCompleteConnection(player, startPoint))
        return false;

      if (!InstanceFinder.IsOffline)
      {
        if (IsServerFor(player))
          TryCompleteConnectionOnServer(startPoint, endPoint, player.Owner);
        else
        {
          // 서버는 원격 발신자의 인벤토리를 볼 수 없으므로, 요구 아이템은 여기서 먼저 소비하고
          // 영수증을 보류 요청에 붙여 둔다. 서버가 거부하면 응답 처리에서 되돌린다.
          if (!startPoint.TryReserveConnectionRequirement(player, out var reserved))
          {
            player.SetLineConnectionMode(false, false);
            ClearPendingFor(player);
            player.RefreshInteractableHintsNow();
            return false;
          }
          if (_pendingConnections.TryGetValue(player.GetInstanceID(), out var pending))
          {
            pending.EndPoint = endPoint;
            pending.RequestId = NextTopologyRequestId();
            pending.ReservedRequirement = reserved;
          }
          if (!ScenarioNetworkRelay.RequestLineTopologyChange(
                startPoint.ConnectionIdentifier, endPoint.ConnectionIdentifier, connected: true,
                pending != null ? pending.RequestId : 0))
          {
            if (pending != null)
              SettleReservedRequirement(pending, accepted: false);
            else
              player.CompleteConsumedItemUse(reserved, accepted: false);
            return false;
          }
          return true;
        }
        ClearPendingFor(player);
        player.SetLineConnectionMode(false, false);
        player.RefreshInteractableHintsNow();
        return true;
      }

      if (!startPoint.TryConsumeConnectionRequirement(player))
      {
        player.SetLineConnectionMode(false, false);
        ClearPendingFor(player);
        player.RefreshInteractableHintsNow();
        return false;
      }

      if (!CreateAndRegisterConnection(startPoint, endPoint))
      {
        player.SetLineConnectionMode(false, false);
        ClearPendingFor(player);
        player.RefreshInteractableHintsNow();
        return false;
      }

      using (ScenarioSignalPlayerContext.Push(player.Owner))
      {
        startPoint.NotifyConnectionCompleted(endPoint);
        startPoint.NotifyLineConnected(endPoint);
        endPoint.NotifyLineConnected(startPoint);
      }

      ClearPendingFor(player);
      player.SetLineConnectionMode(false, false);
      player.RefreshInteractableHintsNow();
      return true;
    }

    private static int NextTopologyRequestId()
    {
      _nextTopologyRequestId = _nextTopologyRequestId == int.MaxValue ? 1 : _nextTopologyRequestId + 1;
      return _nextTopologyRequestId;
    }

    internal bool TryCompleteConnectionOnServer(
      LineConnectionPoint startPoint,
      LineConnectionPoint endPoint,
      NetworkConnection sender,
      bool broadcast = true)
    {
      if (!TryResolveSenderPlayer(sender, out var player)
          || !ValidateAuthoritativeConnection(player, startPoint, endPoint, sender, out _))
        return false;

      // 인벤토리는 소유 클라이언트에만 있다. 호스트 자신의 플레이어는 여기서 소비하고, 원격 발신자는
      // 요청 전에 자기 인벤토리에서 소비한 뒤 왔으므로(TryReserveConnectionRequirement) 서버 복제본의
      // 빈 인벤토리로 다시 검사하지 않는다. 그렇게 하면 호스트가 아닌 참여자는 라인을 연결할 수 없다.
      if (player.IsOwner && !startPoint.TryConsumeConnectionRequirement(player))
        return false;
      if (!CreateAndRegisterConnection(startPoint, endPoint))
        return false;

      AddAuthoritativePair(startPoint, endPoint);

      using (ScenarioSignalPlayerContext.Push(sender))
      {
        startPoint.NotifyConnectionCompleted(endPoint);
        startPoint.NotifyLineConnected(endPoint);
        endPoint.NotifyLineConnected(startPoint);
      }

      if (broadcast)
        ScenarioNetworkRelay.PublishLineTopologyChange(
          startPoint.ConnectionIdentifier, endPoint.ConnectionIdentifier, connected: true);
      return true;
    }

    internal bool ValidateAuthoritativeConnection(
      PlayerController player,
      LineConnectionPoint startPoint,
      LineConnectionPoint endPoint,
      NetworkConnection sender,
      out string reason)
    {
      reason = string.Empty;
      if (player == null || startPoint == null || endPoint == null || ReferenceEquals(startPoint, endPoint))
      {
        reason = "missing or identical connection endpoint";
        return false;
      }
      if (!startPoint.CanAcceptAdditionalConnection || !endPoint.CanAcceptAdditionalConnection
          || !startPoint.CanConnectTo(endPoint) || !endPoint.CanConnectTo(startPoint)
          || !CanConnectBetweenDifferentOwners(startPoint, endPoint, out reason))
        return false;
      if (!IsWithinConnectionDistance(player, startPoint)
          || !IsWithinConnectionDistance(player, endPoint))
      {
        reason = "player is too far from an endpoint";
        return false;
      }
      if (!startPoint.CanPlayerCompleteConnection(player, endPoint)
          || !endPoint.CanPlayerCompleteConnection(player, startPoint))
      {
        reason = "endpoint rejected player";
        return false;
      }

      if (!TryResolvePatientBCNormalSalineContext(startPoint, endPoint, out var patient, out var salinePoint))
        return true;
      if (!IsExactPatientNormalSalineEndpointPair(patient, salinePoint, startPoint, endPoint))
      {
        reason = "saline endpoint is not connected to the exact patient IV endpoint";
        return false;
      }
      if (sender == null || !sender.IsValid
          || !patient.CanPlayerCompletePatientBCNormalSalineConnection(player))
      {
        reason = "sender does not have the assigned patient treatment role";
        return false;
      }
      if (!patient.CanAuthoritativelyConnectPatientBCNormalSaline())
      {
        reason = "patient is not awaiting IV or normal saline";
        return false;
      }
      return true;
    }

    public void CancelConnectionMode(PlayerController player)
    {
      if (player == null)
        return;

      if (!_pendingConnections.Remove(player.GetInstanceID()))
        return;

      player.RefreshInteractableHintsNow();
    }

    public void DisconnectFromPoint(LineConnectionPoint point, PlayerController player = null)
    {
      if (point == null)
        return;

      if (!InstanceFinder.IsOffline)
      {
        if (IsServerFor(player))
          DisconnectFromPointOnServer(point, player.Owner);
        else
          ScenarioNetworkRelay.RequestLineTopologyChange(
            point.ConnectionIdentifier, point.ConnectionIdentifier, connected: false);
        return;
      }

      bool destroyedAny = false;
      while (point.TryGetAnyConnectedLineObject(out var each) && each != null)
      {
        destroyedAny = true;
        DestroyLineObject(each);
      }

      if (destroyedAny)
      {
        var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
          players[i]?.RefreshInteractableHintsNow();
      }
    }

    internal bool DisconnectFromPointOnServer(
      LineConnectionPoint point, NetworkConnection sender, bool broadcast = true)
    {
      if (point == null || !TryResolveSenderPlayer(sender, out var player)
          || !IsWithinConnectionDistance(player, point))
        return false;

      bool destroyedAny = false;
      while (point.TryGetAnyConnectedLineObject(out var lineObject) && lineObject != null)
      {
        if (!lineObject.TryGetComponent<LineConnectionRuntime>(out var runtime)
            || runtime == null)
          break;
        var other = ReferenceEquals(runtime.StartPoint, point) ? runtime.EndPoint : runtime.StartPoint;
        RemoveAuthoritativePair(point, other);
        DestroyLineObject(lineObject);
        if (broadcast)
          ScenarioNetworkRelay.PublishLineTopologyChange(
            point.ConnectionIdentifier, other.ConnectionIdentifier, connected: false);
        destroyedAny = true;
      }
      return destroyedAny;
    }

    private bool DisconnectPairOnServer(
      LineConnectionPoint first, LineConnectionPoint second, NetworkConnection sender, bool broadcast)
    {
      if (first == null || second == null || !TryResolveSenderPlayer(sender, out var player)
          || !IsWithinConnectionDistance(player, first) || !IsWithinConnectionDistance(player, second)
          || !first.TryGetConnectedLineObjectTo(second, out var lineObject))
        return false;

      RemoveAuthoritativePair(first, second);
      DestroyLineObject(lineObject);
      if (broadcast)
        ScenarioNetworkRelay.PublishLineTopologyChange(
          first.ConnectionIdentifier, second.ConnectionIdentifier, connected: false);
      return true;
    }

    public void BeginReplicatedTopologySnapshot()
    {
      BeginReplicatedTopologySnapshot(_lastAppliedTopologyVersion + 1);
    }

    public void BeginReplicatedTopologySnapshot(long version)
    {
      if (version <= _lastAppliedTopologyVersion)
        return;
      _applyingReplicatedSnapshot = true;
      _snapshotTopologyVersion = version;
      _snapshotStalePairs.Clear();
      EnsureLinesRoot();
      if (_linesRoot == null)
        return;
      var runtimes = _linesRoot.GetComponentsInChildren<LineConnectionRuntime>(true);
      for (int i = 0; i < runtimes.Length; i++)
      {
        var runtime = runtimes[i];
        if (runtime != null && runtime.StartPoint != null && runtime.EndPoint != null)
          _snapshotStalePairs.Add(new ActiveConnectionPair(runtime.StartPoint, runtime.EndPoint));
      }
    }

    public bool ApplyReplicatedSnapshotPair(
      LineConnectionPoint first,
      LineConnectionPoint second)
    {
      if (!_applyingReplicatedSnapshot)
        return false;
      RemovePairFromList(_snapshotStalePairs, first, second);
      return ApplyReplicatedConnection(first, second);
    }

    public void EndReplicatedTopologySnapshot()
    {
      EndReplicatedTopologySnapshot(_snapshotTopologyVersion);
    }

    public void EndReplicatedTopologySnapshot(long version)
    {
      if (!_applyingReplicatedSnapshot || version != _snapshotTopologyVersion)
        return;
      for (int i = 0; i < _snapshotStalePairs.Count; i++)
      {
        var pair = _snapshotStalePairs[i];
        ApplyReplicatedDisconnect(pair.First, pair.Second);
      }
      _snapshotStalePairs.Clear();
      _applyingReplicatedSnapshot = false;
      _lastAppliedTopologyVersion = Math.Max(_lastAppliedTopologyVersion, version);
      _snapshotTopologyVersion = -1;

      _deferredTopologyChanges.Sort((left, right) => left.Version.CompareTo(right.Version));
      var deferredChanges = _deferredTopologyChanges.ToArray();
      _deferredTopologyChanges.Clear();
      for (int i = 0; i < deferredChanges.Length; i++)
      {
        var pending = deferredChanges[i];
        HandleTopologyMirror(pending.Version, pending.First, pending.Second, pending.Connected);
      }
    }

    public bool ApplyReplicatedConnection(LineConnectionPoint first, LineConnectionPoint second)
    {
      if (first == null || second == null || first.IsPhysicallyConnectedTo(second))
        return false;
      if (!CreateAndRegisterConnection(first, second))
        return false;
      first.NotifyReplicatedLineConnected(second);
      second.NotifyReplicatedLineConnected(first);
      return true;
    }

    public bool ApplyReplicatedDisconnect(LineConnectionPoint first, LineConnectionPoint second)
    {
      if (first == null || second == null)
        return false;
      if (!first.TryGetConnectedLineObjectTo(second, out var lineObject))
        return false;
      DestroyLineObject(lineObject, notifyEndpoints: false);
      first.NotifyReplicatedLineDisconnected(second);
      second.NotifyReplicatedLineDisconnected(first);
      return true;
    }

    public bool CreateAndRegisterConnection(LineConnectionPoint startPoint, LineConnectionPoint endPoint)
    {
      if (startPoint == null || endPoint == null || startPoint.IsPhysicallyConnectedTo(endPoint))
        return false;
      var lineObject = CreateLineObject(startPoint, endPoint);
      if (lineObject == null)
        return false;
      startPoint.RegisterConnectedLineObject(lineObject);
      endPoint.RegisterConnectedLineObject(lineObject);
      return true;
    }

    /// <summary>
    /// 플레이어 상호작용이나 아이템 소모 없이 권한 있는 연결을 만든다.
    /// 새로 설치되는 CareZone 장비 전용이다.
    /// </summary>
    public bool TryCreateAutomaticConnection(LineConnectionPoint startPoint, LineConnectionPoint endPoint)
    {
      if (startPoint == null || endPoint == null || ReferenceEquals(startPoint, endPoint)
          || startPoint.IsPhysicallyConnectedTo(endPoint)
          || !startPoint.CanAcceptAdditionalConnection || !endPoint.CanAcceptAdditionalConnection
          || !startPoint.CanConnectTo(endPoint) || !endPoint.CanConnectTo(startPoint)
          || !CanConnectBetweenDifferentOwners(startPoint, endPoint, out _)
          || (!InstanceFinder.IsOffline && !InstanceFinder.IsServerStarted))
        return false;

      if (!CreateAndRegisterConnection(startPoint, endPoint))
        return false;

      if (!InstanceFinder.IsOffline)
        AddAuthoritativePair(startPoint, endPoint);
      AddAutomaticPair(startPoint, endPoint);
      startPoint.NotifyLineConnected(endPoint);
      endPoint.NotifyLineConnected(startPoint);
      if (!InstanceFinder.IsOffline)
        ScenarioNetworkRelay.PublishLineTopologyChange(
          startPoint.ConnectionIdentifier, endPoint.ConnectionIdentifier, connected: true);
      return true;
    }

    /// <summary>지정한 자동 관리 종단점 쌍만 제거한다.</summary>
    public bool DisconnectAutomaticConnection(LineConnectionPoint first, LineConnectionPoint second)
    {
      if (first == null || second == null || !ContainsAutomaticPair(first, second)
          || !first.TryGetConnectedLineObjectTo(second, out var lineObject)
          || (!InstanceFinder.IsOffline && !InstanceFinder.IsServerStarted))
        return false;

      RemoveAutomaticPair(first, second);
      RemoveAuthoritativePair(first, second);
      DestroyLineObject(lineObject);
      if (!InstanceFinder.IsOffline)
        ScenarioNetworkRelay.PublishLineTopologyChange(
          first.ConnectionIdentifier, second.ConnectionIdentifier, connected: false);
      return true;
    }

    public int ActiveAuthoritativePairCount
    {
      get
      {
        PruneInvalidAuthoritativePairs();
        return _activeAuthoritativePairs.Count;
      }
    }

    public bool AddAuthoritativePair(LineConnectionPoint first, LineConnectionPoint second)
    {
      if (first == null || second == null || ReferenceEquals(first, second)
          || ContainsAuthoritativePair(first, second))
        return false;
      _activeAuthoritativePairs.Add(new ActiveConnectionPair(first, second));
      return true;
    }

    public bool RemoveAuthoritativePair(LineConnectionPoint first, LineConnectionPoint second)
    {
      return RemovePairFromList(_activeAuthoritativePairs, first, second);
    }

    private bool AddAutomaticPair(LineConnectionPoint first, LineConnectionPoint second)
    {
      if (first == null || second == null || ReferenceEquals(first, second) || ContainsAutomaticPair(first, second))
        return false;
      _automaticPairs.Add(new ActiveConnectionPair(first, second));
      return true;
    }

    private bool RemoveAutomaticPair(LineConnectionPoint first, LineConnectionPoint second) =>
      RemovePairFromList(_automaticPairs, first, second);

    private bool ContainsAutomaticPair(LineConnectionPoint first, LineConnectionPoint second)
    {
      for (int i = 0; i < _automaticPairs.Count; i++)
      {
        var pair = _automaticPairs[i];
        if ((ReferenceEquals(pair.First, first) && ReferenceEquals(pair.Second, second))
            || (ReferenceEquals(pair.First, second) && ReferenceEquals(pair.Second, first)))
          return true;
      }
      return false;
    }

    private static bool RemovePairFromList(
      List<ActiveConnectionPair> pairs,
      LineConnectionPoint first,
      LineConnectionPoint second)
    {
      for (int i = pairs.Count - 1; i >= 0; i--)
      {
        var pair = pairs[i];
        if ((ReferenceEquals(pair.First, first) && ReferenceEquals(pair.Second, second))
            || (ReferenceEquals(pair.First, second) && ReferenceEquals(pair.Second, first)))
        {
          pairs.RemoveAt(i);
          return true;
        }
      }
      return false;
    }

    public bool ContainsAuthoritativePair(LineConnectionPoint first, LineConnectionPoint second)
    {
      for (int i = 0; i < _activeAuthoritativePairs.Count; i++)
      {
        var pair = _activeAuthoritativePairs[i];
        if ((ReferenceEquals(pair.First, first) && ReferenceEquals(pair.Second, second))
            || (ReferenceEquals(pair.First, second) && ReferenceEquals(pair.Second, first)))
          return true;
      }
      return false;
    }

    private void PruneInvalidAuthoritativePairs()
    {
      for (int i = _activeAuthoritativePairs.Count - 1; i >= 0; i--)
      {
        var pair = _activeAuthoritativePairs[i];
        if (pair.First == null || pair.Second == null
            || !pair.First.IsPhysicallyConnectedTo(pair.Second))
          _activeAuthoritativePairs.RemoveAt(i);
      }
      for (int i = _automaticPairs.Count - 1; i >= 0; i--)
      {
        var pair = _automaticPairs[i];
        if (pair.First == null || pair.Second == null || !pair.First.IsPhysicallyConnectedTo(pair.Second))
          _automaticPairs.RemoveAt(i);
      }
    }


    private GameObject CreateLineObject(LineConnectionPoint startPoint, LineConnectionPoint endPoint)
    {
      EnsureLinesRoot();
      if (_linesRoot == null)
        return null;

      var lineObject = new GameObject($"Line_{startPoint.name}_to_{endPoint.name}");
      lineObject.transform.SetParent(_linesRoot, false);

      LineRenderer lineRenderer = null;
      if (!MppmLiteMode.IsHeadless)
      {
        lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCornerVertices = 6;
        lineRenderer.numCapVertices = 6;
        float lineWidth = GetLineWidth(startPoint);
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = _lineColor;
        lineRenderer.endColor = _lineColor;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        ApplyLineMaterial(startPoint, endPoint, lineRenderer);
      }

      var runtime = lineObject.AddComponent<LineConnectionRuntime>();
      runtime.Bind(
        startPoint,
        endPoint,
        lineRenderer,
        _lineSegments,
        _lineSagAmount,
        GetLineElasticity(startPoint),
        _usePhysicsSimulation,
        _simulationStepsPerFrame,
        _solverIterations,
        _gravityScale,
        _velocityDamping,
        _slackLength,
        _collideWithWorld,
        _collisionRadius,
        _collisionMask,
        _triggerInteraction);

      return lineObject;
    }

    private void DestroyLineObject(GameObject lineObject, bool notifyEndpoints = true)
    {
      if (lineObject == null)
        return;

      LineConnectionPoint startPoint = null;
      LineConnectionPoint endPoint = null;

      if (lineObject.TryGetComponent<LineConnectionRuntime>(out var runtime) && runtime != null)
      {
        startPoint = runtime.StartPoint;
        endPoint = runtime.EndPoint;

        RemoveAutomaticPair(startPoint, endPoint);
        startPoint?.UnregisterConnectedLineObject(lineObject);
        endPoint?.UnregisterConnectedLineObject(lineObject);
      }

      // 복제된 토폴로지는 에디터 검증/테스트에서도 다시 만들어진다. 플레이 모드 밖에서
      // Destroy 는 오류를 내고 오브젝트를 프레임 끝까지 살려두므로, 연결 해제 직후
      // 재연결이 오래된 토폴로지를 관찰할 수 있다.
      if (Application.isPlaying)
        Destroy(lineObject);
      else
        DestroyImmediate(lineObject);

      if (notifyEndpoints)
      {
        startPoint?.NotifyLineDisconnected(endPoint);
        endPoint?.NotifyLineDisconnected(startPoint);
      }
    }

    private void EnsureLinesRoot()
    {
      if (_linesRoot != null)
        return;

      var existing = transform.Find("Lines");
      if (existing != null)
      {
        _linesRoot = existing;
        return;
      }

      var rootObject = new GameObject("Lines");
      _linesRoot = rootObject.transform;
      _linesRoot.SetParent(transform, false);
    }

    private void ClearPendingFor(PlayerController player)
    {
      if (player == null)
        return;

      if (_pendingConnections.TryGetValue(player.GetInstanceID(), out var context))
        SettleReservedRequirement(context, accepted: false);
      _pendingConnections.Remove(player.GetInstanceID());
    }

    private void CleanupInvalidPendingConnections()
    {
      if (_pendingConnections.Count == 0)
        return;

      var keys = new List<int>();
      foreach (var each in _pendingConnections)
      {
        var context = each.Value;
        var player = context?.Player;
        var startPoint = context?.StartPoint;

        if (player == null || startPoint == null)
        {
          keys.Add(each.Key);
          continue;
        }

        if (!player.IsLineConnectionMode)
          keys.Add(each.Key);
      }

      for (int i = 0; i < keys.Count; i++)
      {
        if (!_pendingConnections.TryGetValue(keys[i], out var context))
          continue;

        _pendingConnections.Remove(keys[i]);
        // 서버 응답 없이 보류 요청이 사라지면(연결 모드 취소 등) 미리 소비한 요구 아이템을 되돌린다.
        SettleReservedRequirement(context, accepted: false);
        context?.Player?.RefreshInteractableHintsNow();
      }
    }

    private static bool CanConnectBetweenDifferentOwners(
      LineConnectionPoint startPoint,
      LineConnectionPoint endPoint,
      out string reason)
    {
      reason = string.Empty;

      if (startPoint == null || endPoint == null)
      {
        reason = "start/end point is null";
        return false;
      }

      bool ok = !ReferenceEquals(startPoint.transform.root, endPoint.transform.root);
      if (!ok)
        reason = "same endpoint root";
      return ok;
    }

    private static bool IsServerFor(PlayerController player) =>
      player != null && player.IsServerStarted;

    private static bool IsWithinConnectionDistance(PlayerController player, LineConnectionPoint point) =>
      player != null && point != null
      && (player.transform.position - point.transform.position).sqrMagnitude <= 9f;

    private static bool TryResolveSenderPlayer(NetworkConnection sender, out PlayerController player)
    {
      player = null;
      if (sender == null || !sender.IsValid
          || !MultiplayerInfrastructure.Registry.Registry.TryGetEntityByClientId(sender.ClientId, out var entity)
          || entity?.GameObject == null)
        return false;
      player = entity.GameObject.GetComponent<PlayerController>()
               ?? entity.GameObject.GetComponentInChildren<PlayerController>(true);
      return player != null && player.Owner != null && player.Owner.IsValid
             && player.Owner.ClientId == sender.ClientId;
    }

    private static bool TryResolvePatientBCNormalSalineContext(
      LineConnectionPoint startPoint,
      LineConnectionPoint endPoint,
      out TriageTrainer.Entity.PatientController patient,
      out IntravenousLineConnectionPoint salinePoint)
    {
      patient = startPoint.GetComponentInParent<TriageTrainer.Entity.PatientController>()
                ?? endPoint.GetComponentInParent<TriageTrainer.Entity.PatientController>();
      if (patient == null)
      {
        var fallbackBed = startPoint.GetComponentInParent<TriageTrainer.Entity.MovingPatientBedController>()
                          ?? endPoint.GetComponentInParent<TriageTrainer.Entity.MovingPatientBedController>();
        patient = fallbackBed?.ReposedTarget as TriageTrainer.Entity.PatientController;
      }
      var bed = startPoint.GetComponentInParent<TriageTrainer.Entity.MovingPatientBedController>()
                 ?? endPoint.GetComponentInParent<TriageTrainer.Entity.MovingPatientBedController>();
      salinePoint = startPoint as IntravenousLineConnectionPoint;
      if (bed == null || !bed.IsNormalSalineConnectionPoint(salinePoint))
        salinePoint = endPoint as IntravenousLineConnectionPoint;
      return patient != null
             && (string.Equals(patient.Identifier, "patient_b", StringComparison.Ordinal)
                 || string.Equals(patient.Identifier, "patient_c", StringComparison.Ordinal))
             && salinePoint != null
             && bed != null
             && bed.IsNormalSalineConnectionPoint(salinePoint);
    }

    public static bool IsExactPatientNormalSalineEndpointPair(
      TriageTrainer.Entity.PatientController patient,
      IntravenousLineConnectionPoint salinePoint,
      LineConnectionPoint startPoint,
      LineConnectionPoint endPoint)
    {
      if (patient == null || salinePoint == null || ReferenceEquals(startPoint, endPoint))
        return false;

      // 환자 측 정맥로 포인트가 배선되지 않았으면(null) 어떤 끝점도 인정하지 않는다.
      // 이 검사가 없으면 null 끝점이 null 참조와 ReferenceEquals 로 일치해버린다.
      var patientPoint = patient.PatientBCIvAttachmentPoint;
      if (patientPoint == null)
        return false;

      return (ReferenceEquals(startPoint, salinePoint) && ReferenceEquals(endPoint, patientPoint))
             || (ReferenceEquals(endPoint, salinePoint) && ReferenceEquals(startPoint, patientPoint));
    }

    private void ApplyLineMaterial(
      LineConnectionPoint startPoint,
      LineConnectionPoint endPoint,
      LineRenderer lineRenderer)
    {
      // 포인트가 특수 케이스 라인의 머티리얼을 재정의할 수도 있다. 그렇지 않으면
      // 서비스가 구체적 포인트 타입별 머티리얼을 소유한다. 어느 한쪽 끝의 명시적
      // 재정의는 항상 서비스 기본값보다 우선하므로, 타입별 머티리얼으로 넘어가기
      // 전에 양쪽 끝을 모두 확인한다.
      var overrideMaterial = GetOverrideMaterial(startPoint) ?? GetOverrideMaterial(endPoint);
      if (overrideMaterial != null)
      {
        lineRenderer.sharedMaterial = overrideMaterial;
        return;
      }

      var material = GetLineMaterial(startPoint) ?? GetLineMaterial(endPoint);
      if (material != null)
      {
        lineRenderer.sharedMaterial = material;
        return;
      }

      if (startPoint != null)
        startPoint.ApplyLineMaterial(lineRenderer);
      else
        endPoint?.ApplyLineMaterial(lineRenderer);
    }

    private static Material GetOverrideMaterial(LineConnectionPoint point)
    {
      // Unity fake-null 을 실제 null 로 정규화해 ?? 연산이 기대대로 동작하게 한다.
      var material = point != null ? point.LineMaterial : null;
      return material != null ? material : null;
    }

    private Material GetLineMaterial(LineConnectionPoint point)
    {
      var material = point switch
      {
        IntravenousLineConnectionPoint => IntravenousLineConnectionPoint.DefaultMaterial,
        CentralLineConnectionPoint => CentralLineConnectionPoint.DefaultMaterial,
        AEDLineConnectionPoint => AEDLineConnectionPoint.DefaultMaterial,
        ElectricalLineConnectionPoint => ElectricalLineConnectionPoint.DefaultMaterial,
        OxyLineConnectionPoint => OxyLineConnectionPoint.DefaultMaterial,
        SuctionLineConnectionPoint => SuctionLineConnectionPoint.DefaultMaterial,
        _ => null,
      };

      // Unity fake-null 정규화(위와 동일한 이유).
      return material != null ? material : null;
    }

    private float GetLineElasticity(LineConnectionPoint point)
    {
      return point switch
      {
        IntravenousLineConnectionPoint => IntravenousLineConnectionPoint.Elasticity,
        CentralLineConnectionPoint => CentralLineConnectionPoint.Elasticity,
        AEDLineConnectionPoint => AEDLineConnectionPoint.Elasticity,
        ElectricalLineConnectionPoint => ElectricalLineConnectionPoint.Elasticity,
        OxyLineConnectionPoint => OxyLineConnectionPoint.Elasticity,
        SuctionLineConnectionPoint => SuctionLineConnectionPoint.Elasticity,
        _ => 0f,
      };
    }

    private float GetLineWidth(LineConnectionPoint point)
    {
      return point switch
      {
        IntravenousLineConnectionPoint => IntravenousLineConnectionPoint.LineWidth,
        CentralLineConnectionPoint => CentralLineConnectionPoint.LineWidth,
        AEDLineConnectionPoint => AEDLineConnectionPoint.LineWidth,
        ElectricalLineConnectionPoint => ElectricalLineConnectionPoint.LineWidth,
        OxyLineConnectionPoint => OxyLineConnectionPoint.LineWidth,
        SuctionLineConnectionPoint => SuctionLineConnectionPoint.LineWidth,
        _ => 0.01f,
      };
    }

  }

  public class LineConnectionRuntime : MonoBehaviour
  {
    [SerializeField] private LineConnectionPoint _startPoint;
    [SerializeField] private LineConnectionPoint _endPoint;
    [SerializeField] private LineRenderer _lineRenderer;

    [SerializeField, Min(2)] private int _segments = 18;
    [SerializeField, Min(0f)] private float _sagAmount = 0.015f;
    [SerializeField, Range(0f, 1f)] private float _elasticity = 0.15f;

    [SerializeField] private bool _usePhysicsSimulation = true;
    [SerializeField, Min(1)] private int _simulationStepsPerFrame = 2;
    [SerializeField, Min(1)] private int _solverIterations = 8;
    [SerializeField, Range(0f, 1f)] private float _gravityScale = 0.18f;
    [SerializeField, Range(0f, 1f)] private float _velocityDamping = 0.1f;
    [SerializeField, Min(0f)] private float _slackLength = 0.06f;

    [SerializeField] private bool _collideWithWorld = true;
    [SerializeField, Min(0.0005f)] private float _collisionRadius = 0.006f;
    [SerializeField] private LayerMask _collisionMask = ~0;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

    private const float Epsilon = 0.000001f;
    private readonly Collider[] _overlapResults = new Collider[24];

    private Vector3[] _points = Array.Empty<Vector3>();
    private Vector3[] _previousPoints = Array.Empty<Vector3>();
    private bool _simulationInitialized;

    public LineConnectionPoint StartPoint => _startPoint;
    public LineConnectionPoint EndPoint => _endPoint;

    public void Bind(
      LineConnectionPoint startPoint,
      LineConnectionPoint endPoint,
      LineRenderer lineRenderer,
      int segments,
      float sagAmount,
      float elasticity,
      bool usePhysicsSimulation,
      int simulationStepsPerFrame,
      int solverIterations,
      float gravityScale,
      float velocityDamping,
      float slackLength,
      bool collideWithWorld,
      float collisionRadius,
      LayerMask collisionMask,
      QueryTriggerInteraction triggerInteraction)
    {
      _startPoint = startPoint;
      _endPoint = endPoint;
      _lineRenderer = lineRenderer;

      _segments = Mathf.Max(2, segments);
      _sagAmount = Mathf.Max(0f, sagAmount);
      _elasticity = Mathf.Clamp01(elasticity);

      _usePhysicsSimulation = usePhysicsSimulation;
      _simulationStepsPerFrame = Mathf.Max(1, simulationStepsPerFrame);
      _solverIterations = Mathf.Max(1, solverIterations);
      _gravityScale = Mathf.Clamp01(gravityScale);
      _velocityDamping = Mathf.Clamp01(velocityDamping);
      _slackLength = Mathf.Max(0f, slackLength);

      _collideWithWorld = collideWithWorld;
      _collisionRadius = Mathf.Max(0.0005f, collisionRadius);
      _collisionMask = collisionMask;
      _triggerInteraction = triggerInteraction;

      _simulationInitialized = false;
      EnsureBuffers();
      RefreshLine();
    }

    private void LateUpdate()
    {
      RefreshLine();
    }

    private void OnDestroy()
    {
      if (_startPoint != null)
        _startPoint.UnregisterConnectedLineObject(gameObject);

      if (_endPoint != null)
        _endPoint.UnregisterConnectedLineObject(gameObject);
    }

    private void RefreshLine()
    {
      if (_lineRenderer == null)
      {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
          return;
      }

      if (_startPoint == null || _endPoint == null)
      {
        _lineRenderer.enabled = false;
        _lineRenderer.positionCount = 0;
        return;
      }

      Vector3 start = _startPoint.transform.position;
      Vector3 end = _endPoint.transform.position;

      _lineRenderer.enabled = true;

      if (!_usePhysicsSimulation || !Application.isPlaying)
      {
        RenderAnalytic(start, end);
        return;
      }

      RenderPhysics(start, end);
    }

    private void RenderAnalytic(Vector3 start, Vector3 end)
    {
      EnsureBuffers();

      int count = _points.Length;
      for (int i = 0; i < count; i++)
      {
        float t = i / (count - 1f);
        Vector3 point = Vector3.Lerp(start, end, t);
        point += Vector3.down * (Mathf.Sin(t * Mathf.PI) * _sagAmount * (1f + _elasticity));
        _points[i] = point;
      }

      _lineRenderer.positionCount = count;
      _lineRenderer.SetPositions(_points);
      _simulationInitialized = false;
    }

    private void RenderPhysics(Vector3 start, Vector3 end)
    {
      EnsureBuffers();
      int count = _points.Length;

      if (!_simulationInitialized ||
          (_points[0] - start).sqrMagnitude > 0.5f ||
          (_points[count - 1] - end).sqrMagnitude > 0.5f)
      {
        InitializeSimulation(start, end);
      }

      int last = count - 1;
      _points[0] = start;
      _points[last] = end;
      _previousPoints[0] = start;
      _previousPoints[last] = end;

      float deltaTime = Mathf.Clamp(Time.deltaTime, 1f / 240f, 1f / 30f);
      float stepDt = deltaTime / Mathf.Max(1, _simulationStepsPerFrame);
      int stepCount = Mathf.Max(1, _simulationStepsPerFrame);

      Vector3 gravityVector = Physics.gravity.sqrMagnitude > Epsilon ? Physics.gravity : Vector3.down * 9.81f;
      Vector3 gravityStep = gravityVector * _gravityScale * stepDt * stepDt;
      float dampingFactor = 1f - _velocityDamping;

      float totalLength = Vector3.Distance(start, end) + _slackLength;
      float targetSegmentLength = totalLength / (count - 1);

      for (int step = 0; step < stepCount; step++)
      {
        Integrate(gravityStep, dampingFactor);

        for (int i = 0; i < _solverIterations; i++)
        {
          SolveDistanceConstraints(targetSegmentLength, start, end);
          if (_collideWithWorld)
            ResolveCollisions();
        }
      }

      _lineRenderer.positionCount = count;
      _lineRenderer.SetPositions(_points);
    }

    private void EnsureBuffers()
    {
      int count = Mathf.Max(2, _segments);
      if (_points.Length == count)
        return;

      _points = new Vector3[count];
      _previousPoints = new Vector3[count];
      _simulationInitialized = false;
    }

    private void InitializeSimulation(Vector3 start, Vector3 end)
    {
      int count = _points.Length;
      for (int i = 0; i < count; i++)
      {
        float t = i / (count - 1f);
        Vector3 point = Vector3.Lerp(start, end, t);
        point += Vector3.down * (Mathf.Sin(t * Mathf.PI) * _sagAmount * (1f + _elasticity));

        _points[i] = point;
        _previousPoints[i] = point;
      }

      _simulationInitialized = true;
    }

    private void Integrate(Vector3 gravityStep, float dampingFactor)
    {
      for (int i = 1; i < _points.Length - 1; i++)
      {
        Vector3 current = _points[i];
        Vector3 velocity = (current - _previousPoints[i]) * dampingFactor;
        _previousPoints[i] = current;
        _points[i] = current + velocity + gravityStep;
      }
    }

    private void SolveDistanceConstraints(float targetLength, Vector3 start, Vector3 end)
    {
      int last = _points.Length - 1;
      _points[0] = start;
      _points[last] = end;

      for (int i = 0; i < _points.Length - 1; i++)
      {
        Vector3 p1 = _points[i];
        Vector3 p2 = _points[i + 1];
        Vector3 delta = p2 - p1;
        float distance = delta.magnitude;
        if (distance < Epsilon)
          continue;

        float distanceError = distance - targetLength;
        // 탄성은 제약 강성을 낮춰 라인이 늘어날 수 있게 한다.
        float stiffness = Mathf.Lerp(1f, 0.1f, _elasticity);
        Vector3 correction = delta * (distanceError / distance) * stiffness;

        if (i == 0)
        {
          p2 -= correction;
        }
        else if (i + 1 == last)
        {
          p1 += correction;
        }
        else
        {
          Vector3 half = correction * 0.5f;
          p1 += half;
          p2 -= half;
        }

        _points[i] = p1;
        _points[i + 1] = p2;
      }

      _points[0] = start;
      _points[last] = end;
    }

    private void ResolveCollisions()
    {
      for (int i = 1; i < _points.Length - 1; i++)
      {
        Vector3 previous = _previousPoints[i];
        Vector3 current = _points[i];
        Vector3 movement = current - previous;
        float movementDistance = movement.magnitude;

        if (movementDistance > Epsilon)
        {
          if (Physics.SphereCast(
                previous,
                _collisionRadius,
                movement / movementDistance,
                out RaycastHit hit,
                movementDistance,
                _collisionMask,
                _triggerInteraction))
          {
            current = hit.point + hit.normal * _collisionRadius;
            _previousPoints[i] = Vector3.Lerp(previous, current, 0.5f);
          }
        }

        int overlapCount = Physics.OverlapSphereNonAlloc(
          current,
          _collisionRadius,
          _overlapResults,
          _collisionMask,
          _triggerInteraction);

        for (int overlapIndex = 0; overlapIndex < overlapCount; overlapIndex++)
        {
          Collider collider = _overlapResults[overlapIndex];
          if (collider == null || !collider.enabled)
            continue;

          Vector3 closest = GetClosestPointSafe(collider, current);
          Vector3 toPoint = current - closest;
          float distance = toPoint.magnitude;

          if (distance < Epsilon)
          {
            Vector3 fallbackDirection = current - collider.bounds.center;
            if (fallbackDirection.sqrMagnitude < Epsilon)
              fallbackDirection = Vector3.up;

            current = closest + fallbackDirection.normalized * _collisionRadius;
            continue;
          }

          if (distance < _collisionRadius)
            current = closest + (toPoint / distance) * _collisionRadius;
        }

        _points[i] = current;
      }
    }

    private static Vector3 GetClosestPointSafe(Collider collider, Vector3 point)
    {
      if (collider == null)
        return point;

      if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
        return collider.ClosestPoint(point);

      if (collider is MeshCollider meshCollider && meshCollider.convex)
        return collider.ClosestPoint(point);

      // 지원하지 않는 콜라이더 타입(예: 볼록이 아닌 MeshCollider)용 폴백.
      return collider.bounds.ClosestPoint(point);
    }
  }
}
