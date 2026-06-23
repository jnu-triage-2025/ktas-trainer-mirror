using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using UnityEngine;
using TriageTrainer.Entity;
using TriageTrainer.Entity.Patient;
using TriageTrainer.Entity.PatientMonitor;
using TriageTrainer.Entity.PatientMonitor.Models;

namespace TriageTrainer.Scenario
{
  /// <summary>
  /// Registers TriageTrainer scenario event handlers without modifying base infrastructure.
  ///
  /// NOTE:
  /// - Current handlers are safe placeholders for MVP wiring.
  /// - Replace each coroutine body with real presentation/interaction logic incrementally.
  /// </summary>
  public partial class TriageScenarioEventBootstrap : MonoBehaviour
  {
    [Header("Scenario Graph Registration")]
    [SerializeField] private TextAsset _disasterIntroMvpGraph;
    [SerializeField] private string _disasterIntroMvpGraphIdentifier = "disaster_intro_mvp";

    [Header("triage_patientA_dummyA")]
    [SerializeField] private string _patientAEntityIdentifier = "patientA";
    [SerializeField] private string[] _patientAAliases = { "patientA", "patient_a", "patient" };
    [SerializeField] private string _dummyAEntityIdentifier = "dummyA";
    [SerializeField] private string[] _dummyAAliases = { "dummyA", "dummy_a", "dummy" };
    [SerializeField] private string _patientABedEntityIdentifier = "patientABed";
    [SerializeField] private string[] _patientABedAliases = { "patientABed", "patient_a_bed", "bedA" };
    [SerializeField] private string _dummyABedEntityIdentifier = "dummyABed";
    [SerializeField] private string[] _dummyABedAliases = { "dummyABed", "dummy_a_bed", "bedDummyA" };
    [SerializeField] private GameObject _patientAObject;
    [SerializeField] private GameObject _dummyAObject;
    [SerializeField] private GameObject _patientABedObject;
    [SerializeField] private GameObject _dummyABedObject;
    [SerializeField] private Transform _patientASpawnPoint;
    [SerializeField] private Transform _dummyASpawnPoint;
    [SerializeField] private Transform _patientABedSpawnPoint;
    [SerializeField] private Transform _dummyABedSpawnPoint;
    [SerializeField, Min(0f)] private float _spawnMoveDurationSeconds = 0f;
    [SerializeField] private bool _autoAttachPatientsToBeds = true;
    [SerializeField] private bool _preferMovingBedsForSpawn = true;

    [Header("show_patientA_ui / show_dummyA_ui")]
    [SerializeField] private GameObject _patientAUiPanel;
    [SerializeField] private GameObject _dummyAUiPanel;
    [SerializeField, Min(0f)] private float _uiPanelAutoHideSeconds = 0f;

    [Header("patient_a_critical (P1 MVP)")]
    [SerializeField] private string _patientATreatmentBedEntityIdentifier = "patientATreatmentBed";
    [SerializeField] private string[] _patientATreatmentBedAliases = { "patientATreatmentBed", "patient_a_treatment_bed", "treatmentBedA" };
    [SerializeField] private GameObject _patientATreatmentBedObject;
    [SerializeField] private Transform _patientATreatmentRoomPoint;
    [SerializeField, Min(0f)] private float _patientATreatmentMoveDurationSeconds = 1.5f;
    [SerializeField] private bool _autoAttachPatientAToTreatmentBed = true;

    [SerializeField] private string _patientAVitalMonitorEntityIdentifier = "patientA_monitor";
    [SerializeField] private string[] _patientAVitalMonitorAliases = { "patientA_monitor", "patient_a_monitor", "monitorA" };
    [SerializeField] private GameObject _patientAVitalMonitorObject;
    [SerializeField] private PatientMonitorController _patientAVitalMonitorController;
    [SerializeField] private GameObject _patientAVitalPanel;
    [SerializeField] private bool _applyPatientAInitialMonitorProfile = true;
    [SerializeField] private ECGParameters _patientAInitialMonitorParameters = new ECGParameters
    {
      bpm = 140f,
      pAmp = 0.15f,
      pWidth = 0.04f,
      qAmp = -0.15f,
      rAmp = 1.2f,
      sAmp = -0.25f,
      tAmp = 0.3f,
      tWidth = 0.08f,
      uAmp = 0f,
      stElevation = 0f,
      noise = 0.02f,
      irregularity = 0.03f,
      qrsWidthScale = 1.0f
    };
    [SerializeField] private ECGParameters _patientACrashMonitorParameters = new ECGParameters
    {
      bpm = 80f,
      pAmp = 0.08f,
      pWidth = 0.04f,
      qAmp = -0.12f,
      rAmp = 0.7f,
      sAmp = -0.20f,
      tAmp = 0.18f,
      tWidth = 0.08f,
      uAmp = 0f,
      stElevation = 0f,
      noise = 0.03f,
      irregularity = 0.12f,
      qrsWidthScale = 1.1f
    };
    [SerializeField] private ECGParameters _patientAAsystoleMonitorParameters = new ECGParameters
    {
      bpm = 0f,
      pAmp = 0f,
      pWidth = 0.04f,
      qAmp = 0f,
      rAmp = 0f,
      sAmp = 0f,
      tAmp = 0f,
      tWidth = 0.08f,
      uAmp = 0f,
      stElevation = 0f,
      noise = 0.01f,
      irregularity = 0f,
      qrsWidthScale = 1.0f
    };
    [SerializeField] private ECGParameters _patientARoscMonitorParameters = new ECGParameters
    {
      bpm = 110f,
      pAmp = 0.14f,
      pWidth = 0.04f,
      qAmp = -0.14f,
      rAmp = 1.0f,
      sAmp = -0.22f,
      tAmp = 0.28f,
      tWidth = 0.08f,
      uAmp = 0f,
      stElevation = 0f,
      noise = 0.02f,
      irregularity = 0.04f,
      qrsWidthScale = 1.0f
    };

