using System;
using System.Text.Json.Serialization;
using FishNet;

namespace MultiplayerInfrastructure.Scenario
{
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum ScenarioTimeUnit
  {
    Tick,
    Milliseconds,
    Seconds
  }

  /// <summary>단위와 값을 함께 보존하는 시나리오 공통 시간 값.</summary>
  [Serializable]
  public struct ScenarioTimeValue
  {
    private const double FallbackTicksPerSecond = 30d;

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("unit")]
    public ScenarioTimeUnit Unit { get; set; }

    public ScenarioTimeValue(double value, ScenarioTimeUnit unit)
    {
      Value = value;
      Unit = unit;
    }

    public double ToSeconds()
    {
      if (double.IsNaN(Value) || double.IsInfinity(Value) || Value <= 0d)
        return 0d;

      return Unit switch
      {
        ScenarioTimeUnit.Tick => Value * (InstanceFinder.TimeManager?.TickDelta ?? 1d / FallbackTicksPerSecond),
        ScenarioTimeUnit.Milliseconds => Value / 1000d,
        _ => Value
      };
    }

    public uint ToTicks()
    {
      if (double.IsNaN(Value) || double.IsInfinity(Value) || Value <= 0d)
        return 0u;

      if (Unit == ScenarioTimeUnit.Tick)
        return (uint)Math.Max(1d, Math.Round(Value));

      var timeManager = InstanceFinder.TimeManager;
      double seconds = ToSeconds();
      return timeManager != null
        ? Math.Max(1u, timeManager.TimeToTicks(seconds))
        : (uint)Math.Max(1d, Math.Round(seconds * FallbackTicksPerSecond));
    }

    public static ScenarioTimeValue Seconds(double value) => new(value, ScenarioTimeUnit.Seconds);
  }
}
