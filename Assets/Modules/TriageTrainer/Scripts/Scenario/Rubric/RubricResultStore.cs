using System;
using System.Collections.Generic;
using System.Text;

namespace TriageTrainer.Scenario.Rubric
{
  /// <summary>
  /// 세션 단위 루브릭 결과 저장소(메모리). 항목×대상(플레이어/팀) 키로 결과를 누적한다.
  /// 직렬화/내보내기는 외부(파일/화면)에서 <see cref="ExportCsv"/> / <see cref="Snapshot"/> 로 수행한다.
  /// </summary>
  public sealed class RubricResultStore
  {
    private const string TeamKey = "__team__";

    private readonly Dictionary<string, RubricResult> _results = new();

    public string SessionId { get; }

    public RubricResultStore(string sessionId)
    {
      SessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId;
    }

    private static string MakeKey(string itemId, string playerId)
        => $"{itemId}\u241F{playerId ?? TeamKey}";

    /// <summary>
    /// 항목 상태를 기록/갱신한다. Performed 로 이미 확정된 항목은 NotPerformed 로 되돌리지 않는다
    /// (수행이 우선). Pending → 어떤 상태로든 갱신 가능.
    /// </summary>
    public RubricResult Record(string itemId, string playerId, RubricStatus status, string note = null)
    {
      if (string.IsNullOrWhiteSpace(itemId))
      {
        return null;
      }

      var key = MakeKey(itemId, playerId);
      if (!_results.TryGetValue(key, out var result))
      {
        result = new RubricResult
        {
          SessionId = SessionId,
          PlayerId = playerId,
          ItemId = itemId,
          Status = RubricStatus.Pending
        };
        _results[key] = result;
      }

      // 수행 우선: 이미 Performed 인 항목을 타임아웃 등으로 NotPerformed 로 덮어쓰지 않는다.
      if (result.Status == RubricStatus.Performed && status == RubricStatus.NotPerformed)
      {
        return result;
      }

      result.Status = status;
      result.Note = note;
      result.UpdatedAtUtc = DateTime.UtcNow.ToString("o");
      return result;
    }

    /// <summary>사정 퀴즈 오답/재응시 횟수를 누적한다.</summary>
    public void IncrementRetry(string itemId, string playerId)
    {
      if (string.IsNullOrWhiteSpace(itemId))
      {
        return;
      }

      var key = MakeKey(itemId, playerId);
      if (!_results.TryGetValue(key, out var result))
      {
        result = new RubricResult
        {
          SessionId = SessionId,
          PlayerId = playerId,
          ItemId = itemId,
          Status = RubricStatus.Pending
        };
        _results[key] = result;
      }

      result.Retries++;
      result.UpdatedAtUtc = DateTime.UtcNow.ToString("o");
    }

    public bool TryGet(string itemId, string playerId, out RubricResult result)
        => _results.TryGetValue(MakeKey(itemId, playerId), out result);

    public IReadOnlyCollection<RubricResult> Snapshot() => _results.Values;

    public void Clear() => _results.Clear();

    /// <summary>
    /// 디브리핑용 CSV 내보내기. 컬럼: sessionId, playerId, itemId, status, retries, updatedAtUtc, note.
    /// </summary>
    public string ExportCsv()
    {
      var sb = new StringBuilder(256);
      sb.AppendLine("sessionId,playerId,itemId,status,retries,updatedAtUtc,note");
      foreach (var r in _results.Values)
      {
        sb.Append(Escape(r.SessionId)).Append(',')
          .Append(Escape(r.PlayerId ?? TeamKey)).Append(',')
          .Append(Escape(r.ItemId)).Append(',')
          .Append(r.Status).Append(',')
          .Append(r.Retries).Append(',')
          .Append(Escape(r.UpdatedAtUtc)).Append(',')
          .Append(Escape(r.Note))
          .AppendLine();
      }

      return sb.ToString();
    }

    private static string Escape(string value)
    {
      if (string.IsNullOrEmpty(value))
      {
        return string.Empty;
      }

      if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
      {
        return value;
      }

      return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
  }
}
