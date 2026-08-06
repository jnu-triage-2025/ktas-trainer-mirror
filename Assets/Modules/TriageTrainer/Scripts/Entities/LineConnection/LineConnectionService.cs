using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using TriageTrainer.Entity.AEDLine;
using TriageTrainer.Entity.OxyLine;
using TriageTrainer.Entity.SuctionLine;
using TriageTrainer.Entity.IntravenousLine;
using UnityEngine;
using UnityEngine.Serialization;

namespace TriageTrainer.Entity.LineConnection
{
  public class LineConnectionService : MonoBehaviour
  {
    [Serializable]
    private sealed class PendingConnectionContext
    {
      public PlayerController Player;
      public LineConnectionPoint StartPoint;
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

    [Header("Hierarchy")]
    [SerializeField] private Transform _linesRoot;

    [Header("Line Visual")]
    [SerializeField, Range(0f, 1f)] private float _intravenousLineElasticity = 0.21f;
    [SerializeField, Range(0f, 1f)] private float _aedLineElasticity = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _oxyLineElasticity = 0.2f;
    [SerializeField, Range(0f, 1f)] private float _suctionLineElasticity = 0.13f;
    [FormerlySerializedAs("_lineWidth")]
    [SerializeField, Min(0.001f)] private float _intravenousLineWidth = 0.03f;
    [SerializeField, Min(0.001f)] private float _aedLineWidth = 0.03f;
    [SerializeField, Min(0.001f)] private float _oxyLineWidth = 0.035f;
    [SerializeField, Min(0.001f)] private float _suctionLineWidth = 0.04f;
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
    private bool _applyingReplicatedSnapshot;

    private void Awake()
    {
      EnsureLinesRoot();
    }

    private void Update()
    {
      CleanupInvalidPendingConnections();
    }

    private void OnValidate()
    {
      EnsureLinesRoot();
      _lineSegments = Mathf.Max(2, _lineSegments);
      _lineSagAmount = Mathf.Max(0f, _lineSagAmount);
      _intravenousLineElasticity = Mathf.Clamp01(_intravenousLineElasticity);
      _aedLineElasticity = Mathf.Clamp01(_aedLineElasticity);
      _oxyLineElasticity = Mathf.Clamp01(_oxyLineElasticity);
      _suctionLineElasticity = Mathf.Clamp01(_suctionLineElasticity);
      _intravenousLineWidth = Mathf.Max(0.001f, _intravenousLineWidth);
      _aedLineWidth = Mathf.Max(0.001f, _aedLineWidth);
      _oxyLineWidth = Mathf.Max(0.001f, _oxyLineWidth);
      _suctionLineWidth = Mathf.Max(0.001f, _suctionLineWidth);
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
    /// Completes a generic line connection after the start point consumes any
    /// point-specific requirement it defines.
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
          endPoint.RequestAuthoritativeConnection(startPoint);
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

    internal bool TryCompleteConnectionOnServer(
      LineConnectionPoint startPoint,
      LineConnectionPoint endPoint,
      NetworkConnection sender)
    {
      if (!TryResolveSenderPlayer(sender, out var player)
          || !ValidateAuthoritativeConnection(player, startPoint, endPoint, sender, out _)
          || !startPoint.TryConsumeConnectionRequirement(player)
          || !CreateAndRegisterConnection(startPoint, endPoint))
        return false;

      AddAuthoritativePair(startPoint, endPoint);

      using (ScenarioSignalPlayerContext.Push(sender))
      {
        startPoint.NotifyConnectionCompleted(endPoint);
        startPoint.NotifyLineConnected(endPoint);
        endPoint.NotifyLineConnected(startPoint);
      }

      endPoint.BroadcastAuthoritativeConnection(startPoint);
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
      if (!startPoint.IsSpawned || !endPoint.IsSpawned)
      {
        reason = "endpoint is not server-spawned";
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
          || !UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor)
          || descriptor == null
          || string.IsNullOrWhiteSpace(descriptor.Identifier)
          || !PlayerTagService.HasTag(descriptor.Identifier, "nurse_c"))
      {
        reason = "sender is not nurse_c";
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
        point.RequestAuthoritativeDisconnect(player);
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

    internal bool DisconnectFromPointOnServer(LineConnectionPoint point, NetworkConnection sender)
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
        point.BroadcastAuthoritativeDisconnect(other);
        destroyedAny = true;
      }
      return destroyedAny;
    }

    internal void ReplayAuthoritativeTopologySnapshot(
      LineConnectionPoint rpcAnchor,
      NetworkConnection target)
    {
      if (rpcAnchor == null || target == null || !target.IsValid)
        return;

      PruneInvalidAuthoritativePairs();
      rpcAnchor.SendTopologySnapshotBegin(target);
      for (int i = 0; i < _activeAuthoritativePairs.Count; i++)
      {
        var pair = _activeAuthoritativePairs[i];
        rpcAnchor.SendTopologySnapshotPair(target, pair.First, pair.Second);
      }
      rpcAnchor.SendTopologySnapshotEnd(target);
    }

