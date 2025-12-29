using System;
using System.Collections;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// Maps scenario event identifiers to handlers that can be invoked by scenario nodes.
  /// Handlers may return a coroutine to allow asynchronous execution; returning null is treated as an immediate completion.
  /// </summary>
  public static class ScenarioEventIdentifierRegistry
  {
    public delegate IEnumerator ScenarioEventHandler();

    private static readonly Dictionary<string, ScenarioEventHandler> Handlers = new Dictionary<string, ScenarioEventHandler>(StringComparer.Ordinal);

    public static void Register(string identifier, ScenarioEventHandler handler)
    {
      if (string.IsNullOrWhiteSpace(identifier)) throw new ArgumentException("Identifier cannot be null or whitespace.", nameof(identifier));
      if (handler == null) throw new ArgumentNullException(nameof(handler));

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
