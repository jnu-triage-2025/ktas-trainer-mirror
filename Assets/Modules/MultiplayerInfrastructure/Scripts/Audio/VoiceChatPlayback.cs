using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>네트워크에서 받은 PCM을 Unity 오디오 스레드에 공급하는 작은 지터 버퍼입니다.</summary>
  public sealed class VoiceChatPlayback : MonoBehaviour
  {
    private const int SampleRate = 16000;
    private const int MaxBufferedSamples = SampleRate;
    private const int PrebufferSamples = SampleRate * 60 / 1000;
    private readonly Queue<float> _samples = new(MaxBufferedSamples);
    private readonly object _gate = new();
    private AudioSource _source;
    private AudioClip _clip;
    private bool _buffering = true;

    public void Initialize()
    {
      if (_source != null) return;
      _source = gameObject.AddComponent<AudioSource>();
      _source.playOnAwake = false; _source.loop = true; _source.spatialBlend = 1f;
      _source.minDistance = 2f; _source.maxDistance = 15f;
      _source.rolloffMode = AudioRolloffMode.Linear; _source.dopplerLevel = 0f;
      _clip = AudioClip.Create("ProximityVoice", SampleRate, 1, SampleRate, true, FillAudio);
      _source.clip = _clip; _source.Play();
    }

    public void Enqueue(byte[] pcm16)
    {
      if (pcm16 == null || pcm16.Length == 0 || (pcm16.Length & 1) != 0) return;
      lock (_gate)
      {
        int incoming = pcm16.Length / 2;
        while (_samples.Count + incoming > MaxBufferedSamples && _samples.Count > 0) _samples.Dequeue();
        for (int i = 0; i < pcm16.Length; i += 2)
        {
          short value = (short)(pcm16[i] | (pcm16[i + 1] << 8));
          _samples.Enqueue(value / 32768f);
        }
      }
    }

    private void Update() { if (_source != null) _source.volume = VoiceChatSettings.OutputVolume; }
    private void FillAudio(float[] data)
    {
      lock (_gate)
      {
        if (_buffering)
        {
          if (_samples.Count < Mathf.Max(PrebufferSamples, data.Length))
          {
            Array.Clear(data, 0, data.Length);
            return;
          }
          _buffering = false;
        }
        if (_samples.Count < data.Length)
        {
          _buffering = true;
          Array.Clear(data, 0, data.Length);
          return;
        }
        for (int i = 0; i < data.Length; i++) data[i] = _samples.Dequeue();
      }
    }
    private void OnDestroy() { if (_clip != null) Destroy(_clip); }
  }
}
