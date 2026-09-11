using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 씬마다 EventSystem 과 InputSystemUIInputModule 이 정확히 하나씩 살아 있고, 그 모듈의 UI 액션이
  /// 실제로 활성 상태인지 보장한다.
  ///
  /// InputSystemUIInputModule.AssignDefaultActions 가 쓰는 기본 액션 에셋은 모듈들이 공유하는 정적
  /// 객체이며, 그 에셋을 쓰는 모듈 하나가 비활성화될 때 Destroy 된다(UnassignActions). 씬 전환으로 두
  /// 모듈이 잠시 겹치면 살아남은 모듈의 액션까지 폐기되고, 그 뒤 AssignDefaultActions 를 다시 불러도
  /// 이전 액션이 이미 꺼져 있어서 새 액션은 활성화되지 않는다. 그러면 커서·오버레이 상태는 모두 정상인데
  /// 클릭만 UI 에 전달되지 않는 상태가 되며 로그에도 남지 않는다. 이 가드는 자기만 소유하는 액션 에셋을
  /// 모듈에 연결하고, 액션이 꺼져 있으면 모듈을 다시 켜서 활성화한다.
  /// </summary>
  public static class UIRuntimeEventSystemGuard
  {
    private const string LogPrefix = "[UIRuntimeEventSystemGuard]";

    private static bool _bootstrapped;
    private static UIRuntimeEventSystemRecoveryDriver _recoveryDriver;

    // 가드가 관리하는 모든 모듈이 공유하며 절대 폐기하지 않는 액션 에셋.
    private static DefaultInputActions _guardActions;

    // 복구가 반복될 때 경고가 매 프레임 쌓이지 않도록 기록 간격을 제한한다.
    private const float RepairWarningIntervalSeconds = 2f;
    private static float _lastRepairWarningTime = float.NegativeInfinity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration()
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
      _bootstrapped = false;
      _recoveryDriver = null;
      _guardActions = null;
      _lastRepairWarningTime = float.NegativeInfinity;
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

    /// <summary>
    /// 지금 즉시 EventSystem 과 UI 입력 모듈을 점검·복구한다. 씬 로드 밖에서 포인터 입력이 꼭 필요한
    /// 화면(연결 실패 화면 등)이 호출한다.
    /// </summary>
    public static void EnsureNow()
    {
      EnsureRecoveryDriver();
      EnsureValidEventSystem();
    }

    private static void EnsureRecoveryDriver()
    {
      if (_recoveryDriver != null)
        return;

      var go = new GameObject("UI EventSystem Recovery");
      Object.DontDestroyOnLoad(go);
      _recoveryDriver = go.AddComponent<UIRuntimeEventSystemRecoveryDriver>();
    }

    private static DefaultInputActions GuardActions
    {
      get
      {
        // 도메인 리로드 없이 재생을 반복하면 에셋만 파괴된 채 래퍼가 남을 수 있다.
        if (_guardActions != null && _guardActions.asset == null)
          _guardActions = null;

        return _guardActions ??= new DefaultInputActions();
      }
    }

    private static void EnsureValidEventSystem()
    {
      EventSystem current = ResolveEventSystem();
      if (current == null)
        return;

      if (!current.enabled)
        current.enabled = true;

      var inputModule = current.GetComponent<InputSystemUIInputModule>();
      bool created = inputModule == null;
      if (created)
        inputModule = current.gameObject.AddComponent<InputSystemUIInputModule>();

      if (!inputModule.enabled)
        inputModule.enabled = true;

      if (IsInputModuleHealthy(inputModule))
        return;

      RepairInputModule(inputModule, created);
    }

    /// <summary>
    /// 현재 EventSystem 의 UI 입력 모듈이 살아 있고 포인터 액션이 켜져 있는지 여부.
    /// </summary>
    internal static bool IsCurrentInputHealthy(EventSystem current)
    {
      if (current == null || !current.isActiveAndEnabled)
        return false;

      var inputModule = current.GetComponent<InputSystemUIInputModule>();
      return inputModule != null && inputModule.enabled && IsInputModuleHealthy(inputModule);
    }

    private static bool IsInputModuleHealthy(InputSystemUIInputModule inputModule)
    {
      // 폐기된 에셋은 UnityEngine.Object 비교로 null 이 된다.
      if (inputModule.actionsAsset == null || inputModule.actionsAsset != GuardActions.asset)
        return false;

      if (!IsActionReady(inputModule.point) || !IsActionReady(inputModule.leftClick)
          || !IsActionReady(inputModule.submit) || !IsActionReady(inputModule.cancel))
        return false;

      // 모듈이 켜져 있는데 액션이 꺼져 있으면 입력이 전달되지 않는다.
      if (inputModule.isActiveAndEnabled
          && (!inputModule.point.action.enabled || !inputModule.leftClick.action.enabled))
        return false;

      return true;
    }

    private static bool IsActionReady(InputActionReference reference)
      => reference != null && reference.action != null;

    private static void RepairInputModule(InputSystemUIInputModule inputModule, bool created)
    {
      var actions = GuardActions;
      inputModule.actionsAsset = actions.asset;
      inputModule.cancel = InputActionReference.Create(actions.UI.Cancel);
      inputModule.submit = InputActionReference.Create(actions.UI.Submit);
      inputModule.move = InputActionReference.Create(actions.UI.Navigate);
      inputModule.leftClick = InputActionReference.Create(actions.UI.Click);
      inputModule.rightClick = InputActionReference.Create(actions.UI.RightClick);
      inputModule.middleClick = InputActionReference.Create(actions.UI.MiddleClick);
      inputModule.point = InputActionReference.Create(actions.UI.Point);
      inputModule.scrollWheel = InputActionReference.Create(actions.UI.ScrollWheel);
      inputModule.trackedDeviceOrientation = InputActionReference.Create(actions.UI.TrackedDeviceOrientation);
      inputModule.trackedDevicePosition = InputActionReference.Create(actions.UI.TrackedDevicePosition);

      // 액션 참조를 바꾸는 것만으로는 이전 액션이 꺼져 있던 경우 새 액션이 켜지지 않는다.
      // 모듈을 다시 켜면 OnEnable 이 모든 액션을 연결하고 활성화한다.
      if (inputModule.isActiveAndEnabled)
      {
        inputModule.enabled = false;
        inputModule.enabled = true;
      }

      if (created)
      {
        Debug.Log($"{LogPrefix} UI 입력 모듈을 새로 만들고 액션을 연결했습니다. eventSystem={inputModule.gameObject.name}@{inputModule.gameObject.scene.name}");
        return;
      }

      if (Time.unscaledTime - _lastRepairWarningTime < RepairWarningIntervalSeconds)
        return;

      _lastRepairWarningTime = Time.unscaledTime;
      Debug.LogWarning($"{LogPrefix} UI 입력 액션이 꺼져 있거나 폐기되어 다시 연결했습니다. eventSystem={inputModule.gameObject.name}@{inputModule.gameObject.scene.name}, pointEnabled={inputModule.point?.action?.enabled}, clickEnabled={inputModule.leftClick?.action?.enabled}");
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
      if (current != null && current.gameObject.activeInHierarchy)
        return current;

      // 살아 있는 EventSystem 이 하나라도 있으면 그것을 쓴다. 언로드 중인 씬의 것이라도 새로 만들지
      // 않는다. 두 개가 겹치면 먼저 죽는 쪽이 공유 액션 에셋을 폐기해 남은 쪽의 입력을 끊기 때문이다.
      // 그 객체가 사라지면 복구 드라이버가 다음 프레임에 새로 만든다.
      for (int i = 0; i < allSystems.Length; i++)
      {
        var system = allSystems[i];
        if (system != null && system.gameObject.activeInHierarchy)
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
      if (_validationFrames > 0)
      {
        _validationFrames--;
        UIRuntimeEventSystemGuard.EnsureValidEventSystemForRecovery();
        return;
      }

      // EventSystem 이 사라졌거나, 남아 있어도 UI 액션이 꺼져 있으면 즉시 복구한다.
      // 액션 폐기는 예외를 내지 않으므로 매 프레임 확인하지 않으면 클릭 불가 상태가 그대로 남는다.
      if (!UIRuntimeEventSystemGuard.IsCurrentInputHealthy(EventSystem.current))
        UIRuntimeEventSystemGuard.EnsureValidEventSystemForRecovery();
    }
  }
}
