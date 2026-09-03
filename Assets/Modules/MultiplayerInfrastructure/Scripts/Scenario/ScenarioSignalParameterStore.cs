using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MultiplayerInfrastructure.Logging;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 신호의 마지막 JSON 파라미터를 신호·발신 플레이어별로 보관한다.
  /// 서버가 권위 원본을 기록하고, 클라이언트는 네트워크 중계기로 받은 미러만 갱신한다.
  /// </summary>
  public static class ScenarioSignalParameterStore
  {
    public const string ServerPlayerIdentifier = "server";
    public const int MaxStoredEntries = 512;
    public const int MaxStoredEntriesPerPlayer = 128;
    private const char KeySeparator = '\u001f';
    private static readonly Dictionary<string, ScenarioSignalParameter> Values = new(StringComparer.Ordinal);
    // 신호별 귀속 기록 수. (signal, player) 키 전체를 순회하지 않고 귀속 유무를 판정하기 위해 유지한다.
    private static readonly Dictionary<string, int> RecordCountBySignal = new(StringComparer.Ordinal);
    private static long _latestSequence;

    public static event Action<ScenarioSignalParameter> OnValueRecorded;
    public static event Action OnFlushed;

    public static bool TryValidateJson(string parameterJson, out string error)
    {
      error = string.Empty;
      if (parameterJson == null)
        return true;

      try
      {
        using JsonDocument _ = JsonDocument.Parse(parameterJson);
        return true;
      }
      catch (JsonException exception)
      {
        error = exception.Message;
        return false;
      }
    }

    /// <summary>권위 서버/오프라인 실행에서 새 값을 기록한다.</summary>
    internal static ScenarioSignalParameter RecordAuthoritative(
      string normalizedSignalIdentifier,
      string playerIdentifier,
      string playerDisplayName,
      string parameterJson)
    {
      long sequence = ++_latestSequence;
      var value = new ScenarioSignalParameter(
        normalizedSignalIdentifier,
        NormalizePlayerIdentifier(playerIdentifier),
        string.IsNullOrWhiteSpace(playerDisplayName) ? NormalizePlayerIdentifier(playerIdentifier) : playerDisplayName.Trim(),
        parameterJson,
        DateTime.UtcNow.Ticks,
        sequence);
      Apply(value);
      return value;
    }

    /// <summary>서버에서 전송한 확정 값을 클라이언트 미러에 반영한다.</summary>
    internal static void ApplyMirror(ScenarioSignalParameter value)
    {
      if (string.IsNullOrWhiteSpace(value.SignalIdentifier) || string.IsNullOrWhiteSpace(value.PlayerIdentifier))
        return;
      _latestSequence = Math.Max(_latestSequence, value.Sequence);
      Apply(value);
    }

    /// <summary>서버가 보낸 늦은 접속자 스냅샷으로 로컬 읽기 미러를 교체한다.</summary>
    internal static void ApplySnapshotFromServer(IEnumerable<ScenarioSignalParameter> values)
    {
      FlushLocal();
      if (values == null)
        return;
      foreach (ScenarioSignalParameter value in values)
        ApplyMirror(value);
    }

    public static bool TryGetLatest(string signalIdentifier, out ScenarioSignalParameter value)
    {
      string normalized = ScenarioInteractionSignals.Normalize(signalIdentifier);
      value = Values.Values
        .Where(item => string.Equals(item.SignalIdentifier, normalized, StringComparison.Ordinal))
        .OrderByDescending(item => item.Sequence)
        .FirstOrDefault();
      return !string.IsNullOrWhiteSpace(value.SignalIdentifier);
    }

    public static bool TryGetForPlayer(string signalIdentifier, string playerIdentifier, out ScenarioSignalParameter value)
      => Values.TryGetValue(BuildKey(ScenarioInteractionSignals.Normalize(signalIdentifier), NormalizePlayerIdentifier(playerIdentifier)), out value);

    /// <summary>
    /// 이 신호에 대해 발신자 귀속 기록이 하나라도 남아 있는지 확인한다.
    /// 저장소 한도 초과나 미러 유실로 귀속이 없는 신호를 구별해, 귀속 기반 판정이
    /// 진행을 막지 않고 전역 신호 결과로 물러설 수 있게 한다.
    /// </summary>
    public static bool HasAnyRecord(string signalIdentifier)
    {
      string normalized = ScenarioInteractionSignals.Normalize(signalIdentifier);
      return !string.IsNullOrWhiteSpace(normalized)
        && RecordCountBySignal.TryGetValue(normalized, out int count)
        && count > 0;
    }

    public static IReadOnlyList<ScenarioSignalParameter> GetAll(string signalIdentifier = null)
    {
      string normalized = string.IsNullOrWhiteSpace(signalIdentifier) ? null : ScenarioInteractionSignals.Normalize(signalIdentifier);
      return Values.Values
        .Where(item => normalized == null || string.Equals(item.SignalIdentifier, normalized, StringComparison.Ordinal))
        .OrderByDescending(item => item.Sequence)
        .ToArray();
    }

    /// <summary>새로운 (signal, player) 키를 저장할 서버 수용 여력이 있는지 확인한다.</summary>
    internal static bool CanRecord(string normalizedSignalIdentifier, string playerIdentifier, out string error)
    {
      error = string.Empty;
      string normalizedPlayer = NormalizePlayerIdentifier(playerIdentifier);
      if (Values.ContainsKey(BuildKey(normalizedSignalIdentifier, normalizedPlayer)))
        return true;

      if (Values.Count >= MaxStoredEntries)
      {
        error = $"시그널 파라미터 저장소 한도({MaxStoredEntries})에 도달했습니다.";
        return false;
      }

      int playerCount = Values.Values.Count(value => string.Equals(value.PlayerIdentifier, normalizedPlayer, StringComparison.Ordinal));
      if (playerCount >= MaxStoredEntriesPerPlayer)
      {
        error = $"플레이어별 시그널 파라미터 한도({MaxStoredEntriesPerPlayer})에 도달했습니다.";
        return false;
      }
      return true;
    }

    internal static void FlushLocal()
    {
      if (Values.Count == 0)
        return;
      Values.Clear();
      RecordCountBySignal.Clear();
      OnFlushed?.Invoke();
      GameLogService.WriteSignal("Scenario signal parameters flushed.", "scenario-signal-parameters");
    }

    private static void Apply(ScenarioSignalParameter value)
    {
      string key = BuildKey(value.SignalIdentifier, value.PlayerIdentifier);
      bool isNewKey = !Values.TryGetValue(key, out var current);
      if (!isNewKey && current.Sequence >= value.Sequence)
        return;
      Values[key] = value;
      if (isNewKey)
      {
        RecordCountBySignal.TryGetValue(value.SignalIdentifier, out int recordCount);
        RecordCountBySignal[value.SignalIdentifier] = recordCount + 1;
      }
      OnValueRecorded?.Invoke(value);
      GameLogService.WriteSignal(
        $"Signal parameter recorded: signal={FormatForLog(value.SignalIdentifier)}, "
        + $"player={FormatForLog(value.PlayerDisplayName)} ({FormatForLog(value.PlayerIdentifier)}), "
        + $"parameter={FormatForLog(value.ParameterJson)}",
        FormatForLog(value.SignalIdentifier));
    }

    /// <summary>한 줄 세션 로그에 안전하게 넣을 수 있도록 JSON 원문을 이스케이프한다.</summary>
    internal static string FormatForLog(string value)
      => value == null ? "(none)" : JsonSerializer.Serialize(value);

    private static string NormalizePlayerIdentifier(string playerIdentifier)
      => string.IsNullOrWhiteSpace(playerIdentifier) ? ServerPlayerIdentifier : playerIdentifier.Trim();

    private static string BuildKey(string signalIdentifier, string playerIdentifier)
      => signalIdentifier + KeySeparator + playerIdentifier;
  }

  /// <summary>신호 식별자·발신 플레이어별 마지막 JSON 파라미터의 읽기 모델.</summary>
  public readonly struct ScenarioSignalParameter
  {
    public string SignalIdentifier { get; }
    public string PlayerIdentifier { get; }
    public string PlayerDisplayName { get; }
    public string ParameterJson { get; }
    public long OccurredAtUtcTicks { get; }
    public long Sequence { get; }
    public bool HasParameter => ParameterJson != null;
    public string DisplayParameter => HasParameter ? ParameterJson : "(none)";

    public ScenarioSignalParameter(string signalIdentifier, string playerIdentifier, string playerDisplayName,
      string parameterJson, long occurredAtUtcTicks, long sequence)
    {
      SignalIdentifier = signalIdentifier ?? string.Empty;
      PlayerIdentifier = playerIdentifier ?? string.Empty;
      PlayerDisplayName = playerDisplayName ?? string.Empty;
      ParameterJson = parameterJson;
      OccurredAtUtcTicks = occurredAtUtcTicks;
      Sequence = sequence;
    }
  }
}
