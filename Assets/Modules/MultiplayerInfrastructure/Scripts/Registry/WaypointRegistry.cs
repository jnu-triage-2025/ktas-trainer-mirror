using System;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public class WaypointRegistry : NetworkBehaviour
  {
    public static WaypointRegistry Instance { get; private set; }

    [SerializeField] private WaypointRegistryDataSO dataContainer;
    [SerializeField] private WaypointAnchor[] waypointAnchors;
    [SerializeField] private bool autoDiscoverAnchors = true;

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
      AddScriptableObjectEntries();
      AddAnchorEntries();
    }

    public Vector3? GetWaypointPosition(string identifier)
    {
      if (_runtimeDict != null && _runtimeDict.TryGetValue(identifier, out var position))
      {
        return position;
      }
      return null;
    }

    private void AddScriptableObjectEntries()
    {
      if (dataContainer == null || dataContainer.Entries == null)
      {
        return;
      }

      foreach (var entry in dataContainer.Entries)
      {
        if (string.IsNullOrWhiteSpace(entry.Identifier))
        {
          continue;
        }

        _runtimeDict[entry.Identifier] = entry.Position;
      }
    }

    private void AddAnchorEntries()
    {
      var anchors = GetAnchors();
      if (anchors == null)
      {
        return;
      }

      foreach (var anchor in anchors)
      {
        if (anchor == null)
        {
          continue;
        }

        var identifier = anchor.Identifier;
        if (string.IsNullOrWhiteSpace(identifier))
        {
          continue;
        }

        _runtimeDict[identifier] = anchor.transform.position;
      }
    }

    private WaypointAnchor[] GetAnchors()
    {
      if (waypointAnchors != null && waypointAnchors.Length > 0)
      {
        return waypointAnchors;
      }

      if (!autoDiscoverAnchors)
      {
        return null;
      }

      return FindObjectsOfType<WaypointAnchor>(true);
    }

#if UNITY_EDITOR
    private const float GizmoRadius = 0.2f;

    private void OnDrawGizmos()
    {
      DrawWaypointGizmos();
    }

    private void OnDrawGizmosSelected()
    {
      DrawWaypointGizmos();
    }

    private void DrawWaypointGizmos()
    {
      if (dataContainer?.Entries != null)
      {
        Gizmos.color = Color.cyan;
        foreach (var entry in dataContainer.Entries)
        {
          Gizmos.DrawSphere(entry.Position, GizmoRadius);
        }
      }

      var anchors = GetAnchors();
      if (anchors != null)
      {
        Gizmos.color = Color.yellow;
        foreach (var anchor in anchors)
        {
          if (anchor == null)
          {
            continue;
          }

          Gizmos.DrawSphere(anchor.transform.position, GizmoRadius);
        }
      }
    }
#endif
  }
}
