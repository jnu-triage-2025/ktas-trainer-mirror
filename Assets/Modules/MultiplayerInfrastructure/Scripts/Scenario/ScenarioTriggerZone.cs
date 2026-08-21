using System;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 트리거 존 진입 시 자동으로 시나리오를 시작합니다.
  /// 신호 계열 존(그래프 미지정)은 재생 중인 시나리오가 있을 때만 감지/발신합니다
  /// (<see cref="ScenarioController.HasActiveScenario"/> 기준).
  /// </summary>
  public class ScenarioTriggerZone : MonoBehaviour
  {
    #region Serialized Fields

    [Header("Identity")]
    [SerializeField] private string _identifier;

    [Header("Scenario Settings")]
    [SerializeField] private TextAsset _scenarioJson;
    [SerializeField] private string _startNodeIdentifier;

    [Header("Trigger Settings")]
    [SerializeField] private bool _triggerOnce = true;
    [SerializeField] private float _triggerCooldown = 1f;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private bool _disableAfterTrigger = false;

    [Header("Interaction Signals (옵션)")]
    [Tooltip("플레이어가 존에 진입할 때 올릴 시나리오 인터랙션 신호(sig.* 게이팅용). 예: enter_triage_zone, arrive_triagearea. 비워 두면 신호를 올리지 않는다(기존 동작). 재생 중인 시나리오가 없으면 발신되지 않는다.")]
    [SerializeField] private string[] _raiseSignalsOnEnter = Array.Empty<string>();

    [Header("Per-Entity Signal (옵션)")]
    [Tooltip("진입한 '식별된 엔티티'(IScenarioIdentifiedEntity 구현, 예: 환자)마다 대상별 신호를 올릴 때 사용하는 템플릿. " +
             "'{id}' 는 진입 엔티티의 식별자로 치환된다. 예: 'enter_triage_zone_{id}' → enter_triage_zone_patient_b. " +
             "비워 두면 대상별 신호를 올리지 않는다. 이 경로는 _playerTag 필터와 무관하게 동작한다. " +
             "재생 중인 시나리오가 없으면 발신되지 않는다.")]
    [SerializeField] private string _perEntitySignalTemplate = string.Empty;

    [Tooltip("대상별 신호가 이미 발신된 엔티티에는 재발신하지 않는다(distinct 계측용, 기본 true). " +
             "SignalCounter 로 도착 인원 수를 셀 때 중복 발신을 방지한다.")]
    [SerializeField] private bool _perEntityRaiseOncePerEntity = true;

    [Tooltip("Only PlayerController entities may raise the per-entity signal.")]
    [SerializeField] private bool _perEntityPlayersOnly;

    [Header("Debug")]
    [SerializeField] private bool _debugTriggerLogs = false;

    #endregion

    #region Private Fields

    private ScenarioGraph _cachedGraph;
    private bool _hasTriggered;
    private float _lastTriggerTime = float.NegativeInfinity;
    private string _registeredIdentifier;

    // 대상별 신호를 이미 발신한 엔티티 식별자(중복 발신 방지).
    private readonly HashSet<string> _perEntityRaised = new(StringComparer.Ordinal);

    // 새 시나리오 실행마다 중복 방지 상태를 되돌리기 위해 활성 존을 추적한다.
    private static readonly List<ScenarioTriggerZone> LiveZones = new();

    #endregion

    #region Events

    public static event Action<ScenarioGraph, string, int?> OnScenarioRequested;

    #endregion

    #region Properties

    public bool HasTriggered => _hasTriggered;
    public string Identifier => _identifier;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
      CacheScenarioGraph();
      RegisterToRegistry();
    }

    private void OnEnable()
    {
      RegisterToRegistry();
      if (!LiveZones.Contains(this))
        LiveZones.Add(this);
    }

    private void OnDisable()
    {
      UnregisterFromRegistry();
      LiveZones.Remove(this);
    }

    private void OnDestroy()
    {
      UnregisterFromRegistry();
    }

    private void OnTriggerEnter(Collider other)
    {
      TryRaisePerEntitySignal(other.gameObject);
      TryTrigger(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
      TryRaisePerEntitySignal(other.gameObject);
      TryTrigger(other.gameObject);
    }

    #endregion

    #region Trigger Logic

    /// <summary>
    /// 현재 활성화된(재생 중인) 시나리오 그래프가 존재하는지 여부.
    /// ScenarioController 가 없는 씬에서는 false 로 간주한다.
    /// 재생 여부 판정은 노드 실행 상태 기반(IsActive)이 아니라 그래프 존재 기반이어야
    /// 클라이언트 표시 모드/즉시 진행 노드 체인 사이에서도 재생 중으로 올바르게 판정된다.
    /// </summary>
    private static bool IsScenarioPlaying =>
      ScenarioController.Instance != null && ScenarioController.Instance.HasActiveScenario;

    private void TryTrigger(GameObject other)
    {
      if (!other.CompareTag(_playerTag))
      {
        if (_debugTriggerLogs)
          Debug.Log($"[ScenarioTriggerZone] Ignored '{other.name}': tag '{other.tag}' != required '{_playerTag}'.", this);
        return;
      }

      bool hasGraph = _cachedGraph != null;

      // 신호 계열 존(그래프 미지정)은 시나리오가 재생 중일 때만 감지한다.
      // 재생 중이 아니면 올린 신호가 소비되지 않고(다음 시나리오 시작 시 모두 초기화되므로)
      // 진입을 무시한다. 쿨다운/1회 트리거 상태도 소비하지 않는다.
      // 시나리오 시작용 존(그래프 지정)은 재생 중이 아닐 때 유일하게 동작해야 하므로 게이팅 예외다.
      if (!hasGraph && !IsScenarioPlaying)
      {
        if (_debugTriggerLogs)
          Debug.Log("[ScenarioTriggerZone] Ignored trigger: no scenario is playing.", this);
        return;
      }

      if (_triggerOnce && _hasTriggered)
      {
        if (_debugTriggerLogs)
          Debug.Log("[ScenarioTriggerZone] Ignored trigger: triggerOnce is enabled and zone already fired.", this);
        return;
      }

      if (Time.time - _lastTriggerTime < _triggerCooldown)
      {
        if (_debugTriggerLogs)
          Debug.Log($"[ScenarioTriggerZone] Ignored trigger: cooldown active ({Time.time - _lastTriggerTime:F2}s < {_triggerCooldown:F2}s).", this);
        return;
      }

      // 신호 전용 존(시나리오 그래프 미지정)도 허용한다: 그래프가 있으면 시나리오를 시작하고,
      // 없더라도 진입 신호(_raiseSignalsOnEnter)만 올리는 게이트 트리거로 동작할 수 있다.
      bool hasSignals = _raiseSignalsOnEnter != null && _raiseSignalsOnEnter.Length > 0;

      if (!hasGraph && !hasSignals)
      {
        // 대상별 신호 전용 존(_perEntitySignalTemplate 만 설정)은 정상 구성이다.
        // 이 경로(플레이어 태그 필터 통과)에서는 할 일이 없으므로 조용히 반환한다.
        if (!string.IsNullOrWhiteSpace(_perEntitySignalTemplate))
          return;

        Debug.LogError("[ScenarioTriggerZone] Scenario graph is null and no enter-signals configured; nothing to trigger.", this);
        return;
      }

      ExecuteTrigger(other, hasGraph);
    }

    private void ExecuteTrigger(GameObject triggeringObject = null, bool startScenario = true)
    {
      _hasTriggered = true;
      _lastTriggerTime = Time.time;

      // 진입 신호를 먼저 올린다(게이트 통과용). 시나리오 시작 여부와 독립적으로 동작한다.
      RaiseEnterSignals();

      if (startScenario && _cachedGraph != null)
      {
        Debug.Log("[ScenarioTriggerZone] Triggering scenario");

        int? clientId = null;
        if (triggeringObject != null)
        {
          var netObj = triggeringObject.GetComponentInParent<NetworkObject>();
          // 서버 소유(Owner 미지정) 오브젝트는 ClientId 가 -1 이므로 소유자로 취급하지 않는다.
          if (netObj != null && netObj.Owner != null && netObj.Owner.IsValid)
          {
            clientId = (int)netObj.Owner.ClientId;
          }
        }

        OnScenarioRequested?.Invoke(_cachedGraph, _startNodeIdentifier, clientId);
      }

      if (_disableAfterTrigger)
      {
        gameObject.SetActive(false);
      }
    }

    private void RaiseEnterSignals()
    {
      if (_raiseSignalsOnEnter == null)
        return;

      foreach (var signal in _raiseSignalsOnEnter)
      {
        if (string.IsNullOrWhiteSpace(signal))
          continue;

        if (_debugTriggerLogs)
          Debug.Log($"[ScenarioTriggerZone] Raising enter signal '{signal}'.", this);

        ScenarioInteractionSignals.Raise(signal);
      }
    }

    /// <summary>
    /// 진입한 오브젝트가 <see cref="IScenarioIdentifiedEntity"/> 를 구현하면, 대상별 신호 템플릿의
    /// "{id}" 를 그 식별자로 치환해 신호를 올린다(예: 환자별 트리아지 구역 도착).
    /// _playerTag 필터와 무관하게 동작하며, 기본적으로 엔티티당 1회만 발신한다.
    /// 대상별 신호는 재생 중인 시나리오의 게이트/카운터에서만 소비되므로, 재생 중이 아니면 발신하지 않는다.
    /// </summary>
    private void TryRaisePerEntitySignal(GameObject other)
    {
      if (string.IsNullOrWhiteSpace(_perEntitySignalTemplate) || other == null)
        return;

      // 재생 중이 아니면 신호가 소비되지 않으므로 발신하지 않는다.
      // (_perEntityRaised 가 재생 전 진입으로 오염되는 것도 함께 방지된다.)
      if (!IsScenarioPlaying)
        return;

      var identified = other.GetComponentInParent<IScenarioIdentifiedEntity>();
      if (_perEntityPlayersOnly && identified is not Player.PlayerController)
        return;
      string id = identified?.ScenarioEntityIdentifier;
      if (string.IsNullOrWhiteSpace(id))
      {
        if (_debugTriggerLogs)
          Debug.Log($"[ScenarioTriggerZone] Per-entity signal skipped for '{other.name}': no IScenarioIdentifiedEntity identifier.", this);
        return;
      }

      if (_perEntityRaiseOncePerEntity && !_perEntityRaised.Add(id))
        return; // 이미 이 엔티티에 발신함

      string signal = _perEntitySignalTemplate.IndexOf("{id}", StringComparison.Ordinal) >= 0
        ? _perEntitySignalTemplate.Replace("{id}", id)
        : _perEntitySignalTemplate;

      if (_debugTriggerLogs)
        Debug.Log($"[ScenarioTriggerZone] Raising per-entity signal '{signal}' for entity '{id}'.", this);

      ScenarioInteractionSignals.Raise(signal);
    }

    #endregion

    #region Scenario Graph

    private void CacheScenarioGraph()
    {
      if (_scenarioJson == null)
        return;

      try
      {
        _cachedGraph = ScenarioGraphLoader.LoadFromJson(_scenarioJson.text);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[ScenarioTriggerZone] Failed to load scenario: {ex.Message}");
      }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      CacheScenarioGraph();
      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_identifier, gameObject, "scenario-trigger-zone");
    }
