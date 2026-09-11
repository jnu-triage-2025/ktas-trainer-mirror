using PlayerPrefs = MultiplayerInfrastructure.Automation.ProfilePlayerPrefs;
using System;
using System.Collections;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 오디오 출력/입력 장치 선택을 저장하고 되살리는 서비스입니다.
  ///
  /// 저장 방식은 다른 설정과 같습니다. <see cref="AudioDeviceSettingsData"/>를
  /// <see cref="JsonUtility"/>로 직렬화해 <see cref="PlayerPrefs"/>에 넣습니다.
  ///
  /// 저장된 장치가 지금은 없을 때:
  ///   - 실제로 쓰이는 값(<see cref="ResolveEffectiveDeviceId"/>)은 곧바로 시스템 설정으로 돌아갑니다.
  ///   - 저장값 자체를 지우는 것은 장치 목록을 제대로 읽었을 때만입니다. 목록 읽기가 통째로
  ///     실패한 상황(권한 문제 등)에서 멀쩡한 사용자 선택을 날려버리지 않기 위해서입니다.
  ///
  /// 출력 장치는 <see cref="AudioOutputRouting"/>이 운영체제 쪽에서 경로를 돌려 줍니다.
  /// Unity에 출력 장치를 고르는 API가 없어서 플랫폼별 우회에 기대므로, 지원하지 않는
  /// 플랫폼이나 실패한 경우에는 저장만 남고 재생은 시스템 설정을 따릅니다.
  /// 입력 장치는 <c>Microphone.Start</c>에 넘길 이름을 정하므로 그대로 반영됩니다.
  ///
  /// 씬에는 하나만 두세요(TexturePerformanceService와 같은 시스템 오브젝트를 권장).
  /// </summary>
  public class AudioDevicePreferenceService : MonoBehaviour
  {
    private const string PlayerPrefsKey = "MultiplayerInfrastructure.AudioDeviceSettings.v1";

    /// <summary>재생이 끝나기를 기다려 주는 상한입니다. 이보다 길어지면 그냥 옮깁니다.</summary>
    private const float RestartWaitLimitSeconds = 8f;

    /// <summary>재생이 끝났는지 확인하는 간격입니다.</summary>
    private const float RestartPollSeconds = 0.25f;

    private Coroutine _pendingRestart;
    private bool _restartPending;

    private AudioDeviceSettingsData _currentSettings = new AudioDeviceSettingsData();

    /// <summary>
    /// 출력 경로를 실제로 옮기는 데 성공했는지입니다.
    /// 지원하지 않는 플랫폼이거나 비공개 API 호출이 실패하면 false로 남고,
    /// 설정 화면은 이 값을 보고 "저장만 되었다"는 안내를 띄웁니다.
    /// </summary>
    public bool IsOutputRoutingActive { get; private set; }

    /// <summary>출력 경로 변경이 실패했을 때의 사유입니다. 성공했거나 시도하지 않았으면 null입니다.</summary>
    public string OutputRoutingFailureReason { get; private set; }

    /// <summary>이 플랫폼에서 출력 경로를 바꿀 수 있으면 true입니다.</summary>
    public static bool IsOutputRoutingSupported => AudioOutputRouting.IsSupported;

    /// <summary>
    /// 새 장치로 옮기는 일을 재생이 끝날 때까지 미뤄 둔 상태면 true입니다.
    /// 설정 화면이 "곧 옮긴다"고 안내하는 데 씁니다.
    /// </summary>
    public bool IsOutputRestartPending => _restartPending;

    /// <summary>설정이 바뀔 때마다 최신 사본이 전달됩니다.</summary>
    public event Action<AudioDeviceSettingsData> OnSettingsChanged;

    /// <summary>
    /// 입력 장치 선택이 바뀌었을 때 발생합니다.
    ///
    /// 마이크를 쓰는 쪽은 서비스 인스턴스보다 먼저 만들어지기도 하고 씬을 넘나들기도 해서,
    /// 인스턴스를 붙잡지 않고도 구독할 수 있도록 정적 이벤트로 둡니다.
    /// 구독한 쪽은 반드시 해제까지 챙기세요.
    /// </summary>
    public static event Action InputDeviceChanged;

    /// <summary>현재 설정의 사본입니다.</summary>
    public AudioDeviceSettingsData CurrentSettings => _currentSettings.Clone();

    /// <summary>등록되어 있으면 서비스 인스턴스를, 아니면 null을 돌려줍니다.</summary>
    public static AudioDevicePreferenceService Instance
      => Registry.Registry.Get<AudioDevicePreferenceService>(
        RegistryType.Service, Registry.Registry.TypeKey<AudioDevicePreferenceService>());

    /// <summary>
    /// 장면에 배치된 서비스가 아직 로드되지 않았어도 설정 화면에서 바로 쓸 수 있게 합니다.
    ///
    /// 시작 화면에서는 SystemOverlayScene이 아직 추가 로드되지 않을 수 있습니다. 이때도
    /// 입력 장치 설정을 저장할 수 있도록, 필요한 경우 지속되는 런타임 서비스를 만듭니다.
    /// </summary>
    public static AudioDevicePreferenceService GetOrCreateInstance()
    {
      var service = Instance;
      if (service != null)
        return service;

      service = UnityEngine.Object.FindAnyObjectByType<AudioDevicePreferenceService>();
      if (service != null)
      {
        Registry.Registry.Register(
          RegistryType.Service,
          Registry.Registry.TypeKey<AudioDevicePreferenceService>(),
          service);
        return service;
      }

      var host = new GameObject(nameof(AudioDevicePreferenceService));
      UnityEngine.Object.DontDestroyOnLoad(host);
      return host.AddComponent<AudioDevicePreferenceService>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Unity 라이프사이클
    // ──────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
      Registry.Registry.Register(
        RegistryType.Service,
        Registry.Registry.TypeKey<AudioDevicePreferenceService>(),
        this);

      LoadAndReconcile();
    }

    private void OnDestroy()
    {
      CancelPendingRestart();
      if (Instance == this)
      {
        Registry.Registry.Unregister(
          RegistryType.Service,
          Registry.Registry.TypeKey<AudioDevicePreferenceService>());
      }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 공개 API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>저장값을 읽고 지금 장치 목록과 대조합니다. Awake에서 자동으로 불립니다.</summary>
    public void LoadAndReconcile()
    {
      AudioDeviceCatalog.Refresh();
      _currentSettings = Load();
      ReconcileAgainstCatalog(notify: false);

      // 저장값을 운영체제에 다시 알린다. 시작 시점에는 이미 그 장치로 열려 있으므로
      // 엔진을 다시 열지 않는다.
      ApplyOutputRouting(restartAudioEngine: false);

      Debug.Log("[AudioDevice] 장치 설정 불러오기: " +
                $"출력={DescribeSelection(AudioDeviceKind.Output)}, " +
                $"입력={DescribeSelection(AudioDeviceKind.Input)}");
    }

    /// <summary>운영체제에 장치 목록을 다시 묻고, 사라진 장치를 가리키던 설정을 정리합니다.</summary>
    public void RefreshDevices()
    {
      AudioDeviceCatalog.Refresh();
      ReconcileAgainstCatalog(notify: true);
    }

    /// <summary>한쪽 방향의 장치를 고릅니다. 빈 식별자를 넘기면 시스템 설정을 따릅니다.</summary>
    public void SetDevice(AudioDeviceKind kind, string deviceId)
    {
      var devices = AudioDeviceCatalog.GetDevices(kind);
      var descriptor = AudioDeviceSelectionResolver.Find(deviceId, devices);

      var next = _currentSettings.Clone();
      if (descriptor == null)
        next.SetDevice(kind, AudioDeviceSelectionResolver.SystemDefaultId, string.Empty);
      else
        next.SetDevice(kind, descriptor.Id, descriptor.DisplayName);

      SetSettings(next);
    }

    /// <summary>설정 전체를 갈아끼우고 저장합니다.</summary>
    public void SetSettings(AudioDeviceSettingsData settings)
    {
      if (settings == null)
        throw new ArgumentNullException(nameof(settings));

      string previousInputId = _currentSettings.InputDeviceId;
      string previousOutputId = _currentSettings.OutputDeviceId;

      _currentSettings = settings.Clone();
      _currentSettings.Sanitize();
      Save(_currentSettings);

      bool outputChanged = !string.Equals(previousOutputId, _currentSettings.OutputDeviceId, StringComparison.Ordinal);
      if (outputChanged)
        ApplyOutputRouting(restartAudioEngine: true);

      OnSettingsChanged?.Invoke(CurrentSettings);
      if (!string.Equals(previousInputId, _currentSettings.InputDeviceId, StringComparison.Ordinal))
        RaiseInputDeviceChanged();
      Debug.Log("[AudioDevice] 장치 설정 저장: " +
                $"출력={DescribeSelection(AudioDeviceKind.Output)}, " +
                $"입력={DescribeSelection(AudioDeviceKind.Input)}");
    }

    /// <summary>시스템 설정으로 되돌립니다.</summary>
    public void ResetToSystemDefault() => SetSettings(new AudioDeviceSettingsData());

    /// <summary>
    /// 시스템 설정으로 되돌리고 저장된 값도 지웁니다. 출력 장치가 바뀌면 오디오 엔진을 다시 여는 것까지
    /// <see cref="ResetToSystemDefault"/>가 맡으므로, 여기서는 그 뒤에 저장값만 정리합니다.
    /// </summary>
    public void ResetToDefault()
    {
      ResetToSystemDefault();
      ClearStoredValue();
    }

    /// <summary>저장된 장치 설정을 지웁니다.</summary>
    public static void ClearStoredValue()
    {
      PlayerPrefs.DeleteKey(PlayerPrefsKey);
      PlayerPrefs.Save();
    }

    /// <summary>
    /// 실제로 적용할 식별자입니다. 저장된 장치가 지금 목록에 없으면 시스템 설정을 뜻하는
    /// 빈 문자열이 나옵니다.
    /// </summary>
    public string ResolveEffectiveDeviceId(AudioDeviceKind kind)
      => AudioDeviceSelectionResolver.Reconcile(
        _currentSettings.GetDeviceId(kind), AudioDeviceCatalog.GetDevices(kind));

    /// <summary>
    /// <c>Microphone.Start</c>에 넘길 장치 이름입니다.
    /// 시스템 설정을 따르는 경우 null이 나오며, 이는 운영체제 기본 마이크를 뜻합니다.
    ///
    /// 서비스가 씬에 없어도 동작하도록 저장값을 직접 읽는 경로를 함께 둡니다.
    /// (마이크를 쓰는 쪽이 서비스보다 먼저 깨어나는 경우가 있습니다.)
    /// </summary>
    public static string ResolveInputDeviceName()
    {
      var service = Instance;
      string deviceId = service != null
        ? service.ResolveEffectiveDeviceId(AudioDeviceKind.Input)
        : AudioDeviceSelectionResolver.Reconcile(
          Load().InputDeviceId, AudioDeviceCatalog.GetDevices(AudioDeviceKind.Input));

      // 입력 장치는 식별자 자체가 Microphone 장치 이름이다.
      return AudioDeviceSelectionResolver.IsSystemDefault(deviceId) ? null : deviceId;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 내부 구현
    // ──────────────────────────────────────────────────────────────────────────
    private void ReconcileAgainstCatalog(bool notify)
    {
      bool outputChanged = ReconcileOne(AudioDeviceKind.Output);
      bool inputChanged = ReconcileOne(AudioDeviceKind.Input);

      if (!outputChanged && !inputChanged)
        return;

      Save(_currentSettings);

      // 사라진 장치를 정리한 결과이지 사용자가 고른 것이 아니다. 설정 창을 여는 것만으로
      // 재생이 끊기지 않도록, 여기서는 경로만 갱신하고 엔진은 그대로 둔다.
      if (outputChanged)
        ApplyOutputRouting(restartAudioEngine: false);
      if (notify)
        OnSettingsChanged?.Invoke(CurrentSettings);
      if (inputChanged)
        RaiseInputDeviceChanged();
    }

    /// <summary>지금 저장된 출력 선택을 운영체제 쪽에 반영합니다.</summary>
    private void ApplyOutputRouting(bool restartAudioEngine)
    {
      if (!AudioOutputRouting.IsSupported)
      {
        IsOutputRoutingActive = false;
        OutputRoutingFailureReason = null;
        return;
      }

      IsOutputRoutingActive = AudioOutputRouting.Apply(
        _currentSettings.OutputDeviceId, out string failureReason);
      OutputRoutingFailureReason = IsOutputRoutingActive ? null : failureReason;

      if (IsOutputRoutingActive && restartAudioEngine)
        ScheduleAudioEngineRestart();
    }

    /// <summary>
    /// 오디오 엔진 재시작을 소리가 잦아든 뒤로 미룹니다.
    ///
    /// <see cref="AudioSettings.Reset"/>은 재생 중인 소리를 끊고 마이크 클립까지 무효로 만듭니다.
    /// 환자 음성이 나가는 도중에 끊으면 교육생은 듣지 못했는데 TTS 코루틴은 클립 길이만큼
    /// 기다렸다가 정상 종료하므로, 시나리오는 말한 것처럼 진행되어 버립니다.
    /// 그래서 재생이 끝나기를 기다렸다가 옮깁니다.
    ///
    /// 하염없이 기다리지는 않습니다. <see cref="RestartWaitLimitSeconds"/>가 지나면 그냥 옮깁니다.
    /// </summary>
    private void ScheduleAudioEngineRestart()
    {
      // 비활성 상태에서는 코루틴을 돌릴 수 없으니 곧바로 옮긴다.
      if (!isActiveAndEnabled)
      {
        AudioOutputRouting.RestartAudioEngine();
        return;
      }

      CancelPendingRestart();
      _pendingRestart = StartCoroutine(RestartAudioEngineWhenQuiet());
    }

    private IEnumerator RestartAudioEngineWhenQuiet()
    {
      _restartPending = true;

      float deadline = Time.unscaledTime + RestartWaitLimitSeconds;
      while (Time.unscaledTime < deadline && AudioOutputRouting.IsAnyAudioPlaying())
        yield return new WaitForSecondsRealtime(RestartPollSeconds);

      bool cutOffPlayback = AudioOutputRouting.IsAnyAudioPlaying();

      _restartPending = false;
      _pendingRestart = null;
      AudioOutputRouting.RestartAudioEngine();

      if (cutOffPlayback)
        Debug.LogWarning($"[AudioDevice] {RestartWaitLimitSeconds:0}초를 기다려도 재생이 끝나지 않아 " +
                         "소리를 끊고 새 장치로 옮겼습니다.");
    }

    private void CancelPendingRestart()
    {
      if (_pendingRestart != null)
        StopCoroutine(_pendingRestart);

      _pendingRestart = null;
      _restartPending = false;
    }

    private static void RaiseInputDeviceChanged()
    {
      try
      {
        InputDeviceChanged?.Invoke();
      }
      catch (Exception exception)
      {
        // 구독자 하나가 넘어져도 설정 저장 자체는 이미 끝난 뒤여야 한다.
        Debug.LogWarning($"[AudioDevice] 입력 장치 변경 알림 처리 중 예외가 났습니다: {exception}");
      }
    }

    private bool ReconcileOne(AudioDeviceKind kind)
    {
      string storedId = _currentSettings.GetDeviceId(kind);
      if (AudioDeviceSelectionResolver.IsSystemDefault(storedId))
        return false;

      var devices = AudioDeviceCatalog.GetDevices(kind);

      // 목록을 통째로 못 읽은 상황과 "장치가 사라진" 상황을 구분한다.
      // 전자에서 사용자 선택을 지우면 다음 실행에서 되살릴 방법이 없다.
      if (devices == null || devices.Count == 0)
      {
        if (AudioDeviceCatalog.IsSupported(kind))
          Debug.LogWarning($"[AudioDevice] {KindLabel(kind)} 장치를 하나도 찾지 못해 저장된 선택을 그대로 둡니다.");
        return false;
      }

      if (AudioDeviceSelectionResolver.Find(storedId, devices) != null)
        return false;

      string lostName = _currentSettings.GetDeviceName(kind);
      _currentSettings.SetDevice(kind, AudioDeviceSelectionResolver.SystemDefaultId, string.Empty);
      Debug.Log($"[AudioDevice] 저장된 {KindLabel(kind)} 장치" +
                (string.IsNullOrEmpty(lostName) ? string.Empty : $" '{lostName}'") +
                "를 찾을 수 없어 시스템 설정으로 되돌렸습니다.");
      return true;
    }

    private string DescribeSelection(AudioDeviceKind kind)
    {
      string id = _currentSettings.GetDeviceId(kind);
      if (AudioDeviceSelectionResolver.IsSystemDefault(id))
        return AudioDeviceSelectionResolver.BuildSystemDefaultLabel(AudioDeviceCatalog.GetDevices(kind));

      string name = _currentSettings.GetDeviceName(kind);
      return string.IsNullOrEmpty(name) ? id : name;
    }

    private static string KindLabel(AudioDeviceKind kind)
      => kind == AudioDeviceKind.Output ? "출력" : "입력";

    private static void Save(AudioDeviceSettingsData settings)
    {
      PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(settings));
      PlayerPrefs.Save();
    }

    private static AudioDeviceSettingsData Load()
    {
      if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        return new AudioDeviceSettingsData();

      try
      {
        var loaded = JsonUtility.FromJson<AudioDeviceSettingsData>(PlayerPrefs.GetString(PlayerPrefsKey));
        if (loaded != null)
        {
          loaded.Sanitize();
          return loaded;
        }
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[AudioDevice] 저장된 장치 설정을 읽지 못해 시스템 설정으로 시작합니다: {exception.Message}");
      }

      return new AudioDeviceSettingsData();
    }
  }
}
