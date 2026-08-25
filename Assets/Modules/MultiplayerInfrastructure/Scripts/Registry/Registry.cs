using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;
using ISItem = MultiplayerInfrastructure.ItemSystem.Item;

namespace MultiplayerInfrastructure.Registry
{
  public static partial class Registry
  {
    private static readonly Dictionary<string, object> _itemRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _scenarioEventRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _scenarioGraphRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _iconSpriteRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _npcRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _waypointRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _spawnPointRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _entityRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _serviceRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _runtimeStateRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _interactableEntityRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _uiRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _playerModelRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _entityPresetRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _problemSetRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _problemFigureRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, object> _playerTagRegistry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> _playerEntityIdentifierByOwnerUserIdentifier = new(StringComparer.Ordinal);
    private static bool _builtInRegistryInitialized;

    public static event Action<RegistryType, string, object> OnEntryRegistered;
    public static event Action<RegistryType, string> OnEntryUnregistered;

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
      OnEntryRegistered?.Invoke(registryType, identifier, definition);
    }

    public static void Unregister(RegistryType registryType, string identifier)
    {
      EnsureBuiltInRegistryInitialized();

      if (string.IsNullOrWhiteSpace(identifier))
      {
        return;
      }

      var registry = ResolveRegistry(registryType);

      if (registryType == RegistryType.Entity
          && registry.TryGetValue(identifier, out var existing)
          && existing is EntityDescriptor existingDescriptor)
      {
        RemoveEntityIndexes(existingDescriptor);
      }

      if (registry.Remove(identifier))
        OnEntryUnregistered?.Invoke(registryType, identifier);
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
      {
        // Scenario documents use the ".scenario.json" extension, so Unity exposes them
        // as "<identifier>.scenario".
        var textAsset = Resources.Load<TextAsset>($"Scenario/{identifier}{ScenarioGraphAssetSuffix}");
        if (textAsset != null)
        {
          registry[identifier] = textAsset;
          definition = textAsset;
        }
        else
        {
          // Secondary fallback: scan all scenario TextAssets and register them.
          // This allows lookup by graph identifier even when file name differs.
          PreloadScenarioGraphsFromResources(validateWithSchema);

          if (!registry.TryGetValue(identifier, out definition))
            return false;
        }
      }

      return TryResolveScenarioGraph(RegistryType.ScenarioGraph, identifier, registry, ref definition, validateWithSchema);
    }

    public static int PreloadScenarioGraphsFromResources(bool validateWithSchema = true)
    {
      EnsureBuiltInRegistryInitialized();

      var registry = ResolveRegistry(RegistryType.ScenarioGraph);
      var assets = Resources.LoadAll<TextAsset>("Scenario");
      int successCount = 0;

      for (int i = 0; i < assets.Length; i++)
      {
        var asset = assets[i];
        if (asset == null)
          continue;

        string assetName = asset.name?.Trim();
        if (string.IsNullOrWhiteSpace(assetName))
          continue;

        // Only ScenarioGraph documents are loaded as graphs. Scenario documents use the
        // ".scenario.json" extension; Unity strips the trailing ".json", so they surface
        // as asset names ending in ".scenario". Companion artifacts (editor layout
        // sidecars ".scenario.editor", conversion reports ".scenario.unsupported.flags",
        // and rubric definitions ".rubric") intentionally do not match the ScenarioGraph
        // schema and must not be loaded as graphs.
        if (!IsScenarioGraphAsset(assetName))
          continue;

        // Register under the bare identifier (without the ".scenario" suffix) so lookups
        // by scenario identifier resolve regardless of the file extension scheme.
        string key = StripScenarioGraphSuffix(assetName);
        if (string.IsNullOrWhiteSpace(key))
          continue;

        if (!registry.TryGetValue(key, out var definition) || definition == null)
        {
          registry[key] = asset;
          definition = asset;
        }

        if (TryResolveScenarioGraph(RegistryType.ScenarioGraph, key, registry, ref definition, validateWithSchema))
          successCount++;
      }

      return successCount;
    }

