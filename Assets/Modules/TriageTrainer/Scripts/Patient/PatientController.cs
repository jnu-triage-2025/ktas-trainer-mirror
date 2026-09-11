using System;
using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity.IntravenousLine;
using TriageTrainer.Entity.CentralLine;
using TriageTrainer.Entity.OxyLine;
using TriageTrainer.Entity.Patient;
using TriageTrainer.Entity.SuctionLine;
using TriageTrainer.ItemDefinitions;
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

    [Tooltip("프리팹에서는 배치되어 있지만 시나리오 이벤트 전에는 숨겨야 하는 자식 오브젝트 이름입니다.")]
    [SerializeField] private string[] _initiallyHiddenChildNames = Array.Empty<string>();

    [Header("Patient")]
    [SerializeField] private int _weight = 4;

    [Header("Runtime")]
    [SerializeField] private PatientSupportExternalRefs _supportExternalRefs;
    [SerializeField] private IntravenousLineConnectionPoint _ivAttachmentPoint;
    [Tooltip("환자 B/C 정맥로(캐뉼라 삽입 부위) 측 IV 연결 지점입니다. 환자 유형별 State 컴포넌트" +
             "(PatientTypeBMaleState 등)의 참조 필드에서 주입되며, 식별자 문자열로 검색하지 않습니다.")]
    [SerializeField] private IntravenousLineConnectionPoint _patientBCIvAttachmentPoint;
    [Tooltip("C-line(중심정맥관) 환자 측 전용 연결 지점입니다. Patient A의 Cline_A 자식 연결점을 직접 참조합니다.")]
    [SerializeField] private CentralLineConnectionPoint _centralLineAttachmentPoint;
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
    /// <summary>C-line(중심정맥관) 환자 측 전용 연결 지점. 단일 연결만 허용한다.</summary>
    public CentralLineConnectionPoint CentralLineAttachmentPoint
    {
      get
      {
        ConfigureCentralLineAttachmentPoint();
        return _centralLineAttachmentPoint;
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
    // The carry flag is local to the player that picked the patient up and
    // can arrive a frame later than a replicated bed link on remote peers.
    // A patient already resting on a bed is stationary and must remain
    // assessable; only an actively carried patient is gated.
    public bool CanPerformTriageOrAssessment => !_isPlayerAttached || CurrentBed != null;

    private void Awake()
    {
      Awake_Animation();
      EnsureCarryAttachPoint();
      EnsureIvAttachmentPoint();
      ConfigureCentralLineAttachmentPoint();
      InitializeCollider();
      EnsureMedicalStateDefaults();
      InitializeTreatmentDisplaysFromConfiguredState();
      SetNamedChildrenActive(_initiallyHiddenChildNames, false);
      _weight = Mathf.Max(0, _weight);
      BuildInteractEntries();
      GetPatientState()?.InitializeRuntimeReferences(this);
    }

    /// <summary>
    /// 이름으로 찾은 자식 오브젝트의 활성 상태를 바꾼다(예: 제세동 패드 표시).
    /// 네트워크 전파는 <see cref="PatientController"/> 의 NamedChildDisplay 파셜이 담당한다.
    /// </summary>
    public bool SetNamedChildActive(string childName, bool active)
    {
      bool applied = ApplyNamedChildActiveLocal(childName, active);
      if (applied)
        PublishNamedChildActive(childName, active);

      return applied;
    }

    private bool ApplyNamedChildActiveLocal(string childName, bool active)
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
      if (string.Equals(itemIdentifier, Gauze.Identifier, StringComparison.Ordinal))
        return ApplyAndConsumeBleedingControlItemUse(user, itemIdentifier);
      if (string.Equals(itemIdentifier, Plaster.Identifier, StringComparison.Ordinal))
      {
        if (string.Equals(ResolveTreatmentIdentifierForItem(itemIdentifier), TreatmentPlasterOnIntubation, StringComparison.Ordinal))
          return ApplyAndConsumeIntubationFixationItemUse(user, itemIdentifier);
        return ApplyAndConsumeBleedingControlItemUse(user, itemIdentifier);
      }
      return ApplyAndConsumeOtherItemUse(user, itemIdentifier);
    }

    private bool ApplyAndConsumeBleedingControlItemUse(MI.Entity.Entity user, string itemIdentifier)
    {
      if (!string.Equals(itemIdentifier, Gauze.Identifier, StringComparison.Ordinal)
          && !string.Equals(ResolveTreatmentIdentifierForItem(itemIdentifier), TreatmentPlasterOnGauze, StringComparison.Ordinal))
        return false;

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

      // 디버그/테스트는 플레이어 없이 적용할 수 있다. 실제 플레이어 사용은 아이템을 소유해야만 한다.
      if (sourcePlayer != null && sourcePlayer.CountItemInInventory(itemIdentifier) < 1)
        return false;

      if (sourcePlayer != null && !HasEquippedGloves(sourcePlayer))
      {
        PresentBleedingControlRequirementsDialogue();
        return false;
      }

      if (IsFishNetClientInitialized && !IsFishNetServerStarted)
      {
        CmdApplyPatientItemUse(itemIdentifier);
        return true;
      }

      if (HasPendingApprovedItemUse(itemIdentifier) || !CanApplyItemUse(itemIdentifier))
        return false;

      // 실제 플레이어 경로는 소비를 먼저 확정한 뒤에만 상태·Display·신호를 변경한다.
      PlayerController.ItemUseConsumptionReceipt receipt = null;
      if (sourcePlayer != null && !sourcePlayer.TryConsumeItemUse(itemIdentifier, out receipt))
        return false;

      bool applied = ApplyItemUse(itemIdentifier);
      sourcePlayer?.CompleteConsumedItemUse(receipt, applied);
      return applied;
    }

    private bool ApplyAndConsumeIntubationFixationItemUse(MI.Entity.Entity user, string itemIdentifier)
    {
      if (!string.Equals(ResolveTreatmentIdentifierForItem(itemIdentifier), TreatmentPlasterOnIntubation, StringComparison.Ordinal))
        return false;

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

      // 디버그/테스트는 플레이어 없이 적용할 수 있다. 실제 플레이어 사용은 아이템을 소유해야만 한다.
      if (sourcePlayer != null && sourcePlayer.CountItemInInventory(itemIdentifier) < 1)
        return false;

      if (IsFishNetClientInitialized && !IsFishNetServerStarted)
      {
        CmdApplyPatientItemUse(itemIdentifier);
        return true;
      }

      if (HasPendingApprovedItemUse(itemIdentifier) || !CanApplyItemUse(itemIdentifier))
        return false;

      // 실제 플레이어 경로는 소비를 먼저 확정한 뒤에만 상태·Display·신호를 변경한다.
      PlayerController.ItemUseConsumptionReceipt receipt = null;
      if (sourcePlayer != null && !sourcePlayer.TryConsumeItemUse(itemIdentifier, out receipt))
        return false;

      bool applied = ApplyItemUse(itemIdentifier);
      sourcePlayer?.CompleteConsumedItemUse(receipt, applied);
      return applied;
    }

    private bool ApplyAndConsumeOtherItemUse(MI.Entity.Entity user, string itemIdentifier)
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

      // 디버그/테스트는 플레이어 없이 적용할 수 있다. 실제 플레이어 사용은 아이템을 소유해야만 한다.
      if (sourcePlayer != null && sourcePlayer.CountItemInInventory(itemIdentifier) < 1)
        return false;

      if (IsFishNetClientInitialized && !IsFishNetServerStarted)
      {
        CmdApplyPatientItemUse(itemIdentifier);
        return true;
      }

      if (HasPendingApprovedItemUse(itemIdentifier) || !CanApplyItemUse(itemIdentifier))
        return false;

      // 실제 플레이어 경로는 소비를 먼저 확정한 뒤에만 상태·Display·신호를 변경한다.
      PlayerController.ItemUseConsumptionReceipt receipt = null;
      if (sourcePlayer != null && !sourcePlayer.TryConsumeItemUse(itemIdentifier, out receipt))
        return false;

      bool applied = ApplyItemUse(itemIdentifier);
      sourcePlayer?.CompleteConsumedItemUse(receipt, applied);
      return applied;
    }

    private static bool HasEquippedGloves(PlayerController player)
    {
      if (player == null)
        return false;

      var equipmentSlots = player.EquipmentSlots;
      for (int i = 0; i < equipmentSlots.Count; i++)
      {
        var slot = equipmentSlots[i];
        if (slot != null && slot.SlotType == EquipmentSlotType.Glove && !slot.IsEmpty)
          return true;
      }

      return false;
    }

    private static void PresentBleedingControlRequirementsDialogue()
    {
      var dialogue = Registry.Get<DialoguePanelUIController>(
        RegistryType.UI, Registry.TypeKey<DialoguePanelUIController>());
      if (dialogue == null)
        dialogue = FindFirstObjectByType<DialoguePanelUIController>(FindObjectsInactive.Exclude);

      dialogue?.TryPresentTransientDialogue(
        DialoguePanelUIController.PlayerNamePlaceholder,
        "(지혈하려면 장갑을 착용하고 거즈와 플라스터를 준비해야 한다.)");
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

    /// <summary>
    /// 환자 B/C 정맥로(캐뉼라 삽입 부위) 측 IV 연결 지점.
    ///
    /// <para>
    /// 환자 유형마다 정맥로 위치와 모델 구성이 다르므로, 이 지점은 환자 유형별 State 컴포넌트
    /// (<c>PatientTypeBMaleState</c> 등)가 프리팹에서 직접 참조해 주입한다. 식별자 문자열로
    /// 자식 포인트를 검색하거나 런타임에 포인트를 생성하지 않는다. 동적으로 추가한
    /// NetworkBehaviour 는 FishNet 스폰 대상이 아니어서 온라인에서는 자동 연결이 불가능하므로,
    /// 네트워크 프리팹에 배치된 포인트를 참조하는 것이 유일하게 유효한 배선 방법이다.
    /// </para>
    /// </summary>
    public IntravenousLineConnectionPoint PatientBCIvAttachmentPoint
    {
      get
      {
        if (!IsPatientBC)
          return IvAttachmentPoint;

        // Unity 의 가짜 null(파괴된 오브젝트)을 실제 null 로 정규화한다.
        // 연결 판정이 ReferenceEquals 로 양 끝점을 비교하기 때문이다.
        return _patientBCIvAttachmentPoint != null ? _patientBCIvAttachmentPoint : null;
      }
    }

    /// <summary>Inspector 에 직렬화된 B/C 정맥로 IV 연결 지점 참조(주입 검증용).</summary>
    public IntravenousLineConnectionPoint ConfiguredPatientBCIvAttachmentPoint => _patientBCIvAttachmentPoint;

    /// <summary>
    /// 환자 유형별 State 컴포넌트가 프리팹에서 참조한 B/C 정맥로 IV 연결 지점을 주입한다.
    /// (<see cref="SetOxygenMaskAttachmentPointFromPatientComponent"/> 과 동일한 관례.)
    /// </summary>
    public void SetPatientBCIvAttachmentPointFromPatientComponent(IntravenousLineConnectionPoint point)
    {
      _patientBCIvAttachmentPoint = point;
    }

    private void EnsureIvAttachmentPoint()
    {
      if (_ivAttachmentPoint == null)
      {
        var candidates = GetComponentsInChildren<IntravenousLineConnectionPoint>(true);
        for (int i = 0; i < candidates.Length; i++)
        {
          if (candidates[i] == null
              || string.Equals(candidates[i].Identifier, "cline_iv_connection_point",
                   StringComparison.Ordinal))
            continue;

          _ivAttachmentPoint = candidates[i];
          break;
        }
      }

      // C-line 연결점을 일반 IV 연결점으로 잘못 선택하지 않도록 제외한다.
      if (_ivAttachmentPoint != null
          && string.Equals(_ivAttachmentPoint.Identifier, "cline_iv_connection_point",
               StringComparison.Ordinal))
        _ivAttachmentPoint = null;

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

    private void ConfigureCentralLineAttachmentPoint()
    {
      if (_centralLineAttachmentPoint == null)
        return;

      _centralLineAttachmentPoint.SetAllowsMultipleConnections(false);
    }

    protected override void OnValidate()
    {
      base.OnValidate();
      OnValidate_Animation();
      EnsureCarryAttachPoint();
      InitializeCollider();
      EnsureMedicalStateDefaults();
      _weight = Mathf.Max(0, _weight);
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
      bool hasResetSignature = !_intravenousLineCannulaConfig.Supported;
      if (!hasResetSignature)
        return;

      _supportExternalRefs.InitializeEmptyCollections();
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
      _initiallyHiddenChildNames = Array.Empty<string>();
      _weight = 4;
      _supportExternalRefs = default;
      _supportExternalRefs.InitializeEmptyCollections();
      _ivAttachmentPoint = null;
      _patientBCIvAttachmentPoint = null;
      _oxygenMaskAttachmentPoint = null;
      _suctionLineAttachmentPoint = null;
      _carryAttachPoint = null;
      _isMovingPatientBedAttached = false;
      _isPlayerAttached = false;

      _runtimeAnimatorController = null;
      _animatorObject = GetComponentInChildren<PatientAnimatorRootObject>(true);
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
      BuildInteractEntries();
    }
  }
}