    [SerializeField] private GameObject _suctionChecklistUiPanel;
    [SerializeField] private GameObject _intuChecklistUiPanel;
    [SerializeField] private GameObject _ivChecklistUiPanel;
    [SerializeField, TextArea] private string _patientAVitalInfoMessage = "[환자 A 활력징후] BP 70/40, HR 140, RR 8, BT 35.9, SpO2 82";
    [SerializeField] private bool _vitalInfoAlsoActivateMonitor = true;
    [SerializeField] private GameObject _patientAEtTubePreparedVisual;
    [SerializeField] private GameObject _patientAEtTubeInsertedVisual;
    [SerializeField] private GameObject _patientAEtTubeWithoutStyletVisual;
    [SerializeField] private GameObject _patientATPieceConnectedVisual;
    [SerializeField] private GameObject _patientAGauzeVisual;
    [SerializeField] private GameObject _patientAGauzeWithPlasterVisual;
    [SerializeField] private GameObject _patientA18gLeftVisual;
    [SerializeField] private GameObject _patientANs1LeftConnectedVisual;
    [SerializeField] private GameObject _patientA18gRightVisual;
    [SerializeField] private GameObject _patientAPs1RightConnectedVisual;
    [SerializeField] private GameObject _patientACentralLineVisual;
    [SerializeField] private GameObject _level1ReadyVisual;
    [SerializeField] private GameObject _patientAAmbuConnectedVisual;
    [SerializeField] private GameObject _patientADefibPadVisual;
    [SerializeField] private GameObject _defibIrregularUiPanel;
    [SerializeField] private ECGParameters _patientADefibIrregularMonitorParameters = new ECGParameters
    {
      bpm = 150f,
      pAmp = 0.05f,
      pWidth = 0.03f,
      qAmp = -0.20f,
      rAmp = 0.8f,
      sAmp = -0.30f,
      tAmp = 0.15f,
      tWidth = 0.06f,
      uAmp = 0f,
      stElevation = 0f,
      noise = 0.08f,
      irregularity = 0.35f,
      qrsWidthScale = 1.1f
    };
    [SerializeField] private Animator[] _ambuBaggingAnimators;
    [SerializeField] private string _ambuBaggingBoolName = "IsAmbuBagging";
    [SerializeField] private Animator[] _chestCompressionAnimators;
    [SerializeField] private string _chestCompressionBoolName = "IsChestCompressing";

    [Header("patient_b_c_ct intro (MVP)")]
    [SerializeField] private string _patientBEntityIdentifier = "patientB";
    [SerializeField] private string[] _patientBAliases = { "patientB", "patient_b" };
    [SerializeField] private string _patientCEntityIdentifier = "patientC";
    [SerializeField] private string[] _patientCAliases = { "patientC", "patient_c" };
    [SerializeField] private string _dummyBEntityIdentifier = "dummyB";
    [SerializeField] private string[] _dummyBAliases = { "dummyB", "dummy_b" };
    [SerializeField] private GameObject _patientBObject;
    [SerializeField] private GameObject _patientCObject;
    [SerializeField] private GameObject _dummyBObject;
    [SerializeField] private Transform _patientBSpawnPoint;
    [SerializeField] private Transform _patientCSpawnPoint;
    [SerializeField] private Transform _dummyBSpawnPoint;
    [SerializeField, Min(0f)] private float _patientBcdSpawnMoveDurationSeconds = 0f;
    [SerializeField] private GameObject _patientBUiPanel;
    [SerializeField] private GameObject _patientCUiPanel;
    [SerializeField] private GameObject _dummyBUiPanel;
    [SerializeField] private string _patientBTreatmentBedEntityIdentifier = "patientBTreatmentBed";
    [SerializeField] private string[] _patientBTreatmentBedAliases = { "patientBTreatmentBed", "patient_b_treatment_bed", "treatmentBedB" };
    [SerializeField] private string _patientCTreatmentBedEntityIdentifier = "patientCTreatmentBed";
    [SerializeField] private string[] _patientCTreatmentBedAliases = { "patientCTreatmentBed", "patient_c_treatment_bed", "treatmentBedC" };
    [SerializeField] private GameObject _patientBTreatmentBedObject;
    [SerializeField] private GameObject _patientCTreatmentBedObject;
    [SerializeField] private Transform _patientBTreatmentRoomPoint;
    [SerializeField] private Transform _patientCTreatmentRoomPoint;
    [SerializeField, Min(0f)] private float _patientBTreatmentMoveDurationSeconds = 1.5f;
    [SerializeField, Min(0f)] private float _patientCTreatmentMoveDurationSeconds = 1.5f;
    [SerializeField] private bool _autoAttachPatientBToTreatmentBed = true;
    [SerializeField] private bool _autoAttachPatientCToTreatmentBed = true;

