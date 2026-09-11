using System;
using Concentus;
using Concentus.Enums;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>근접 음성채팅의 플랫폼 공통 Opus 인코더입니다.</summary>
  public sealed class VoiceChatEncoder
  {
    public const int SampleRate = 16000;
    public const int FrameSamples = 320;
    public const int MaximumPacketBytes = 256;
    private readonly IOpusEncoder _encoder;
    private readonly byte[] _packetBuffer = new byte[MaximumPacketBytes];

    public VoiceChatEncoder()
    {
      OpusCodecFactory.AttemptToUseNativeLibrary = false;
      _encoder = OpusCodecFactory.CreateEncoder(SampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
      _encoder.Bitrate = 24000;
      _encoder.Complexity = 5;
      _encoder.UseVBR = true;
      _encoder.UseConstrainedVBR = true;
      _encoder.UseInbandFEC = true;
      _encoder.PacketLossPercent = 10;
      _encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
    }

    public byte[] Encode(float[] pcm)
    {
      if (pcm == null || pcm.Length != FrameSamples)
        throw new ArgumentException($"Opus input must contain exactly {FrameSamples} samples.", nameof(pcm));
      int length = _encoder.Encode(pcm, FrameSamples, _packetBuffer, MaximumPacketBytes);
      var packet = new byte[length];
      Buffer.BlockCopy(_packetBuffer, 0, packet, 0, length);
      return packet;
    }

    public void Reset() => _encoder.ResetState();
  }

  /// <summary>스트림마다 독립 상태를 유지하는 Opus 디코더입니다.</summary>
  public sealed class VoiceChatDecoder
  {
    private readonly IOpusDecoder _decoder;

    public VoiceChatDecoder()
    {
      OpusCodecFactory.AttemptToUseNativeLibrary = false;
      _decoder = OpusCodecFactory.CreateDecoder(VoiceChatEncoder.SampleRate, 1);
    }

    public int Decode(byte[] packet, bool useForwardErrorCorrection, float[] destination)
    {
      if (destination == null || destination.Length < VoiceChatEncoder.FrameSamples)
        throw new ArgumentException("Opus destination is too small.", nameof(destination));
      ReadOnlySpan<byte> encoded = packet == null ? ReadOnlySpan<byte>.Empty : packet;
      return _decoder.Decode(encoded, destination, VoiceChatEncoder.FrameSamples, useForwardErrorCorrection);
    }

    public void Reset() => _decoder.ResetState();
  }
}
