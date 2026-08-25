using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.Commons;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Entity
{
  [DisallowMultipleComponent]
  public class Npc : Interactable, ISpawnedEntityIdentifierReceiver, IOverheadPresentationAnchorProvider
  {
    [Header("Npc")]
    [SerializeField] private NPCBaseModelSO _npcBaseModel;
    [SerializeField] private string _identifier;
    [Tooltip("카메라와 이 거리(m)보다 멀어지면 머리 위 이름표를 숨긴다. 0 이하이면 거리 제한 없이 항상 표시한다.")]
    [SerializeField, Min(0f)] private float _overheadNameMaxVisibleDistance = 12f;

    [Header("Scenario Interacts")]
    [SerializeField] private List<NPCScenarioInteractDefinition> _scenarioInteracts = new();

    [Header("Item Submission Interacts")]
    [Tooltip("이 NPC 에게 아이템을 제출하는 상호작용. 런타임에 ItemSubmissionInteractable 컴포넌트를 자동 생성해 부착한다.")]
    [SerializeField] private List<NPCSubmissionInteractDefinition> _submissionInteracts = new();

    [Header("Custom Interacts")]
    [SerializeField] private List<MonoBehaviour> _customInteractSources = new();

    public string Identifier => _identifier;
    public override string PresentationEntityIdentifier => _identifier;
    public Transform OverheadPresentationAnchor
    {
      get
      {
        EnsureScenarioOverheadNameAnchor();
        _scenarioOverheadNameAnchor.localPosition = new Vector3(0f, GetOverheadNameHeight(), 0f);
        return _scenarioOverheadNameAnchor;
      }
    }

    private readonly List<IInteract> _resolvedInteracts = new List<IInteract>();
    private readonly List<IInteract> _runtimeActorInteracts = new List<IInteract>();
    private bool _interactsDirty = true;

    // 자동 생성된 submission Interactable 컴포넌트. 재빌드 시 재생성하지 않도록 정의별로 캐시한다.
    private readonly List<ItemSubmissionInteractable> _generatedSubmissionInteractables = new List<ItemSubmissionInteractable>();
    private readonly List<ItemSubmissionInteractable> _generatedActorSubmissionInteractables = new List<ItemSubmissionInteractable>();
    private bool _submissionInteractablesBuilt;

    private bool _baseModelApplied;
    private string _registeredIdentifier;
    private Transform _scenarioOverheadNameAnchor;
    private bool _scenarioOverheadNameVisible;

    private void Awake()
    {
      EnsureBaseModelApplied();
      MarkInteractsDirty();

      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = gameObject.name;

      RegisterToRegistry();
    }

    private void OnEnable()
    {
      EnsureBaseModelApplied();
      MarkInteractsDirty();
      RegisterToRegistry();
      if (_scenarioOverheadNameVisible)
        ConfigureScenarioOverheadNameLabel(gameObject.name);
    }

    private void OnDestroy()
    {
      UnregisterFromRegistry();
      DestroyScenarioOverheadNameLabel();
    }

    private void OnDisable()
    {
      UnregisterFromRegistry();
      DestroyScenarioOverheadNameLabel();
    }

    private void RegisterToRegistry()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        return;

      _registeredIdentifier = _identifier;
      Registry.Registry.Register(RegistryType.Npc, _registeredIdentifier, gameObject);
      Registry.Registry.RegisterEntity(_registeredIdentifier, EntityType.Npc, gameObject, displayName: gameObject.name);
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      if (Registry.Registry.Get<GameObject>(RegistryType.Npc, _registeredIdentifier) == gameObject)
        Registry.Registry.Unregister(RegistryType.Npc, _registeredIdentifier);

      if (Registry.Registry.TryGetEntity(_registeredIdentifier, out var descriptor)
          && descriptor?.GameObject == gameObject)
        Registry.Registry.UnregisterEntity(_registeredIdentifier);
      _registeredIdentifier = null;
    }

    public override void Interact(Transform interactor)
    {
      var interacts = Interacts;
      if (interacts == null || interacts.Length == 0)
      {
        Debug.LogWarning($"[Npc] '{name}' has no interact options.", this);
        return;
      }

      interacts[0].Interact(interactor);
    }

    public override IInteract[] Interacts
    {
      get
      {
        if (_interactsDirty)
          RebuildInteracts();

        return _resolvedInteracts.ToArray();
      }
    }

    private void RebuildInteracts()
    {
      _resolvedInteracts.Clear();

      if (_scenarioInteracts != null)
      {
        for (int i = 0; i < _scenarioInteracts.Count; i++)
        {
          var each = _scenarioInteracts[i];
          if (each == null || !each.IsValid)
            continue;

          _resolvedInteracts.Add(new ScenarioNpcInteract(this, each));
        }
      }

      EnsureSubmissionInteractablesBuilt();
      for (int i = 0; i < _generatedSubmissionInteractables.Count; i++)
      {
        var submission = _generatedSubmissionInteractables[i];
        if (submission != null)
          _resolvedInteracts.Add(submission);
      }

      if (_customInteractSources != null)
      {
        for (int i = 0; i < _customInteractSources.Count; i++)
        {
          var source = _customInteractSources[i];
          if (source == null)
            continue;

          if (source is IInteract customInteract)
            _resolvedInteracts.Add(customInteract);
          else
            Debug.LogWarning($"[Npc] '{name}' custom interact source '{source.name}' does not implement IInteract.", source);
        }
      }

      for (int i = 0; i < _runtimeActorInteracts.Count; i++)
      {
        var runtimeInteract = _runtimeActorInteracts[i];
        if (runtimeInteract != null)
          _resolvedInteracts.Add(runtimeInteract);
      }

      _interactsDirty = false;
    }

    /// <summary>
    /// EntityPresetSpawn이 요청한 런타임 식별자를 NPC/Entity 레지스트리에 동일하게 적용한다.
    /// Instantiate의 Awake에서 프리팹 식별자로 먼저 등록된 경우에도 안전하게 재등록한다.
    /// </summary>
    public void ApplySpawnedEntityIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      UnregisterFromRegistry();
      _identifier = identifier.Trim();
      RegisterToRegistry();
    }

    /// <summary>시나리오 최상위 actingNpcs 정의를 이 NPC 인스턴스에 적용한다.</summary>
    public void ConfigureScenarioActingNpc(ScenarioActingNpcDefinition actingNpc)
    {
      if (actingNpc == null)
        return;

      if (!string.IsNullOrWhiteSpace(actingNpc.DisplayName))
        gameObject.name = actingNpc.DisplayName;

      SetScenarioDisplay(actingNpc.DisplayName, actingNpc.ShowOverheadName);

      if (!string.IsNullOrWhiteSpace(actingNpc.Identifier))
        ApplySpawnedEntityIdentifier(actingNpc.Identifier);

      EnsureSubmissionInteractablesBuilt();
      ClearRuntimeActorInteracts();
      _runtimeActorInteracts.Clear();
      if (actingNpc.Interactions == null)
      {
        MarkInteractsDirty();
        return;
      }

      for (int i = 0; i < actingNpc.Interactions.Count; i++)
      {
        var definition = actingNpc.Interactions[i];
        if (definition == null)
          continue;

        switch (definition.InteractionType)
        {
          case ScenarioActingNpcInteractionType.StartScenario:
            if (!string.IsNullOrWhiteSpace(definition.ScenarioIdentifier))
              _runtimeActorInteracts.Add(new ScenarioActingNpcStartInteract(this, definition));
            break;
          case ScenarioActingNpcInteractionType.ItemSubmission:
            BuildRuntimeSubmissionInteract(definition, i);
            break;
          case ScenarioActingNpcInteractionType.Signal:
            if (!string.IsNullOrWhiteSpace(definition.CompletionSignalIdentifier))
              _runtimeActorInteracts.Add(new ScenarioActingNpcSignalInteract(this, definition));
            break;
        }
      }

      MarkInteractsDirty();
    }

    // 트리아지 오버헤드 라벨(EntityOverheadLabelElement)과 같은 UI Toolkit 라벨을 이름표로 재사용한다.
    // 같은 엘리먼트를 쓰므로 텍스트 크기/외곽선/반투명 배경이 트리아지 표기와 동일하게 렌더링되고,
    // 색상 사각형(swatch)만 숨겨 텍스트만 표시한다.
    private void ConfigureScenarioOverheadNameLabel(string displayName)
    {
      if (string.IsNullOrWhiteSpace(displayName))
      {
        ResolveOverheadLabelUI()?.RemoveLabel(_scenarioOverheadNameAnchor, "npc-name");
        return;
      }

      var overheadAnchor = OverheadPresentationAnchor;
      // 이름표는 멀리서 화면을 어지럽히지 않도록 거리 제한을 둔다.
      // 퀘스트 마크는 길 안내 용도이므로 같은 앵커에 있어도 이 제한을 공유하지 않는다.
      ResolveOverheadLabelUI()?.SetLabel(
        overheadAnchor,
        "npc-name",
        0,
        new EntityOverheadLabelUIController.LabelContent(displayName.Trim(), Color.white),
        _overheadNameMaxVisibleDistance);
    }

    // 이름표를 띄울 머리 위 앵커. UI 컨트롤러가 이 위치를 화면에 투영해 라벨을 배치한다.
    private void EnsureScenarioOverheadNameAnchor()
    {
      if (_scenarioOverheadNameAnchor != null)
        return;

      var anchorObject = new GameObject("Scenario Overhead Name Anchor");
      anchorObject.transform.SetParent(transform, false);
      _scenarioOverheadNameAnchor = anchorObject.transform;
    }

    // ActiveInstance 가 없으면 씬 내 컴포넌트를 직접 탐색해 폴백으로 사용한다(PatientController 와 동일한 방식).
    // (씬에 EntityOverheadLabelUIController 가 배치되지 않은 경우 경고를 출력한다.)
    private static EntityOverheadLabelUIController _cachedOverheadLabelUI;

    private static EntityOverheadLabelUIController ResolveOverheadLabelUI()
    {
      var instance = EntityOverheadLabelUIController.ActiveInstance;
      if (instance != null)
        return instance;

      if (_cachedOverheadLabelUI != null)
        return _cachedOverheadLabelUI;

      _cachedOverheadLabelUI = UnityEngine.Object.FindFirstObjectByType<EntityOverheadLabelUIController>();
      if (_cachedOverheadLabelUI == null)
      {
        Debug.LogWarning("[Npc] EntityOverheadLabelUIController 를 씬에서 찾을 수 없어 머리 위 이름표를 표시하지 않습니다. " +
                         "씬에 EntityOverheadLabelUIController + UIDocument 컴포넌트를 배치하세요.");
      }
      return _cachedOverheadLabelUI;
    }

    /// <summary>시나리오 노드가 NPC의 표시명과 머리 위 이름표를 런타임에 갱신한다.</summary>
    public void SetScenarioDisplay(string displayName, bool? showOverheadName)
    {
      bool nameChanged = !string.IsNullOrWhiteSpace(displayName);
      if (nameChanged)
      {
        gameObject.name = displayName.Trim();
        Registry.Registry.UpdateEntityDisplayName(
          string.IsNullOrWhiteSpace(_registeredIdentifier) ? _identifier : _registeredIdentifier,
          gameObject.name);
      }

      if (showOverheadName.HasValue)
      {
        _scenarioOverheadNameVisible = showOverheadName.Value;
        ConfigureScenarioOverheadNameLabel(_scenarioOverheadNameVisible ? gameObject.name : null);
      }
      else if (nameChanged && _scenarioOverheadNameAnchor != null)
        ConfigureScenarioOverheadNameLabel(gameObject.name);
    }

    private float GetOverheadNameHeight()
    {
      var renderers = GetComponentsInChildren<Renderer>(true);
      bool hasBounds = false;
      float maxY = 0f;
      for (int i = 0; i < renderers.Length; i++)
      {
        var renderer = renderers[i];
        if (renderer == null)
          continue;

        if (!hasBounds || renderer.bounds.max.y > maxY)
        {
          maxY = renderer.bounds.max.y;
          hasBounds = true;
        }
      }

      if (!hasBounds)
        return 2f;

      // 라벨과 머리 사이 여유 간격은 UI 컨트롤러의 worldHeightOffset 이 담당하므로 머리 상단 높이만 반환한다.
      return transform.InverseTransformPoint(new Vector3(transform.position.x, maxY, transform.position.z)).y;
    }

    private void DestroyScenarioOverheadNameLabel()
    {
      // 파괴된 앵커는 컨트롤러 LateUpdate 의 stale 정리가 제거하지만, 명시적으로 먼저 해제한다.
      EntityOverheadLabelUIController.ActiveInstance?.RemoveLabels(_scenarioOverheadNameAnchor);

      if (_scenarioOverheadNameAnchor != null)
      {
        Destroy(_scenarioOverheadNameAnchor.gameObject);
        _scenarioOverheadNameAnchor = null;
      }
    }

    private void BuildRuntimeSubmissionInteract(ScenarioActingNpcInteractionDefinition source, int index)
    {
      if (source.RequiredItems == null || source.RequiredItems.Count == 0)
        return;

      var requiredItems = new List<ItemRequirement>();
      for (int i = 0; i < source.RequiredItems.Count; i++)
      {
        var item = source.RequiredItems[i];
        if (item == null || string.IsNullOrWhiteSpace(item.ItemIdentifier))
          continue;
        requiredItems.Add(new ItemRequirement(item.ItemIdentifier, item.Count));
      }
      if (requiredItems.Count == 0)
        return;

      var child = new GameObject($"{name}_ActorSubmission_{index}");
      child.transform.SetParent(transform, false);
      var interactable = child.AddComponent<ItemSubmissionInteractable>();
      interactable.Configure(
        source.Identifier,
        new ItemSubmissionDefinition
        {
          displayText = string.IsNullOrWhiteSpace(source.DisplayText) ? "제출하기" : source.DisplayText,
          title = string.IsNullOrWhiteSpace(source.Title) ? "아이템 제출" : source.Title,
          submitButtonText = string.IsNullOrWhiteSpace(source.SubmitButtonText) ? "제출" : source.SubmitButtonText,
          requiredItems = requiredItems,
          completionSignalIdentifier = source.CompletionSignalIdentifier,
          consumeOnce = source.ConsumeOnce
        },
        displayIcon: ResolveActorIcon(source.IconIdentifier),
        enabled: source.Enabled);
      _generatedSubmissionInteractables.Add(interactable);
      _generatedActorSubmissionInteractables.Add(interactable);
    }

    private void ClearRuntimeActorInteracts()
    {
      for (int i = _generatedActorSubmissionInteractables.Count - 1; i >= 0; i--)
      {
        var interactable = _generatedActorSubmissionInteractables[i];
        _generatedSubmissionInteractables.Remove(interactable);
        if (interactable != null)
          Destroy(interactable.gameObject);
      }
      _generatedActorSubmissionInteractables.Clear();
    }

    private static Sprite ResolveActorIcon(string identifier)
      => string.IsNullOrWhiteSpace(identifier)
        ? null
        : Registry.Registry.Get<Sprite>(RegistryType.IconSprite, identifier);

    private void MarkInteractsDirty()
    {
      _interactsDirty = true;
    }

    /// <summary>
    /// 인스펙터/SO 에 정의된 submission 상호작용을 실제 <see cref="ItemSubmissionInteractable"/> 컴포넌트로 한 번 생성한다.
    /// 각 정의마다 NPC 하위에 자식 GameObject 를 만들어 컴포넌트를 부착하고 정의로 구성한다.
    /// </summary>
    private void EnsureSubmissionInteractablesBuilt()
    {
      if (_submissionInteractablesBuilt)
        return;

      _submissionInteractablesBuilt = true;
      _generatedSubmissionInteractables.Clear();

      if (_submissionInteracts == null)
        return;

      for (int i = 0; i < _submissionInteracts.Count; i++)
      {
        var def = _submissionInteracts[i];
        if (def == null || !def.IsValid)
          continue;

        var child = new GameObject($"{name}_Submission_{i}");
        child.transform.SetParent(transform, worldPositionStays: false);

        var interactable = child.AddComponent<ItemSubmissionInteractable>();
        interactable.Configure(
          identifier: def.InteractableIdentifier,
          definition: def.ToSubmissionDefinition(),
          displayIcon: def.DisplayIcon,
          displayColor: def.DisplayColor,
          enabled: def.Enabled);

        _generatedSubmissionInteractables.Add(interactable);
      }
    }

    /// <summary>
    /// NPC 에 커스텀 Interactable 소스(<see cref="IInteract"/> 를 구현한 MonoBehaviour)를 런타임에 추가한다.
    /// 시나리오 그래프 노드(NPCControl)가 특정 시점에 상호작용을 부여할 때 사용한다.
    /// </summary>
    public bool AddCustomInteractSource(MonoBehaviour source)
    {
      if (source == null || source is not IInteract)
        return false;

      _customInteractSources ??= new List<MonoBehaviour>();
      if (_customInteractSources.Contains(source))
        return true;

      _customInteractSources.Add(source);
      MarkInteractsDirty();
      return true;
    }

    /// <summary>이전에 추가된 커스텀 Interactable 소스를 NPC 에서 제거한다.</summary>
    public bool RemoveCustomInteractSource(MonoBehaviour source)
    {
      if (source == null || _customInteractSources == null)
        return false;

      bool removed = _customInteractSources.Remove(source);
      if (removed)
        MarkInteractsDirty();

      return removed;
    }

    private bool TryStartScenarioInteract(NPCScenarioInteractDefinition interactDefinition, Transform interactor)
    {
      if (interactDefinition == null || !interactDefinition.IsValid)
        return false;

      string scenarioIdentifier = interactDefinition.ScenarioIdentifier;
      string startNodeIdentifier = interactDefinition.ScenarioStartNodeIdentifier;

      if (string.IsNullOrWhiteSpace(scenarioIdentifier))
        return false;

      if (ScenarioController.Instance == null)
      {
        Debug.LogWarning($"[Npc] '{name}' cannot start scenario: ScenarioController.Instance is null.", this);
        return false;
      }

      if (!Registry.Registry.TryGetScenarioGraph(scenarioIdentifier, out var graph, out string error))
      {
        Debug.LogWarning($"[Npc] '{name}' failed to load scenario '{scenarioIdentifier}': {error}", this);
        return false;
      }

      int? ownerClientId = null;
      var interactorNetworkObject = interactor != null ? interactor.GetComponentInParent<NetworkObject>() : null;
      if (interactorNetworkObject != null && interactorNetworkObject.Owner.IsValid)
        ownerClientId = interactorNetworkObject.Owner.ClientId;

      ScenarioController.Instance.StartScenario(graph, startNodeIdentifier, ownerClientId);
      return true;
    }

    [ContextMenu("NPC/Apply Base Model Now")]
    private void ApplyBaseModelFromContextMenu()
    {
      _baseModelApplied = false;
      EnsureBaseModelApplied(logResult: true);
    }

    private void EnsureBaseModelApplied(bool logResult = false)
    {
      if (_baseModelApplied)
        return;

      bool applied = ApplyBaseModel();
      _baseModelApplied = true;

      if (!logResult)
        return;

      if (_npcBaseModel == null)
      {
        Debug.LogWarning($"[Npc] '{name}' has no NPCBaseModelSO assigned.", this);
        return;
      }

      Debug.Log($"[Npc] '{name}' base model apply {(applied ? "succeeded" : "completed with fallback values")}: identifier='{_identifier}', scenarioInteractCount={_scenarioInteracts.Count}, customInteractCount={_customInteractSources.Count}", this);
    }

    private bool ApplyBaseModel()
    {
      if (_npcBaseModel == null)
        return false;

      bool changed = false;

      if (!string.IsNullOrWhiteSpace(_npcBaseModel.identifier) && _identifier != _npcBaseModel.identifier)
      {
        _identifier = _npcBaseModel.identifier;
        changed = true;
      }

      if (_npcBaseModel.scenarioInteracts != null && _npcBaseModel.scenarioInteracts.Count > 0)
      {
        _scenarioInteracts = new List<NPCScenarioInteractDefinition>();
        for (int i = 0; i < _npcBaseModel.scenarioInteracts.Count; i++)
        {
          var each = _npcBaseModel.scenarioInteracts[i];
          if (each == null)
            continue;
          _scenarioInteracts.Add(each.Clone());
        }
        changed = true;
      }

      if (_npcBaseModel.submissionInteracts != null && _npcBaseModel.submissionInteracts.Count > 0)
      {
        _submissionInteracts = new List<NPCSubmissionInteractDefinition>();
        for (int i = 0; i < _npcBaseModel.submissionInteracts.Count; i++)
        {
          var each = _npcBaseModel.submissionInteracts[i];
          if (each == null)
            continue;
          _submissionInteracts.Add(each.Clone());
        }
        // 정의가 교체되었으므로 이미 생성된 submission Interactable 이 있으면 재생성 대상으로 표시한다.
        _submissionInteractablesBuilt = false;
        changed = true;
      }

      if (changed)
        MarkInteractsDirty();

      return changed;
    }

    private sealed class ScenarioNpcInteract : IInteract, IQuestPresentationTarget
    {
      private readonly Npc _npc;
      private readonly NPCScenarioInteractDefinition _definition;

      public ScenarioNpcInteract(Npc npc, NPCScenarioInteractDefinition definition)
      {
        _npc = npc;
        _definition = definition;
      }

      public string DisplayText
      {
        get
        {
          if (string.IsNullOrWhiteSpace(_definition.DisplayText))
            return "시나리오 시작";

          return _definition.DisplayText;
        }
      }

      public Sprite DisplayIcon => _definition.DisplayIcon;
      public bool AllowDisplayIconFallback => _definition.AllowDisplayIconFallback;
      public Color DisplayColor => _definition.DisplayColor;
      public string PresentationEntityIdentifier => _npc.Identifier;
      public string InteractionIdentifier => _definition.ScenarioIdentifier;

      public void Interact(Transform interactor)
      {
        _npc.TryStartScenarioInteract(_definition, interactor);
      }
    }

    private sealed class ScenarioActingNpcStartInteract : IInteract, IQuestPresentationTarget
    {
      private readonly Npc _npc;
      private readonly ScenarioActingNpcInteractionDefinition _definition;

      public ScenarioActingNpcStartInteract(Npc npc, ScenarioActingNpcInteractionDefinition definition)
      {
        _npc = npc;
        _definition = definition;
      }

      public string DisplayText => string.IsNullOrWhiteSpace(_definition.DisplayText)
        ? "시나리오 시작"
        : _definition.DisplayText;
      public Sprite DisplayIcon => ResolveActorIcon(_definition.IconIdentifier)
        ?? Registry.Registry.Get<Sprite>(RegistryType.IconSprite, IconSpriteIdentifiers.ScenarioDefault);
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public string PresentationEntityIdentifier => _npc.Identifier;
      public string InteractionIdentifier => _definition.Identifier;

      public void Interact(Transform interactor)
      {
        if (ScenarioController.Instance == null)
        {
          Debug.LogWarning(
            $"[Npc] '{_npc.name}' failed to start actingNpc interaction scenario " +
            $"'{_definition.ScenarioIdentifier}': ScenarioController.Instance is null.", _npc);
          return;
        }
        if (!Registry.Registry.TryGetScenarioGraph(
              _definition.ScenarioIdentifier, out var graph, out var error))
        {
          Debug.LogWarning(
            $"[Npc] '{_npc.name}' failed to start actingNpc interaction scenario " +
            $"'{_definition.ScenarioIdentifier}': {error}", _npc);
          return;
        }

        int? ownerClientId = null;
        var networkObject = interactor != null ? interactor.GetComponentInParent<NetworkObject>() : null;
        if (networkObject != null && networkObject.Owner.IsValid)
          ownerClientId = networkObject.Owner.ClientId;
        ScenarioController.Instance.StartScenario(
          graph, _definition.ScenarioStartNodeIdentifier, ownerClientId);
      }
    }

    private sealed class ScenarioActingNpcSignalInteract : IInteract, IQuestPresentationTarget
    {
      private readonly Npc _npc;
      private readonly ScenarioActingNpcInteractionDefinition _definition;

      public ScenarioActingNpcSignalInteract(Npc npc, ScenarioActingNpcInteractionDefinition definition)
      {
        _npc = npc;
        _definition = definition;
      }

      public string DisplayText => string.IsNullOrWhiteSpace(_definition.DisplayText)
        ? "상호작용"
        : _definition.DisplayText;
      public Sprite DisplayIcon => ResolveActorIcon(_definition.IconIdentifier);
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public string PresentationEntityIdentifier => _npc.Identifier;
      public string InteractionIdentifier => _definition.Identifier;

      public void Interact(Transform interactor)
      {
        ScenarioInteractionSignals.Raise(_definition.CompletionSignalIdentifier);
      }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      _baseModelApplied = false;
      ApplyBaseModel();
      MarkInteractsDirty();

      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_identifier, gameObject, "npc");
    }
#endif
  }
}
