using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity.OxyLine;
using TriageTrainer.Scenario;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 벽면에 장착하는 산소 유량계(Oxyflowmeter) 표현입니다.
  ///
  /// <para>
  /// 처음에는 <b>보이지 않는 상태</b>로 시작합니다. 플레이어가 인벤토리의 산소 유량계
  /// (<see cref="TriageTrainer.ItemDefinitions.Oxyflowmeter"/>)를 <b>손에 든 채</b> 이 오브젝트 근처
  /// (부착된 트리거 Collider 범위 안)에 있으면 "설치(장착)" 상호작용이 힌트로 노출됩니다. 상호작용하면
  /// 이 오브젝트가 표시(Show)되고 인벤토리의 산소 유량계 1개가 소비됩니다.
  /// </para>
  ///
  /// <para>
  /// 상호작용으로 표시된 상태를 "설치했다 / 적용했다" 로 이해하며, 이 상태는 이후 데이터로 사용할 수
  /// 있도록 <see cref="IsAttached"/> 불리언 플래그로 공개합니다.
  /// </para>
  ///
  /// <para>
  /// 설치 상태의 전파 방식은 베이스의 <see cref="StaticObjectDisplayment.ShareMode"/> 로 설정합니다
  /// (기본값 <see cref="StaticObjectDisplaymentShareMode.ServerShared"/>). ServerShared 이면 상호작용이
  /// <see cref="PlayerController"/> 의 서버 권위 프로토콜로 위임되어, 확정 시 서버가 모든 클라이언트에
  /// 표시(설치)를 브로드캐스트하고(신규 접속자 포함) 요청자 클라이언트에서 산소 유량계가 소비됩니다
  /// (진실 원천: <see cref="StaticObjectDisplaymentService"/>). LocalOnly 이면 상호작용한 클라이언트에서만
  /// 소비/표시되고 전파되지 않습니다.
  /// </para>
  /// </summary>
  [DisallowMultipleComponent]
  public class WallAttachedOxyflowmeter : StaticObjectDisplayment, INearestOnlyInteract
  {
    public const string QuestPresentationInteractionIdentifier = "oxyflowmeter";
    public const string UnmarkedInteractionIdentifier = "oxyflowmeter_unmarked";

    /// <summary>
    /// 회수 상태의 상호작용 식별자. 설치/조작과 분리해 두면 퀘스트 표시 바인딩이
    /// "산소 유량계 회수"에는 붙지 않는다(회수는 퀘스트가 지시하는 행동이 아니다).
    /// </summary>
    public const string DetachInteractionIdentifier = "oxyflowmeter_detach";

    public static event Action<WallAttachedOxyflowmeter, bool> AttachmentStateChanged;
    /// <summary>플레이어 상호작용으로 새 설치가 확정된 경우에만 발생한다.</summary>
    public static event Action<WallAttachedOxyflowmeter> InstallationConfirmed;

    /// <summary>설치(장착) 상호작용으로 인정하는, 손에 든 아이템 식별자입니다.</summary>
    private const string RequiredItemIdentifier = TriageTrainer.ItemDefinitions.Oxyflowmeter.Identifier;

    [Header("WallAttachedOxyflowmeter")]
    [Tooltip("설치 시 인벤토리에서 소비할 산소 유량계 수량입니다.")]
    [SerializeField] private int _consumeCount = 1;

    [Tooltip("상호작용 힌트에 표시할 문구입니다.")]
    [SerializeField] private string _attachDisplayText = "산소 유량계 설치";

    [Tooltip("설치된 산소 유량계를 회수할 때 표시할 문구입니다.")]
    [SerializeField] private string _detachDisplayText = "산소 유량계 회수";

    [Tooltip("설치(적용) 완료 시 인게임 서버로 올릴 시나리오 신호입니다. 비우면 신호를 올리지 않습니다.")]
    [SerializeField] private string _attachCompletionSignal;

    [Tooltip("설치 상태에서 상호작용했을 때 올릴 시나리오 신호입니다. 이 신호가 아직 올라가 있지 않다면 " +
      "상호작용은 신호만 올리고 회수는 수행하지 않습니다(이미 올라가 있으면 기존처럼 회수). 비우면 설치 상태 상호작용은 항상 회수입니다.")]
    [SerializeField] private string _attachedInteractSignal;

    [Tooltip("_attachedInteractSignal 이 아직 올라가지 않은 설치 상태에서 상호작용 힌트에 표시할 문구입니다. " +
      "비우면 '산소 유량계 조작'을 표시합니다.")]
    [SerializeField] private string _attachedInteractDisplayText = "산소 유량계 조작";

    [Tooltip("자동 산소 라인 연결에 사용할 유량계 측 포트입니다.")]
    [SerializeField] private OxyLineConnectionPoint _oxyLineConnectionPoint;

    /// <summary>
    /// 이 산소 유량계가 벽면에 설치(적용)되었는지 여부입니다.
    /// 상호작용으로 표시된 상태를 "설치했다 / 적용했다" 로 이해하며, 이후 데이터로 사용할 수 있도록 공개합니다.
    /// 서버 권위 프로토콜에 의해 모든 클라이언트에서 동일하게 반영됩니다.
    /// </summary>
    [SerializeField] private bool _isAttached;
    public bool IsAttached => _isAttached;
    public override string PresentationEntityIdentifier => EntityIdentifier;
    public override string InteractionIdentifier
    {
      get
      {
        if (IsDetachInteraction)
          return DetachInteractionIdentifier;
        return IsQuestOxygenConnectionTarget()
          ? QuestPresentationInteractionIdentifier
          : UnmarkedInteractionIdentifier;
      }
    }

    /// <summary>
    /// 설치 상태에서 조작 신호까지 올라가, 다음 상호작용이 회수로 동작하는지 여부.
    /// </summary>
    public bool IsDetachInteraction => IsAttached && IsAttachedInteractCompleted;

    /// <summary>
    /// 이 유량계의 "조작" 상호작용이 이미 수행되었는지 여부.
    /// 조작 신호를 설정하지 않은 유량계는 설치 즉시 회수 대상이므로 완료로 본다.
    /// </summary>
    public bool IsAttachedInteractCompleted
    {
      get
      {
        if (string.IsNullOrWhiteSpace(_attachedInteractSignal))
          return true;
        return MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.IsRaised(
          ResolveAttachedInteractSignal());
      }
    }

    /// <summary>
    /// 이 유량계 전용 조작 신호. 설정값(<c>_attachedInteractSignal</c>)은 프리팹 공유라
    /// 그대로 쓰면 구역 하나를 조작한 순간 다른 구역의 유량계까지 회수 상태가 된다.
    /// 엔티티 식별자를 붙여 유량계별로 상태를 분리한다.
    /// </summary>
    public string ResolveAttachedInteractSignal()
    {
      if (string.IsNullOrWhiteSpace(_attachedInteractSignal))
        return null;

      // 힌트 갱신과 퀘스트 표시가 매 프레임 이 값을 읽는다. 엔티티 식별자는 레이아웃 적용
      // 시점에 정해지므로, 식별자가 바뀔 때만 다시 만든다.
      string identifier = EntityIdentifier;
      if (_cachedAttachedInteractSignal == null
          || !string.Equals(_cachedAttachedInteractSignalSource, identifier, StringComparison.Ordinal))
      {
        _cachedAttachedInteractSignalSource = identifier;
        _cachedAttachedInteractSignal = string.IsNullOrWhiteSpace(identifier)
          ? _attachedInteractSignal
          : $"{_attachedInteractSignal}_{identifier}";
      }

      return _cachedAttachedInteractSignal;
    }

    private string _cachedAttachedInteractSignal;
    private string _cachedAttachedInteractSignalSource;
    /// <summary>산소 라인 자동 연결에 사용할 유량계 측 포트. 프리팹에 설정되지 않으면 null이다.</summary>
    public OxyLineConnectionPoint OxyLineConnectionPoint => _oxyLineConnectionPoint;
    private Sprite _heldItemIcon;

    protected override string EntityIdPrefix => "wall_oxyflowmeter";
    // 미설치 활성화 후보가 구역 경계에서 여러 개 감지되어도 PlayerController가 같은 그룹 중
    // 플레이어와 가장 가까운 하나만 힌트에 남긴다. 설치 후 조작/회수 상호작용은 그대로 노출한다.
    public string NearestOnlyGroup => IsAttached ? null : RequiredItemIdentifier;
    public Transform NearestOnlyDistanceOrigin => transform;
    public Collider NearestOnlyCollider => GetComponent<Collider>();
    public int NearestOnlyTieBreaker => GetInstanceID();
    public override IReadOnlyList<Sprite> DisplayIcons => new[] { Icon.ClearRightBottom, _heldItemIcon };

    public override string DisplayText
    {
      get
      {
        string baseText = base.DisplayText;
        if (!string.IsNullOrWhiteSpace(baseText))
          return baseText;

        if (IsAttached)
        {
          // 설치 상태에서 첫 상호작용이 신호 발행(회수가 아님)으로 동작하는 동안은
          // 힌트도 그에 맞게 표시한다(실제 동작과 힌트의 불일치 방지).
          if (!IsDetachInteraction)
            return string.IsNullOrWhiteSpace(_attachedInteractDisplayText) ? "산소 유량계 조작" : _attachedInteractDisplayText;

          return string.IsNullOrWhiteSpace(_detachDisplayText) ? "산소 유량계 회수" : _detachDisplayText;
        }

        return string.IsNullOrWhiteSpace(_attachDisplayText) ? "산소 유량계 설치" : _attachDisplayText;
      }
    }

    /// <summary>
    /// 이 오브젝트는 규약상 항상 "미설치(숨김)" 상태로 시작하므로 베이스의 <c>_initiallyVisible</c> 설정을 무시한다.
    /// </summary>
    protected override void ApplyInitialVisibility()
    {
      SetAttached(false);
      Hide();
    }

    public override void Hide()
    {
      base.Hide();
      if (!gameObject.activeSelf)
        gameObject.SetActive(true);
    }

    // ── IInteractorConditional ───────────────────────────────────────────────

    /// <summary>
    /// 아직 설치되지 않았고, 플레이어가 산소 유량계를 손에 든 채 근처(콜라이더 범위)에 있을 때만 상호작용 가능합니다.
    /// </summary>
    public override bool CanInteract(Transform interactor)
    {
      var player = ResolvePlayer(interactor);
      if (player == null)
        return false;

      if (IsAttached)
      {
        _heldItemIcon = null;
        return true;
      }

      _heldItemIcon = player.HandlingItem?.CurrentItemIconTexture;
      return true;
    }

    // ── IInteract ─────────────────────────────────────────────────────────────

    public override void Interact(Transform interactor)
    {
      var player = ResolvePlayer(interactor);
      if (player == null || !CanInteract(interactor))
      {
        if (player == null)
          Debug.LogWarning("[WallAttachedOxyflowmeter] interactor 에서 PlayerController 를 찾지 못했습니다.", this);
        return;
      }

      if (IsAttached)
      {
        // 설치 상태에서의 첫 상호작용은 시나리오 신호만 올린다(예: 유량계 클릭으로 산소량 결정 단계).
        // 신호가 이미 올라간 뒤의 상호작용은 기존처럼 회수로 처리한다.
        if (!IsDetachInteraction)
        {
          // 유량계별 신호와 별개로, 기존 시나리오(환자 A 계열)가 대기하는 공용 신호도 유지한다.
          MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(_attachedInteractSignal);
          MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(ResolveAttachedInteractSignal());
          // 산소 라인은 신호가 모든 피어에 미러링된 뒤 CareZone이 만든다. 조작한 피어에서는
          // 힌트/라인이 한 프레임이라도 늦지 않도록 여기서도 한 번 시도한다.
          RequestOxygenLineReconcile();
          player.RefreshInteractableHintsNow();
          return;
        }

        player.TryClearStaticObjectDisplaymentAndGrantItem(EntityIdentifier, RequiredItemIdentifier);
        return;
      }

      if (!IsHandlingOxyflowmeter(player))
      {
        ShowMissingOxyflowmeterDialogue();
        return;
      }

      // 표시(설치) 요청을 베이스에 위임한다. ShareMode 에 따라 서버 전파(ServerShared) 또는 로컬 전용(LocalOnly)으로 처리된다.
      // - ServerShared: 서버 승인 → 요청자 인벤토리에서 산소 유량계 소비 → 확정 시 전체 브로드캐스트.
      // - LocalOnly: 이 클라이언트에서만 소비하고 즉시 표시.
      RequestApplyShown(player, RequiredItemIdentifier, Mathf.Max(1, _consumeCount));
    }

    private static void ShowMissingOxyflowmeterDialogue()
    {
      var dialogue = MultiplayerInfrastructure.Registry.Registry.Get<DialoguePanelUIController>(
        MultiplayerInfrastructure.Registry.RegistryType.UI,
        MultiplayerInfrastructure.Registry.Registry.TypeKey<DialoguePanelUIController>());
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", "(완성된 산소 유량계를 갖고 있지 않다.)");
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", "(습윤병, 1L 멸균증류수, 유량계를 찾아 조립하자.)");
    }

    // ── 표시 적용 (서버 권위 RPC 에서 호출) ────────────────────────────────────

    /// <summary>
    /// 이 오브젝트가 표시(설치/적용)될 때 로컬 표현에 반영한다(모든 클라이언트에서 실행).
    /// 표시와 함께 <see cref="IsAttached"/> 를 확정한다. 완료 신호는 "최초 확정 시 1회"만 필요하므로
    /// 여기가 아니라 <see cref="OnShownConfirmed"/> 에서 처리한다(옵저버 중복 실행 방지).
    /// </summary>
    public override void ApplyShownFromNetwork()
    {
      Show();
      // CareZone listens to the attachment event and immediately scans active
      // colliders. Make the representation/collider visible before publishing it.
      SetAttached(true);
    }

    /// <summary>
    /// 표시(설치)가 권위 경로에서 최초로 확정될 때 1회 호출된다(ServerShared: 서버, LocalOnly: 로컬).
    /// 설치 완료 시나리오 신호를 여기서 올린다.
    /// </summary>
    public override void OnShownConfirmed()
    {
      SetAttached(true);
      TriageWorldInteractionSignals.RaiseOxyflowmeterInstalled(EntityIdentifier);
      TriageWorldInteractionSignals.RaiseOxyflowmeterEnabled(EntityIdentifier);
      InstallationConfirmed?.Invoke(this);
      RaiseCompletionSignalIfAny();
    }

    public override void OnHiddenConfirmed()
    {
      SetAttached(false);
      ClearAttachedInteractSignal();
      TriageWorldInteractionSignals.RaiseOxyflowmeterRemoved(EntityIdentifier);
      TriageWorldInteractionSignals.RaiseOxyflowmeterDisabled(EntityIdentifier);
    }

    /// <summary>
    /// 회수한 유량계의 조작 이력을 지운다. 이 신호가 남아 있으면 다시 설치했을 때
    /// 첫 상호작용이 "조작"이 아니라 "회수"로 잡혀, 산소 공급 단계를 더 이상 진행할 수 없다.
    /// 유량계별 신호만 지운다. 공용 신호(<c>interact_oxyflow_wall</c>)는 환자 A 계열 시나리오의
    /// 게이트가 참조하므로 건드리지 않는다.
    /// </summary>
    private void ClearAttachedInteractSignal()
    {
      string signal = ResolveAttachedInteractSignal();
      if (!string.IsNullOrWhiteSpace(signal))
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Clear(signal);
    }

    public override void ApplyHiddenFromNetwork()
    {
      SetAttached(false);
      base.ApplyHiddenFromNetwork();
    }

    /// <summary>설치 상태를 해제하고 다시 숨긴다(관리자 리셋/시나리오 되돌림 등에서 사용하는 로컬 표현 API).</summary>
    public void Detach()
    {
      if (!IsAttached)
        return;
      Hide();
      OnHiddenConfirmed();
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 이 유량계가 속한 CareZone에 산소 라인 연결을 다시 판정하게 한다.
    /// 유량계는 로컬 정적 오브젝트지만 서버가 공통 토폴로지 변경을 확정하고,
    /// 각 클라이언트는 그 결과를 로컬 렌더링으로 반영한다.
    /// </summary>
    private void RequestOxygenLineReconcile()
    {
      var zones = FindObjectsByType<PatientCareDescriptionZone>(
        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < zones.Length; i++)
        zones[i].TryReconcileOxygenLineFor(this);
    }

    /// <summary>
    /// 산소 공급 처치 목표를 받는 환자들. 이 환자를 담당하는 유량계에만 퀘스트 마크를 붙인다.
    /// </summary>
    private static readonly string[] OxygenTreatmentPatientIdentifiers = { "patient_b", "patient_c" };

    private bool IsQuestOxygenConnectionTarget()
    {
      var zones = FindObjectsByType<PatientCareDescriptionZone>(
        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < zones.Length; i++)
      {
        var patient = zones[i].GetPatientForOxyflowmeter(this);
        string patientIdentifier = patient != null ? patient.Identifier : null;
        if (!IsOxygenTreatmentPatient(patientIdentifier))
          continue;
        return !MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.IsRaised(
          $"equipment_connected_oxyflowmeter_{patientIdentifier}");
      }
      return false;
    }

    private static bool IsOxygenTreatmentPatient(string patientIdentifier)
    {
      if (string.IsNullOrWhiteSpace(patientIdentifier))
        return false;

      for (int i = 0; i < OxygenTreatmentPatientIdentifiers.Length; i++)
      {
        if (string.Equals(patientIdentifier, OxygenTreatmentPatientIdentifiers[i], StringComparison.Ordinal))
          return true;
      }

      return false;
    }

    /// <summary>플레이어 인벤토리에 산소 유량계가 있는지 판정한다.</summary>
    private static bool IsHandlingOxyflowmeter(PlayerController player)
    {
      return player != null && player.CountItemInInventory(RequiredItemIdentifier) > 0;
    }

    private void SetAttached(bool attached)
    {
      if (IsAttached == attached)
        return;
      _isAttached = attached;
      GameLogService.WriteInteraction(
        $"Oxygen flowmeter attachment state: entity={EntityIdentifier}, attached={attached}",
        EntityIdentifier);
      AttachmentStateChanged?.Invoke(this, attached);
    }

    private void RaiseCompletionSignalIfAny()
    {
      if (string.IsNullOrWhiteSpace(_attachCompletionSignal))
        return;

      MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(_attachCompletionSignal);
    }
  }
}
