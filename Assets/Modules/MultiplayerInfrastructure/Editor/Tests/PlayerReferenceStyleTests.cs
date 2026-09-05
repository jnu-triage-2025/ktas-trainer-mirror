using MultiplayerInfrastructure.Command;
using MultiplayerInfrastructure.Session;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests
{
  /// <summary>
  /// 시스템 메시지가 플레이어를 지칭할 때, 명령이 대상을 지정한 방식에 따라 표시 이름만 쓰거나
  /// <c>이름(clientId)</c>·<c>이름(uuid)</c> 형식을 쓰는지 확인한다.
  /// </summary>
  public sealed class PlayerReferenceStyleTests
  {
    private const string AliceIdentifier = "aaaaaaaa11112222333344445555aaaa";
    private const int AliceClientId = 7;
    private UserDescriptor _alice;

    [SetUp]
    public void RegisterPlayer()
    {
      _alice = new UserDescriptor(AliceIdentifier, "Alice Kim");
      UserDescriptorService.Register(AliceClientId, _alice);
    }

    [TearDown]
    public void UnregisterPlayer()
    {
      UserDescriptorService.Unregister(AliceIdentifier);
    }

    [TestCase(null, PlayerReferenceStyle.DisplayName)]
    [TestCase("", PlayerReferenceStyle.DisplayName)]
    [TestCase("@s", PlayerReferenceStyle.DisplayName)]
    [TestCase("@a[tag=nurse]", PlayerReferenceStyle.DisplayName)]
    [TestCase("me", PlayerReferenceStyle.DisplayName)]
    [TestCase("Alice Kim", PlayerReferenceStyle.DisplayName)]
    [TestCase("alice kim", PlayerReferenceStyle.DisplayName)]
    [TestCase("name:Alice Kim", PlayerReferenceStyle.DisplayName)]
    [TestCase("fish:7", PlayerReferenceStyle.ClientId)]
    [TestCase("client:7", PlayerReferenceStyle.ClientId)]
    [TestCase("conn:7", PlayerReferenceStyle.ClientId)]
    [TestCase("id:aaaaaaaa11112222333344445555aaaa", PlayerReferenceStyle.Identifier)]
    [TestCase("uuid:aaaaaaaa", PlayerReferenceStyle.Identifier)]
    [TestCase("aaaaaaaa11112222333344445555aaaa", PlayerReferenceStyle.Identifier)]
    [TestCase("aaaaaaaa1111", PlayerReferenceStyle.Identifier)]
    public void ReferenceStyleFollowsHowTheTargetWasAddressed(string token, PlayerReferenceStyle expected)
    {
      Assert.That(PlayerTargetResolver.GetReferenceStyle(token, _alice, null), Is.EqualTo(expected));
    }

    [Test]
    public void BareNumberIsAClientIdOnlyWhenItMatchedTheConnection()
    {
      // 연결이 없으면 숫자 토큰이 연결 번호로 해석됐다고 단정할 수 없으므로 표시 이름으로 돌아간다.
      Assert.That(PlayerTargetResolver.GetReferenceStyle("7", _alice, null), Is.EqualTo(PlayerReferenceStyle.DisplayName));
    }

    [Test]
    public void DisplayNameStyleUsesTheNameOnly()
    {
      Assert.That(PlayerTargetResolver.DescribeTarget("@s", _alice, null), Is.EqualTo("Alice Kim"));
      Assert.That(PlayerTargetResolver.DescribeTarget("Alice Kim", _alice, null), Is.EqualTo("Alice Kim"));
      Assert.That(PlayerTargetResolver.DescribeTarget(null, _alice, null), Is.EqualTo("Alice Kim"));
    }

    [Test]
    public void IdentifierStyleAppendsTheFullUuid()
    {
      Assert.That(PlayerTargetResolver.DescribeTarget("id:aaaaaaaa", _alice, null),
        Is.EqualTo("Alice Kim(aaaaaaaa11112222333344445555aaaa)"));
      Assert.That(PlayerTargetResolver.DescribeTarget("aaaaaaaa11112222333344445555aaaa", _alice, null),
        Is.EqualTo("Alice Kim(aaaaaaaa11112222333344445555aaaa)"));
    }

    [Test]
    public void ClientIdStyleFallsBackToTheNameWithoutAConnection()
    {
      // fish: 로 지정했더라도 연결 객체가 없으면 붙일 번호가 없으므로 이름만 남긴다.
      Assert.That(PlayerTargetResolver.DescribeTarget("fish:7", _alice, null), Is.EqualTo("Alice Kim"));
    }

    [Test]
    public void UnknownDescriptorDegradesGracefully()
    {
      Assert.That(PlayerTargetResolver.DescribeTarget("id:zzzz", null, null), Is.EqualTo("Unknown"));
    }
  }
}
