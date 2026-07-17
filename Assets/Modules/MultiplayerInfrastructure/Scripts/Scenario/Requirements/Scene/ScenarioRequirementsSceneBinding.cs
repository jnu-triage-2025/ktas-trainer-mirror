using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  [Serializable]
  public sealed class ScenarioRequirementObjectBinding
  {
    [SerializeField] private ScenarioRequirementKind _kind;
    [SerializeField] private string _identifier;
    [SerializeField] private UnityEngine.Object _target;
    [SerializeField] private string _notes;

    public ScenarioRequirementKind Kind => _kind;
    public string Identifier => _identifier;
    public UnityEngine.Object Target => _target;
    public string Notes => _notes;

    public ScenarioRequirementObjectBinding()
    {
    }

    internal ScenarioRequirementObjectBinding(ScenarioRequirementKind kind, string identifier, UnityEngine.Object target, string notes)
    {
      _kind = kind;
      _identifier = identifier ?? string.Empty;
      _target = target;
      _notes = notes ?? string.Empty;
    }

    internal void SetTarget(UnityEngine.Object target) => _target = target;
    internal void SetNotes(string notes) => _notes = notes ?? string.Empty;

    public bool TryGetKey(out ScenarioRequirementKey key)
    {
      key = default;
      if (string.IsNullOrWhiteSpace(_identifier)) return false;
      key = new ScenarioRequirementKey(_kind, _identifier.Trim());
      return true;
    }
  }

  [DisallowMultipleComponent]
  public sealed class ScenarioRequirementsSceneBinding : MonoBehaviour
  {
    [SerializeField] private int _keySchemaVersion = 1;
    [SerializeField] private string _compositionIdentifier;
    [SerializeField] private List<ScenarioRequirementObjectBinding> _bindings = new List<ScenarioRequirementObjectBinding>();

    public int KeySchemaVersion => _keySchemaVersion;
    public string CompositionIdentifier => _compositionIdentifier ?? string.Empty;
    public IReadOnlyList<ScenarioRequirementObjectBinding> Bindings => _bindings;

    public void AddOrUpdateBinding(ScenarioRequirementKey key, UnityEngine.Object target, string notes = null)
    {
      if (target == null) throw new ArgumentNullException(nameof(target));
      RejectCrossSceneTarget(target);
      var binding = _bindings.FirstOrDefault(value => value != null && value.TryGetKey(out var existing) && existing.Equals(key));
      if (binding == null)
      {
        _bindings.Add(new ScenarioRequirementObjectBinding(key.Kind, key.Identifier, target, notes));
        return;
      }
      binding.SetTarget(target);
      if (notes != null) binding.SetNotes(notes);
    }

    /// <summary>
    /// Adds a separate binding without coalescing an existing key.  This is
    /// useful for explicitly authoring a cardinality/duplicate validation
    /// case; callers that intend replacement should use AddOrUpdateBinding.
    /// </summary>
    public void AddBinding(ScenarioRequirementKey key, UnityEngine.Object target, string notes = null)
    {
      if (target == null) throw new ArgumentNullException(nameof(target));
      RejectCrossSceneTarget(target);
      _bindings.Add(new ScenarioRequirementObjectBinding(key.Kind, key.Identifier, target, notes));
    }

    /// <summary>
    /// A scene binding may only directly reference targets that live in the same
    /// scene as this component.  Cross-scene serialized references are not
    /// supported; providers in other scenes are composed via identifier and the
    /// composition snapshot instead (proposal §10).  Persistent assets have no
    /// scene and are rejected here as well because a scene binding target must be
    /// a scene object.
    /// </summary>
    private void RejectCrossSceneTarget(UnityEngine.Object target)
    {
      UnityEngine.SceneManagement.Scene targetScene;
      switch (target)
      {
        case GameObject gameObjectTarget:
          targetScene = gameObjectTarget.scene;
          break;
        case Component componentTarget:
          targetScene = componentTarget.gameObject.scene;
          break;
        default:
          throw new ArgumentException(
            "Scenario requirement binding target must be a scene GameObject or Component, not a persistent asset.",
            nameof(target));
      }

      if (!targetScene.IsValid() || targetScene != gameObject.scene)
      {
        throw new ArgumentException(
          $"Scenario requirement binding target '{target.name}' must belong to the same scene as the binding component '{gameObject.scene.name}'. Cross-scene references are not supported.",
          nameof(target));
      }
    }

    public void SetCompositionIdentifier(string compositionIdentifier)
      => _compositionIdentifier = compositionIdentifier ?? string.Empty;

    public bool RemoveBinding(ScenarioRequirementKey key)
    {
      var index = _bindings.FindIndex(value => value != null && value.TryGetKey(out var existing) && existing.Equals(key));
      if (index < 0) return false;
      _bindings.RemoveAt(index);
      return true;
    }
  }
}
