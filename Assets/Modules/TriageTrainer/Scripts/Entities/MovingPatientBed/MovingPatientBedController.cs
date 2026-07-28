using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using MultiplayerInfrastructure.Logging;
using UnityEngine;

using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  public partial class MovingPatientBedController : MinecraftBoadLikeControl, IInteractable, IInteract, IInteractorConditional, ISpawnedEntityIdentifierReceiver, IEntityPresetParentLinkReceiver
  {
    private const string DefaultPlayerAttachPointName = "PlayerAttachPoint";
    private const string DefaultPatientAttachPointName = "PatientAttachPoint";

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
    [SerializeField] private string _displayText = "이동식 환자 침대";
    [SerializeField] private Sprite _displayIcon = null;
    [SerializeField] private string _reposeDisplayText = "환자 침대에 내려놓기";
    [SerializeField] private Sprite _reposeDisplayIcon = null;

    [Header("Bed")]
    [SerializeField] private int _weight = 0;
    [SerializeField] private Transform _reposeAnchor;
    [SerializeField] private bool _enablePatientRepose = true;

    [Header("Positioning Snap")]
    [SerializeField] private bool _enablePositioningSnap = true;
    [SerializeField, Min(0f)] private float _positioningSnapReleasePadding = 0.2f;

    [Header("Attach Points")]
    [SerializeField, Min(1)] private int _maximumPlayerParticipants = 2;
    [SerializeField] private List<Transform> PatientAttachPoints = new();

    [Header("Attachable Item Visuals")]
    [SerializeField] private List<AttachableItemVisualPair> _attachableItemVisualPairs = new();

    [Header("Runtime")]
    [SerializeField] private MonoBehaviour _reposedTargetComponent;

    private readonly Dictionary<string, GameObject> _attachableVisualMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _lastNoticeByInteractor = new(StringComparer.Ordinal);
    private readonly HashSet<string> _attachedItemIdentifiers = new(StringComparer.Ordinal);
    private readonly List<MonoBehaviour> _patientAttachPointOccupants = new();

    private ChatUIController _chatUI;
    private BedReposeInteract _reposeInteract;
    private IInteract[] _interacts;
    private MovingPatientBedPositioningPoint _latchedPositioningPoint;

    public string Identifier => EffectiveBedIdentifier;
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
    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    public int Weight => Mathf.Max(0, _weight);
    public IReposable ReposedTarget => _reposedTargetComponent as IReposable;
    public MovingPatientBedPositioningPoint LatchedPositioningPoint => _latchedPositioningPoint;
    public int RequiredInteractorCount => Mathf.Max(Weight, ReposedTarget?.Weight ?? 0);

    /// <summary>장비형 파생 구성에서 침대 조종 로직을 단일 사용자로 제한한다.</summary>
    public void SetMaximumPlayerParticipants(int count)
    {
      _maximumPlayerParticipants = Mathf.Max(1, count);
    }

    /// <summary>침대 이동만 재사용하는 장비가 환자 내려놓기 메뉴를 숨길 수 있게 한다.</summary>
    public void SetPatientReposeEnabled(bool enabled)
    {
      _enablePatientRepose = enabled;
    }

    private void Awake()
    {
      Awake_MinecraftBoadLikeControl();
      Configure(
        _maximumPlayerParticipants,
        ConstantString.HintExitPatientBedMovingMode);
      ParticipantAssigned += OnMinecraftBoadParticipantAssigned;
      _reposeInteract = new BedReposeInteract(this);
      _interacts = new IInteract[] { this, _reposeInteract };
      InitializeAttachPoints();
      RebuildAttachableVisualMap();
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
      SyncReposedTargetTransform();
      SetMinimumMovementDivisor(RequiredInteractorCount);
      Update_MinecraftBoadLikeControl();
      TrySnapToPositioningPoint();
    }

    private void TrySnapToPositioningPoint()
    {
      if (!_enablePositioningSnap || (!IsServerStarted && IsClientStarted))
        return;

      if (_latchedPositioningPoint != null)
      {
        float releaseDistance = _latchedPositioningPoint.SnapDistance + _positioningSnapReleasePadding;
        Vector3 offset = transform.position - _latchedPositioningPoint.Position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= releaseDistance * releaseDistance)
          return;

        _latchedPositioningPoint = null;
      }

      MovingPatientBedPositioningPoint nearest = FindNearestPositioningPoint();
      if (nearest == null)
        return;

      _latchedPositioningPoint = nearest;
      SetAuthoritativeTransform(nearest.Position, nearest.Rotation);
      PublishPositioningPointReached(nearest);
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

      var player = interactor.GetComponentInParent<PlayerController>();
      if (player != null && player.IsCarryingReposable)
        return false;

      return CanToggle(interactor);
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

    private void OnValidate()
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
