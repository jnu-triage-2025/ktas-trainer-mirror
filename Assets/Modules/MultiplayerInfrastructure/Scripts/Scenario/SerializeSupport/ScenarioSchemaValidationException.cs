using System;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 예외: 시나리오 JSON이 스키마 검증에 실패했을 때 사용합니다.
  /// </summary>
  public sealed class ScenarioSchemaValidationException : Exception
  {
    public ScenarioSchemaValidationException(string message)
        : base(message) { }

    public ScenarioSchemaValidationException(string message, Exception innerException)
        : base(message, innerException) { }
  }
}
