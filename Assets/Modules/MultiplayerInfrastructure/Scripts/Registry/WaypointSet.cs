using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Commons;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// 순서가 있는 waypoint 묶음이다. 자식 <see cref="WaypointAnchor"/>의 형제 순서가
  /// 이동 순서이며, 각 자식은 독립된 waypoint로도 계속 레지스트리에 등록된다.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class WaypointSet : MonoBehaviour
  {
    [SerializeField] private string _identifier;

    private static readonly Dictionary<string, WaypointSet> _setsByIdentifier =
      new(StringComparer.Ordinal);
    private readonly List<WaypointAnchor> _waypoints = new();
    private string _registeredIdentifier;

    public string Identifier => _identifier;
    public IReadOnlyList<WaypointAnchor> Waypoints
    {
      get
      {
        RebuildWaypointList();
        return _waypoints;
      }
    }

    public void ConfigureIdentifier(string identifier)
    {
      string trimmed = identifier?.Trim() ?? string.Empty;
      if (string.Equals(_identifier, trimmed, StringComparison.Ordinal))
        return;

      Unregister();
      _identifier = trimmed;
      Register();
    }

    public static bool TryGet(string identifier, out WaypointSet waypointSet)
    {
      waypointSet = null;
      return !string.IsNullOrWhiteSpace(identifier)
             && _setsByIdentifier.TryGetValue(identifier.Trim(), out waypointSet)
             && waypointSet != null;
    }

    private void Awake() => Register();
    private void OnEnable() => Register();
    private void OnDisable() => Unregister();
    private void OnDestroy() => Unregister();

    private void OnValidate()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = EntityId.Ensure(_identifier, gameObject, "waypoint-set");
      RebuildWaypointList();
    }

    private void Register()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        return;

      _registeredIdentifier = _identifier.Trim();
      _setsByIdentifier[_registeredIdentifier] = this;
    }

    private void Unregister()
    {
      if (!string.IsNullOrWhiteSpace(_registeredIdentifier)
          && _setsByIdentifier.TryGetValue(_registeredIdentifier, out var current)
          && ReferenceEquals(current, this))
        _setsByIdentifier.Remove(_registeredIdentifier);
      _registeredIdentifier = null;
    }

    private void RebuildWaypointList()
    {
      _waypoints.Clear();
      for (int i = 0; i < transform.childCount; i++)
      {
        var waypoint = transform.GetChild(i).GetComponent<WaypointAnchor>();
        if (waypoint != null)
          _waypoints.Add(waypoint);
      }
    }
  }
}
