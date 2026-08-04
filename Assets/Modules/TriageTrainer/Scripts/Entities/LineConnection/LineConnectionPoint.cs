using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace TriageTrainer.Entity.LineConnection
{
  /// <summary>
  /// Common endpoint for a physical line. Concrete points own their interaction
  /// and domain-specific behaviour; this class owns only connection state and
  /// line presentation configuration.
  /// </summary>
  public abstract class LineConnectionPoint : NetworkBehaviour
  {
    [Header("Line Visual")]
    [SerializeField] private Material _lineMaterial;

    [Header("Runtime")]
    [SerializeField] private List<GameObject> _connectedLineObjects = new();

    [Header("Connection Capacity")]
    [SerializeField] private bool _allowMultipleConnections;

    public NetworkObject OwningNetworkObject => GetComponentInParent<NetworkObject>();
    public Material LineMaterial => _lineMaterial;

    public override void OnStartClient()
    {
      base.OnStartClient();
      if (!IsServerStarted)
        CmdRequestAuthoritativeTopologySnapshot();
    }

    public bool HasAnyConnection
    {
      get
      {
        for (int i = _connectedLineObjects.Count - 1; i >= 0; i--)
        {
          if (_connectedLineObjects[i] != null)
            return true;

          _connectedLineObjects.RemoveAt(i);
        }

        return false;
      }
    }

    public bool CanAcceptAdditionalConnection => _allowMultipleConnections || !HasAnyConnection;

    /// <summary>
    /// A line connects only matching concrete point types by default. A point
    /// that intentionally supports another type must opt in by overriding this.
    /// </summary>
    public virtual bool CanConnectTo(LineConnectionPoint other) =>
      other != null && other.GetType() == GetType();

    public virtual bool CanPlayerCompleteConnection(PlayerController player, LineConnectionPoint other) => true;

    /// <summary>
    /// Consumes a point-specific connection requirement after common connection
    /// validation succeeds. Points without a requirement accept by default.
    /// </summary>
    public virtual bool TryConsumeConnectionRequirement(PlayerController player) => true;

    public void SetAllowsMultipleConnections(bool allow) => _allowMultipleConnections = allow;

    public void RegisterConnectedLineObject(GameObject lineObject)
    {
      if (lineObject != null && !_connectedLineObjects.Contains(lineObject))
        _connectedLineObjects.Add(lineObject);
    }

    public void UnregisterConnectedLineObject(GameObject lineObject)
    {
      if (lineObject != null)
        _connectedLineObjects.Remove(lineObject);
    }

    public bool TryGetAnyConnectedLineObject(out GameObject lineObject)
    {
      for (int i = 0; i < _connectedLineObjects.Count; i++)
      {
        if (_connectedLineObjects[i] == null)
          continue;

        lineObject = _connectedLineObjects[i];
        return true;
      }

      lineObject = null;
      return false;
    }

    public bool IsPhysicallyConnectedTo(LineConnectionPoint other)
    {
      if (other == null)
        return false;

      for (int i = 0; i < _connectedLineObjects.Count; i++)
      {
        var runtime = _connectedLineObjects[i] != null
          ? _connectedLineObjects[i].GetComponent<LineConnectionRuntime>()
          : null;
        if (runtime == null)
          continue;
        if ((ReferenceEquals(runtime.StartPoint, this) && ReferenceEquals(runtime.EndPoint, other))
            || (ReferenceEquals(runtime.EndPoint, this) && ReferenceEquals(runtime.StartPoint, other)))
          return true;
      }

      return false;
    }

    public bool TryGetConnectedLineObjectTo(LineConnectionPoint other, out GameObject lineObject)
    {
      lineObject = null;
      if (other == null)
        return false;
      for (int i = 0; i < _connectedLineObjects.Count; i++)
      {
        var candidate = _connectedLineObjects[i];
        var runtime = candidate != null ? candidate.GetComponent<LineConnectionRuntime>() : null;
        if (runtime == null)
          continue;
        if ((ReferenceEquals(runtime.StartPoint, this) && ReferenceEquals(runtime.EndPoint, other))
            || (ReferenceEquals(runtime.EndPoint, this) && ReferenceEquals(runtime.StartPoint, other)))
        {
          lineObject = candidate;
          return true;
        }
      }
      return false;
    }

    public void RequestAuthoritativeConnection(LineConnectionPoint startPoint)
    {
      if (startPoint == null || ReferenceEquals(startPoint, this))
        return;

      if (IsClientInitialized)
        CmdRequestAuthoritativeConnection(startPoint);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestAuthoritativeConnection(
      LineConnectionPoint startPoint,
      NetworkConnection sender = null)
    {
      ResolveConnectionService()?.TryCompleteConnectionOnServer(startPoint, this, sender);
    }

    public void RequestAuthoritativeDisconnect(PlayerController player)
    {
      if (IsServerStarted)
        ResolveConnectionService()?.DisconnectFromPointOnServer(this, player?.Owner);
      else if (IsClientInitialized)
        CmdRequestAuthoritativeDisconnect();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestAuthoritativeDisconnect(NetworkConnection sender = null)
    {
      ResolveConnectionService()?.DisconnectFromPointOnServer(this, sender);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestAuthoritativeTopologySnapshot(NetworkConnection sender = null)
    {
      ResolveConnectionService()?.ReplayAuthoritativeTopologySnapshot(this, sender);
    }

    internal void SendTopologySnapshotBegin(NetworkConnection target) =>
      TargetTopologySnapshotBegin(target);

    internal void SendTopologySnapshotPair(
      NetworkConnection target,
      LineConnectionPoint first,
      LineConnectionPoint second) =>
      TargetTopologySnapshotPair(target, first, second);

    internal void SendTopologySnapshotEnd(NetworkConnection target) =>
      TargetTopologySnapshotEnd(target);

    [TargetRpc]
    private void TargetTopologySnapshotBegin(NetworkConnection target)
    {
      ResolveConnectionService()?.BeginReplicatedTopologySnapshot();
    }

    [TargetRpc]
    private void TargetTopologySnapshotPair(
      NetworkConnection target,
      LineConnectionPoint first,
      LineConnectionPoint second)
    {
      ResolveConnectionService()?.ApplyReplicatedSnapshotPair(first, second);
    }

    [TargetRpc]
    private void TargetTopologySnapshotEnd(NetworkConnection target)
    {
      ResolveConnectionService()?.EndReplicatedTopologySnapshot();
    }

    internal void BroadcastAuthoritativeConnection(LineConnectionPoint other)
    {
      if (other != null)
        RpcApplyAuthoritativeConnection(other);
    }

    [ObserversRpc]
    private void RpcApplyAuthoritativeConnection(LineConnectionPoint other)
    {
      if (!IsServerStarted)
        ResolveConnectionService()?.ApplyReplicatedConnection(this, other);
    }

    internal void BroadcastAuthoritativeDisconnect(LineConnectionPoint other)
    {
      if (other != null)
        RpcApplyAuthoritativeDisconnect(other);
    }

    [ObserversRpc]
    private void RpcApplyAuthoritativeDisconnect(LineConnectionPoint other)
    {
      if (!IsServerStarted)
        ResolveConnectionService()?.ApplyReplicatedDisconnect(this, other);
    }

    private static LineConnectionService ResolveConnectionService()
    {
      var service = FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
      return service != null
        ? service
        : FindAnyObjectByType<LineConnectionService>(FindObjectsInactive.Include);
    }

    /// <summary>Called by LineConnectionService through a concrete point type branch.</summary>
    public virtual void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      if (lineRenderer != null && _lineMaterial != null)
        lineRenderer.sharedMaterial = _lineMaterial;
    }

    /// <summary>Called when this point begins a line-connection operation.</summary>
    public virtual void NotifyConnectionStarted() { }

    /// <summary>Called once from the start point after a line is created.</summary>
    public virtual void NotifyConnectionCompleted(LineConnectionPoint other) { }

    /// <summary>Called for each endpoint after a line is created.</summary>
    public virtual void NotifyLineConnected(LineConnectionPoint other) { }

    /// <summary>Called for each endpoint when one of its lines is removed.</summary>
    public virtual void NotifyLineDisconnected(LineConnectionPoint other) { }

    /// <summary>Observer-only local lifecycle; must not emit authoritative signals.</summary>
    public virtual void NotifyReplicatedLineConnected(LineConnectionPoint other) { }

    /// <summary>Observer-only local lifecycle; must not emit authoritative signals.</summary>
    public virtual void NotifyReplicatedLineDisconnected(LineConnectionPoint other) { }
  }
}
