using System;
using System.Collections.Generic;
using TriageTrainer.Definitions;
using UnityEngine;

[Serializable]
public struct ItemRegistryRequirement
{
  public ItemBaseModelSO itemDataModel;
  public GameObject itemPrefab;
}

public class ItemRegistry : MonoBehaviour
{
  private static ItemRegistry _instance;
  public static ItemRegistry Instance
  {
    get { return _instance; }
  }

  [Header("Register all items here.")]
  public ItemRegistryRequirement[] registeredItems = new ItemRegistryRequirement[0];

  [Header("Icon Loading")]
  [Tooltip("Resources folder path (under Assets/Resources) where item icons are stored; filenames must match item identifiers.")]
  public string iconResourcesPath = DefaultsItemRegistry.ItemTexturesPath;

  Dictionary<string, ItemBaseModelSO> _itemBaseModelCache = new Dictionary<string, ItemBaseModelSO>();
  Dictionary<string, GameObject> _itemPrefabCache = new Dictionary<string, GameObject>();
  Dictionary<string, Sprite> _itemIconCache = new Dictionary<string, Sprite>();
  HashSet<string> _missingIcons = new HashSet<string>();

  private string GetTrimmedIconPath()
  {
    return string.IsNullOrWhiteSpace(iconResourcesPath)
      ? string.Empty
      : iconResourcesPath.Trim().TrimEnd('/');
  }
  
  [Header("Debug")]
  [HideInInspector] public List<ItemBaseModelSO> _debugRegisteredItemModels = new List<ItemBaseModelSO>();
  [HideInInspector] public List<GameObject> _debugRegisteredItemPrefabs = new List<GameObject>();
  [HideInInspector] public List<Sprite> _debugRegisteredItemIcons = new List<Sprite>();

  void Awake()
  {
    if (_instance != null && _instance != this)
    {
      Destroy(this.gameObject);
      return;
    }
    _instance = this;
    DontDestroyOnLoad(this.gameObject);


    _itemBaseModelCache.Clear();
    _itemPrefabCache.Clear();
    _itemIconCache.Clear();
    _missingIcons.Clear();

    foreach (var entry in registeredItems)
    {
      if (entry.itemDataModel == null || string.IsNullOrEmpty(entry.itemDataModel.identifier))
        continue;
      _itemBaseModelCache[entry.itemDataModel.identifier] = entry.itemDataModel;
      _itemPrefabCache[entry.itemDataModel.identifier] = entry.itemPrefab;
      PreloadIcon(entry.itemDataModel.identifier);
    }
  }

  public ItemBaseModelSO GetItemBaseModelSO(string id)
  {
    if (string.IsNullOrEmpty(id)) return null;
    _itemBaseModelCache.TryGetValue(id, out var model);
    return model;
  }

  public GameObject GetItemPrefab(string id)
  {
    if (string.IsNullOrEmpty(id)) return null;
    _itemPrefabCache.TryGetValue(id, out var prefab);
    return prefab;
  }

  public Sprite GetItemIcon(string identifier)
  {
    if (string.IsNullOrEmpty(identifier)) return null;

    if (_itemIconCache.TryGetValue(identifier, out var cached) && cached != null)
      return cached;

    string trimmedPath = GetTrimmedIconPath();

    Sprite TryLoad(string id)
    {
      string path = string.IsNullOrEmpty(trimmedPath) ? id : $"{trimmedPath}/{id}";
      return Resources.Load<Sprite>(path);
    }

    Sprite loaded = TryLoad(identifier);
    if (loaded == null)
    {
      string lower = identifier.ToLowerInvariant();
      if (lower != identifier)
      {
        loaded = TryLoad(lower);
        if (loaded != null)
          identifier = lower;
      }
    }

    if (loaded != null)
    {
      _itemIconCache[identifier] = loaded;
      return loaded;
    }

    string missingKey = $"{trimmedPath}:{identifier}";
    if (!_missingIcons.Contains(missingKey))
    {
      _missingIcons.Add(missingKey);
      Debug.LogWarning($"ItemRegistry: Sprite not found for identifier '{identifier}' at Resources path '{(string.IsNullOrEmpty(trimmedPath) ? identifier : trimmedPath + "/" + identifier)}'. Also tried lowercase.");
    }

    return null;
  }

  private Sprite PreloadIcon(string identifier)
  {
    if (string.IsNullOrEmpty(identifier)) return null;

    if (_itemIconCache.TryGetValue(identifier, out var cached) && cached != null)
      return cached;

    string trimmedPath = GetTrimmedIconPath();

    Sprite TryLoad(string id)
    {
      string path = string.IsNullOrEmpty(trimmedPath) ? id : $"{trimmedPath}/{id}";
      return Resources.Load<Sprite>(path);
    }

    Sprite loaded = TryLoad(identifier);
    if (loaded == null)
    {
      string lower = identifier.ToLowerInvariant();
      if (lower != identifier)
      {
        loaded = TryLoad(lower);
        if (loaded != null)
          identifier = lower;
      }
    }

    if (loaded != null)
    {
      _itemIconCache[identifier] = loaded;
      return loaded;
    }

    string missingKey = $"{trimmedPath}:{identifier}";
    if (!_missingIcons.Contains(missingKey))
    {
      _missingIcons.Add(missingKey);
      Debug.LogWarning($"ItemRegistry: Sprite not found for identifier '{identifier}' at Resources path '{(string.IsNullOrEmpty(trimmedPath) ? identifier : trimmedPath + "/" + identifier)}'. Also tried lowercase.");
    }

    return null;
  }

  public bool IsItemRegistered(string id)
  {
    if (string.IsNullOrEmpty(id))
      return false;

    return _itemBaseModelCache.ContainsKey(id);
  }
}