    [SerializeField] private string _patientBVitalMonitorEntityIdentifier = "patientB_monitor";
    [SerializeField] private string[] _patientBVitalMonitorAliases = { "patientB_monitor", "patient_b_monitor", "monitorB" };
    [SerializeField] private string _patientCVitalMonitorEntityIdentifier = "patientC_monitor";
    [SerializeField] private string[] _patientCVitalMonitorAliases = { "patientC_monitor", "patient_c_monitor", "monitorC" };
    [SerializeField] private GameObject _patientBVitalMonitorObject;
    [SerializeField] private GameObject _patientCVitalMonitorObject;
    [SerializeField] private PatientMonitorController _patientBVitalMonitorController;
    [SerializeField] private PatientMonitorController _patientCVitalMonitorController;
    [SerializeField] private GameObject _patientBVitalPanel;
    [SerializeField] private GameObject _patientCVitalPanel;
    [SerializeField] private bool _applyPatientBInitialMonitorProfile = true;
    [SerializeField] private bool _applyPatientCInitialMonitorProfile = true;
    [SerializeField] private ECGParameters _patientBInitialMonitorParameters = new ECGParameters
    {
      bpm = 120f,
      pAmp = 0.16f,
      pWidth = 0.04f,
      qAmp = -0.14f,
      rAmp = 1.1f,
      sAmp = -0.22f,
      tAmp = 0.28f,
      tWidth = 0.08f,
      uAmp = 0f,
      stElevation = 0f,
      noise = 0.02f,
      irregularity = 0.04f,
      qrsWidthScale = 1.0f
    };
    [SerializeField] private GameObject _patientBPupilReflexUiPanel;
    [SerializeField] private GameObject _patientCPupilReflexUiPanel;
    [SerializeField] private GameObject _patientBPupilLeftReactiveIndicator;
    [SerializeField] private GameObject _patientBPupilRightFixedIndicator;
    [SerializeField] private GameObject _patientCPupilLeftFixedIndicator;
    [SerializeField] private GameObject _patientCPupilRightReactiveIndicator;

    [SerializeField] private Transform _patientBCtRoomPoint;
    [SerializeField] private Transform _patientCCtRoomPoint;
    [SerializeField, Min(0f)] private float _patientsToCtMoveDurationSeconds = 2.0f;
    [SerializeField] private GameObject _ctTransferFadePanel;
    [SerializeField, Min(0f)] private float _ctTransferFadeHoldSeconds = 1.0f;
    [SerializeField, Min(0f)] private float _ctTransferFadeAutoHideSeconds = 0f;
    [SerializeField] private GameObject _patientBGauzeVisual;
    [SerializeField] private GameObject _patientBGauzeWithPlasterVisual;
    [SerializeField] private GameObject _patientCGauzeVisual;
    [SerializeField] private GameObject _patientCGauzeWithPlasterVisual;
    [SerializeField] private GameObject _patientB20gRightVisual;
    [SerializeField] private GameObject _patientBNs1RightConnectedVisual;
    [SerializeField] private GameObject _patientC20gLeftVisual;
    [SerializeField] private GameObject _patientCNs1LeftConnectedVisual;
    [SerializeField] private ECGParameters _patientCInitialMonitorParameters = new ECGParameters
    {
      bpm = 120f,
      pAmp = 0.16f,
      pWidth = 0.04f,
      qAmp = -0.14f,
      rAmp = 1.1f,
      sAmp = -0.22f,
      tAmp = 0.28f,
      tWidth = 0.08f,
      uAmp = 0f,
      stElevation = 0f,
      noise = 0.02f,
      irregularity = 0.04f,
      qrsWidthScale = 1.0f
    };

    [Header("B_C_D_to_triage")]
    [SerializeField] private string _nurseBEntityIdentifier = "NurseB";
    [SerializeField] private string[] _nurseBAliases = { "NurseB", "nurse_b", "b" };
    [SerializeField] private string _nurseAEntityIdentifier = "NurseA";
    [SerializeField] private string[] _nurseAAliases = { "NurseA", "nurse_a", "a" };
    [SerializeField] private string _nurseCEntityIdentifier = "NurseC";
    [SerializeField] private string[] _nurseCAliases = { "NurseC", "nurse_c", "c" };
    [SerializeField] private string _nurseDEntityIdentifier = "NurseD";
    [SerializeField] private string[] _nurseDAliases = { "NurseD", "nurse_d", "d" };
    [SerializeField] private Transform _nurseATransform;
    [SerializeField] private Transform _nurseBTransform;
    [SerializeField] private Transform _nurseCTransform;
    [SerializeField] private Transform _nurseDTransform;
    [SerializeField] private Transform _triageArrivalPoint;
    [SerializeField] private Transform _playerATriagePoint;
    [SerializeField, Min(0f)] private float _playerAMoveToTriageDurationSeconds = 1.0f;
    [SerializeField] private bool _instantMoveBcd = true;
    [SerializeField, Min(0f)] private float _bcdMoveDurationSeconds = 1.0f;