    /// <summary>
    /// Registers Scenario TextAssets by identifier without parsing them. The first typed
    /// Get/TryGet (or an explicit preload) performs schema validation and replaces the
    /// TextAsset with the resolved graph. This keeps scene startup lightweight without
    /// bypassing runtime validation.
    /// </summary>
    public static int IndexScenarioGraphAssetsFromResources()
    {
      EnsureBuiltInRegistryInitialized();

      var registry = ResolveRegistry(RegistryType.ScenarioGraph);
      var assets = Resources.LoadAll<TextAsset>("Scenario");
      int indexedCount = 0;

      for (int i = 0; i < assets.Length; i++)
      {
        var asset = assets[i];
        if (asset == null)
          continue;

        string assetName = asset.name?.Trim();
        if (!IsScenarioGraphAsset(assetName))
          continue;

        string key = StripScenarioGraphSuffix(assetName);
        if (string.IsNullOrWhiteSpace(key))
          continue;

        // Do not replace an explicitly registered or already resolved graph.
        if (!registry.TryGetValue(key, out var definition) || definition == null)
          registry[key] = asset;

        indexedCount++;
      }

      return indexedCount;
    }

    /// <summary>
    /// Asset-name suffix exposed by Unity for ScenarioGraph documents. Scenario files use
    /// the ".scenario.json" extension; Unity strips the trailing ".json", leaving
    /// ".scenario".
    /// </summary>
    private const string ScenarioGraphAssetSuffix = ".scenario";

