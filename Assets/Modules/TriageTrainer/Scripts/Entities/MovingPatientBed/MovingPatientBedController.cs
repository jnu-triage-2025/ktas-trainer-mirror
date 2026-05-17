using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  public class MovingPatientBedController : Ridable, IInteractable, IInteract, IInteractorConditional
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

    public enum BedInteractionMode
    {
      Toggle = 0,
      Hold = 1
    }

    private sealed class RidingParticipant
    {
      public PlayerController Player;
      public Transform Interactor;
      public Transform AttachPoint;
      public float LastActionbarRefreshAt;
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
    [SerializeField] private BedInteractionMode _interactionMode = BedInteractionMode.Toggle;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _moveSpeed = 3.5f;
    [SerializeField, Min(0f)] private float _turnSpeed = 120f;
    [SerializeField] private float _forwardYawOffsetDegrees = -90f;
    [SerializeField] private LayerMask _movementBlockingMask = ~0;

    [Header("Attach Points")]
    [SerializeField] private List<Transform> PlayerAttachPoints = new();
    [SerializeField] private List<Transform> PatientAttachPoints = new();

    [Header("Attachable Item Visuals")]
    [SerializeField] private List<AttachableItemVisualPair> _attachableItemVisualPairs = new();

    [Header("Runtime")]
    [SerializeField] private MonoBehaviour _reposedTargetComponent;

    private readonly Dictionary<string, GameObject> _attachableVisualMap = new(StringComparer.Ordinal);
    private readonly Dictionary<int, RidingParticipant> _participants = new();
    private readonly Dictionary<string, float> _lastNoticeByInteractor = new(StringComparer.Ordinal);
    private readonly HashSet<string> _attachedItemIdentifiers = new(StringComparer.Ordinal);
    private readonly List<PlayerController> _playerAttachPointOccupants = new();
    private readonly List<MonoBehaviour> _patientAttachPointOccupants = new();

    private ChatUIController _chatUI;
    private TitleUIController _titleUI;
    private BedReposeInteract _reposeInteract;
    private IInteract[] _interacts;

    public string Identifier => string.IsNullOrWhiteSpace(_entityRuntimeIdentifier) ? _entityTypeIdentifier : _entityRuntimeIdentifier;
    public IInteract[] Interacts => _interacts ?? Array.Empty<IInteract>();
    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    public int Weight => Mathf.Max(0, _weight);
    public IReposable ReposedTarget => _reposedTargetComponent as IReposable;
    public int RequiredInteractorCount => Mathf.Max(Weight, ReposedTarget?.Weight ?? 0);

    private void Awake()
    {
      Awake_Ridable();
      _reposeInteract = new BedReposeInteract(this);
      _interacts = new IInteract[] { this, _reposeInteract };
      InitializeAttachPoints();
      RebuildAttachableVisualMap();
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
    public void SetIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      _entityRuntimeIdentifier = identifier;

      // Register in the global Registry as an Entity
      try
      {
        Registry.RegisterEntity(
          _entityRuntimeIdentifier,
          EntityType.MovingPatientBed,
          gameObject,
          displayName: _displayText,
          ownerUserIdentifier: null,
          clientId: null,
          isNetworked: false);
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[MovingPatientBed] Failed to register entity '{_entityRuntimeIdentifier}': {ex.Message}");
      }
    }

    private void OnDestroy()
    {
      foreach (var each in _participants)
      {
        var participant = each.Value;
        if (participant?.Player != null)
          ExitMovingMode(participant.Player, participant);
      }

      _participants.Clear();

      if (!string.IsNullOrWhiteSpace(_entityRuntimeIdentifier))
        Registry.UnregisterEntity(_entityRuntimeIdentifier);
    }

    private void Update()
    {
      if (_interactionMode == BedInteractionMode.Hold)
        CleanupReleasedHoldInteractors();

      CleanupExitKeyParticipants();

      if (_participants.Count == 0)
        return;

      RefreshOwnerActionbars();
      MoveBedFromParticipantsInput();
    }

    public void Interact(Transform interactor)
    {
      if (interactor == null)
        return;

      var player = interactor.GetComponentInParent<PlayerController>();
      if (player == null)
        return;

      int id = interactor.GetInstanceID();

      if (_participants.TryGetValue(id, out var existing))
      {
        ExitMovingMode(player, existing);
        _participants.Remove(id);
        return;
      }

      if (!TryOccupyNextPlayerAttachPoint(player, out Transform attachPoint))
      {
        ShowThrottledMessage(interactor, "침대의 모든 이동 위치가 이미 사용 중입니다.");
        return;
      }

      var participant = new RidingParticipant
      {
        Player = player,
        Interactor = interactor,
        AttachPoint = attachPoint,
        LastActionbarRefreshAt = -100f,
      };

      _participants[id] = participant;
      EnterMovingMode(participant);
    }

    public bool CanInteract(Transform interactor)
    {
      if (interactor == null)
        return false;

      var player = interactor.GetComponentInParent<PlayerController>();
      if (player != null && player.IsCarryingReposable)
        return false;

      return !_participants.ContainsKey(interactor.GetInstanceID());
    }

    public bool TryReposeTarget(IReposable target, Transform interactor = null)
    {
      if (target == null)
        return false;

      if (ReposedTarget != null)
        return false;

      if (target is not MonoBehaviour targetBehaviour)
        return false;

      if (PatientAttachPoints.Count > 0 && !TryOccupyNextPatientAttachPoint(targetBehaviour, out _))
        return false;

      if (interactor != null)
      {
        var player = interactor.GetComponentInParent<PlayerController>();
        if (player != null && player.IsCarryingReposable && ReferenceEquals(player.CarriedReposable, target))
          player.TryDropCarriedReposable(out _);
      }

      Transform patientAnchor = ResolvePatientAnchor(targetBehaviour);
      targetBehaviour.transform.SetParent(patientAnchor, false);
      targetBehaviour.transform.localPosition = Vector3.zero;
      targetBehaviour.transform.localRotation = Quaternion.identity;
      _reposedTargetComponent = targetBehaviour;
      target.OnMovingPatientBedAttachedEnter();

      if (targetBehaviour.TryGetComponent(out PatientController patient))
        patient.SetCurrentBed(this);

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

      var liftedBehaviour = _reposedTargetComponent;
      _reposedTargetComponent = null;
      ReleasePatientAttachPoint(liftedBehaviour);
      lifted.OnMovingPatientBedAttachedExit();

      if (liftedBehaviour != null)
        liftedBehaviour.transform.SetParent(null, true);

      if (!player.TryPickUpReposable(lifted))
      {
        if (liftedBehaviour != null)
        {
          liftedBehaviour.transform.SetParent(_reposeAnchor, false);
          liftedBehaviour.transform.localPosition = Vector3.zero;
          liftedBehaviour.transform.localRotation = Quaternion.identity;
          _reposedTargetComponent = liftedBehaviour;
          TryOccupyNextPatientAttachPoint(liftedBehaviour, out _);
          lifted.OnMovingPatientBedAttachedEnter();
        }
        return false;
      }

      if (liftedBehaviour != null && liftedBehaviour.TryGetComponent(out PatientController patient))
        patient.SetCurrentBed(null);

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

    private void EnterMovingMode(RidingParticipant participant)
    {
      if (participant?.Player == null)
        return;

      participant.Player.AlignYawTo(GetBedForwardDirection());
      participant.Player.SetForcedFollowAnchor(participant.AttachPoint);
      TryShowActionbar(participant);
      participant.Player.RefreshInteractableHintsNow();
    }

    private void ExitMovingMode(PlayerController player, RidingParticipant participant)
    {
      player?.ClearForcedFollowAnchor(participant?.AttachPoint);
      ReleasePlayerAttachPoint(player);
      player?.RefreshInteractableHintsNow();
    }

    private void InitializeAttachPoints()
    {
      EnsureDefaultPlayerAttachPoint();
      EnsureDefaultPatientAttachPoint();

      if (PlayerAttachPoints.Count == 0)
      {
        var playerAttachObjects = GetComponentsInChildren<RidableAttachPointObject>(true);
        for (int i = 0; i < playerAttachObjects.Length; i++)
        {
          var attach = playerAttachObjects[i];
          if (attach != null)
            PlayerAttachPoints.Add(attach.transform);
        }
      }

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

      _playerAttachPointOccupants.Clear();
      for (int i = 0; i < PlayerAttachPoints.Count; i++)
        _playerAttachPointOccupants.Add(null);

      _patientAttachPointOccupants.Clear();
      for (int i = 0; i < PatientAttachPoints.Count; i++)
        _patientAttachPointOccupants.Add(null);
    }

    private void EnsureDefaultPlayerAttachPoint()
    {
      if (PlayerAttachPoints.Count > 0)
        return;

      var existing = GetComponentInChildren<RidableAttachPointObject>(true);
      if (existing != null)
      {
        PlayerAttachPoints.Add(existing.transform);
        return;
      }

      var go = new GameObject(DefaultPlayerAttachPointName);
      var attach = go.AddComponent<RidableAttachPointObject>();
      var attachTransform = attach.transform;
      attachTransform.SetParent(transform, false);
      attachTransform.localPosition = new Vector3(0f, 0f, -0.8f);
      attachTransform.localRotation = Quaternion.identity;
      PlayerAttachPoints.Add(attachTransform);
    }

    private void EnsureDefaultPatientAttachPoint()
    {
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

    private bool TryOccupyNextPlayerAttachPoint(PlayerController player, out Transform attachPoint)
    {
      attachPoint = null;
      if (player == null)
        return false;

      for (int i = 0; i < _playerAttachPointOccupants.Count; i++)
      {
        if (_playerAttachPointOccupants[i] != null)
          continue;

        _playerAttachPointOccupants[i] = player;
        attachPoint = PlayerAttachPoints[i] != null ? PlayerAttachPoints[i] : transform;
        return true;
      }

      return false;
    }

    private bool ReleasePlayerAttachPoint(PlayerController player)
    {
      if (player == null)
        return false;

      for (int i = 0; i < _playerAttachPointOccupants.Count; i++)
      {
        if (_playerAttachPointOccupants[i] != player)
          continue;

        _playerAttachPointOccupants[i] = null;
        return true;
      }

      return false;
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

    private void RefreshOwnerActionbars()
    {
      // Intentionally no-op. Actionbar hint is shown once on mode entry.
    }

    private void TryShowActionbar(RidingParticipant participant)
    {
      if (participant?.Player == null || !participant.Player.IsOwner)
        return;

      if (_titleUI == null)
        _titleUI = Registry.Get<TitleUIController>(RegistryType.UI, Registry.TypeKey<TitleUIController>());

      if (_titleUI == null)
        return;

      participant.LastActionbarRefreshAt = Time.time;
      _titleUI.ShowActionbar(ConstantString.HintExitPatientBedMovingMode);
    }

    private void MoveBedFromParticipantsInput()
    {
      int participantCount = 0;
      float summedForwardInput = 0f;
      float summedTurnInput = 0f;

      foreach (var each in _participants)
      {
        var participant = each.Value;
        var player = participant?.Player;
        if (player == null)
          continue;

        participantCount++;

        var localInput = player.CurrentMoveInputVector;
        summedForwardInput += localInput.z;
        summedTurnInput += localInput.x;
      }

      if (participantCount <= 0)
        return;

      int requiredWeight = RequiredInteractorCount;
      int divisor = requiredWeight > 0 ? Mathf.Max(participantCount, requiredWeight) : participantCount;
      divisor = Mathf.Max(1, divisor);

      float forwardRatio = Mathf.Clamp(summedForwardInput / divisor, -1f, 1f);
      float turnRatio = Mathf.Clamp(summedTurnInput / divisor, -1f, 1f);

      if (Mathf.Abs(turnRatio) > 0.0001f)
      {
        float yawDelta = turnRatio * _turnSpeed * Time.deltaTime;
        transform.Rotate(0f, yawDelta, 0f, Space.World);
      }

      if (Mathf.Abs(forwardRatio) <= 0.0001f)
        return;

      Vector3 bedForward = GetBedForwardDirection();
      Vector3 desiredMove = bedForward * (forwardRatio * _moveSpeed * Time.deltaTime);
      Vector3 target = transform.position + desiredMove;

      if (Physics.Linecast(transform.position, target, _movementBlockingMask, QueryTriggerInteraction.Ignore))
        return;

      transform.position = target;
    }

    private Vector3 GetBedForwardDirection()
    {
      Quaternion forwardBasis = Quaternion.Euler(0f, _forwardYawOffsetDegrees, 0f);
      Vector3 bedForward = (forwardBasis * transform.forward).normalized;
      bedForward.y = 0f;
      if (bedForward.sqrMagnitude <= 0.0001f)
        return transform.forward;

      return bedForward;
    }

    private void CleanupReleasedHoldInteractors()
    {
      var keys = ListPool<int>.Get();
      foreach (var each in _participants)
      {
        var participant = each.Value;
        var interactor = participant?.Interactor;
        if (interactor == null)
        {
          keys.Add(each.Key);
          continue;
        }

        var player = participant.Player;
        if (player != null && player.IsOwner && !Input.GetKey(MI.Definitions.DefaultsKeyConfiguration.InteractInteractableObject))
          keys.Add(each.Key);
      }

      for (int i = 0; i < keys.Count; i++)
      {
        int key = keys[i];
        if (!_participants.TryGetValue(key, out var participant))
          continue;

        ExitMovingMode(participant.Player, participant);
        _participants.Remove(key);
      }

      ListPool<int>.Release(keys);
    }

    private void CleanupExitKeyParticipants()
    {
      var keys = ListPool<int>.Get();
      foreach (var each in _participants)
      {
        var participant = each.Value;
        var player = participant?.Player;
        if (player == null || !player.IsOwner)
          continue;

        if (Input.GetKeyDown(KeyCode.LeftShift))
          keys.Add(each.Key);
      }

      for (int i = 0; i < keys.Count; i++)
      {
        int key = keys[i];
        if (!_participants.TryGetValue(key, out var participant))
          continue;

        ExitMovingMode(participant.Player, participant);
        _participants.Remove(key);
      }

      ListPool<int>.Release(keys);
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
      if (_reposeAnchor == null)
        _reposeAnchor = transform;
      RebuildAttachableVisualMap();

      for (int i = PlayerAttachPoints.Count - 1; i >= 0; i--)
      {
        if (PlayerAttachPoints[i] == null)
          PlayerAttachPoints.RemoveAt(i);
      }

      for (int i = PatientAttachPoints.Count - 1; i >= 0; i--)
      {
        if (PatientAttachPoints[i] == null)
          PatientAttachPoints.RemoveAt(i);
      }

      EnsureDefaultPlayerAttachPoint();
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

    private static class ListPool<T>
    {
      [ThreadStatic] private static List<T> _cache;

      public static List<T> Get()
      {
        var list = _cache;
        if (list == null)
          return new List<T>();

        _cache = null;
        return list;
      }

      public static void Release(List<T> list)
      {
        if (list == null)
          return;

        list.Clear();
        _cache = list;
      }
    }
  }
}
