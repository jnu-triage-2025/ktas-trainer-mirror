using System.Collections.Generic;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// 신호 발신자 귀속 판정의 규약을 고정한다.
  ///
  /// <para>여기서 확인하는 핵심은 두 가지다. 첫째, 다른 참여자가 올린 신호가 소유자의 목표를
  /// 완료시키지 않아야 한다. 둘째, 귀속을 확인할 수 없는 모든 상황에서는 전역 판정으로 물러서서
  /// 세션 진행이 멈추지 않아야 한다.</para>
  /// </summary>
  public sealed class ScenarioSignalAttributionTests
  {
    private const string OwnerIdentifier = "player-owner";
    private const string OtherIdentifier = "player-other";

    private readonly List<string> _raisedSignals = new();

    [SetUp]
    public void SetUp() => ScenarioNetworkRelay.FlushSignalParametersAuthoritative();

    [TearDown]
    public void TearDown()
    {
      for (int i = 0; i < _raisedSignals.Count; i++)
        Registry.Registry.Unregister(RegistryType.RuntimeState, _raisedSignals[i]);
      _raisedSignals.Clear();
      ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
    }

    /// <summary>발신자 귀속과 함께 신호를 올린다.</summary>
    private void RaiseBy(string signalId, string playerIdentifier)
    {
      string normalized = ScenarioInteractionSignals.Normalize(signalId);
      _raisedSignals.Add(normalized);
      Assert.That(
        ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(normalized, null, playerIdentifier, playerIdentifier),
        Is.True,
        $"신호 '{normalized}' 를 '{playerIdentifier}' 귀속으로 올리지 못했습니다.");
    }

    /// <summary>귀속 기록 없이 전역 신호만 올린다(저장소 한도 초과·미러 유실 상황의 재현).</summary>
    private void RaiseWithoutAttribution(string signalId)
    {
      string normalized = ScenarioInteractionSignals.Normalize(signalId);
      _raisedSignals.Add(normalized);
      Registry.Registry.Register(RegistryType.RuntimeState, normalized, true);
    }

    [Test]
    public void UnraisedSignalIsNotSatisfiedInAnyScope()
    {
      const string signal = "attribution.unraised";

      Assert.That(ScenarioSignalAttribution.IsRaised(signal), Is.False);
      Assert.That(ScenarioSignalAttribution.IsRaisedBy(signal, OwnerIdentifier), Is.False);
    }

    [Test]
    public void SignalRaisedByOwnerSatisfiesOwnerScope()
    {
      const string signal = "attribution.by-owner";
      RaiseBy(signal, OwnerIdentifier);

      Assert.That(ScenarioSignalAttribution.IsRaisedBy(signal, OwnerIdentifier), Is.True);
    }

    /// <summary>교차 완료를 막는 핵심 판정이다.</summary>
    [Test]
    public void SignalRaisedByAnotherPlayerDoesNotSatisfyOwnerScope()
    {
      const string signal = "attribution.by-other";
      RaiseBy(signal, OtherIdentifier);

      Assert.That(ScenarioSignalAttribution.IsRaised(signal), Is.True,
        "전역 신호 판정은 그대로 유지되어야 합니다. 그래프 게이트가 이 판정을 사용합니다.");
      Assert.That(ScenarioSignalAttribution.IsRaisedBy(signal, OwnerIdentifier), Is.False,
        "다른 참여자가 올린 신호가 소유자의 목표를 완료시켰습니다.");
    }

    [Test]
    public void MissingAttributionRecordFallsBackToGlobalDecision()
    {
      const string signal = "attribution.no-record";
      RaiseWithoutAttribution(signal);

      Assert.That(ScenarioSignalParameterStore.HasAnyRecord(signal), Is.False);
      Assert.That(ScenarioSignalAttribution.IsRaisedBy(signal, OwnerIdentifier), Is.True,
        "귀속 기록이 없다는 이유로 목표가 미완료로 남으면 진행이 막힙니다.");
    }

    [Test]
    public void SystemRaisedSignalSatisfiesEveryPlayer()
    {
      const string signal = "attribution.by-system";
      RaiseBy(signal, ScenarioSignalParameterStore.ServerPlayerIdentifier);

      Assert.That(ScenarioSignalAttribution.IsRaisedBy(signal, OwnerIdentifier), Is.True);
      Assert.That(ScenarioSignalAttribution.IsRaisedBy(signal, OtherIdentifier), Is.True);
    }

    [Test]
    public void UnknownPlayerIdentifierFallsBackToGlobalDecision()
    {
      const string signal = "attribution.unknown-owner";
      RaiseBy(signal, OtherIdentifier);

      Assert.That(ScenarioSignalAttribution.IsRaisedBy(signal, null), Is.True);
      Assert.That(ScenarioSignalAttribution.IsRaisedBy(signal, "   "), Is.True);
    }

    [Test]
    public void AnyScopeIgnoresAttribution()
    {
      const string signal = "attribution.any-scope";
      RaiseBy(signal, OtherIdentifier);

      Assert.That(
        ScenarioSignalAttribution.IsSatisfied(signal, ScenarioSignalScope.Any, OwnerIdentifier), Is.True,
        "공동 목표의 판정 범위가 좁아지면 팀 단위 퀘스트의 진행 표시가 어긋납니다.");
      Assert.That(
        ScenarioSignalAttribution.IsSatisfied(signal, ScenarioSignalScope.Owner, OwnerIdentifier), Is.False);
    }

    [Test]
    public void StoreReportsAttributionPresencePerSignal()
    {
      const string signal = "attribution.record-presence";

      Assert.That(ScenarioSignalParameterStore.HasAnyRecord(signal), Is.False);
      RaiseBy(signal, OwnerIdentifier);
      Assert.That(ScenarioSignalParameterStore.HasAnyRecord(signal), Is.True);

      ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
      Assert.That(ScenarioSignalParameterStore.HasAnyRecord(signal), Is.False);
    }

    [Test]
    public void CriteriaSignalScopeDefaultsToAnyAndSurvivesClone()
    {
      var criteria = new QuestCompletionCriteria
      {
        Identifier = "step",
        Type = QuestCompletionCriteriaType.InteractionSignalReceived,
        SignalId = "attribution.clone"
      };

      Assert.That(criteria.SignalScope, Is.EqualTo(ScenarioSignalScope.Any),
        "기본값이 Owner 이면 기존 공동 목표 데이터의 동작이 바뀝니다.");

      criteria.SignalScope = ScenarioSignalScope.Owner;
      Assert.That(criteria.Clone().SignalScope, Is.EqualTo(ScenarioSignalScope.Owner));
    }
  }
}
