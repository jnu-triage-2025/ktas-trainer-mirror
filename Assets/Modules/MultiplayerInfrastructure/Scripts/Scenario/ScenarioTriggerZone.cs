using System;
using UnityEngine;
using FishNet.Object;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 트리거 존 진입 시 자동으로 시나리오를 시작합니다.
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
    [Tooltip("플레이어가 존에 진입할 때 올릴 시나리오 인터랙션 신호(sig.* 게이팅용). 예: enter_triage_zone, arrive_triagearea. 비워 두면 신호를 올리지 않는다(기존 동작).")]
    [SerializeField] private string[] _raiseSignalsOnEnter = Array.Empty<string>();

    [Header("Debug")]
    [SerializeField] private bool _debugTriggerLogs = false;

    #endregion

    #region Private Fields

    private ScenarioGraph _cachedGraph;
    private bool _hasTriggered;
    private float _lastTriggerTime;
    private string _registeredIdentifier;

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
    }

    private void OnDisable()
    {
      UnregisterFromRegistry();
    }

    private void OnDestroy()
    {
      UnregisterFromRegistry();
    }

    private void OnTriggerEnter(Collider other)
    {
      TryTrigger(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
      TryTrigger(other.gameObject);
    }

    #endregion

    #region Trigger Logic

    private void TryTrigger(GameObject other)
    {
      if (!other.CompareTag(_playerTag))
      {
        if (_debugTriggerLogs)
          Debug.Log($"[ScenarioTriggerZone] Ignored '{other.name}': tag '{other.tag}' != required '{_playerTag}'.", this);
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
      bool hasGraph = _cachedGraph != null;
      bool hasSignals = _raiseSignalsOnEnter != null && _raiseSignalsOnEnter.Length > 0;

      if (!hasGraph && !hasSignals)
      {
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

    [ContextMenu("Scenario Trigger/Reset Trigger")]
    public void ResetTrigger()
    {
      _hasTriggered = false;
      _lastTriggerTime = 0f;
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
