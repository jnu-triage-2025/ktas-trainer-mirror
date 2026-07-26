using System;
using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity.IntravenousLine;
using UnityEngine;

using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  [RequireComponent(typeof(CapsuleCollider))]
  public partial class PatientController : NetworkBehaviour, IInteractable, IReposable, IItemUseTarget, IScenarioEntityInitTarget, IScenarioTriageAssessTarget, IScenarioIdentifiedEntity
  {
    private const string DefaultPatientCarryAttachPointName = "PatientCarryAttachPoint";

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

    [Header("Runtime")]
    [SerializeField] private PatientSupportExternalRefs _supportExternalRefs;
    [SerializeField] private IntravenousLineConnectionPoint _ivAttachmentPoint;
    [SerializeField] private Transform _carryAttachPoint;
    [SerializeField] private bool _isMovingPatientBedAttached;
    [SerializeField] private bool _isPlayerAttached;

    private readonly Dictionary<string, float> _lastNoticeByInteractor = new(StringComparer.Ordinal);

    private ChatUIController _chatUI;

    public string Identifier => EffectiveIdentifier;

    /// <summary>
    /// <see cref="IScenarioIdentifiedEntity"/> 구현: 시나리오 식별자를 노출한다(레지스트리 등록 식별자와 동일).
    /// 트리거 존 등이 진입 엔티티의 식별자를 도메인 비의존적으로 조회하는 데 사용한다.
    /// </summary>
    public string ScenarioEntityIdentifier => EffectiveIdentifier;

    public int Weight => Mathf.Max(0, _weight);

    public bool IsReposed => _supportExternalRefs.PatientBed != null;
    public MovingPatientBedController CurrentBed => _supportExternalRefs.PatientBed;
    public PatientSupportExternalRefs SupportExternalRefs => _supportExternalRefs;
    /// <summary>여러 수액 줄 연결을 허용하는 환자 IV attachment point.</summary>
    public IntravenousLineConnectionPoint IvAttachmentPoint => _ivAttachmentPoint;
    public Transform CarryAttachPoint => _carryAttachPoint != null ? _carryAttachPoint : transform;
    public bool IsMovingPatientBedAttached => _isMovingPatientBedAttached;
    public bool IsPlayerAttached => _isPlayerAttached;

    /// <summary>
    /// 환자가 플레이어에게 들린 상태가 아닐 때만 수행 가능한 정지 상태 사정/분류의 공통 게이트.
    /// </summary>
    public bool CanPerformTriageOrAssessment => !_isPlayerAttached;

    private void Awake()
    {
      Awake_Animation();
      EnsureCarryAttachPoint();
      EnsureIvAttachmentPoint();
      InitializeCollider();
      EnsureMedicalStateDefaults();
      _weight = Mathf.Max(0, _weight);
      BuildInteractEntries();
    }

    public void OnAttacked(MI.Entity.Entity attacker, int damageAmount)
    {
      TryAttachCurrentHandlingItem(attacker);
    }

    /// <summary>
    /// 아이템 사용 대상으로서의 처리(<see cref="IItemUseTarget"/>): 코드 하드코딩 매핑(<see cref="ApplyItemUse"/>)에 따라
    /// 처치 시각 표현을 켜고 시나리오 게이팅 신호를 올린다.
    /// </summary>
    public bool OnItemUsed(MI.Entity.Entity user, string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return false;

      return ApplyItemUse(itemIdentifier);
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

        return ApplyItemUse(itemIdentifier);
      }

      return false;
    }

    public void SetCurrentBed(MovingPatientBedController bed)
    {
      var previous = _supportExternalRefs.PatientBed;
      _supportExternalRefs.PatientBed = bed;
      OnMovingPatientBedAttachedStateChanged(_supportExternalRefs.PatientBed != null || _isMovingPatientBedAttached);
      Update_Animation();
      NotifyBedConnectionChanged(previous, bed);
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
      OnMovingPatientBedAttachedStateChanged(CurrentBed != null || _isMovingPatientBedAttached);
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

    private void EnsureIvAttachmentPoint()
    {
      if (_ivAttachmentPoint == null)
        _ivAttachmentPoint = GetComponentInChildren<IntravenousLineConnectionPoint>(true);

      if (_ivAttachmentPoint == null)
      {
        var pointObject = new GameObject("IVAttachmentPoint");
        pointObject.transform.SetParent(transform, false);
        pointObject.transform.localPosition = new Vector3(0f, 1.1f, 0.35f);
        pointObject.transform.localRotation = Quaternion.identity;
        _ivAttachmentPoint = pointObject.AddComponent<IntravenousLineConnectionPoint>();
      }

      _ivAttachmentPoint.SetAllowsMultipleConnections(true);
    }

    private void OnValidate()
    {
      OnValidate_Animation();
      EnsureCarryAttachPoint();
      InitializeCollider();
      EnsureMedicalStateDefaults();
      _weight = Mathf.Max(0, _weight);
      EnsureDefaultInteractConfigs();
    }
  }
}
