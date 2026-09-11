using PlayerPrefs = MultiplayerInfrastructure.Automation.ProfilePlayerPrefs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  public enum VoiceActivationMode { VoiceActivity = 0, PushToTalk = 1 }

  /// <summary>이 기기의 근접 음성채팅 설정을 저장하고 변경 사항을 알립니다.</summary>
  public static class VoiceChatSettings
  {
    public const string PushToTalkActionId = "voice_push_to_talk";
    public const KeyCode DefaultPushToTalkKey = KeyCode.V;
    private const string ModeKey = "voiceChat.activationMode";
    private const string EnabledKey = "voiceChat.enabled";
    private const string InputVolumeKey = "voiceChat.inputVolume";
    private const string OutputVolumeKey = "voiceChat.outputVolume";
    private const string SensitivityKey = "voiceChat.sensitivity";
    private static readonly HashSet<string> MutedPlayers = new(StringComparer.Ordinal);
    public static event Action Changed;
    private static float _microphoneTestUntil;
    public static bool Enabled => PlayerPrefs.GetInt(EnabledKey, 1) != 0;
    public static VoiceActivationMode ActivationMode => (VoiceActivationMode)Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, 0), 0, 1);
    public static float InputVolume => PlayerPrefs.GetFloat(InputVolumeKey, 1f);
    public static float OutputVolume => PlayerPrefs.GetFloat(OutputVolumeKey, 1f);
    public static float Sensitivity => PlayerPrefs.GetFloat(SensitivityKey, 0.55f);
    public static bool IsMicrophoneTestActive => Time.unscaledTime < _microphoneTestUntil;
    public static void SetEnabled(bool value) => SetInt(EnabledKey, value ? 1 : 0);
    public static void SetActivationMode(VoiceActivationMode value) => SetInt(ModeKey, (int)value);
    public static void SetInputVolume(float value) => SetFloat(InputVolumeKey, value);
    public static void SetOutputVolume(float value) => SetFloat(OutputVolumeKey, value);
    public static void SetSensitivity(float value) => SetFloat(SensitivityKey, value);
    public static void StartMicrophoneTest(float seconds = 5f) => _microphoneTestUntil = Time.unscaledTime + Mathf.Max(1f, seconds);
    public static bool IsPlayerMuted(string identifier) => !string.IsNullOrEmpty(identifier) && MutedPlayers.Contains(identifier);
    public static void SetPlayerMuted(string identifier, bool muted)
    {
      if (string.IsNullOrWhiteSpace(identifier)) return;
      if (muted) MutedPlayers.Add(identifier); else MutedPlayers.Remove(identifier);
      Changed?.Invoke();
    }
    public static void ClearSessionMutes() { MutedPlayers.Clear(); Changed?.Invoke(); }
    public static float ResolveVadThreshold(float sensitivity) => Mathf.Lerp(0.04f, 0.006f, Mathf.Clamp01(sensitivity));

    public static void ClearStoredValues()
    {
      PlayerPrefs.DeleteKey(ModeKey); PlayerPrefs.DeleteKey(EnabledKey);
      PlayerPrefs.DeleteKey(InputVolumeKey); PlayerPrefs.DeleteKey(OutputVolumeKey);
      PlayerPrefs.DeleteKey(SensitivityKey);
      PlayerPrefs.Save(); Changed?.Invoke();
      ClearSessionMutes();
    }
    private static void SetInt(string key, int value) { PlayerPrefs.SetInt(key, value); PlayerPrefs.Save(); Changed?.Invoke(); }
    private static void SetFloat(string key, float value) { PlayerPrefs.SetFloat(key, Mathf.Clamp01(value)); PlayerPrefs.Save(); Changed?.Invoke(); }
  }
}
