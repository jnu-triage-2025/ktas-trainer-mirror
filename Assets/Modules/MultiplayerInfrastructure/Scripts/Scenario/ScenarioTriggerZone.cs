using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 트리거 존 진입 시 자동으로 시나리오를 시작합니다.
  /// </summary>
  public class ScenarioTriggerZone : MonoBehaviour
  {
    #region Serialized Fields

    [Header("Scenario Settings")]
    [SerializeField] private TextAsset _scenarioJson;
    [SerializeField] private string _startNodeIdentifier;

    [Header("Trigger Settings")]
    [SerializeField] private bool _triggerOnce = true;
    [SerializeField] private float _triggerCooldown = 1f;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private bool _disableAfterTrigger = false;

    #endregion

    #region Private Fields

    private ScenarioGraph _cachedGraph;
    private bool _hasTriggered;
    private float _lastTriggerTime;

    #endregion

    #region Events

    public static event Action<ScenarioGraph, string> OnScenarioRequested;

    #endregion

    #region Properties

    public bool HasTriggered => _hasTriggered;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
      CacheScenarioGraph();
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
        return;

      if (_triggerOnce && _hasTriggered)
        return;

      if (Time.time - _lastTriggerTime < _triggerCooldown)
        return;

      if (_cachedGraph == null)
      {
        Debug.LogError("[ScenarioTriggerZone] Scenario graph is null");
        return;
      }

      ExecuteTrigger();
    }

    private void ExecuteTrigger()
    {
      _hasTriggered = true;
      _lastTriggerTime = Time.time;

      Debug.Log("[ScenarioTriggerZone] Triggering scenario");

      OnScenarioRequested?.Invoke(_cachedGraph, _startNodeIdentifier);

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

    public void ResetTrigger()
    {
      _hasTriggered = false;
      _lastTriggerTime = 0f;
      gameObject.SetActive(true);
    }

    public void ForceTrigger()
    {
      ExecuteTrigger();
    }

    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
      CacheScenarioGraph();
    }
#endif
  }
}