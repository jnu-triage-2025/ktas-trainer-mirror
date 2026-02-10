using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MultiplayerInfrastructure.Definitions;
using UnityEngine;
using MultiplayerInfrastructure.Registry;

public class ItemRegistry : MonoBehaviour
{
  private static ItemRegistry _instance;
  public static ItemRegistry Instance => _instance;

  [Header("Register all items here.")]
  public ItemRegistryRequirement[] registeredItems = new ItemRegistryRequirement[0];

  [Header("Icon Loading")]
  [Tooltip("Resources-relative folder (recommended), or a project path containing '/Resources/'. " +
           "Example (recommended): 'Textures/Items'. " +
           "Example (also accepted): 'Assets/Modules/TriageTrainer/Resources/Textures/Items'")]
  public string iconResourcesPath = DefaultsItemRegistry.ItemTexturesPath;

  [Tooltip("If true, will start async preloading of registered item icons on Awake.")]
  public bool preloadIconsOnAwakeAsync = false;

  [Tooltip("If false, missing icon warnings will be suppressed.")]
  public bool logMissingIcons = true;

  private readonly Dictionary<string, ItemBaseModelSO> _itemBaseModelCache = new Dictionary<string, ItemBaseModelSO>();
  private readonly Dictionary<string, GameObject> _itemPrefabCache = new Dictionary<string, GameObject>();

  // Keep icon cache case-sensitive to surface real casing/path issues on platforms with case-sensitive FS.
  private readonly Dictionary<string, Sprite> _itemIconCache = new Dictionary<string, Sprite>(StringComparer.Ordinal);

  // De-dup missing logs. Key format: "<trimmedResourcesPath>:<identifier>"
  private readonly HashSet<string> _missingIcons = new HashSet<string>(StringComparer.Ordinal);

  // De-dup in-flight async loads.
  private readonly Dictionary<string, ResourceRequest> _iconLoadRequests = new Dictionary<string, ResourceRequest>(StringComparer.Ordinal);

  [Header("Debug")]
  public List<ItemBaseModelSO> _debugRegisteredItemModels = new List<ItemBaseModelSO>();
  public List<GameObject> _debugRegisteredItemPrefabs = new List<GameObject>();
  public List<Sprite> _debugRegisteredItemIcons = new List<Sprite>();

  void Awake()
  {
    if (_instance != null && _instance != this)
    {
      Destroy(gameObject);
      return;
    }

    _instance = this;
    DontDestroyOnLoad(gameObject);

    _itemBaseModelCache.Clear();
    _itemPrefabCache.Clear();
    _itemIconCache.Clear();
    _missingIcons.Clear();
    _iconLoadRequests.Clear();

    _debugRegisteredItemModels.Clear();
    _debugRegisteredItemPrefabs.Clear();
    _debugRegisteredItemIcons.Clear();

    foreach (var entry in registeredItems)
    {
      if (entry.itemDataModel == null) continue;

      var id = entry.itemDataModel.identifier;
      if (string.IsNullOrWhiteSpace(id)) continue;

      _itemBaseModelCache[id] = entry.itemDataModel;
      _itemPrefabCache[id] = entry.itemPrefab;
      _itemIconCache[id] = entry.itemSprite;

      _debugRegisteredItemModels.Add(entry.itemDataModel);
      _debugRegisteredItemPrefabs.Add(entry.itemPrefab);
      _debugRegisteredItemIcons.Add(entry.itemSprite);

      // If a sprite was explicitly registered, skip preload attempts.
      if (entry.itemSprite != null)
        continue;

      // Don’t block startup unless you want sync preload.
      if (preloadIconsOnAwakeAsync)
        PreloadIconAsync(id);
      else
        PreloadIcon(id);
    }
  }

  public ItemBaseModelSO GetItemBaseModelSO(string id)
  {
    if (string.IsNullOrWhiteSpace(id)) return null;
    _itemBaseModelCache.TryGetValue(id, out var model);
    return model;
  }

  public GameObject GetItemPrefab(string id)
  {
    if (string.IsNullOrWhiteSpace(id)) return null;
    _itemPrefabCache.TryGetValue(id, out var prefab);
    return prefab;
  }

  public bool IsItemRegistered(string id)
  {
    if (string.IsNullOrWhiteSpace(id)) return false;
    return _itemBaseModelCache.ContainsKey(id);
  }

