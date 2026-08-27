using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity.SuctionLine;
using TriageTrainer.Scenario;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 벽면에 장착하는 흡인기(WallSuction) 표현입니다.
  ///
  /// <para>
  /// 처음에는 <b>보이지 않는 상태</b>로 시작합니다. 플레이어가 인벤토리의 흡인기
  /// (<see cref="TriageTrainer.ItemDefinitions.WallSuction"/>)을 <b>손에 든 채</b> 이 오브젝트 근처
  /// (부착된 트리거 Collider 범위 안)에 있으면 "설치(장착)" 상호작용이 힌트로 노출됩니다. 상호작용하면
  /// 이 오브젝트가 표시(Show)되고 인벤토리의 흡인기 1개가 소비됩니다.
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
  /// 표시(설치)를 브로드캐스트하고(신규 접속자 포함) 요청자 클라이언트에서 흡인기가 소비됩니다
  /// (진실 원천: <see cref="StaticObjectDisplaymentService"/>). LocalOnly 이면 상호작용한 클라이언트에서만
  /// 소비/표시되고 전파되지 않습니다.
  /// </para>
  /// </summary>
  [DisallowMultipleComponent]
  public class WallAttachedWallSuction : StaticObjectDisplayment, INearestOnlyInteract
  {
    private sealed class YankauerConnectionInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly WallAttachedWallSuction _owner;
      public YankauerConnectionInteract(WallAttachedWallSuction owner) => _owner = owner;
      public string DisplayText => _owner._yankauerConnected ? "양커를 흡인기에서 분리" : "양커 팁 연결";
      public string PresentationEntityIdentifier => "patient_a_wall_suction";
      public string InteractionIdentifier => "connect_yankauer";
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public bool CanInteract(Transform interactor) =>
        _owner.IsPatientAInstallationTarget && _owner.IsAttached
        && interactor?.GetComponentInParent<PlayerController>() != null;
      public void Interact(Transform interactor)
      {
        if (!CanInteract(interactor))
          return;
        var player = interactor.GetComponentInParent<PlayerController>();
        if (_owner._yankauerConnected)
        {
          _owner.DisconnectYankauer();
          return;
        }
        if (player.CountItemInInventory("yankauer_suction_ready") < 1)
        {
          _owner.ShowMissingYankauerDialogue();
          return;
        }
        _owner.ConnectYankauer(player);
      }
    }

    public static event Action<WallAttachedWallSuction, bool> AttachmentStateChanged;
    /// <summary>플레이어 상호작용으로 새 설치가 확정된 경우에만 발생한다.</summary>
    public static event Action<WallAttachedWallSuction> InstallationConfirmed;

    /// <summary>설치(장착) 상호작용으로 인정하는, 손에 든 아이템 식별자입니다.</summary>
    private const string RequiredItemIdentifier = TriageTrainer.ItemDefinitions.WallSuction.Identifier;

    [Header("WallAttachedWallSuction")]
    [Tooltip("설치 시 인벤토리에서 소비할 흡인기 수량입니다.")]
    [SerializeField] private int _consumeCount = 1;

    [Tooltip("상호작용 힌트에 표시할 문구입니다.")]
    [SerializeField] private string _attachDisplayText = "흡인기 설치";

    [Tooltip("설치된 흡인기를 회수할 때 표시할 문구입니다.")]
    [SerializeField] private string _detachDisplayText = "흡인기 회수";

    [Tooltip("설치(적용) 완료 시 인게임 서버로 올릴 시나리오 신호입니다. 비우면 신호를 올리지 않습니다.")]
    [SerializeField] private string _attachCompletionSignal;

    [Tooltip("자동 석션 라인 연결에 사용할 장비 측 포트입니다.")]
    [SerializeField] private SuctionLineConnectionPoint _suctionLineConnectionPoint;

    [Tooltip("양커 라인이 플레이어 쪽에서 붙는 지점의 로컬 위치입니다. 연결 시 이 위치에 전용 지점을 만들어 플레이어를 따라다니게 합니다.")]
    [SerializeField] private Vector3 _yankauerHolderPointLocalPosition = new(0f, 0.9f, 0.35f);

    [SerializeField] private bool _yankauerConnected;
    private PlayerController _yankauerHolder;
    private Transform _yankauerHolderPoint;
    private LineRenderer _yankauerLine;
    private YankauerConnectionInteract _yankauerInteract;

    /// <summary>양커 라인의 플레이어 측 지점으로 사용할 자식 오브젝트 이름입니다.</summary>
    private const string YankauerHolderPointName = "YankauerSuctionLinePoint";

    public override IInteract[] Interacts => new IInteract[]
    {
      this,
      _yankauerInteract ??= new YankauerConnectionInteract(this)
    };

    /// <summary>
    /// 이 흡인기가 벽면에 설치(적용)되었는지 여부입니다.
    /// 상호작용으로 표시된 상태를 "설치했다 / 적용했다" 로 이해하며, 이후 데이터로 사용할 수 있도록 공개합니다.
    /// 서버 권위 프로토콜(ServerShared)에서는 모든 클라이언트에서 동일하게 반영됩니다.
    /// </summary>
    [SerializeField] private bool _isAttached;
    public bool IsAttached => _isAttached;
    /// <summary>석션 라인 자동 연결에 사용할 장비 측 포트. 프리팹에 설정되지 않으면 null이다.</summary>
    public SuctionLineConnectionPoint SuctionLineConnectionPoint => _suctionLineConnectionPoint;
    private bool IsPatientAInstallationTarget =>
      string.Equals(_attachCompletionSignal, "connect_wall_component_1", StringComparison.Ordinal);
    public override string PresentationEntityIdentifier =>
      IsPatientAInstallationTarget ? "patient_a_wall_suction" : base.PresentationEntityIdentifier;
    public override string InteractionIdentifier =>
      IsPatientAInstallationTarget ? "wall_suction_install" : base.InteractionIdentifier;
    private Sprite _installationItemIcon;

    protected override string EntityIdPrefix => "wall_suction";
    // 미설치 활성화 후보가 구역 경계에서 여러 개 감지되어도 PlayerController가 같은 그룹 중
    // 플레이어와 가장 가까운 하나만 힌트에 남긴다. 설치 후 회수 상호작용에는 필터를 적용하지 않는다.
    public string NearestOnlyGroup => IsAttached ? null : RequiredItemIdentifier;
    public Transform NearestOnlyDistanceOrigin => transform;
    public Collider NearestOnlyCollider => GetComponent<Collider>();
    public int NearestOnlyTieBreaker => GetInstanceID();
    public override IReadOnlyList<Sprite> DisplayIcons => new[] { Icon.ClearRightBottom, ResolveInstallationItemIcon() };

    public override string DisplayText
    {
      get
      {
        string baseText = base.DisplayText;
        if (!string.IsNullOrWhiteSpace(baseText))
          return baseText;

        if (IsAttached)
          return string.IsNullOrWhiteSpace(_detachDisplayText) ? "흡인기 회수" : _detachDisplayText;

        return string.IsNullOrWhiteSpace(_attachDisplayText) ? "흡인기 설치" : _attachDisplayText;
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

    // ── IInteractorConditional ───────────────────────────────────────────────

    /// <summary>
    /// 아직 설치되지 않았고, 플레이어가 흡인기를 손에 든 채 근처(콜라이더 범위)에 있을 때만 상호작용 가능합니다.
    /// </summary>
    public override bool CanInteract(Transform interactor)
    {
      var player = ResolvePlayer(interactor);
      if (player == null)
        return false;

      if (IsAttached)
      {
        return true;
      }

      return true;
    }

    private Sprite ResolveInstallationItemIcon()
    {
      if (_installationItemIcon != null)
        return _installationItemIcon;

      _installationItemIcon = MultiplayerInfrastructure.Registry.Registry.CreateItemInstance(
        RequiredItemIdentifier)?.CurrentItemIconTexture;
      return _installationItemIcon;
    }

    // ── IInteract ─────────────────────────────────────────────────────────────

    public override void Interact(Transform interactor)
    {
      var player = ResolvePlayer(interactor);
      if (player == null || !CanInteract(interactor))
      {
        if (player == null)
          Debug.LogWarning("[WallAttachedWallSuction] interactor 에서 PlayerController 를 찾지 못했습니다.", this);
        return;
      }

      if (IsAttached)
      {
        player.TryClearStaticObjectDisplaymentAndGrantItem(EntityIdentifier, RequiredItemIdentifier);
        return;
      }

      if (!IsHandlingWallSuction(player))
      {
        ShowMissingWallSuctionDialogue();
        return;
      }

      // 표시(설치) 요청을 베이스에 위임한다. ShareMode 에 따라 서버 전파(ServerShared) 또는 로컬 전용(LocalOnly)으로 처리된다.
      // - ServerShared: 서버 승인 → 요청자 인벤토리에서 흡인기 소비 → 확정 시 전체 브로드캐스트.
      // - LocalOnly: 이 클라이언트에서만 소비하고 즉시 표시.
      RequestApplyShown(player, RequiredItemIdentifier, Mathf.Max(1, _consumeCount));
    }

    private static void ShowMissingWallSuctionDialogue()
    {
      var dialogue = MultiplayerInfrastructure.Registry.Registry.Get<DialoguePanelUIController>(
        MultiplayerInfrastructure.Registry.RegistryType.UI,
        MultiplayerInfrastructure.Registry.Registry.TypeKey<DialoguePanelUIController>());
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", "(흡인기를 갖고 있지 않다.)");
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", "(흡인기를 찾자.)");
    }

    private void ShowMissingYankauerDialogue()
    {
      var dialogue = MultiplayerInfrastructure.Registry.Registry.Get<DialoguePanelUIController>(
        MultiplayerInfrastructure.Registry.RegistryType.UI,
        MultiplayerInfrastructure.Registry.Registry.TypeKey<DialoguePanelUIController>());
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", "(석션 라인과 양커 팁을 조립해두지 않았다.)");
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", "(석션 라인과 양커 팁을 찾아 조립하자.)");
    }

    private void ConnectYankauer(PlayerController player)
    {
      _yankauerConnected = true;
      _yankauerHolder = player;
      _yankauerHolderPoint = ResolveYankauerHolderPoint(player);
      var lineObject = new GameObject("PatientA_YankauerSuctionLine");
      lineObject.transform.SetParent(transform, false);
      _yankauerLine = lineObject.AddComponent<LineRenderer>();
      _yankauerLine.positionCount = 2;
      _yankauerLine.useWorldSpace = true;
      _yankauerLine.startWidth = SuctionLineConnectionPoint.LineWidth;
      _yankauerLine.endWidth = SuctionLineConnectionPoint.LineWidth;
      _yankauerLine.material = SuctionLineConnectionPoint.DefaultMaterial;
      UpdateYankauerLine();
      GameLogService.WriteInteraction(
        $"Yankauer suction line connected: entity={EntityIdentifier}, player={player.ScenarioEntityIdentifier}",
        EntityIdentifier);
      MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(
        "connect_wall_component_and_yankauer");
      player.RefreshInteractableHintsNow();
    }

    /// <summary>
      /// 양커 라인이 플레이어 쪽에서 붙을 지점을 확보한다. 플레이어 하위에 전용 자식 오브젝트를 두므로,
    /// 플레이어가 이동하거나 회전해도 지점이 함께 따라간다. 이미 만들어 둔 지점이 있으면 그대로 재사용한다.
    /// </summary>
    private Transform ResolveYankauerHolderPoint(PlayerController player)
    {
      var existing = player.transform.Find(YankauerHolderPointName);
      if (existing != null)
        return existing;

      var point = new GameObject(YankauerHolderPointName).transform;
      point.SetParent(player.transform, false);
      point.localPosition = _yankauerHolderPointLocalPosition;
      point.localRotation = Quaternion.identity;
      return point;
    }

    private void LateUpdate()
    {
      if (!_yankauerConnected)
        return;

      // 연결은 "양커를 흡인기에서 분리" 상호작용으로만 끊는다. 구강 흡인으로 양커가
      // 인벤토리에서 소비되어도 라인은 플레이어 지점에 그대로 붙어 있어야 하므로, 인벤토리 수량으로
      // 연결을 끊지 않는다. 연결 상대가 사라진 경우(퇴장·디스폰)에만 그릴 대상이 없으므로 정리한다.
      if (_yankauerHolder == null || _yankauerHolderPoint == null)
      {
        DisconnectYankauer();
        return;
      }

      UpdateYankauerLine();
    }

    private void UpdateYankauerLine()
    {
      if (_yankauerLine == null || _yankauerHolderPoint == null)
        return;
      _yankauerLine.SetPosition(0,
        _suctionLineConnectionPoint != null ? _suctionLineConnectionPoint.transform.position : transform.position);
      _yankauerLine.SetPosition(1, _yankauerHolderPoint.position);
    }

    private void DisconnectYankauer()
    {
      if (!_yankauerConnected)
        return;
      _yankauerConnected = false;
      _yankauerHolder = null;
      if (_yankauerHolderPoint != null)
        Destroy(_yankauerHolderPoint.gameObject);
      _yankauerHolderPoint = null;
      if (_yankauerLine != null)
        Destroy(_yankauerLine.gameObject);
      _yankauerLine = null;
      GameLogService.WriteInteraction(
        $"Yankauer suction line disconnected: entity={EntityIdentifier}", EntityIdentifier);
      MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Clear(
        "connect_wall_component_and_yankauer");
    }

    // ── 표시 적용 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 이 오브젝트가 표시(설치/적용)될 때 로컬 표현에 반영한다(모든 클라이언트에서 실행).
    /// 표시와 함께 <see cref="IsAttached"/> 를 확정한다. 완료 신호는 "최초 확정 시 1회"만 필요하므로
    /// 여기가 아니라 <see cref="OnShownConfirmed"/> 에서 처리한다(옵저버 중복 실행 방지).
    /// </summary>
    public override void ApplyShownFromNetwork()
    {
      Show();
      // CareZone reconnects synchronously from AttachmentStateChanged, so the
      // equipment collider must already be visible when the event is raised.
      SetAttached(true);
    }

    /// <summary>
    /// 표시(설치)가 권위 경로에서 최초로 확정될 때 1회 호출된다(ServerShared: 서버, LocalOnly: 로컬).
    /// 설치 완료 시나리오 신호를 여기서 올린다.
    /// </summary>
    public override void OnShownConfirmed()
    {
      SetAttached(true);
      TriageWorldInteractionSignals.RaiseWallSuctionInstalled(EntityIdentifier);
      TriageWorldInteractionSignals.RaiseWallSuctionEnabled(EntityIdentifier);
      InstallationConfirmed?.Invoke(this);
      RaiseCompletionSignalIfAny();
    }

    public override void OnHiddenConfirmed()
    {
      SetAttached(false);
      TriageWorldInteractionSignals.RaiseWallSuctionRemoved(EntityIdentifier);
      TriageWorldInteractionSignals.RaiseWallSuctionDisabled(EntityIdentifier);
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

    /// <summary>플레이어 인벤토리에 흡인기가 있는지 판정한다.</summary>
    private static bool IsHandlingWallSuction(PlayerController player)
    {
      return player != null && player.CountItemInInventory(RequiredItemIdentifier) > 0;
    }

    private void SetAttached(bool attached)
    {
      if (IsAttached == attached)
        return;
      _isAttached = attached;
      GameLogService.WriteInteraction(
        $"Wall suction attachment state: entity={EntityIdentifier}, attached={attached}",
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
