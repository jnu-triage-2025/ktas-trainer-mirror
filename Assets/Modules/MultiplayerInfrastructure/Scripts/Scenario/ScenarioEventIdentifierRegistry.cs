using System;
using System.Collections;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 이벤트 식별자를 시나리오 노드가 호출할 수 있는 핸들러에 매핑한다.
  /// 핸들러는 비동기 실행을 위해 코루틴을 반환할 수 있으며, null 을 반환하면 즉시 완료로 취급한다.
  /// </summary>
  public static class ScenarioEventIdentifierRegistry
  {
    public delegate IEnumerator ScenarioEventHandler();

    private static readonly Dictionary<string, ScenarioEventHandler> Handlers = new Dictionary<string, ScenarioEventHandler>(StringComparer.Ordinal);

    public static void Register(string identifier, ScenarioEventHandler handler)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        throw new ArgumentException("Identifier cannot be null or whitespace.", nameof(identifier));
      if (handler == null)
        throw new ArgumentNullException(nameof(handler));

      Handlers[identifier] = handler;
    }

    public static bool Unregister(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
      {
        return false;
      }

      return Handlers.Remove(identifier);
    }

    public static bool Unregister(string identifier, ScenarioEventHandler expectedHandler)
    {
      if (string.IsNullOrWhiteSpace(identifier) || expectedHandler == null)
        return false;
      if (!Handlers.TryGetValue(identifier, out var current) || current != expectedHandler)
        return false;
      return Handlers.Remove(identifier);
    }

    public static bool TryGetHandler(string identifier, out ScenarioEventHandler handler)
    {
      handler = null;
      if (string.IsNullOrWhiteSpace(identifier))
      {
        return false;
      }

      return Handlers.TryGetValue(identifier, out handler);
    }

    public static void Clear() => Handlers.Clear();
  }
}
