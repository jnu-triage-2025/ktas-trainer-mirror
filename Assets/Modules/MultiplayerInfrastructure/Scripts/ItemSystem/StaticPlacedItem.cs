using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 유니티 에디터에 사전 배치되는 정적 아이템입니다.
  ///
  /// <para>
  /// <see cref="ItemObject"/>(월드 드롭 아이템)와 달리 Rigidbody 물리로 스폰/산란하지 않습니다.
  /// 네트워크 스폰 오브젝트가 아니라 각 프로세스에 로컬로 존재하는 "맵의 일부"이며, 엔티티가 아닌 것에 가깝지만
  /// (Registry에는 편의를 위해 <see cref="EntityType.StaticPlacedItem"/> 로 등록됨) Interactable 합니다.
  /// 상호작용(획득)하면 아이템을 얻고, 설정에 따라 맵에서 사라지게 할 수 있습니다.
  /// </para>
  ///
  /// <para>
  /// 배치가 에디터 표현 그대로 유지되므로 서버 시작 시 위치가 흩어지거나 서로 충돌하는 문제가 없습니다.
  /// </para>
  ///
  /// <para>
  /// 상태(Remains)의 진실 원천은 서버이며 <see cref="StaticPlacedItemService"/> 가 관리합니다.
  /// 상호작용 요청은 <see cref="PlayerController"/> 의 서버 권위 픽업 프로토콜로 위임됩니다.
  /// </para>
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Collider))]
  public class StaticPlacedItem : Interactable, IInteractorConditional
  {
    [Header("StaticPlacedItem")]
    [SerializeField] private string _entityIdentifier;

    [Header("Pickup")]
    [SerializeField]
    private StaticPlacedItemPickupReward _pickupReward = StaticPlacedItemPickupReward.CreateDefault();

    [SerializeField]
    private StaticPlacedItemState _initialState = StaticPlacedItemState.CreateDefault();

    [Header("Vanish Behaviour")]
    [SerializeField]
    private StaticPlacedItemVanishMode _vanishMode = StaticPlacedItemVanishMode.VanishedGlobalOnPickup;

    [SerializeField]
    private StaticPlacedItemVanishBehavior _vanishBehavior = StaticPlacedItemVanishBehavior.Invisible;

    [Header("Model")]
    [Tooltip("비우면 Resources/Models/Items/{ItemIdentifier} 프리팹을 자동 로드합니다. 직접 자식 모델을 두었다면 비워두세요.")]
    [SerializeField] private bool _autoLoadModel = false;

    private const string ModelRootPath = "Models/Items";

    /// <summary>이 정적 아이템의 전역 식별자(서버/모든 클라이언트 동일).</summary>
    public string EntityIdentifier => _entityIdentifier;

    public StaticPlacedItemVanishMode VanishMode => _vanishMode;
    public StaticPlacedItemVanishBehavior VanishBehavior => _vanishBehavior;
    public StaticPlacedItemPickupReward PickupReward => _pickupReward;
    public int InitialRemains => _initialState.Remains;

    private GameObject _loadedModel;
    private string _registeredIdentifier;

    /// <summary>현재 로컬 표현이 사라짐(Vanished) 상태로 적용되어 있는지.</summary>
    private bool _localVanished;

    private void Awake()
    {
      if (string.IsNullOrWhiteSpace(_entityIdentifier))
        _entityIdentifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_entityIdentifier, gameObject, "static-item");

      if (_autoLoadModel)
        LoadModelIfNeeded();

      EnsureInteractionCollider();
      RegisterToRegistry();
    }

    /// <summary>
    /// 상호작용 감지를 위한 Collider 를 보장합니다.
    ///
    /// <para>
    /// <see cref="NearbyInteractablesDetector"/> 는 Collider 와 같은 GameObject 의 <see cref="IInteractable"/>
    /// 을 통해 감지하므로 Collider 가 반드시 필요합니다. <c>[RequireComponent(typeof(Collider))]</c> 로 인해
    /// 에디터에서 BoxCollider 가 자동 부착되지만, 자동 부착된 콜라이더는 크기가 잡히지 않을 수 있습니다.
    /// 콜라이더 크기가 사실상 0이면 자식 Renderer 들의 bounds 로 보정합니다(에디터 변환 도구가 이미 크기를 설정했다면 건드리지 않음).
    /// </para>
    /// </summary>
    private void EnsureInteractionCollider()
    {
      if (!TryGetComponent<Collider>(out var existing) || existing == null)
      {
        // RequireComponent 가 보장하지 못한 예외적 상황 폴백.
        gameObject.AddComponent<BoxCollider>();
      }

      if (!TryGetComponent<BoxCollider>(out var box) || box == null)
        return;

      // 이미 유의미한 크기가 설정되어 있으면 그대로 둔다.
      if (box.size.sqrMagnitude > 0.0001f)
        return;

      FitBoxColliderToRenderers(box);
    }

    private void FitBoxColliderToRenderers(BoxCollider box)
    {
      var renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
      if (renderers == null || renderers.Length == 0)
      {
        // Renderer 가 없으면 최소한의 기본 크기라도 부여해 감지가 가능하게 한다.
        box.size = Vector3.one * 0.25f;
        return;
      }

      bool hasBounds = false;
      Bounds worldBounds = default;
      for (int i = 0; i < renderers.Length; i++)
      {
        if (renderers[i] == null || !renderers[i].enabled)
          continue;

        if (!hasBounds)
        {
          worldBounds = renderers[i].bounds;
          hasBounds = true;
        }
        else
        {
          worldBounds.Encapsulate(renderers[i].bounds);
        }
      }

      if (!hasBounds)
      {
        box.size = Vector3.one * 0.25f;
        return;
      }

      // 월드 bounds → 로컬 기준으로 변환.
      box.center = transform.InverseTransformPoint(worldBounds.center);
      var lossy = transform.lossyScale;
      box.size = new Vector3(
        SafeDivide(worldBounds.size.x, lossy.x),
        SafeDivide(worldBounds.size.y, lossy.y),
        SafeDivide(worldBounds.size.z, lossy.z));
    }

    private static float SafeDivide(float value, float divisor)
      => Mathf.Approximately(divisor, 0f) ? value : value / divisor;

    private void OnEnable()
    {
      RegisterToRegistry();
    }

    private void OnDisable()
    {
      // Deactivate behavior 로 인한 사라짐(Vanished)일 때는 Registry 등록을 유지한다.
      // 그래야 신규 접속자 동기화/서버 조회가 계속 대상을 찾을 수 있다.
      // 씬 언로드/실제 파괴는 OnDestroy 에서 해제한다.
      if (_localVanished && _vanishBehavior == StaticPlacedItemVanishBehavior.Deactivate)
        return;

      UnregisterFromRegistry();
    }

    private void OnDestroy()
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

    private void LoadModelIfNeeded()
    {
      if (_loadedModel != null || string.IsNullOrWhiteSpace(_pickupReward.ItemIdentifier))
        return;

      string path = $"{ModelRootPath}/{_pickupReward.ItemIdentifier}";
      var prefab = Resources.Load<GameObject>(path);
      if (prefab == null)
      {
        Debug.LogWarning(
          $"[StaticPlacedItem] '{name}' 모델 프리팹을 찾지 못했습니다. (경로: Resources/{path})");
        return;
      }

      _loadedModel = Instantiate(prefab, transform);
      _loadedModel.name = "StaticPlacedItemModel";
      _loadedModel.transform.localPosition = Vector3.zero;
      _loadedModel.transform.localRotation = Quaternion.identity;
    }

    // ── IInteract ──────────────────────────────────────────────────────────

    public override string DisplayText
    {
      get
      {
        string baseText = base.DisplayText;
        if (!string.IsNullOrWhiteSpace(baseText))
          return baseText;

        return "획득";
      }
    }

    public override void Interact(Transform interactor)
    {
      if (_localVanished)
        return;

      var player = interactor.GetComponentInParent<PlayerController>()
                ?? interactor.GetComponent<PlayerController>();
      if (player == null)
      {
        Debug.LogWarning("[StaticPlacedItem] interactor에서 PlayerController를 찾지 못했습니다.");
        return;
      }

      if (string.IsNullOrWhiteSpace(_entityIdentifier))
      {
        Debug.LogWarning($"[StaticPlacedItem] '{name}' has empty identifier. Cannot pickup.");
        return;
      }

      player.TryPickupStaticPlacedItem(_entityIdentifier);
    }

    // ── IInteractorConditional ───────────────────────────────────────────────

    /// <summary>
    /// 사라짐(Vanished) 상태에서는 어떤 <see cref="StaticPlacedItemVanishBehavior"/> 든 상호작용을 항상 차단합니다.
    /// </summary>
    public bool CanInteract(Transform interactor)
    {
      return !_localVanished;
    }

    // ── 로컬 표현 제어 (PlayerController RPC 에서 호출) ───────────────────────

    /// <summary>
    /// 로컬 프로세스에서 이 정적 아이템을 사라짐(Vanished) 상태로 적용합니다.
    /// VanishBehavior 에 따라 렌더러/오브젝트/상호작용을 처리합니다.
    /// </summary>
    public void ApplyVanished()
    {
      _localVanished = true;

      switch (_vanishBehavior)
      {
        case StaticPlacedItemVanishBehavior.Deactivate:
          gameObject.SetActive(false);
          break;

        case StaticPlacedItemVanishBehavior.DisableInteraction:
          // 보이되 상호작용만 비활성(CanInteract 가 false 반환).
          break;

        case StaticPlacedItemVanishBehavior.Invisible:
        default:
          SetRenderersEnabled(false);
          break;
      }
    }

    /// <summary>
    /// 로컬 프로세스에서 사라짐 상태를 해제하고 다시 존재/획득 가능하게 되돌립니다.
    ///
    /// <para>
    /// 현재 프로토콜은 사라짐을 단방향으로만 처리하므로 이 메서드는 의도적으로 호출되지 않는다.
    /// 향후 런타임 복원(관리자 리셋 / Remains 재충전 / 리스폰) 기능이 추가될 때 사용하기 위한 예약 API 이다.
    /// </para>
    /// </summary>
    public void ApplyRestored()
    {
      _localVanished = false;

      if (!gameObject.activeSelf)
        gameObject.SetActive(true);

      SetRenderersEnabled(true);
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

#if UNITY_EDITOR
    private void OnValidate()
    {
      if (string.IsNullOrWhiteSpace(_entityIdentifier))
        _entityIdentifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_entityIdentifier, gameObject, "static-item");
    }

    private void OnDrawGizmos()
    {
      bool hasItem = !string.IsNullOrWhiteSpace(_pickupReward.ItemIdentifier);

      Gizmos.color = hasItem
        ? new Color(0.2f, 0.6f, 0.95f, 0.55f)
        : new Color(0.95f, 0.2f, 0.2f, 0.55f);
      Gizmos.DrawSphere(transform.position, 0.15f);

      Gizmos.color = hasItem
        ? new Color(0.1f, 0.45f, 0.85f, 1f)
        : new Color(0.8f, 0.1f, 0.1f, 1f);
      Gizmos.DrawWireSphere(transform.position, 0.15f);

      string label = hasItem ? _pickupReward.ItemIdentifier : "(item 미설정)";
      UnityEditor.Handles.Label(
        transform.position + Vector3.up * 0.26f,
        hasItem ? $"[static] {label} x{_pickupReward.Amount}" : label,
        new GUIStyle(UnityEditor.EditorStyles.miniLabel)
        {
          normal = { textColor = hasItem ? Color.cyan : Color.red }
        });
    }
#endif
  }
}