  /// <summary>
  /// Runtime-safe, synchronous icon getter.
  /// Uses cache first, then attempts Resources.Load.
  /// </summary>
  public Sprite GetItemIcon(string identifier)
  {
    identifier = NormalizeIdentifier(identifier);
    if (string.IsNullOrEmpty(identifier)) return null;

    if (_itemIconCache.TryGetValue(identifier, out var cached) && cached != null)
      return cached;

    var loaded = TryLoadIconSync(identifier, out var resolvedKey);
    if (loaded != null)
    {
      CacheIcon(resolvedKey, identifier, loaded);
      return loaded;
    }

    LogMissingIconOnce(identifier);
    return DefaultsResource.FallbackSprite;
  }

  /// <summary>
  /// Sync preload (same pipeline as GetItemIcon). Keeps API compatibility with your current code.
  /// </summary>
  private Sprite PreloadIcon(string identifier)
  {
    identifier = NormalizeIdentifier(identifier);
    if (string.IsNullOrEmpty(identifier)) return null;

    if (_itemIconCache.TryGetValue(identifier, out var cached) && cached != null)
      return cached;

    var loaded = TryLoadIconSync(identifier, out var resolvedKey);
    if (loaded != null)
    {
      CacheIcon(resolvedKey, identifier, loaded);

      // debug
      if (!_debugRegisteredItemIcons.Contains(loaded))
        _debugRegisteredItemIcons.Add(loaded);

      return loaded;
    }

    LogMissingIconOnce(identifier);
    return DefaultsResource.FallbackSprite;
  }

  /// <summary>
  /// Runtime-safe async preload (non-blocking). Uses Resources.LoadAsync internally.
  /// This starts a coroutine on this MonoBehaviour.
  /// </summary>
  public void PreloadIconAsync(string identifier)
  {
    identifier = NormalizeIdentifier(identifier);
    if (string.IsNullOrEmpty(identifier)) return;

    if (_itemIconCache.TryGetValue(identifier, out var cached) && cached != null)
      return;

    // Prevent duplicate async loads for same key
    if (_iconLoadRequests.ContainsKey(identifier))
      return;

    StartCoroutine(PreloadIconCoroutine(identifier));
  }

  private IEnumerator PreloadIconCoroutine(string identifier)
  {
    // Try exact first, then lowercase fallback (helps if identifiers are inconsistent)
    // Note: actual Resources path is case-sensitive on many platforms.
    yield return LoadAndCacheIconAsync(identifier, identifier);

    if (_itemIconCache.TryGetValue(identifier, out var cached) && cached != null)
      yield break;

    var lower = identifier.ToLowerInvariant();
    if (lower != identifier)
      yield return LoadAndCacheIconAsync(lower, identifier);

    if (_itemIconCache.TryGetValue(identifier, out cached) && cached != null)
      yield break;

    LogMissingIconOnce(identifier);
  }

  private IEnumerator LoadAndCacheIconAsync(string attemptKey, string originalKeyToAlsoCache)
  {
    if (_itemIconCache.TryGetValue(originalKeyToAlsoCache, out var existing) && existing != null)
      yield break;

    var path = BuildResourcesSpritePath(attemptKey, iconResourcesPath);
    if (string.IsNullOrEmpty(path))
      path = BuildResourcesSpritePath(attemptKey, DefaultsItemRegistry.ItemTexturesPath);
    if (string.IsNullOrEmpty(path))
      yield break;

    // If another coroutine already kicked this exact request, wait for it.
    if (_iconLoadRequests.TryGetValue(originalKeyToAlsoCache, out var inflight) && inflight != null)
    {
      yield return inflight;
      var sprite = inflight.asset as Sprite;
      if (sprite != null)
      {
        CacheIcon(attemptKey, originalKeyToAlsoCache, sprite);
      }
      yield break;
    }

    var req = Resources.LoadAsync<Sprite>(path);
    _iconLoadRequests[originalKeyToAlsoCache] = req;

    yield return req;

    _iconLoadRequests.Remove(originalKeyToAlsoCache);

    var loaded = req.asset as Sprite;
    if (loaded != null)
    {
      CacheIcon(attemptKey, originalKeyToAlsoCache, loaded);

      // debug
      if (!_debugRegisteredItemIcons.Contains(loaded))
        _debugRegisteredItemIcons.Add(loaded);
    }
  }

  // -------------------------
  // Loading pipeline (sync)
  // -------------------------

  private Sprite TryLoadIconSync(string identifier, out string resolvedIdentifier)
  {
    resolvedIdentifier = identifier;
    if (string.IsNullOrEmpty(identifier)) return null;

    // 1) exact
    var sprite = LoadSpriteFromResourcesSync(identifier);
    if (sprite != null) return sprite;

    // 1.1) exact in default folder (any module Resources/Textures/Items)
    sprite = LoadSpriteFromResourcesSync(identifier, DefaultsItemRegistry.ItemTexturesPath);
    if (sprite != null) return sprite;

    // 2) lowercase fallback
    var lower = identifier.ToLowerInvariant();
    if (lower == identifier) return null;

    sprite = LoadSpriteFromResourcesSync(lower);
    if (sprite != null)
    {
      resolvedIdentifier = lower;
      return sprite;
    }

    // 2.1) lowercase in default folder
    sprite = LoadSpriteFromResourcesSync(lower, DefaultsItemRegistry.ItemTexturesPath);
    if (sprite != null)
    {
      resolvedIdentifier = lower;
      return sprite;
    }

    return null;
  }