    [Header("Auto Resolve")]
    [SerializeField] private bool _autoResolveReferencesFromRegistry = true;
    [SerializeField] private bool _logRegistrySnapshotOnEnable = true;
    [SerializeField, Min(0f)] private float _smokeTestStepDelaySeconds = 0.25f;

    private readonly List<string> _registeredEventIds = new();
    private ChatUIController _chatUi;

    private void Awake()
    {
      RegisterScenarioGraphs();
    }

    private void OnEnable()
    {
      RegisterIntroAndPatientAEvents();
      RegisterPatientBCEvents();

      if (_logRegistrySnapshotOnEnable)
      {
        StartCoroutine(LogRegistrySnapshotDeferred());
      }
    }

    private void OnDisable()
    {
      for (int i = 0; i < _registeredEventIds.Count; i++)
      {
        Registry.UnregisterScenarioEvent(_registeredEventIds[i]);
      }

      _registeredEventIds.Clear();
    }

    private void RegisterScenarioGraphs()
    {
      if (_disasterIntroMvpGraph == null || string.IsNullOrWhiteSpace(_disasterIntroMvpGraphIdentifier))
      {
        return;
      }

      Registry.Register(RegistryType.ScenarioGraph, _disasterIntroMvpGraphIdentifier, _disasterIntroMvpGraph);
    }

    private IEnumerator LogRegistrySnapshotDeferred()
    {
      // Defer one frame so other runtime registrations in Awake/Start can settle.
      yield return null;
      LogRegistrySnapshot();
    }

    [ContextMenu("Log Registry Snapshot")]
    private void LogRegistrySnapshot()
    {
      var npcs = Registry.GetAll<GameObject>(RegistryType.Npc);
      var entities = Registry.GetAllEntities();

      var sb = new StringBuilder(512);
      sb.AppendLine("[TriageScenarioEventBootstrap] Registry snapshot");
      sb.AppendLine($"- NPC keys: {npcs.Count}");
      foreach (var pair in npcs)
      {
        sb.Append("  - ").Append(pair.Key).AppendLine();
      }

      sb.AppendLine($"- Entity keys: {entities.Count}");
      foreach (var pair in entities)
      {
        var descriptor = pair.Value;
        if (descriptor == null)
        {
          sb.Append("  - ").Append(pair.Key).AppendLine(" (null)");
          continue;
        }

        sb.Append("  - ")
          .Append(pair.Key)
          .Append(" [")
          .Append(descriptor.EntityType)
          .AppendLine("]");
      }

      Debug.Log(sb.ToString());
    }

    [ContextMenu("Validate Event Wiring")]
    private void ValidateEventWiring()
    {
      var missing = new List<string>(64);

      // Core routing references used by multiple events.
      AppendMissingIfNull(missing, nameof(_triageArrivalPoint), _triageArrivalPoint);
      AppendMissingIfNull(missing, nameof(_playerATriagePoint), _playerATriagePoint);
      AppendMissingIfNull(missing, nameof(_patientATreatmentRoomPoint), _patientATreatmentRoomPoint);
      AppendMissingIfNull(missing, nameof(_patientBTreatmentRoomPoint), _patientBTreatmentRoomPoint);
      AppendMissingIfNull(missing, nameof(_patientCTreatmentRoomPoint), _patientCTreatmentRoomPoint);
      AppendMissingIfNull(missing, nameof(_patientBCtRoomPoint), _patientBCtRoomPoint);
      AppendMissingIfNull(missing, nameof(_patientCCtRoomPoint), _patientCCtRoomPoint);

      // Frequently toggled panels/visuals.
      AppendMissingIfNull(missing, nameof(_patientAUiPanel), _patientAUiPanel);
      AppendMissingIfNull(missing, nameof(_dummyAUiPanel), _dummyAUiPanel);
      AppendMissingIfNull(missing, nameof(_patientBUiPanel), _patientBUiPanel);
      AppendMissingIfNull(missing, nameof(_dummyBUiPanel), _dummyBUiPanel);
      AppendMissingIfNull(missing, nameof(_patientCUiPanel), _patientCUiPanel);
      AppendMissingIfNull(missing, nameof(_suctionChecklistUiPanel), _suctionChecklistUiPanel);
      AppendMissingIfNull(missing, nameof(_intuChecklistUiPanel), _intuChecklistUiPanel);
      AppendMissingIfNull(missing, nameof(_ivChecklistUiPanel), _ivChecklistUiPanel);
      AppendMissingIfNull(missing, nameof(_ctTransferFadePanel), _ctTransferFadePanel);

      if (missing.Count == 0)
      {
        Debug.Log("[TriageScenarioEventBootstrap] Validate Event Wiring: no missing critical references.", this);
        return;
      }

      var sb = new StringBuilder(256);
      sb.AppendLine("[TriageScenarioEventBootstrap] Validate Event Wiring: missing references");
      for (int i = 0; i < missing.Count; i++)
      {
        sb.Append("- ").AppendLine(missing[i]);
      }

      Debug.LogWarning(sb.ToString(), this);
    }

