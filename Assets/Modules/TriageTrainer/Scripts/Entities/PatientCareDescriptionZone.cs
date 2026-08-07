using System.Collections.Generic;
using TriageTrainer.Entity.LineConnection;
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
    public event System.Action<PatientController> PatientEntered;
    public event System.Action<PatientController> PatientExited;
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

    /// <summary>환자 진입 폴링 주기(초).</summary>
    private const float PatientPollIntervalSeconds = 0.15f;

    private readonly Dictionary<PatientController, int> _patientColliderCounts = new();
    // 폴링 경로가 추적하는 환자 집합과 스크래치 버퍼.
    private readonly HashSet<PatientController> _polledPatients = new();
    private readonly HashSet<PatientController> _polledInsideScratch = new();
    private readonly List<PatientController> _polledExitScratch = new();
    private float _nextPatientPollTime;
    private readonly Dictionary<MovingPatientBedController, int> _bedColliderCounts = new();
    private readonly HashSet<MovingPatientBedController> _snappedBeds = new();
    private readonly List<WallAttachedWallSuction> _wallSuction = new();
    private readonly List<WallAttachedOxyflowmeter> _oxyflowmeters = new();
    private readonly HashSet<WallAttachedWallSuction> _newlyInstalledWallSuction = new();
    private readonly HashSet<WallAttachedOxyflowmeter> _newlyInstalledOxyflowmeters = new();
    private readonly HashSet<string> _warnedAutomaticLineFailures = new();
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
    public PatientController CurrentPatient => _activePatient;

    public bool ContainsWorldPosition(Vector3 worldPosition)
    {
      Vector3 local = transform.InverseTransformPoint(worldPosition) - _center;
      return Mathf.Abs(local.x) <= _size.x * 0.5f
        && Mathf.Abs(local.y) <= _size.y * 0.5f
        && Mathf.Abs(local.z) <= _size.z * 0.5f;
    }

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

    private void OnEnable()
    {
      WallAttachedWallSuction.AttachmentStateChanged += OnWallSuctionAttachmentStateChanged;
      WallAttachedOxyflowmeter.AttachmentStateChanged += OnOxyflowmeterAttachmentStateChanged;
      WallAttachedWallSuction.InstallationConfirmed += OnWallSuctionInstallationConfirmed;
      WallAttachedOxyflowmeter.InstallationConfirmed += OnOxyflowmeterInstallationConfirmed;
      TriageWorldInteractionSignals.RaiseCareZoneEnabled(Identifier);
    }

    private void OnDisable()
    {
      WallAttachedWallSuction.AttachmentStateChanged -= OnWallSuctionAttachmentStateChanged;
      WallAttachedOxyflowmeter.AttachmentStateChanged -= OnOxyflowmeterAttachmentStateChanged;
      WallAttachedWallSuction.InstallationConfirmed -= OnWallSuctionInstallationConfirmed;
      WallAttachedOxyflowmeter.InstallationConfirmed -= OnOxyflowmeterInstallationConfirmed;
      if (_activePatient != null)
        ReleaseEquipmentIfOwned(_activePatient);
      _activePatient = null;
      _patientColliderCounts.Clear();
      _polledPatients.Clear();
      _bedColliderCounts.Clear();
      _snappedBeds.Clear();
      _newlyInstalledWallSuction.Clear();
      _newlyInstalledOxyflowmeters.Clear();
      _warnedAutomaticLineFailures.Clear();
      TriageWorldInteractionSignals.RaiseCareZoneDisabled(Identifier);
    }

    private void OnWallSuctionAttachmentStateChanged(WallAttachedWallSuction equipment, bool attached)
    {
      if (!attached)
        _newlyInstalledWallSuction.Remove(equipment);
      RefreshActivePatientEquipment();
    }

    private void OnOxyflowmeterAttachmentStateChanged(WallAttachedOxyflowmeter equipment, bool attached)
    {
      if (!attached)
        _newlyInstalledOxyflowmeters.Remove(equipment);
      RefreshActivePatientEquipment();
    }

    private void OnWallSuctionInstallationConfirmed(WallAttachedWallSuction equipment)
    {
      if (equipment == null || !IsEquipmentInZone(equipment))
        return;
      _newlyInstalledWallSuction.Add(equipment);
      RefreshActivePatientEquipment();
    }

    private void OnOxyflowmeterInstallationConfirmed(WallAttachedOxyflowmeter equipment)
    {
      if (equipment == null || !IsEquipmentInZone(equipment))
        return;
      _newlyInstalledOxyflowmeters.Add(equipment);
      RefreshActivePatientEquipment();
    }

    private void RefreshActivePatientEquipment()
    {
      if (_activePatient != null)
        Connect(_activePatient);
      else
        RefreshEquipment();
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
        _patientColliderCounts.TryGetValue(patient, out int count);
        bool firstCollider = count == 0;
        _patientColliderCounts[patient] = count + 1;

        if (firstCollider)
          HandlePatientEntered(patient);
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
      if (patient != null && ReferenceEquals(_activePatient, patient) && _patientColliderCounts.ContainsKey(patient))
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
        _polledPatients.Remove(patient);
        HandlePatientExited(patient);
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

    private void Update()
    {
      if (Time.time < _nextPatientPollTime)
        return;
      _nextPatientPollTime = Time.time + PatientPollIntervalSeconds;
      PollPatients();
    }

    /// <summary>
    /// 물리 트리거 없이도 환자 진입/이탈을 감지하는 폴링 경로.
    /// 이 프로젝트의 환자·침대·Zone은 모두 Rigidbody 없이 transform 직접 갱신으로 이동하므로
    /// 정적 콜라이더 쌍에는 OnTriggerEnter/Exit 물리 이벤트가 발생하지 않는다.
    /// RefreshEquipment 와 같은 OverlapBox 질의로 구역 안 환자 집합을 주기적으로 비교하여
    /// 트리거 경로와 동일한 진입/체류/이탈 처리를 수행한다.
    /// </summary>
    private void PollPatients()
    {
      Vector3 scale = transform.lossyScale;
      Vector3 halfExtents = Vector3.Scale(_size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.5f;
      Collider[] hits = Physics.OverlapBox(transform.TransformPoint(_center), halfExtents, transform.rotation);

      _polledInsideScratch.Clear();
      for (int i = 0; i < hits.Length; i++)
      {
        PatientController patient = hits[i].GetComponentInParent<PatientController>();
        if (patient != null)
          _polledInsideScratch.Add(patient);
      }

      foreach (PatientController patient in _polledInsideScratch)
      {
        // 물리 트리거가 이미 추적 중이거나 이전 폴링에서 등록한 환자는 건너뛴다.
        if (_patientColliderCounts.ContainsKey(patient))
          continue;
        _patientColliderCounts[patient] = 1;
        _polledPatients.Add(patient);
        HandlePatientEntered(patient);
      }

      if (_polledPatients.Count > 0)
      {
        _polledExitScratch.Clear();
        foreach (PatientController patient in _polledPatients)
        {
          if (patient == null || !_polledInsideScratch.Contains(patient))
            _polledExitScratch.Add(patient);
        }
        foreach (PatientController patient in _polledExitScratch)
        {
          _polledPatients.Remove(patient);
          _patientColliderCounts.Remove(patient);
          HandlePatientExited(patient);
        }
      }

      // OnTriggerStay 대체: 활성 환자가 구역에 머무는 동안 장비 연결을 주기적으로 재평가한다.
      // 침대가 환자 진입 이후 positioning point 에 스냅되는 흐름에서도 연결이 성립해야 한다.
      if (_activePatient != null && _patientColliderCounts.ContainsKey(_activePatient))
        Connect(_activePatient);
    }

    private void HandlePatientEntered(PatientController patient)
    {
      if (_activePatient != null && !ReferenceEquals(_activePatient, patient))
      {
        Debug.LogWarning($"[PatientCareDescriptionZone] '{Identifier}' already owns patient '{_activePatient.Identifier}'; ignoring '{patient.Identifier}'.", this);
        PatientEntered?.Invoke(patient);
        return;
      }
      _activePatient = patient;
      Connect(patient);
      TriageWorldInteractionSignals.RaiseCareZonePatientEntered(Identifier, patient.Identifier);
      PatientEntered?.Invoke(patient);
    }

    private void HandlePatientExited(PatientController patient)
    {
      if (!ReferenceEquals(_activePatient, patient))
        return;

      _activePatient = null;
      PatientExited?.Invoke(patient);
      ReleaseEquipmentIfOwned(patient);
      TriageWorldInteractionSignals.RaiseCareZonePatientExited(Identifier, patient.Identifier);

      foreach (var occupant in _patientColliderCounts)
      {
        if (occupant.Key == null)
          continue;

        _activePatient = occupant.Key;
        Connect(_activePatient);
        PatientEntered?.Invoke(_activePatient);
        break;
      }
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
      WallAttachedWallSuction previousSuction = _connectedWallSuction;
      WallAttachedOxyflowmeter previousOxyflowmeter = _connectedOxyflowmeter;
      bool ownsPreviousSuction = previousSuction != null && ReferenceEquals(patient.ConnectedWallSuction, previousSuction);
      bool ownsPreviousOxyflowmeter = previousOxyflowmeter != null && ReferenceEquals(patient.ConnectedOxyflowmeter, previousOxyflowmeter);
      if (_requireBedSnapForPatient && !IsPatientSupportedInZone(patient))
      {
        if (ownsPreviousSuction || ownsPreviousOxyflowmeter)
          ReleaseEquipmentIfOwned(patient);
        else
        {
          _connectedWallSuction = null;
          _connectedOxyflowmeter = null;
        }
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
      ReconcileAutomaticLines(patient);
      if (ownsPreviousSuction && !ReferenceEquals(previousSuction, patient.ConnectedWallSuction))
      {
        DisconnectAutomaticSuctionLine(patient, previousSuction);
        TriageWorldInteractionSignals.RaiseCareZonePatientEquipmentDisconnected(Identifier, patient.Identifier, PatientController.EquipmentTypeWallSuction, previousSuction);
      }
      if (patient.ConnectedWallSuction != null && !ReferenceEquals(previousSuction, patient.ConnectedWallSuction))
        TriageWorldInteractionSignals.RaiseCareZonePatientEquipmentConnected(Identifier, patient.Identifier, PatientController.EquipmentTypeWallSuction, patient.ConnectedWallSuction);
      if (ownsPreviousOxyflowmeter && !ReferenceEquals(previousOxyflowmeter, patient.ConnectedOxyflowmeter))
      {
        DisconnectAutomaticOxyLine(patient, previousOxyflowmeter);
        TriageWorldInteractionSignals.RaiseCareZonePatientEquipmentDisconnected(Identifier, patient.Identifier, PatientController.EquipmentTypeOxyflowmeter, previousOxyflowmeter);
      }
      if (patient.ConnectedOxyflowmeter != null && !ReferenceEquals(previousOxyflowmeter, patient.ConnectedOxyflowmeter))
        TriageWorldInteractionSignals.RaiseCareZonePatientEquipmentConnected(Identifier, patient.Identifier, PatientController.EquipmentTypeOxyflowmeter, patient.ConnectedOxyflowmeter);
    }

    private void ReleaseEquipmentIfOwned(PatientController patient)
    {
      if (patient == null)
        return;

      if (_connectedWallSuction != null && ReferenceEquals(patient.ConnectedWallSuction, _connectedWallSuction))
      {
        DisconnectAutomaticSuctionLine(patient, _connectedWallSuction);
        TriageWorldInteractionSignals.RaiseCareZonePatientEquipmentDisconnected(Identifier, patient.Identifier, PatientController.EquipmentTypeWallSuction, _connectedWallSuction);
        patient.SetConnectedWallSuctionConnections(null);
      }
      if (_connectedOxyflowmeter != null && ReferenceEquals(patient.ConnectedOxyflowmeter, _connectedOxyflowmeter))
      {
        DisconnectAutomaticOxyLine(patient, _connectedOxyflowmeter);
        TriageWorldInteractionSignals.RaiseCareZonePatientEquipmentDisconnected(Identifier, patient.Identifier, PatientController.EquipmentTypeOxyflowmeter, _connectedOxyflowmeter);
        patient.SetConnectedOxyflowmeterConnections(null);
      }
      _connectedWallSuction = null;
      _connectedOxyflowmeter = null;
    }

    private void ReconcileAutomaticLines(PatientController patient)
    {
      if (patient == null || !IsPatientSupportedInZone(patient))
        return;

      WallAttachedOxyflowmeter oxyflowmeter = patient.ConnectedOxyflowmeter;
      if (oxyflowmeter != null && _newlyInstalledOxyflowmeters.Contains(oxyflowmeter))
      {
        // TODO: Configure the oxyflowmeter prefab port and the installed oxygen-mask port in their serialized fields.
        var equipmentPoint = oxyflowmeter.OxyLineConnectionPoint;
        var patientPoint = patient.OxygenMaskAttachmentPoint;
        if (TryCreateAutomaticLine(equipmentPoint, patientPoint, "oxygen"))
          _newlyInstalledOxyflowmeters.Remove(oxyflowmeter);
      }

      WallAttachedWallSuction suction = patient.ConnectedWallSuction;
      if (suction != null && _newlyInstalledWallSuction.Contains(suction))
      {
        // TODO: Configure the wall-suction and patient suction ports after the appropriate targets are finalized.
        var equipmentPoint = suction.SuctionLineConnectionPoint;
        var patientPoint = patient.SuctionLineAttachmentPoint;
        if (TryCreateAutomaticLine(equipmentPoint, patientPoint, "suction"))
          _newlyInstalledWallSuction.Remove(suction);
      }
    }

    private bool TryCreateAutomaticLine(LineConnectionPoint equipmentPoint, LineConnectionPoint patientPoint, string lineType)
    {
      if (equipmentPoint == null || patientPoint == null)
      {
        WarnAutomaticLineFailure(lineType, "required attach point is missing");
        return false;
      }

      var service = FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
      if (service == null)
      {
        WarnAutomaticLineFailure(lineType, "LineConnectionService is missing");
        return false;
      }

      if (equipmentPoint.IsPhysicallyConnectedTo(patientPoint)
          || service.TryCreateAutomaticConnection(equipmentPoint, patientPoint))
      {
        _warnedAutomaticLineFailures.Remove(lineType);
        return true;
      }

      WarnAutomaticLineFailure(lineType, "endpoint is unavailable, incompatible, or not server-spawned");
      return false;
    }

    private void WarnAutomaticLineFailure(string lineType, string reason)
    {
      if (_warnedAutomaticLineFailures.Add(lineType))
        Debug.LogWarning($"[PatientCareDescriptionZone] '{Identifier}' cannot auto-connect {lineType}: {reason}.", this);
    }

    private static void DisconnectAutomaticOxyLine(PatientController patient, WallAttachedOxyflowmeter equipment)
    {
      DisconnectAutomaticLine(equipment?.OxyLineConnectionPoint, patient?.ConfiguredOxygenMaskAttachmentPoint);
    }

    private static void DisconnectAutomaticSuctionLine(PatientController patient, WallAttachedWallSuction equipment)
    {
      DisconnectAutomaticLine(equipment?.SuctionLineConnectionPoint, patient?.ConfiguredSuctionLineAttachmentPoint);
    }

    private static void DisconnectAutomaticLine(LineConnectionPoint equipmentPoint, LineConnectionPoint patientPoint)
    {
      if (equipmentPoint == null || patientPoint == null)
        return;
      var service = FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
      service?.DisconnectAutomaticConnection(equipmentPoint, patientPoint);
    }

    private bool IsEquipmentInZone(Component equipment) =>
      equipment != null && IsPointInside(equipment.transform.position);

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
