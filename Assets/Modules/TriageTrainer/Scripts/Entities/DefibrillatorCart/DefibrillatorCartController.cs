using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using TriageTrainer.Scenario;
using UnityEngine;
using UnityEngine.Serialization;

using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 제세동 카트의 1인 조종을 전담하는 컨트롤러.
  /// 탑승/점유/이동의 네트워크 구현은 도메인 독립 공통 모듈
  /// <see cref="MinecraftBoatLikeControl"/> 에 위임한다(<see cref="Level1RapidInfuserController"/> 와 동일한 상속 패턴).
  ///
  /// 카트가 허용된 snap point에 도달하면 서버 권위로 스냅하고 시나리오 신호
  /// defibrillator_cart_snap_point_reached_{카트 식별자}_{포인트 식별자} 를 발생시킨다.
  /// </summary>
  public sealed class DefibrillatorCartController : MinecraftBoatLikeControl,
    IInteractable, IInteract, IInteractorConditional, ISpawnedEntityIdentifierReceiver
  {
    [Header("Identity")]
    [Tooltip("씬에 사전 배치된 이 카트 인스턴스의 고유 식별자입니다. EntityPreset으로 스폰되면 스폰 요청의 식별자로 대체됩니다.")]
    [SerializeField] private string _entityIdentifier = "defibrillator_cart_a";
    [SerializeField] private string _entityTypeIdentifier = "defibrillator_cart_a";

    [Header("Display")]
    [SerializeField] private string _displayText = "제세동 카트 조종";
    [SerializeField] private Sprite _displayIcon;

    [Header("Snap Point")]
    [FormerlySerializedAs("_enablePositioningSnap")]
    [SerializeField] private bool _enableSnap = true;
    [FormerlySerializedAs("_positioningSnapReleasePadding")]
    [SerializeField, Min(0f)] private float _snapReleasePadding = 0.2f;

    [Header("Snap Point Filter")]
    [Tooltip("비어 있으면 모든 DefibrillatorCartSnapPoint를 대상으로 스냅합니다. 값이 있으면 해당 Identifier 목록만 스냅 대상으로 허용합니다.")]
    [FormerlySerializedAs("_allowedPositioningPointIdentifiers")]
    [SerializeField] private List<string> _allowedSnapPointIdentifiers = new();

    [Header("AED Connection")]
    [Tooltip("환자에게 부착한 제세동 패드와 연결할 AED 라인 연결 지점들입니다. 비워 두면 자식 오브젝트에서 AEDLineConnectionPoint 를 찾는 fallback 을 사용합니다.")]
    [SerializeField]
    private TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint[] _aedConnectionPoints =
      Array.Empty<TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint>();

    /// <summary>
    /// 카트 측 AED 라인 연결 지점 목록. 참조가 비어 있으면(배선 누락)
    /// 자식 오브젝트에서 클래스 기준으로 찾는 fallback 을 수행한다.
    /// </summary>
    public IReadOnlyList<TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint> AedConnectionPoints
    {
      get
      {
        if (_aedConnectionPoints == null || _aedConnectionPoints.Length == 0)
          _aedConnectionPoints = GetComponentsInChildren<TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint>(true);
        return _aedConnectionPoints;
      }
    }

    /// <summary>런타임 엔티티 식별자(서버 권위 + 전 피어 복제). 비어 있으면 _entityTypeIdentifier를 사용한다.</summary>
    private readonly SyncVar<string> _runtimeIdentifierSync = new(string.Empty);
    private readonly SyncVar<string> _snapPointIdentifierSync = new(string.Empty);

    private DefibrillatorCartSnapPoint _latchedSnapPoint;
    private StaticPlacedItem _staticPlacedItem;
    private string _pendingSnapPointIdentifier;
    private string _entityRuntimeIdentifier;

    private string EffectiveIdentifier =>
      !string.IsNullOrWhiteSpace(_runtimeIdentifierSync.Value) ? _runtimeIdentifierSync.Value
      : !string.IsNullOrWhiteSpace(_entityRuntimeIdentifier) ? _entityRuntimeIdentifier
      : !string.IsNullOrWhiteSpace(_entityIdentifier) ? _entityIdentifier
      : _entityTypeIdentifier;

    public string Identifier => EffectiveIdentifier;
    public DefibrillatorCartSnapPoint LatchedSnapPoint => _latchedSnapPoint;

    public IInteract[] Interacts
    {
      get
      {
        // 제세동 패드는 카트와 동일한 감지 Collider를 사용한다. 조종 중에는
        // 패드를 집을 수 없도록 메뉴에서 제외하고, 하차하면 다시 노출한다.
        if (HasParticipants)
          return new IInteract[] { this };

        _staticPlacedItem ??= GetComponent<StaticPlacedItem>();

        var list = new List<IInteract> { this };
        if (_staticPlacedItem != null)
          list.Add(_staticPlacedItem);
        return list.ToArray();
      }
    }
    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon != null ? _displayIcon : ResolvedDefaultControlIcon;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    private void Awake()
    {
      Awake_MinecraftBoatLikeControl();
      Configure(1); // 제세동 카트는 한 명만 조종한다.
      _staticPlacedItem = GetComponent<StaticPlacedItem>();
    }

    private void OnDestroy()
    {
      UnregisterCartEntity();
    }

    private void Update()
    {
      ResolveSyncedSnapPointIfPending();
      Update_MinecraftBoatLikeControl();
      TrySnapToSnapPoint();
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      _runtimeIdentifierSync.OnChange += OnRuntimeIdentifierChanged;
      _snapPointIdentifierSync.OnChange += OnSnapPointIdentifierChanged;

      // 스폰 페이로드로 동기화된 식별자로 모든 피어에서 등록한다.
      RegisterCartEntity();
      RequestSnapPointResolution(_snapPointIdentifierSync.Value);
    }

    public override void OnStartServer()
    {
      base.OnStartServer();
      // 전용 서버에는 OnStartClient가 호출되지 않으므로 여기서도 엔티티를 등록한다.
      RegisterCartEntity();
    }

    public override void OnStopServer()
    {
      // 풀링된 NetworkObject는 OnDestroy 없이 재사용될 수 있으므로 서버 수명주기에서 해제한다.
      UnregisterCartEntity();
      base.OnStopServer();
    }

    public override void OnStopClient()
    {
      _runtimeIdentifierSync.OnChange -= OnRuntimeIdentifierChanged;
      _snapPointIdentifierSync.OnChange -= OnSnapPointIdentifierChanged;
      _pendingSnapPointIdentifier = null;
      _latchedSnapPoint = null;
      UnregisterCartEntity();
      base.OnStopClient();
    }

    public void Interact(Transform interactor)
    {
      if (interactor == null)
        return;

      Toggle(interactor);
    }

    public bool CanInteract(Transform interactor)
    {
      if (interactor == null)
        return false;

      // 대상을 들고 있는 상태에서는 카트 조종을 시작할 수 없다(침대와 동일한 정책).
      var player = interactor.GetComponentInParent<PlayerController>();
      if (player != null && player.IsCarryingReposable)
        return false;

      return CanToggle(interactor);
    }

    /// <summary>
    /// 엔티티 프리셋 스폰 시 식별자를 주입받는다(ISpawnedEntityIdentifierReceiver).
    /// </summary>
    public void ApplySpawnedEntityIdentifier(string identifier) => SetIdentifier(identifier);

    /// <summary>
    /// 런타임 엔티티 식별자를 설정한다. 서버에서 호출되면 SyncVar로 전 피어에 복제되고,
    /// 모든 피어가 동일 식별자로 레지스트리에 등록한다.
    /// </summary>
    public void SetIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      string trimmed = identifier.Trim();
      if (!string.IsNullOrWhiteSpace(_entityRuntimeIdentifier) &&
          !string.Equals(_entityRuntimeIdentifier, trimmed, StringComparison.Ordinal))
        UnregisterCartEntity();

      _entityRuntimeIdentifier = trimmed;

      if (IsServerStarted)
        _runtimeIdentifierSync.Value = trimmed;

      RegisterCartEntity();
    }

    private void OnRuntimeIdentifierChanged(string previous, string next, bool asServer)
    {
      if (string.IsNullOrWhiteSpace(next))
        return;

      if (!string.IsNullOrWhiteSpace(_entityRuntimeIdentifier) &&
          !string.Equals(_entityRuntimeIdentifier, next, StringComparison.Ordinal))
        UnregisterCartEntity();

      _entityRuntimeIdentifier = next;
      RegisterCartEntity();
    }

    /// <summary>동기화된 식별자로 카트를 레지스트리에 등록한다(모든 피어). 중복/갱신 안전.</summary>
    private void RegisterCartEntity()
    {
      string id = EffectiveIdentifier;
      if (string.IsNullOrWhiteSpace(id))
        return;

      _entityRuntimeIdentifier = id;

      try
      {
        Registry.RegisterEntity(
          id,
          EntityType.DefibrillatorCart,
          gameObject,
          displayName: _displayText,
          ownerUserIdentifier: null,
          clientId: null,
          isNetworked: IsClientStarted || IsServerStarted);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[DefibrillatorCart] Failed to register entity '{id}': {ex.Message}");
      }
    }

    private void UnregisterCartEntity()
    {
      if (!string.IsNullOrWhiteSpace(_entityRuntimeIdentifier))
        Registry.UnregisterEntity(_entityRuntimeIdentifier);
      _entityRuntimeIdentifier = null;
    }

    // ── Snap point ─────────────────────────────────────────────────────

    /// <summary>
    /// 시나리오 수동 진입 준비 체인이 카트를 지정한 자리로 옮길 때 사용한다.
    /// 정박 판정은 <see cref="TrySnapToSnapPoint"/> 가 매 프레임 거리로 수행하므로, 이 메서드는
    /// 위치만 서버 권위로 옮기고 정박·도달 신호는 올리지 않는다. 신호는 "플레이어가 카트를 실제로
    /// 밀고 왔다"는 근거이므로 준비 체인이 대신 만들어서는 안 된다.
    ///
    /// <para>
    /// 이전 회차에서 어딘가에 정박해 있었다면 그 참조를 먼저 끊는다. 정박 참조가 남아 있으면
    /// 다음 프레임의 판정이 이탈 처리를 거치면서 정박 해제 신호를 한 번 더 올린다.
    /// </para>
    /// </summary>
    public void PlaceForScenario(Vector3 position, Quaternion rotation)
    {
      if (!IsServerStarted && IsClientStarted)
      {
        Debug.LogWarning("[DefibrillatorCart] 시나리오 배치는 서버 권위 경로에서만 수행할 수 있습니다.", this);
        return;
      }

      _latchedSnapPoint = null;
      _pendingSnapPointIdentifier = null;
      SetAuthoritativeSnapPointIdentifier(string.Empty);
      SetAuthoritativeTransform(position, rotation);
    }

    private void TrySnapToSnapPoint()
    {
      if (!_enableSnap || (!IsServerStarted && IsClientStarted))
        return;

      // 단독 이동자의 입력으로 스냅이 발생했을 때만 해당 플레이어를 신호 발신자로 보존한다.
      using (IDisposable signalContext = TryGetSingleMovingParticipantConnection(out var mover)
               ? MI.Scenario.ScenarioSignalPlayerContext.Push(mover)
               : null)
      {
        TrySnapToSnapPointWithSignalContext();
      }
    }

    private void TrySnapToSnapPointWithSignalContext()
    {
      if (_latchedSnapPoint != null)
      {
        float releaseDistance = _latchedSnapPoint.SnapDistance + _snapReleasePadding;
        Vector3 offset = transform.position - _latchedSnapPoint.Position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= releaseDistance * releaseDistance)
          return;

        string previousPointIdentifier = _latchedSnapPoint.Identifier;
        _latchedSnapPoint = null;
        SetAuthoritativeSnapPointIdentifier(string.Empty);
        if (IsServerStarted)
          RpcApplyUnlatchedState();
        TriageTrainer.Scenario.TriageWorldInteractionSignals.RaiseDefibrillatorCartSnapPointUnlatched(Identifier, previousPointIdentifier);
      }

      DefibrillatorCartSnapPoint nearest = FindNearestSnapPoint();
      if (nearest == null)
        return;

      if (!TryResolveSnapPointCollision(nearest))
        return;

      _latchedSnapPoint = nearest;
      SetAuthoritativeTransform(nearest.Position, nearest.Rotation);
      ReleaseParticipantsAfterSnapIfConfigured(nearest);
      PublishAuthoritativeSnappedState(nearest);
      TriageTrainer.Scenario.TriageWorldInteractionSignals.RaiseDefibrillatorCartSnapPointLatched(Identifier, nearest.Identifier);
      PublishSnapPointReached(nearest);
    }

    /// <summary>
    /// 스냅 대상 위치를 점유 중인 다른 제세동 카트를 snap point 정책에 따라 처리한다.
    /// </summary>
    private bool TryResolveSnapPointCollision(DefibrillatorCartSnapPoint point)
    {
      DefibrillatorCartController[] carts = FindObjectsByType<DefibrillatorCartController>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);
      for (int i = 0; i < carts.Length; i++)
      {
        DefibrillatorCartController other = carts[i];
        if (other == null || other == this || !other.isActiveAndEnabled)
          continue;

        if (other._latchedSnapPoint != point && !point.IsWithinSnapDistance(other.transform.position))
          continue;

        if (point.BlockWhenDefibrillatorCartIsPresent)
          return false;

        if (point.DespawnExistingDefibrillatorCartWhenPresent)
        {
          DespawnBlockingDefibrillatorCart(other);
          continue;
        }

        if (point.BlockWhenAnyDefibrillatorCartIsPresent)
          return false;
      }

      return true;
    }

    private static void DespawnBlockingDefibrillatorCart(DefibrillatorCartController cart)
    {
      NetworkObject networkObject = cart.GetComponent<NetworkObject>();
      if (InstanceFinder.IsServerStarted && networkObject != null && networkObject.IsSpawned)
      {
        InstanceFinder.ServerManager.Despawn(networkObject);
        return;
      }

      UnityEngine.Object.Destroy(cart.gameObject);
    }

    private DefibrillatorCartSnapPoint FindNearestSnapPoint()
    {
      DefibrillatorCartSnapPoint[] points = FindObjectsByType<DefibrillatorCartSnapPoint>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);
      DefibrillatorCartSnapPoint nearest = null;
      float nearestDistanceSquared = float.MaxValue;

      for (int i = 0; i < points.Length; i++)
      {
        DefibrillatorCartSnapPoint point = points[i];
        if (point == null || !point.isActiveAndEnabled || !point.IsWithinSnapDistance(transform.position))
          continue;

        if (!IsSnapPointAllowed(point))
          continue;

        Vector3 offset = transform.position - point.Position;
        offset.y = 0f;
        float distanceSquared = offset.sqrMagnitude;
        if (distanceSquared < nearestDistanceSquared)
        {
          nearest = point;
          nearestDistanceSquared = distanceSquared;
        }
      }

      return nearest;
    }

    private bool IsSnapPointAllowed(DefibrillatorCartSnapPoint point)
    {
      if (point == null)
        return false;

      if (_allowedSnapPointIdentifiers == null || _allowedSnapPointIdentifiers.Count == 0)
        return true;

      string identifier = point.Identifier;
      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      for (int i = 0; i < _allowedSnapPointIdentifiers.Count; i++)
      {
        string allowed = _allowedSnapPointIdentifiers[i];
        if (string.IsNullOrWhiteSpace(allowed))
          continue;

        if (string.Equals(allowed.Trim(), identifier, StringComparison.Ordinal))
          return true;
      }

      return false;
    }

    private void ReleaseParticipantsAfterSnapIfConfigured(DefibrillatorCartSnapPoint point)
    {
      if (point != null && point.ReleaseParticipantsOnSnap)
        DetachAllParticipants();
    }

    private void PublishAuthoritativeSnappedState(DefibrillatorCartSnapPoint point)
    {
      if (point == null)
        return;

      SetAuthoritativeSnapPointIdentifier(point.Identifier);
      if (IsServerStarted)
      {
        // 카트 자체 RPC에서 원격 조종 상태와 스냅 참조를 먼저 적용한다.
        RpcApplySnappedState(
          point.Identifier,
          point.Position,
          point.Rotation,
          point.ReleaseParticipantsOnSnap);
      }
    }

    private void SetAuthoritativeSnapPointIdentifier(string identifier)
    {
      if (IsServerStarted)
        _snapPointIdentifierSync.Value = identifier ?? string.Empty;
    }

    [ObserversRpc]
    private void RpcApplySnappedState(
      string pointIdentifier,
      Vector3 position,
      Quaternion rotation,
      bool releaseParticipants)
    {
      if (IsServerStarted)
        return;

      transform.SetPositionAndRotation(position, rotation);
      if (releaseParticipants)
        ClearLocalParticipants();
      RequestSnapPointResolution(pointIdentifier);
    }

    [ObserversRpc]
    private void RpcApplyUnlatchedState()
    {
      if (!IsServerStarted)
        RequestSnapPointResolution(string.Empty);
    }

    private void OnSnapPointIdentifierChanged(string previous, string next, bool asServer)
    {
      RequestSnapPointResolution(next);
    }

    private void RequestSnapPointResolution(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
      {
        _pendingSnapPointIdentifier = null;
        _latchedSnapPoint = null;
        return;
      }

      _pendingSnapPointIdentifier = identifier.Trim();
      ResolveSyncedSnapPointIfPending();
    }

    private void ResolveSyncedSnapPointIfPending()
    {
      if (string.IsNullOrWhiteSpace(_pendingSnapPointIdentifier))
        return;

      var points = FindObjectsByType<DefibrillatorCartSnapPoint>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None);
      for (int i = 0; i < points.Length; i++)
      {
        DefibrillatorCartSnapPoint point = points[i];
        if (point == null || !string.Equals(
              point.Identifier,
              _pendingSnapPointIdentifier,
              StringComparison.Ordinal))
        {
          continue;
        }

        _latchedSnapPoint = point;
        _pendingSnapPointIdentifier = null;
        return;
      }
    }

    private void PublishSnapPointReached(DefibrillatorCartSnapPoint point)
    {
      if (point == null || string.IsNullOrWhiteSpace(point.Identifier))
      {
        if (point != null)
          Debug.LogWarning($"[DefibrillatorCart] Snap point '{point.name}' has no identifier; snap event was not published.", point);
        return;
      }

      string pointIdentifier = point.Identifier;
      string signalIdentifier = $"defibrillator_cart_snap_point_reached_{pointIdentifier}";
      MI.Scenario.ScenarioInteractionSignals.Raise(signalIdentifier);

      string moverIdentifier = Identifier;
      if (!string.IsNullOrWhiteSpace(moverIdentifier))
      {
        string scopedSignalIdentifier = $"defibrillator_cart_snap_point_reached_{moverIdentifier}_{pointIdentifier}";
        MI.Scenario.ScenarioInteractionSignals.Raise(scopedSignalIdentifier);
      }

      GameLogService.WriteInteraction(
        $"Defibrillator cart reached snap point: cart={Identifier}, point={pointIdentifier}",
        pointIdentifier);
    }

    /// <summary>
    /// 카트는 바닥 높이에서 이동한다. 카트 원점보다 위로 솟지 않는 바닥 콜라이더가
    /// 수평 레이를 타일 이음새 충돌로 오인하지 않도록 무시한다(침대와 동일한 정책).
    /// </summary>
    protected override bool ShouldIgnoreMovementBlocker(Collider collider)
    {
      if (collider == null)
        return false;

      return collider.bounds.max.y <= transform.position.y + 0.01f;
    }

    protected override bool ShouldLogMovementBlockers => true;

    protected override void OnValidate()
    {
      base.OnValidate();
      _snapReleasePadding = Mathf.Max(0f, _snapReleasePadding);

      if (_allowedSnapPointIdentifiers != null)
      {
        for (int i = _allowedSnapPointIdentifiers.Count - 1; i >= 0; i--)
        {
          string value = _allowedSnapPointIdentifiers[i];
          if (string.IsNullOrWhiteSpace(value))
          {
            _allowedSnapPointIdentifiers.RemoveAt(i);
            continue;
          }

          _allowedSnapPointIdentifiers[i] = value.Trim();
        }
      }
    }
  }
}