    [ContextMenu("Run Core Smoke Test")]
    private void RunCoreSmokeTest()
    {
      if (!Application.isPlaying)
      {
        Debug.LogWarning("[TriageScenarioEventBootstrap] Run Core Smoke Test is available only in Play Mode.", this);
        return;
      }

      StartCoroutine(RunCoreSmokeTestRoutine());
    }

    private void Register(string eventId, ScenarioEventIdentifierRegistry.ScenarioEventHandler handler)
    {
      if (string.IsNullOrWhiteSpace(eventId) || handler == null)
      {
        return;
      }

      Registry.RegisterScenarioEvent(eventId, handler);
      _registeredEventIds.Add(eventId);
    }

    

    private void ResolveRuntimeReferencesIfNeeded()
    {
      if (!_autoResolveReferencesFromRegistry)
      {
        return;
      }

      _patientAObject = ResolveEntityObject(_patientAObject, _patientAEntityIdentifier);
      _patientAObject ??= ResolveByAliases(_patientAAliases);

      _dummyAObject = ResolveEntityObject(_dummyAObject, _dummyAEntityIdentifier);
      _dummyAObject ??= ResolveByAliases(_dummyAAliases);

      _patientABedObject = ResolveEntityObject(_patientABedObject, _patientABedEntityIdentifier);
      _patientABedObject ??= ResolveByAliases(_patientABedAliases);

      _dummyABedObject = ResolveEntityObject(_dummyABedObject, _dummyABedEntityIdentifier);
      _dummyABedObject ??= ResolveByAliases(_dummyABedAliases);

      _patientATreatmentBedObject = ResolveEntityObject(_patientATreatmentBedObject, _patientATreatmentBedEntityIdentifier);
      _patientATreatmentBedObject ??= ResolveByAliases(_patientATreatmentBedAliases);

      _patientAVitalMonitorObject = ResolveEntityObject(_patientAVitalMonitorObject, _patientAVitalMonitorEntityIdentifier);
      _patientAVitalMonitorObject ??= ResolveByAliases(_patientAVitalMonitorAliases);
      if (_patientAVitalMonitorController == null && _patientAVitalMonitorObject != null)
      {
        _patientAVitalMonitorController = _patientAVitalMonitorObject.GetComponent<PatientMonitorController>();
      }

      _patientBObject = ResolveEntityObject(_patientBObject, _patientBEntityIdentifier);
      _patientBObject ??= ResolveByAliases(_patientBAliases);
      _patientCObject = ResolveEntityObject(_patientCObject, _patientCEntityIdentifier);
      _patientCObject ??= ResolveByAliases(_patientCAliases);
      _dummyBObject = ResolveEntityObject(_dummyBObject, _dummyBEntityIdentifier);
      _dummyBObject ??= ResolveByAliases(_dummyBAliases);

      _nurseATransform = ResolveEntityTransform(_nurseATransform, _nurseAEntityIdentifier);
      _nurseATransform ??= ResolveByAliases(_nurseAAliases)?.transform;

      _patientBTreatmentBedObject = ResolveEntityObject(_patientBTreatmentBedObject, _patientBTreatmentBedEntityIdentifier);
      _patientBTreatmentBedObject ??= ResolveByAliases(_patientBTreatmentBedAliases);
      _patientCTreatmentBedObject = ResolveEntityObject(_patientCTreatmentBedObject, _patientCTreatmentBedEntityIdentifier);
      _patientCTreatmentBedObject ??= ResolveByAliases(_patientCTreatmentBedAliases);

      _patientBVitalMonitorObject = ResolveEntityObject(_patientBVitalMonitorObject, _patientBVitalMonitorEntityIdentifier);
      _patientBVitalMonitorObject ??= ResolveByAliases(_patientBVitalMonitorAliases);
      if (_patientBVitalMonitorController == null && _patientBVitalMonitorObject != null)
      {
        _patientBVitalMonitorController = _patientBVitalMonitorObject.GetComponent<PatientMonitorController>();
      }

      _patientCVitalMonitorObject = ResolveEntityObject(_patientCVitalMonitorObject, _patientCVitalMonitorEntityIdentifier);
      _patientCVitalMonitorObject ??= ResolveByAliases(_patientCVitalMonitorAliases);
      if (_patientCVitalMonitorController == null && _patientCVitalMonitorObject != null)
      {
        _patientCVitalMonitorController = _patientCVitalMonitorObject.GetComponent<PatientMonitorController>();
      }

      _nurseBTransform = ResolveEntityTransform(_nurseBTransform, _nurseBEntityIdentifier);
      _nurseBTransform ??= ResolveByAliases(_nurseBAliases)?.transform;
      _nurseCTransform = ResolveEntityTransform(_nurseCTransform, _nurseCEntityIdentifier);
      _nurseCTransform ??= ResolveByAliases(_nurseCAliases)?.transform;
      _nurseDTransform = ResolveEntityTransform(_nurseDTransform, _nurseDEntityIdentifier);
      _nurseDTransform ??= ResolveByAliases(_nurseDAliases)?.transform;

      LogUnresolvedTargetsIfAny();
    }