  private Sprite LoadSpriteFromResourcesSync(string identifier)
  {
    var path = BuildResourcesSpritePath(identifier, iconResourcesPath);
    if (string.IsNullOrEmpty(path)) return null;
    return Resources.Load<Sprite>(path);
  }

  private Sprite LoadSpriteFromResourcesSync(string identifier, string resourcesPath)
  {
    var path = BuildResourcesSpritePath(identifier, resourcesPath);
    if (string.IsNullOrEmpty(path)) return null;
    return Resources.Load<Sprite>(path);
  }

  // -------------------------
  // Path / caching / logging
  // -------------------------

  private void CacheIcon(string resolvedKey, string originalKey, Sprite sprite)
  {
    if (sprite == null) return;

    // Cache under the resolved key (exact path key that worked)
    _itemIconCache[resolvedKey] = sprite;

    // Also cache under the original identifier to avoid repeating fallback work.
    _itemIconCache[originalKey] = sprite;
  }

  private void LogMissingIconOnce(string identifier)
  {
    if (!logMissingIcons) return;

    string trimmedPath = NormalizeToResourcesRelativeFolder(iconResourcesPath);
    string missingKey = $"{trimmedPath}:{identifier}";
    if (_missingIcons.Contains(missingKey)) return;

    _missingIcons.Add(missingKey);

    string requested = string.IsNullOrEmpty(trimmedPath) ? identifier : $"{trimmedPath}/{identifier}";
    Debug.LogWarning($"ItemRegistry: Sprite not found for identifier '{identifier}' at Resources path '{requested}'. Also tried lowercase.");
  }

  private string BuildResourcesSpritePath(string identifier, string resourcesPath)
  {
    identifier = NormalizeIdentifier(identifier);
    if (string.IsNullOrEmpty(identifier)) return null;

    string folder = NormalizeToResourcesRelativeFolder(resourcesPath);

    // If inspector points *directly* at a sprite file path by mistake, strip extension from identifier anyway.
    // Resources.Load expects no extension.
    var combined = string.IsNullOrEmpty(folder) ? identifier : $"{folder}/{identifier}";
    combined = combined.Replace("\\", "/").Trim('/');
    return string.IsNullOrEmpty(combined) ? null : combined;
  }

  /// <summary>
  /// Accepts:
  /// - "Textures/Items"
  /// - "Assets/Modules/TriageTrainer/Resources/Textures/Items"
  /// - ".../Resources/Textures/Items"
  /// Returns a Resources-relative folder without leading/trailing slashes.
  /// </summary>
  private static string NormalizeToResourcesRelativeFolder(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
      return string.Empty;

    string path = input.Trim().Replace("\\", "/");

    // Remove trailing slash
    path = path.Trim('/');

    // If the user pasted a full project path, trim everything up to and including "/Resources/"
    // Use LastIndexOf to handle nested occurrences safely.
    int idx = path.LastIndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);
    if (idx >= 0)
    {
      path = path.Substring(idx + "/Resources/".Length);
      return path.Trim('/');
    }

    // If they ended exactly with ".../Resources"
    if (path.EndsWith("/Resources", StringComparison.OrdinalIgnoreCase))
      return string.Empty;

    // If they wrote just "Resources/Textures/Items"
    if (path.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
      return path.Substring("Resources/".Length).Trim('/');

    // Otherwise assume already Resources-relative
    return path.Trim('/');
  }

  /// <summary>
  /// Normalizes an identifier used as a filename in Resources:
  /// - trims
  /// - removes extension (".png", ".asset", etc.)
  /// - strips folder parts if caller accidentally passes "foo/bar/icon"
  ///   (optional behavior: keep folder segments if you want; currently keeps them)
  /// </summary>
  private static string NormalizeIdentifier(string identifier)
  {
    if (string.IsNullOrWhiteSpace(identifier))
      return null;

    string id = identifier.Trim().Replace("\\", "/").Trim('/');

    // Remove extension if provided
    // Path.GetFileNameWithoutExtension would drop folders; we do it manually to keep folders if present.
    int dot = id.LastIndexOf('.');
    if (dot > -1 && dot > id.LastIndexOf('/'))
      id = id.Substring(0, dot);

    return id;
  }
}
