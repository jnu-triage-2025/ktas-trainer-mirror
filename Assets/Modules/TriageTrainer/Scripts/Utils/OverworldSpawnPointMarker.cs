using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TriageTrainer.Utils
{
  public sealed class OverworldSpawnPointMarker : MonoBehaviour
  {
    [SerializeField] private string _identifier;

    public void SetIdentifier(string identifier)
    {
      _identifier = identifier;
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
