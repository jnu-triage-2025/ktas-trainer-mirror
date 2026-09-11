using System;
using MultiplayerInfrastructure.UI;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MultiplayerInfrastructure.Tests.UI
{
  /// <summary>
  /// 같은 오버레이가 스택에 중복으로 쌓이면 Pop 한 번으로 내려가지 않아,
  /// 대화창이 닫힌 뒤에도 플레이어 입력이 영구히 잠긴다. 그 회귀를 막는 검사다.
  /// </summary>
  public sealed class UIOverlayStackDuplicatePushTests
  {
    private sealed class FakeOverlay : IUIOverlay
    {
      private readonly string _name;

      public FakeOverlay(string name) => _name = name;

      public int PushedCount { get; private set; }
      public int PoppedCount { get; private set; }

      /// <summary>마지막 OnOverlayPopped 시점에 스택에 아직 남아 있었는지(= 가려진 것뿐인지).</summary>
      public bool WasInStackWhenPopped { get; private set; }

      /// <summary>콜백 안에서 예외를 던지는 상황을 흉내 내기 위한 훅.</summary>
      public Action PoppedHook { get; set; }

      public event Action OverlayPushed;
      public event Action OverlayPopped;

      public void OnOverlayPushed()
      {
        PushedCount++;
        OverlayPushed?.Invoke();
      }

      public void OnOverlayPopped()
      {
        PoppedCount++;
        WasInStackWhenPopped = UIOverlayStack.Contains(this);
        PoppedHook?.Invoke();
        OverlayPopped?.Invoke();
      }

      public override string ToString() => _name;
    }

    [SetUp]
    public void SetUp() => UIOverlayStack.Clear();

    [TearDown]
    public void TearDown() => UIOverlayStack.Clear();

    [Test]
    public void PushingAnOverlayThatIsAlreadyTopKeepsASingleEntry()
    {
      var dialogue = new FakeOverlay("dialogue");

      UIOverlayStack.Push(dialogue);
      UIOverlayStack.Push(dialogue);
      UIOverlayStack.Pop();

      Assert.That(UIOverlayStack.IsEmpty(), Is.True);
    }

    [Test]
    public void RepushingAnOverlayBuriedInTheStackDoesNotLeaveADuplicate()
    {
      // 대화창 위에 커맨드 채팅을 연 뒤 다음 대화 노드가 표시되는 상황을 재현한다.
      var dialogue = new FakeOverlay("dialogue");
      var chat = new FakeOverlay("chat");

      UIOverlayStack.Push(dialogue);
      UIOverlayStack.Push(chat);
      UIOverlayStack.Push(dialogue);

      Assert.That(UIOverlayStack.IsTop(dialogue), Is.True);

      // 채팅을 닫고 대화를 진행하면 스택이 완전히 비어야 한다.
      UIOverlayStack.Pop(); // dialogue
      UIOverlayStack.Pop(); // chat

      Assert.That(UIOverlayStack.IsEmpty(), Is.True,
        "대화창 항목이 중복으로 남으면 월드 입력이 영구히 잠긴다.");
    }

    [Test]
    public void RepushingRestoresTheOverlayBelowExactlyOnce()
    {
      var inventory = new FakeOverlay("inventory");
      var dialogue = new FakeOverlay("dialogue");
      var chat = new FakeOverlay("chat");

      UIOverlayStack.Push(inventory);
      UIOverlayStack.Push(dialogue);
      UIOverlayStack.Push(chat);
      UIOverlayStack.Push(dialogue);

      UIOverlayStack.Pop();

      Assert.That(UIOverlayStack.IsTop(chat), Is.True);

      UIOverlayStack.Pop();

      Assert.That(UIOverlayStack.IsTop(inventory), Is.True);
      Assert.That(UIOverlayStack.IsEmpty(), Is.False);
    }

    [Test]
    public void RemovingAnOverlayBuriedBelowAnotherOverlayDoesNotLeaveItToBlockInput()
    {
      var dialogue = new FakeOverlay("dialogue");
      var chat = new FakeOverlay("chat");

      UIOverlayStack.Push(dialogue);
      UIOverlayStack.Push(chat);

      Assert.That(UIOverlayStack.Remove(dialogue), Is.True);
      Assert.That(dialogue.PoppedCount, Is.EqualTo(2),
        "가려질 때와 실제로 제거될 때 각각 통보해야 UI가 표시 상태와 내부 상태를 구분해 정리한다.");
      Assert.That(UIOverlayStack.IsTop(chat), Is.True);

      UIOverlayStack.Pop();

      Assert.That(UIOverlayStack.IsEmpty(), Is.True,
        "종료된 대화창이 채팅 아래에 남으면 채팅을 닫은 뒤 월드 입력이 잠긴다.");
    }

    [Test]
    public void CoveredOverlayCanTellItIsStillInTheStackAndReturnsToTopWhenUncovered()
    {
      // 대화창 위에 커맨드 채팅이 열렸다가 닫히는 상황. 대화창은 가려질 때 상태를 지우면 안 된다.
      var dialogue = new FakeOverlay("dialogue");
      var chat = new FakeOverlay("chat");

      UIOverlayStack.Push(dialogue);
      UIOverlayStack.Push(chat);

      Assert.That(dialogue.PoppedCount, Is.EqualTo(1));
      Assert.That(dialogue.WasInStackWhenPopped, Is.True,
        "다른 오버레이에 가려진 것뿐이면 Contains 가 true 여야 표시만 숨기고 상태를 유지할 수 있다.");
      Assert.That(UIOverlayStack.Contains(dialogue), Is.True);

      UIOverlayStack.Pop(); // chat

      Assert.That(UIOverlayStack.IsTop(dialogue), Is.True);
      Assert.That(dialogue.PushedCount, Is.EqualTo(2), "덮개가 닫히면 다시 Pushed 통보를 받아 표시를 복구한다.");

      UIOverlayStack.Pop(); // dialogue

      Assert.That(dialogue.WasInStackWhenPopped, Is.False, "실제로 스택에서 빠질 때는 Contains 가 false 다.");
      Assert.That(UIOverlayStack.Contains(dialogue), Is.False);
    }

    [Test]
    public void RemovedOverlayIsNotInTheStackDuringItsPoppedCallback()
    {
      var dialogue = new FakeOverlay("dialogue");
      var chat = new FakeOverlay("chat");

      UIOverlayStack.Push(dialogue);
      UIOverlayStack.Push(chat);
      UIOverlayStack.Remove(dialogue);

      Assert.That(dialogue.WasInStackWhenPopped, Is.False);

      UIOverlayStack.Clear();

      Assert.That(chat.WasInStackWhenPopped, Is.False);
    }

    [Test]
    public void ExceptionInsidePoppedCallbackStillLeavesStackAndNotificationConsistent()
    {
      // 콜백 예외가 새어 나오면 스택은 비었는데 StackChanged 가 발행되지 않아 이동 불가·커서 해제 상태가 남는다.
      var inventory = new FakeOverlay("inventory")
      {
        PoppedHook = () => throw new InvalidOperationException("held item return failed"),
      };

      int changedCount = 0;
      Action onChanged = () => changedCount++;
      UIOverlayStack.StackChanged += onChanged;
      bool previousIgnore = LogAssert.ignoreFailingMessages;
      LogAssert.ignoreFailingMessages = true;
      try
      {
        UIOverlayStack.Push(inventory);
        Assert.That(changedCount, Is.EqualTo(1));

        Assert.DoesNotThrow(() => UIOverlayStack.Pop());

        Assert.That(UIOverlayStack.IsEmpty(), Is.True);
        Assert.That(changedCount, Is.EqualTo(2), "예외가 나도 스택 변경 통보는 반드시 나가야 한다.");
      }
      finally
      {
        LogAssert.ignoreFailingMessages = previousIgnore;
        UIOverlayStack.StackChanged -= onChanged;
      }
    }
  }
}
