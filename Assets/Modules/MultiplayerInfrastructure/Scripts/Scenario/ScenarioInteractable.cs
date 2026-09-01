using System;
using FishNet.Object;
using MultiplayerInfrastructure.Commons;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 상호작용 시 시나리오를 시작하는 컴포넌트입니다.
  /// NPC 또는 오브젝트에 부착합니다.
  /// </summary>
  public class ScenarioInteractable : NetworkBehaviour, IInteractable, IInteract
  {
    #region Serialized Fields

    [Header("Identity")]
    [SerializeField] private string _identifier;

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
    private string _registeredIdentifier;

    #endregion

    #region IInteractable / IInteract

    public IInteract[] Interacts => new IInteract[] { this };

    public string DisplayText => _displayText;

    // 시나리오 실행을 fire하는 interactable의 기본 아이콘은 message-circle 입니다.
    // 인스펙터에서 _displayIcon 을 지정하면 그 값으로 덮어써집니다.
    public Sprite DisplayIcon =>
      _displayIcon != null
        ? _displayIcon
        : Registry.Registry.Get<Sprite>(RegistryType.IconSprite, IconSpriteIdentifiers.ScenarioDefault);

    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => _displayColor;
    public string Identifier => _identifier;

    #endregion

    private void Awake()
    {
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

    #region Events

    public static event Action<ScenarioGraph, string, int?> OnScenarioRequested;

    /// <summary>
    /// 정적 이벤트 구독을 초기화한다. 도메인 리로드가 비활성인 환경에서는 정적 이벤트가
    /// 플레이 세션 사이에 유지되어, 이전 세션에서 파괴된 컨트롤러의 핸들러가 남아 호출된다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
      OnScenarioRequested = null;
    }

    #endregion

    #region IInteract.Interact

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
      var interactorNetworkObject = interactor.GetComponentInParent<NetworkObject>();
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
    private new void Reset()
    {
      _displayText = "시나리오 시작";
      _displayColor = Color.white;
    }

    protected override void OnValidate()
    {
      base.OnValidate();
      _cachedGraph = null;
      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_identifier, gameObject, "scenario-interactable");
    }
#endif

    private void RegisterToRegistry()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        return;

      _registeredIdentifier = _identifier;
      Registry.Registry.RegisterEntity(_registeredIdentifier, EntityType.ScenarioInteractable, gameObject, displayName: gameObject.name, isNetworked: true);
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      Registry.Registry.UnregisterEntity(_registeredIdentifier);
      _registeredIdentifier = null;
    }
  }
}