    private GameObject ResolveEntityObject(GameObject current, string entityIdentifier)
    {
      if (current != null)
      {
        return current;
      }

      if (string.IsNullOrWhiteSpace(entityIdentifier))
      {
        return null;
      }

      var fromRegistry = Registry.Get<GameObject>(RegistryType.Entity, entityIdentifier)
                         ?? Registry.Get<GameObject>(RegistryType.Npc, entityIdentifier);
      if (fromRegistry != null)
      {
        return fromRegistry;
      }

      // Final fallback for authored scenes where entity registration is not yet complete.
      return GameObject.Find(entityIdentifier);
    }

    private Transform ResolveEntityTransform(Transform current, string entityIdentifier)
    {
      if (current != null)
      {
        return current;
      }

      var go = ResolveEntityObject(null, entityIdentifier);
      return go != null ? go.transform : null;
    }

    private GameObject ResolveByAliases(string[] aliases)
    {
      if (aliases == null || aliases.Length == 0)
      {
        return null;
      }

      // 1) Registry direct lookup by aliases.
      for (int i = 0; i < aliases.Length; i++)
      {
        var alias = aliases[i];
        if (string.IsNullOrWhiteSpace(alias))
        {
          continue;
        }

        var byRegistry = Registry.Get<GameObject>(RegistryType.Entity, alias)
                         ?? Registry.Get<GameObject>(RegistryType.Npc, alias);
        if (byRegistry != null)
        {
          return byRegistry;
        }
      }

      // 2) Find NPC component by authored Identifier.
      var npcs = FindObjectsByType<Npc>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < npcs.Length; i++)
      {
        var npc = npcs[i];
        if (npc == null)
        {
          continue;
        }

        string npcId = npc.Identifier;
        if (MatchesAnyAlias(npcId, aliases))
        {
          return npc.gameObject;
        }

        if (MatchesAnyAlias(npc.gameObject.name, aliases))
        {
          return npc.gameObject;
        }
      }

      // 3) Final fallback: global name search.
      for (int i = 0; i < aliases.Length; i++)
      {
        var alias = aliases[i];
        if (string.IsNullOrWhiteSpace(alias))
        {
          continue;
        }

        var byName = GameObject.Find(alias);
        if (byName != null)
        {
          return byName;
        }
      }

      return null;
    }

