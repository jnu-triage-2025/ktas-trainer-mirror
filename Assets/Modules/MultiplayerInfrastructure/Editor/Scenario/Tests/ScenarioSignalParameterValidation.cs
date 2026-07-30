#if UNITY_EDITOR
using System;
using System.Reflection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>배치 실행에서 JSON 파라미터 신호의 핵심 서버 저장 계약을 확인한다.</summary>
  public static class ScenarioSignalParameterValidation
  {
    private const string Signal = "validation.signal-parameter";

    [MenuItem("Tools/Multiplayer Infrastructure/Validate Scenario Signal Parameters")]
    public static void Run()
    {
      ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
      Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize(Signal));
      try
      {
        Require(ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(
          ScenarioInteractionSignals.Normalize(Signal), "{\"value\":1}", "player-a", "Alice"),
          "유효 JSON 파라미터 신호가 수락되지 않았습니다.");
        Require(ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(
          ScenarioInteractionSignals.Normalize(Signal), "[2]", "player-b", "Bob"),
          "두 번째 유효 JSON 파라미터 신호가 수락되지 않았습니다.");
        Require(ScenarioSignalParameterStore.TryGetLatest(Signal, out var latest)
          && latest.PlayerIdentifier == "player-b" && latest.ParameterJson == "[2]",
          "전체 최신 파라미터 값이 올바르지 않습니다.");
        Require(ScenarioSignalParameterStore.TryGetForPlayer(Signal, "player-a", out var player)
          && player.ParameterJson == "{\"value\":1}",
          "플레이어별 최신 파라미터 값이 올바르지 않습니다.");
        Require(!ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(
          ScenarioInteractionSignals.Normalize("validation.invalid"), "{broken", "player-a", "Alice"),
          "유효하지 않은 JSON 파라미터가 거부되지 않았습니다.");
        Require(!ScenarioSignalParameterStore.TryGetLatest("validation.invalid", out _),
          "유효하지 않은 JSON 파라미터가 저장되었습니다.");

        using (ScenarioSignalPlayerContext.Push("player-c", "Charlie"))
          ScenarioInteractionSignals.Raise("validation.context", "\"host action\"");
        Require(ScenarioSignalParameterStore.TryGetForPlayer("validation.context", "player-c", out var contextual)
          && contextual.PlayerDisplayName == "Charlie",
          "서버 콜백 플레이어 컨텍스트가 신호 발신자로 보존되지 않았습니다.");

        IDisposable outerContext = ScenarioSignalPlayerContext.Push("player-outer", "Outer");
        IDisposable innerContext = ScenarioSignalPlayerContext.Push("player-inner", "Inner");
        outerContext.Dispose();
        ScenarioInteractionSignals.Raise("validation.context-out-of-order", "true");
        Require(ScenarioSignalParameterStore.TryGetForPlayer("validation.context-out-of-order", "player-inner", out _),
          "순서 밖 컨텍스트 해제가 내부 플레이어 컨텍스트를 제거했습니다.");
        innerContext.Dispose();
        ScenarioInteractionSignals.Raise("validation.context-after-dispose", "true");
        Require(ScenarioSignalParameterStore.TryGetForPlayer("validation.context-after-dispose",
          ScenarioSignalParameterStore.ServerPlayerIdentifier, out _),
          "모든 컨텍스트 해제 후 플레이어 컨텍스트가 남아 있습니다.");

        ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
        for (int i = 0; i < 30; i++)
        {
          Require(ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(
            ScenarioInteractionSignals.Normalize($"validation.rate.{i}"), "0", "rate-player", "Rate"),
            "플레이어별 갱신 한도 안의 신호가 거부되었습니다.");
        }
        Require(!ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(
          ScenarioInteractionSignals.Normalize("validation.rate.overflow"), "0", "rate-player", "Rate"),
          "플레이어별 갱신 한도를 넘는 신호가 수락되었습니다.");
        Require(!ScenarioInteractionSignals.IsRaised("validation.rate.overflow"),
          "갱신 한도를 넘은 실제 시나리오 신호가 발생했습니다.");

        MethodInfo tokenize = typeof(ChatService).GetMethod("TokenizeCommandLine",
          BindingFlags.Static | BindingFlags.NonPublic);
        string[] tokens = tokenize?.Invoke(null, new object[] { "signal raise validation.command {\"memo\":\"a  b|c&d\"}" }) as string[];
        Require(tokens != null && tokens.Length == 4 && tokens[3] == "{\"memo\":\"a  b|c&d\"}",
          "명령 JSON 문자열 내부의 공백 또는 파이프 문자가 보존되지 않았습니다.");

        ScenarioInteractionSignals.Raise("validation.stale", "true");
        MethodInfo applySnapshot = typeof(ScenarioSignalParameterStore).GetMethod("ApplySnapshotFromServer",
          BindingFlags.Static | BindingFlags.NonPublic);
        Require(applySnapshot != null, "늦은 접속자 스냅샷 적용 API를 찾을 수 없습니다.");
        applySnapshot.Invoke(null, new object[] { new[]
        {
          new ScenarioSignalParameter(ScenarioInteractionSignals.Normalize("validation.snapshot"),
            "player-d", "Dana", "{\"lateJoin\":true}", DateTime.UtcNow.Ticks, 100L),
        } });
        Require(!ScenarioSignalParameterStore.TryGetLatest("validation.stale", out _)
          && ScenarioSignalParameterStore.TryGetForPlayer("validation.snapshot", "player-d", out var snapshot)
          && snapshot.ParameterJson == "{\"lateJoin\":true}",
          "늦은 접속자 스냅샷이 기존 미러를 비우고 서버 값을 적용하지 못했습니다.");

        ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
        for (int i = 0; i < ScenarioSignalParameterStore.MaxStoredEntries; i++)
        {
          Require(ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(
            ScenarioInteractionSignals.Normalize($"validation.capacity.{i}"), "0", $"capacity-player-{i}", "Capacity"),
            "저장소 한도 안의 신호가 거부되었습니다.");
        }
        Require(!ScenarioNetworkRelay.RaiseAuthoritativeForPlayer(
          ScenarioInteractionSignals.Normalize("validation.capacity.overflow"), "0", "capacity-player-overflow", "Capacity"),
          "저장소 상한을 넘는 새 신호가 수락되었습니다.");
        Require(ScenarioInteractionSignals.IsRaised("validation.capacity.overflow"),
          "저장소 상한 때문에 실제 시나리오 신호까지 누락되었습니다.");

        ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
        Require(!ScenarioSignalParameterStore.TryGetLatest(Signal, out _),
          "flush 후 파라미터 값이 남아 있습니다.");
        Debug.Log("[ScenarioSignalParameterValidation] Passed.");
      }
      finally
      {
        ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize(Signal));
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("validation.invalid"));
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("validation.context"));
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("validation.context-out-of-order"));
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("validation.context-after-dispose"));
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("validation.stale"));
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("validation.snapshot"));
        for (int i = 0; i < 30; i++)
          Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize($"validation.rate.{i}"));
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("validation.rate.overflow"));
        for (int i = 0; i < ScenarioSignalParameterStore.MaxStoredEntries; i++)
          Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize($"validation.capacity.{i}"));
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize("validation.capacity.overflow"));
      }
    }

    private static void Require(bool condition, string message)
    {
      if (!condition)
        throw new InvalidOperationException(message);
    }
  }
}
#endif
