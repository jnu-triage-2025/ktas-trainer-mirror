using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace TriageTrainer.Entity.LineConnection
{
  /// <summary>
  /// 물리적 라인의 공통 종단점이다. 구체적인 포인트가 상호작용과 도메인 고유 동작을 담당하고,
  /// 이 클래스는 연결 상태와 라인 표시 설정만 담당한다.
  /// </summary>
  public abstract class LineConnectionPoint : MonoBehaviour
  {
    [Header("Line Visual")]
    [SerializeField] private Material _lineMaterial;

    [Header("Runtime")]
    [SerializeField] private List<GameObject> _connectedLineObjects = new();

    [Header("Connection Capacity")]
    [SerializeField] private bool _allowMultipleConnections;

    protected virtual void OnValidate()
    {
    }

    public Material LineMaterial => _lineMaterial;

    public string ConnectionIdentifier
    {
      get
      {
        var networkObject = GetComponentInParent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
          return "net:" + networkObject.ObjectId + "/" + GetRelativePath(networkObject.transform);

        var staticEntity = GetComponentInParent<StaticObjectDisplayment>();
        if (staticEntity != null && !string.IsNullOrWhiteSpace(staticEntity.EntityIdentifier))
          return staticEntity.EntityIdentifier + "/" + GetRelativePath(staticEntity.transform);

        var patient = GetComponentInParent<PatientController>();
        if (patient != null && !string.IsNullOrWhiteSpace(patient.Identifier))
          return patient.Identifier + "/" + GetRelativePath(patient.transform);

        return string.Empty;
      }
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
    /// 라인은 기본적으로 동일한 구체적 포인트 타입끼리만 연결된다. 다른 타입을 의도적으로
    /// 지원하려는 포인트는 이 메서드를 재정의하여 명시적으로 허용해야 한다.
    /// </summary>
    public virtual bool CanConnectTo(LineConnectionPoint other) =>
      other != null && other.GetType() == GetType();

    public virtual bool CanPlayerCompleteConnection(PlayerController player, LineConnectionPoint other) => true;

    /// <summary>
    /// 공통 연결 검증이 통과된 뒤 포인트 고유의 연결 요구사항을 소모한다.
    /// 요구사항이 없는 포인트는 기본적으로 허용한다.
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

    private string GetRelativePath(Transform root)
    {
      return GetHierarchyPath(transform, root);
    }

    private static string GetHierarchyPath(Transform value, Transform stopBefore = null)
    {
      var names = new List<string>();
      for (var current = value; current != null && current != stopBefore; current = current.parent)
        names.Add(current.name + "[" + current.GetSiblingIndex() + "]");
      names.Reverse();
      return string.Join("/", names);
    }

    /// <summary>LineConnectionService 가 구체적 포인트 타입 분기를 통해 호출한다.</summary>
    public virtual void ApplyLineMaterial(LineRenderer lineRenderer)
    {
      if (lineRenderer != null && _lineMaterial != null)
        lineRenderer.sharedMaterial = _lineMaterial;
    }

    /// <summary>이 포인트에서 라인 연결 작업을 시작할 때 호출된다.</summary>
    public virtual void NotifyConnectionStarted() { }

    /// <summary>라인이 생성된 후 시작점에서 한 번 호출된다.</summary>
    public virtual void NotifyConnectionCompleted(LineConnectionPoint other) { }

    /// <summary>라인이 생성된 후 각 종단점마다 호출된다.</summary>
    public virtual void NotifyLineConnected(LineConnectionPoint other) { }

    /// <summary>종단점의 라인 하나가 제거될 때 호출된다.</summary>
    public virtual void NotifyLineDisconnected(LineConnectionPoint other) { }

    /// <summary>관전자 전용 로컬 수명 주기이며, 권한 있는 신호를 발생시켜서는 안 된다.</summary>
    public virtual void NotifyReplicatedLineConnected(LineConnectionPoint other) { }

    /// <summary>관전자 전용 로컬 수명 주기이며, 권한 있는 신호를 발생시켜서는 안 된다.</summary>
    public virtual void NotifyReplicatedLineDisconnected(LineConnectionPoint other) { }
  }
}
