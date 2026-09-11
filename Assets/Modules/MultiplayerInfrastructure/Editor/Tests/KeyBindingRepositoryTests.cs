using System.Collections.Generic;
using MultiplayerInfrastructure.UI;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.UI
{
  /// <summary>
  /// 저장된 키 바인딩을 읽을 때 <see cref="KeyCode"/>에 없는 정수를 걸러 내는지 검증합니다.
  /// 걸러 내지 못하면 정의되지 않은 키가 입력 판정에 들어가고 설정 화면에는 숫자만 표시됩니다.
  /// </summary>
  public sealed class KeyBindingRepositoryTests
  {
    private const string ActionId = "unit_test_action";
    private const string PrefsKey = "KeyBinding_" + ActionId;

    private bool _hadStoredValue;
    private int _storedValue;

    [SetUp]
    public void SetUp()
    {
      _hadStoredValue = PlayerPrefs.HasKey(PrefsKey);
      _storedValue = _hadStoredValue ? PlayerPrefs.GetInt(PrefsKey) : 0;
      PlayerPrefs.DeleteKey(PrefsKey);
    }

    [TearDown]
    public void TearDown()
    {
      if (_hadStoredValue)
        PlayerPrefs.SetInt(PrefsKey, _storedValue);
      else
        PlayerPrefs.DeleteKey(PrefsKey);
      PlayerPrefs.Save();
    }

    [Test]
    public void UndefinedStoredValueFallsBackToDefault()
    {
      PlayerPrefs.SetInt(PrefsKey, 987654);

      Assert.That(KeyBindingRepository.GetBoundKey(ActionId, KeyCode.E), Is.EqualTo(KeyCode.E));
      Assert.That(KeyBindingRepository.TryGetStoredKey(ActionId, out _), Is.False);

      var bindings = new List<KeyBindingEntry> { new KeyBindingEntry(ActionId, "테스트", KeyCode.E) };
      Assert.That(KeyBindingRepository.LoadInto(bindings), Is.False);
      Assert.That(bindings[0].boundKey, Is.EqualTo(KeyCode.E));
    }

    [Test]
    public void DefinedStoredValueIsUsed()
    {
      PlayerPrefs.SetInt(PrefsKey, (int)KeyCode.F5);

      Assert.That(KeyBindingRepository.GetBoundKey(ActionId, KeyCode.E), Is.EqualTo(KeyCode.F5));

      var bindings = new List<KeyBindingEntry> { new KeyBindingEntry(ActionId, "테스트", KeyCode.E) };
      Assert.That(KeyBindingRepository.LoadInto(bindings), Is.True);
      Assert.That(bindings[0].boundKey, Is.EqualTo(KeyCode.F5));
    }

    [Test]
    public void MissingValueFallsBackToDefault()
    {
      Assert.That(KeyBindingRepository.GetBoundKey(ActionId, KeyCode.E), Is.EqualTo(KeyCode.E));
      Assert.That(KeyBindingRepository.TryGetStoredKey(ActionId, out var key), Is.False);
      Assert.That(key, Is.EqualTo(KeyCode.None));
    }

    [Test]
    public void IsValidStoredKeyRecognisesOnlyRealKeyCodes()
    {
      Assert.That(KeyBindingRepository.IsValidStoredKey((int)KeyCode.Space), Is.True);
      Assert.That(KeyBindingRepository.IsValidStoredKey((int)KeyCode.None), Is.True);
      Assert.That(KeyBindingRepository.IsValidStoredKey(-1), Is.False);
      Assert.That(KeyBindingRepository.IsValidStoredKey(987654), Is.False);
    }
  }
}
