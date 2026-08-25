namespace TriageTrainer.SceneBootstrapper
{
  /// <summary>
  /// 네트워크 세션 실패의 상태 코드와 상세 정보를 나타냅니다.
  /// 상태 코드 범위: 1xxx=정당한 사유, 2xxx=예기치 않은 오류, 9xxx=미분류.
  /// </summary>
  public sealed class NetworkSessionFailure
  {
    public enum StatusCode
    {
      /// <summary>서버가 정상적으로 세션을 종료했습니다 (정당한 사유).</summary>
      ServerShutdown = 1001,

      /// <summary>서버가 연결을 거부했습니다.</summary>
      ConnectionRefused = 2001,

      /// <summary>서버에 연결 시도 중 타임아웃이 발생했습니다.</summary>
      ConnectionTimeout = 2002,

      /// <summary>연결이 수립된 후 예기치 않게 끊겼습니다.</summary>
      ConnectionLost = 2003,

      /// <summary>분류할 수 없는 연결 오류입니다.</summary>
      Unknown = 9999,
    }

    /// <summary>실패 상태 코드입니다.</summary>
    public StatusCode Code { get; }

    /// <summary>기계 판독 가능한 사유 식별자입니다.</summary>
    public string Reason { get; }

    /// <summary>사람이 읽을 수 있는 상세 설명입니다.</summary>
    public string Detail { get; }

    /// <summary>정당한 사유로 인한 실패인지 여부입니다. 상태 코드 1xxx 범위가 정당한 사유에 해당합니다.</summary>
    public bool IsLegitimate => (int)Code >= 1000 && (int)Code < 2000;

    private NetworkSessionFailure(StatusCode code, string reason, string detail)
    {
      Code = code;
      Reason = reason;
      Detail = string.IsNullOrWhiteSpace(detail) ? "알 수 없는 연결 오류" : detail.Trim();
    }

    /// <summary>서버 정상 종료에 의한 실패를 생성합니다. 정당한 사유로 분류됩니다.</summary>
    public static NetworkSessionFailure ServerShutdownFailure(string detail)
    {
      return new NetworkSessionFailure(StatusCode.ServerShutdown, "ServerShutdown", detail);
    }

    /// <summary>연결 거부 실패를 생성합니다.</summary>
    public static NetworkSessionFailure ConnectionRefusedFailure(string detail)
    {
      return new NetworkSessionFailure(StatusCode.ConnectionRefused, "ConnectionRefused", detail);
    }

    /// <summary>연결 타임아웃 실패를 생성합니다.</summary>
    public static NetworkSessionFailure ConnectionTimeoutFailure(string detail)
    {
      return new NetworkSessionFailure(StatusCode.ConnectionTimeout, "ConnectionTimeout", detail);
    }

    /// <summary>연결 손실 실패를 생성합니다.</summary>
    public static NetworkSessionFailure ConnectionLostFailure(string detail)
    {
      return new NetworkSessionFailure(StatusCode.ConnectionLost, "ConnectionLost", detail);
    }

    /// <summary>분류할 수 없는 일반 연결 오류를 생성합니다.</summary>
    public static NetworkSessionFailure ConnectionError(string detail)
    {
      return new NetworkSessionFailure(StatusCode.Unknown, "ConnectionError", detail);
    }

    public override string ToString()
    {
      return $"code={(int)Code}({Code}); reason={Reason}; detail={Detail}; legitimate={IsLegitimate}";
    }
  }
}
