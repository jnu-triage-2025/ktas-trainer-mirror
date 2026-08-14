using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using TriageTrainer.Scenario;
using UnityEngine;

using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 제세동 카트의 1인 조종을 전담하는 컨트롤러.
  /// 탑승/점유/이동의 네트워크 구현은 도메인 독립 공통 모듈
  /// <see cref="MinecraftBoadLikeControl"/> 에 위임한다(<see cref="Level1RapidInfuserController"/> 와 동일한 상속 패턴).
  ///
  /// 카트가 허용된 positioning point(환자 옆)에 도달하면 서버 권위로 스냅하고 시나리오 신호
  /// patient_bed_position_reached_{카트 식별자}_{포인트 식별자} 를 발생시킨다.
  /// 이 신호 이름은 기존 시나리오 데이터(patient_a_critical*)와의 호환을 위해 유지한다.
  /// </summary>
  public sealed class DefibCartController : MinecraftBoadLikeControl,
    IInteractable, IInteract, IInteractorConditional, ISpawnedEntityIdentifierReceiver
  {
    [Header("Identity")]
    [SerializeField] private string _entityTypeIdentifier = "defib_cart_a";

    [Header("Display")]
    [SerializeField] private string _displayText = "제세동 카트 조종";
    [SerializeField] private Sprite _displayIcon;

    [Header("Positioning Snap")]
    [SerializeField] private bool _enablePositioningSnap = true;
    [SerializeField, Min(0f)] private float _positioningSnapReleasePadding = 0.2f;

    [Header("Positioning Point Filter")]
    [Tooltip("비어 있으면 모든 MovingPatientBedPositioningPoint를 대상으로 스냅합니다. 값이 있으면 해당 Identifier 목록만 스냅 대상으로 허용합니다.")]
    [SerializeField] private List<string> _allowedPositioningPointIdentifiers = new();

    /// <summary>런타임 엔티티 식별자(서버 권위 + 전 피어 복제). 비어 있으면 _entityTypeIdentifier를 사용한다.</summary>
    private readonly SyncVar<string> _runtimeIdentifierSync = new(string.Empty);
    private readonly SyncVar<string> _positioningPointIdentifierSync = new(string.Empty);

    private MovingPatientBedPositioningPoint _latchedPositioningPoint;
    private string _pendingPositioningPointIdentifier;
    private string _entityRuntimeIdentifier;

    private string EffectiveIdentifier =>
      !string.IsNullOrWhiteSpace(_runtimeIdentifierSync.Value) ? _runtimeIdentifierSync.Value
      : !string.IsNullOrWhiteSpace(_entityRuntimeIdentifier) ? _entityRuntimeIdentifier
      : _entityTypeIdentifier;

    public string Identifier => EffectiveIdentifier;
    public MovingPatientBedPositioningPoint LatchedPositioningPoint => _latchedPositioningPoint;

    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    private void Awake()
    {
      Awake_MinecraftBoadLikeControl();
      Configure(1); // 제세동 카트는 한 명만 조종한다.
    }

    private void OnDestroy()
    {
      UnregisterCartEntity();
    }

    private void Update()
    {
      ResolveSyncedPositioningPointIfPending();
      Update_MinecraftBoadLikeControl();
      TrySnapToPositioningPoint();
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      _runtimeIdentifierSync.OnChange += OnRuntimeIdentifierChanged;
      _positioningPointIdentifierSync.OnChange += OnPositioningPointIdentifierChanged;

      // 스폰 페이로드로 동기화된 식별자로 모든 피어에서 등록한다.
      RegisterCartEntity();
      RequestPositioningPointResolution(_positioningPointIdentifierSync.Value);
    }

    public override void OnStopClient()
    {
      _runtimeIdentifierSync.OnChange -= OnRuntimeIdentifierChanged;
      _positioningPointIdentifierSync.OnChange -= OnPositioningPointIdentifierChanged;
      _pendingPositioningPointIdentifier = null;
      _latchedPositioningPoint = null;
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
      _entityRuntimeIdentifier = trimmed;

      if (IsServerStarted)
        _runtimeIdentifierSync.Value = trimmed;

      RegisterCartEntity();
    }

    private void OnRuntimeIdentifierChanged(string previous, string next, bool asServer)
    {
      if (string.IsNullOrWhiteSpace(next))
        return;

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
          EntityType.DefibCart,
          gameObject,
          displayName: _displayText,
          ownerUserIdentifier: null,
          clientId: null,
          isNetworked: IsClientStarted || IsServerStarted);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[DefibCart] Failed to register entity '{id}': {ex.Message}");
      }
    }

    private void UnregisterCartEntity()
    {
      if (!string.IsNullOrWhiteSpace(_entityRuntimeIdentifier))
        Registry.UnregisterEntity(_entityRuntimeIdentifier);
    }

    // ── Positioning snap ─────────────────────────────────────────────────────

    private void TrySnapToPositioningPoint()
    {
      if (!_enablePositioningSnap || (!IsServerStarted && IsClientStarted))
        return;

      // 단독 이동자의 입력으로 스냅이 발생했을 때만 해당 플레이어를 신호 발신자로 보존한다.
      using (IDisposable signalContext = TryGetSingleMovingParticipantConnection(out var mover)
               ? MI.Scenario.ScenarioSignalPlayerContext.Push(mover)
               : null)
      {
        TrySnapToPositioningPointWithSignalContext();
      }
    }

    private void TrySnapToPositioningPointWithSignalContext()
    {
      if (_latchedPositioningPoint != null)
      {
        float releaseDistance = _latchedPositioningPoint.SnapDistance + _positioningSnapReleasePadding;
        Vector3 offset = transform.position - _latchedPositioningPoint.Position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= releaseDistance * releaseDistance)
          return;

        string previousPointIdentifier = _latchedPositioningPoint.Identifier;
        _latchedPositioningPoint = null;
        SetAuthoritativePositioningPointIdentifier(string.Empty);
        if (IsServerStarted)
          RpcApplyUnlatchedState();
        TriageWorldInteractionSignals.RaisePatientBedPositioningPointUnlatched(Identifier, previousPointIdentifier);
      }

      MovingPatientBedPositioningPoint nearest = FindNearestPositioningPoint();
      if (nearest == null)
        return;

      if (!TryResolvePositioningPointCollision(nearest))
        return;

      _latchedPositioningPoint = nearest;
      SetAuthoritativeTransform(nearest.Position, nearest.Rotation);
      ReleaseParticipantsAfterSnapIfConfigured(nearest);
      PublishAuthoritativeSnappedState(nearest);
      TriageWorldInteractionSignals.RaisePatientBedPositioningPointLatched(Identifier, nearest.Identifier);
      PublishPositioningPointReached(nearest);
    }

    /// <summary>
    /// 스냅 대상 위치를 점유 중인 다른 이동체를 검사한다.
    /// 환자가 결합된 침대는 포인트 정책에 따라 스냅을 차단하고, 빈 침대는 정책에 따라 제거한다.
    /// 다른 제세동 카트가 이미 점유 중이면 항상 차단한다.
    /// </summary>
    private bool TryResolvePositioningPointCollision(MovingPatientBedPositioningPoint point)
    {
      MovingPatientBedController[] beds = FindObjectsByType<MovingPatientBedController>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);

      for (int i = 0; i < beds.Length; i++)
      {
        MovingPatientBedController existing = beds[i];
        if (existing == null || !existing.isActiveAndEnabled || !IsOccupyingPositioningPoint(existing, point))
          continue;

        if (existing.ReposedTarget != null)
        {
          if (point.BlockWhenPatientBedIsPresent)
            return false;
          if (point.BlockWhenAnyBedIsPresent)
            return false;
          continue;
        }

        if (point.BlockWhenAnyBedIsPresent)
          return false;

        if (point.DespawnEmptyBedWhenPresent)
          DespawnBlockingEmptyBed(existing);
      }

      DefibCartController[] carts = FindObjectsByType<DefibCartController>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);
      for (int i = 0; i < carts.Length; i++)
      {
        DefibCartController other = carts[i];
        if (other == null || other == this || !other.isActiveAndEnabled)
          continue;

        if (other._latchedPositioningPoint == point || point.IsWithinSnapDistance(other.transform.position))
          return false;
      }

      return true;
    }

    private bool IsOccupyingPositioningPoint(MovingPatientBedController bed, MovingPatientBedPositioningPoint point)
    {
      return bed.LatchedPositioningPoint == point || point.IsWithinSnapDistance(bed.transform.position);
    }

    private void DespawnBlockingEmptyBed(MovingPatientBedController bed)
    {
      NetworkObject networkObject = bed.GetComponent<NetworkObject>();
      if (InstanceFinder.IsServerStarted && networkObject != null && networkObject.IsSpawned)
      {
        InstanceFinder.ServerManager.Despawn(networkObject);
        return;
      }

      Destroy(bed.gameObject);
    }

    private MovingPatientBedPositioningPoint FindNearestPositioningPoint()
    {
      MovingPatientBedPositioningPoint[] points = FindObjectsByType<MovingPatientBedPositioningPoint>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);
      MovingPatientBedPositioningPoint nearest = null;
      float nearestDistanceSquared = float.MaxValue;

      for (int i = 0; i < points.Length; i++)
      {
        MovingPatientBedPositioningPoint point = points[i];
        if (point == null || !point.isActiveAndEnabled || !point.IsWithinSnapDistance(transform.position))
          continue;

        if (!IsPositioningPointAllowed(point))
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

    private bool IsPositioningPointAllowed(MovingPatientBedPositioningPoint point)
    {
      if (point == null)
        return false;

      if (_allowedPositioningPointIdentifiers == null || _allowedPositioningPointIdentifiers.Count == 0)
        return true;

      string identifier = point.Identifier;
      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      for (int i = 0; i < _allowedPositioningPointIdentifiers.Count; i++)
      {
        string allowed = _allowedPositioningPointIdentifiers[i];
        if (string.IsNullOrWhiteSpace(allowed))
          continue;

        if (string.Equals(allowed.Trim(), identifier, StringComparison.Ordinal))
          return true;
      }

      return false;
    }

    private void ReleaseParticipantsAfterSnapIfConfigured(MovingPatientBedPositioningPoint point)
    {
      if (point != null && point.ReleaseParticipantsOnSnap)
        DetachAllParticipants();
    }

    private void PublishAuthoritativeSnappedState(MovingPatientBedPositioningPoint point)
    {
      if (point == null)
        return;

      SetAuthoritativePositioningPointIdentifier(point.Identifier);
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

    private void SetAuthoritativePositioningPointIdentifier(string identifier)
    {
      if (IsServerStarted)
        _positioningPointIdentifierSync.Value = identifier ?? string.Empty;
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
      RequestPositioningPointResolution(pointIdentifier);
    }

    [ObserversRpc]
    private void RpcApplyUnlatchedState()
    {
      if (!IsServerStarted)
        RequestPositioningPointResolution(string.Empty);
    }

    private void OnPositioningPointIdentifierChanged(string previous, string next, bool asServer)
    {
      RequestPositioningPointResolution(next);
    }

    private void RequestPositioningPointResolution(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
      {
        _pendingPositioningPointIdentifier = null;
        _latchedPositioningPoint = null;
        return;
      }

      _pendingPositioningPointIdentifier = identifier.Trim();
      ResolveSyncedPositioningPointIfPending();
    }

    private void ResolveSyncedPositioningPointIfPending()
    {
      if (string.IsNullOrWhiteSpace(_pendingPositioningPointIdentifier))
        return;

      var points = FindObjectsByType<MovingPatientBedPositioningPoint>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None);
      for (int i = 0; i < points.Length; i++)
      {
        MovingPatientBedPositioningPoint point = points[i];
        if (point == null || !string.Equals(
              point.Identifier,
              _pendingPositioningPointIdentifier,
              StringComparison.Ordinal))
        {
          continue;
        }

        _latchedPositioningPoint = point;
        _pendingPositioningPointIdentifier = null;
        return;
      }
    }

    private void PublishPositioningPointReached(MovingPatientBedPositioningPoint point)
    {
      if (point == null || string.IsNullOrWhiteSpace(point.Identifier))
      {
        if (point != null)
          Debug.LogWarning($"[DefibCart] Positioning point '{point.name}' has no identifier; snap event was not published.", point);
        return;
      }

      string pointIdentifier = point.Identifier;
      // 신호 이름은 기존 시나리오 데이터와의 호환을 위해 patient_bed_position_reached_* 규칙을 유지한다.
      string signalIdentifier = $"patient_bed_position_reached_{pointIdentifier}";
      MI.Scenario.ScenarioInteractionSignals.Raise(signalIdentifier);

      string moverIdentifier = Identifier;
      if (!string.IsNullOrWhiteSpace(moverIdentifier))
      {
        string scopedSignalIdentifier = $"patient_bed_position_reached_{moverIdentifier}_{pointIdentifier}";
        MI.Scenario.ScenarioInteractionSignals.Raise(scopedSignalIdentifier);
      }

      GameLogService.WriteInteraction(
        $"Defib cart reached positioning point: cart={Identifier}, point={pointIdentifier}",
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
      _positioningSnapReleasePadding = Mathf.Max(0f, _positioningSnapReleasePadding);

      if (_allowedPositioningPointIdentifiers != null)
      {
        for (int i = _allowedPositioningPointIdentifiers.Count - 1; i >= 0; i--)
        {
          string value = _allowedPositioningPointIdentifiers[i];
          if (string.IsNullOrWhiteSpace(value))
          {
            _allowedPositioningPointIdentifiers.RemoveAt(i);
            continue;
          }

          _allowedPositioningPointIdentifiers[i] = value.Trim();
        }
      }
    }
  }
}