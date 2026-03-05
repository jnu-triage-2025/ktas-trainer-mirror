using FishNet.Object;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace MultiplayerInfrastructure.Entity
{
  [DisallowMultipleComponent]
  public class Npc : Interactable
  {
    [Header("Npc")]
    [SerializeField] private NPCBaseModelSO _npcBaseModel;
    [SerializeField] private string _identifier;

    [Header("Scenario Interacts")]
    [SerializeField] private List<NPCScenarioInteractDefinition> _scenarioInteracts = new();

    [Header("Custom Interacts")]
    [SerializeField] private List<MonoBehaviour> _customInteractSources = new();

    public string Identifier => _identifier;

    private readonly List<IInteract> _resolvedInteracts = new List<IInteract>();
    private bool _interactsDirty = true;

    private bool _baseModelApplied;

    private void Awake()
    {
      EnsureBaseModelApplied();
      MarkInteractsDirty();

      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = gameObject.name;

      Registry.Registry.Register(RegistryType.Npc, _identifier, gameObject);
    }

    private void OnEnable()
    {
      EnsureBaseModelApplied();
      MarkInteractsDirty();
    }

    private void OnDestroy()
    {
      Registry.Registry.Unregister(RegistryType.Npc, _identifier);
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

      _interactsDirty = false;
    }

    private void MarkInteractsDirty()
    {
      _interactsDirty = true;
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
          if (each == null) continue;
          _scenarioInteracts.Add(each.Clone());
        }
        changed = true;
      }

      if (changed)
        MarkInteractsDirty();

      return changed;
    }

    private sealed class ScenarioNpcInteract : IInteract
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

      public void Interact(Transform interactor)
      {
        _npc.TryStartScenarioInteract(_definition, interactor);
      }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      _baseModelApplied = false;
      ApplyBaseModel();
      MarkInteractsDirty();

      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = gameObject.name;
    }
#endif
  }
}
