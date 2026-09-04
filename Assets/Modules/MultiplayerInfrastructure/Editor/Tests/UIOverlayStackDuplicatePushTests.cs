using System;
using MultiplayerInfrastructure.UI;
using NUnit.Framework;

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
      Assert.That(dialogue.PoppedCount, Is.EqualTo(1));
      Assert.That(UIOverlayStack.IsTop(chat), Is.True);

      UIOverlayStack.Pop();

      Assert.That(UIOverlayStack.IsEmpty(), Is.True,
        "종료된 대화창이 채팅 아래에 남으면 채팅을 닫은 뒤 월드 입력이 잠긴다.");
    }
  }
}
