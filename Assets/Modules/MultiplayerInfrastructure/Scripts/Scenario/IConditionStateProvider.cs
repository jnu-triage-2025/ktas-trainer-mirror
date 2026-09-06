using System;
using System.Collections.Generic;
using System.Globalization;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>조건 절이 비교할 수 있는 값. bool, 숫자, 문자열 가운데 하나를 담는다.</summary>
  public readonly struct ConditionValue
  {
    public enum ValueKind
    {
      Bool,
      Number,
      Text
    }

    public ValueKind Kind { get; }
    public bool BoolValue { get; }
    public double NumberValue { get; }
    public string TextValue { get; }

    private ConditionValue(ValueKind kind, bool boolValue, double numberValue, string textValue)
    {
      Kind = kind;
      BoolValue = boolValue;
      NumberValue = numberValue;
      TextValue = textValue;
    }

    public static ConditionValue From(bool value) => new ConditionValue(ValueKind.Bool, value, value ? 1d : 0d, value ? "true" : "false");
    public static ConditionValue From(int value) => new ConditionValue(ValueKind.Number, value != 0, value, value.ToString(CultureInfo.InvariantCulture));
    public static ConditionValue From(float value) => new ConditionValue(ValueKind.Number, value != 0f, value, value.ToString(CultureInfo.InvariantCulture));
    public static ConditionValue From(double value) => new ConditionValue(ValueKind.Number, value != 0d, value, value.ToString(CultureInfo.InvariantCulture));
    public static ConditionValue From(string value) => new ConditionValue(ValueKind.Text, !string.IsNullOrWhiteSpace(value), 0d, value ?? string.Empty);

    /// <summary>
    /// 데이터에 적힌 비교 값과 이 값을 비교한다. 비교 값이 비어 있으면 true 와 비교한다.
    /// 숫자 값은 숫자로, bool 값은 true/false 로, 그 밖에는 문자열(대소문자 무시)로 비교한다.
    /// </summary>
    public bool Satisfies(ScenarioConditionCompare compare, string expected)
    {
      switch (Kind)
      {
        case ValueKind.Bool:
        {
          bool rhs = string.IsNullOrWhiteSpace(expected) || ParseBool(expected);
          return CompareOrdered(BoolValue ? 1d : 0d, rhs ? 1d : 0d, compare);
        }
        case ValueKind.Number:
        {
          if (string.IsNullOrWhiteSpace(expected))
            return CompareOrdered(NumberValue, 1d, compare);
          if (double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out double rhs))
            return CompareOrdered(NumberValue, rhs, compare);
          if (bool.TryParse(expected, out bool rhsBool))
            return CompareOrdered(NumberValue, rhsBool ? 1d : 0d, compare);
          return false;
        }
        default:
        {
          string lhs = TextValue ?? string.Empty;
          if (string.IsNullOrWhiteSpace(expected))
            return CompareOrdered(BoolValue ? 1d : 0d, 1d, compare);
          int order = string.Compare(lhs.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
          return CompareOrdered(order, 0, compare);
        }
      }
    }

    private static bool ParseBool(string value)
    {
      if (bool.TryParse(value, out bool parsed))
        return parsed;
      if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
        return number != 0d;
      return string.Equals(value?.Trim(), "yes", StringComparison.OrdinalIgnoreCase)
             || string.Equals(value?.Trim(), "on", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CompareOrdered(double lhs, double rhs, ScenarioConditionCompare compare)
    {
      switch (compare)
      {
        case ScenarioConditionCompare.NotEqual: return Math.Abs(lhs - rhs) > double.Epsilon;
        case ScenarioConditionCompare.LessThan: return lhs < rhs;
        case ScenarioConditionCompare.LessThanOrEqual: return lhs <= rhs;
        case ScenarioConditionCompare.GreaterThan: return lhs > rhs;
        case ScenarioConditionCompare.GreaterThanOrEqual: return lhs >= rhs;
        default: return Math.Abs(lhs - rhs) <= double.Epsilon;
      }
    }

    public override string ToString() => TextValue;
  }

  /// <summary>
  /// 조건 절(<see cref="ScenarioConditionType.PlayerState"/>, <see cref="ScenarioConditionType.EntityState"/>)이
  /// 참조할 수 있도록 이름 붙인 값을 노출하는 규약. 플레이어와 엔티티 컴포넌트가 구현한다.
  ///
  /// <para>
  /// 키는 소문자와 밑줄로 쓰고, 하위 구분은 조건 절의 qualifier 로 받는다. 값은 서버 권위 상태
  /// (SyncVar, 복제된 레지스트리 등)만 바탕으로 해야 모든 피어에서 같은 판정이 나온다.
  /// 값이 바뀌면 <see cref="InteractableEntity.InteractionRegistry.RequestHintRefresh"/> 를 호출해
  /// 힌트 목록이 다시 계산되게 한다.
  /// </para>
  /// </summary>
  public interface IConditionStateProvider
  {
    /// <summary>키(와 선택적 하위 구분자)에 해당하는 값을 돌려준다. 모르는 키면 false.</summary>
    bool TryGetConditionValue(string key, string qualifier, out ConditionValue value);

    /// <summary>이 제공자가 아는 키 목록. 에디터 진단과 인스펙터가 목록을 만드는 데 쓴다.</summary>
    IEnumerable<string> ConditionKeys { get; }
  }
}
