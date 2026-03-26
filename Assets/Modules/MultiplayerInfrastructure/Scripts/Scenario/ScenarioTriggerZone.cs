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

      if (_cachedGraph == null)
      {
        Debug.LogError("[ScenarioTriggerZone] Scenario graph is null");
        return;
      }

      ExecuteTrigger(other);
    }

    private void ExecuteTrigger(GameObject triggeringObject = null)
    {
      _hasTriggered = true;
      _lastTriggerTime = Time.time;

      Debug.Log("[ScenarioTriggerZone] Triggering scenario");

      int? clientId = null;
      if (triggeringObject != null)
      {
        var netObj = triggeringObject.GetComponent<NetworkObject>();
        if (netObj != null)
        {
          clientId = (int)netObj.Owner.ClientId;
        }
      }

      OnScenarioRequested?.Invoke(_cachedGraph, _startNodeIdentifier, clientId);

      if (_disableAfterTrigger)
      {
        gameObject.SetActive(false);
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