    /// <summary>
    /// Returns true when a Resources/Scenario asset is an actual ScenarioGraph document.
    /// Scenario documents end in ".scenario" (from ".scenario.json"). Companion artifacts
    /// such as editor layout sidecars (".scenario.editor"), conversion reports
    /// (".scenario.unsupported.flags"), and rubric definitions (".rubric") intentionally do
    /// not match the ScenarioGraph schema and must not be loaded as graphs.
    /// </summary>
    private static bool IsScenarioGraphAsset(string assetName)
    {
      if (string.IsNullOrWhiteSpace(assetName))
        return false;

      // ".scenario.editor", ".scenario.unsupported.flags", etc. carry additional suffixes
      // after ".scenario" and must be excluded.
      return assetName.EndsWith(ScenarioGraphAssetSuffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Strips the trailing ".scenario" suffix from a ScenarioGraph asset name, yielding the
    /// bare scenario identifier used as the registry key.
    /// </summary>
    private static string StripScenarioGraphSuffix(string assetName)
    {
      if (string.IsNullOrWhiteSpace(assetName))
        return assetName;

      if (assetName.EndsWith(ScenarioGraphAssetSuffix, StringComparison.OrdinalIgnoreCase))
        return assetName.Substring(0, assetName.Length - ScenarioGraphAssetSuffix.Length);

      return assetName;
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

      if (TryGetEntity(descriptor.Identifier, out var existingDescriptor) && existingDescriptor != null)
      {
        RemoveEntityIndexes(existingDescriptor);
      }

      Register(RegistryType.Entity, descriptor.Identifier, descriptor);
      AddEntityIndexes(descriptor);
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
    {
      if (TryGetEntity(identifier, out var descriptor) && descriptor != null)
      {
        RemoveEntityIndexes(descriptor);
      }

      Unregister(RegistryType.Entity, identifier);
    }

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

      if (_playerEntityIdentifierByOwnerUserIdentifier.TryGetValue(ownerUserIdentifier, out var entityIdentifier)
          && TryGetEntity(entityIdentifier, out var indexedDescriptor)
          && indexedDescriptor != null
          && indexedDescriptor.EntityType == EntityType.Player)
      {
        descriptor = indexedDescriptor;
        return true;
      }

      var all = GetAllEntities(EntityType.Player);
      foreach (var pair in all)
      {
        if (string.Equals(pair.Value?.OwnerUserIdentifier, ownerUserIdentifier, StringComparison.Ordinal))
        {
          descriptor = pair.Value;
          _playerEntityIdentifierByOwnerUserIdentifier[ownerUserIdentifier] = pair.Key;
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
        RegistryType.ScenarioEvent => _scenarioEventRegistry,
        RegistryType.ScenarioGraph => _scenarioGraphRegistry,
        RegistryType.IconSprite => _iconSpriteRegistry,
        RegistryType.Npc => _npcRegistry,
        RegistryType.Waypoint => _waypointRegistry,
        RegistryType.SpawnPoint => _spawnPointRegistry,
        RegistryType.Entity => _entityRegistry,
        RegistryType.Service => _serviceRegistry,
        RegistryType.RuntimeState => _runtimeStateRegistry,
        RegistryType.InteractableEntity => _interactableEntityRegistry,
        RegistryType.UI => _uiRegistry,
        RegistryType.PlayerModel => _playerModelRegistry,
        RegistryType.EntityPreset => _entityPresetRegistry,
        RegistryType.ProblemSet => _problemSetRegistry,
        RegistryType.ProblemFigure => _problemFigureRegistry,
        RegistryType.PlayerTag => _playerTagRegistry,
        _ => throw new ArgumentOutOfRangeException(nameof(registryType), registryType, "Unknown registry type")
      };
    }

    private static void AddEntityIndexes(EntityDescriptor descriptor)
    {
      if (descriptor == null)
      {
        return;
      }

      if (descriptor.EntityType != EntityType.Player)
      {
        return;
      }

      if (string.IsNullOrWhiteSpace(descriptor.OwnerUserIdentifier)
          || string.IsNullOrWhiteSpace(descriptor.Identifier))
      {
        return;
      }

      _playerEntityIdentifierByOwnerUserIdentifier[descriptor.OwnerUserIdentifier] = descriptor.Identifier;
    }

    private static void RemoveEntityIndexes(EntityDescriptor descriptor)
    {
      if (descriptor == null)
      {
        return;
      }

      if (descriptor.EntityType != EntityType.Player)
      {
        return;
      }

      if (string.IsNullOrWhiteSpace(descriptor.OwnerUserIdentifier))
      {
        return;
      }

      if (_playerEntityIdentifierByOwnerUserIdentifier.TryGetValue(descriptor.OwnerUserIdentifier, out var registeredIdentifier)
          && string.Equals(registeredIdentifier, descriptor.Identifier, StringComparison.Ordinal))
      {
        _playerEntityIdentifierByOwnerUserIdentifier.Remove(descriptor.OwnerUserIdentifier);
      }
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
        else
          graph.Identifier = graph.Identifier.Trim();

        registry[identifier] = graph;
        definition = graph;

        RegisterScenarioGraphAlias(registry, graph, identifier);
        return true;
      }
      catch (Exception ex)
      {
        Debug.LogError($"[Registry] Failed to load ScenarioGraph '{identifier}': {ex.Message}");
        return false;
      }
    }

    private static void RegisterScenarioGraphAlias(Dictionary<string, object> registry, ScenarioGraph graph, string sourceIdentifier)
    {
      if (registry == null || graph == null || string.IsNullOrWhiteSpace(graph.Identifier))
        return;

      string alias = graph.Identifier.Trim();
      if (string.IsNullOrWhiteSpace(alias))
        return;

      if (registry.TryGetValue(alias, out var existing))
      {
        if (existing is ScenarioGraph existingGraph && !ReferenceEquals(existingGraph, graph)
                                               && !string.Equals(alias, sourceIdentifier, StringComparison.Ordinal))
        {
          Debug.LogWarning(
            $"[Registry] ScenarioGraph alias conflict: '{alias}' is already mapped. Source key '{sourceIdentifier}' keeps existing mapping.");
        }

        return;
      }

      registry[alias] = graph;
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
    // ScenarioEvent helpers
    // =========================================================================

    /// <summary>
    /// 시나리오 이벤트 핸들러를 Registry와 ScenarioEventIdentifierRegistry에 동시 등록합니다.
    /// </summary>
    public static void RegisterScenarioEvent(string identifier, ScenarioEventIdentifierRegistry.ScenarioEventHandler handler)
    {
      if (string.IsNullOrWhiteSpace(identifier) || handler == null)
      {
        return;
      }

      Register(RegistryType.ScenarioEvent, identifier, handler);
      ScenarioEventIdentifierRegistry.Register(identifier, handler);
    }

    /// <summary>
    /// 등록된 시나리오 이벤트 핸들러를 조회합니다.
    /// </summary>
    public static bool TryGetScenarioEvent(string identifier, out ScenarioEventIdentifierRegistry.ScenarioEventHandler handler)
      => TryGet(RegistryType.ScenarioEvent, identifier, out handler);

    /// <summary>
    /// 시나리오 이벤트 핸들러 등록을 해제합니다.
    /// </summary>
    public static void UnregisterScenarioEvent(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
      {
        return;
      }

      Unregister(RegistryType.ScenarioEvent, identifier);
      ScenarioEventIdentifierRegistry.Unregister(identifier);
    }

    /// <summary>
    /// 등록된 모든 시나리오 이벤트를 반환합니다.
    /// </summary>
    public static IReadOnlyDictionary<string, ScenarioEventIdentifierRegistry.ScenarioEventHandler> GetAllScenarioEvents()
      => GetAll<ScenarioEventIdentifierRegistry.ScenarioEventHandler>(RegistryType.ScenarioEvent);

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
