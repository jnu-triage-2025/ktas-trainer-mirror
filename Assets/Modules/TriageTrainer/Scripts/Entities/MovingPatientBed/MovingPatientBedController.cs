using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  public class MovingPatientBedController : MonoBehaviour, IInteractable, IInteract
  {
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

    [Header("Identity")]
    [SerializeField] private string _entityTypeIdentifier = "moving_patient_bed";
    private string _entityRuntimeIdentifier;

    [Header("Display")]
    [SerializeField] private string _displayText = "이동식 환자 침대";
    [SerializeField] private Sprite _displayIcon = null;

    [Header("Bed")]
    [SerializeField] private int _weight = 0;
    [SerializeField] private Transform _reposeAnchor;
    [SerializeField] private BedInteractionMode _interactionMode = BedInteractionMode.Toggle;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _followSpeed = 3.5f;
    [SerializeField, Min(0f)] private float _followStopDistance = 1.2f;
    [SerializeField] private LayerMask _movementBlockingMask = ~0;

    [Header("Attachable Item Visuals")]
    [SerializeField] private List<AttachableItemVisualPair> _attachableItemVisualPairs = new();

    [Header("Runtime")]
    [SerializeField] private MonoBehaviour _reposedTargetComponent;

    private readonly Dictionary<string, GameObject> _attachableVisualMap = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Transform> _interactors = new();
    private readonly Dictionary<string, float> _lastNoticeByInteractor = new(StringComparer.Ordinal);
    private readonly HashSet<string> _attachedItemIdentifiers = new(StringComparer.Ordinal);

    private ChatUIController _chatUI;

    public string Identifier => string.IsNullOrWhiteSpace(_entityRuntimeIdentifier) ? _entityTypeIdentifier : _entityRuntimeIdentifier;
    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    public int Weight => Mathf.Max(0, _weight);
    public IReposable ReposedTarget => _reposedTargetComponent as IReposable;
    public int RequiredInteractorCount => Mathf.Max(Weight, ReposedTarget?.Weight ?? 0);

    private void Awake()
    {
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
      if (!string.IsNullOrWhiteSpace(_entityRuntimeIdentifier))
        Registry.UnregisterEntity(_entityRuntimeIdentifier);
    }

    private void Update()
    {
      if (_interactionMode == BedInteractionMode.Hold)
        CleanupReleasedHoldInteractors();

      if (_interactors.Count == 0)
        return;

      if (_interactors.Count < RequiredInteractorCount)
        return;

      Transform driver = ResolveDriver();
      if (driver == null)
        return;

      var delta = driver.position - transform.position;
      delta.y = 0f;

      if (delta.sqrMagnitude <= _followStopDistance * _followStopDistance)
        return;

      var step = Mathf.Min(delta.magnitude, _followSpeed * Time.deltaTime);
      if (step <= 0f)
        return;

      var move = delta.normalized * step;
      var target = transform.position + move;

      if (Physics.Linecast(transform.position, target, _movementBlockingMask, QueryTriggerInteraction.Ignore))
        return;

      transform.position = target;
      transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized), 8f * Time.deltaTime);
    }

    public void Interact(Transform interactor)
    {
      if (interactor == null)
        return;

      int id = interactor.GetInstanceID();

      if (_interactors.ContainsKey(id))
      {
        _interactors.Remove(id);
        return;
      }

      if (_interactors.Count + 1 < RequiredInteractorCount)
      {
        ShowThrottledMessage(interactor, $"이 침대는 최소 {RequiredInteractorCount}명이 함께 이동해야 합니다.");
        return;
      }

      _interactors[id] = interactor;
    }

    public bool TryReposeTarget(IReposable target, Transform interactor = null)
    {
      if (target == null)
        return false;

      if (ReposedTarget != null)
        return false;

      if (target is not MonoBehaviour targetBehaviour)
        return false;

      if (interactor != null)
      {
        var player = interactor.GetComponentInParent<PlayerController>();
        if (player != null && player.IsCarryingReposable && ReferenceEquals(player.CarriedReposable, target))
          player.TryDropCarriedReposable(out _);
      }

      targetBehaviour.transform.SetParent(_reposeAnchor, false);
      targetBehaviour.transform.localPosition = Vector3.zero;
      targetBehaviour.transform.localRotation = Quaternion.identity;
      _reposedTargetComponent = targetBehaviour;

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

    private Transform ResolveDriver()
    {
      foreach (var each in _interactors)
      {
        if (each.Value != null)
          return each.Value;
      }

      return null;
    }

    private void CleanupReleasedHoldInteractors()
    {
      var keys = ListPool<int>.Get();
      foreach (var each in _interactors)
      {
        var interactor = each.Value;
        if (interactor == null)
        {
          keys.Add(each.Key);
          continue;
        }

        var player = interactor.GetComponentInParent<PlayerController>();
        if (player != null && player.IsOwner && !Input.GetKey(MI.Definitions.DefaultsKeyConfiguration.InteractInteractableObject))
          keys.Add(each.Key);
      }

      for (int i = 0; i < keys.Count; i++)
        _interactors.Remove(keys[i]);

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
