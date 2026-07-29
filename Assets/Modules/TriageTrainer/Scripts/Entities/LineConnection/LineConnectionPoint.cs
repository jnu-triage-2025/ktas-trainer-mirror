using System.Collections.Generic;
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
  }
}
