using System.Collections.Generic;
using MultiplayerInfrastructure.Audio;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.Audio.Tests
{
  /// <summary>
  /// 오디오 장치 설정의 직렬화와 "사라진 장치 되돌리기" 규칙을 검증합니다.
  ///
  /// 실제 장치가 없는 CI에서도 돌아야 하므로, 운영체제를 건드리지 않는 순수 로직만 다룹니다.
  /// </summary>
  public class AudioDeviceSettingsTests
  {
    private static List<AudioDeviceDescriptor> SampleDevices() => new()
    {
      new AudioDeviceDescriptor("{0.0.0}.{speaker}", "스피커(Realtek Audio)", isSystemDefault: true),
      new AudioDeviceDescriptor("{0.0.0}.{headset}", "헤드셋(USB Audio)"),
    };

    // ──────────────────────────────────────────────────────────────────────────
    // 직렬화
    // ──────────────────────────────────────────────────────────────────────────
    [Test]
    public void Settings_SurviveJsonRoundTrip()
    {
      var original = new AudioDeviceSettingsData();
      original.SetDevice(AudioDeviceKind.Output, "{0.0.0}.{headset}", "헤드셋(USB Audio)");
      original.SetDevice(AudioDeviceKind.Input, "마이크(USB Audio)", "마이크(USB Audio)");

      var restored = JsonUtility.FromJson<AudioDeviceSettingsData>(JsonUtility.ToJson(original));

      Assert.That(restored.OutputDeviceId, Is.EqualTo(original.OutputDeviceId));
      Assert.That(restored.OutputDeviceName, Is.EqualTo(original.OutputDeviceName));
      Assert.That(restored.InputDeviceId, Is.EqualTo(original.InputDeviceId));
      Assert.That(restored.InputDeviceName, Is.EqualTo(original.InputDeviceName));
    }

    [Test]
    public void Sanitize_TrimsValuesAndDropsOrphanNames()
    {
      var settings = new AudioDeviceSettingsData
      {
        OutputDeviceId = "  {0.0.0}.{headset}  ",
        OutputDeviceName = "  헤드셋  ",
        InputDeviceId = "   ",
        InputDeviceName = "예전 마이크",
      };

      settings.Sanitize();

      Assert.That(settings.OutputDeviceId, Is.EqualTo("{0.0.0}.{headset}"));
      Assert.That(settings.OutputDeviceName, Is.EqualTo("헤드셋"));
      Assert.That(settings.InputDeviceId, Is.Empty);
      Assert.That(settings.InputDeviceName, Is.Empty, "식별자가 비면 이름 캐시도 함께 비워야 한다.");
    }

    [Test]
    public void Clone_DoesNotShareState()
    {
      var original = new AudioDeviceSettingsData();
      original.SetDevice(AudioDeviceKind.Output, "a", "A");

      var copy = original.Clone();
      copy.SetDevice(AudioDeviceKind.Output, "b", "B");

      Assert.That(original.OutputDeviceId, Is.EqualTo("a"));
      Assert.That(copy.OutputDeviceId, Is.EqualTo("b"));
    }

    [Test]
    public void DefaultSettings_FollowSystemOnBothDirections()
    {
      var settings = new AudioDeviceSettingsData();

      Assert.That(AudioDeviceSelectionResolver.IsSystemDefault(settings.OutputDeviceId), Is.True);
      Assert.That(AudioDeviceSelectionResolver.IsSystemDefault(settings.InputDeviceId), Is.True);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 사라진 장치 되돌리기
    // ──────────────────────────────────────────────────────────────────────────
    [Test]
    public void Reconcile_KeepsSelectionThatStillExists()
    {
      var devices = SampleDevices();

      Assert.That(AudioDeviceSelectionResolver.Reconcile("{0.0.0}.{headset}", devices),
        Is.EqualTo("{0.0.0}.{headset}"));
    }

    [Test]
    public void Reconcile_FallsBackToSystemDefaultWhenDeviceIsGone()
    {
      var devices = SampleDevices();

      Assert.That(AudioDeviceSelectionResolver.Reconcile("{0.0.0}.{unplugged}", devices),
        Is.EqualTo(AudioDeviceSelectionResolver.SystemDefaultId));
    }

    [Test]
    public void Reconcile_FallsBackWhenNothingIsDetected()
    {
      Assert.That(AudioDeviceSelectionResolver.Reconcile("{0.0.0}.{headset}", new List<AudioDeviceDescriptor>()),
        Is.EqualTo(AudioDeviceSelectionResolver.SystemDefaultId));
      Assert.That(AudioDeviceSelectionResolver.Reconcile("{0.0.0}.{headset}", null),
        Is.EqualTo(AudioDeviceSelectionResolver.SystemDefaultId));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 표시 문구
    // ──────────────────────────────────────────────────────────────────────────
    [Test]
    public void SystemDefaultLabel_ShowsTheDeviceTheSystemIsUsing()
    {
      Assert.That(AudioDeviceSelectionResolver.BuildSystemDefaultLabel(SampleDevices()),
        Is.EqualTo("시스템 설정(스피커(Realtek Audio))"));
    }

    [Test]
    public void SystemDefaultLabel_MarksUnknownWhenDefaultCannotBeRead()
    {
      var devices = new List<AudioDeviceDescriptor>
      {
        new AudioDeviceDescriptor("a", "장치 A"),
      };

      Assert.That(AudioDeviceSelectionResolver.BuildSystemDefaultLabel(devices),
        Is.EqualTo($"시스템 설정({AudioDeviceSelectionResolver.UnknownDeviceLabel})"));
      Assert.That(AudioDeviceSelectionResolver.BuildSystemDefaultLabel(new List<AudioDeviceDescriptor>()),
        Is.EqualTo($"시스템 설정({AudioDeviceSelectionResolver.UnknownDeviceLabel})"));
    }

    [Test]
    public void ChoiceLabels_PutSystemDefaultFirst()
    {
      var labels = AudioDeviceSelectionResolver.BuildChoiceLabels(SampleDevices());

      Assert.That(labels.Count, Is.EqualTo(3));
      Assert.That(labels[0], Is.EqualTo("시스템 설정(스피커(Realtek Audio))"));
      Assert.That(labels[1], Is.EqualTo("스피커(Realtek Audio) (기본)"));
      Assert.That(labels[2], Is.EqualTo("헤드셋(USB Audio)"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 드롭다운 인덱스 왕복
    // ──────────────────────────────────────────────────────────────────────────
    [Test]
    public void ChoiceIndex_RoundTripsThroughDeviceId()
    {
      var devices = SampleDevices();

      Assert.That(AudioDeviceSelectionResolver.IndexOfChoice(AudioDeviceSelectionResolver.SystemDefaultId, devices),
        Is.EqualTo(0));
      Assert.That(AudioDeviceSelectionResolver.IndexOfChoice("{0.0.0}.{headset}", devices), Is.EqualTo(2));
      Assert.That(AudioDeviceSelectionResolver.ChoiceIdAt(2, devices), Is.EqualTo("{0.0.0}.{headset}"));
      Assert.That(AudioDeviceSelectionResolver.ChoiceIdAt(0, devices),
        Is.EqualTo(AudioDeviceSelectionResolver.SystemDefaultId));
    }

    [Test]
    public void ChoiceIndex_HandlesDevicesSharingTheSameName()
    {
      // 같은 모델을 두 개 꽂으면 표시 이름이 겹친다. 그래서 문구가 아니라 인덱스로 짚어야 한다.
      var devices = new List<AudioDeviceDescriptor>
      {
        new AudioDeviceDescriptor("id-1", "USB Audio"),
        new AudioDeviceDescriptor("id-2", "USB Audio"),
      };

      Assert.That(AudioDeviceSelectionResolver.IndexOfChoice("id-2", devices), Is.EqualTo(2));
      Assert.That(AudioDeviceSelectionResolver.ChoiceIdAt(1, devices), Is.EqualTo("id-1"));
      Assert.That(AudioDeviceSelectionResolver.ChoiceIdAt(2, devices), Is.EqualTo("id-2"));
    }

    [Test]
    public void ChoiceIdAt_ClampsOutOfRangeIndexToSystemDefault()
    {
      var devices = SampleDevices();

      Assert.That(AudioDeviceSelectionResolver.ChoiceIdAt(99, devices),
        Is.EqualTo(AudioDeviceSelectionResolver.SystemDefaultId));
      Assert.That(AudioDeviceSelectionResolver.ChoiceIdAt(-1, devices),
        Is.EqualTo(AudioDeviceSelectionResolver.SystemDefaultId));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Windows 장치 인터페이스 경로
    // ──────────────────────────────────────────────────────────────────────────
    [Test]
    public void EndpointPath_WrapsRenderEndpointForPolicyConfig()
    {
      Assert.That(
        AudioEndpointPath.ForWindowsEndpoint("{0.0.0.00000000}.{abc}", AudioDeviceKind.Output),
        Is.EqualTo(@"\\?\SWD#MMDEVAPI#{0.0.0.00000000}.{abc}#{e6327cad-dcec-4949-ae8a-991e976a79d2}"));
    }

    [Test]
    public void EndpointPath_UsesCaptureInterfaceForInput()
    {
      Assert.That(
        AudioEndpointPath.ForWindowsEndpoint("{0.0.1.00000000}.{abc}", AudioDeviceKind.Input),
        Does.EndWith("#{2eef81be-33fa-4800-9670-1cd474972c3f}"));
    }

    [Test]
    public void EndpointPath_TreatsSystemDefaultAsNoTarget()
    {
      // 빈 경로는 "지정 해제" 신호다. 네이티브 쪽에서 널 HSTRING으로 넘어간다.
      Assert.That(AudioEndpointPath.ForWindowsEndpoint(string.Empty, AudioDeviceKind.Output), Is.Empty);
      Assert.That(AudioEndpointPath.ForWindowsEndpoint(null, AudioDeviceKind.Output), Is.Empty);
      Assert.That(AudioEndpointPath.ForWindowsEndpoint("   ", AudioDeviceKind.Output), Is.Empty);
    }

    [Test]
    public void EndpointPath_RoundTripsThroughUnwrap()
    {
      const string endpointId = "{0.0.0.00000000}.{9d0a1234-5678-90ab-cdef-1234567890ab}";

      foreach (var kind in new[] { AudioDeviceKind.Output, AudioDeviceKind.Input })
      {
        var wrapped = AudioEndpointPath.ForWindowsEndpoint(endpointId, kind);
        Assert.That(AudioEndpointPath.UnwrapWindowsEndpoint(wrapped), Is.EqualTo(endpointId));
      }
    }
  }
}
