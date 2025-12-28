using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public class WaypointRegistry : NetworkBehaviour
  {
    public static WaypointRegistry Instance { get; private set; }

    [SerializeField] private WaypointRegistryDataSO dataContainer;
    [SerializeField] private WaypointRequirements[] sceneEntries;

    private Dictionary<string, Vector3> _runtimeDict;

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
      _runtimeDict = new Dictionary<string, Vector3>();
      foreach (var entry in dataContainer.Entries)
      {
        _runtimeDict[entry.Identifier] = entry.Position;
      }
      foreach (var entry in sceneEntries)
      {
        _runtimeDict[entry.Identifier] = entry.Position;
      }
    }

    public Vector3? GetWaypointPosition(string identifier)
    {
      if (_runtimeDict != null && _runtimeDict.TryGetValue(identifier, out var position))
      {
        return position;
      }
      return null;
    }
  }
}
