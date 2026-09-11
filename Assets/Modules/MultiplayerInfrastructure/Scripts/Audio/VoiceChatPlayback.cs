using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>네트워크에서 받은 PCM을 Unity 오디오 스레드에 공급하는 작은 지터 버퍼입니다.</summary>
  public sealed class VoiceChatPlayback : MonoBehaviour
  {
    private const int SampleRate = VoiceChatEncoder.SampleRate;
    private const int MaxBufferedSamples = SampleRate;
    private const int PrebufferSamples = SampleRate * 60 / 1000;
    private const int SamplesPerFrame = VoiceChatEncoder.FrameSamples;
    private const int ReorderWindowFrames = 3;
    private readonly Queue<float> _samples = new(MaxBufferedSamples);
    private readonly object _gate = new();
    private readonly Dictionary<ushort, EncodedFrame> _pendingFrames = new();
    private AudioSource _source;
    private AudioClip _clip;
    private bool _buffering = true;
    private bool _hasExpectedSequence;
    private ushort _expectedSequence;
    private readonly VoiceChatDecoder _decoder = new();
    private readonly float[] _decodedFrame = new float[SamplesPerFrame];

    private readonly struct EncodedFrame
    {
      public readonly byte[] Packet;
      public readonly bool BeginsTalkspurt;
      public EncodedFrame(byte[] packet, bool beginsTalkspurt)
      {
        Packet = packet;
        BeginsTalkspurt = beginsTalkspurt;
      }
    }

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

    public void Enqueue(ushort sequence, byte[] opus, bool beginsTalkspurt)
    {
      if (opus == null || opus.Length == 0 || opus.Length > VoiceChatEncoder.MaximumPacketBytes) return;
      lock (_gate)
      {
        if (!_hasExpectedSequence)
        {
          _hasExpectedSequence = true;
          _expectedSequence = sequence;
        }
        ushort forward = (ushort)(sequence - _expectedSequence);
        if (forward >= 32768 || _pendingFrames.ContainsKey(sequence)) return;
        _pendingFrames.Add(sequence, new EncodedFrame(opus, beginsTalkspurt));
        DrainContiguousFrames();
        while (_pendingFrames.Count >= ReorderWindowFrames)
        {
          AppendConcealedFrame();
          _expectedSequence++;
          DrainContiguousFrames();
        }
      }
    }

    private void DrainContiguousFrames()
    {
      while (_pendingFrames.Remove(_expectedSequence, out var frame))
      {
        if (frame.BeginsTalkspurt) _decoder.Reset();
        AppendDecodedFrame(frame.Packet, false);
        _expectedSequence++;
      }
    }

    private void AppendConcealedFrame()
    {
      ushort followingSequence = (ushort)(_expectedSequence + 1);
      byte[] fecPacket = _pendingFrames.TryGetValue(followingSequence, out var following)
        ? following.Packet
        : null;
      AppendDecodedFrame(fecPacket, fecPacket != null);
    }

    private void AppendDecodedFrame(byte[] packet, bool useForwardErrorCorrection)
    {
      int decoded;
      try { decoded = _decoder.Decode(packet, useForwardErrorCorrection, _decodedFrame); }
      catch (Exception)
      {
        Array.Clear(_decodedFrame, 0, _decodedFrame.Length);
        decoded = 0;
      }
      if (decoded <= 0) decoded = SamplesPerFrame;
      while (_samples.Count + decoded > MaxBufferedSamples && _samples.Count > 0) _samples.Dequeue();
      for (int i = 0; i < decoded; i++) _samples.Enqueue(_decodedFrame[i]);
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
