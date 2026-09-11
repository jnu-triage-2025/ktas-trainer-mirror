using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.UI
{
  public static class UIRuntimeEventSystemGuard
  {
    private static bool _bootstrapped;
    private static UIRuntimeEventSystemRecoveryDriver _recoveryDriver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration()
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
      _bootstrapped = false;
      _recoveryDriver = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
      if (_bootstrapped)
        return;

      _bootstrapped = true;
      SceneManager.sceneLoaded += OnSceneLoaded;
      EnsureRecoveryDriver();
      EnsureValidEventSystem();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      EnsureValidEventSystem();
      EnsureRecoveryDriver();
      _recoveryDriver.RequestValidation();
    }

    private static void EnsureRecoveryDriver()
    {
      if (_recoveryDriver != null)
        return;

      var go = new GameObject("UI EventSystem Recovery");
      Object.DontDestroyOnLoad(go);
      _recoveryDriver = go.AddComponent<UIRuntimeEventSystemRecoveryDriver>();
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
      var activeScene = SceneManager.GetActiveScene();
      var allSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

      // Single 씬 전환의 sceneLoaded 콜백에서는 EventSystem.current가 아직 이전 씬의
      // 종료 예정 객체를 가리킬 수 있다. 새 활성 씬의 EventSystem을 먼저 선택해야
      // 프레임 종료 후 IntroScene의 UI 입력이 사라지지 않는다.
      for (int i = 0; i < allSystems.Length; i++)
      {
        var system = allSystems[i];
        if (system != null && system.gameObject.scene == activeScene && system.gameObject.activeInHierarchy)
          return system;
      }

      var current = EventSystem.current;
      if (current != null && current.gameObject.scene.IsValid() && current.gameObject.scene.isLoaded)
        return current;

      for (int i = 0; i < allSystems.Length; i++)
      {
        var system = allSystems[i];
        if (system != null && system.gameObject.activeInHierarchy && system.gameObject.scene.isLoaded)
          return system;
      }

      var go = new GameObject("EventSystem");
      return go.AddComponent<EventSystem>();
    }

    internal static void EnsureValidEventSystemForRecovery()
    {
      EnsureValidEventSystem();
    }
  }

  [DisallowMultipleComponent]
  internal sealed class UIRuntimeEventSystemRecoveryDriver : MonoBehaviour
  {
    private int _validationFrames;

    public void RequestValidation()
    {
      // 씬 교체 프레임과 그 다음 프레임을 모두 확인한다. 이전 씬의 EventSystem이
      // 지연 파괴되는 실행 순서에서도 두 번째 검사에서 새 EventSystem을 복구한다.
      _validationFrames = 2;
    }

    private void LateUpdate()
    {
      if (_validationFrames <= 0 && EventSystem.current != null)
        return;

      UIRuntimeEventSystemGuard.EnsureValidEventSystemForRecovery();
      if (_validationFrames > 0)
        _validationFrames--;
    }
  }
}
