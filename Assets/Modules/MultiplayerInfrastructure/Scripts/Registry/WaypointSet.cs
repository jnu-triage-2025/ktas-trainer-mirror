using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Commons;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// 순서가 있는 waypoint 묶음이다. <see cref="_waypoints"/> 목록의 인덱스가 이동
  /// 순서이며, 각 waypoint는 독립된 waypoint로도 계속 레지스트리에 등록된다.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class WaypointSet : MonoBehaviour
  {
    [SerializeField] private string _identifier;

    private static readonly Dictionary<string, WaypointSet> _setsByIdentifier =
      new(StringComparer.Ordinal);
    [SerializeField] private List<WaypointAnchor> _waypoints = new();
    private string _registeredIdentifier;

    public string Identifier => _identifier;
    public IReadOnlyList<WaypointAnchor> Waypoints => _waypoints;

    public void ConfigureIdentifier(string identifier)
    {
      string trimmed = identifier?.Trim() ?? string.Empty;
      if (string.Equals(_identifier, trimmed, StringComparison.Ordinal))
        return;

      Unregister();
      _identifier = trimmed;
      Register();
    }

    /// <summary>이동 순서대로 waypoint 참조를 구성한다.</summary>
    public void ConfigureWaypoints(IEnumerable<WaypointAnchor> waypoints)
    {
      _waypoints.Clear();
      if (waypoints == null)
        return;

      foreach (var waypoint in waypoints)
      {
        if (waypoint != null)
          _waypoints.Add(waypoint);
      }
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
      _waypoints.RemoveAll(waypoint => waypoint == null);
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
  }
}
