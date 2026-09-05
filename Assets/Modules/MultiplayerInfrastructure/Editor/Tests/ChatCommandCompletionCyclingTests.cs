using System.Collections.Generic;
using MultiplayerInfrastructure.Command;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Command
{
  /// <summary>
  /// Tab 을 연달아 누르면 후보 사이를 오갈 수 있어야 한다. 세션이 한 번 누를 때마다
  /// 끊기면 첫 후보가 확정된 뒤 곧바로 다음 인수 자동완성으로 넘어가 버려서,
  /// 사용자는 두 번째 후보를 아예 고를 수 없다. 그 회귀를 막는 검사다.
  /// </summary>
  public sealed class ChatCommandCompletionCyclingTests
  {
    private GameObject _host;
    private ChatCommandService _commandService;
    private ChatCommandCompletionService _completion;

    [SetUp]
    public void SetUp()
    {
      _host = new GameObject(nameof(ChatCommandCompletionCyclingTests));
      _commandService = _host.AddComponent<ChatCommandService>();
      // 커맨드 등록만 필요하므로 채팅 서비스는 연결하지 않는다.
      _commandService.Initialize(null);
      _completion = new ChatCommandCompletionService(_commandService);
    }

    [TearDown]
    public void TearDown()
    {
      if (_host != null)
        Object.DestroyImmediate(_host);
    }

    [Test]
    public void RepeatedTabMovesToTheNextCandidate()
    {
      var first = _completion.HandleTabPress("/s", 2);
      Assert.That(first.HasValue, Is.True, "`/s` 에 대한 후보가 하나도 없다.");

      IReadOnlyList<ChatCommandCompletionService.CompletionCandidate> candidates =
        _completion.ActiveCandidates;
      Assert.That(candidates, Is.Not.Null);
      Assert.That(candidates.Count, Is.GreaterThan(1),
        "후보가 둘 이상이어야 전환을 확인할 수 있다.");

      // 사용자가 아무 키도 누르지 않고 Tab 만 다시 누른 상황을 그대로 재현한다.
      var second = _completion.HandleTabPress(first.Value.text, first.Value.cursorPos);
      Assert.That(second.HasValue, Is.True);
      Assert.That(second.Value.text, Is.Not.EqualTo(first.Value.text),
        "두 번째 Tab 이 다음 후보로 전환하지 않고 같은 결과를 반복했다.");
      Assert.That(_completion.ActiveCandidateIndex, Is.EqualTo(1));

      // 전환한 결과도 여전히 커맨드 이름 자리를 채운 것이어야 한다. 다음 인수
      // 자동완성으로 넘어갔다면 완성된 커맨드 뒤에 인수가 덧붙는다.
      Assert.That(second.Value.text.Trim().Split(' ').Length, Is.EqualTo(1),
        $"커맨드 이름 대신 다음 인수를 완성했다: {second.Value.text}");
    }

    [Test]
    public void CyclingPastTheLastCandidateWrapsToTheFirst()
    {
      var result = _completion.HandleTabPress("/s", 2);
      Assert.That(result.HasValue, Is.True);

      int count = _completion.ActiveCandidates.Count;
      string firstText = result.Value.text;

      for (int i = 0; i < count; i++)
        result = _completion.HandleTabPress(result.Value.text, result.Value.cursorPos);

      Assert.That(result.Value.text, Is.EqualTo(firstText));
      Assert.That(_completion.ActiveCandidateIndex, Is.EqualTo(0));
    }

    [Test]
    public void EditingTheInputStartsANewSession()
    {
      var first = _completion.HandleTabPress("/s", 2);
      Assert.That(first.HasValue, Is.True);

      // 완성된 텍스트를 사용자가 직접 고친 상황이다. 이때는 순환이 아니라
      // 고쳐진 낱말에 대한 새 후보 목록이 만들어져야 한다.
      var afterEdit = _completion.HandleTabPress("/he", 3);
      Assert.That(afterEdit.HasValue, Is.True);
      Assert.That(afterEdit.Value.text, Does.StartWith("/help"));
      Assert.That(_completion.ActiveCandidateIndex, Is.EqualTo(0));
    }

    [Test]
    public void ResettingTheSessionClearsTheCandidateList()
    {
      Assert.That(_completion.HandleTabPress("/s", 2).HasValue, Is.True);
      Assert.That(_completion.HasActiveSession, Is.True);

      _completion.ResetSession();

      Assert.That(_completion.HasActiveSession, Is.False);
      Assert.That(_completion.ActiveCandidates, Is.Null);
    }

    [Test]
    public void ArrowKeysMoveTheSelectionInBothDirectionsAndWrap()
    {
      var first = _completion.HandleTabPress("/s", 2);
      Assert.That(first.HasValue, Is.True);
      int count = _completion.ActiveCandidates.Count;
      Assert.That(count, Is.GreaterThan(1));

      var down = _completion.MoveSelection(1);
      Assert.That(down.HasValue, Is.True);
      Assert.That(_completion.ActiveCandidateIndex, Is.EqualTo(1));
      Assert.That(down.Value.text, Is.Not.EqualTo(first.Value.text));

      // 위로 두 칸: 처음을 지나 마지막 후보로 이어진다.
      _completion.MoveSelection(-1);
      var wrapped = _completion.MoveSelection(-1);
      Assert.That(wrapped.HasValue, Is.True);
      Assert.That(_completion.ActiveCandidateIndex, Is.EqualTo(count - 1));
      Assert.That(wrapped.Value.text, Does.StartWith(_completion.ActiveCandidates[count - 1].Text));
    }

    [Test]
    public void TabContinuesFromTheCandidatePickedWithTheArrowKeys()
    {
      Assert.That(_completion.HandleTabPress("/s", 2).HasValue, Is.True);
      var moved = _completion.MoveSelection(1);
      Assert.That(moved.HasValue, Is.True);

      // 화살표로 고른 뒤 Tab 을 누르면 그 자리에서 이어서 순환해야 한다.
      var next = _completion.HandleTabPress(moved.Value.text, moved.Value.cursorPos);
      Assert.That(next.HasValue, Is.True);
      Assert.That(_completion.ActiveCandidateIndex, Is.EqualTo(2 % _completion.ActiveCandidates.Count));
    }

    [Test]
    public void MovingTheSelectionWithoutASessionDoesNothing()
    {
      Assert.That(_completion.MoveSelection(1), Is.Null);
      Assert.That(_completion.HasActiveSession, Is.False);
    }

    [Test]
    public void CommandNameCandidatesCarryTheirDescription()
    {
      Assert.That(_completion.HandleTabPress("/he", 3).HasValue, Is.True);

      var candidates = _completion.ActiveCandidates;
      Assert.That(candidates, Is.Not.Null);
      Assert.That(candidates.Count, Is.GreaterThan(0));
      Assert.That(candidates[0].Text, Is.EqualTo("/help"));
      Assert.That(candidates[0].Description, Is.Not.Empty,
        "후보 목록 오버레이에 함께 보여 줄 설명이 비어 있다.");
    }
  }
}
