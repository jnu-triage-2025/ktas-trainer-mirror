using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 맵에 사전 배치되어 있으며 다른 제어 흐름(상호작용/시나리오 등)에 의해 표시(Show)/비표시(Hide)될 수 있는
  /// 정적 오브젝트의 공통 골격(추상 베이스)입니다.
  ///
  /// <para>
  /// <see cref="StaticPlacedItem"/> 과 마찬가지로 Rigidbody 물리로 스폰하지 않으며, 네트워크 스폰 오브젝트가
  /// 아니라 각 프로세스에 로컬로 존재하는 "맵의 일부"입니다. Interactable 하며(상호작용 항목이 있고),
  /// 상호작용/외부 흐름에 따라 렌더러 또는 게임오브젝트를 켜고 끌 수 있습니다.
  /// (편의를 위해 Registry 에는 <see cref="EntityType.StaticPlacedItem"/> 로 등록됩니다.)
  /// </para>
  ///
  /// <para>
  /// <see cref="StaticPlacedItem"/> 는 "획득 상호작용 → 설정에 따라 표시 On/Off" 라는 하나의 구체적인 동작을
  /// 가지지만, <see cref="StaticObjectDisplayment"/> 는 <b>구조만</b> 공유합니다. 실제 상호작용 동작
  /// (무엇을 소비/적용하고 언제 표시할지)은 파생 구현마다 다르므로, <see cref="Interact"/> 와
  /// <see cref="CanInteract"/> 를 파생 클래스가 정의합니다. 베이스는 등록/콜라이더 보장/표시 토글 유틸리티만
  /// 제공합니다.
  /// </para>
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Collider))]
  public abstract class StaticObjectDisplayment : Interactable, IInteractorConditional
  {
    [Header("StaticObjectDisplayment")]
    [SerializeField] private string _entityIdentifier;

    [Tooltip("시작 시 표시(보임) 여부. false 이면 처음에 숨겨진 상태로 시작합니다. " +
             "일부 파생 구현은 규약상 항상 특정 초기 상태로 시작하도록 이 값을 무시(강제)할 수 있습니다.")]
    [SerializeField] private bool _initiallyVisible = true;

    [Tooltip("표시/비표시를 게임오브젝트 활성화(SetActive)로 처리할지 여부. " +
             "false 이면 자식 Renderer 들의 enabled 만 토글합니다(콜라이더/스크립트는 유지).")]
    [SerializeField] private bool _toggleByActiveState = false;

    [Tooltip("표시(설치/적용) 상태를 서버 권위로 모든 클라이언트에 전파할지(ServerShared, 기본값), " +
             "아니면 각 로컬 세션에서만 유지할지(LocalOnly) 결정합니다.")]
    [SerializeField] private StaticObjectDisplaymentShareMode _shareMode = StaticObjectDisplaymentShareMode.ServerShared;

    /// <summary>파생 클래스가 식별자 자동 생성 시 사용할 접두사입니다.</summary>
    protected virtual string EntityIdPrefix => "static-object";

    /// <summary>이 정적 오브젝트의 전역 식별자(서버/모든 클라이언트 동일).</summary>
    public string EntityIdentifier => _entityIdentifier;

    public void SetEntityIdentifier(string identifier)
    {
      string normalized = identifier == null ? string.Empty : identifier.Trim();
      if (string.IsNullOrWhiteSpace(normalized)
          || string.Equals(_entityIdentifier, normalized, System.StringComparison.Ordinal))
        return;

      UnregisterFromRegistry();
      _entityIdentifier = normalized;
      RegisterToRegistry();
    }

    /// <summary>표시(설치/적용) 상태의 전파 방식입니다.</summary>
    public StaticObjectDisplaymentShareMode ShareMode => _shareMode;

    /// <summary>표시(설치/적용) 상태가 서버 권위로 모든 클라이언트에 전파되는지 여부입니다.</summary>
    public bool SharesShownStateAcrossServer => _shareMode == StaticObjectDisplaymentShareMode.ServerShared;

    /// <summary>현재 로컬 표현이 표시(보임) 상태인지 여부입니다.</summary>
    public bool IsVisible { get; private set; }

    public virtual bool TryGetServerSharedItemExchange(out string itemIdentifier, out int consumeCount)
    {
      itemIdentifier = string.Empty;
      consumeCount = 0;
      return false;
    }

    private string _registeredIdentifier;

    protected virtual void Awake()
    {
      if (string.IsNullOrWhiteSpace(_entityIdentifier))
        _entityIdentifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_entityIdentifier, gameObject, EntityIdPrefix);

      EnsureInteractionCollider();
      RegisterToRegistry();

      // 초기 표시 상태를 적용한다(파생 구현이 초기 상태를 오버라이드할 여지를 남긴다).
      ApplyInitialVisibility();
    }

    /// <summary>
    /// 시작 시점의 표시 상태를 적용합니다. 기본 구현은 인스펙터의 <c>_initiallyVisible</c> 값을 따릅니다.
    /// 규약상 항상 특정 상태로 시작해야 하는 파생 구현은 이 메서드를 오버라이드합니다.
    /// </summary>
    protected virtual void ApplyInitialVisibility()
    {
      SetVisible(_initiallyVisible);
    }

    protected virtual void OnEnable()
    {
      RegisterToRegistry();
    }

    protected virtual void OnDisable()
    {
      // SetActive(false) 로 인한 비표시일 때는 Registry 등록을 유지한다.
      // 그래야 신규 접속자 동기화/서버 조회가 계속 대상을 찾을 수 있다.
      // 씬 언로드/실제 파괴는 OnDestroy 에서 해제한다.
      if (!IsVisible && _toggleByActiveState)
        return;

      UnregisterFromRegistry();
    }

    protected virtual void OnDestroy()
    {
      UnregisterFromRegistry();
    }

    private void RegisterToRegistry()
    {
      if (string.IsNullOrWhiteSpace(_entityIdentifier))
        return;

      _registeredIdentifier = _entityIdentifier;
      Registry.Registry.RegisterEntity(
        _registeredIdentifier,
        EntityType.StaticPlacedItem,
        gameObject,
        displayName: gameObject.name);
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      Registry.Registry.UnregisterEntity(_registeredIdentifier);
      _registeredIdentifier = null;
    }

    /// <summary>
    /// 상호작용 감지를 위한 Collider 를 보장합니다.
    /// <see cref="NearbyInteractablesDetector"/> 는 Collider 와 같은 GameObject 의 <see cref="IInteractable"/>
    /// 을 통해 감지하므로 Collider 가 반드시 필요합니다.
    /// </summary>
    private void EnsureInteractionCollider()
    {
      if (!TryGetComponent<Collider>(out var existing) || existing == null)
      {
        // RequireComponent 가 보장하지 못한 예외적 상황 폴백.
        gameObject.AddComponent<BoxCollider>();
      }
    }

    // ── 표시 제어 (파생 구현 / 외부 제어 흐름에서 호출) ───────────────────────

    /// <summary>이 오브젝트를 표시(보임) 상태로 만든다.</summary>
    public virtual void Show()
    {
      IsVisible = true;

      if (_toggleByActiveState)
      {
        if (!gameObject.activeSelf)
          gameObject.SetActive(true);
      }
      else
      {
        SetRenderersEnabled(true);
      }
    }

    /// <summary>이 오브젝트를 비표시(숨김) 상태로 만든다.</summary>
    public virtual void Hide()
    {
      IsVisible = false;

      if (_toggleByActiveState)
      {
        gameObject.SetActive(false);
      }
      else
      {
        SetRenderersEnabled(false);
      }
    }

    /// <summary>표시 상태를 <paramref name="visible"/> 로 설정한다.</summary>
    public void SetVisible(bool visible)
    {
      if (visible)
        Show();
      else
        Hide();
    }

    /// <summary>
    /// "이 오브젝트를 표시(설치/적용)된 상태로 로컬 표현에 반영" 하는 진입점입니다.
    /// ServerShared 모드에서는 서버 권위 프로토콜(<see cref="Player.PlayerController"/> 의 표시 적용 RPC)에 의해
    /// 모든 클라이언트에서 호출되고, LocalOnly 모드에서는 상호작용한 클라이언트에서 <see cref="RequestApplyShown"/>
    /// 를 통해 호출됩니다.
    ///
    /// <para>
    /// 기본 구현은 단순히 <see cref="Show"/> 를 호출합니다. 파생 클래스가 표시 외에 추가 상태
    /// (예: <c>IsAttached</c> 플래그)를 함께 갱신해야 하면 이 메서드를 오버라이드합니다.
    /// 단, "최초 확정 시 1회" 부수효과는 여기가 아니라 <see cref="OnShownConfirmed"/> 에서 처리합니다.
    /// </para>
    /// </summary>
    public virtual void ApplyShownFromNetwork()
    {
      Show();
    }

    /// <summary>서버 전역 상태가 해제됐을 때 모든 클라이언트의 로컬 표현을 숨깁니다.</summary>
    public virtual void ApplyHiddenFromNetwork()
    {
      Hide();
    }

    /// <summary>
    /// 표시(설치/적용)가 <b>권위 경로에서 최초로 확정</b>되었을 때 한 번만 호출되는 훅입니다.
    ///
    /// <list type="bullet">
    /// <item><see cref="StaticObjectDisplaymentShareMode.ServerShared"/>: 서버에서 상태를 확정한 직후
    /// (전체 브로드캐스트 전에) 서버 컨텍스트에서 1회 호출됩니다.</item>
    /// <item><see cref="StaticObjectDisplaymentShareMode.LocalOnly"/>: 로컬에서 표시를 적용할 때 1회 호출됩니다.</item>
    /// </list>
    ///
    /// 시나리오 신호 발생처럼 "최초 확정 시 정확히 한 번" 수행해야 하는 부수효과를 이 훅에서 처리하세요.
    /// (모든 옵저버에서 실행되는 <see cref="ApplyShownFromNetwork"/> 에 두면 중복 실행됩니다.)
    /// 기본 구현은 아무것도 하지 않습니다.
    /// </summary>
    public virtual void OnShownConfirmed()
    {
    }

    /// <summary>표시(설치)가 권위 경로에서 해제되었을 때 한 번 호출되는 훅입니다.</summary>
    public virtual void OnHiddenConfirmed()
    {
    }

    /// <summary>
    /// 표시(설치/적용)를 요청합니다. <see cref="ShareMode"/> 에 따라 전파 경로가 달라집니다.
    ///
    /// <list type="bullet">
    /// <item><see cref="StaticObjectDisplaymentShareMode.ServerShared"/>: <see cref="PlayerController"/> 의 서버 권위
    /// 프로토콜로 위임합니다(승인 → 요청자 인벤토리 소비 → 전체 브로드캐스트, 신규 접속자 동기화 포함).</item>
    /// <item><see cref="StaticObjectDisplaymentShareMode.LocalOnly"/>: 이 클라이언트에서만 요구 아이템을 소비하고
    /// 즉시 표시를 적용합니다(네트워크 전파 없음).</item>
    /// </list>
    ///
    /// 요구 아이템 소비가 필요 없으면 <paramref name="requiredItemIdentifier"/> 를 비우거나
    /// <paramref name="consumeCount"/> 를 0 으로 둡니다.
    /// </summary>
    /// <returns>요청/적용을 시작했으면 true. (실패한 로컬 소비 등으로) 적용하지 못했으면 false.</returns>
    protected bool RequestApplyShown(PlayerController requester, string requiredItemIdentifier = null, int consumeCount = 0)
    {
      if (requester == null)
        return false;

      if (string.IsNullOrWhiteSpace(_entityIdentifier))
      {
        Debug.LogWarning($"[StaticObjectDisplayment] '{name}' 의 식별자가 비어 있어 표시를 적용할 수 없습니다.", this);
        return false;
      }

      int count = Mathf.Max(0, consumeCount);
      bool consumeRequired = !string.IsNullOrWhiteSpace(requiredItemIdentifier) && count > 0;

      if (SharesShownStateAcrossServer)
      {
        // 서버 권위 프로토콜로 위임: 소비/표시 확정/브로드캐스트를 서버가 조율한다.
        requester.TryApplyStaticObjectDisplayment(_entityIdentifier, requiredItemIdentifier, count);
        return true;
      }

      // LocalOnly: 이 클라이언트에서만 소비하고 즉시 표시한다.
      if (consumeRequired)
      {
        if (requester.CountItemInInventory(requiredItemIdentifier) < count)
          return false;

        int removed = requester.RemoveItemFromInventory(requiredItemIdentifier, count);
        if (removed < count)
        {
          Debug.LogWarning(
            $"[StaticObjectDisplayment] '{name}' 로컬 표시 적용: 아이템 소비 부족({removed}/{count}).", this);
          return false;
        }
      }

      ApplyShownFromNetwork();
      // 로컬 전용 경로에서도 "최초 확정" 훅을 1회 호출한다(전파는 없지만 부수효과는 필요).
      OnShownConfirmed();
      requester.RefreshInteractableHintsNow();
      return true;
    }

    private void SetRenderersEnabled(bool enabled)
    {
      var renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
      for (int i = 0; i < renderers.Length; i++)
      {
        if (renderers[i] != null)
          renderers[i].enabled = enabled;
      }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    /// <summary>interactor Transform 에서 <see cref="PlayerController"/> 를 해석한다(없으면 null).</summary>
    protected static PlayerController ResolvePlayer(Transform interactor)
    {
      if (interactor == null)
        return null;

      return interactor.GetComponentInParent<PlayerController>()
          ?? interactor.GetComponent<PlayerController>();
    }

    // ── IInteract / IInteractorConditional (파생 구현에서 정의) ────────────────

    /// <inheritdoc />
    public abstract override void Interact(Transform interactor);

    /// <inheritdoc />
    public abstract bool CanInteract(Transform interactor);

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
      if (string.IsNullOrWhiteSpace(_entityIdentifier))
        _entityIdentifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_entityIdentifier, gameObject, EntityIdPrefix);
    }
#endif
  }
}
