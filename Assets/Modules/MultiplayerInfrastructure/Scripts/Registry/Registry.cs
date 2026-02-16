using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public static partial class Registry
  {
    private static readonly Dictionary<string, object> _itemRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _scenarioGraphRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _iconSpriteRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _npcRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _waypointRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _entityRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _interactableEntityRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _uiRegistry = new(StringComparer.Ordinal);
    private static bool _builtInRegistryInitialized;

    static partial void RegisterBuiltInLiterals();

    private static void EnsureBuiltInRegistryInitialized()
    {
      if (_builtInRegistryInitialized)
        return;

      _builtInRegistryInitialized = true;
      RegisterBuiltInLiterals();
    }

    public static void Register(RegistryType registryType, string identifier, object definition)
    {
      EnsureBuiltInRegistryInitialized();

      if (string.IsNullOrWhiteSpace(identifier) || definition == null)
      {
        return;
      }

      var registry = ResolveRegistry(registryType);
      registry[identifier] = definition;
    }

    public static void Unregister(RegistryType registryType, string identifier)
    {
      EnsureBuiltInRegistryInitialized();

      if (string.IsNullOrWhiteSpace(identifier))
      {
        return;
      }

      var registry = ResolveRegistry(registryType);
      registry.Remove(identifier);
    }

    public static T Get<T>(RegistryType registryType, string identifier)
    {
      EnsureBuiltInRegistryInitialized();

      if (string.IsNullOrWhiteSpace(identifier))
      {
        return default;
      }

      var registry = ResolveRegistry(registryType);
      if (!registry.TryGetValue(identifier, out var definition))
      {
        return default;
      }

      if (!TryResolveScenarioGraph(registryType, identifier, registry, ref definition, validateWithSchema: true))
      {
        return default;
      }

      if (!TryResolveIconSprite(registryType, identifier, registry, ref definition))
      {
        return default;
      }

      return definition is T typedDefinition ? typedDefinition : default;
    }

    public static bool Contains(RegistryType registryType, string identifier)
    {
      EnsureBuiltInRegistryInitialized();

      if (string.IsNullOrWhiteSpace(identifier))
      {
        return false;
      }

      var registry = ResolveRegistry(registryType);
      return registry.ContainsKey(identifier);
    }

    public static bool TryGet<T>(RegistryType registryType, string identifier, out T value)
    {
      EnsureBuiltInRegistryInitialized();

      value = default;
      if (string.IsNullOrWhiteSpace(identifier))
      {
        return false;
      }

      var registry = ResolveRegistry(registryType);
      if (!registry.TryGetValue(identifier, out var definition))
      {
        return false;
      }

      if (!TryResolveScenarioGraph(registryType, identifier, registry, ref definition, validateWithSchema: true))
      {
        return false;
      }

      if (!TryResolveIconSprite(registryType, identifier, registry, ref definition))
      {
        return false;
      }

      if (definition is not T typedDefinition)
      {
        return false;
      }

      value = typedDefinition;
      return true;
    }

    public static bool PreloadScenarioGraph(string identifier, bool validateWithSchema = true)
    {
      EnsureBuiltInRegistryInitialized();

      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      var registry = ResolveRegistry(RegistryType.ScenarioGraph);
      if (!registry.TryGetValue(identifier, out var definition))
        return false;

      return TryResolveScenarioGraph(RegistryType.ScenarioGraph, identifier, registry, ref definition, validateWithSchema);
    }

    public static IReadOnlyDictionary<string, T> GetAll<T>(RegistryType registryType)
    {
      EnsureBuiltInRegistryInitialized();

      var registry = ResolveRegistry(registryType);
      var result = new Dictionary<string, T>(registry.Count, StringComparer.Ordinal);

      foreach (var pair in registry)
      {
        if (pair.Value is T typedDefinition)
        {
          result[pair.Key] = typedDefinition;
        }
      }

      return result;
    }

    public static string TypeKey<T>()
    {
      return typeof(T).FullName ?? typeof(T).Name;
    }

    public static string TypeKey(Type type)
    {
      if (type == null)
      {
        throw new ArgumentNullException(nameof(type));
      }

      return type.FullName ?? type.Name;
    }

    private static Dictionary<string, object> ResolveRegistry(RegistryType registryType)
    {
      return registryType switch
      {
        RegistryType.Item => _itemRegistry,
        RegistryType.ScenarioGraph => _scenarioGraphRegistry,
        RegistryType.IconSprite => _iconSpriteRegistry,
        RegistryType.Npc => _npcRegistry,
        RegistryType.Waypoint => _waypointRegistry,
        RegistryType.Entity => _entityRegistry,
        RegistryType.InteractableEntity => _interactableEntityRegistry,
        RegistryType.UI => _uiRegistry,
        _ => throw new ArgumentOutOfRangeException(nameof(registryType), registryType, "Unknown registry type")
      };
    }

    private static bool TryResolveScenarioGraph(
      RegistryType registryType,
      string identifier,
      Dictionary<string, object> registry,
      ref object definition,
      bool validateWithSchema)
    {
      if (registryType != RegistryType.ScenarioGraph)
        return true;

      if (definition is ScenarioGraph)
        return true;

      if (definition is not TextAsset textAsset)
        return false;

      if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
        return false;

      try
      {
        var graph = ScenarioGraphLoader.LoadFromJson(textAsset.text, validateWithSchema);
        if (string.IsNullOrWhiteSpace(graph.Identifier))
          graph.Identifier = identifier;

        registry[identifier] = graph;
        definition = graph;
        return true;
      }
      catch (Exception ex)
      {
        Debug.LogError($"[Registry] Failed to load ScenarioGraph '{identifier}': {ex.Message}");
        return false;
      }
    }

    private static bool TryResolveIconSprite(
      RegistryType registryType,
      string identifier,
      Dictionary<string, object> registry,
      ref object definition)
    {
      if (registryType != RegistryType.IconSprite)
        return true;

      if (definition is Sprite)
        return true;

      if (definition is not string spriteResourcePath || string.IsNullOrWhiteSpace(spriteResourcePath))
        return false;

      var sprite = Resources.Load<Sprite>(spriteResourcePath);
      if (sprite == null)
      {
        Debug.LogWarning($"[Registry] Failed to load icon sprite '{identifier}' from '{spriteResourcePath}'.");
        return false;
      }

      registry[identifier] = sprite;
      definition = sprite;
      return true;
    }
  }
}
