using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Scenario;
using UnityEngine;

using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  public partial class MovingPatientBedController : MinecraftBoatLikeControl, IInteractable, IInteract, IInteractorConditional, ISpawnedEntityIdentifierReceiver, IEntityPresetParentLinkReceiver, IQuestPresentationTarget
  {
    private const string DefaultPlayerAttachPointName = "PlayerAttachPoint";
    private const string DefaultPatientAttachPointName = "PatientAttachPoint";
    public const string InteractionIdentifierMoveBed = "move_bed";

    [Serializable]
    public class AttachableItemVisualPair
    {
      [SerializeField] private string _itemIdentifier;
      [SerializeField] private GameObject _visualObject;

      public string ItemIdentifier => _itemIdentifier;
      public GameObject VisualObject => _visualObject;
    }

    private sealed class BedReposeInteract : IInteract, IInteractorConditional
    {
      private readonly MovingPatientBedController _owner;

      public BedReposeInteract(MovingPatientBedController owner)
      {
        _owner = owner;
      }

      public string DisplayText => _owner._reposeDisplayText;
      public Sprite DisplayIcon => _owner._reposeDisplayIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
      {
        if (_owner == null || interactor == null)
          return false;

        if (_owner.ReposedTarget != null)
          return false;

        var player = interactor.GetComponentInParent<PlayerController>();
        if (player == null || !player.IsCarryingReposable)
          return false;

        return player.CarriedReposable != null;
      }

      public void Interact(Transform interactor)
      {
        if (_owner == null || interactor == null)
          return;

        var player = interactor.GetComponentInParent<PlayerController>();
        if (player == null || !player.IsCarryingReposable)
          return;

        if (_owner.TryReposeTarget(player.CarriedReposable, interactor))
          _owner.ShowThrottledMessage(interactor, "환자를 침대에 내려놓았습니다.");
        else
          _owner.ShowThrottledMessage(interactor, "환자를 침대에 내려놓을 수 없습니다.");

        player.RefreshInteractableHintsNow();
      }
    }

    [Header("Identity")]
    [SerializeField] private string _entityTypeIdentifier = "moving_patient_bed";
    private string _entityRuntimeIdentifier;

    [Header("Display")]
    [SerializeField] private string _displayText = "침대로 움직이기";
    [SerializeField] private Sprite _displayIcon = null;
    [SerializeField] private string _reposeDisplayText = "환자 침대에 내려놓기";
    [SerializeField] private Sprite _reposeDisplayIcon = null;

    [Header("Bed")]
    [SerializeField] private int _weight = 0;
    [SerializeField] private Transform _reposeAnchor;
    [SerializeField] private bool _enablePatientRepose = true;
    [SerializeField] private bool _enableMovementInteraction = true;
    [SerializeField] private string _dismountCompletionSignal = "all_players_dismounted_patient_a_bed";

    [Header("Positioning Snap")]
    [SerializeField] private bool _enablePositioningSnap = true;
    [SerializeField, Min(0f)] private float _positioningSnapReleasePadding = 0.2f;

    [Header("Attach Points")]
    [SerializeField] private List<Transform> PatientAttachPoints = new();

    [Header("Positioning Point Filter")]
    [Tooltip("비어 있으면 모든 MovingPatientBedPositioningPoint를 대상으로 스냅합니다. 값이 있으면 해당 Identifier 목록만 스냅 대상으로 허용합니다.")]
    [SerializeField] private List<string> _allowedPositioningPointIdentifiers = new();

    [Header("Attachable Item Visuals")]
    [SerializeField] private List<AttachableItemVisualPair> _attachableItemVisualPairs = new();

    [Header("Runtime")]
    [SerializeField] private MonoBehaviour _reposedTargetComponent;

    private readonly Dictionary<string, GameObject> _attachableVisualMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _lastNoticeByInteractor = new(StringComparer.Ordinal);
    private readonly HashSet<string> _attachedItemIdentifiers = new(StringComparer.Ordinal);
    private readonly HashSet<int> _dismountedClientIds = new();
    private readonly List<MonoBehaviour> _patientAttachPointOccupants = new();

    private ChatUIController _chatUI;
    private BedReposeInteract _reposeInteract;
    private IInteract[] _interacts;
    private MovingPatientBedPositioningPoint _latchedPositioningPoint;
    private readonly SyncVar<string> _positioningPointIdentifierSync = new(string.Empty);
    private string _pendingPositioningPointIdentifier;

    public string Identifier => EffectiveBedIdentifier;
    public string PresentationEntityIdentifier => Identifier;
    public string InteractionIdentifier => InteractionIdentifierMoveBed;
    public IInteract[] Interacts
    {
      get
      {
        var result = new List<IInteract>();
        if (_interacts != null)
        {
          for (int i = 0; i < _interacts.Length; i++)
          {
            var interact = _interacts[i];
            if (interact == null || (!_enablePatientRepose && ReferenceEquals(interact, _reposeInteract)))
              continue;
            result.Add(interact);
          }
        }
        var providers = GetComponents<IAdditionalInteractProvider>();
        foreach (var provider in providers)
        {
          if (provider?.AdditionalInteracts == null)
            continue;
          foreach (var interact in provider.AdditionalInteracts)
            if (interact != null)
              result.Add(interact);
        }
        AddIntravenousFluidInteracts(result);
        return result.ToArray();
      }
    }
    public string DisplayText => GetDisplayText();
    public Sprite DisplayIcon => _displayIcon != null ? _displayIcon : ResolvedDefaultControlIcon;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    public int Weight => Mathf.Max(0, _weight);
    public IReposable ReposedTarget => _reposedTargetComponent as IReposable;
    public MovingPatientBedPositioningPoint LatchedPositioningPoint => _latchedPositioningPoint;
    public int RequiredInteractorCount => Mathf.Max(Weight, ReposedTarget?.Weight ?? 0);
    public string DismountCompletionSignal => _dismountCompletionSignal?.Trim();

    protected override void OnServerParticipantEntered(int clientId, PlayerController player, int handle)
    {
      _dismountedClientIds.Remove(clientId);
      if (_dismountedClientIds.Count == 0)
        return;

      string signal = DismountCompletionSignal;
      if (string.IsNullOrWhiteSpace(signal))
        return;

      ScenarioInteractionSignals.Clear(signal);
    }

    public void ResetDismountCompletionTracking()
    {
      _dismountedClientIds.Clear();
    }

    protected override void OnServerParticipantExited(int clientId, PlayerController player, int handle)
    {
      if (clientId < 0)
        return;

      _dismountedClientIds.Add(clientId);

      // 설정된 최대 참가자 수에 도달한 시점에만 완료 신호를 발행한다.
      if (_dismountedClientIds.Count < Capacity)
        return;

      string signal = DismountCompletionSignal;
      if (string.IsNullOrWhiteSpace(signal))
        return;

      RaiseDismountCompletionSignal();
    }

    private void RaiseDismountCompletionSignal()
    {
      string signal = DismountCompletionSignal;
      if (!string.IsNullOrWhiteSpace(signal))
        ScenarioInteractionSignals.Raise(signal);
    }

    /// <summary>침대 이동만 재사용하는 장비가 환자 내려놓기 메뉴를 숨길 수 있게 한다.</summary>
    public void SetPatientReposeEnabled(bool enabled)
    {
      _enablePatientRepose = enabled;
    }

    public void RefreshDisplayName()
    {
      if (string.IsNullOrWhiteSpace(Identifier))
        return;

      Registry.UpdateEntityDisplayName(Identifier, GetDisplayText());
    }

    private string GetDisplayText()
    {
      if (ReposedTarget is PatientController patient
          && patient.Descriptor != null
          && !string.IsNullOrWhiteSpace(patient.Descriptor.name))
      {
        return $"{patient.Descriptor.name.Trim()}의 침대 움직이기";
      }

      return "침대로 움직이기";
    }

    private void Awake()
    {
      Awake_MinecraftBoatLikeControl();
      Configure(
        4,
        ConstantString.HintExitPatientBedMovingMode);
      ParticipantAssigned += OnMinecraftBoadParticipantAssigned;
      _reposeInteract = new BedReposeInteract(this);
      _interacts = new IInteract[] { this, _reposeInteract };
      InitializeAttachPoints();
      RebuildAttachableVisualMap();
      HideAllAttachableVisuals();
      InitializeIntravenousAttachmentDisplay();
      if (_reposeAnchor == null)
        _reposeAnchor = transform;
      // Note: Entity identifier is assigned by server via SetIdentifier().
      // Do not generate UUID here; wait for server assignment.
    }

    /// <summary>
    /// Called by server/network system to assign a runtime entity identifier.
    /// Registers this bed in the global Registry if an identifier is provided.
    /// </summary>
    /// <param name="identifier">Server-assigned entity identifier (e.g., "moving_patient_bed:{uuid}"), or null to defer registration.</param>
    /// <summary>
    /// 엔티티 프리셋 스폰 시 식별자를 주입받는다(ISpawnedEntityIdentifierReceiver).
    /// 침대는 SetIdentifier 로 식별자 설정 + 레지스트리 등록이 이루어지므로 그대로 위임한다.
    /// </summary>
    public void ApplySpawnedEntityIdentifier(string identifier) => SetIdentifier(identifier);

    /// <summary>
    /// 런타임 엔티티 식별자를 설정한다. 서버에서 호출되면 SyncVar 로 전 피어에 복제되고,
    /// 모든 피어가 동일 식별자로 레지스트리에 등록한다(원격 클라에서도 식별자 기반 조회/결합이 동작하도록).
    /// 등록 자체는 SyncVar 경로(OnStartClient/OnChange)에서 수행한다.
    /// </summary>
    public void SetIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      string trimmed = identifier.Trim();
      _entityRuntimeIdentifier = trimmed; // 로컬 즉시 반영

      if (IsServerStarted)
      {
        _runtimeIdentifierSync.Value = trimmed;
      }

      // 서버에서 이미 스폰/등록된 뒤 재주입되는 경우를 위해 즉시 재등록도 시도.
      RegisterBedEntity();
    }

    private void OnDestroy()
    {
      ParticipantAssigned -= OnMinecraftBoadParticipantAssigned;

      UnregisterBedEntity();
    }

    private void Update()
    {
      ResolveSyncedPositioningPointIfPending();
      SyncReposedTargetTransform();
      SetMinimumMovementDivisor(RequiredInteractorCount);
      Update_MinecraftBoatLikeControl();
      TrySnapToPositioningPoint();
    }

    private void TrySnapToPositioningPoint()
    {
      if (!_enablePositioningSnap || (!IsServerStarted && IsClientStarted))
        return;

      // 단독 이동자의 입력으로 스냅이 발생했을 때만 해당 플레이어를 신호 발신자로 보존한다.
      // 협동 이동 및 외부 서버 보정은 행동 주체가 불명확하므로 서버 발신으로 기록한다.
      using (IDisposable signalContext = TryGetSingleMovingParticipantConnection(out var mover)
               ? MI.Scenario.ScenarioSignalPlayerContext.Push(mover)
               : null)
      {
        TrySnapToPositioningPointWithSignalContext();
      }
    }

    protected override bool ShouldIgnoreMovementBlocker(Collider collider)
    {
      if (collider == null)
        return false;

      // The bed moves at floor height. Floor colliders whose top does not rise above the
      // bed origin must not turn a horizontal ray into a collision at tile seams.
      if (collider.bounds.max.y <= transform.position.y + 0.01f)
        return true;

      return _reposedTargetComponent != null &&
             collider.transform.IsChildOf(_reposedTargetComponent.transform);
    }

    protected override bool ShouldLogMovementBlockers => true;

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

      if (!TryResolvePositioningPointBedCollision(nearest))
        return;

      _latchedPositioningPoint = nearest;
      SetAuthoritativeTransform(nearest.Position, nearest.Rotation);
      ReleaseParticipantsAfterSnapIfConfigured(nearest);
      PublishAuthoritativeSnappedState(nearest);
      TriageWorldInteractionSignals.RaisePatientBedPositioningPointLatched(Identifier, nearest.Identifier);
      PublishPositioningPointReached(nearest);
    }

    /// <summary>
    /// Resolves another bed occupying a positioning point before the incoming bed latches to it.
    /// A patient-bearing bed is protected by default; empty beds are removed by default so stale
    /// scenario beds do not prevent the next patient bed from reaching the point.
    /// </summary>
    private bool TryResolvePositioningPointBedCollision(MovingPatientBedPositioningPoint point)
    {
      MovingPatientBedController[] beds = FindObjectsByType<MovingPatientBedController>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);
      bool blockIncomingBed = false;

      for (int i = 0; i < beds.Length; i++)
      {
        MovingPatientBedController existing = beds[i];
        if (existing == null || existing == this || !existing.isActiveAndEnabled ||
            !existing.IsOccupyingPositioningPoint(point))
        {
          continue;
        }

        if (existing.ReposedTarget != null)
        {
          if (point.BlockWhenPatientBedIsPresent)
            return false;
          if (point.BlockWhenAnyBedIsPresent)
            blockIncomingBed = true;
          continue;
        }

        // This explicit opt-in takes precedence over replacing an empty bed.
        if (point.BlockWhenAnyBedIsPresent)
        {
          blockIncomingBed = true;
          continue;
        }

        if (point.DespawnEmptyBedWhenPresent)
        {
          existing.DespawnForPositioningPointReplacement();
          continue;
        }
      }

      return !blockIncomingBed;
    }

    private bool IsOccupyingPositioningPoint(MovingPatientBedPositioningPoint point)
    {
      return _latchedPositioningPoint == point || point.IsWithinSnapDistance(transform.position);
    }

    private void DespawnForPositioningPointReplacement()
    {
      NetworkObject networkObject = GetComponent<NetworkObject>();
      if (InstanceFinder.IsServerStarted && networkObject != null && networkObject.IsSpawned)
      {
        InstanceFinder.ServerManager.Despawn(networkObject);
        return;
      }

      Destroy(gameObject);
    }

    private void PublishPositioningPointReached(MovingPatientBedPositioningPoint point)
    {
      if (point == null || string.IsNullOrWhiteSpace(point.Identifier))
      {
        if (point != null)
          Debug.LogWarning($"[MovingPatientBed] Positioning point '{point.name}' has no identifier; snap event was not published.", point);
        return;
      }

      string pointIdentifier = point.Identifier;
      string signalIdentifier = $"patient_bed_position_reached_{pointIdentifier}";
      MI.Scenario.ScenarioInteractionSignals.Raise(signalIdentifier);

      string moverIdentifier = Identifier;
      if (!string.IsNullOrWhiteSpace(moverIdentifier))
      {
        string scopedSignalIdentifier = $"patient_bed_position_reached_{moverIdentifier}_{pointIdentifier}";
        MI.Scenario.ScenarioInteractionSignals.Raise(scopedSignalIdentifier);
      }

      GameLogService.WriteInteraction(
        $"Patient bed reached positioning point: bed={Identifier}, point={pointIdentifier}",
        pointIdentifier);
    }

    private MovingPatientBedPositioningPoint FindNearestPositioningPoint()
    {
      MovingPatientBedPositioningPoint[] points = FindObjectsByType<MovingPatientBedPositioningPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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

    /// <summary>
    /// 지정한 포지셔닝 포인트 식별자에 도달했을 때 즉시 스냅을 적용한다.
    /// 서버 권위에서만 동작하며, 성공 시 기존 자동 스냅과 동일한 신호를 발행한다.
    /// </summary>
    public bool TryForceSnapToPositioningPoint(string pointIdentifier)
      => TryForceSnapToPositioningPoint(pointIdentifier, teleportToPoint: false);

    /// <summary>
    /// 지정한 포지셔닝 포인트에 스냅한다.
    /// </summary>
    /// <param name="pointIdentifier">대상 포지셔닝 포인트 식별자.</param>
    /// <param name="teleportToPoint">
    /// true 면 현재 거리와 무관하게 침대를 포인트 위치로 옮긴 뒤 붙인다. 시나리오가 재생 위치를
    /// 건너뛰어 침대를 밀고 온 과정 자체가 없었던 경우에 쓴다.
    /// false 면 기존 동작대로 스냅 범위 안에 있을 때만 붙는다.
    /// </param>
    public bool TryForceSnapToPositioningPoint(string pointIdentifier, bool teleportToPoint)
    {
      if (!IsServerStarted || string.IsNullOrWhiteSpace(pointIdentifier))
        return false;

      string trimmed = pointIdentifier.Trim();
      var points = FindObjectsByType<MovingPatientBedPositioningPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      MovingPatientBedPositioningPoint point = null;
      for (int i = 0; i < points.Length; i++)
      {
        var candidate = points[i];
        if (candidate == null || !candidate.isActiveAndEnabled)
          continue;

        if (string.Equals(candidate.Identifier, trimmed, StringComparison.Ordinal))
        {
          point = candidate;
          break;
        }
      }

      if (point == null)
        return false;

      // 순간이동 요청이면 거리 게이트를 건너뛴다. 허용 목록과 충돌 정책은 그대로 지킨다.
      if (!teleportToPoint && !point.IsWithinSnapDistance(transform.position))
        return false;

      if (!IsPositioningPointAllowed(point))
        return false;

      if (!TryResolvePositioningPointBedCollision(point))
        return false;

      _latchedPositioningPoint = point;
      SetAuthoritativeTransform(point.Position, point.Rotation);
      ReleaseParticipantsAfterSnapIfConfigured(point);
      PublishAuthoritativeSnappedState(point);
      TriageWorldInteractionSignals.RaisePatientBedPositioningPointLatched(Identifier, point.Identifier);
      PublishPositioningPointReached(point);
      return true;
    }

    private void ReleaseParticipantsAfterSnapIfConfigured(MovingPatientBedPositioningPoint point)
    {
      if (point != null && point.ReleaseParticipantsOnSnap)
        ForceReleaseAllParticipants();
    }

    private void PublishAuthoritativeSnappedState(MovingPatientBedPositioningPoint point)
    {
      if (point == null)
        return;

      SetAuthoritativePositioningPointIdentifier(point.Identifier);
      if (IsServerStarted)
      {
        // 침대 자체 RPC에서 원격 조종 상태와 스냅 참조를 먼저 적용한다. 이후 별도
        // ScenarioNetworkRelay가 완료 신호를 보내더라도 클라이언트는 완성된 상태를 관찰한다.
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

    public void Interact(Transform interactor)
    {
      if (interactor == null)
        return;

      Toggle(interactor);
    }

    public bool CanInteract(Transform interactor)
    {
      if (!_enableMovementInteraction)
        return false;

      if (interactor == null)
        return false;

      var player = interactor.GetComponentInParent<PlayerController>();
      if (player != null && player.IsCarryingReposable)
        return false;

      return CanToggle(interactor);
    }

    /// <summary>
    /// 침대 이동 상호작용(손잡이 탑승/해제)을 런타임에서 활성/비활성화한다.
    /// 비활성화 시 현재 참가자를 함께 해제할 수 있다.
    /// </summary>
    public void SetMovementInteractionEnabled(bool enabled, bool releaseParticipantsIfDisabled = true)
    {
      _enableMovementInteraction = enabled;
      if (!enabled && releaseParticipantsIfDisabled)
        ForceReleaseAllParticipants();
    }

    /// <summary>
    /// 현재 침대를 잡고 있는 모든 플레이어의 조종 상태를 서버 권위로 강제 해제한다.
    /// 스냅 직후 같은 프레임에 호출하면 참가자 고정(anchor)과 조종 상태가 동시에 해제된다.
    /// </summary>
    public bool ForceReleaseAllParticipants()
    {
      bool detachedAny = false;
      if (!IsClientStarted && !IsServerStarted)
      {
        detachedAny = DetachAllParticipants();
      }
      else if (IsServerStarted)
      {
        // 전체 참가자 해제는 서버 내부 작업으로만 허용한다. 원격 클라이언트는
        // SyncList 변경 콜백을 통해 자신의 로컬 anchor/control 상태를 해제한다.
        detachedAny = DetachAllParticipants();
      }

      // 자동 스냅은 정원보다 적은 인원으로 이동한 경우에도 현재 참가자를 전부
      // 해제한 것이므로 완료다. Capacity 누적 조건과 별개로 완료 신호를 보장한다.
      if (detachedAny && !HasParticipants)
        RaiseDismountCompletionSignal();

      return detachedAny;
    }

    /// <summary>
    /// 특정 포지셔닝 포인트 식별자를 스냅 허용 목록에 보장한다.
    /// 허용 목록이 비어 있으면(=모든 포인트 허용) 아무 작업도 하지 않는다.
    /// </summary>
    public void EnsureAllowedPositioningPointIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      // 빈 목록은 "모든 포인트 허용" 의미이므로 추가할 필요가 없다.
      if (_allowedPositioningPointIdentifiers == null || _allowedPositioningPointIdentifiers.Count == 0)
        return;

      string trimmed = identifier.Trim();
      for (int i = 0; i < _allowedPositioningPointIdentifiers.Count; i++)
      {
        string existing = _allowedPositioningPointIdentifiers[i];
        if (!string.IsNullOrWhiteSpace(existing)
            && string.Equals(existing.Trim(), trimmed, StringComparison.Ordinal))
        {
          return;
        }
      }

      _allowedPositioningPointIdentifiers.Add(trimmed);
    }

    private void OnMinecraftBoadParticipantAssigned(int handle)
    {
      string patientIdentifier = _reposedTargetIdentifier.Value;
      if (!string.IsNullOrWhiteSpace(patientIdentifier))
        MI.Scenario.ScenarioInteractionSignals.Raise(
          $"grab_stretcher_{patientIdentifier}_handle_{handle}");
    }

    public bool TryReposeTarget(IReposable target, Transform interactor = null)
    {
      if (!_enablePatientRepose)
        return false;

      if (target == null)
        return false;

      if (ReposedTarget != null)
        return false;

      if (target is not MonoBehaviour targetBehaviour)
        return false;

      // 결합 권위는 서버의 SyncVar(_reposedTargetIdentifier)에 있다. 식별자로 환자를 가리켜야 하므로
      // PatientController(자가 등록 식별자 보유)만 결합 대상으로 허용한다.
      if (!targetBehaviour.TryGetComponent(out PatientController patient) || patient == null)
        return false;

      string patientIdentifier = patient.Identifier;
      if (string.IsNullOrWhiteSpace(patientIdentifier))
      {
        Debug.LogWarning("[MovingPatientBed] Repose target patient has no identifier yet; cannot establish networked repose link.");
        return false;
      }

      // 들고 있던 플레이어가 내려놓는 경우, 먼저 내려놓기 처리.
      if (interactor != null)
      {
        var player = interactor.GetComponentInParent<PlayerController>();
        if (player != null && player.IsCarryingReposable && ReferenceEquals(player.CarriedReposable, target))
          player.TryDropCarriedReposable(out _);
      }

      // 권위값 설정(서버) 또는 서버로 위임(클라). 실제 로컬 결합 적용은 SyncVar OnChange 가 모든 피어에서 수행한다.
      SetReposedTargetByIdentifier(patientIdentifier);
      return true;
    }

    public bool TryLiftTarget(PlayerController player, out IReposable lifted)
    {
      lifted = ReposedTarget;
      if (lifted == null)
        return false;

      if (player == null)
        return false;

      if (player.IsCarryingReposable)
      {
        ShowThrottledMessage(player.transform, "이미 다른 대상을 들고 있어 환자를 옮길 수 없습니다.");
        return false;
      }

      // 들어올림 시도: 먼저 플레이어가 실제로 들 수 있는지 확인한 뒤, 성공하면 권위값을 해제한다.
      if (!player.TryPickUpReposable(lifted))
      {
        return false;
      }

      // 권위 결합 해제(서버) 또는 서버로 위임(클라). 로컬 결합 해제는 OnChange 가 수행한다.
      SetReposedTargetByIdentifier(null);
      return true;
    }

    public void OnAttacked(MI.Entity.Entity attacker, int damageAmount)
    {
      TryAttachCurrentHandlingItem(attacker);
    }

    public void OnItemUsed(MI.Entity.Entity user, string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return;

      TryAttachItem(itemIdentifier);
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

        return TryAttachItem(itemIdentifier);
      }

      return false;
    }

    public bool TryAttachItem(string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return false;

      if (!_attachableVisualMap.TryGetValue(itemIdentifier, out var visual) || visual == null)
        return false;

      visual.SetActive(true);
      _attachedItemIdentifiers.Add(itemIdentifier);
      return true;
    }

    private void InitializeAttachPoints()
    {
      if (_enablePatientRepose)
      {
        EnsureDefaultPatientAttachPoint();

        if (PatientAttachPoints.Count == 0)
        {
          var patientAttachObjects = GetComponentsInChildren<MovingPatientBedPatientAttachPointObject>(true);
          for (int i = 0; i < patientAttachObjects.Length; i++)
          {
            var attach = patientAttachObjects[i];
            if (attach != null)
              PatientAttachPoints.Add(attach.transform);
          }
        }
      }
      else
      {
        PatientAttachPoints.Clear();
      }

      _patientAttachPointOccupants.Clear();
      for (int i = 0; i < PatientAttachPoints.Count; i++)
        _patientAttachPointOccupants.Add(null);
    }

    private void EnsureDefaultPatientAttachPoint()
    {
      if (!_enablePatientRepose)
        return;

      if (PatientAttachPoints.Count > 0)
        return;

      var existing = GetComponentInChildren<MovingPatientBedPatientAttachPointObject>(true);
      if (existing != null)
      {
        PatientAttachPoints.Add(existing.transform);
        return;
      }

      var go = new GameObject(DefaultPatientAttachPointName);
      var attach = go.AddComponent<MovingPatientBedPatientAttachPointObject>();
      var attachTransform = attach.transform;
      attachTransform.SetParent(transform, false);
      attachTransform.localPosition = new Vector3(0f, 0.9f, 0f);
      attachTransform.localRotation = Quaternion.identity;
      PatientAttachPoints.Add(attachTransform);
    }

    private bool TryOccupyNextPatientAttachPoint(MonoBehaviour patient, out Transform attachPoint)
    {
      attachPoint = null;
      if (patient == null)
        return false;

      for (int i = 0; i < _patientAttachPointOccupants.Count; i++)
      {
        if (_patientAttachPointOccupants[i] != null)
          continue;

        _patientAttachPointOccupants[i] = patient;
        attachPoint = PatientAttachPoints[i] != null ? PatientAttachPoints[i] : transform;
        return true;
      }

      return false;
    }

    private bool ReleasePatientAttachPoint(MonoBehaviour patient)
    {
      if (patient == null)
        return false;

      for (int i = 0; i < _patientAttachPointOccupants.Count; i++)
      {
        if (_patientAttachPointOccupants[i] != patient)
          continue;

        _patientAttachPointOccupants[i] = null;
        return true;
      }

      return false;
    }

    private Transform ResolvePatientAnchor(MonoBehaviour patient)
    {
      if (patient != null)
      {
        for (int i = 0; i < _patientAttachPointOccupants.Count; i++)
        {
          if (_patientAttachPointOccupants[i] != patient)
            continue;

          var anchor = i >= 0 && i < PatientAttachPoints.Count ? PatientAttachPoints[i] : null;
          if (anchor != null)
            return anchor;
          break;
        }
      }

      if (_reposeAnchor != null)
        return _reposeAnchor;

      return transform;
    }

    private void SyncReposedTargetTransform()
    {
      // 권위값(SyncVar)으로 지정됐으나 아직 환자 등록 전이라 보류 중인 결합이 있으면 매 프레임 재시도한다.
      ResolveDesiredReposeLinkIfPending();

      if (_reposedTargetComponent == null)
        return;

      SnapReposedTargetToAnchor(_reposedTargetComponent);
    }

    private void SnapReposedTargetToAnchor(MonoBehaviour patient)
    {
      if (patient == null)
        return;

      Transform patientAnchor = ResolvePatientAnchor(patient);
      if (patientAnchor == null)
        return;

      Vector3 worldPosition = patientAnchor.position;
      Quaternion worldRotation = patientAnchor.rotation;

      if (patient.TryGetComponent<PatientController>(out var patientController)
          && patientController != null
          && patientController.TryGetLayingOnMovingBedOffsets(out var localOffset, out var localRotationOffset))
      {
        worldPosition += patientAnchor.TransformVector(localOffset);
        worldRotation = patientAnchor.rotation * localRotationOffset;
      }

      patient.transform.SetPositionAndRotation(worldPosition, worldRotation);
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
        Debug.Log($"[MovingPatientBed] {message}", this);
    }

    private void RebuildAttachableVisualMap()
    {
      _attachableVisualMap.Clear();
      for (int i = 0; i < _attachableItemVisualPairs.Count; i++)
      {
        var pair = _attachableItemVisualPairs[i];
        if (pair == null || string.IsNullOrWhiteSpace(pair.ItemIdentifier) || pair.VisualObject == null)
          continue;

        _attachableVisualMap[pair.ItemIdentifier] = pair.VisualObject;
      }
    }

    /// <summary>
    /// 스폰 시 모든 부착 가능 아이템 시각 오브젝트를 비활성화합니다.
    /// 프리팹 편집 편의를 위해 자식이 활성화된 채 저장되어 있어도, 런타임에서는
    /// <see cref="TryAttachItem"/> 호출에 의해 명시적으로 켜진 부착물만 보이도록 합니다.
    /// </summary>
    private void HideAllAttachableVisuals()
    {
      foreach (var visual in _attachableVisualMap.Values)
      {
        if (visual != null)
          visual.SetActive(false);
      }
    }

    protected override void OnValidate()
    {
      _weight = Mathf.Max(0, _weight);
      _positioningSnapReleasePadding = Mathf.Max(0f, _positioningSnapReleasePadding);
      if (_reposeAnchor == null)
        _reposeAnchor = transform;
      RebuildAttachableVisualMap();

      for (int i = PatientAttachPoints.Count - 1; i >= 0; i--)
      {
        if (PatientAttachPoints[i] == null)
          PatientAttachPoints.RemoveAt(i);
      }

      EnsureDefaultPatientAttachPoint();

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

    private void OnDrawGizmosSelected()
    {
      if (_reposeAnchor == null)
        return;

      Gizmos.color = Color.cyan;
      Gizmos.DrawWireSphere(_reposeAnchor.position, 0.2f);
      Gizmos.DrawLine(_reposeAnchor.position, _reposeAnchor.position + _reposeAnchor.up * 0.4f);
    }

  }
}
