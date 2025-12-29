using System;
using UnityEngine;
using MultiplayerInfrastructure.InteractableEntity;
using FishNet.Object;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 상호작용 시 시나리오를 시작하는 컴포넌트입니다.
  /// NPC 또는 오브젝트에 부착합니다.
  /// </summary>
  public class ScenarioInteractable : NetworkBehaviour, IInteractable
  {
    #region Serialized Fields

    [Header("Display Settings")]
    [SerializeField] private string _displayText = "시나리오 시작";
    [SerializeField] private Sprite _displayIcon;
    [SerializeField] private Color _displayColor = Color.white;

    [Header("Scenario Settings")]
    [SerializeField] private TextAsset _scenarioJson;
    [SerializeField] private string _startNodeIdentifier;

    #endregion

    #region Private Fields

    private ScenarioGraph _cachedGraph;

    #endregion

    #region IInteractable

    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon;
    public Color DisplayColor => _displayColor;

    #endregion

    #region Events

    public static event Action<ScenarioGraph, string, int?> OnScenarioRequested;

    #endregion

    #region IInteractable.Interact

    public void Interact(Transform interactor)
    {
      var graph = GetScenarioGraph();

      if (graph == null)
      {
        Debug.LogError($"[ScenarioInteractable] {gameObject.name}: 유효한 시나리오 그래프가 없습니다.");
        return;
      }

      Debug.Log($"[ScenarioInteractable] {gameObject.name}: 시나리오 그래프를 시작합니다.");

      int? clientId = null;
      var interactorNetworkObject = interactor.GetComponent<NetworkObject>();
      if (interactorNetworkObject != null)
      {
        clientId = (int)interactorNetworkObject.Owner.ClientId;
      }

      OnScenarioRequested?.Invoke(graph, _startNodeIdentifier, clientId);
    }

    #endregion

    #region Private Methods

    private ScenarioGraph GetScenarioGraph()
    {
      if (_cachedGraph != null)
        return _cachedGraph;

      if (_scenarioJson == null)
        return null;

      try
      {
        _cachedGraph = ScenarioGraphLoader.LoadFromJson(_scenarioJson.text);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[ScenarioInteractable] Failed to load scenario: {ex.Message}");
        return null;
      }

      return _cachedGraph;
    }

    #endregion

    #region Public API

    public void SetScenario(TextAsset scenarioJson, string startNodeIdentifier = null)
    {
      _scenarioJson = scenarioJson;
      _startNodeIdentifier = startNodeIdentifier;
      _cachedGraph = null;
    }

    public void SetScenario(ScenarioGraph graph, string startNodeIdentifier = null)
    {
      _cachedGraph = graph;
      _startNodeIdentifier = startNodeIdentifier;
    }

    #endregion

#if UNITY_EDITOR
    private void Reset()
    {
      _displayText = "시나리오 시작";
      _displayColor = Color.white;
    }

    private void OnValidate()
    {
      _cachedGraph = null;
    }
#endif
  }
}