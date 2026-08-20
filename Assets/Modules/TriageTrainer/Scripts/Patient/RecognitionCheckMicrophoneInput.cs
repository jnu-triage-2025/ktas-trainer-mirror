using System.Collections;
using System.Collections.Generic;
using System;
using MultiplayerInfrastructure.Audio;
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
      bool HasPermission { get; }
      string[] Devices { get; }
      IEnumerator RequestPermission();
      AudioClip Start(string device, int frequency);
      bool IsRecording(string device);
      int GetPosition(string device);
      void End(string device);
    }

    private sealed class UnityAdapter : IAdapter
    {
      public bool HasPermission => Application.HasUserAuthorization(UserAuthorization.Microphone);
      public string[] Devices => Microphone.devices;
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

    internal static event Action AvailabilityChanged;

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

    // Request microphone access before the first scene is shown so gameplay is
    // not interrupted by the platform permission dialog later.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureCreated()
    {
      if (_instance != null)
        return;
      var host = new GameObject(nameof(RecognitionCheckMicrophoneInput));
      DontDestroyOnLoad(host);
      _instance = host.AddComponent<RecognitionCheckMicrophoneInput>();
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
      _instance.StartCoroutine(_instance.RetryRecording(completed));
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
      if (_clip != null || _targets.Count == 0
          || !_permissionRequestCompleted
          || _recordingFailed
          || !_adapter.HasPermission
          || _adapter.Devices == null || _adapter.Devices.Length == 0)
        return;
      Availability before = ResolveAvailability();
      _device = ResolvePreferredDevice(_adapter.Devices);
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
      if (_clip == null)
      {
        EnsureRecording();
        return;
      }

      int position;
      try
      {
        position = _adapter.GetPosition(_device);
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[RecognitionCheckMicrophoneInput] Microphone recording failed: {exception.Message}");
        MarkRecordingFailed();
        return;
      }
      if (position < SampleCount)
        return;
      _clip.GetData(_samples, position - SampleCount);
      float sum = 0f;
      for (int i = 0; i < _samples.Length; i++)
        sum += _samples[i] * _samples[i];
      float rms = Mathf.Sqrt(sum / _samples.Length);
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
    private static string ResolvePreferredDevice(string[] devices)
    {
      var preferred = AudioDevicePreferenceService.ResolveInputDeviceName();
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
    }

    private void OnDisable()
    {
      AudioDevicePreferenceService.InputDeviceChanged -= HandleInputDeviceChanged;
    }

    /// <summary>설정에서 마이크를 바꾸면 녹음을 끊고 새 장치로 다시 연다.</summary>
    private void HandleInputDeviceChanged()
    {
      if (_clip == null)
        return;

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

    private IEnumerator RetryRecording(Action<Availability> completed)
    {
      Availability before = ResolveAvailability();
      StopRecording();
      _recordingFailed = false;
      _permissionRequestCompleted = false;
      NotifyAvailabilityChanged(before);

      if (!_adapter.HasPermission)
        yield return _adapter.RequestPermission();

      _permissionRequestCompleted = true;
      EnsureRecording();
      Availability availability = ResolveAvailability();
      NotifyAvailabilityChanged(Availability.Pending);
      completed?.Invoke(availability);
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
  }
}
