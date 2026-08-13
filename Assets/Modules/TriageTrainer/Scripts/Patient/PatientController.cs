using System;
using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity.IntravenousLine;
using TriageTrainer.Entity.OxyLine;
using TriageTrainer.Entity.SuctionLine;
using TriageTrainer.Entity.Patient;
using TriageTrainer.Patient;
using UnityEngine;

using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  [RequireComponent(typeof(CapsuleCollider))]
  public partial class PatientController : NetworkBehaviour, IInteractable, IReposable, IItemUseTarget, IScenarioEntityInitTarget, IScenarioTriageAssessTarget, IScenarioIdentifiedEntity
  {
    private const string DefaultPatientCarryAttachPointName = "PatientCarryAttachPoint";

    [Header("Identity")]
    [SerializeField] private string _identifier = "patient";

    [Header("Display")]
    [SerializeField] private string _liftDisplayText = "환자를 들어올리기";
    [SerializeField] private string _carryDisplayText = "환자 들어올리기";
    [SerializeField] private string _monitorSelectDisplayText = "이 환자를 모니터링";
    [Tooltip("프리팹에서는 배치되어 있지만 시나리오 이벤트 전에는 숨겨야 하는 자식 오브젝트 이름입니다.")]
    [SerializeField] private string[] _initiallyHiddenChildNames = Array.Empty<string>();

    [Header("Patient")]
    [SerializeField] private int _weight = 4;

    [Header("Runtime")]
    [SerializeField] private PatientSupportExternalRefs _supportExternalRefs;
    [SerializeField] private IntravenousLineConnectionPoint _ivAttachmentPoint;
    [Tooltip("설치된 산소 마스크에 포함된 환자 측 산소 라인 포트입니다.")]
    [SerializeField] private OxyLineConnectionPoint _oxygenMaskAttachmentPoint;
    [Tooltip("환자 측 석션 라인 포트입니다.")]
    [SerializeField] private SuctionLineConnectionPoint _suctionLineAttachmentPoint;
    [SerializeField] private Transform _carryAttachPoint;
    [SerializeField] private bool _isMovingPatientBedAttached;
    [SerializeField] private bool _isPlayerAttached;

    private readonly Dictionary<string, float> _lastNoticeByInteractor = new(StringComparer.Ordinal);

    private ChatUIController _chatUI;

    public string Identifier => EffectiveIdentifier;

    /// <summary>
    /// <see cref="IScenarioIdentifiedEntity"/> 구현: 시나리오 식별자를 노출한다(레지스트리 등록 식별자와 동일).
    /// 트리거 존 등이 진입 엔티티의 식별자를 도메인 비의존적으로 조회하는 데 사용한다.
    /// </summary>
    public string ScenarioEntityIdentifier => EffectiveIdentifier;

    public int Weight => Mathf.Max(0, _weight);

    public bool IsReposed => _supportExternalRefs.PatientBed != null;
    public MovingPatientBedController CurrentBed => _supportExternalRefs.PatientBed;
    public PatientSupportExternalRefs SupportExternalRefs => _supportExternalRefs;
    /// <summary>여러 수액 줄 연결을 허용하는 환자 IV attachment point.</summary>
    public IntravenousLineConnectionPoint IvAttachmentPoint
    {
      get
      {
        // EditMode 생성, 비활성 프리팹 인스턴스 및 초기화 순서에 따라 Awake가 아직
        // 실행되지 않은 상태에서도 연결 서비스가 이 지점을 조회할 수 있다.
        // null을 그대로 노출하면 정상적인 IV 연결 완료 신호가 조용히 유실된다.
        EnsureIvAttachmentPoint();
        return _ivAttachmentPoint;
      }
    }
    /// <summary>설치된 산소 마스크의 환자 측 산소 라인 포트. 설정되지 않으면 null이다.</summary>
    public OxyLineConnectionPoint OxygenMaskAttachmentPoint => _oxygenMaskAttachmentPoint != null && _oxygenMaskAttachmentPoint.isActiveAndEnabled
      ? _oxygenMaskAttachmentPoint : null;
    public OxyLineConnectionPoint ConfiguredOxygenMaskAttachmentPoint => _oxygenMaskAttachmentPoint;
    public void SetOxygenMaskAttachmentPointFromPatientComponent(OxyLineConnectionPoint point)
    {
      _oxygenMaskAttachmentPoint = point;
    }
    /// <summary>환자 측 석션 라인 포트. 설정되지 않거나 비활성이면 null이다.</summary>
    public SuctionLineConnectionPoint SuctionLineAttachmentPoint => _suctionLineAttachmentPoint != null && _suctionLineAttachmentPoint.isActiveAndEnabled
      ? _suctionLineAttachmentPoint : null;
    public SuctionLineConnectionPoint ConfiguredSuctionLineAttachmentPoint => _suctionLineAttachmentPoint;
    public Transform CarryAttachPoint => _carryAttachPoint != null ? _carryAttachPoint : transform;
    public bool IsMovingPatientBedAttached => _isMovingPatientBedAttached;
    public bool IsPlayerAttached => _isPlayerAttached;

    /// <summary>
    /// 환자가 플레이어에게 들린 상태가 아닐 때만 수행 가능한 정지 상태 사정/분류의 공통 게이트.
    /// </summary>
    public bool CanPerformTriageOrAssessment => !_isPlayerAttached;

    private void Awake()
    {
      Awake_Animation();
      EnsureCarryAttachPoint();
      EnsureIvAttachmentPoint();
      InitializeCollider();
      EnsureMedicalStateDefaults();
      InitializeTreatmentDisplaysFromConfiguredState();
      SetNamedChildrenActive(_initiallyHiddenChildNames, false);
      _weight = Mathf.Max(0, _weight);
      BuildInteractEntries();
      GetPatientState()?.InitializeRuntimeReferences(this);
    }

    public bool SetNamedChildActive(string childName, bool active)
    {
      if (string.IsNullOrWhiteSpace(childName))
        return false;

      var children = GetComponentsInChildren<Transform>(true);
      for (int i = 0; i < children.Length; i++)
      {
        var child = children[i];
        if (child == null || !string.Equals(child.name, childName, StringComparison.Ordinal))
          continue;

        child.gameObject.SetActive(active);
        return true;
      }

      return false;
    }

    private void SetNamedChildrenActive(string[] childNames, bool active)
    {
      if (childNames == null)
        return;

      for (int i = 0; i < childNames.Length; i++)
        SetNamedChildActive(childNames[i], active);
    }

    public void OnAttacked(MI.Entity.Entity attacker, int damageAmount)
    {
      TryAttachCurrentHandlingItem(attacker);
    }

    /// <summary>
    /// 아이템 사용 대상으로서의 처리(<see cref="IItemUseTarget"/>): 코드 하드코딩 매핑(<see cref="ApplyItemUse"/>)에 따라
    /// 처치 시각 표현을 켜고 시나리오 게이팅 신호를 올린다.
    /// </summary>
    public bool OnItemUsed(MI.Entity.Entity user, string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return false;

      return ApplyAndConsumeItemUse(user, itemIdentifier);
    }

    public bool TryAttachCurrentHandlingItem(MI.Entity.Entity actorEntity)
    {
      if (actorEntity == null)
        return false;

      var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      foreach (var each in players)
      {
        if (each == null || !ReferenceEquals(each.PlayerEntity, actorEntity))
          continue;

        string itemIdentifier = each.HandlingItem?.CurrentIdentifier;
        if (string.IsNullOrWhiteSpace(itemIdentifier))
          return false;

        return ApplyAndConsumeItemUse(actorEntity, itemIdentifier);
      }

      return false;
    }

    private bool ApplyAndConsumeItemUse(MI.Entity.Entity user, string itemIdentifier)
    {
      PlayerController sourcePlayer = null;
      var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      foreach (var player in players)
      {
        if (player != null && ReferenceEquals(player.PlayerEntity, user))
        {
          sourcePlayer = player;
          break;
        }
      }

      // Debug/tests can apply without a player. Real player use must own the item.
      if (sourcePlayer != null && sourcePlayer.CountItemInInventory(itemIdentifier) < 1)
        return false;

      if (IsPatientBC && IsFishNetClientInitialized && !IsFishNetServerStarted)
        return ApplyItemUse(itemIdentifier);

      if (!ApplyItemUse(itemIdentifier))
        return false;

      return sourcePlayer == null || sourcePlayer.RemoveItemFromInventory(itemIdentifier, 1) == 1;
    }

    public void SetCurrentBed(MovingPatientBedController bed)
    {
      var previous = _supportExternalRefs.PatientBed;
      _supportExternalRefs.PatientBed = bed;
      OnMovingPatientBedAttachedStateChanged(_supportExternalRefs.PatientBed != null || _isMovingPatientBedAttached);
      Update_Animation();
      NotifyBedConnectionChanged(previous, bed);
    }

    public void OnMovingPatientBedAttachedEnter()
    {
      _isMovingPatientBedAttached = true;
      OnMovingPatientBedAttachedStateChanged(true);
      Update_Animation();
    }

    public void OnMovingPatientBedAttachedExit()
    {
      _isMovingPatientBedAttached = false;
      OnMovingPatientBedAttachedStateChanged(CurrentBed != null || _isMovingPatientBedAttached);
      Update_Animation();
    }

    public void OnPlayerAttachedEnter()
    {
      _isPlayerAttached = true;
    }

    public void OnPlayerAttachedExit()
    {
      _isPlayerAttached = false;
    }

    private void ShowThrottledMessage(Transform interactor, string message)
    {
      if (interactor == null || string.IsNullOrWhiteSpace(message))
        return;

      string key = interactor.GetInstanceID() + ":" + message;
      if (_lastNoticeByInteractor.TryGetValue(key, out float lastTime))
      {
        if (Time.time - lastTime < 3f)
          return;
      }

      _lastNoticeByInteractor[key] = Time.time;

      if (_chatUI == null)
        _chatUI = Registry.Get<ChatUIController>(RegistryType.UI, Registry.TypeKey<ChatUIController>());

      if (_chatUI != null)
        _chatUI.AppendMessage($"<color=#FFD700>[System]</color> {message}", true);
      else
        Debug.Log($"[Patient] {message}", this);
    }

    private void EnsureCarryAttachPoint()
    {
      if (_carryAttachPoint != null)
        return;

      var existing = transform.Find(DefaultPatientCarryAttachPointName);
      if (existing != null)
      {
        _carryAttachPoint = existing;
        return;
      }

      var go = new GameObject(DefaultPatientCarryAttachPointName);
      _carryAttachPoint = go.transform;
      _carryAttachPoint.SetParent(transform, false);
      _carryAttachPoint.localPosition = new Vector3(0f, 1.0f, 0.2f);
      _carryAttachPoint.localRotation = Quaternion.identity;
    }

    private void EnsureIvAttachmentPoint()
    {
      if (_ivAttachmentPoint == null)
        _ivAttachmentPoint = GetComponentInChildren<IntravenousLineConnectionPoint>(true);

      if (_ivAttachmentPoint == null)
      {
        var pointObject = new GameObject("IVAttachmentPoint");
        pointObject.transform.SetParent(transform, false);
        pointObject.transform.localPosition = new Vector3(0f, 1.1f, 0.35f);
        pointObject.transform.localRotation = Quaternion.identity;
        _ivAttachmentPoint = pointObject.AddComponent<IntravenousLineConnectionPoint>();
      }

      _ivAttachmentPoint.SetAllowsMultipleConnections(true);
    }

    protected override void OnValidate()
    {
      base.OnValidate();
      OnValidate_Animation();
      EnsureCarryAttachPoint();
      InitializeCollider();
      EnsureMedicalStateDefaults();
      _weight = Mathf.Max(0, _weight);
      EnsureDefaultInteractConfigs();
      var state = GetPatientState();
      RestoreLegacyDefaultsAfterInspectorResetIfNeeded(state);
      state?.InitializeRuntimeReferences(this);
    }

    protected override void Reset()
    {
      base.Reset();
      ApplySerializedDefaultsForInspectorReset();
      GetPatientState()?.InitializeRuntimeReferences(this);
    }

    private void RestoreLegacyDefaultsAfterInspectorResetIfNeeded(PatientStateABC state)
    {
      if (state == null || !state.RestoresLegacyPatientControllerDefaultsOnInspectorReset)
        return;

      // Inspector 문맥 메뉴 Reset은 Unity 기본 직렬화값을 적용한 뒤 OnValidate를 호출한다.
      // B Male/Female의 이전 프리팹 값과 구별되는 이 조합일 때만 코드 리터럴을 복구한다.
      bool hasResetSignature = (_assessActions == null || _assessActions.Count == 0)
                               && !_intravenousLineCannulaConfig.Supported;
      if (!hasResetSignature)
        return;

      _supportExternalRefs.InitializeEmptyCollections();
      ApplySerializedDefaultAssessActions();
      _intravenousLineCannulaConfig.Supported = true;
      EnsureDefaultRuntimeAnimatorController();
      BuildInteractEntries();
    }

    /// <summary>
    /// PatientController가 직접 소유한 모든 Inspector 직렬화 필드의 코드 기본값이다.
    /// 환자 유형별 값은 이 메서드 뒤 PatientStateABC가 다시 적용한다.
    /// </summary>
    private void ApplySerializedDefaultsForInspectorReset()
    {
      _identifier = "patient";
      _liftDisplayText = "환자를 들어올리기";
      _carryDisplayText = "환자 들어올리기";
      _monitorSelectDisplayText = "이 환자를 모니터링";
      _initiallyHiddenChildNames = Array.Empty<string>();
      _weight = 4;
      _supportExternalRefs = default;
      _supportExternalRefs.InitializeEmptyCollections();
      _ivAttachmentPoint = null;
      _oxygenMaskAttachmentPoint = null;
      _suctionLineAttachmentPoint = null;
      _carryAttachPoint = null;
      _isMovingPatientBedAttached = false;
      _isPlayerAttached = false;

      _runtimeAnimatorController = null;
      _capsuleCollider = null;
      _standingCapsuleCenter = new Vector3(0f, 0.9f, 0f);
      _standingCapsuleHeight = 1.8f;
      _standingCapsuleRadius = 0.3f;
      _standingCapsuleDirection = 1;
      _lyingCapsuleCenter = new Vector3(0f, 0.45f, 0f);
      _lyingCapsuleHeight = 1.8f;
      _lyingCapsuleRadius = 0.3f;
      _lyingCapsuleDirection = 2;

      _patientDescriptor = new PatientDescriptor();
      _medicalState = new PatientMedicalState();
      _interactConfigs = new List<InteractConfig>();
      ApplySerializedDefaultAssessActions();
      _intravenousLineCannulaConfig = default;
      _intravenousLineCannulaConfig.Supported = true;
      _intravenousLineCannulaInteractable = true;
      _triageConfig = TriageAssessmentConfig.Default();
      _triageLabelAnchor = null;
      _debugTreatmentDisplay = TreatmentDisplay.GauzePatchedOnThorax;
      _debugItemIdentifier = "gauze";

      EnsureCarryAttachPoint();
      EnsureDefaultRuntimeAnimatorController();
      InitializeCollider();
      EnsureMedicalStateDefaults();
      EnsureDefaultInteractConfigs();
      BuildInteractEntries();
    }
  }
}
