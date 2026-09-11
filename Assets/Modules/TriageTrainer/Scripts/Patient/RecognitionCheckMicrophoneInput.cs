using System;
using System.Collections;
using System.Collections.Generic;
using MultiplayerInfrastructure.Audio;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>마이크 음량이 임계치를 1초 이상 넘으면 활성 의식 확인을 완료한다.</summary>
  public sealed class RecognitionCheckMicrophoneInput : MonoBehaviour
  {
    public enum Availability
    {
      Pending,
      Ready,
      PermissionDenied,
      NoDevice,
      RecordingFailed
    }

    internal interface IAdapter
    {
      public bool HasPermission { get; }
      public string[] Devices { get; }

      /// <summary>
      /// 설정에서 고른 마이크 이름입니다. 고르지 않았으면 null입니다.
      /// 테스트가 운영체제를 건드리지 않고도 이 경로를 흉내낼 수 있도록 어댑터에 둡니다.
      /// </summary>
      public string PreferredDevice { get; }
      public IEnumerator RequestPermission();
      public AudioClip Start(string device, int frequency);
      public bool IsRecording(string device);
      public int GetPosition(string device);
      public void End(string device);
    }

    private sealed class UnityAdapter : IAdapter
    {
      public bool HasPermission => Application.HasUserAuthorization(UserAuthorization.Microphone);
      public string[] Devices => Microphone.devices;
      public string PreferredDevice => AudioDevicePreferenceService.ResolveInputDeviceName();
      public IEnumerator RequestPermission()
      {
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
      }
      public AudioClip Start(string device, int frequency) => Microphone.Start(device, true, 2, frequency);
      public bool IsRecording(string device) => Microphone.IsRecording(device);
      public int GetPosition(string device) => Microphone.GetPosition(device);
      public void End(string device) => Microphone.End(device);
    }

    private const float VolumeThreshold = 0.02f;

    // 음량 게이지가 가득 차는 기준 음량이다. 임계치의 10배로 두어, 확인이 진행되기 시작하는
    // 지점이 게이지의 10% 부근에 놓이고 보통 크기의 말소리가 중간 이상을 채우게 한다.
    private const float GaugeFullScaleVolume = VolumeThreshold * 10f;
    private const float RequiredDurationSeconds = 1f;
    private const int SampleCount = 256;
    private static RecognitionCheckMicrophoneInput _instance;
    private readonly HashSet<PatientController> _targets = new();
    private AudioClip _clip;
    private string _device;
    private float _aboveThresholdSeconds;
    private bool _recordingFailed;
    private readonly float[] _samples = new float[SampleCount];
    private IAdapter _adapter = new UnityAdapter();
    private Coroutine _retryRoutine;
    private int _retryGeneration;
    internal static event Action AvailabilityChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration()
    {
      _instance = null;
      AvailabilityChanged = null;
    }

    internal static Availability CurrentAvailability
    {
      get
      {
        EnsureCreated();
        return _instance.ResolveAvailability();
      }
    }

    internal static bool IsUnavailable => IsUnavailableState(CurrentAvailability);

    public static bool IsUnavailableState(Availability availability)
      => availability == Availability.PermissionDenied
         || availability == Availability.NoDevice
         || availability == Availability.RecordingFailed;

    public static string GetUnavailableGuidance(Availability availability)
    {
      return availability switch
      {
        Availability.PermissionDenied => "마이크 권한을 사용할 수 없습니다. 운영체제 설정에서 이 앱의 마이크 권한을 허용한 뒤 '마이크 다시 사용하기'를 선택해 주세요. 그동안 '말 걸기'로 진행할 수 있습니다.",
        Availability.NoDevice => "사용 가능한 마이크를 찾지 못했습니다. 마이크를 연결한 뒤 '마이크 다시 사용하기'를 선택해 주세요. 그동안 '말 걸기'로 진행할 수 있습니다.",
        Availability.RecordingFailed => "마이크 녹음을 시작하지 못했습니다. 다른 앱의 마이크 사용을 종료하거나 장치를 다시 연결한 뒤 '마이크 다시 사용하기'를 선택해 주세요. 그동안 '말 걸기'로 진행할 수 있습니다.",
        _ => string.Empty
      };
    }

    // 첫 씬이 표시되기 전에 마이크 접근 권한을 요청하여, 이후 게임 플레이가
    // 플랫폼 권한 대화 상자로 중단되지 않게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureCreated()
    {
      if (_instance != null)
        return;
      var host = new GameObject(nameof(RecognitionCheckMicrophoneInput));
      DontDestroyOnLoad(host);
      _instance = host.AddComponent<RecognitionCheckMicrophoneInput>();

      // 데디케이티드 서버에는 입력 장치와 권한 대화 상자가 없으므로 마이크 권한을 요청하지 않는다.
      if (MultiplayerInfrastructure.Server.DedicatedServerRuntime.IsActive)
        return;

      _instance.StartCoroutine(_instance.RequestPermissionEarly());
    }

    public static void StartMonitoring(PatientController target)
    {
      EnsureCreated();
      if (target != null)
        _instance._targets.Add(target);
      _instance.EnsureRecording();
    }

    public static void StopMonitoring(PatientController target)
    {
      if (_instance == null || target == null)
        return;
      _instance._targets.Remove(target);
      if (_instance._targets.Count == 0)
        _instance.StopRecording();
    }

    public static void Retry(Action<Availability> completed)
    {
      EnsureCreated();
      _instance._retryGeneration++;
      if (_instance._retryRoutine != null)
        _instance.StopCoroutine(_instance._retryRoutine);
      var callbackTarget = completed?.Target as UnityEngine.Object;
      _instance._retryRoutine = _instance.StartCoroutine(
        _instance.RetryRecording(_instance._retryGeneration, callbackTarget, completed));
    }

    internal static void SetAdapterForTests(IAdapter adapter)
    {
      EnsureCreated();
      _instance.StopRecording();
      _instance._adapter = adapter ?? new UnityAdapter();
      _instance._permissionRequestCompleted = false;
      _instance._recordingFailed = false;
    }

    private void EnsureRecording()
    {
      EnsureRecordingCore();
      SyncCaptureIndicator();
    }

    /// <summary>
    /// 실제로 마이크를 여는 부분이다. 열 수 없는 조건에서는 중간에 빠져나가므로,
    /// 표시 갱신은 이 메서드를 감싸는 <see cref="EnsureRecording"/> 에서 한 번만 한다.
    /// </summary>
    private void EnsureRecordingCore()
    {
      if (MultiplayerInfrastructure.Player.PlayerController.TryGetLocalVoiceInputLevel(out _))
        return;
      if (_clip != null || _targets.Count == 0
          || !_permissionRequestCompleted
          || _recordingFailed
          || !_adapter.HasPermission)
        return;

      // Devices는 부를 때마다 새 배열을 만든다. 검사한 것과 고르는 것이 같은 목록이어야
      // 그 사이에 장치가 빠져도 빈 배열을 인덱싱하지 않는다.
      var devices = _adapter.Devices;
      if (devices == null || devices.Length == 0)
        return;

      Availability before = ResolveAvailability();
      _device = ResolvePreferredDevice(devices, _adapter.PreferredDevice);
      try
      {
        _clip = _adapter.Start(_device, AudioSettings.outputSampleRate);
      }
      catch (System.Exception exception)
      {
        Debug.LogWarning($"[RecognitionCheckMicrophoneInput] Microphone recording failed: {exception.Message}");
      }

      if (_clip == null)
      {
        _recordingFailed = true;
        _device = null;
        NotifyAvailabilityChanged(before);
        return;
      }

      _aboveThresholdSeconds = 0f;
      NotifyAvailabilityChanged(before);
    }

    private void Update()
    {
      if (_targets.Count == 0)
        return;
      if (MultiplayerInfrastructure.Player.PlayerController.TryGetLocalVoiceInputLevel(out float sharedRms))
      {
        ProcessRms(sharedRms);
        return;
      }
      if (_clip == null)
      {
        EnsureRecording();
        return;
      }

      // 엔진 재시작 직후에는 클립이 무효인 채로 한 프레임이 지나갈 수 있다.
      // 읽기 전체를 같은 try로 감싸 예외 대신 재시도 가능한 실패 상태로 떨어뜨린다.
      int position;
      try
      {
        position = _adapter.GetPosition(_device);
        if (position < SampleCount)
          return;
        _clip.GetData(_samples, position - SampleCount);
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[RecognitionCheckMicrophoneInput] Microphone recording failed: {exception.Message}");
        MarkRecordingFailed();
        return;
      }
      float sum = 0f;
      for (int i = 0; i < _samples.Length; i++)
        sum += _samples[i] * _samples[i];
      float rms = Mathf.Sqrt(sum / _samples.Length);
      ProcessRms(rms);
    }

    private void ProcessRms(float rms)
    {
      MicrophoneCaptureIndicatorUIController.SetGaugeLevel(rms / GaugeFullScaleVolume);
      _aboveThresholdSeconds = rms >= VolumeThreshold
        ? _aboveThresholdSeconds + Time.unscaledDeltaTime
        : 0f;
      if (_aboveThresholdSeconds < RequiredDurationSeconds)
        return;

      var snapshot = new List<PatientController>(_targets);
      _aboveThresholdSeconds = 0f;
      foreach (var target in snapshot)
        target?.RequestRecognitionCheckCompletion(FindLocalPlayer(), microphone: true);
    }

    private static MultiplayerInfrastructure.Player.PlayerController FindLocalPlayer()
    {
      var players = FindObjectsByType<MultiplayerInfrastructure.Player.PlayerController>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);
      foreach (var player in players)
      {
        if (player != null && player.IsOwner)
          return player;
      }
      return null;
    }

    /// <summary>
    /// 설정에서 고른 마이크를 씁니다. 고르지 않았거나 그 장치가 지금 없으면
    /// 운영체제 기본 마이크(목록의 첫 장치)로 돌아갑니다.
    /// </summary>
    private static string ResolvePreferredDevice(string[] devices, string preferred)
    {
      if (!string.IsNullOrEmpty(preferred))
      {
        for (int i = 0; i < devices.Length; i++)
        {
          if (string.Equals(devices[i], preferred, StringComparison.Ordinal))
            return devices[i];
        }
      }

      return devices[0];
    }

    private void OnEnable()
    {
      AudioDevicePreferenceService.InputDeviceChanged += HandleInputDeviceChanged;
      AudioSettings.OnAudioConfigurationChanged += HandleAudioConfigurationChanged;
    }

    private void OnDisable()
    {
      AudioDevicePreferenceService.InputDeviceChanged -= HandleInputDeviceChanged;
      AudioSettings.OnAudioConfigurationChanged -= HandleAudioConfigurationChanged;
    }

    /// <summary>설정에서 마이크를 바꾸면 녹음을 끊고 새 장치로 다시 연다.</summary>
    private void HandleInputDeviceChanged() => RestartRecording();

    /// <summary>
    /// 오디오 엔진이 다시 열리면 <c>Microphone.Start</c>로 만든 클립이 무효가 된다.
    /// 출력 장치를 바꿔 <see cref="AudioSettings.Reset"/>이 불린 경우가 대표적이다.
    /// 잡고 있던 클립을 버리고 새 엔진 위에서 다시 열어야 의식 확인이 계속 동작한다.
    /// </summary>
    private void HandleAudioConfigurationChanged(bool deviceWasChanged) => RestartRecording();

    /// <summary>녹음을 끊고 다시 연다. 열 수 없는 상태면 EnsureRecording이 알아서 걸러 낸다.</summary>
    private void RestartRecording()
    {
      StopRecording();
      _recordingFailed = false;
      EnsureRecording();
    }

    private void StopRecording()
    {
      if (!string.IsNullOrEmpty(_device) && _adapter.IsRecording(_device))
        _adapter.End(_device);
      _clip = null;
      _device = null;
      _aboveThresholdSeconds = 0f;
      SyncCaptureIndicator();
    }

    private bool _permissionRequestCompleted;

    private IEnumerator RequestPermissionEarly()
    {
      Availability before = ResolveAvailability();
      if (!_adapter.HasPermission)
        yield return _adapter.RequestPermission();

      _permissionRequestCompleted = true;
      EnsureRecording();
      NotifyAvailabilityChanged(before);
    }

    private IEnumerator RetryRecording(
      int generation,
      UnityEngine.Object callbackTarget,
      Action<Availability> completed)
    {
      Availability before = ResolveAvailability();
      StopRecording();
      _recordingFailed = false;
      _permissionRequestCompleted = false;
      NotifyAvailabilityChanged(before);

      if (!_adapter.HasPermission)
        yield return _adapter.RequestPermission();

      if (generation != _retryGeneration)
        yield break;

      _permissionRequestCompleted = true;
      EnsureRecording();
      Availability availability = ResolveAvailability();
      NotifyAvailabilityChanged(Availability.Pending);
      _retryRoutine = null;
      if (!(callbackTarget is not null && callbackTarget == null))
        completed?.Invoke(availability);
    }

    private void OnDestroy()
    {
      _retryGeneration++;
      _retryRoutine = null;
      StopRecording();
      if (_instance == this)
        _instance = null;
    }

    private Availability ResolveAvailability()
    {
      if (!_permissionRequestCompleted)
        return Availability.Pending;
      if (!_adapter.HasPermission)
        return Availability.PermissionDenied;
      if (_adapter.Devices == null || _adapter.Devices.Length == 0)
        return Availability.NoDevice;
      return _recordingFailed ? Availability.RecordingFailed : Availability.Ready;
    }

    private void MarkRecordingFailed()
    {
      Availability before = ResolveAvailability();
      StopRecording();
      _recordingFailed = true;
      NotifyAvailabilityChanged(before);
    }

    private void NotifyAvailabilityChanged(Availability before)
    {
      if (before != ResolveAvailability())
        AvailabilityChanged?.Invoke();
    }

    /// <summary>
    /// 마이크에서 실제로 입력을 받아오는 동안에만 화면 우측 하단 표시를 켠다.
    /// 감시 대상이 있어도 마이크를 열지 못한 상태(권한 거부·장치 없음·녹음 실패)에서는
    /// 받아오는 입력이 없으므로 켜지 않는다.
    /// </summary>
    private void SyncCaptureIndicator()
    {
      MicrophoneCaptureIndicatorUIController.SetCapturing(_targets.Count > 0 && _clip != null);
    }
  }
}
