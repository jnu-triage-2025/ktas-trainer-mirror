using System;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MultiplayerInfrastructure.Problem
{
  public static class ProblemSetLoader
  {
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
      PropertyNameCaseInsensitive = true,
      ReadCommentHandling = JsonCommentHandling.Skip,
      AllowTrailingCommas = true,
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
      WriteIndented = true
    };

    public static ProblemSetDefinition LoadFromJson(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("Problem set json text is null or empty.", nameof(json));

      var parsed = JsonSerializer.Deserialize<ProblemSetDefinition>(json, SerializerOptions);
      if (parsed == null)
        throw new JsonException("Problem set payload is missing.");

      parsed.Problems ??= new System.Collections.Generic.List<ProblemDefinition>();
      return parsed;
    }
  }
}
