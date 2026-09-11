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
      _playback.Enqueue(1, Frame(100));
      _playback.Enqueue(3, Frame(300));
      _playback.Enqueue(2, Frame(200));

      float[] samples = Samples().ToArray();
      Assert.That(samples.Length, Is.EqualTo(960));
      Assert.That(samples[0], Is.EqualTo(100 / 32768f));
      Assert.That(samples[320], Is.EqualTo(200 / 32768f));
      Assert.That(samples[640], Is.EqualTo(300 / 32768f));
    }

    [Test]
    public void Enqueue_InsertsSilenceForFrameMissingBeyondWindow()
    {
      _playback.Enqueue(1, Frame(100));
      _playback.Enqueue(3, Frame(300));
      _playback.Enqueue(4, Frame(400));
      _playback.Enqueue(5, Frame(500));

      float[] samples = Samples().ToArray();
      Assert.That(samples.Length, Is.EqualTo(1600));
      Assert.That(samples[320], Is.Zero);
      Assert.That(samples[640], Is.EqualTo(300 / 32768f));
    }

    private Queue<float> Samples()
      => (Queue<float>)typeof(VoiceChatPlayback)
        .GetField("_samples", BindingFlags.Instance | BindingFlags.NonPublic)
        .GetValue(_playback);

    private static byte[] Frame(short value)
    {
      var frame = new byte[640];
      for (int i = 0; i < frame.Length; i += 2)
      {
        frame[i] = (byte)value;
        frame[i + 1] = (byte)(value >> 8);
      }
      return frame;
    }
  }
}
