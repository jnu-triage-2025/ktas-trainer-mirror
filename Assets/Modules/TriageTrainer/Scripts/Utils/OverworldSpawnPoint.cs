using UnityEngine;
using MultiplayerInfrastructure.FishNetSupports;
using MultiplayerInfrastructure.Registry;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TriageTrainer.Utils
{
  public sealed class OverworldSpawnPoint : MonoBehaviour, IPlayerSpawnPointProvider
  {
    [SerializeField] private string _identifier;

    private string _registeredIdentifier;

    public string Identifier => _identifier;
    public Transform SpawnTransform => transform;
    public bool IsAvailable => isActiveAndEnabled && gameObject.activeInHierarchy;

    public void SetIdentifier(string identifier)
    {
      _identifier = identifier;
    }

    private void OnEnable()
    {
      RegisterToRegistry();
      PlayerSpawnPointRegistry.Register(this);
    }

    private void OnDisable()
    {
      PlayerSpawnPointRegistry.Unregister(this);
      UnregisterFromRegistry();
    }

    private void OnDestroy()
    {
      UnregisterFromRegistry();
    }

    private void RegisterToRegistry()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        return;

      _registeredIdentifier = _identifier;
      Registry.Register(RegistryType.SpawnPoint, _registeredIdentifier, transform);
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      Registry.Unregister(RegistryType.SpawnPoint, _registeredIdentifier);
      _registeredIdentifier = null;
    }

    private void OnDrawGizmos()
    {
      DrawMarker();
    }

    private void OnDrawGizmosSelected()
    {
      DrawMarker();
    }

    private void DrawMarker()
    {
      Gizmos.color = new Color(0.2f, 0.95f, 0.35f, 0.95f);
      Gizmos.DrawSphere(transform.position, 0.16f);

      Gizmos.color = new Color(0.2f, 0.95f, 0.35f, 0.45f);
      Gizmos.DrawWireSphere(transform.position, 0.5f);

#if UNITY_EDITOR
      var label = string.IsNullOrWhiteSpace(_identifier) ? "SpawnPoint" : _identifier;
      Handles.color = new Color(0.12f, 0.42f, 0.16f, 1f);
      Handles.Label(transform.position + Vector3.up * 0.25f, label);
#endif
    }
  }
}
