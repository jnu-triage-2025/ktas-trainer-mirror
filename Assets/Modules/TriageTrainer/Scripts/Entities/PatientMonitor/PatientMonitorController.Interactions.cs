using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public partial class PatientMonitorController : IInteractable, IInteractionDefinitionSource, PatientController.IMonitorSelectionRequester, PatientController.IMedicalStateListener
  {
    /// <summary>모든 환자 모니터가 초기화 사이클에서 받는 태그. 시나리오 데이터가 태그 참조로 모니터 전체에 정의를 붙일 때 쓴다.</summary>
    public const string EntityTag = "patient_monitor";

    [Header("Identity")]
    [Tooltip("레지스트리 주소와 엔티티 레지스트리에 쓰는 식별자. 비우면 자동 생성된다.")]
    [SerializeField] private string _entityIdentifier;
    private string _registeredEntityIdentifier;

    public string EntityIdentifier => _entityIdentifier;

    public IEnumerable<InteractionDeclaration> DeclareInteractions()
    {
      for (int i = 0; i < _interacts.Count; i++)
      {
        if (_interacts[i] is not IQuestPresentationTarget target)
          continue;
        yield return new InteractionDeclaration(
          InteractionDefinition.Code(_entityIdentifier, target.InteractionIdentifier, _interacts[i].DisplayText, initialVisible: true),
          _interacts[i]);
      }
    }

    private void RegisterMonitorEntity()
    {
      if (string.IsNullOrWhiteSpace(_entityIdentifier))
        _entityIdentifier = MultiplayerInfrastructure.Registry.EntityId.Ensure(_entityIdentifier, gameObject, "patient_monitor");
      _registeredEntityIdentifier = _entityIdentifier;
      MultiplayerInfrastructure.Registry.Registry.RegisterEntity(
        _registeredEntityIdentifier, MultiplayerInfrastructure.Registry.EntityType.PatientMonitor, gameObject, displayName: gameObject.name);
      InteractionRegistry.RemoveCodeDefinitions(_registeredEntityIdentifier);
      InteractionRegistry.DeclareCode(_registeredEntityIdentifier, this);
      InteractionRegistry.AssignEntityTag(_registeredEntityIdentifier, EntityTag);
    }

    private void UnregisterMonitorEntity()
    {
      if (string.IsNullOrWhiteSpace(_registeredEntityIdentifier))
        return;
      InteractionRegistry.RemoveCodeDefinitions(_registeredEntityIdentifier);
      MultiplayerInfrastructure.Registry.Registry.UnregisterEntity(_registeredEntityIdentifier);
      _registeredEntityIdentifier = null;
    }
    private sealed class MonitorSelectModeInteract : IInteract, IInteractorConditional, INearestOnlyInteract, IQuestPresentationTarget
    {
      private readonly PatientMonitorController _owner;
      public MonitorSelectModeInteract(PatientMonitorController owner) { _owner = owner; }
      // 추적 환자가 정해지기 전에 뜨는 인터랙션이다. 그래서 표시 주소를 추적 환자가 아니라
      // 모니터 공통 식별자로 잡는다. 시나리오는 바인딩 한 줄로 모든 환자 모니터에 마크를 띄운다.
      public string PresentationEntityIdentifier => MonitorPresentationEntityIdentifier;
      public string InteractionIdentifier => InteractIdSelectPatient;
      public string DisplayText => "모니터링할 환자 선택";
      public Sprite DisplayIcon => _owner._interactIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public string NearestOnlyGroup => NearestGroupSelectPatient;
      public Transform NearestOnlyDistanceOrigin => _owner.transform;
      public Collider NearestOnlyCollider => _owner.GetComponent<Collider>();
      public int NearestOnlyTieBreaker => _owner.GetInstanceID();
      // 노출(시나리오 조건)은 레지스트리가 판정한다. 여기서는 모니터의 추적 방식만 본다.
      public bool CanInteract(Transform interactor) => _owner.IsPatientTrackingMethodEnabled(PatientTrackingMethod.Interactable);
      public void Interact(Transform interactor)
      {
        if (!CanInteract(interactor))
          return;
        _owner.EnterSelectionMode(interactor);
      }
    }

    private sealed class MonitorDetailInteract : IInteract, IInteractorConditional, INearestOnlyInteract, IQuestPresentationTarget
    {
      private readonly PatientMonitorController _owner;
      public MonitorDetailInteract(PatientMonitorController owner) { _owner = owner; }
      public string PresentationEntityIdentifier => _owner.PresentationEntityIdentifier;
      public string InteractionIdentifier => InteractIdDetailOverlay;
      public string DisplayText => "환자 모니터 자세히 보기";
      public Sprite DisplayIcon => _owner._interactIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public string NearestOnlyGroup => NearestGroupDetailOverlay;
      public Transform NearestOnlyDistanceOrigin => _owner.transform;
      public Collider NearestOnlyCollider => _owner.GetComponent<Collider>();
      public int NearestOnlyTieBreaker => _owner.GetInstanceID();
      public bool CanInteract(Transform interactor) => _owner.EnableDetailedContentOverlay;
      public void Interact(Transform interactor)
      {
        if (!CanInteract(interactor))
          return;
        // 닫기 완료 신호는 시나리오 데이터가 정의의 extras(closeSignal, closeSignalPatient)로 지정한다.
        var patient = _owner.MonitoringPatient;
        if (patient != null && InteractionRegistry.TryGetDefinition(this, out var definition))
        {
          string closeSignal = definition.GetExtra("closeSignal");
          string closePatient = definition.GetExtra("closeSignalPatient");
          if (!string.IsNullOrWhiteSpace(closeSignal)
              && (string.IsNullOrWhiteSpace(closePatient)
                  || string.Equals(patient.Identifier, closePatient.Trim(), System.StringComparison.Ordinal)))
            _owner.ArmScenarioClose(patient, closeSignal.Trim());
        }
        _owner.OpenDetailedContentOverlay();
      }
    }

    private sealed class MonitorDisconnectPatientInteract : IInteract, IInteractorConditional, INearestOnlyInteract, IQuestPresentationTarget
    {
      private readonly PatientMonitorController _owner;
      public MonitorDisconnectPatientInteract(PatientMonitorController owner) { _owner = owner; }
      public string PresentationEntityIdentifier => _owner.PresentationEntityIdentifier;
      public string InteractionIdentifier => InteractIdDisconnectPatient;
      public string DisplayText => "이 환자 모니터를 환자와 연결 해제";
      public Sprite DisplayIcon => _owner._interactIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public string NearestOnlyGroup => NearestGroupDisconnectPatient;
      public Transform NearestOnlyDistanceOrigin => _owner.transform;
      public Collider NearestOnlyCollider => _owner.GetComponent<Collider>();
      public int NearestOnlyTieBreaker => _owner.GetInstanceID();
      public bool CanInteract(Transform interactor)
        => _owner._monitoringPatient != null;
      public void Interact(Transform interactor)
      {
        if (!CanInteract(interactor))
          return;
        _owner.SetMonitoringPatient(null);
      }
    }

    private const string InteractIdSelectPatient = "select_patient_mode";
    private const string InteractIdDetailOverlay = "detail_overlay";
    private const string InteractIdDisconnectPatient = "disconnect_patient";
    private const string NearestGroupSelectPatient = "patient_monitor:select_patient_mode";
    private const string NearestGroupDetailOverlay = "patient_monitor:detail_overlay";
    private const string NearestGroupDisconnectPatient = "patient_monitor:disconnect_patient";

    /// <summary>
    /// 특정 환자에 매이지 않는 모니터 인터랙션의 퀘스트 표시 주소.
    /// 여기에 바인딩한 마크는 추적 환자와 상관없이 모든 환자 모니터에 함께 붙는다.
    /// </summary>
    public const string MonitorPresentationEntityIdentifier = "patient_monitor";

    // 자세히 보기처럼 추적 환자에 매인 인터랙션이 쓰는 주소다. 추적 환자가 없으면 모니터 공통
    // 식별자로 떨어진다. 아무도 보고 있지 않은 모니터가 특정 환자용 마크를 가져가는 일을 막는다.
    public string PresentationEntityIdentifier => _monitoringPatient?.Identifier
      ?? patientState?.Identifier
      ?? MonitorPresentationEntityIdentifier;

    // Zone 경계처럼 여러 환자 모니터 콜라이더가 Detector 범위에 함께 들어오면 같은 이름의
    // 인터렉션을 구분할 수 없다. 각 Monitor interact가 기능별 NearestOnlyGroup을 제공하여
    // 환자 선택과 자세히 보기는 모두 유지하되, 각 기능은 플레이어에게 가장 가까운 모니터 하나만 노출한다.

    protected bool IsPatientTrackingMethodEnabled(PatientTrackingMethod method)
      => (_patientTrackingMethod & method) == method;

    [Header("Interact")]
    [SerializeField] private Sprite _interactIcon;

    private readonly List<IInteract> _interacts = new();
    private readonly Dictionary<int, PlayerController> _selectionModePlayers = new();

    public IInteract[] Interacts => _interacts.ToArray();

    protected virtual void Awake()
    {
      // Awake 중에는 AddComponent가 금지된다. 콜라이더가 없다면 바로 뒤따르는
      // OnEnable에서 생성한다.
      EnsureInteractionCollider(allowCreate: false);
      BuildInteracts();
      RegisterMonitorEntity();
    }

    protected virtual void OnDisable()
    {
      CloseDetailedContentOverlay();
      ExitSelectionModeForAll();
      UnconfigurePatientTracking();
      if (_monitoringPatient != null)
        _monitoringPatient.ClearMonitoringPatientMonitor(this);
      UnregisterMedicalStateSubscription();
      DisableTrackingLine();
    }

    protected virtual void OnDestroy()
    {
      UnregisterMonitorEntity();
      ExitSelectionModeForAll();
      UnconfigurePatientTracking();
      if (_monitoringPatient != null)
        _monitoringPatient.ClearMonitoringPatientMonitor(this);
      UnregisterMedicalStateSubscription();
      ReleaseTrackingLineResources();
    }

    private void BuildInteracts()
    {
      _interacts.Clear();
      _interacts.Add(new MonitorSelectModeInteract(this));
      _interacts.Add(new MonitorDetailInteract(this));
      _interacts.Add(new MonitorDisconnectPatientInteract(this));
    }

    private void EnterSelectionMode(Transform interactor)
    {
      if (!IsPatientTrackingMethodEnabled(PatientTrackingMethod.Interactable))
        return;

      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null)
        return;

      _selectionModePlayers[player.GetInstanceID()] = player;
      BeginNetworkSelection(player);
      player.SetPatientSelectionMode(true);

      var patients = FindObjectsByType<PatientController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < patients.Length; i++)
      {
        var patient = patients[i];
        if (patient == null)
          continue;

        patient.SetMonitorSelectionRequester(this);
      }

      player.RefreshInteractableHintsNow();
    }

    private void ExitSelectionModeFor(PlayerController player)
    {
      if (player == null)
        return;

      _selectionModePlayers.Remove(player.GetInstanceID());
      EndNetworkSelection(player);
      player.SetPatientSelectionMode(false);
      if (_selectionModePlayers.Count <= 0)
      {
        var patients = FindObjectsByType<PatientController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < patients.Length; i++)
        {
          var patient = patients[i];
          if (patient == null)
            continue;

          patient.ClearMonitorSelectionRequester(this);
        }
      }

      player.RefreshInteractableHintsNow();
    }

    private void ExitSelectionModeForAll()
    {
      var values = new List<PlayerController>(_selectionModePlayers.Values);
      _selectionModePlayers.Clear();

      var patients = FindObjectsByType<PatientController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < patients.Length; i++)
      {
        var patient = patients[i];
        if (patient == null)
          continue;

        patient.ClearMonitorSelectionRequester(this);
      }

      for (int i = 0; i < values.Count; i++)
      {
        EndNetworkSelection(values[i]);
        values[i]?.SetPatientSelectionMode(false);
        values[i]?.RefreshInteractableHintsNow();
      }
    }

    public void HandlePatientSelected(PatientController patient, Transform interactor)
    {
      if (patient == null)
        return;

      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null)
        return;

      if (!_selectionModePlayers.ContainsKey(player.GetInstanceID()))
        return;

      SetMonitoringPatient(patient);
      if (!IsClientStarted || IsServerStarted)
        ExitSelectionModeFor(player);
    }

    private PatientCareDescriptionZone _patientCareZone;

    protected void ConfigurePatientTracking()
    {
      if (!IsPatientTrackingMethodEnabled(PatientTrackingMethod.DependsOnPatientCareZone))
        return;

      var matchingZones = FindObjectsByType<PatientCareDescriptionZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID)
        .Where(zone => zone != null && zone.ContainsWorldPosition(transform.position))
        .ToArray();
      if (matchingZones.Length > 1)
      {
        Debug.LogError($"[PatientMonitorController] Monitor '{name}' overlaps {matchingZones.Length} patient care zones. Tracking was not configured.", this);
        return;
      }
      _patientCareZone = matchingZones.FirstOrDefault();
      if (_patientCareZone == null)
        return;

      _patientCareZone.PatientEntered += HandleCareZonePatientEntered;
      _patientCareZone.PatientExited += HandleCareZonePatientExited;
      if (_patientCareZone.CurrentPatient != null)
        ApplyCareZonePatient(_patientCareZone.CurrentPatient);
    }

    protected void UnconfigurePatientTracking()
    {
      if (_patientCareZone == null)
        return;

      _patientCareZone.PatientEntered -= HandleCareZonePatientEntered;
      _patientCareZone.PatientExited -= HandleCareZonePatientExited;
      _patientCareZone = null;
    }

    protected void UpdatePatientTrackingLocation()
    {
      if (!IsPatientTrackingMethodEnabled(PatientTrackingMethod.DependsOnPatientCareZone))
        return;

      if (_patientCareZone == null || !_patientCareZone.ContainsWorldPosition(transform.position))
      {
        UnconfigurePatientTracking();
        ConfigurePatientTracking();
      }
    }

    private void HandleCareZonePatientEntered(PatientController patient)
    {
      if ((IsServerStarted || IsClientStarted) && !IsServerStarted)
        return;

      if (patient == null)
        return;

      if (_monitoringPatient != null &&
          _onEnterAnotherPatientAlreadyPatientExists == OnEnterAnotherPatientAlreadyPatientExists.IgnoreNewEnter)
        return;

      ApplyCareZonePatient(patient);
    }

    private void HandleCareZonePatientExited(PatientController patient)
    {
      if ((IsServerStarted || IsClientStarted) && !IsServerStarted)
        return;

      if (ReferenceEquals(_monitoringPatient, patient))
        SetMonitoringPatient(null);
    }

    private void ApplyCareZonePatient(PatientController patient)
    {
      if ((IsServerStarted || IsClientStarted) && !IsServerStarted)
        return;

      if (_monitoringPatient == null ||
          _onEnterAnotherPatientAlreadyPatientExists == OnEnterAnotherPatientAlreadyPatientExists.Refresh)
        SetMonitoringPatient(patient);
    }
  }
}
