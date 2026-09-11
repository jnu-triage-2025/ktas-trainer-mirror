using System.Collections.Generic;
using MultiplayerInfrastructure.UI;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.UI
{
  /// <summary>
  /// 설정 초기화가 이 기기에 저장된 사용자 설정 키를 빠짐없이 지우는지 검증합니다.
  /// 저장 키를 하나라도 빠뜨리면 초기화 뒤에도 옛 값이 되살아나므로, 담당 클래스가 쓰는 키 이름을
  /// 여기서 고정합니다. 담당 클래스가 키를 바꾸거나 새 키를 추가하면 이 목록도 같이 고쳐야 합니다.
  /// </summary>
  public sealed class UserPreferenceResetTests
  {
    private static readonly string[] IntKeys =
    {
      "MultiplayerInfrastructure.DialogueSkipEnabled",
      "MultiplayerInfrastructure.TTSEngineDisabled",
      "MultiplayerInfrastructure.UIScale",
      "MultiplayerInfrastructure.TextureQuality",
      "KeyBinding_move_forward",
      "KeyBinding_run",
    };

    private static readonly string[] FloatKeys =
    {
      "audio.masterVolume",
      "audio.sfxVolume",
      "audio.bgmVolume",
      "MultiplayerInfrastructure.AudioVolume",
    };

    private static readonly string[] StringKeys =
    {
      "MultiplayerInfrastructure.AudioDeviceSettings.v1",
      "MultiplayerInfrastructure.GraphicsSettings.v2",
      "IntroScene.PlayerName",
    };

    private static readonly List<KeyBindingEntry> Bindings = new()
    {
      new KeyBindingEntry("move_forward", "앞으로 이동", KeyCode.W),
      new KeyBindingEntry("run", "달리기", KeyCode.LeftControl),
    };

    private readonly Dictionary<string, int> _savedInts = new();
    private readonly Dictionary<string, float> _savedFloats = new();
    private readonly Dictionary<string, string> _savedStrings = new();

    [SetUp]
    public void SetUp()
    {
      // 편집기 PlayerPrefs를 건드리므로 원래 값을 보관했다가 되돌린다.
      _savedInts.Clear();
      _savedFloats.Clear();
      _savedStrings.Clear();
      foreach (var key in IntKeys)
        if (PlayerPrefs.HasKey(key)) _savedInts[key] = PlayerPrefs.GetInt(key);
      foreach (var key in FloatKeys)
        if (PlayerPrefs.HasKey(key)) _savedFloats[key] = PlayerPrefs.GetFloat(key);
      foreach (var key in StringKeys)
        if (PlayerPrefs.HasKey(key)) _savedStrings[key] = PlayerPrefs.GetString(key);
    }

    [TearDown]
    public void TearDown()
    {
      foreach (var key in AllKeys())
        PlayerPrefs.DeleteKey(key);
      foreach (var pair in _savedInts)
        PlayerPrefs.SetInt(pair.Key, pair.Value);
      foreach (var pair in _savedFloats)
        PlayerPrefs.SetFloat(pair.Key, pair.Value);
      foreach (var pair in _savedStrings)
        PlayerPrefs.SetString(pair.Key, pair.Value);
      PlayerPrefs.Save();
    }

    [Test]
    public void ClearStoredValuesRemovesEveryKnownKey()
    {
      WriteAllKeys();

      UserPreferenceReset.ClearStoredValues(Bindings);

      foreach (var key in AllKeys())
        Assert.That(PlayerPrefs.HasKey(key), Is.False, $"'{key}' 키가 초기화 뒤에도 남아 있습니다.");
    }

    [Test]
    public void ClearStoredValuesWithoutBindingsLeavesKeyBindingsAlone()
    {
      WriteAllKeys();

      UserPreferenceReset.ClearStoredValues(null);

      Assert.That(PlayerPrefs.HasKey("KeyBinding_move_forward"), Is.True);
      Assert.That(PlayerPrefs.HasKey("MultiplayerInfrastructure.DialogueSkipEnabled"), Is.False);
    }

    [Test]
    public void ClearStoredValuesIsSafeWhenNothingIsStored()
    {
      foreach (var key in AllKeys())
        PlayerPrefs.DeleteKey(key);

      Assert.DoesNotThrow(() => UserPreferenceReset.ClearStoredValues(Bindings));
    }

    private static void WriteAllKeys()
    {
      foreach (var key in IntKeys)
        PlayerPrefs.SetInt(key, 1);
      foreach (var key in FloatKeys)
        PlayerPrefs.SetFloat(key, 0.5f);
      foreach (var key in StringKeys)
        PlayerPrefs.SetString(key, "stored");
    }

    private static IEnumerable<string> AllKeys()
    {
      foreach (var key in IntKeys) yield return key;
      foreach (var key in FloatKeys) yield return key;
      foreach (var key in StringKeys) yield return key;
    }
  }
}
