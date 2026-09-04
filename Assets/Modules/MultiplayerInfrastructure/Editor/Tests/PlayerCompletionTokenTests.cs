using System.Linq;
using MultiplayerInfrastructure.Command;
using MultiplayerInfrastructure.Session;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests
{
  /// <summary>
  /// 자동 완성 후보 목록에 플레이어를 지정하는 모든 수단이 함께 오르는지, 그리고 각 후보에
  /// 사람이 알아볼 수 있는 설명이 붙는지 확인한다.
  /// </summary>
  public sealed class PlayerCompletionTokenTests
  {
    private const string AliceIdentifier = "aaaaaaaa11112222333344445555aaaa";
    private const string BobIdentifier = "bbbbbbbb11112222333344445555bbbb";
    private const int AliceClientId = 1;
    private const int BobClientId = 4;

    [SetUp]
    public void RegisterPlayers()
    {
      UserDescriptorService.Register(AliceClientId, new UserDescriptor(AliceIdentifier, "Alice Kim"));
      UserDescriptorService.Register(BobClientId, new UserDescriptor(BobIdentifier, "Bob"));
    }

    [TearDown]
    public void UnregisterPlayers()
    {
      UserDescriptorService.Unregister(AliceIdentifier);
      UserDescriptorService.Unregister(BobIdentifier);
    }

    [Test]
    public void SuggestionsCarryNamesIdentifiersClientIdsAndSelectorsTogether()
    {
      var suggestions = PlayerTargetResolver.CollectTokenSuggestions();

      Assert.That(suggestions, Contains.Item("\"Alice Kim\""), "공백이 있는 이름은 따옴표로 감싸 제안해야 한다.");
      Assert.That(suggestions, Contains.Item("Bob"));
      Assert.That(suggestions, Contains.Item("id:" + AliceIdentifier));
      Assert.That(suggestions, Contains.Item("id:" + BobIdentifier));
      Assert.That(suggestions, Contains.Item("fish:" + AliceClientId));
      Assert.That(suggestions, Contains.Item("fish:" + BobClientId));
      Assert.That(suggestions, Is.SupersetOf(new[] { "@s", "@a", "@p", "@r", "@n", "@e" }));
      Assert.That(suggestions.Distinct().Count(), Is.EqualTo(suggestions.Count));
    }

    [TestCase("id:aaaaaaaa11112222333344445555aaaa", "Alice Kim")]
    [TestCase("id:bbbbbbbb", "Bob")]
    [TestCase("fish:1", "Alice Kim")]
    [TestCase("fish:4", "Bob")]
    [TestCase("client:4", "Bob")]
    public void IdentifierAndClientIdTokensAreLabelledWithThePlayerName(string token, string expected)
    {
      Assert.That(PlayerTargetResolver.DescribeToken(token), Is.EqualTo(expected));
    }

    [TestCase("@s", "yourself")]
    [TestCase("@self", "yourself")]
    [TestCase("@a", "all players")]
    [TestCase("@p", "nearest player")]
    [TestCase("@r", "random player")]
    public void SelectorsAreLabelledWithTheirMeaning(string selector, string expected)
    {
      Assert.That(PlayerTargetResolver.DescribeToken(selector), Is.EqualTo(expected));
    }

    [Test]
    public void EverySuggestedSelectorHasAMeaning()
    {
      foreach (string selector in PlayerTargetResolver.CollectSelectorSuggestions())
        Assert.That(PlayerTargetResolver.DescribeToken(selector), Is.Not.Empty, selector);
    }

    [TestCase("Bob")]
    [TestCase("\"Alice Kim\"")]
    [TestCase("apple")]
    [TestCase("id:ffffffff")]
    [TestCase("fish:99")]
    [TestCase("fish:abc")]
    [TestCase("@zzz")]
    [TestCase("")]
    [TestCase(null)]
    public void TokensWithoutAnExplanationReturnAnEmptyDescription(string token)
    {
      Assert.That(PlayerTargetResolver.DescribeToken(token), Is.Empty);
    }

    [Test]
    public void SelectorCompletionFillsTheTokenAndExposesItsMeaning()
    {
      var service = new ChatCommandCompletionService(null);

      var result = service.HandleTabPress("/tp @", 5);

      Assert.That(result, Is.Not.Null, "@ 로 시작하는 토큰에는 선택자 후보가 나와야 한다.");
      Assert.That(service.HasActiveSession, Is.True);
      Assert.That(service.ActiveCandidates, Is.Not.Null);
      Assert.That(service.ActiveCandidates.Select(candidate => candidate.Text), Is.SupersetOf(new[] { "@a", "@s" }));

      foreach (var candidate in service.ActiveCandidates)
        Assert.That(candidate.Description, Is.Not.Empty, candidate.Text);
    }

    [Test]
    public void PlainChatCompletesDisplayNamesAndQuotesNamesThatContainSpaces()
    {
      var service = new ChatCommandCompletionService(null);

      var result = service.HandleTabPress("Ali", 3);

      Assert.That(result, Is.Not.Null);
      Assert.That(result.Value.text, Does.StartWith("\"Alice Kim\""));
    }
  }
}
