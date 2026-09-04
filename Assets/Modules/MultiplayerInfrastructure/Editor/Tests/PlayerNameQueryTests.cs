using System.Collections.Generic;
using MultiplayerInfrastructure.Session;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests
{
  public sealed class PlayerNameQueryTests
  {
    private static readonly UserDescriptor Alice = new("aaaaaaaa-0000-0000-0000-000000000000", "Alice Kim");
    private static readonly UserDescriptor Bob = new("bbbbbbbb-0000-0000-0000-000000000000", "Bob");
    private static readonly UserDescriptor Bobby = new("cccccccc-0000-0000-0000-000000000000", "Bobby");

    private static List<UserDescriptor> Source => new() { Alice, Bob, Bobby };

    [TestCase("\"Alice Kim\"", "Alice Kim")]
    [TestCase("  'Bob'  ", "Bob")]
    [TestCase("Bob", "Bob")]
    public void NormalizeStripsWrappingQuotesLeftByTheChatTokenizer(string input, string expected)
    {
      Assert.That(PlayerNameQuery.Normalize(input), Is.EqualTo(expected));
    }

    [TestCase("Alice Kim", "alicekim")]
    [TestCase("alice_kim", "alicekim")]
    [TestCase("ALICE-KIM", "alicekim")]
    public void NormalizeLooseIgnoresCaseWhitespaceAndPunctuation(string input, string expected)
    {
      Assert.That(PlayerNameQuery.NormalizeLoose(input), Is.EqualTo(expected));
    }

    [Test]
    public void QuotedDisplayNameWithWhitespaceResolves()
    {
      Assert.That(
        PlayerNameQuery.TryResolve("\"Alice Kim\"", Source, out var descriptor, out _, out var kind),
        Is.True);
      Assert.That(descriptor, Is.SameAs(Alice));
      Assert.That(kind, Is.EqualTo(PlayerNameMatchKind.DisplayName));
    }

    [Test]
    public void DisplayNameMatchIgnoresCase()
    {
      Assert.That(PlayerNameQuery.TryResolve("bob", Source, out var descriptor, out _, out var kind), Is.True);
      Assert.That(descriptor, Is.SameAs(Bob));
      Assert.That(kind, Is.EqualTo(PlayerNameMatchKind.DisplayName));
    }

    [Test]
    public void ExactDisplayNameWinsOverLongerPrefixCandidate()
    {
      Assert.That(PlayerNameQuery.TryResolve("Bob", Source, out var descriptor, out _, out _), Is.True);
      Assert.That(descriptor, Is.SameAs(Bob));
    }

    [Test]
    public void SpacelessInputMatchesNameThatContainsWhitespace()
    {
      Assert.That(PlayerNameQuery.TryResolve("alicekim", Source, out var descriptor, out _, out var kind), Is.True);
      Assert.That(descriptor, Is.SameAs(Alice));
      Assert.That(kind, Is.EqualTo(PlayerNameMatchKind.DisplayNameLoose));
    }

    [Test]
    public void UniquePrefixResolvesAndPartialNameResolvesWhenUnambiguous()
    {
      Assert.That(PlayerNameQuery.TryResolve("Ali", Source, out var byPrefix, out _, out _), Is.True);
      Assert.That(byPrefix, Is.SameAs(Alice));

      Assert.That(PlayerNameQuery.TryResolve("Kim", Source, out var byContains, out _, out var kind), Is.True);
      Assert.That(byContains, Is.SameAs(Alice));
      Assert.That(kind, Is.EqualTo(PlayerNameMatchKind.DisplayNameContains));
    }

    [Test]
    public void AmbiguousPrefixReportsEveryCandidateInsteadOfGuessing()
    {
      Assert.That(PlayerNameQuery.TryResolve("Bo", Source, out var descriptor, out var ambiguous, out _), Is.False);
      Assert.That(descriptor, Is.Null);
      Assert.That(ambiguous, Is.EquivalentTo(new[] { Bob, Bobby }));
      Assert.That(PlayerNameQuery.DescribeCandidates(ambiguous), Does.Contain("Bobby"));
    }

    [Test]
    public void IdentifierMatchesExactlyAndByPrefix()
    {
      Assert.That(
        PlayerNameQuery.TryResolve(Alice.Identifier, Source, out var exact, out _, out var exactKind),
        Is.True);
      Assert.That(exact, Is.SameAs(Alice));
      Assert.That(exactKind, Is.EqualTo(PlayerNameMatchKind.Identifier));

      Assert.That(PlayerNameQuery.TryResolve("aaaaaaaa", Source, out var byPrefix, out _, out var prefixKind), Is.True);
      Assert.That(byPrefix, Is.SameAs(Alice));
      Assert.That(prefixKind, Is.EqualTo(PlayerNameMatchKind.IdentifierPrefix));
    }

    [Test]
    public void UnknownAndEmptyTokensDoNotResolve()
    {
      Assert.That(PlayerNameQuery.TryResolve("Charlie", Source, out _, out _, out _), Is.False);
      Assert.That(PlayerNameQuery.TryResolve("   ", Source, out _, out _, out _), Is.False);
      Assert.That(PlayerNameQuery.TryResolve(null, Source, out _, out _, out _), Is.False);
    }

    [Test]
    public void DuplicateDisplayNamesResolveToTheSameDescriptorEveryTime()
    {
      var duplicates = new List<UserDescriptor>
      {
        new("zzzzzzzz-0000-0000-0000-000000000000", "duplicate"),
        new("dddddddd-0000-0000-0000-000000000000", "duplicate"),
      };

      Assert.That(PlayerNameQuery.TryResolve("duplicate", duplicates, out var descriptor, out _, out _), Is.True);
      Assert.That(descriptor.Identifier, Is.EqualTo("dddddddd-0000-0000-0000-000000000000"));
    }
  }
}
