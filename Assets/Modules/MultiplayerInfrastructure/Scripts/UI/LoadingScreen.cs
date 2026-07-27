using System;
using System.Collections;
using MultiplayerInfrastructure.Definitions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 씬과 카메라의 수명과 무관하게 표시되는 전역 로딩 화면입니다.
  /// 여러 비동기 작업이 겹쳐도 마지막 작업이 끝날 때까지 화면을 유지합니다.
  /// </summary>
  [DefaultExecutionOrder(-2000)]
  public sealed class LoadingScreen : MonoBehaviour
  {
    private static LoadingScreen _instance;
    private static int _activeOperations;
    private static string _message = DefaultsLoadingScreen.GenericLoadingMessage;
    private static float? _progress;
    private static int _hideAfterFrame = -1;
    private static bool _sceneTransitionInProgress;

    /// <summary>로딩 상태를 시작하고, Dispose 시 해당 작업을 완료합니다.</summary>
    public static IDisposable Begin(string message = DefaultsLoadingScreen.GenericLoadingMessage)
    {
      EnsureInstance();
      _activeOperations++;
      _message = string.IsNullOrWhiteSpace(message) ? DefaultsLoadingScreen.GenericLoadingMessage : message;
      _progress = null;
      _hideAfterFrame = -1;
      return new Operation();
    }

    /// <summary>현재 로딩 화면의 안내 문구와 진행률을 갱신합니다.</summary>
    public static void Report(string message, float? progress = null)
    {
      if (_activeOperations == 0)
        return;

      if (!string.IsNullOrWhiteSpace(message))
        _message = message;
      _progress = progress.HasValue ? Mathf.Clamp01(progress.Value) : null;
    }

    /// <summary>
    /// 단일 씬 전환을 시작합니다. 코루틴은 이 영구 오브젝트가 실행하므로
    /// 출발 씬의 UI가 파괴되어도 로딩 작업의 종료 처리가 보장됩니다.
    /// </summary>
    public static void LoadSceneAsync(string sceneName, string message = null)
    {
      if (_sceneTransitionInProgress)
        return;

      EnsureInstance();
      _sceneTransitionInProgress = true;
      _instance.StartCoroutine(LoadSceneRoutine(
        sceneName,
        string.IsNullOrWhiteSpace(message) ? DefaultsLoadingScreen.GetSceneLoadingMessage(sceneName) : message));
    }

    private static IEnumerator LoadSceneRoutine(string sceneName, string message)
    {
      using (Begin(message))
      {
        try
        {
          var operation = SceneManager.LoadSceneAsync(sceneName);
          if (operation == null)
          {
            Debug.LogWarning($"[LoadingScreen] Failed to start scene load for '{sceneName}'.");
            yield break;
          }

          while (!operation.isDone)
          {
            // Unity AsyncOperation은 활성화 직전 0.9에서 멈출 수 있어 사용자에게는 100%로 보이지 않게 한다.
            Report(message, Mathf.Clamp01(operation.progress / 0.9f) * 0.99f);
            yield return null;
          }
        }
        finally
        {
          _sceneTransitionInProgress = false;
        }
      }
    }

    private static void EnsureInstance()
    {
      if (_instance != null)
        return;

      var existing = FindFirstObjectByType<LoadingScreen>();
      if (existing != null)
      {
        _instance = existing;
        return;
      }

      var go = new GameObject("LoadingScreen");
      _instance = go.AddComponent<LoadingScreen>();
    }

    private void Awake()
    {
      if (_instance != null && _instance != this)
      {
        Destroy(gameObject);
        return;
      }

      _instance = this;
      DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
      if (_hideAfterFrame >= 0 && Time.frameCount >= _hideAfterFrame)
      {
        _activeOperations = 0;
        _progress = null;
        _hideAfterFrame = -1;
      }
    }

    private void OnGUI()
    {
      if (_activeOperations <= 0)
        return;

      var previousDepth = GUI.depth;
      GUI.depth = -1000;
      var previousColor = GUI.color;
      GUI.color = new Color(0f, 0f, 0f, 0.88f);
      GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
      GUI.color = previousColor;

      const float width = 440f;
      const float height = 100f;
      var box = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
      var style = new GUIStyle(GUI.skin.label)
      {
        alignment = TextAnchor.MiddleCenter,
        fontSize = 22,
        normal = { textColor = Color.white }
      };
      GUI.Label(new Rect(box.x, box.y, box.width, 38f), _message, style);

      if (_progress.HasValue)
      {
        var progressRect = new Rect(box.x + 24f, box.y + 58f, box.width - 48f, 18f);
        GUI.Box(progressRect, GUIContent.none);
        GUI.color = new Color(0.25f, 0.72f, 1f, 1f);
        GUI.DrawTexture(new Rect(progressRect.x + 2f, progressRect.y + 2f,
          (progressRect.width - 4f) * _progress.Value, progressRect.height - 4f), Texture2D.whiteTexture);
        GUI.color = previousColor;
      }

      GUI.depth = previousDepth;

      if (Event.current != null && Event.current.isMouse)
        Event.current.Use();
    }

    private sealed class Operation : IDisposable
    {
      private bool _disposed;

      public void Dispose()
      {
        if (_disposed)
          return;

        _disposed = true;
        _activeOperations = Mathf.Max(0, _activeOperations - 1);
        if (_activeOperations == 0 && _instance != null)
          // 새 씬의 Start()가 추가 초기화를 등록할 기회를 한 프레임 제공한다.
          _hideAfterFrame = Time.frameCount + 1;
      }
    }
  }
}
