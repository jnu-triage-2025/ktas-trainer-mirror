using System;
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
  /// 출력 장치에 대한 참고: Unity 6에는 오디오 출력 경로를 고르는 API가 없습니다.
  /// 그래서 출력 선택은 저장과 표시까지만 담당하고, 실제 재생 경로는 운영체제 설정을 따릅니다.
  /// 입력 장치는 <c>Microphone.Start</c>에 넘길 이름을 정하므로 그대로 반영됩니다.
  ///
  /// 씬에는 하나만 두세요(TexturePerformanceService와 같은 시스템 오브젝트를 권장).
  /// </summary>
  public class AudioDevicePreferenceService : MonoBehaviour
  {
    private const string PlayerPrefsKey = "MultiplayerInfrastructure.AudioDeviceSettings.v1";

    private AudioDeviceSettingsData _currentSettings = new AudioDeviceSettingsData();

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
      Registry.Registry.Unregister(
        RegistryType.Service,
        Registry.Registry.TypeKey<AudioDevicePreferenceService>());
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

      _currentSettings = settings.Clone();
      _currentSettings.Sanitize();
      Save(_currentSettings);

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
      if (notify)
        OnSettingsChanged?.Invoke(CurrentSettings);
      if (inputChanged)
        RaiseInputDeviceChanged();
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