#endif

    private void RegisterToRegistry()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        return;

      _registeredIdentifier = _identifier;
      Registry.Registry.RegisterEntity(_registeredIdentifier, EntityType.ScenarioTriggerZone, gameObject, displayName: gameObject.name);
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      Registry.Registry.UnregisterEntity(_registeredIdentifier);
      _registeredIdentifier = null;
    }

    public void SetScenario(TextAsset scenarioJson, string startNodeIdentifier = null)
    {
      _scenarioJson = scenarioJson;
      _startNodeIdentifier = startNodeIdentifier;
      CacheScenarioGraph();
    }

    public void SetScenario(ScenarioGraph graph, string startNodeIdentifier = null)
    {
      _cachedGraph = graph;
      _startNodeIdentifier = startNodeIdentifier;
    }

    #endregion

    #region Public API

    /// <summary>
    /// 새 시나리오 실행을 위해 모든 활성 존의 중복 방지 상태를 되돌린다.
    ///
    /// <para>
    /// 존의 발신 억제 상태는 한 번의 시나리오 실행 안에서만 의미가 있다. 시나리오가 다시
    /// 시작되면 신호 레지스트리는 비워지지만 존의 상태는 그대로 남아, 같은 엔티티나 플레이어가
    /// 다시 진입해도 신호를 올리지 않는다. 그 신호를 기다리는 게이트와 카운터는 영구히 막힌다.
    /// </para>
    /// </summary>
    public static void ResetAllForNewScenarioRun()
    {
      for (int index = LiveZones.Count - 1; index >= 0; index--)
      {
        var zone = LiveZones[index];
        // 파괴된 존은 목록에서 정리한다(UnityEngine.Object 의 == 오버로드를 사용한다).
        if (zone == null)
        {
          LiveZones.RemoveAt(index);
          continue;
        }

        zone.ResetForNewScenarioRun();
      }
    }

    private void ResetForNewScenarioRun()
    {
      _perEntityRaised.Clear();

      // 시나리오를 시작시키는 존(그래프 지정)은 방금 자신이 띄운 시나리오를 다시 시작시키면
      // 안 되므로 1회 트리거 상태를 유지한다. 신호 전용 존은 실행마다 다시 발신해야 한다.
      if (_cachedGraph != null)
        return;

      _hasTriggered = false;
      _lastTriggerTime = float.NegativeInfinity;
    }

    [ContextMenu("Scenario Trigger/Reset Trigger")]
    public void ResetTrigger()
    {
      _hasTriggered = false;
      _lastTriggerTime = float.NegativeInfinity;
      _perEntityRaised.Clear();
      gameObject.SetActive(true);
    }

    [ContextMenu("Scenario Trigger/Force Trigger")]
    public void ForceTrigger()
    {
      ExecuteTrigger();
    }

    #endregion
  }
}
