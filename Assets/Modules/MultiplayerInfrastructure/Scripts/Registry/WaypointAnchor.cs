using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MultiplayerInfrastructure.Registry
{
  public class WaypointAnchor : MonoBehaviour
  {
    [SerializeField] private string identifier;
    private string _registeredIdentifier;

    public string Identifier => identifier;

    private void Awake()
    {
      RegisterToRegistry();
    }

    private void OnEnable()
    {
      RegisterToRegistry();
    }

    private void OnDisable()
    {
      UnregisterFromRegistry();
    }

    private void OnDestroy()
    {
      UnregisterFromRegistry();
    }

    private void OnValidate()
    {
      if (string.IsNullOrWhiteSpace(identifier))
      {
        identifier = gameObject.name;
      }
    }

    private void RegisterToRegistry()
    {
      if (string.IsNullOrWhiteSpace(identifier))
        identifier = gameObject.name;

      if (string.IsNullOrWhiteSpace(identifier))
        return;

      _registeredIdentifier = identifier;
      Registry.Register(RegistryType.Waypoint, _registeredIdentifier, transform.position);
      Registry.Register(RegistryType.InteractableEntity, _registeredIdentifier, transform.position);
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      Registry.Unregister(RegistryType.Waypoint, _registeredIdentifier);
      Registry.Unregister(RegistryType.InteractableEntity, _registeredIdentifier);
      _registeredIdentifier = null;
    }

    private void OnDrawGizmos()
    {
      DrawAnchorGizmo();
    }

    private void OnDrawGizmosSelected()
    {
      DrawAnchorGizmo();
    }

    private void DrawAnchorGizmo()
    {
      Gizmos.color = Color.yellow;
      Gizmos.DrawSphere(transform.position, 0.2f);

#if UNITY_EDITOR
      var label = string.IsNullOrWhiteSpace(identifier) ? "(unset)" : identifier;
      Handles.color = Color.black;
      Handles.Label(transform.position + Vector3.up * 0.25f, label);
#endif
    }
  }
}
