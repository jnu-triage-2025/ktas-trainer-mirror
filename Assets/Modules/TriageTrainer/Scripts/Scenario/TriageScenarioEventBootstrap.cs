using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity;
using TriageTrainer.Entity.Patient;
using TriageTrainer.Entity.PatientMonitor.Models;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  /// <summary>
  /// 기반 인프라를 수정하지 않고 TriageTrainer 시나리오 이벤트 핸들러를 등록한다.
  ///
  /// 참고:
  /// - 현재 핸들러는 MVP 연결을 위한 안전한 플레이스홀더다.
  /// - 각 코루틴 본문을 실제 표시/상호작용 로직으로 점진적으로 교체한다.
  /// </summary>
  public partial class TriageScenarioEventBootstrap : MonoBehaviour
  {
    [Header("Scenario Graph Registration")]
    [SerializeField] private TextAsset _disasterIntroMvpGraph;
    [SerializeField] private string _disasterIntroMvpGraphIdentifier = "disaster_intro_mvp";

    [Header("triage_patient_a_patient_dummy_d_a")]
    [SerializeField] private string _patientAEntityIdentifier = "patientA";
    [SerializeField] private string[] _patientAAliases = { "patientA", "patient_a", "patient" };
    [SerializeField] private string _patientDummyDAEntityIdentifier = "patientDummyDA";
    [SerializeField] private string[] _patientDummyDAAliases = { "patientDummyDA", "patient_dummy_d_a" };
    [SerializeField] private string _patientABedEntityIdentifier = "patientABed";
    [SerializeField] private string[] _patientABedAliases = { "patientABed", "patient_a_bed", "bedA" };
    [SerializeField] private string _patientDummyDABedEntityIdentifier = "patientDummyDABed";
    [SerializeField] private string[] _patientDummyDABedAliases = { "patientDummyDABed", "patient_dummy_d_a_bed", "bed_d_a" };
    [SerializeField] private GameObject _patientAObject;
    [SerializeField] private GameObject _patientDummyDAObject;
    [SerializeField] private GameObject _patientABedObject;
    [SerializeField] private GameObject _patientDummyDABedObject;
    [SerializeField] private Transform _patientASpawnPoint;
    [SerializeField] private Transform _patientDummyDASpawnPoint;
    [SerializeField] private Transform _patientABedSpawnPoint;
    [SerializeField] private Transform _patientDummyDABedSpawnPoint;
    [SerializeField, Min(0f)] private float _spawnMoveDurationSeconds = 0f;
    [SerializeField] private bool _autoAttachPatientsToBeds = true;
    [SerializeField] private bool _preferMovingBedsForSpawn = true;

    [Header("show_patientA_ui / show_patientDummyDA_ui")]
    [SerializeField] private GameObject _patientAUiPanel;
    [SerializeField] private GameObject _patientDummyDAUiPanel;
    [SerializeField, Min(0f)] private float _uiPanelAutoHideSeconds = 0f;

    [Header("patient_a_critical (P1 MVP)")]
    [SerializeField] private string _patientATreatmentBedEntityIdentifier = "patientATreatmentBed";
    [SerializeField] private string[] _patientATreatmentBedAliases = { "patientATreatmentBed", "patient_a_treatment_bed", "treatmentBedA" };
    [SerializeField] private GameObject _patientATreatmentBedObject;
    [SerializeField] private Transform _patientATreatmentRoomPoint;
    [SerializeField, Min(0f)] private float _patientATreatmentMoveDurationSeconds = 1.5f;
    [SerializeField] private bool _autoAttachPatientAToTreatmentBed = true;
    [SerializeField] private bool _waitForManualPatientATransfer = true;
    [SerializeField] private string _patientATreatmentPositioningPointIdentifier = "treatmentroom_patient_a";
    [Tooltip("수동 진입에서 환자 A 침대를 정박시킬 포지셔닝 포인트입니다. 일반 진행의 이송 목표와 달리 " +
             "수동 진입은 침대를 밀고 오는 과정이 없으므로, 이 포인트로 바로 붙입니다.")]
    [SerializeField] private string _patientAManualEntryBedSnapPointIdentifier = "zone_a:bed_snap_point";
    [SerializeField, Min(0f)] private float _patientATransferWaitTimeoutSeconds = 0f;
    [SerializeField, Min(0f)] private float _patientADismountWaitTimeoutSeconds = 0f;

    [SerializeField] private string _patientAVitalMonitorEntityIdentifier = "patientA_monitor";
    [SerializeField] private string[] _patientAVitalMonitorAliases = { "patientA_monitor", "patient_a_monitor", "monitorA" };
    [SerializeField] private GameObject _patientAVitalMonitorObject;
    [SerializeField] private PatientMonitorController _patientAVitalMonitorController;
    [SerializeField] private GameObject _patientAVitalPanel;
    [SerializeField] private bool _applyPatientAInitialMonitorProfile = true;
    [SerializeField]
    private ECGParameters _patientAInitialMonitorParameters = new ECGParameters
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
    [SerializeField]
    private ECGParameters _patientACrashMonitorParameters = new ECGParameters
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
    [SerializeField]
    private ECGParameters _patientAAsystoleMonitorParameters = new ECGParameters
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
    [SerializeField]
    private ECGParameters _patientARoscMonitorParameters = new ECGParameters
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
    [SerializeField] private PatientAEtTubePreparedVisualMarker _patientAEtTubePreparedVisual;
    [SerializeField] private PatientAEtTubeInsertedVisualMarker _patientAEtTubeInsertedVisual;
    [SerializeField] private GameObject _patientAEtTubeWithoutStyletVisual;
    [SerializeField] private PatientATPieceConnectedVisualMarker _patientATPieceConnectedVisual;
    [SerializeField] private PatientAGauzeVisualMarker _patientAGauzeVisual;
    [SerializeField] private PatientAGauzeWithPlasterVisualMarker _patientAGauzeWithPlasterVisual;
    [SerializeField] private PatientA18gLeftVisualMarker _patientA18gLeftVisual;
    [SerializeField] private GameObject _patientANs1LeftConnectedVisual;
    [SerializeField] private PatientA18gRightVisualMarker _patientA18gRightVisual;
    [SerializeField] private GameObject _patientAPs1RightConnectedVisual;
    [SerializeField] private PatientACentralLineVisualMarker _patientACentralLineVisual;
    [SerializeField] private GameObject _level1ReadyVisual;
    [SerializeField] private PatientAAmbuConnectedVisualMarker _patientAAmbuConnectedVisual;
    [SerializeField] private GameObject _patientADefibrillatorPadVisual;
    [SerializeField] private GameObject _defibrillatorIrregularUiPanel;
    [SerializeField]
    private ECGParameters _patientADefibrillatorIrregularMonitorParameters = new ECGParameters
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
    [Header("patient_a_critical CPR animation clips")]
    [SerializeField] private AnimationClip _chestCompressionAnimationClip;
    [SerializeField] private AnimationClip _cprReceivingPatientAnimationClip;
    [SerializeField] private Vector3 _cprReceivingPatientPositionOffset =
      new Vector3(0f, 0.425f, 0f);
    [SerializeField, Min(0f)] private float _cprPerformingPlayerHeightOffset = 0.85f;

    [Header("patient_b_c_ct intro (MVP)")]
    [SerializeField] private string _patientBEntityIdentifier = "patientB";
    [SerializeField] private string[] _patientBAliases = { "patientB", "patient_b" };
    [SerializeField] private string _patientCEntityIdentifier = "patientC";
    [SerializeField] private string[] _patientCAliases = { "patientC", "patient_c" };
    [SerializeField] private string _patientDummyDBEntityIdentifier = "patientDummyDB";
    [SerializeField] private string[] _patientDummyDBAliases = { "patientDummyDB", "patient_dummy_d_b" };
    [SerializeField] private GameObject _patientBObject;
    [SerializeField] private GameObject _patientCObject;
    [SerializeField] private GameObject _patientDummyDBObject;
    [SerializeField] private Transform _patientBSpawnPoint;
    [SerializeField] private Transform _patientCSpawnPoint;
    [SerializeField] private Transform _patientDummyDBSpawnPoint;
    [SerializeField, Min(0f)] private float _patientBcdSpawnMoveDurationSeconds = 0f;
    [SerializeField] private GameObject _patientBUiPanel;
    [SerializeField] private GameObject _patientCUiPanel;
    [SerializeField] private GameObject _patientDummyDBUiPanel;
    [SerializeField] private string _patientBTreatmentBedEntityIdentifier = "patientBTreatmentBed";
    [SerializeField] private string[] _patientBTreatmentBedAliases = { "patientBTreatmentBed", "patient_b_treatment_bed", "treatmentBedB" };
    [SerializeField] private string _patientCTreatmentBedEntityIdentifier = "patientCTreatmentBed";
    [SerializeField] private string[] _patientCTreatmentBedAliases = { "patientCTreatmentBed", "patient_c_treatment_bed", "treatmentBedC" };
    [Tooltip("시나리오 프리셋 스폰이 만드는 환자 B 침대의 엔티티 식별자. 씬에 사전 배치된 침대가 아니므로 별도로 해석한다.")]
    [SerializeField] private string _patientBSpawnedBedEntityIdentifier = "bed_b";
    [Tooltip("시나리오 프리셋 스폰이 만드는 환자 C 침대의 엔티티 식별자. 씬에 사전 배치된 침대가 아니므로 별도로 해석한다.")]
    [SerializeField] private string _patientCSpawnedBedEntityIdentifier = "bed_c";
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
    [SerializeField]
    private ECGParameters _patientBInitialMonitorParameters = new ECGParameters
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
    [SerializeField] private PatientBGauzeVisualMarker _patientBGauzeVisual;
    [SerializeField] private PatientBGauzeWithPlasterVisualMarker _patientBGauzeWithPlasterVisual;
    [SerializeField] private PatientCGauzeVisualMarker _patientCGauzeVisual;
    [SerializeField] private PatientCGauzeWithPlasterVisualMarker _patientCGauzeWithPlasterVisual;
    [SerializeField] private PatientB20gRightVisualMarker _patientB20gRightVisual;
    [SerializeField] private GameObject _patientBNs1RightConnectedVisual;
    [SerializeField] private PatientC20gLeftVisualMarker _patientC20gLeftVisual;
    [SerializeField] private GameObject _patientCNs1LeftConnectedVisual;
    [SerializeField]
    private ECGParameters _patientCInitialMonitorParameters = new ECGParameters
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
    private ScenarioController _questStateFlagScopeController;
    private ChatUIController _chatUi;
    private QuestUIController _questUi;

    /// <summary>
    /// 활성화된 부트스트랩 인스턴스. 두 번째 인스턴스가 붙는 상황을 막기 위한 기준값이다.
    ///
    /// <para>
    /// 상호작용 처리는 <c>ScenarioActionInteractable.OnInteractionCompleted</c> 라는 static 이벤트를
    /// 거치므로 살아 있는 인스턴스가 모두 실행하는 반면, 시나리오 이벤트 핸들러 사전은 식별자마다
    /// 마지막 등록만 남긴다. 그래서 인스턴스가 둘이면 CPR PlayableGraph 와 모델 Y 오프셋을 두 번
    /// 걸어 놓고 한 번만 해제하게 되어, 환자가 CPR 자세를 유지한 채 침대 아래로 내려앉는다.
    /// </para>
    ///
    /// <para>
    /// 먼저 활성화된 인스턴스를 유지하고 나중에 올라온 쪽을 비활성화한다. 반대로 처리하면 이미
    /// 등록해 둔 이벤트를 나중 인스턴스가 덮어쓴 뒤 앞선 인스턴스의 <c>OnDisable</c> 이 같은
    /// 식별자를 그대로 지워 버려서, 시나리오 이벤트가 아무 데도 남지 않는다.
    /// </para>
    /// </summary>
    private static TriageScenarioEventBootstrap _activeInstance;

    private void Awake()
    {
      // 여기서는 활성 인스턴스를 선점하지 않는다. 비활성 상태로 배치된 컴포넌트도 Awake 는 실행되므로,
      // 선점해 버리면 정작 켜져 있는 인스턴스가 중복으로 판정된다. 선점은 OnEnable 에서 수행한다.
      if (IsSupersededByActiveInstance())
      {
        return;
      }

      EnsurePatientAWorldAnchors();
      RecoverTypedReferences();
      RegisterScenarioGraphs();
    }

    private void OnEnable()
    {
      if (!TryBecomeActiveInstance())
      {
        return;
      }

      ScenarioActionInteractable.OnInteractionCompleted += HandleScenarioActionInteractionCompleted;
      EnablePatientATreatmentSignalHandlers();
      RegisterIntroAndPatientAEvents();
      RegisterPatientBCEvents();
      SubscribeQuestStateFlagScope();

      if (_logRegistrySnapshotOnEnable)
      {
        StartCoroutine(LogRegistrySnapshotDeferred());
      }
    }

    /// <summary>
    /// 이 인스턴스를 활성 인스턴스로 삼을 수 있으면 <c>true</c> 를 반환한다. 이미 다른 인스턴스가
    /// 활성 상태라면 자기 자신을 비활성화한 뒤 <c>false</c> 를 반환한다. 앞선 인스턴스가 파괴되어
    /// 참조가 비어 있으면 이 인스턴스가 그 자리를 이어받는다.
    /// </summary>
    private bool TryBecomeActiveInstance()
    {
      if (_activeInstance == this)
      {
        return true;
      }

      if (IsSupersededByActiveInstance())
      {
        return false;
      }

      _activeInstance = this;
      return true;
    }

    /// <summary>
    /// 다른 인스턴스가 이미 활성 상태이면 경고를 남기고 이 인스턴스를 비활성화한 뒤 <c>true</c> 를
    /// 반환한다.
    /// </summary>
    private bool IsSupersededByActiveInstance()
    {
      if (_activeInstance == null || _activeInstance == this)
      {
        return false;
      }

      Debug.LogWarning(
        "[TriageScenarioEventBootstrap] 부트스트랩 인스턴스가 이미 활성 상태이므로 이 인스턴스를 "
        + $"비활성화합니다. (활성='{_activeInstance.gameObject.scene.name}/{_activeInstance.name}', "
        + $"중복='{gameObject.scene.name}/{name}') 씬 구성에서 중복된 컴포넌트를 제거해 주세요.",
        this);
      enabled = false;
      return true;
    }

    private void OnDisable()
    {
      // 중복으로 판정되어 비활성화된 인스턴스는 아무것도 구독하거나 등록한 적이 없다. 여기서 해제를
      // 수행하면 활성 인스턴스가 등록해 둔 시나리오 이벤트까지 함께 지워진다.
      if (_activeInstance != this)
      {
        return;
      }

      _activeInstance = null;
      ScenarioActionInteractable.OnInteractionCompleted -= HandleScenarioActionInteractionCompleted;
      StopPatientACprAnimations();
      DisablePatientATreatmentSignalHandlers();
      UnsubscribeQuestStateFlagScope();
      DisposePatientBCFinalFadeOverlay();
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
        // 등록을 건너뛰면 시나리오를 시작하는 시점에야 "그래프 없음"으로 드러난다.
        // 원인이 인스펙터 참조 누락이라는 사실을 여기서 남겨 둔다.
        Debug.LogWarning(
          "[TriageScenarioEventBootstrap] 시나리오 그래프를 등록하지 못했습니다. "
          + $"그래프 에셋={(_disasterIntroMvpGraph == null ? "(미할당)" : _disasterIntroMvpGraph.name)}, "
          + $"식별자='{_disasterIntroMvpGraphIdentifier}'.",
          this);
        return;
      }

      Registry.Register(RegistryType.ScenarioGraph, _disasterIntroMvpGraphIdentifier, _disasterIntroMvpGraph);
    }

    private IEnumerator LogRegistrySnapshotDeferred()
    {
      // 한 프레임 미뤄서 Awake/Start 의 다른 런타임 등록이 안정될 시간을 준다.
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
      RecoverTypedReferences();
      var missing = new List<string>(64);

      // 여러 이벤트가 공유하는 핵심 라우팅 참조.
      AppendMissingIfNull(missing, nameof(_triageArrivalPoint), _triageArrivalPoint);
      AppendMissingIfNull(missing, nameof(_playerATriagePoint), _playerATriagePoint);
      AppendMissingIfNull(missing, nameof(_patientATreatmentRoomPoint), _patientATreatmentRoomPoint);
      AppendMissingIfNull(missing, nameof(_patientBTreatmentRoomPoint), _patientBTreatmentRoomPoint);
      AppendMissingIfNull(missing, nameof(_patientCTreatmentRoomPoint), _patientCTreatmentRoomPoint);
      AppendMissingIfNull(missing, nameof(_patientBCtRoomPoint), _patientBCtRoomPoint);
      AppendMissingIfNull(missing, nameof(_patientCCtRoomPoint), _patientCCtRoomPoint);

      // 자주 켜고 끄는 패널/표시 요소.
      AppendMissingIfNull(missing, nameof(_patientAUiPanel), _patientAUiPanel);
      AppendMissingIfNull(missing, nameof(_patientDummyDAUiPanel), _patientDummyDAUiPanel);
      AppendMissingIfNull(missing, nameof(_patientBUiPanel), _patientBUiPanel);
      AppendMissingIfNull(missing, nameof(_patientDummyDBUiPanel), _patientDummyDBUiPanel);
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

      Debug.LogError(sb.ToString(), this);
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

    // ── 퀘스트 상태 플래그 게이트 범위 ────────────────────────────────────
    // patient_a_critical 은 상호작용 노출을 플레이어별 퀘스트 상태 플래그로 판정한다. 게이트는 그
    // 시나리오가 도는 동안에만 켜져야 하므로, 시나리오 시작·종료에 맞춰 여닫는다. 표시 전용 피어에서도
    // 같은 이벤트가 발생하므로 별도 분기 없이 모든 피어에서 동일하게 동작한다.

    private void SubscribeQuestStateFlagScope()
    {
      ScenarioController.InstanceAvailable += HandleScenarioControllerAvailableForFlagScope;
      BindQuestStateFlagScope(ScenarioController.Instance);
    }

    private void UnsubscribeQuestStateFlagScope()
    {
      ScenarioController.InstanceAvailable -= HandleScenarioControllerAvailableForFlagScope;
      UnbindQuestStateFlagScope();
      PatientACriticalQuestStateFlags.Disarm();
    }

    private void HandleScenarioControllerAvailableForFlagScope(ScenarioController controller)
      => BindQuestStateFlagScope(controller);

    private void BindQuestStateFlagScope(ScenarioController controller)
    {
      if (controller == null || ReferenceEquals(_questStateFlagScopeController, controller))
        return;

      UnbindQuestStateFlagScope();
      _questStateFlagScopeController = controller;
      controller.OnScenarioStarted += HandleScenarioStartedForFlagScope;
      controller.OnScenarioEnded += HandleScenarioEndedForFlagScope;
    }

    private void UnbindQuestStateFlagScope()
    {
      if (_questStateFlagScopeController == null)
        return;

      _questStateFlagScopeController.OnScenarioStarted -= HandleScenarioStartedForFlagScope;
      _questStateFlagScopeController.OnScenarioEnded -= HandleScenarioEndedForFlagScope;
      _questStateFlagScopeController = null;
    }

    private void HandleScenarioStartedForFlagScope()
      => PatientACriticalQuestStateFlags.ArmFor(_questStateFlagScopeController?.CurrentGraph?.Identifier);

    private void HandleScenarioEndedForFlagScope()
      => PatientACriticalQuestStateFlags.Disarm();



    private void ResolveRuntimeReferencesIfNeeded()
    {
      if (!_autoResolveReferencesFromRegistry)
      {
        return;
      }

      _patientAObject = ResolveEntityObject(_patientAObject, _patientAEntityIdentifier);
      _patientAObject ??= ResolveByAliases(_patientAAliases);

      _patientDummyDAObject = ResolveEntityObject(_patientDummyDAObject, _patientDummyDAEntityIdentifier);
      _patientDummyDAObject ??= ResolveByAliases(_patientDummyDAAliases);

      _patientABedObject = ResolveEntityObject(_patientABedObject, _patientABedEntityIdentifier);
      _patientABedObject ??= ResolveByAliases(_patientABedAliases);

      _patientDummyDABedObject = ResolveEntityObject(_patientDummyDABedObject, _patientDummyDABedEntityIdentifier);
      _patientDummyDABedObject ??= ResolveByAliases(_patientDummyDABedAliases);

      _patientATreatmentBedObject = ResolveEntityObject(_patientATreatmentBedObject, _patientATreatmentBedEntityIdentifier);
      _patientATreatmentBedObject ??= ResolveByAliases(_patientATreatmentBedAliases);

      _patientAVitalMonitorObject = ResolveEntityObject(_patientAVitalMonitorObject, _patientAVitalMonitorEntityIdentifier);
      _patientAVitalMonitorObject ??= ResolveByAliases(_patientAVitalMonitorAliases);
      if (_patientAVitalMonitorController == null && _patientAVitalMonitorObject != null)
      {
        // 씬의 모니터 프리팹은 컨트롤러를 루트가 아닌 하위 화면 오브젝트에 둘 수 있다.
        // 루트만 조회하면 상태 전환 이벤트는 패널만 켜고 실제 파형은 갱신하지 못한다.
        _patientAVitalMonitorController = _patientAVitalMonitorObject.GetComponentInChildren<PatientMonitorController>(true);
      }

      _patientBObject = ResolveEntityObject(_patientBObject, _patientBEntityIdentifier);
      _patientBObject ??= ResolveByAliases(_patientBAliases);
      _patientCObject = ResolveEntityObject(_patientCObject, _patientCEntityIdentifier);
      _patientCObject ??= ResolveByAliases(_patientCAliases);
      _patientDummyDBObject = ResolveEntityObject(_patientDummyDBObject, _patientDummyDBEntityIdentifier);
      _patientDummyDBObject ??= ResolveByAliases(_patientDummyDBAliases);

      _nurseATransform = ResolveEntityTransform(_nurseATransform, _nurseAEntityIdentifier);
      _nurseATransform ??= ResolveByAliases(_nurseAAliases)?.transform;

      _patientBTreatmentBedObject = ResolveEntityObject(_patientBTreatmentBedObject, _patientBTreatmentBedEntityIdentifier);
      _patientBTreatmentBedObject ??= ResolveByAliases(_patientBTreatmentBedAliases);
      // 환자 B/C의 처치 침대는 씬에 배치되어 있지 않고 시나리오 프리셋 스폰(bed_b / bed_c)으로 생성된다.
      // 씬 별칭만으로는 영원히 해석되지 않으므로, 스폰 식별자와 환자가 실제로 누워 있는 침대를 차례로 본다.
      _patientBTreatmentBedObject ??= ResolveEntityObject(null, _patientBSpawnedBedEntityIdentifier);
      _patientBTreatmentBedObject ??= ResolveAttachedBedObject(_patientBObject);
      _patientCTreatmentBedObject = ResolveEntityObject(_patientCTreatmentBedObject, _patientCTreatmentBedEntityIdentifier);
      _patientCTreatmentBedObject ??= ResolveByAliases(_patientCTreatmentBedAliases);
      _patientCTreatmentBedObject ??= ResolveEntityObject(null, _patientCSpawnedBedEntityIdentifier);
      _patientCTreatmentBedObject ??= ResolveAttachedBedObject(_patientCObject);

      _patientBVitalMonitorObject = ResolveEntityObject(_patientBVitalMonitorObject, _patientBVitalMonitorEntityIdentifier);
      _patientBVitalMonitorObject ??= ResolveByAliases(_patientBVitalMonitorAliases);
      if (_patientBVitalMonitorController == null && _patientBVitalMonitorObject != null)
      {
        _patientBVitalMonitorController = _patientBVitalMonitorObject.GetComponentInChildren<PatientMonitorController>(true);
      }

      _patientCVitalMonitorObject = ResolveEntityObject(_patientCVitalMonitorObject, _patientCVitalMonitorEntityIdentifier);
      _patientCVitalMonitorObject ??= ResolveByAliases(_patientCVitalMonitorAliases);
      if (_patientCVitalMonitorController == null && _patientCVitalMonitorObject != null)
      {
        _patientCVitalMonitorController = _patientCVitalMonitorObject.GetComponentInChildren<PatientMonitorController>(true);
      }

      _nurseBTransform = ResolveEntityTransform(_nurseBTransform, _nurseBEntityIdentifier);
      _nurseBTransform ??= ResolveByAliases(_nurseBAliases)?.transform;
      _nurseCTransform = ResolveEntityTransform(_nurseCTransform, _nurseCEntityIdentifier);
      _nurseCTransform ??= ResolveByAliases(_nurseCAliases)?.transform;
      _nurseDTransform = ResolveEntityTransform(_nurseDTransform, _nurseDEntityIdentifier);
      _nurseDTransform ??= ResolveByAliases(_nurseDAliases)?.transform;

      RecoverTypedReferences();
      LogUnresolvedTargetsIfAny();
    }

    /// <summary>환자가 이미 결합(repose)된 침대를 반환한다. 프리셋 스폰 침대는 씬 참조가 없으므로 이 경로가 최후 수단이다.</summary>
    private static GameObject ResolveAttachedBedObject(GameObject patientObject)
    {
      var patient = patientObject != null ? patientObject.GetComponentInChildren<PatientController>(true) : null;
      var bed = patient != null ? patient.CurrentBed : null;
      return bed != null ? bed.gameObject : null;
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

      // 엔티티 등록이 아직 끝나지 않은 작성 씬을 위한 최종 폴백.
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

      // 1) Registry 의 별칭 직접 조회.
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

      // 2) 작성된 Identifier 로 NPC 컴포넌트를 찾는다.
      var npcs = FindObjectsByType<Npc>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
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

      // 3) 최종 폴백: 전역 이름 탐색.
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

      }

      return false;
    }

    private void LogUnresolvedTargetsIfAny()
    {
      bool unresolved = _patientAObject == null
                        || _patientDummyDAObject == null
                        || _patientABedObject == null
                        || _patientDummyDABedObject == null
                        || _patientATreatmentBedObject == null
                        || _patientAVitalMonitorObject == null
                        || _patientBObject == null
                        || _patientCObject == null
                        || _patientDummyDBObject == null
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
        _lastUnresolvedTargetsLog = null;
        return;
      }

      var sb = new StringBuilder(256);
      sb.AppendLine("[TriageScenarioEventBootstrap] Unresolved targets after auto-resolve:");
      if (_patientAObject == null)
        sb.AppendLine("- patientA (set _patientAEntityIdentifier or aliases)");
      if (_patientDummyDAObject == null)
        sb.AppendLine("- patientDummyDA (set _patientDummyDAEntityIdentifier or aliases)");
      if (_patientABedObject == null)
        sb.AppendLine("- patientABed (set _patientABedEntityIdentifier or aliases)");
      if (_patientDummyDABedObject == null)
        sb.AppendLine("- patientDummyDABed (set _patientDummyDABedEntityIdentifier or aliases)");
      if (_patientATreatmentBedObject == null)
        sb.AppendLine("- patientATreatmentBed (set _patientATreatmentBedEntityIdentifier or aliases)");
      if (_patientAVitalMonitorObject == null)
        sb.AppendLine("- patientAMonitor (set _patientAVitalMonitorEntityIdentifier or aliases)");
      if (_patientBObject == null)
        sb.AppendLine("- patientB (set _patientBEntityIdentifier or aliases)");
      if (_patientCObject == null)
        sb.AppendLine("- patientC (set _patientCEntityIdentifier or aliases)");
      if (_patientDummyDBObject == null)
        sb.AppendLine("- patientDummyDB (set _patientDummyDBEntityIdentifier or aliases)");
      if (_patientBTreatmentBedObject == null)
        sb.AppendLine("- patientBTreatmentBed (set _patientBTreatmentBedEntityIdentifier or aliases)");
      if (_patientCTreatmentBedObject == null)
        sb.AppendLine("- patientCTreatmentBed (set _patientCTreatmentBedEntityIdentifier or aliases)");
      if (_patientBVitalMonitorObject == null)
        sb.AppendLine("- patientBMonitor (set _patientBVitalMonitorEntityIdentifier or aliases)");
      if (_patientCVitalMonitorObject == null)
        sb.AppendLine("- patientCMonitor (set _patientCVitalMonitorEntityIdentifier or aliases)");
      if (_nurseATransform == null)
        sb.AppendLine("- nurseA (set _nurseAEntityIdentifier or aliases)");
      if (_nurseBTransform == null)
        sb.AppendLine("- nurseB (set _nurseBEntityIdentifier or aliases)");
      if (_nurseCTransform == null)
        sb.AppendLine("- nurseC (set _nurseCEntityIdentifier or aliases)");
      if (_nurseDTransform == null)
        sb.AppendLine("- nurseD (set _nurseDEntityIdentifier or aliases)");

      // 준비 대기 루프가 매 프레임 해석을 재시도하므로, 같은 내용이 반복 출력되지 않도록 변화가 있을 때만 남긴다.
      string message = sb.ToString();
      if (string.Equals(_lastUnresolvedTargetsLog, message, StringComparison.Ordinal))
      {
        return;
      }

      _lastUnresolvedTargetsLog = message;
      Debug.LogWarning(message);
    }

    private string _lastUnresolvedTargetsLog;

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
    private bool TryAttachPatientToBedByIdentifier(
      string patientIdentifier,
      string bedIdentifier,
      bool logFailure = true)
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
        if (logFailure)
        {
          Debug.LogWarning(
            $"[TriageScenarioEventBootstrap] Attach by identifier failed: " +
            $"patient '{patientIdentifier}'={(patientObject != null)}, bed '{bedIdentifier}'={(bedObject != null)} not resolved.");
        }
        return false;
      }

      var patient = patientObject.GetComponent<PatientController>();
      var bed = bedObject.GetComponent<MovingPatientBedController>();
      if (patient == null || bed == null)
      {
        if (logFailure)
        {
          Debug.LogWarning(
            $"[TriageScenarioEventBootstrap] Attach by identifier failed: component missing " +
            $"(patient {patient != null}, bed {bed != null}).");
        }
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

    private void SetActiveIfPresent(Component target, bool active)
    {
      if (target != null)
      {
        target.gameObject.SetActive(active);
      }
    }

    private void ToggleChecklistPanel(ref GameObject panel,
      bool active,
      string message,
      string[] candidateObjectNames)
    {
      panel = ResolveChecklistPanel(panel, candidateObjectNames);
      if (panel != null)
      {
        panel.SetActive(active);
        EmitSystemMessage(message);
        return;
      }

      if (_questUi == null)
      {
        _questUi = Registry.Get<QuestUIController>(RegistryType.UI, Registry.TypeKey<QuestUIController>());
        _questUi ??= FindFirstObjectByType<QuestUIController>(FindObjectsInactive.Include);
      }

      if (_questUi != null)
      {
        if (active)
        {
          _questUi.Open();
        }
        else if (_questUi.IsOpen)
        {
          _questUi.Close();
        }

        EmitSystemMessage($"{message} (퀘스트 UI 폴백)");
        return;
      }

      EmitSystemMessage($"{message} (체크리스트 UI 참조 없음)");
    }

    private static GameObject ResolveChecklistPanel(GameObject current, string[] candidateObjectNames)
    {
      if (current != null)
      {
        return current;
      }

      if (candidateObjectNames == null || candidateObjectNames.Length == 0)
      {
        return null;
      }

      var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      GameObject match = null;
      for (int i = 0; i < transforms.Length; i++)
      {
        var tr = transforms[i];
        if (tr == null)
        {
          continue;
        }

        var name = tr.name;
        for (int j = 0; j < candidateObjectNames.Length; j++)
        {
          var candidate = candidateObjectNames[j];
          if (string.IsNullOrWhiteSpace(candidate))
          {
            continue;
          }

          if (string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase))
          {
            if (match != null && !ReferenceEquals(match, tr.gameObject))
            {
              Debug.LogError($"[{nameof(TriageScenarioEventBootstrap)}] Checklist panel name '{candidate}' is ambiguous; automatic recovery was skipped.");
              return null;
            }
            match = tr.gameObject;
          }
        }
      }

      return match;
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

    private PatientController ResolvePatientAController()
    {
      ResolveRuntimeReferencesIfNeeded();
      return _patientAObject != null
        ? _patientAObject.GetComponentInChildren<PatientController>(true)
        : null;
    }

    /// <summary>
    /// 모니터/패널을 활성화하고 시나리오 초기 프로파일을 적용합니다.
    /// <paramref name="message"/>가 비어 있으면 채팅 시스템 메시지를 남기지 않습니다.
    /// </summary>
    private IEnumerator ApplyMonitorProfile(GameObject monitorObject,
      GameObject panelObject,
      PatientMonitorController monitorController,
      ECGParameters parameters,
      bool applyProfile,
      string message = null)
    {
      SetActiveIfPresent(monitorObject, true);
      SetActiveIfPresent(panelObject, true);

      if (monitorController == null)
      {
#if UNITY_EDITOR
        Debug.LogWarning(
          $"[TriageScenarioEventBootstrap] 활력징후 모니터 컨트롤러 참조가 없습니다. (monitor='{(monitorObject != null ? monitorObject.name : "<null>")}')",
          this);
        Debug.Log("[EmitSystemMessage] " + (string.IsNullOrWhiteSpace(message) ? null : message + " (모니터 컨트롤러 참조 없음)"));
#endif
        yield break;
      }

      if (applyProfile)
      {
        monitorController.SetCustomParameters(parameters);
      }

      monitorController.enabled = true;
#if UNITY_EDITOR
      Debug.Log("[EmitSystemMessage] " + (string.IsNullOrWhiteSpace(message) ? null : message));
#endif
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

      // 인트로 + patient A critical 핵심 흐름.
      yield return RunSmokeStep("triage_patient_a_patient_dummy_d_a", Event_TriagePatientAAndPatientDummyDA);
      yield return RunSmokeStep("show_patientA_ui", Event_ShowPatientAUi);
      yield return RunSmokeStep("show_patient_dummy_d_a_ui", Event_ShowPatientDummyDAUi);
      yield return RunSmokeStep("move_patientA_to_treatmentroom", Event_MovePatientAToTreatmentRoom);
      yield return RunSmokeStep("activate_vital_monitor_ui_patientA", Event_ActivateVitalMonitorUiPatientA);

      // 환자 B/C 진입 + 전이 흐름.
      yield return RunSmokeStep("triage_patient_b_patient_c_patient_dummy_d_b", Event_TriagePatientBPatientCPatientDummyDB);
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