    public void BeginReplicatedTopologySnapshot()
    {
      _applyingReplicatedSnapshot = true;
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
      for (int i = 0; i < _snapshotStalePairs.Count; i++)
      {
        var pair = _snapshotStalePairs[i];
        ApplyReplicatedDisconnect(pair.First, pair.Second);
      }
      _snapshotStalePairs.Clear();
      _applyingReplicatedSnapshot = false;
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
    /// Creates an authoritative connection without a player interaction or item
    /// consumption. This is reserved for newly installed CareZone equipment.
    /// </summary>
    public bool TryCreateAutomaticConnection(LineConnectionPoint startPoint, LineConnectionPoint endPoint)
    {
      if (startPoint == null || endPoint == null || ReferenceEquals(startPoint, endPoint)
          || startPoint.IsPhysicallyConnectedTo(endPoint)
          || !startPoint.CanAcceptAdditionalConnection || !endPoint.CanAcceptAdditionalConnection
          || !startPoint.CanConnectTo(endPoint) || !endPoint.CanConnectTo(startPoint)
          || !CanConnectBetweenDifferentOwners(startPoint, endPoint, out _)
          || (!InstanceFinder.IsOffline && (!InstanceFinder.IsServerStarted || !startPoint.IsSpawned || !endPoint.IsSpawned)))
        return false;

      if (!CreateAndRegisterConnection(startPoint, endPoint))
        return false;

      if (!InstanceFinder.IsOffline)
        AddAuthoritativePair(startPoint, endPoint);
      AddAutomaticPair(startPoint, endPoint);
      startPoint.NotifyLineConnected(endPoint);
      endPoint.NotifyLineConnected(startPoint);
      if (!InstanceFinder.IsOffline)
        startPoint.BroadcastAuthoritativeConnection(endPoint);
      return true;
    }

    /// <summary>Removes only the specified automatically managed endpoint pair.</summary>
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
        first.BroadcastAuthoritativeDisconnect(second);
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
        ApplyLineMaterial(startPoint, lineRenderer);
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

      Destroy(lineObject);

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

      var startNetworkObject = startPoint.OwningNetworkObject;
      var endNetworkObject = endPoint.OwningNetworkObject;

      if (startNetworkObject != null && endNetworkObject != null)
      {
        bool ok = !ReferenceEquals(startNetworkObject, endNetworkObject);
        if (!ok)
          reason = "same network object";
        return ok;
      }

      return true;
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
        var bed = startPoint.GetComponentInParent<TriageTrainer.Entity.MovingPatientBedController>()
                  ?? endPoint.GetComponentInParent<TriageTrainer.Entity.MovingPatientBedController>();
        patient = bed?.ReposedTarget as TriageTrainer.Entity.PatientController;
      }
      salinePoint = startPoint as IntravenousLineConnectionPoint;
      if (salinePoint == null || !string.Equals(salinePoint.Identifier, "connect_cannula_and_ns1", StringComparison.Ordinal))
        salinePoint = endPoint as IntravenousLineConnectionPoint;
      return patient != null
             && (string.Equals(patient.Identifier, "patient_b", StringComparison.Ordinal)
                 || string.Equals(patient.Identifier, "patient_c", StringComparison.Ordinal))
             && salinePoint != null
             && string.Equals(salinePoint.Identifier, "connect_cannula_and_ns1", StringComparison.Ordinal);
    }

    public static bool IsExactPatientNormalSalineEndpointPair(
      TriageTrainer.Entity.PatientController patient,
      IntravenousLineConnectionPoint salinePoint,
      LineConnectionPoint startPoint,
      LineConnectionPoint endPoint) =>
      patient != null && salinePoint != null
      && !ReferenceEquals(startPoint, endPoint)
      && ((ReferenceEquals(startPoint, salinePoint) && ReferenceEquals(endPoint, patient.IvAttachmentPoint))
          || (ReferenceEquals(endPoint, salinePoint) && ReferenceEquals(startPoint, patient.IvAttachmentPoint)));

    private void ApplyLineMaterial(LineConnectionPoint point, LineRenderer lineRenderer)
    {
      if (point != null && point.LineMaterial != null)
      {
        lineRenderer.sharedMaterial = point.LineMaterial;
        return;
      }

      point?.ApplyLineMaterial(lineRenderer);
    }

    private float GetLineElasticity(LineConnectionPoint point)
    {
      return point switch
      {
        IntravenousLineConnectionPoint => _intravenousLineElasticity,
        AEDLineConnectionPoint => _aedLineElasticity,
        OxyLineConnectionPoint => _oxyLineElasticity,
        SuctionLineConnectionPoint => _suctionLineElasticity,
        _ => 0f,
      };
    }

    private float GetLineWidth(LineConnectionPoint point)
    {
      return point switch
      {
        IntravenousLineConnectionPoint => _intravenousLineWidth,
        AEDLineConnectionPoint => _aedLineWidth,
        OxyLineConnectionPoint => _oxyLineWidth,
        SuctionLineConnectionPoint => _suctionLineWidth,
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
        // Elasticity reduces constraint stiffness, allowing the line to stretch.
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

      // Fallback for unsupported collider types (eg non-convex MeshCollider).
      return collider.bounds.ClosestPoint(point);
    }
  }
}
