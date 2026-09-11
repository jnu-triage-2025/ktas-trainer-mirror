using System.Collections;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.FishNetSupports;
using MultiplayerInfrastructure.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Session
{
  /// <summary>
  /// 게임 중 타이틀 복귀 절차(세션 종료 대기 후 IntroScene 로드)를 실행합니다.
  ///
  /// 이 절차를 요청한 UI(Esc 메뉴 등)는 씬 NetworkObject의 자식이므로, 클라이언트 연결이
  /// 끊기는 순간 FishNet이 디스폰하면서 비활성화되고 그 위에서 돌던 코루틴도 함께 멈춥니다.
  /// 그러면 세션은 끊겼는데 IntroScene 로드는 시작되지 않은 채 연결 실패 화면만 남습니다.
  /// 따라서 절차는 씬과 네트워크 수명에 영향을 받지 않는 영구 오브젝트에서 실행하고,
  /// 진행 중임을 <see cref="IsReturning"/> 으로 알려 연결 실패 화면이 의도적인 종료를
  /// 장애로 표시하지 않게 합니다.
  /// </summary>
  public static class TitleReturnService
  {
    private static TitleReturnRunner _runner;

    /// <summary>타이틀 복귀 절차가 시작되어 IntroScene 진입을 마치기 전까지 참입니다.</summary>
    public static bool IsReturning => _runner != null && _runner.IsRunning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration()
    {
      _runner = null;
    }

    /// <summary>
    /// 현재 세션을 종료하고 타이틀 씬으로 이동합니다. 이미 진행 중이면 다시 시작하지 않습니다.
    /// </summary>
    /// <returns>새 절차를 시작했으면 true.</returns>
    public static bool ReturnToTitle(string introSceneName = DefaultsSceneControl.IntroSceneName)
    {
      if (IsReturning)
        return false;

      if (_runner == null)
      {
        var runnerObject = new GameObject("TitleReturnRunner");
        Object.DontDestroyOnLoad(runnerObject);
        _runner = runnerObject.AddComponent<TitleReturnRunner>();
      }

      _runner.Begin(string.IsNullOrWhiteSpace(introSceneName) ? DefaultsSceneControl.IntroSceneName : introSceneName);
      return true;
    }
  }

  internal sealed class TitleReturnRunner : MonoBehaviour
  {
    // IntroScene 로드 요청 뒤 실제 진입을 기다리는 상한. 넘기면 상태만 정리한다.
    private const float IntroSceneWaitTimeoutSeconds = 15f;

    public bool IsRunning { get; private set; }

    public void Begin(string introSceneName)
    {
      IsRunning = true;
      StartCoroutine(Run(introSceneName));
    }

    private IEnumerator Run(string introSceneName)
    {
      try
      {
        // 세션이 끊기는 동안 월드가 그대로 보이지 않도록 로딩 화면으로 덮는다.
        using (LoadingScreen.Begin(DefaultsLoadingScreen.GetSceneLoadingMessage(introSceneName)))
        {
          var fishNetSupport = FishNetSupport.Instance ?? FindFirstObjectByType<FishNetSupport>();
          if (fishNetSupport != null)
            yield return fishNetSupport.StopSessionAndWait();

          LoadingScreen.LoadSceneAsync(introSceneName);
        }

        float deadline = Time.realtimeSinceStartup + IntroSceneWaitTimeoutSeconds;
        while (SceneManager.GetActiveScene().name != introSceneName && Time.realtimeSinceStartup < deadline)
          yield return null;
      }
      finally
      {
        IsRunning = false;
      }
    }
  }
}
