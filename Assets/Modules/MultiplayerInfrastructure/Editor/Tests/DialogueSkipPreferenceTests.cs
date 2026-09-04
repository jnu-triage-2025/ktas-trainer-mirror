using MultiplayerInfrastructure.UI;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.UI
{
  /// <summary>
  /// 대화 연출 건너뛰기 설정의 기본값·저장·복원 규칙과, 건너뛰기가 꺼져 있을 때 재생 정지를
  /// 감지하는 판정을 검증합니다. 이 판정이 틀리면 대화창이 입력만 소비하며 영원히 닫히지 않으므로
  /// 시나리오 흐름이 멈춥니다.
  /// </summary>
  public sealed class DialogueSkipPreferenceTests
  {
    private bool _hadStoredValue;
    private bool _storedValue;
    private bool _liveValue;

    [SetUp]
    public void SetUp()
    {
      // 편집기 PlayerPrefs를 건드리므로 원래 값을 보관했다가 되돌린다.
      _hadStoredValue = UnityEngine.PlayerPrefs.HasKey("MultiplayerInfrastructure.DialogueSkipEnabled");
      _storedValue = DialogueSkipPreference.ReadStoredValue();
      _liveValue = DialogueSkipPreference.IsEnabled;
      DialogueSkipPreference.ClearStoredValue();
      DialogueSkipPreference.LoadAndApply();
    }

    [TearDown]
    public void TearDown()
    {
      if (_hadStoredValue)
        DialogueSkipPreference.SetEnabled(_storedValue);
      else
        DialogueSkipPreference.ClearStoredValue();

      DialogueSkipPreference.LoadAndApply();
      if (DialogueSkipPreference.IsEnabled != _liveValue)
        DialogueSkipPreference.SetEnabled(_liveValue);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 기본값 / 저장 / 복원
    // ──────────────────────────────────────────────────────────────────────────
    [Test]
    public void DefaultsToEnabledWhenNothingIsStored()
    {
      Assert.That(DialogueSkipPreference.IsEnabled, Is.True);
      Assert.That(DialogueSkipPreference.ReadStoredValue(), Is.True);
    }

    [Test]
    public void DisablingPersistsAndSurvivesReload()
    {
      DialogueSkipPreference.SetEnabled(false);
      DialogueSkipPreference.LoadAndApply();

      Assert.That(DialogueSkipPreference.IsEnabled, Is.False);

      DialogueSkipPreference.SetEnabled(true);
      DialogueSkipPreference.LoadAndApply();

      Assert.That(DialogueSkipPreference.IsEnabled, Is.True);
    }

    [Test]
    public void ClearingStoredValueRestoresTheDefault()
    {
      DialogueSkipPreference.SetEnabled(false);
      DialogueSkipPreference.ClearStoredValue();
      DialogueSkipPreference.LoadAndApply();

      Assert.That(DialogueSkipPreference.IsEnabled, Is.True);
    }

    [Test]
    public void ChangeEventFiresOnlyWhenTheValueActuallyChanges()
    {
      int calls = 0;
      bool last = true;
      void Handler(bool value)
      {
        calls++;
        last = value;
      }

      DialogueSkipPreference.EnabledChanged += Handler;
      try
      {
        DialogueSkipPreference.SetEnabled(true);
        Assert.That(calls, Is.EqualTo(0));

        DialogueSkipPreference.SetEnabled(false);
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(last, Is.False);

        DialogueSkipPreference.SetEnabled(false);
        Assert.That(calls, Is.EqualTo(1));
      }
      finally
      {
        DialogueSkipPreference.EnabledChanged -= Handler;
      }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 재생 정지 감지 (건너뛰기가 꺼져 있을 때의 안전 장치)
    // ──────────────────────────────────────────────────────────────────────────
    [Test]
    public void NormalTypingProgressIsNotTreatedAsStalled()
    {
      const float typingSpeed = 0.05f;

      Assert.That(DialoguePanelUIController.IsTypingStalled(0f, typingSpeed), Is.False);
      Assert.That(DialoguePanelUIController.IsTypingStalled(typingSpeed, typingSpeed), Is.False);
      Assert.That(DialoguePanelUIController.IsTypingStalled(
        DialoguePanelUIController.TypingStallGraceSeconds, typingSpeed), Is.False);
    }

    [Test]
    public void LongSilenceAfterLastCharacterCountsAsStalled()
    {
      const float typingSpeed = 0.05f;

      Assert.That(DialoguePanelUIController.IsTypingStalled(
        DialoguePanelUIController.TypingStallGraceSeconds + 0.01f, typingSpeed), Is.True);
    }

    [Test]
    public void SlowTypingSpeedWidensTheStallGrace()
    {
      // 글자 간격이 1초라면 4초까지는 정상 재생으로 본다.
      const float typingSpeed = 1f;

      Assert.That(DialoguePanelUIController.IsTypingStalled(3.9f, typingSpeed), Is.False);
      Assert.That(DialoguePanelUIController.IsTypingStalled(4.1f, typingSpeed), Is.True);
    }

    [Test]
    public void NegativeTypingSpeedFallsBackToTheBaseGrace()
    {
      Assert.That(DialoguePanelUIController.IsTypingStalled(
        DialoguePanelUIController.TypingStallGraceSeconds + 0.01f, -1f), Is.True);
    }
  }
}
