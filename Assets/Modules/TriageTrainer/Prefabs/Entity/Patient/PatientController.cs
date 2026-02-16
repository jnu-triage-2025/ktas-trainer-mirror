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
  public class PatientController : MonoBehaviour, IInteractable, IInteract, IReposable
  {
    [Serializable]
    public class AttachableItemVisualPair
    {
      [SerializeField] private string _itemIdentifier;
      [SerializeField] private GameObject _visualObject;

      public string ItemIdentifier => _itemIdentifier;
      public GameObject VisualObject => _visualObject;
    }

    [Header("Identity")]
    [SerializeField] private string _identifier = "patient";

    [Header("Display")]
    [SerializeField] private string _displayText = "환자";
    [SerializeField] private Sprite _displayIcon = null;

    [Header("Patient")]
    [SerializeField] private int _weight = 4;

    [Header("Attachable Item Visuals")]
    [SerializeField] private List<AttachableItemVisualPair> _attachableItemVisualPairs = new();

    [Header("Runtime")]
    [SerializeField] private MovingPatientBedController _currentBed;

    private readonly Dictionary<string, GameObject> _attachableVisualMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _lastNoticeByInteractor = new(StringComparer.Ordinal);

    private ChatUIController _chatUI;

    public string Identifier => _identifier;
    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon;
    public Color DisplayColor => Color.white;
    public int Weight => Mathf.Max(0, _weight);

    public bool IsReposed => _currentBed != null;
    public MovingPatientBedController CurrentBed => _currentBed;

    private void Awake()
    {
      RebuildAttachableVisualMap();
      _weight = Mathf.Max(0, _weight);
    }

    public void Interact(Transform interactor)
    {
      if (_currentBed == null)
        return;

      if (interactor == null)
        return;

      var player = interactor.GetComponentInParent<PlayerController>();
      if (player == null)
        return;

      if (player.IsCarryingReposable)
      {
        ShowThrottledMessage(interactor, "이미 다른 대상을 들고 있어 환자를 들어올릴 수 없습니다.");
        return;
      }

      if (!_currentBed.TryLiftTarget(player, out _))
      {
        ShowThrottledMessage(interactor, "환자를 침대에서 들어올릴 수 없습니다.");
        return;
      }

      ShowThrottledMessage(interactor, "환자를 침대에서 들어올렸습니다.");
    }

    public void OnAttacked(MI.Entity.Entity attacker, int damageAmount)
    {
      TryAttachCurrentHandlingItem(attacker);
    }

    public void OnItemUsed(MI.Entity.Entity user, MI.Item.Item itemUsing)
    {
      if (itemUsing == null)
        return;

      TryAttachItem(itemUsing.ItemIdentifier);
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

        string itemIdentifier = each.HandlingItem?.identifier;
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
      return true;
    }

    public void SetCurrentBed(MovingPatientBedController bed)
    {
      _currentBed = bed;
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
        Debug.Log($"[Patient] {message}", this);
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
      RebuildAttachableVisualMap();
    }
  }
}