    private static bool MatchesAnyAlias(string value, string[] aliases)
    {
      if (string.IsNullOrWhiteSpace(value) || aliases == null)
      {
        return false;
      }

      for (int i = 0; i < aliases.Length; i++)
      {
        var alias = aliases[i];
        if (string.IsNullOrWhiteSpace(alias))
        {
          continue;
        }

        if (string.Equals(value, alias, System.StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }

        if (value.IndexOf(alias, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
          return true;
        }
      }

      return false;
    }

    private void LogUnresolvedTargetsIfAny()
    {
      bool unresolved = _patientAObject == null
                        || _dummyAObject == null
                        || _patientABedObject == null
                        || _dummyABedObject == null
                        || _patientATreatmentBedObject == null
                        || _patientAVitalMonitorObject == null
                        || _patientBObject == null
                        || _patientCObject == null
                        || _dummyBObject == null
                        || _patientBTreatmentBedObject == null
                        || _patientCTreatmentBedObject == null
                        || _patientBVitalMonitorObject == null
                        || _patientCVitalMonitorObject == null
                        || _nurseATransform == null
                        || _nurseBTransform == null
                        || _nurseCTransform == null
                        || _nurseDTransform == null;
      if (!unresolved)
      {
        return;
      }

      var sb = new StringBuilder(256);
      sb.AppendLine("[TriageScenarioEventBootstrap] Unresolved targets after auto-resolve:");
      if (_patientAObject == null) sb.AppendLine("- patientA (set _patientAEntityIdentifier or aliases)");
      if (_dummyAObject == null) sb.AppendLine("- dummyA (set _dummyAEntityIdentifier or aliases)");
      if (_patientABedObject == null) sb.AppendLine("- patientABed (set _patientABedEntityIdentifier or aliases)");
      if (_dummyABedObject == null) sb.AppendLine("- dummyABed (set _dummyABedEntityIdentifier or aliases)");
      if (_patientATreatmentBedObject == null) sb.AppendLine("- patientATreatmentBed (set _patientATreatmentBedEntityIdentifier or aliases)");
      if (_patientAVitalMonitorObject == null) sb.AppendLine("- patientAMonitor (set _patientAVitalMonitorEntityIdentifier or aliases)");
      if (_patientBObject == null) sb.AppendLine("- patientB (set _patientBEntityIdentifier or aliases)");
      if (_patientCObject == null) sb.AppendLine("- patientC (set _patientCEntityIdentifier or aliases)");
      if (_dummyBObject == null) sb.AppendLine("- dummyB (set _dummyBEntityIdentifier or aliases)");
      if (_patientBTreatmentBedObject == null) sb.AppendLine("- patientBTreatmentBed (set _patientBTreatmentBedEntityIdentifier or aliases)");
      if (_patientCTreatmentBedObject == null) sb.AppendLine("- patientCTreatmentBed (set _patientCTreatmentBedEntityIdentifier or aliases)");
      if (_patientBVitalMonitorObject == null) sb.AppendLine("- patientBMonitor (set _patientBVitalMonitorEntityIdentifier or aliases)");
      if (_patientCVitalMonitorObject == null) sb.AppendLine("- patientCMonitor (set _patientCVitalMonitorEntityIdentifier or aliases)");
      if (_nurseATransform == null) sb.AppendLine("- nurseA (set _nurseAEntityIdentifier or aliases)");
      if (_nurseBTransform == null) sb.AppendLine("- nurseB (set _nurseBEntityIdentifier or aliases)");
      if (_nurseCTransform == null) sb.AppendLine("- nurseC (set _nurseCEntityIdentifier or aliases)");
      if (_nurseDTransform == null) sb.AppendLine("- nurseD (set _nurseDEntityIdentifier or aliases)");
      Debug.LogWarning(sb.ToString());
    }

    private static Transform GetPreferredDestination(Transform preferred, Transform fallback)
    {
      return preferred != null ? preferred : fallback;
    }

    private void TryAttachPatientToBed(GameObject patientObject, GameObject bedObject, string patientLabel)
    {
      if (patientObject == null || bedObject == null)
      {
        return;
      }

      var patient = patientObject.GetComponent<PatientController>();
      var bed = bedObject.GetComponent<MovingPatientBedController>();
      if (patient == null || bed == null)
      {
        return;
      }

      if (patient.CurrentBed == bed)
      {
        return;
      }

      if (bed.TryReposeTarget(patient))
      {
        Debug.Log($"[TriageScenarioEventBootstrap] Attached {patientLabel} to bed {bed.name}.");
      }
    }

    /// <summary>
    /// 레지스트리에 등록된 식별자로 환자/침대를 찾아 논리 결합(repose)을 재설정한다.
    /// 프리셋 그룹 스폰(환자+침대 분리) 직후, 위계 없는 두 독립 객체를 "결합 상태"로 만들기 위해 사용한다.
    /// </summary>
    private bool TryAttachPatientToBedByIdentifier(string patientIdentifier, string bedIdentifier)
    {
      if (string.IsNullOrWhiteSpace(patientIdentifier) || string.IsNullOrWhiteSpace(bedIdentifier))
      {
        return false;
      }

      var patientObject = Registry.Get<GameObject>(RegistryType.Entity, patientIdentifier)
                          ?? Registry.Get<GameObject>(RegistryType.Npc, patientIdentifier);
      var bedObject = Registry.Get<GameObject>(RegistryType.Entity, bedIdentifier)
                      ?? Registry.Get<GameObject>(RegistryType.Npc, bedIdentifier);

      if (patientObject == null || bedObject == null)
      {
        Debug.LogWarning(
          $"[TriageScenarioEventBootstrap] Attach by identifier failed: " +
          $"patient '{patientIdentifier}'={(patientObject != null)}, bed '{bedIdentifier}'={(bedObject != null)} not resolved.");
        return false;
      }

      var patient = patientObject.GetComponent<PatientController>();
      var bed = bedObject.GetComponent<MovingPatientBedController>();
      if (patient == null || bed == null)
      {
        Debug.LogWarning(
          $"[TriageScenarioEventBootstrap] Attach by identifier failed: component missing " +
          $"(patient {patient != null}, bed {bed != null}).");
        return false;
      }

      if (patient.CurrentBed == bed)
      {
        return true;
      }

      if (bed.TryReposeTarget(patient))
      {
        Debug.Log($"[TriageScenarioEventBootstrap] Attached '{patientIdentifier}' to bed '{bedIdentifier}'.");
        return true;
      }

      return false;
    }

    private IEnumerator ShowPanelTemporarily(GameObject panel, float autoHideSeconds, string message)
    {
      if (panel == null)
      {
        EmitSystemMessage(message + " (패널 참조 없음)");
        yield break;
      }

      panel.SetActive(true);
      EmitSystemMessage(message);

      if (autoHideSeconds > 0f)
      {
        yield return new WaitForSeconds(autoHideSeconds);
        panel.SetActive(false);
      }
    }

    private IEnumerator ShowPupilReflexPanel(GameObject panel,
      GameObject reactiveIndicator,
      GameObject fixedIndicator,
      string message)
    {
      if (panel == null)
      {
        EmitSystemMessage(message + " (패널 참조 없음)");
        yield break;
      }

      panel.SetActive(true);
      SetActiveIfPresent(reactiveIndicator, true);
      SetActiveIfPresent(fixedIndicator, true);
      EmitSystemMessage(message);

      if (_uiPanelAutoHideSeconds > 0f)
      {
        yield return new WaitForSeconds(_uiPanelAutoHideSeconds);
        panel.SetActive(false);
      }
    }

    private void SetActiveIfPresent(GameObject target, bool active)
    {
      if (target != null)
      {
        target.SetActive(active);
      }
    }

    private void SnapToIfPresent(GameObject target, Transform destination)
    {
      if (target == null || destination == null)
      {
        return;
      }

      target.transform.position = destination.position;
      target.transform.rotation = destination.rotation;
    }

    private IEnumerator MoveToIfPresent(GameObject target, Transform destination, float durationSeconds)
    {
      if (target == null || destination == null)
      {
        yield break;
      }

      yield return MoveTransformTo(target.transform, destination, durationSeconds);
    }

    private void SnapTransformTo(Transform target, Transform destination)
    {
      if (target == null || destination == null)
      {
        return;
      }

      target.position = destination.position;
      target.rotation = destination.rotation;
    }

    private IEnumerator MoveTransformTo(Transform target, Transform destination, float durationSeconds)
    {
      if (target == null || destination == null)
      {
        yield break;
      }

      if (durationSeconds <= 0f)
      {
        SnapTransformTo(target, destination);
        yield break;
      }

      Vector3 startPos = target.position;
      Quaternion startRot = target.rotation;
      float elapsed = 0f;

      while (elapsed < durationSeconds)
      {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / durationSeconds);
        target.position = Vector3.Lerp(startPos, destination.position, t);
        target.rotation = Quaternion.Slerp(startRot, destination.rotation, t);
        yield return null;
      }

      target.position = destination.position;
      target.rotation = destination.rotation;
    }

    private void EmitSystemMessage(string message)
    {
      if (string.IsNullOrWhiteSpace(message))
      {
        return;
      }

      if (_chatUi == null)
      {
        _chatUi = Registry.Get<ChatUIController>(RegistryType.UI, Registry.TypeKey<ChatUIController>());
      }

      if (_chatUi != null)
      {
        _chatUi.AppendMessage($"<color=#FFD700>[System]</color> {message}", true);
      }

      Debug.Log($"[TriageScenarioEventBootstrap] {message}");
    }

    private IEnumerator ApplyPatientAMonitorProfile(ECGParameters parameters, string message)
    {
      ResolveRuntimeReferencesIfNeeded();

      yield return ApplyMonitorProfile(_patientAVitalMonitorObject,
        _patientAVitalPanel,
        _patientAVitalMonitorController,
        parameters,
        true,
        message);
    }

    private IEnumerator ApplyMonitorProfile(GameObject monitorObject,
      GameObject panelObject,
      PatientMonitorController monitorController,
      ECGParameters parameters,
      bool applyProfile,
      string message)
    {
      SetActiveIfPresent(monitorObject, true);
      SetActiveIfPresent(panelObject, true);

      if (monitorController == null)
      {
        EmitSystemMessage(message + " (모니터 컨트롤러 참조 없음)");
        yield break;
      }

      if (applyProfile)
      {
        monitorController.SetCustomParameters(parameters);
      }

      monitorController.enabled = true;
      EmitSystemMessage(message);
      yield break;
    }

    private static void SetAnimatorsBool(Animator[] animators, string boolName, bool value)
    {
      if (animators == null || string.IsNullOrWhiteSpace(boolName))
      {
        return;
      }

      for (int i = 0; i < animators.Length; i++)
      {
        var animator = animators[i];
        if (animator == null)
        {
          continue;
        }

        animator.SetBool(boolName, value);
      }
    }

    private IEnumerator RunCoreSmokeTestRoutine()
    {
      Debug.Log("[TriageScenarioEventBootstrap] Core smoke test started.", this);

      // Intro + patient A critical core flow.
      yield return RunSmokeStep("triage_patientA_dummyA", Event_TriagePatientAAndDummyA);
      yield return RunSmokeStep("show_patientA_ui", Event_ShowPatientAUi);
      yield return RunSmokeStep("show_dummyA_ui", Event_ShowDummyAUi);
      yield return RunSmokeStep("move_patientA_to_treatmentroom", Event_MovePatientAToTreatmentRoom);
      yield return RunSmokeStep("activate_vital_monitor_ui_patientA", Event_ActivateVitalMonitorUiPatientA);

      // Patient B/C entry + transition flow.
      yield return RunSmokeStep("triage_patientB_patientC_dummyB", Event_TriagePatientBPatientCDummyB);
      yield return RunSmokeStep("move_patientB", Event_MovePatientB);
      yield return RunSmokeStep("move_patientC", Event_MovePatientC);
      yield return RunSmokeStep("move_patients_to_CT", Event_MovePatientsToCt);

      Debug.Log("[TriageScenarioEventBootstrap] Core smoke test finished.", this);
    }

    private IEnumerator RunSmokeStep(string eventName, Func<IEnumerator> handler)
    {
      if (handler == null)
      {
        yield break;
      }

      Debug.Log($"[TriageScenarioEventBootstrap] Smoke step: {eventName}", this);
      yield return StartCoroutine(handler());

      if (_smokeTestStepDelaySeconds > 0f)
      {
        yield return new WaitForSeconds(_smokeTestStepDelaySeconds);
      }
    }

    private static void AppendMissingIfNull(List<string> missing, string fieldName, UnityEngine.Object target)
    {
      if (missing == null || string.IsNullOrWhiteSpace(fieldName))
      {
        return;
      }

      if (target == null)
      {
        missing.Add(fieldName);
      }
    }
  }
}
