using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;
using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public partial class PatientMonitorController : IInteractable, PatientController.IMonitorSelectionRequester, PatientController.IMedicalStateListener
  {
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
      public bool CanInteract(Transform interactor) => _owner.IsInteractEnabled(InteractIdSelectPatient);
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
      public string DisplayText => "자세히 보기";
      public Sprite DisplayIcon => _owner._interactIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public string NearestOnlyGroup => NearestGroupDetailOverlay;
      public Transform NearestOnlyDistanceOrigin => _owner.transform;
      public Collider NearestOnlyCollider => _owner.GetComponent<Collider>();
      public int NearestOnlyTieBreaker => _owner.GetInstanceID();
      public bool CanInteract(Transform interactor) => _owner.IsInteractEnabled(InteractIdDetailOverlay);
      public void Interact(Transform interactor)
      {
        if (!CanInteract(interactor))
          return;
        var patient = _owner.MonitoringPatient;
        if (patient != null
            && string.Equals(patient.Identifier, "patient_a", System.StringComparison.Ordinal)
            && ScenarioController.Instance?.CurrentGraph?.Identifier == "patient_a_critical")
          _owner.ArmScenarioClose(patient, "close_vital_ui_a");
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
        => _owner.IsInteractEnabled(InteractIdDisconnectPatient) && _owner._monitoringPatient != null;
      public void Interact(Transform interactor)
      {
        if (!CanInteract(interactor))
          return;
        _owner.SetMonitoringPatient(null);
      }
    }

    [Serializable]
    public class InteractEntry
    {
      [SerializeField] private string _identifier;
      [SerializeField] private bool _enabled = true;

      public string Identifier => _identifier;
      public bool Enabled
      {
        get => _enabled;
        set => _enabled = value;
      }

      public InteractEntry(string identifier, bool enabled = true)
      {
        _identifier = identifier;
        _enabled = enabled;
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
    [SerializeField] private List<InteractEntry> _interactEntries = new();

    private readonly List<IInteract> _interacts = new();
    private readonly Dictionary<string, InteractEntry> _interactEntryMap = new(StringComparer.Ordinal);
    private readonly Dictionary<int, PlayerController> _selectionModePlayers = new();

    public IInteract[] Interacts => _interacts.ToArray();

    protected virtual void Awake()
    {
      EnsureInteractionCollider();
      BuildInteracts();
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
      ExitSelectionModeForAll();
      UnconfigurePatientTracking();
      if (_monitoringPatient != null)
        _monitoringPatient.ClearMonitoringPatientMonitor(this);
      UnregisterMedicalStateSubscription();
      ReleaseTrackingLineResources();
    }

    private void BuildInteracts()
    {
      EnsureInteractEntry(InteractIdSelectPatient, IsPatientTrackingMethodEnabled(PatientTrackingMethod.Interactable));
      EnsureInteractEntry(InteractIdDetailOverlay, EnableDetailedContentOverlay);
      EnsureInteractEntry(InteractIdDisconnectPatient, true);
      RebuildInteractEntryMap();

      _interacts.Clear();
      _interacts.Add(new MonitorSelectModeInteract(this));
      _interacts.Add(new MonitorDetailInteract(this));
      _interacts.Add(new MonitorDisconnectPatientInteract(this));
    }

    private void EnsureInteractEntry(string identifier, bool enabled)
    {
      for (int i = 0; i < _interactEntries.Count; i++)
      {
        var each = _interactEntries[i];
        if (each == null || !string.Equals(each.Identifier, identifier, StringComparison.Ordinal))
          continue;

        if (string.Equals(identifier, InteractIdSelectPatient, StringComparison.Ordinal) ||
            string.Equals(identifier, InteractIdDetailOverlay, StringComparison.Ordinal))
          each.Enabled = enabled;
        return;
      }

      _interactEntries.Add(new InteractEntry(identifier, enabled));
    }

    private void RebuildInteractEntryMap()
    {
      _interactEntryMap.Clear();
      for (int i = 0; i < _interactEntries.Count; i++)
      {
        var each = _interactEntries[i];
        if (each == null || string.IsNullOrWhiteSpace(each.Identifier))
          continue;

        _interactEntryMap[each.Identifier] = each;
      }
    }

    public void SetInteractEnabled(string identifier, bool enabled)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      for (int i = 0; i < _interactEntries.Count; i++)
      {
        var each = _interactEntries[i];
        if (each == null || !string.Equals(each.Identifier, identifier, StringComparison.Ordinal))
          continue;

        each.Enabled = enabled;
        RebuildInteractEntryMap();
        return;
      }

      _interactEntries.Add(new InteractEntry(identifier, enabled));
      RebuildInteractEntryMap();
    }

    public void AddInteract(string identifier, bool enabled = true)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      EnsureInteractEntry(identifier, enabled);
      RebuildInteractEntryMap();
    }

    public void RemoveInteract(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      for (int i = _interactEntries.Count - 1; i >= 0; i--)
      {
        var each = _interactEntries[i];
        if (each == null || !string.Equals(each.Identifier, identifier, StringComparison.Ordinal))
          continue;

        _interactEntries.RemoveAt(i);
      }

      RebuildInteractEntryMap();
    }

    public bool IsInteractEnabled(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      return _interactEntryMap.TryGetValue(identifier, out var entry) && entry.Enabled;
    }

    private void EnterSelectionMode(Transform interactor)
    {
      if (!IsInteractEnabled(InteractIdSelectPatient))
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

      _patientCareZone = FindObjectsByType<PatientCareDescriptionZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
        .FirstOrDefault(zone => zone != null && zone.ContainsWorldPosition(transform.position));
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
