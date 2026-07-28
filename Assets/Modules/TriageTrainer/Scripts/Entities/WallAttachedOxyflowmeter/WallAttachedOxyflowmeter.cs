using System;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
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
  public class WallAttachedOxyflowmeter : StaticObjectDisplayment
  {
    /// <summary>설치(장착) 상호작용으로 인정하는, 손에 든 아이템 식별자입니다.</summary>
    private const string RequiredItemIdentifier = TriageTrainer.ItemDefinitions.Oxyflowmeter.Identifier;

    [Header("WallAttachedOxyflowmeter")]
    [Tooltip("설치 시 인벤토리에서 소비할 산소 유량계 수량입니다.")]
    [SerializeField] private int _consumeCount = 1;

    [Tooltip("상호작용 힌트에 표시할 문구입니다.")]
    [SerializeField] private string _attachDisplayText = "산소 유량계 설치";

    [Tooltip("설치(적용) 완료 시 인게임 서버로 올릴 시나리오 신호입니다. 비우면 신호를 올리지 않습니다.")]
    [SerializeField] private string _attachCompletionSignal;

    /// <summary>
    /// 이 산소 유량계가 벽면에 설치(적용)되었는지 여부입니다.
    /// 상호작용으로 표시된 상태를 "설치했다 / 적용했다" 로 이해하며, 이후 데이터로 사용할 수 있도록 공개합니다.
    /// 서버 권위 프로토콜에 의해 모든 클라이언트에서 동일하게 반영됩니다.
    /// </summary>
    public bool IsAttached { get; private set; }

    protected override string EntityIdPrefix => "wall_oxyflowmeter";

    public override string DisplayText
    {
      get
      {
        string baseText = base.DisplayText;
        if (!string.IsNullOrWhiteSpace(baseText))
          return baseText;

        return string.IsNullOrWhiteSpace(_attachDisplayText) ? "산소 유량계 설치" : _attachDisplayText;
      }
    }

    /// <summary>
    /// 이 오브젝트는 계약상 항상 "미설치(숨김)" 상태로 시작하므로 베이스의 <c>_initiallyVisible</c> 설정을 무시한다.
    /// </summary>
    protected override void ApplyInitialVisibility()
    {
      IsAttached = false;
      Hide();
    }

    // ── IInteractorConditional ───────────────────────────────────────────────

    /// <summary>
    /// 아직 설치되지 않았고, 플레이어가 산소 유량계를 손에 든 채 근처(콜라이더 범위)에 있을 때만 상호작용 가능합니다.
    /// </summary>
    public override bool CanInteract(Transform interactor)
    {
      if (IsAttached)
        return false;

      var player = ResolvePlayer(interactor);
      if (player == null)
        return false;

      return IsHandlingOxyflowmeter(player);
    }

    // ── IInteract ─────────────────────────────────────────────────────────────

    public override void Interact(Transform interactor)
    {
      if (!CanInteract(interactor))
        return;

      var player = ResolvePlayer(interactor);
      if (player == null)
      {
        Debug.LogWarning("[WallAttachedOxyflowmeter] interactor 에서 PlayerController 를 찾지 못했습니다.", this);
        return;
      }

      // 표시(설치) 요청을 베이스에 위임한다. ShareMode 에 따라 서버 전파(ServerShared) 또는 로컬 전용(LocalOnly)으로 처리된다.
      // - ServerShared: 서버 승인 → 요청자 인벤토리에서 산소 유량계 소비 → 확정 시 전체 브로드캐스트.
      // - LocalOnly: 이 클라이언트에서만 소비하고 즉시 표시.
      RequestApplyShown(player, RequiredItemIdentifier, Mathf.Max(1, _consumeCount));
    }

    // ── 표시 적용 (서버 권위 RPC 에서 호출) ────────────────────────────────────

    /// <summary>
    /// 이 오브젝트가 표시(설치/적용)될 때 로컬 표현에 반영한다(모든 클라이언트에서 실행).
    /// 표시와 함께 <see cref="IsAttached"/> 를 확정한다. 완료 신호는 "최초 확정 시 1회"만 필요하므로
    /// 여기가 아니라 <see cref="OnShownConfirmed"/> 에서 처리한다(옵저버 중복 실행 방지).
    /// </summary>
    public override void ApplyShownFromNetwork()
    {
      IsAttached = true;
      Show();
    }

    /// <summary>
    /// 표시(설치)가 권위 경로에서 최초로 확정될 때 1회 호출된다(ServerShared: 서버, LocalOnly: 로컬).
    /// 설치 완료 시나리오 신호를 여기서 올린다.
    /// </summary>
    public override void OnShownConfirmed()
    {
      TriageWorldInteractionSignals.RaiseOxyflowmeterInstalled(EntityIdentifier);
      RaiseCompletionSignalIfAny();
    }

    public override void OnHiddenConfirmed()
    {
      TriageWorldInteractionSignals.RaiseOxyflowmeterRemoved(EntityIdentifier);
    }

    /// <summary>설치 상태를 해제하고 다시 숨긴다(관리자 리셋/시나리오 되돌림 등에서 사용하는 로컬 표현 API).</summary>
    public void Detach()
    {
      IsAttached = false;
      Hide();
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    /// <summary>플레이어가 손에 산소 유량계를 들고 있는지 판정한다.</summary>
    private static bool IsHandlingOxyflowmeter(PlayerController player)
    {
      string heldIdentifier = player != null ? player.HandlingItem?.CurrentIdentifier : null;
      if (string.IsNullOrWhiteSpace(heldIdentifier))
        return false;

      return string.Equals(heldIdentifier, RequiredItemIdentifier, StringComparison.Ordinal);
    }

    private void RaiseCompletionSignalIfAny()
    {
      if (string.IsNullOrWhiteSpace(_attachCompletionSignal))
        return;

      MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(_attachCompletionSignal);
    }
  }
}
