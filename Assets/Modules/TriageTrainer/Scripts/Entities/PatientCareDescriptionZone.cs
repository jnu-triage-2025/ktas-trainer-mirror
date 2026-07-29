using System.Collections.Generic;
using TriageTrainer.Scenario;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 환자 케어에 사용할 벽면 장비의 인식 범위를 정의한다.
  ///
  /// <para>구역에 들어온 환자에게 구역 안의 벽면 석션과 산소 유량계를
  /// 환자 측 장비 역참조로 연결한다. 장비는 의도상 하나지만, 배치 오류나
  /// 확장 시에도 누락되지 않도록 감지 결과는 목록으로 보관한다.</para>
  /// </summary>
  [RequireComponent(typeof(BoxCollider))]
  public sealed class PatientCareDescriptionZone : MonoBehaviour
  {
    [Header("Recognition Area")]
    [SerializeField] private Vector3 _size = new(2.4f, 3.5f, 3f);
    [SerializeField] private Vector3 _center = new(0f, 1.5f, 0f);
    [Tooltip("비활성(미설치) 장비를 환자에게 연결할지 여부입니다. 실제 사용 판정 Zone은 false를 사용해야 합니다.")]
    [SerializeField] private bool _includeUnattachedEquipment = false;
    [Tooltip("Zone 안에 같은 장비가 여러 개면 환자 연결을 무효화하고 배치 오류를 보고합니다.")]
    [SerializeField] private bool _requireExactlyOneEquipment = true;
    [Tooltip("환자 장비 판정 전에 환자가 이 Zone 안의 positioning point에 고정된 침대에 연결되어 있어야 합니다.")]
    [SerializeField] private bool _requireBedSnapForPatient = true;
    [SerializeField] private string _identifier;

    private readonly Dictionary<PatientController, int> _patientColliderCounts = new();
    private readonly Dictionary<MovingPatientBedController, int> _bedColliderCounts = new();
    private readonly HashSet<MovingPatientBedController> _snappedBeds = new();
    private readonly List<WallAttachedWallSuction> _wallSuction = new();
    private readonly List<WallAttachedOxyflowmeter> _oxyflowmeters = new();
    private bool _warnedMultipleWallSuction;
    private bool _warnedMultipleOxyflowmeter;
    private bool _warnedMissingEquipment;
    private bool _warnedMissingPositioningPoint;
    private PatientController _activePatient;
    private WallAttachedWallSuction _connectedWallSuction;
    private WallAttachedOxyflowmeter _connectedOxyflowmeter;
    private BoxCollider _collider;

    public IReadOnlyList<WallAttachedWallSuction> WallSuction => _wallSuction;
    public IReadOnlyList<WallAttachedOxyflowmeter> Oxyflowmeters => _oxyflowmeters;
    public string Identifier => _identifier;

    public void SetIdentifierForEditor(string identifier)
    {
      _identifier = identifier == null ? string.Empty : identifier.Trim();
    }

    public void ConfigureSize(Vector3 size)
    {
      _size = new Vector3(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y), Mathf.Max(0.01f, size.z));
      ApplyCollider();
    }

    public void ConfigureArea(Vector3 center, Vector3 size)
    {
      _center = center;
      ConfigureSize(size);
    }

    private void Awake()
    {
      _collider = GetComponent<BoxCollider>();
      _collider.isTrigger = true;
      ApplyCollider();
      RefreshEquipment();
      WarnIfConfigurationInvalid();
    }

    private void OnValidate()
    {
      _size.x = Mathf.Max(0.01f, _size.x);
      _size.y = Mathf.Max(0.01f, _size.y);
      _size.z = Mathf.Max(0.01f, _size.z);
      _collider ??= GetComponent<BoxCollider>();
      ApplyCollider();
    }

    private void ApplyCollider()
    {
      if (_collider == null) return;
      _collider.isTrigger = true;
      _collider.center = _center;
      _collider.size = _size;
    }

    private void OnTriggerEnter(Collider other)
    {
      PatientController patient = other.GetComponentInParent<PatientController>();
      if (patient != null)
      {
        if (_activePatient != null && !ReferenceEquals(_activePatient, patient))
        {
          Debug.LogWarning($"[PatientCareDescriptionZone] '{Identifier}' already owns patient '{_activePatient.Identifier}'; ignoring '{patient.Identifier}'.", this);
          return;
        }
        _patientColliderCounts.TryGetValue(patient, out int count);
        _patientColliderCounts[patient] = count + 1;
        if (count == 0)
        {
          _activePatient = patient;
          Connect(patient);
          TriageWorldInteractionSignals.RaiseCareZonePatientEntered(Identifier, patient.Identifier);
        }
        return;
      }

      MovingPatientBedController bed = other.GetComponentInParent<MovingPatientBedController>();
      if (bed == null) return;
      _bedColliderCounts.TryGetValue(bed, out int bedCount);
      _bedColliderCounts[bed] = bedCount + 1;
      if (bedCount == 0)
        TriageWorldInteractionSignals.RaiseCareZoneBedEntered(Identifier, bed.Identifier);
      UpdateBedSnapSignal(bed);
    }

    private void OnTriggerStay(Collider other)
    {
      PatientController patient = other.GetComponentInParent<PatientController>();
      if (patient != null && _patientColliderCounts.ContainsKey(patient))
        Connect(patient);

      MovingPatientBedController bed = other.GetComponentInParent<MovingPatientBedController>();
      if (bed != null && _bedColliderCounts.ContainsKey(bed))
        UpdateBedSnapSignal(bed);
    }

    private void OnTriggerExit(Collider other)
    {
      PatientController patient = other.GetComponentInParent<PatientController>();
      if (patient != null)
      {
        if (!_patientColliderCounts.TryGetValue(patient, out int count)) return;
        if (count > 1)
        {
          _patientColliderCounts[patient] = count - 1;
          return;
        }
        _patientColliderCounts.Remove(patient);
        if (ReferenceEquals(_activePatient, patient))
          _activePatient = null;
        if (ReferenceEquals(patient.ConnectedWallSuction, _connectedWallSuction))
          patient.SetConnectedWallSuctionConnections(null);
        if (ReferenceEquals(patient.ConnectedOxyflowmeter, _connectedOxyflowmeter))
          patient.SetConnectedOxyflowmeterConnections(null);
        _connectedWallSuction = null;
        _connectedOxyflowmeter = null;
        TriageWorldInteractionSignals.RaiseCareZonePatientExited(Identifier, patient.Identifier);
        return;
      }

      MovingPatientBedController bed = other.GetComponentInParent<MovingPatientBedController>();
      if (bed == null || !_bedColliderCounts.TryGetValue(bed, out int bedCount)) return;
      if (bedCount > 1)
      {
        _bedColliderCounts[bed] = bedCount - 1;
        return;
      }
      _bedColliderCounts.Remove(bed);
      _snappedBeds.Remove(bed);
      TriageWorldInteractionSignals.RaiseCareZoneBedExited(Identifier, bed.Identifier);
    }

    private void UpdateBedSnapSignal(MovingPatientBedController bed)
    {
      MovingPatientBedPositioningPoint point = bed.LatchedPositioningPoint;
      bool snappedInThisZone = point != null && IsPointInside(point.transform.position);
      if (!snappedInThisZone)
      {
        _snappedBeds.Remove(bed);
        return;
      }

      if (_snappedBeds.Add(bed))
        TriageWorldInteractionSignals.RaiseCareZoneBedSnapped(Identifier, bed.Identifier, point.Identifier);
    }

    private bool IsPointInside(Vector3 worldPosition)
    {
      Vector3 local = transform.InverseTransformPoint(worldPosition) - _center;
      return Mathf.Abs(local.x) <= _size.x * 0.5f
        && Mathf.Abs(local.y) <= _size.y * 0.5f
        && Mathf.Abs(local.z) <= _size.z * 0.5f;
    }

    private void Connect(PatientController patient)
    {
      if (_requireBedSnapForPatient && !IsPatientSupportedInZone(patient))
      {
        patient.SetConnectedWallSuctionConnections(null);
        patient.SetConnectedOxyflowmeterConnections(null);
        return;
      }
      RefreshEquipment();
      WarnIfConfigurationInvalid();
      IReadOnlyList<WallAttachedWallSuction> suction = GetUsableWallSuctionSources();
      IReadOnlyList<WallAttachedOxyflowmeter> flowmeter = GetUsableOxyflowmeterSources();
      _connectedWallSuction = suction != null && suction.Count == 1 ? suction[0] : null;
      _connectedOxyflowmeter = flowmeter != null && flowmeter.Count == 1 ? flowmeter[0] : null;
      patient.SetConnectedWallSuctionConnections(suction);
      patient.SetConnectedOxyflowmeterConnections(flowmeter);
    }

    private IReadOnlyList<WallAttachedWallSuction> GetUsableWallSuctionSources()
    {
      if (!_requireExactlyOneEquipment || _wallSuction.Count <= 1)
      {
        _warnedMultipleWallSuction = false;
        return _wallSuction;
      }

      if (!_warnedMultipleWallSuction)
      {
        Debug.LogWarning($"[PatientCareDescriptionZone] '{Identifier}' has {_wallSuction.Count} active wall_suction objects; patient connection is suppressed until exactly one remains.", this);
        _warnedMultipleWallSuction = true;
      }
      return null;
    }

    private IReadOnlyList<WallAttachedOxyflowmeter> GetUsableOxyflowmeterSources()
    {
      if (!_requireExactlyOneEquipment || _oxyflowmeters.Count <= 1)
      {
        _warnedMultipleOxyflowmeter = false;
        return _oxyflowmeters;
      }

      if (!_warnedMultipleOxyflowmeter)
      {
        Debug.LogWarning($"[PatientCareDescriptionZone] '{Identifier}' has {_oxyflowmeters.Count} active oxyflowmeter objects; patient connection is suppressed until exactly one remains.", this);
        _warnedMultipleOxyflowmeter = true;
      }
      return null;
    }

    private void RefreshEquipment()
    {
      _wallSuction.Clear();
      _oxyflowmeters.Clear();
      Vector3 scale = transform.lossyScale;
      Vector3 halfExtents = Vector3.Scale(_size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.5f;
      Collider[] hits = Physics.OverlapBox(transform.TransformPoint(_center), halfExtents, transform.rotation);
      for (int i = 0; i < hits.Length; i++)
      {
        WallAttachedWallSuction suction = hits[i].GetComponentInParent<WallAttachedWallSuction>();
        if (suction != null && (_includeUnattachedEquipment || suction.IsAttached) && !_wallSuction.Contains(suction)) _wallSuction.Add(suction);
        WallAttachedOxyflowmeter flowmeter = hits[i].GetComponentInParent<WallAttachedOxyflowmeter>();
        if (flowmeter != null && (_includeUnattachedEquipment || flowmeter.IsAttached) && !_oxyflowmeters.Contains(flowmeter)) _oxyflowmeters.Add(flowmeter);
      }
    }

    private bool IsPatientSupportedInZone(PatientController patient)
    {
      MovingPatientBedController bed = patient.CurrentBed;
      MovingPatientBedPositioningPoint point = bed == null ? null : bed.LatchedPositioningPoint;
      return bed != null && point != null && IsPointInside(point.transform.position);
    }

    private void WarnIfConfigurationInvalid()
    {
      if (_wallSuction.Count == 0 || _oxyflowmeters.Count == 0)
      {
        if (!_warnedMissingEquipment)
        {
          Debug.LogWarning($"[PatientCareDescriptionZone] '{Identifier}' requires one active wall_suction and one active oxyflowmeter.", this);
          _warnedMissingEquipment = true;
        }
      }
      else _warnedMissingEquipment = false;

      MovingPatientBedPositioningPoint[] points = FindObjectsByType<MovingPatientBedPositioningPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      bool hasPoint = false;
      for (int i = 0; i < points.Length; i++)
        if (IsPointInside(points[i].transform.position)) { hasPoint = true; break; }
      if (!hasPoint && !_warnedMissingPositioningPoint)
      {
        Debug.LogWarning($"[PatientCareDescriptionZone] '{Identifier}' has no positioning point inside its bounds.", this);
        _warnedMissingPositioningPoint = true;
      }
      else if (hasPoint) _warnedMissingPositioningPoint = false;
    }

    private void OnDrawGizmos()
    {
      DrawGizmo(false);
    }

    private void OnDrawGizmosSelected()
    {
      DrawGizmo(true);
    }

    private void DrawGizmo(bool selected)
    {
      Matrix4x4 previousMatrix = Gizmos.matrix;
      Color previousColor = Gizmos.color;
      Gizmos.matrix = transform.localToWorldMatrix;
      Gizmos.color = new Color(0.2f, 1f, 0.4f, selected ? 0.28f : 0.12f);
      Gizmos.DrawCube(_center, _size);
      Gizmos.color = new Color(0.2f, 1f, 0.4f, selected ? 1f : 0.65f);
      Gizmos.DrawWireCube(_center, _size);
      Gizmos.matrix = previousMatrix;
      Gizmos.color = previousColor;

#if UNITY_EDITOR
      if (selected)
        Handles.Label(transform.TransformPoint(_center + Vector3.up * (_size.y * 0.5f + 0.15f)),
          string.IsNullOrWhiteSpace(_identifier) ? "Patient Care Description Zone" : _identifier);
#endif
    }
  }
}
