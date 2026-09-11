using MultiplayerInfrastructure.Audio;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.Tests
{
  public sealed class VoiceChatSettingsTests
  {
    [TearDown]
    public void TearDown() => VoiceChatSettings.ClearSessionMutes();

    [Test]
    public void VadThreshold_DecreasesAsSensitivityIncreases()
    {
      Assert.That(VoiceChatSettings.ResolveVadThreshold(1f),
        Is.LessThan(VoiceChatSettings.ResolveVadThreshold(0f)));
    }

    [TestCase(-1f, 0.04f)]
    [TestCase(2f, 0.006f)]
    public void VadThreshold_ClampsSensitivity(float sensitivity, float expected)
    {
      Assert.That(VoiceChatSettings.ResolveVadThreshold(sensitivity), Is.EqualTo(expected).Within(0.0001f));
    }

    [Test]
    public void PlayerMute_IsScopedToCurrentSessionState()
    {
      VoiceChatSettings.SetPlayerMuted("player-a", true);
      Assert.That(VoiceChatSettings.IsPlayerMuted("player-a"), Is.True);

      VoiceChatSettings.ClearSessionMutes();
      Assert.That(VoiceChatSettings.IsPlayerMuted("player-a"), Is.False);
    }
  }

  public sealed class VoiceChatPlaybackTests
  {
    private GameObject _host;
    private VoiceChatPlayback _playback;

    [SetUp]
    public void SetUp()
    {
      _host = new GameObject("VoiceChatPlaybackTests");
      _playback = _host.AddComponent<VoiceChatPlayback>();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_host);

    [Test]
    public void Enqueue_ReordersFramesArrivingInsideWindow()
    {
      var encoder = new VoiceChatEncoder();
      _playback.Enqueue(1, Frame(encoder, 0.1f), true);
      _playback.Enqueue(3, Frame(encoder, 0.3f), false);
      Assert.That(Samples().Count, Is.EqualTo(320));
      _playback.Enqueue(2, Frame(encoder, 0.2f), false);

      Assert.That(Samples().Count, Is.EqualTo(960));
    }

    [Test]
    public void Enqueue_ConcealsFrameMissingBeyondWindow()
    {
      var encoder = new VoiceChatEncoder();
      _playback.Enqueue(1, Frame(encoder, 0.1f), true);
      _playback.Enqueue(3, Frame(encoder, 0.3f), false);
      _playback.Enqueue(4, Frame(encoder, 0.4f), false);
      _playback.Enqueue(5, Frame(encoder, 0.5f), false);

      Assert.That(Samples().Count, Is.EqualTo(1600));
    }

    private Queue<float> Samples()
      => (Queue<float>)typeof(VoiceChatPlayback)
        .GetField("_samples", BindingFlags.Instance | BindingFlags.NonPublic)
        .GetValue(_playback);

    [Test]
    public void Codec_RoundTripsOneVoiceFrameWithinPacketLimit()
    {
      var encoder = new VoiceChatEncoder();
      byte[] packet = Frame(encoder, 0.25f);
      var decoded = new float[VoiceChatEncoder.FrameSamples];
      int count = new VoiceChatDecoder().Decode(packet, false, decoded);

      Assert.That(packet.Length, Is.InRange(1, VoiceChatEncoder.MaximumPacketBytes));
      Assert.That(count, Is.EqualTo(VoiceChatEncoder.FrameSamples));
    }

    private static byte[] Frame(VoiceChatEncoder encoder, float value)
      => encoder.Encode(System.Linq.Enumerable.ToArray(
        System.Linq.Enumerable.Repeat(value, VoiceChatEncoder.FrameSamples)));
  }
}
