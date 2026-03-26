using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.ItemSystem;
using ISItem = MultiplayerInfrastructure.ItemSystem.Item;
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
    private static readonly Dictionary<string, object> _serviceRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _runtimeStateRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _interactableEntityRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _uiRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _playerTagRegistry = new(StringComparer.Ordinal);
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

      if (definition is UnityEngine.Object unityObject && unityObject == null)
      {
        registry.Remove(identifier);
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

      if (TryResolveEntityValue(registryType, definition, out T entityValue))
        return entityValue;

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
      if (!registry.TryGetValue(identifier, out var definition))
        return false;

      if (definition is UnityEngine.Object unityObject && unityObject == null)
      {
        registry.Remove(identifier);
        return false;
      }

      return true;
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

      if (definition is UnityEngine.Object unityObject && unityObject == null)
      {
        registry.Remove(identifier);
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

      if (TryResolveEntityValue(registryType, definition, out value))
      {
        return true;
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
        if (TryResolveEntityValue(registryType, pair.Value, out T entityValue))
        {
          result[pair.Key] = entityValue;
        }
        else if (pair.Value is T typedDefinition)
        {
          result[pair.Key] = typedDefinition;
        }
      }

      return result;
    }

    public static void RegisterEntity(EntityDescriptor descriptor)
    {
      if (descriptor == null || string.IsNullOrWhiteSpace(descriptor.Identifier) || descriptor.GameObject == null)
        return;

      Register(RegistryType.Entity, descriptor.Identifier, descriptor);
    }

    public static void RegisterEntity(
      string identifier,
      EntityType entityType,
      GameObject gameObject,
      string displayName = null,
      string ownerUserIdentifier = null,
      int? clientId = null,
      bool isNetworked = false)
    {
      RegisterEntity(new EntityDescriptor(
        identifier,
        entityType,
        gameObject,
        displayName,
        ownerUserIdentifier,
        clientId,
        isNetworked));
    }

    public static bool TryGetEntity(string identifier, out EntityDescriptor descriptor)
      => TryGet(RegistryType.Entity, identifier, out descriptor);

    public static void UnregisterEntity(string identifier)
      => Unregister(RegistryType.Entity, identifier);

    public static IReadOnlyDictionary<string, EntityDescriptor> GetAllEntities()
      => GetAll<EntityDescriptor>(RegistryType.Entity);

    public static IReadOnlyDictionary<string, EntityDescriptor> GetAllEntities(EntityType entityType)
    {
      var all = GetAll<EntityDescriptor>(RegistryType.Entity);
      var filtered = new Dictionary<string, EntityDescriptor>(StringComparer.Ordinal);

      foreach (var pair in all)
      {
        if (pair.Value != null && pair.Value.EntityType == entityType)
          filtered[pair.Key] = pair.Value;
      }

      return filtered;
    }

    public static bool TryFindFirstEntityComponent<T>(EntityType entityType, out T component, Predicate<T> predicate = null)
      where T : Component
    {
      component = null;
      var all = GetAllEntities(entityType);

      foreach (var pair in all)
      {
        var go = pair.Value?.GameObject;
        if (go == null)
          continue;

        if (!go.TryGetComponent(out T candidate) || candidate == null)
          continue;

        if (predicate != null && !predicate(candidate))
          continue;

        component = candidate;
        return true;
      }

      return false;
    }

    public static T GetFirstEntityComponent<T>(EntityType entityType, Predicate<T> predicate = null)
      where T : Component
      => TryFindFirstEntityComponent(entityType, out T component, predicate) ? component : null;

    public static bool TryGetEntityByClientId(int clientId, out EntityDescriptor descriptor)
    {
      descriptor = null;
      var all = GetAllEntities(EntityType.Player);
      foreach (var pair in all)
      {
        if (pair.Value?.ClientId == clientId)
        {
          descriptor = pair.Value;
          return true;
        }
      }

      return false;
    }

    public static bool TryGetEntityByOwnerUserIdentifier(string ownerUserIdentifier, out EntityDescriptor descriptor)
    {
      descriptor = null;
      if (string.IsNullOrWhiteSpace(ownerUserIdentifier))
        return false;

      var all = GetAllEntities(EntityType.Player);
      foreach (var pair in all)
      {
        if (string.Equals(pair.Value?.OwnerUserIdentifier, ownerUserIdentifier, StringComparison.Ordinal))
        {
          descriptor = pair.Value;
          return true;
        }
      }

      return false;
    }

    public static void UpdateEntityDisplayName(string identifier, string displayName)
    {
      if (!TryGetEntity(identifier, out var descriptor) || descriptor == null)
        return;

      descriptor.DisplayName = displayName;
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
        RegistryType.Service => _serviceRegistry,
        RegistryType.RuntimeState => _runtimeStateRegistry,
        RegistryType.InteractableEntity => _interactableEntityRegistry,
        RegistryType.UI => _uiRegistry,
        RegistryType.PlayerTag => _playerTagRegistry,
        _ => throw new ArgumentOutOfRangeException(nameof(registryType), registryType, "Unknown registry type")
      };
    }

    private static bool TryResolveEntityValue<T>(RegistryType registryType, object definition, out T value)
    {
      value = default;

      if (registryType != RegistryType.Entity)
        return false;

      if (definition is T typedDefinition)
      {
        value = typedDefinition;
        return true;
      }

      if (definition is not EntityDescriptor descriptor || descriptor.GameObject == null)
        return false;

      if (typeof(T) == typeof(GameObject))
      {
        value = (T)(object)descriptor.GameObject;
        return true;
      }

      if (typeof(T) == typeof(Transform))
      {
        value = (T)(object)descriptor.GameObject.transform;
        return true;
      }

      if (!typeof(Component).IsAssignableFrom(typeof(T)))
        return false;

      var component = descriptor.GameObject.GetComponent(typeof(T));
      if (component == null)
        return false;

      value = (T)(object)component;
      return true;
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

    // =========================================================================
    // ScenarioGraph helpers
    // =========================================================================

    /// <summary>
    /// 등록된 식별자로 ScenarioGraph를 가져옵니다.
    /// TextAsset으로 등록된 경우 파싱 후 캐싱됩니다.
    /// </summary>
    public static bool TryGetScenarioGraph(string identifier, out ScenarioGraph graph, out string error)
    {
      graph = null;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(identifier))
      {
        error = "Scenario identifier is required.";
        return false;
      }

      if (!PreloadScenarioGraph(identifier))
      {
        error = $"Scenario '{identifier}' is not registered or failed to parse.";
        return false;
      }

      graph = Get<ScenarioGraph>(RegistryType.ScenarioGraph, identifier);
      if (graph == null)
      {
        error = $"Scenario '{identifier}' could not be resolved.";
        return false;
      }

      return true;
    }

    // =========================================================================
    // ItemDefinition helpers
    // =========================================================================

    /// <summary>
    /// ItemSystem.Item 파생 클래스의 Type을 레지스트리에 등록합니다.
    /// identifier 는 Item.Identifier 와 일치시키는 것을 권장합니다.
    /// </summary>
    public static void RegisterItemDefinition<T>(string identifier) where T : ISItem, new()
      => Register(RegistryType.Item, identifier, typeof(T));

    /// <summary>
    /// RegisterItemDefinition 으로 등록된 클래스로부터 새 Item 인스턴스를 생성합니다.
    /// 등록된 클래스가 없으면 null 을 반환합니다.
    /// </summary>
    public static ISItem CreateItemInstance(string identifier)
    {
      if (!TryGet<Type>(RegistryType.Item, identifier, out var type) || type == null)
      {
        Debug.LogWarning($"[Registry] ItemDefinition '{identifier}' 이(가) 등록되지 않았습니다.");
        return null;
      }

      try
      {
        return (ISItem)Activator.CreateInstance(type);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[Registry] ItemDefinition '{identifier}' 인스턴스화 실패: {ex.Message}");
        return null;
      }
    }
  }
}
