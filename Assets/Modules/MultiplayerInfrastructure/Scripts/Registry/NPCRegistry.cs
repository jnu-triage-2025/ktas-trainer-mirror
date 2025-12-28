using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public class NPCRegistry : NetworkBehaviour
  {
    public static NPCRegistry Instance { get; private set; }

    [SerializeField] private NPCRegistryDataSO dataContainer;
    [SerializeField] private NPCRegistryRequirements[] sceneEntries;

    private Dictionary<string, GameObject> _runtimeDict;

    private void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
      }
      else
      {
        Destroy(gameObject);
      }
    }

    public override void OnStartServer()
    {
      base.OnStartServer();
      Initialize();
    }

    private void Initialize()
    {
      _runtimeDict = new Dictionary<string, GameObject>();
      foreach (var entry in dataContainer.Entries)
      {
        if (entry.GameObjectRef != null)
        {
          _runtimeDict[entry.Identifier] = entry.GameObjectRef;
        }
      }
      foreach (var entry in sceneEntries)
      {
        if (entry.GameObjectRef != null)
        {
          _runtimeDict[entry.Identifier] = entry.GameObjectRef;
        }
      }
    }

    public GameObject GetNPC(string identifier)
    {
      if (_runtimeDict != null && _runtimeDict.TryGetValue(identifier, out var go))
      {
        return go;
      }
      return null;
    }

    public void RegisterSceneNPC(string identifier, GameObject go)
    {
      if (_runtimeDict == null)
      {
        _runtimeDict = new Dictionary<string, GameObject>();
      }
      _runtimeDict[identifier] = go;
    }
  }
}
