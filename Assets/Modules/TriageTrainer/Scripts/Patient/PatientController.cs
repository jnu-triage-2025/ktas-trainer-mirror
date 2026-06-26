using System;
using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  [RequireComponent(typeof(CapsuleCollider))]
  public partial class PatientController : NetworkBehaviour, IInteractable, IReposable, IItemUseTarget
  {
    private const string DefaultPatientCarryAttachPointName = "PatientCarryAttachPoint";

    [Serializable]
    public class AttachableItemVisualPair
    {
      [SerializeField] private string _itemIdentifier;
      [SerializeField] private GameObject _visualObject;

      [Tooltip("이 아이템을 환자에게 사용(부착)했을 때 올릴 시나리오 인터랙션 신호(sig.* 게이팅용, 옵션). 예: apply_gauze, apply_stabilizer_patient_a. 비워 두면 신호를 올리지 않는다.")]
      [SerializeField] private string _applySignal;

      public string ItemIdentifier => _itemIdentifier;
      public GameObject VisualObject => _visualObject;
      public string ApplySignal => _applySignal;
    }

    [Header("Identity")]
    [SerializeField] private string _identifier = "patient";

    [Header("Display")]
    [SerializeField] private string _liftDisplayText = "환자를 들어올리기";
    [SerializeField] private Sprite _liftDisplayIcon = null;
    [SerializeField] private string _carryDisplayText = "환자 들어올리기";
    [SerializeField] private Sprite _carryDisplayIcon = null;
    [SerializeField] private string _monitorSelectDisplayText = "이 환자를 모니터링";
    [SerializeField] private Sprite _monitorSelectDisplayIcon = null;

    [Header("Patient")]
    [SerializeField] private int _weight = 4;

    [Header("Attachable Item Visuals")]
    [SerializeField] private List<AttachableItemVisualPair> _attachableItemVisualPairs = new();

    [Header("Runtime")]
    [SerializeField] private MovingPatientBedController _currentBed;
    [SerializeField] private Transform _carryAttachPoint;
    [SerializeField] private bool _isMovingPatientBedAttached;
    [SerializeField] private bool _isPlayerAttached;

    private readonly Dictionary<string, GameObject> _attachableVisualMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _attachableApplySignalMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _lastNoticeByInteractor = new(StringComparer.Ordinal);

    private ChatUIController _chatUI;

    public string Identifier => EffectiveIdentifier;
    public int Weight => Mathf.Max(0, _weight);

    public bool IsReposed => _currentBed != null;
    public MovingPatientBedController CurrentBed => _currentBed;
    public Transform CarryAttachPoint => _carryAttachPoint != null ? _carryAttachPoint : transform;
    public bool IsMovingPatientBedAttached => _isMovingPatientBedAttached;
    public bool IsPlayerAttached => _isPlayerAttached;

    private void Awake()
    {
      Awake_Animation();
      EnsureCarryAttachPoint();
      InitializeCollider();
      EnsureMedicalStateDefaults();
      RebuildAttachableVisualMap();
      _weight = Mathf.Max(0, _weight);
      BuildInteractEntries();
    }

    public void OnAttacked(MI.Entity.Entity attacker, int damageAmount)
    {
      TryAttachCurrentHandlingItem(attacker);
    }

    public bool OnItemUsed(MI.Entity.Entity user, string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return false;

      return TryAttachItem(itemIdentifier);
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

      // 처치 적용 완료 시 시나리오 게이팅용 신호를 올린다(설정된 경우). 서버 권한 라우팅.
      if (_attachableApplySignalMap.TryGetValue(itemIdentifier, out var applySignal)
          && !string.IsNullOrWhiteSpace(applySignal))
      {
        MI.Scenario.ScenarioInteractionSignals.Raise(applySignal);
      }

      return true;
    }

    public void SetCurrentBed(MovingPatientBedController bed)
    {
      _currentBed = bed;
      OnMovingPatientBedAttachedStateChanged(_currentBed != null || _isMovingPatientBedAttached);
      Update_Animation();
    }

    public void OnMovingPatientBedAttachedEnter()
    {
      _isMovingPatientBedAttached = true;
      OnMovingPatientBedAttachedStateChanged(true);
      Update_Animation();
    }

    public void OnMovingPatientBedAttachedExit()
    {
      _isMovingPatientBedAttached = false;
      OnMovingPatientBedAttachedStateChanged(_currentBed != null || _isMovingPatientBedAttached);
      Update_Animation();
    }

    public void OnPlayerAttachedEnter()
    {
      _isPlayerAttached = true;
    }

    public void OnPlayerAttachedExit()
    {
      _isPlayerAttached = false;
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
      _attachableApplySignalMap.Clear();

      for (int i = 0; i < _attachableItemVisualPairs.Count; i++)
      {
        var pair = _attachableItemVisualPairs[i];
        if (pair == null || string.IsNullOrWhiteSpace(pair.ItemIdentifier) || pair.VisualObject == null)
          continue;

        _attachableVisualMap[pair.ItemIdentifier] = pair.VisualObject;

        if (!string.IsNullOrWhiteSpace(pair.ApplySignal))
          _attachableApplySignalMap[pair.ItemIdentifier] = pair.ApplySignal;
      }
    }

    private void EnsureCarryAttachPoint()
    {
      if (_carryAttachPoint != null)
        return;

      var existing = transform.Find(DefaultPatientCarryAttachPointName);
      if (existing != null)
      {
        _carryAttachPoint = existing;
        return;
      }

      var go = new GameObject(DefaultPatientCarryAttachPointName);
      _carryAttachPoint = go.transform;
      _carryAttachPoint.SetParent(transform, false);
      _carryAttachPoint.localPosition = new Vector3(0f, 1.0f, 0.2f);
      _carryAttachPoint.localRotation = Quaternion.identity;
    }

    private void OnValidate()
    {
      OnValidate_Animation();
      EnsureCarryAttachPoint();
      InitializeCollider();
      EnsureMedicalStateDefaults();
      _weight = Mathf.Max(0, _weight);
      RebuildAttachableVisualMap();
      EnsureDefaultInteractConfigs();
    }
  }
}
