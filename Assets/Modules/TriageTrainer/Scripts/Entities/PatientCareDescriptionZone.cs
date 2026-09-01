using System.Collections.Generic;
using FishNet;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity.LineConnection;
using TriageTrainer.Entity.OxyLine;
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
    private readonly List<DefibrillatorCartController> _defibrillatorCarts = new();
    private readonly HashSet<WallAttachedWallSuction> _newlyInstalledWallSuction = new();
    private readonly HashSet<string> _warnedAutomaticLineFailures = new();
    private bool _warnedMultipleWallSuction;
    private bool _warnedMultipleOxyflowmeter;
    private bool _warnedMissingEquipment;
    private bool _warnedMissingPositioningPoint;
    private bool _loggedEquipmentScanDiagnostic;
    // 구성 검증용 수량이다. 장비는 시작 시 Hide() 되어 collider가 비활성일 수 있으므로,
    // 실제 환자 연결 목록과 별도로 보관한다.
    private int _wallSuctionInZoneCount;
    private int _oxyflowmeterInZoneCount;
    private PatientController _activePatient;
    private WallAttachedWallSuction _connectedWallSuction;
    private WallAttachedOxyflowmeter _connectedOxyflowmeter;
    private BoxCollider _collider;

    public IReadOnlyList<WallAttachedWallSuction> WallSuction => _wallSuction;
    public IReadOnlyList<WallAttachedOxyflowmeter> Oxyflowmeters => _oxyflowmeters;
    /// <summary>
    /// 이 구역 범위 안에 있는 제세동 카트 목록입니다.
    /// 이동식 장비이므로 조회 직전에 위치를 다시 확인합니다.
    /// </summary>
    public IReadOnlyList<DefibrillatorCartController> DefibrillatorCarts
    {
      get
      {
        RefreshDefibrillatorCarts();
        return _defibrillatorCarts;
      }
    }
    public string Identifier => _identifier;
    public PatientController CurrentPatient => _activePatient;
    public Vector3 WorldCenter => transform.TransformPoint(_center);

    public PatientController GetPatientForOxyflowmeter(WallAttachedOxyflowmeter flowmeter)
    {
      if (flowmeter == null || _activePatient == null)
        return null;
      return IsEquipmentInZone(flowmeter)
             || ReferenceEquals(_activePatient.ConnectedOxyflowmeter, flowmeter)
        ? _activePatient
        : null;
    }

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
      // IsAttached는 장비의 플레이 중 설치 상태이며 zone 소속 판정에는 사용하지 않는다.
      // 구성 유효성 경고는 모든 오브젝트 초기화가 끝난 Start() 에서 수행한다.
    }

    private void Start()
    {
      // 모든 MonoBehaviour 의 Awake 가 끝난 뒤이므로, 씬에 이미 존재하는 장비가
      // Awake~Start 사이에 설치된 경우를 포착할 수 있다.
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
      _warnedAutomaticLineFailures.Clear();
      TriageWorldInteractionSignals.RaiseCareZoneDisabled(Identifier);
    }

    private void OnWallSuctionAttachmentStateChanged(WallAttachedWallSuction equipment, bool attached)
    {
      if (IsEquipmentInZone(equipment))
      {
        if (!_wallSuction.Contains(equipment))
          _wallSuction.Add(equipment);
      }
      else
      {
        _wallSuction.Remove(equipment);
        _newlyInstalledWallSuction.Remove(equipment);
      }
      RecheckConfiguration();
      RefreshActivePatientEquipment(refreshEquipment: false);
    }

    private void OnOxyflowmeterAttachmentStateChanged(WallAttachedOxyflowmeter equipment, bool attached)
    {
      // 회수해도 유량계는 구역 소속으로 남아(위치 기준 판정) 환자 연결이 그대로다.
      // 산소 라인만 따로 끊지 않으면 회수한 자리에 선이 남는다.
      if (!attached && _activePatient != null
          && ReferenceEquals(_activePatient.ConnectedOxyflowmeter, equipment))
        DisconnectAutomaticOxyLine(_activePatient, equipment);

      if (IsEquipmentInZone(equipment))
      {
        if (!_oxyflowmeters.Contains(equipment))
          _oxyflowmeters.Add(equipment);
      }
      else
      {
        _oxyflowmeters.Remove(equipment);
      }
      RecheckConfiguration();
      RefreshActivePatientEquipment(refreshEquipment: false);
    }

    private void OnWallSuctionInstallationConfirmed(WallAttachedWallSuction equipment)
    {
      if (equipment == null || !IsEquipmentInZone(equipment))
        return;
      _newlyInstalledWallSuction.Add(equipment);
      RefreshActivePatientEquipment();
      RecheckConfiguration();
    }

    private void OnOxyflowmeterInstallationConfirmed(WallAttachedOxyflowmeter equipment)
    {
      if (equipment == null || !IsEquipmentInZone(equipment))
        return;
      RefreshActivePatientEquipment();
      RecheckConfiguration();
    }

    private void RefreshActivePatientEquipment(bool refreshEquipment = true)
    {
      if (_activePatient != null)
        Connect(_activePatient, refreshEquipment);
      else
      {
        if (refreshEquipment)
          RefreshEquipment();
      }
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
      if (_collider == null)
        return;
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
      if (bed == null)
        return;
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
        if (!_patientColliderCounts.TryGetValue(patient, out int count))
          return;
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
      if (bed == null || !_bedColliderCounts.TryGetValue(bed, out int bedCount))
        return;
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

    private void Connect(PatientController patient, bool refreshEquipment = true)
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
      if (refreshEquipment)
        RefreshEquipment();
      WarnIfConfigurationInvalid();
      IReadOnlyList<WallAttachedWallSuction> suction = GetWallSuctionSourcesWithFallback();
      IReadOnlyList<WallAttachedOxyflowmeter> flowmeter = GetOxyflowmeterSourcesWithFallback();
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

      // 기획 참고(대화 기록): 산소 유량계와 T-piece/비강 캐뉼라 사이는
      // "상호작용을 실행하면 ... oxy line이 연결"된다. 그래서 산소 라인은 유량계 설치나
      // 환자 진입이 아니라 "산소 유량계 조작"을 마친 뒤에만 만든다(ReconcileOxygenLine).

      WallAttachedWallSuction suction = patient.ConnectedWallSuction;
      if (suction != null && _newlyInstalledWallSuction.Contains(suction))
      {
        // TODO: 적절한 대상이 확정되면 벽면 흡인기와 환자 흡인 포트를 설정한다.
        var equipmentPoint = suction.SuctionLineConnectionPoint;
        var patientPoint = patient.SuctionLineAttachmentPoint;
        if (TryCreateAutomaticLine(equipmentPoint, patientPoint, "suction"))
          _newlyInstalledWallSuction.Remove(suction);
      }

      ReconcileOxygenLine(patient);
    }

    /// <summary>
    /// 유량계 조작 뒤, 이 구역에 유량계와 현재 환자가 모두 있을 때 산소 라인을 재조정한다.
    /// </summary>
    public void TryReconcileOxygenLineFor(WallAttachedOxyflowmeter flowmeter)
    {
      PatientController patient = GetPatientForOxyflowmeter(flowmeter);
      if (patient == null)
        return;

      bool lineConnected = ReconcileOxygenLine(patient, flowmeter);
      if (lineConnected && !ReferenceEquals(patient.ConnectedOxyflowmeter, flowmeter))
        patient.SetConnectedOxyflowmeter(flowmeter);
      if (lineConnected)
        patient.NotifyOxygenLineConnected();
    }

    /// <summary>
    /// 산소 유량계와 비강 캐뉼라 사이의 산소 라인을 만든다.
    ///
    /// <para>연결 조건은 세 가지다. 유량계가 설치되어 있고, "산소 유량계 조작" 상호작용이
    /// 수행되었고, 환자의 비강 캐뉼라 표시가 켜져 산소 포트가 활성화되어 있어야 한다.
    /// 설치나 환자 진입만으로는 만들지 않는다.</para>
    ///
    /// <para>유량계는 로컬 정적 오브젝트지만, 포트 식별자는 레이아웃 엔티티 식별자로 안정화된다.
    /// 따라서 서버가 공통 토폴로지 서비스에서 연결을 확정하고, 각 클라이언트는 전파된 상태만
    /// 로컬 렌더링으로 반영한다.</para>
    /// </summary>
    private void ReconcileOxygenLine(PatientController patient)
    {
      ReconcileOxygenLine(patient, patient != null ? patient.ConnectedOxyflowmeter : null);
    }

    private bool ReconcileOxygenLine(PatientController patient, WallAttachedOxyflowmeter flowmeter)
    {
      if (flowmeter == null || !flowmeter.IsAttached || !flowmeter.IsAttachedInteractCompleted)
        return false;

      var equipmentPoint = flowmeter.OxyLineConnectionPoint;
      if (equipmentPoint == null)
      {
        WarnAutomaticLineFailure("oxy", "flowmeter oxy port is not wired on the prefab");
        return false;
      }

      // 비강 캐뉼라를 아직 적용하지 않았으면 환자 측 포트가 비활성이다. 조작을 먼저 한
      // 순서에서도 캐뉼라 적용 뒤 이 경로가 다시 돌면서 연결되므로 경고하지 않는다.
      var patientPoint = ResolveActiveOxyLineConnectionPoint(patient);
      if (patientPoint == null)
        return false;
      if (equipmentPoint.IsPhysicallyConnectedTo(patientPoint))
        return true;

      if (!equipmentPoint.CanAcceptAdditionalConnection
          || !patientPoint.CanAcceptAdditionalConnection
          || !equipmentPoint.CanConnectTo(patientPoint)
          || !patientPoint.CanConnectTo(equipmentPoint))
      {
        WarnAutomaticLineFailure("oxy", "endpoint is unavailable or incompatible");
        return false;
      }

      var service = LineConnectionService.TopologyService
                    ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
      if (service == null)
      {
        WarnAutomaticLineFailure("oxy", "LineConnectionService is missing");
        return false;
      }

      // 정적 유량계 상호작용은 클라이언트에서 시작될 수 있다. 자동 연결 생성은 서버만
      // 허용하므로, 클라이언트에서는 같은 토폴로지 요청 경로로 서버에 전달한다.
      // 반환값은 "실제로 연결됨"만 나타내야 하므로, 요청만 보낸 이 프레임에는 false다.
      // 서버가 선을 만들면 OxyLineConnectionPoint.NotifyLineConnected가 완료 처리를 한다.
      if (!InstanceFinder.IsOffline && !InstanceFinder.IsServerStarted)
      {
        ScenarioNetworkRelay.RequestLineTopologyChange(
          equipmentPoint.ConnectionIdentifier, patientPoint.ConnectionIdentifier, connected: true);
        return false;
      }

      if (service.TryCreateAutomaticConnection(equipmentPoint, patientPoint))
      {
        _warnedAutomaticLineFailures.Remove("oxy");
        return true;
      }
      return equipmentPoint.IsPhysicallyConnectedTo(patientPoint);
    }

    private static OxyLineConnectionPoint ResolveActiveOxyLineConnectionPoint(PatientController patient)
    {
      if (patient == null)
        return null;

      OxyLineConnectionPoint configured = patient.OxygenMaskAttachmentPoint;
      if (configured != null)
        return configured;

      OxyLineConnectionPoint resolved = null;
      var points = patient.GetComponentsInChildren<OxyLineConnectionPoint>(true);
      for (int i = 0; i < points.Length; i++)
      {
        if (points[i] == null || !points[i].isActiveAndEnabled)
          continue;
        if (resolved != null)
          return null;
        resolved = points[i];
      }
      return resolved;
    }

    private bool TryCreateAutomaticLine(LineConnectionPoint equipmentPoint, LineConnectionPoint patientPoint, string lineType)
    {
      if (equipmentPoint == null || patientPoint == null)
      {
        WarnAutomaticLineFailure(lineType, "required attach point is missing");
        return false;
      }

      var service = LineConnectionService.TopologyService
                    ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
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
      var equipmentPoint = equipment?.OxyLineConnectionPoint;
      var patientPoint = patient?.ConfiguredOxygenMaskAttachmentPoint;
      if (equipmentPoint == null || patientPoint == null)
        return;

      DisconnectAutomaticLine(equipmentPoint, patientPoint);
    }

    private static void DisconnectAutomaticSuctionLine(PatientController patient, WallAttachedWallSuction equipment)
    {
      DisconnectAutomaticLine(equipment?.SuctionLineConnectionPoint, patient?.ConfiguredSuctionLineAttachmentPoint);
    }

    private static void DisconnectAutomaticLine(LineConnectionPoint equipmentPoint, LineConnectionPoint patientPoint)
    {
      if (equipmentPoint == null || patientPoint == null)
        return;
      var service = LineConnectionService.TopologyService
                    ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
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

    private IReadOnlyList<WallAttachedWallSuction> GetWallSuctionSourcesWithFallback()
    {
      IReadOnlyList<WallAttachedWallSuction> sources = GetUsableWallSuctionSources();
      if (sources == null || sources.Count != 0
          || !ScenarioGameRules.AllowsMissingCareZoneEquipmentFallback(
            CareZoneMissingEquipmentFallback.WallSuction))
        return sources;

      WallAttachedWallSuction closest = FindClosestWallSuction();
      return closest != null ? new[] { closest } : sources;
    }

    private IReadOnlyList<WallAttachedOxyflowmeter> GetOxyflowmeterSourcesWithFallback()
    {
      IReadOnlyList<WallAttachedOxyflowmeter> sources = GetUsableOxyflowmeterSources();
      if (sources == null || sources.Count != 0
          || !ScenarioGameRules.AllowsMissingCareZoneEquipmentFallback(
            CareZoneMissingEquipmentFallback.Oxyflowmeter))
        return sources;

      WallAttachedOxyflowmeter closest = FindClosestOxyflowmeter();
      return closest != null ? new[] { closest } : sources;
    }

    private WallAttachedWallSuction FindClosestWallSuction()
    {
      WallAttachedWallSuction closest = null;
      float closestDistanceSquared = float.PositiveInfinity;
      WallAttachedWallSuction[] candidates = FindObjectsByType<WallAttachedWallSuction>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int i = 0; i < candidates.Length; i++)
      {
        WallAttachedWallSuction candidate = candidates[i];
        if (candidate == null)
          continue;

        float distanceSquared = (candidate.transform.position - WorldCenter).sqrMagnitude;
        if (distanceSquared < closestDistanceSquared)
        {
          closest = candidate;
          closestDistanceSquared = distanceSquared;
        }
      }
      return closest;
    }

    private WallAttachedOxyflowmeter FindClosestOxyflowmeter()
    {
      WallAttachedOxyflowmeter closest = null;
      float closestDistanceSquared = float.PositiveInfinity;
      WallAttachedOxyflowmeter[] candidates = FindObjectsByType<WallAttachedOxyflowmeter>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int i = 0; i < candidates.Length; i++)
      {
        WallAttachedOxyflowmeter candidate = candidates[i];
        if (candidate == null)
          continue;

        float distanceSquared = (candidate.transform.position - WorldCenter).sqrMagnitude;
        if (distanceSquared < closestDistanceSquared)
        {
          closest = candidate;
          closestDistanceSquared = distanceSquared;
        }
      }
      return closest;
    }

    private void RefreshEquipment()
    {
      _wallSuction.Clear();
      _oxyflowmeters.Clear();
      _wallSuctionInZoneCount = 0;
      _oxyflowmeterInZoneCount = 0;
      int suctionAttachedCount = 0;
      int flowmeterAttachedCount = 0;

      // 벽면 장비는 미설치 상태에서 Hide() 되며, 이때 collider도 비활성일 수 있다.
      // 따라서 Physics.OverlapBox로 탐색하면 씬에 배치된 장비를 범위 밖으로 오인한다.
      // 컴포넌트를 포함해 찾은 뒤 transform 위치로 zone 포함 여부를 판정한다.
      WallAttachedWallSuction[] suctions = FindObjectsByType<WallAttachedWallSuction>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int i = 0; i < suctions.Length; i++)
      {
        WallAttachedWallSuction suction = suctions[i];
        if (suction != null && IsEquipmentInZone(suction))
        {
          _wallSuctionInZoneCount++;
          if (suction.IsAttached)
            suctionAttachedCount++;
          // IsAttached는 플레이 중 표시/설치 상태일 뿐 zone 소속 판정 조건이 아니다.
          _wallSuction.Add(suction);
        }
      }

      WallAttachedOxyflowmeter[] flowmeters = FindObjectsByType<WallAttachedOxyflowmeter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int i = 0; i < flowmeters.Length; i++)
      {
        WallAttachedOxyflowmeter flowmeter = flowmeters[i];
        if (flowmeter != null && IsEquipmentInZone(flowmeter))
        {
          _oxyflowmeterInZoneCount++;
          if (flowmeter.IsAttached)
            flowmeterAttachedCount++;
          // IsAttached는 플레이 중 표시/설치 상태일 뿐 zone 소속 판정 조건이 아니다.
          _oxyflowmeters.Add(flowmeter);
        }
      }

      // 진단 로그: zone 안의 장비가 없는지, 설치 전 상태인지 구분한다.
      // Connect() 등에서 빈번히 호출되므로 상태 전이 시 1회만 출력한다.
      if (_wallSuctionInZoneCount == 0 || _oxyflowmeterInZoneCount == 0)
      {
        if (!_loggedEquipmentScanDiagnostic)
        {
          _loggedEquipmentScanDiagnostic = true;
          Debug.LogWarning(
            $"[PatientCareDescriptionZone] '{Identifier}' equipment scan: " +
            $"wallSuctionInBounds={_wallSuctionInZoneCount}, oxyflowmeterInBounds={_oxyflowmeterInZoneCount}. " +
            $"장비 컴포넌트가 zone 범위 안에 없습니다. 장비 배치 또는 zone 크기를 확인하세요.", this);
        }
      }
      else if (suctionAttachedCount == 0 || flowmeterAttachedCount == 0)
      {
        if (!_loggedEquipmentScanDiagnostic)
        {
          _loggedEquipmentScanDiagnostic = true;
          Debug.Log(
            $"[PatientCareDescriptionZone] '{Identifier}' equipment scan: " +
            $"wallSuction={_wallSuctionInZoneCount}(attached:{suctionAttachedCount}), " +
            $"oxyflowmeter={_oxyflowmeterInZoneCount}(attached:{flowmeterAttachedCount}). " +
            $"장비가 zone 안에 있으나 아직 설치(IsAttached)되지 않았습니다. " +
            $"플레이어가 장비 설치 상호작용을 하면 자동으로 인식됩니다.", this);
        }
      }
      else
      {
        // 장비가 정상 인식되면 플래그 리셋하여 다음 상태 전이 시 다시 로그 출력.
        _loggedEquipmentScanDiagnostic = false;
      }

      RefreshDefibrillatorCarts();
    }

    /// <summary>
    /// 제세동 카트는 이동식이므로 이벤트에서 현재 구역 소속 여부를 판단할 수 있도록
    /// transform 위치로 매번 목록을 갱신합니다.
    /// </summary>
    private void RefreshDefibrillatorCarts()
    {
      _defibrillatorCarts.Clear();
      DefibrillatorCartController[] carts = FindObjectsByType<DefibrillatorCartController>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int i = 0; i < carts.Length; i++)
      {
        DefibrillatorCartController cart = carts[i];
        if (cart != null && DoesEquipmentOverlapZone(cart))
          _defibrillatorCarts.Add(cart);
      }
    }

    /// <summary>
    /// 이동식 장비는 transform 원점이 구역 밖에 있더라도 실제 외형의 일부가 구역에 걸칠 수 있다.
    /// 자식 Collider까지 포함한 월드 Bounds가 Zone Collider와 겹치면 구역 장비로 판정한다.
    /// Collider가 없는 예외적인 테스트/배치 오브젝트만 기존 원점 판정으로 되돌린다.
    /// </summary>
    private bool DoesEquipmentOverlapZone(Component equipment)
    {
      if (equipment == null)
        return false;

      _collider ??= GetComponent<BoxCollider>();
      Collider[] equipmentColliders = equipment.GetComponentsInChildren<Collider>(true);
      if (_collider != null && equipmentColliders.Length > 0)
      {
        Bounds zoneBounds = _collider.bounds;
        for (int i = 0; i < equipmentColliders.Length; i++)
        {
          Collider equipmentCollider = equipmentColliders[i];
          if (equipmentCollider != null && zoneBounds.Intersects(equipmentCollider.bounds))
            return true;
        }
        return false;
      }

      return IsEquipmentInZone(equipment);
    }

    private bool IsPatientSupportedInZone(PatientController patient)
    {
      MovingPatientBedController bed = patient.CurrentBed;
      MovingPatientBedPositioningPoint point = bed == null ? null : bed.LatchedPositioningPoint;
      return bed != null && point != null && IsPointInside(point.transform.position);
    }

    /// <summary>
    /// 장비 설치/해제 콜백에서 호출된다. 두 장비가 모두 1개 이상 감지되면
    /// _warnedMissingEquipment 플래그를 리셋하여 이후 WarnIfConfigurationInvalid 가
    /// 다시 경고를 발생시킬 수 있게 한다.
    /// </summary>
    private void RecheckConfiguration()
    {
      if (_wallSuction.Count > 0 && _oxyflowmeters.Count > 0)
        _warnedMissingEquipment = false;
    }

    private void WarnIfConfigurationInvalid()
    {
      if (_wallSuctionInZoneCount == 0 || _oxyflowmeterInZoneCount == 0)
      {
        if (!_warnedMissingEquipment)
        {
          Debug.LogWarning($"[PatientCareDescriptionZone] '{Identifier}' requires one wall_suction and one oxyflowmeter in its bounds. " +
            $"Current: wall_suction={_wallSuctionInZoneCount}, oxyflowmeter={_oxyflowmeterInZoneCount}.", this);
          _warnedMissingEquipment = true;
        }
      }
      else
        _warnedMissingEquipment = false;

      MovingPatientBedPositioningPoint[] points = FindObjectsByType<MovingPatientBedPositioningPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      bool hasPoint = false;
      for (int i = 0; i < points.Length; i++)
        if (IsPointInside(points[i].transform.position))
        { hasPoint = true; break; }
      if (!hasPoint && !_warnedMissingPositioningPoint)
      {
        Debug.LogWarning($"[PatientCareDescriptionZone] '{Identifier}' has no positioning point inside its bounds.", this);
        _warnedMissingPositioningPoint = true;
      }
      else if (hasPoint)
        _warnedMissingPositioningPoint = false;
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
