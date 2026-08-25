using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MultiplayerInfrastructure.Scenario
{
  internal static class StrictJsonPropertyValidator
  {
    public static void RejectDuplicateProperties(string json)
    {
      var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
      var objectProperties = new Stack<HashSet<string>>();
      while (reader.Read())
      {
        switch (reader.TokenType)
        {
          case JsonTokenType.StartObject:
            objectProperties.Push(new HashSet<string>(StringComparer.Ordinal));
            break;
          case JsonTokenType.EndObject:
            objectProperties.Pop();
            break;
          case JsonTokenType.PropertyName:
            var name = reader.GetString();
            if (!objectProperties.Peek().Add(name))
              throw new JsonException($"Duplicate JSON property '{name}'.");
            break;
        }
      }
    }
  }
}
