using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>마이크 음량이 임계치를 1초 이상 넘으면 활성 의식 확인을 완료한다.</summary>
  internal sealed class RecognitionCheckMicrophoneInput : MonoBehaviour
  {
    private const float VolumeThreshold = 0.02f;
    private const float RequiredDurationSeconds = 1f;
    private const int SampleCount = 256;
    private static RecognitionCheckMicrophoneInput _instance;
    private readonly HashSet<PatientController> _targets = new();
    private AudioClip _clip;
    private string _device;
    private float _aboveThresholdSeconds;
    private readonly float[] _samples = new float[SampleCount];

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

    private void EnsureRecording()
    {
      if (_clip != null || _targets.Count == 0
          || !_permissionRequestCompleted
          || !Application.HasUserAuthorization(UserAuthorization.Microphone)
          || Microphone.devices == null || Microphone.devices.Length == 0)
        return;
      _device = Microphone.devices[0];
      _clip = Microphone.Start(_device, true, 2, AudioSettings.outputSampleRate);
      _aboveThresholdSeconds = 0f;
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

      int position = Microphone.GetPosition(_device);
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
        target?.RequestRecognitionCheckCompletion();
    }

    private void StopRecording()
    {
      if (!string.IsNullOrEmpty(_device) && Microphone.IsRecording(_device))
        Microphone.End(_device);
      _clip = null;
      _device = null;
      _aboveThresholdSeconds = 0f;
    }

    private bool _permissionRequestCompleted;

    private IEnumerator RequestPermissionEarly()
    {
      if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

      _permissionRequestCompleted = true;
    }
  }
}
