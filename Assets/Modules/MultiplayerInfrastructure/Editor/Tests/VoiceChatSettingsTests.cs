using MultiplayerInfrastructure.Audio;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Editor.Tests
{
  public sealed class VoiceChatSettingsTests
  {
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
  }
}
