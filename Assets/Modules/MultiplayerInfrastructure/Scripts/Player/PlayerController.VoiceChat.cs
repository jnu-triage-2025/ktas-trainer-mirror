using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using MultiplayerInfrastructure.Audio;
using MultiplayerInfrastructure.Server;
using MultiplayerInfrastructure.UI;
using UnityEngine;
using Input = MultiplayerInfrastructure.Automation.PlayerInput;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private const int VoiceSampleRate = 16000;
    private const int VoiceFrameSamples = 320;
    private const int VoiceFrameBytes = VoiceFrameSamples * 2;
    private const float VoiceMaximumDistance = 15f;
    private const float VoiceHangoverSeconds = 0.22f;
    private const float VoicePacketMinimumInterval = 0.012f;
    private VoiceChatPlayback _voicePlayback;
    private AudioClip _voiceMicrophoneClip;
    private string _voiceMicrophoneDevice;
    private int _voiceReadPosition;
    private float _voiceHangoverUntil;
    private float _lastVoicePacketServerTime;
    private float _nextVoiceMicrophoneRetryTime;
    private readonly float[] _voiceInputFrame = new float[VoiceFrameSamples];
    private readonly byte[] _voicePacket = new byte[VoiceFrameBytes];
    private static PlayerController _localVoiceController;
    private static float _localVoiceInputRms;
    private static readonly HashSet<PlayerController> ServerVoicePlayers = new();

    public static bool IsMicrophoneTestAvailable
    {
      get
      {
        if (_localVoiceController == null || !Application.HasUserAuthorization(UserAuthorization.Microphone)) return false;
        try { return Microphone.devices != null && Microphone.devices.Length > 0; }
        catch { return false; }
      }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetVoiceChatStatics()
    {
      ServerVoicePlayers.Clear();
      _localVoiceController = null;
      _localVoiceInputRms = 0f;
    }
    private void OnStartServer_VoiceChat() => ServerVoicePlayers.Add(this);
    private void OnStopServer_VoiceChat() => ServerVoicePlayers.Remove(this);

    private void OnStartClient_VoiceChat()
    {
      if (!IsOwner) { _voicePlayback = gameObject.AddComponent<VoiceChatPlayback>(); _voicePlayback.Initialize(); return; }
      if (DedicatedServerRuntime.IsActive) return;
      _localVoiceController = this;
      AudioDevicePreferenceService.InputDeviceChanged += RestartVoiceMicrophone;
      AudioSettings.OnAudioConfigurationChanged += HandleVoiceAudioConfigurationChanged;
      StartVoiceMicrophone();
    }

    private void OnStopClient_VoiceChat()
    {
      if (IsOwner)
      {
        AudioDevicePreferenceService.InputDeviceChanged -= RestartVoiceMicrophone;
        AudioSettings.OnAudioConfigurationChanged -= HandleVoiceAudioConfigurationChanged;
        StopVoiceMicrophone();
        if (_localVoiceController == this) _localVoiceController = null;
      }
      if (_voicePlayback != null) Destroy(_voicePlayback);
      _voicePlayback = null;
    }

    private void Update_VoiceChat()
    {
      if (!IsOwner || DedicatedServerRuntime.IsActive) return;
      if (!VoiceChatSettings.Enabled && !VoiceChatSettings.IsMicrophoneTestActive)
      {
        if (_voiceMicrophoneClip != null) StopVoiceMicrophone();
        MicrophoneCaptureIndicatorUIController.SetCapturing(false);
        return;
      }
      if (_voiceMicrophoneClip == null)
      {
        if (Time.unscaledTime >= _nextVoiceMicrophoneRetryTime) StartVoiceMicrophone();
        return;
      }
      try
      {
        if (!Microphone.IsRecording(_voiceMicrophoneDevice)) { MarkVoiceMicrophoneUnavailable(); return; }
        int writePosition = Microphone.GetPosition(_voiceMicrophoneDevice);
        if (writePosition < 0) { MarkVoiceMicrophoneUnavailable(); return; }
        int available = writePosition >= _voiceReadPosition ? writePosition - _voiceReadPosition : _voiceMicrophoneClip.samples - _voiceReadPosition + writePosition;
        while (available >= VoiceFrameSamples)
        {
          _voiceMicrophoneClip.GetData(_voiceInputFrame, _voiceReadPosition);
          _voiceReadPosition = (_voiceReadPosition + VoiceFrameSamples) % _voiceMicrophoneClip.samples;
          available -= VoiceFrameSamples; ProcessVoiceFrame();
        }
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[VoiceChat] Microphone became unavailable: {exception.Message}", this);
        MarkVoiceMicrophoneUnavailable();
      }
    }

    private void ProcessVoiceFrame()
    {
      float inputGain = VoiceChatSettings.InputVolume * 2f, sum = 0f;
      for (int i = 0; i < _voiceInputFrame.Length; i++)
      {
        float sample = Mathf.Clamp(_voiceInputFrame[i] * inputGain, -1f, 1f);
        _voiceInputFrame[i] = sample; sum += sample * sample;
      }
      float rms = Mathf.Sqrt(sum / _voiceInputFrame.Length);
      _localVoiceInputRms = rms;
      bool transmitting;
      if (VoiceChatSettings.ActivationMode == VoiceActivationMode.PushToTalk)
      {
        KeyCode key = KeyBindingRepository.GetBoundKey(VoiceChatSettings.PushToTalkActionId, VoiceChatSettings.DefaultPushToTalkKey);
        transmitting = key != KeyCode.None && Input.GetKey(key);
      }
      else
      {
        if (rms >= VoiceChatSettings.ResolveVadThreshold(VoiceChatSettings.Sensitivity)) _voiceHangoverUntil = Time.unscaledTime + VoiceHangoverSeconds;
        transmitting = Time.unscaledTime <= _voiceHangoverUntil;
      }
      MicrophoneCaptureIndicatorUIController.SetGaugeLevel(rms / 0.2f);
      MicrophoneCaptureIndicatorUIController.SetCapturing(transmitting);
      if (VoiceChatSettings.IsMicrophoneTestActive)
      {
        _voicePlayback ??= gameObject.AddComponent<VoiceChatPlayback>();
        _voicePlayback.Initialize();
        EncodeVoiceFrame();
        _voicePlayback.Enqueue(_voicePacket);
      }
      if (!VoiceChatSettings.Enabled || !transmitting || !IsClientInitialized) return;
      EncodeVoiceFrame();
      SubmitVoiceFrameServerRpc(_voicePacket);
    }

    private void EncodeVoiceFrame()
    {
      for (int i = 0, b = 0; i < _voiceInputFrame.Length; i++, b += 2)
      {
        short value = (short)Mathf.RoundToInt(_voiceInputFrame[i] * 32767f);
        _voicePacket[b] = (byte)value; _voicePacket[b + 1] = (byte)(value >> 8);
      }
    }

    [ServerRpc]
    private void SubmitVoiceFrameServerRpc(byte[] pcm16, Channel channel = Channel.Unreliable, NetworkConnection sender = null)
    {
      if (pcm16 == null || pcm16.Length != VoiceFrameBytes || sender == null || sender != Owner) return;
      float now = Time.unscaledTime;
      if (now - _lastVoicePacketServerTime < VoicePacketMinimumInterval) return;
      _lastVoicePacketServerTime = now;
      float maxDistanceSqr = VoiceMaximumDistance * VoiceMaximumDistance;
      foreach (var listener in ServerVoicePlayers)
      {
        if (listener == null || listener == this || listener.Owner == null || !listener.Owner.IsActive) continue;
        if ((listener.transform.position - transform.position).sqrMagnitude > maxDistanceSqr) continue;
        ReceiveVoiceFrameTargetRpc(listener.Owner, pcm16);
      }
    }

    [TargetRpc]
    private void ReceiveVoiceFrameTargetRpc(NetworkConnection target, byte[] pcm16, Channel channel = Channel.Unreliable)
    {
      if (IsOwner || !VoiceChatSettings.Enabled || VoiceChatSettings.IsPlayerMuted(UserIdentifier) || _voicePlayback == null) return;
      var local = FindLocalVoiceListener();
      if (local == null || (local.transform.position - transform.position).sqrMagnitude > VoiceMaximumDistance * VoiceMaximumDistance) return;
      _voicePlayback.Enqueue(pcm16);
    }

    private static PlayerController FindLocalVoiceListener()
      => _localVoiceController;

    private void StartVoiceMicrophone()
    {
      if (_voiceMicrophoneClip != null || (!VoiceChatSettings.Enabled && !VoiceChatSettings.IsMicrophoneTestActive)
          || !Application.HasUserAuthorization(UserAuthorization.Microphone)) return;
      _voiceMicrophoneDevice = AudioDevicePreferenceService.ResolveInputDeviceName();
      try { _voiceMicrophoneClip = Microphone.Start(_voiceMicrophoneDevice, true, 2, VoiceSampleRate); _voiceReadPosition = 0; }
      catch (Exception exception)
      {
        Debug.LogWarning($"[VoiceChat] Microphone recording failed: {exception.Message}", this);
        _voiceMicrophoneClip = null; _nextVoiceMicrophoneRetryTime = Time.unscaledTime + 2f;
      }
    }

    private void StopVoiceMicrophone()
    {
      try
      {
        if (_voiceMicrophoneClip != null && Microphone.IsRecording(_voiceMicrophoneDevice)) Microphone.End(_voiceMicrophoneDevice);
      }
      catch (Exception exception) { Debug.LogWarning($"[VoiceChat] Microphone shutdown failed: {exception.Message}", this); }
      _voiceMicrophoneClip = null; _voiceMicrophoneDevice = null;
      MicrophoneCaptureIndicatorUIController.SetCapturing(false);
    }
    private void MarkVoiceMicrophoneUnavailable()
    {
      _voiceMicrophoneClip = null; _voiceMicrophoneDevice = null;
      _nextVoiceMicrophoneRetryTime = Time.unscaledTime + 2f;
      MicrophoneCaptureIndicatorUIController.SetCapturing(false);
    }
    private void RestartVoiceMicrophone() { StopVoiceMicrophone(); _nextVoiceMicrophoneRetryTime = 0f; StartVoiceMicrophone(); }
    private void HandleVoiceAudioConfigurationChanged(bool _) => RestartVoiceMicrophone();

    public static bool TryGetLocalVoiceInputLevel(out float rms)
    {
      rms = _localVoiceInputRms;
      return _localVoiceController != null && _localVoiceController._voiceMicrophoneClip != null;
    }
  }
}
