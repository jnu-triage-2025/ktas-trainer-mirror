using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.UI
{
  public static class UIRuntimeEventSystemGuard
  {
    private static bool _bootstrapped;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
      if (_bootstrapped)
        return;

      _bootstrapped = true;
      SceneManager.sceneLoaded += OnSceneLoaded;
      EnsureValidEventSystem();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      EnsureValidEventSystem();
    }

    private static void EnsureValidEventSystem()
    {
      EventSystem current = ResolveEventSystem();
      if (current == null)
        return;

      if (!current.enabled)
        current.enabled = true;

      var inputModule = current.GetComponent<InputSystemUIInputModule>()
        ?? current.gameObject.AddComponent<InputSystemUIInputModule>();

      if (!inputModule.enabled)
        inputModule.enabled = true;

      bool hasValidBindings =
        inputModule.point.action != null &&
        inputModule.leftClick.action != null &&
        inputModule.submit.action != null &&
        inputModule.cancel.action != null;

      if (inputModule.actionsAsset == null || !hasValidBindings)
        inputModule.AssignDefaultActions();

    }

    private static EventSystem ResolveEventSystem()
    {
      var current = EventSystem.current;
      if (current != null)
        return current;

      var allSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      if (allSystems != null && allSystems.Length > 0)
      {
        var activeScene = SceneManager.GetActiveScene();
        for (int i = 0; i < allSystems.Length; i++)
        {
          if (allSystems[i] != null && allSystems[i].gameObject.scene == activeScene)
            return allSystems[i];
        }

        return allSystems[0];
      }

      var go = new GameObject("EventSystem");
      return go.AddComponent<EventSystem>();
    }
  }
}
