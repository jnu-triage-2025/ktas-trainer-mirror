using System.Collections;
using System;
using FishNet;
using FishNet.Managing.Client;
using FishNet.Transporting;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.FishNetSupports;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using RegistryStore = MultiplayerInfrastructure.Registry.Registry;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace TriageTrainer.SceneBootstrapper
{
  /// <summary>
  /// 사전에 애디티브 로드된 네트워크 세션 종료 씬의 UXML/USS UI 컨트롤러입니다.
  /// 모든 세션 부트스트래퍼가 같은 씬을 미리 로드하므로, 장애 표시 시 추가 씬 로딩이 없습니다.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class IndevConnectionFailureOverlay : MonoBehaviour
  {
    private const string LogPrefix = "[NetworkSessionFailure]";
    private const string HiddenClass = "is-hidden";

    private UIDocument _document;
    private VisualElement _screen;
    private Label _title;
    private Label _detail;
    private Label _logPath;
    private Button _titleButton;
    private bool _connectionAttempted;
    private bool _connected;
    private string _latestError;
    private string _endpoint = "127.0.0.1:7777";
    private Coroutine _subscriptionRoutine;

    private void Awake()
    {
      _document = GetComponent<UIDocument>();
      Application.logMessageReceivedThreaded += OnUnityLogMessage;
      BindDocument();
      Hide();
    }

    private void OnEnable()
    {
      BindDocument();
      _subscriptionRoutine = StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
      if (_subscriptionRoutine != null)
        StopCoroutine(_subscriptionRoutine);
      Unsubscribe();
    }

    private void OnDestroy()
    {
      Application.logMessageReceivedThreaded -= OnUnityLogMessage;
      if (_titleButton != null)
        _titleButton.clicked -= HandleTitleButtonClicked;
    }

    /// <summary>선택된 네트워크 엔드포인트에 대한 연결 시도를 시작했음을 표시합니다.</summary>
    public void BeginConnectionAttempt(string address, ushort port)
    {
      _connectionAttempted = true;
      _connected = false;
      _latestError = null;
      _endpoint = $"{address}:{port}";
      Hide();
    }

    /// <summary>세션 시작 API 자체가 실패했을 때 즉시 오류 화면을 표시합니다.</summary>
    public void ShowConnectionError(string message)
    {
      ShowFailure(string.IsNullOrWhiteSpace(message) ? "서버에 연결할 수 없습니다." : message);
    }

    private IEnumerator SubscribeWhenReady()
    {
      while (InstanceFinder.ClientManager == null)
        yield return null;

      InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
      InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
      _subscriptionRoutine = null;
    }

    private void Unsubscribe()
    {
      if (InstanceFinder.ClientManager != null)
        InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
      if (args.ConnectionState == LocalConnectionState.Starting)
      {
        _connectionAttempted = true;
        return;
      }

      if (args.ConnectionState == LocalConnectionState.Started)
      {
        _connectionAttempted = true;
        _connected = true;
        Hide();
        return;
      }

      if (args.ConnectionState != LocalConnectionState.Stopped || !_connectionAttempted)
        return;

      if (_connected)
      {
        ShowFailure("서버가 종료되었습니다.");
        return;
      }

      ShowFailure(string.IsNullOrWhiteSpace(_latestError)
        ? "서버에 연결할 수 없습니다."
        : $"오류가 발생했습니다: {_latestError}");
    }

    private void OnUnityLogMessage(string condition, string stackTrace, LogType type)
    {
      if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
        return;

      if (!_connectionAttempted || string.IsNullOrWhiteSpace(condition) || condition.StartsWith(LogPrefix) || !IsNetworkDiagnostic(condition))
        return;

      _latestError ??= string.IsNullOrWhiteSpace(stackTrace)
        ? condition
        : $"{condition}\n{stackTrace}";
    }

    private static bool IsNetworkDiagnostic(string condition)
    {
      return condition.IndexOf("FishNet", StringComparison.OrdinalIgnoreCase) >= 0
        || condition.IndexOf("Tugboat", StringComparison.OrdinalIgnoreCase) >= 0
        || condition.IndexOf("transport", StringComparison.OrdinalIgnoreCase) >= 0
        || condition.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0
        || condition.IndexOf("connect", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void ShowFailure(string reason)
    {
      string safeReason = string.IsNullOrWhiteSpace(reason) ? "알 수 없는 연결 오류" : reason.Trim();
      string diagnostic = string.IsNullOrWhiteSpace(_latestError)
        ? safeReason
        : $"{safeReason}; latestError={_latestError}";
      string dump = $"{LogPrefix} endpoint={_endpoint}; reason={diagnostic}";

      if (!GameLogService.IsInitialized)
      {
        GameSessionService.EnsureInitialized();
        GameLogService.Initialize("client");
      }
      GameLogService.WriteSystem(dump);
      Debug.LogError(dump);

      BindDocument();
      if (_screen == null)
        return;

      if (_title == null || _detail == null || _logPath == null)
      {
        Debug.LogError($"{LogPrefix} Failure UXML is missing one or more required labels.");
        return;
      }

      _title.text = "서버 연결이 종료되었습니다";
      _detail.text = safeReason;
      _logPath.text = $"자세한 내용은 세션 로그를 참조하세요.\n{GameLogService.CurrentLogFilePath ?? "로그 경로를 확인할 수 없습니다."}";
      _screen.RemoveFromClassList(HiddenClass);

      if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
        SceneManager.SetActiveScene(gameObject.scene);
    }

    private void Hide()
    {
      if (_screen != null)
        _screen.AddToClassList(HiddenClass);
    }

    private void BindDocument()
    {
      if (_document == null)
        _document = GetComponent<UIDocument>();

      var root = _document != null ? _document.rootVisualElement : null;
      if (root == null)
        return;

      _screen = root.Q<VisualElement>("failure-screen");
      _title = root.Q<Label>("failure-title");
      _detail = root.Q<Label>("failure-detail");
      _logPath = root.Q<Label>("failure-log-path");
      var titleButton = root.Q<Button>("failure-title-button");
      if (_titleButton != titleButton)
      {
        if (_titleButton != null)
          _titleButton.clicked -= HandleTitleButtonClicked;
        _titleButton = titleButton;
        if (_titleButton != null)
          _titleButton.clicked += HandleTitleButtonClicked;
      }
    }

    private void HandleTitleButtonClicked()
    {
      var fishNetSupport = FishNetSupport.Instance ?? FindFirstObjectByType<FishNetSupport>();
      if (fishNetSupport != null)
      {
        fishNetSupport.StopClient();
        if (RegistryStore.Get<bool>(RegistryType.RuntimeState, RegistryGlobalKeys.IsOpeningServer))
          fishNetSupport.StopServer();
      }

      LoadingScreen.LoadSceneAsync(DefaultsSceneControl.IntroSceneName);
    }
  }
}
