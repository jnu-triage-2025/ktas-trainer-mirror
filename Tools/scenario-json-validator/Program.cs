using System.Text.Json;
using MultiplayerInfrastructure.Scenario;

return ScenarioJsonValidatorProgram.Run(args);

internal static class ScenarioJsonValidatorProgram
{
  private const string DefaultSchemaRelativePath = "Assets/Modules/MultiplayerInfrastructure/Resources/Schema/scenario.schema.json";

  public static int Run(string[] args)
  {
    if (!TryParseArguments(args, out var scenarioPath, out var schemaPath, out var jsonOutput, out var error))
    {
      Console.Error.WriteLine(error);
      PrintUsage();
      return 2;
    }

    try
    {
      var schemaText = File.ReadAllText(schemaPath!);
      var scenarioText = File.ReadAllText(scenarioPath!);
      var knownValues = ReadKnownDiscriminatorValues(schemaText);
      ScenarioJsonSchemaValidator.Validate(
        scenarioText,
        schemaText,
        knownValues.NodeTypes.Contains,
        knownValues.InteractionTypes.Contains);

      if (jsonOutput)
        Console.WriteLine("{\"valid\":true}");
      else
        Console.WriteLine($"VALID: {scenarioPath}");
      return 0;
    }
    catch (ScenarioSchemaValidationException ex)
    {
      if (jsonOutput)
        Console.WriteLine(JsonSerializer.Serialize(new { valid = false, error = ex.Message }));
      else
        Console.Error.WriteLine(ex.Message);
      return 1;
    }
    catch (Exception ex)
    {
      if (jsonOutput)
        Console.WriteLine(JsonSerializer.Serialize(new { valid = false, error = ex.Message }));
      else
        Console.Error.WriteLine($"ERROR: {ex.Message}");
      return 2;
    }
  }

  private static bool TryParseArguments(
    IReadOnlyList<string> args,
    out string scenarioPath,
    out string schemaPath,
    out bool jsonOutput,
    out string error)
  {
    scenarioPath = null;
    schemaPath = null;
    jsonOutput = false;
    error = null;
    for (var index = 0; index < args.Count; index++)
    {
      switch (args[index])
      {
        case "--schema" when index + 1 < args.Count:
          schemaPath = Path.GetFullPath(args[++index]);
          break;
        case "--format" when index + 1 < args.Count:
          var format = args[++index];
          if (format == "json") jsonOutput = true;
          else if (format != "text") { error = $"Unsupported format: {format}"; return false; }
          break;
        case "--help":
        case "-h":
          error = null;
          return false;
        case var argument when !argument.StartsWith("-", StringComparison.Ordinal) && scenarioPath == null:
          scenarioPath = Path.GetFullPath(args[index]);
          break;
        default:
          error = $"Unknown or incomplete argument: {args[index]}";
          return false;
      }
    }

    if (scenarioPath == null) { error = "A scenario JSON path is required."; return false; }
    if (!File.Exists(scenarioPath)) { error = $"Scenario JSON was not found: {scenarioPath}"; return false; }
    schemaPath ??= FindDefaultSchemaPath();
    if (schemaPath == null || !File.Exists(schemaPath)) { error = "Could not find scenario.schema.json. Pass --schema PATH."; return false; }
    return true;
  }

  private static string FindDefaultSchemaPath()
  {
    for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory != null; directory = directory.Parent)
    {
      var candidate = Path.Combine(directory.FullName, DefaultSchemaRelativePath);
      if (File.Exists(candidate)) return candidate;
    }
    return null;
  }

  private static (HashSet<string> NodeTypes, HashSet<string> InteractionTypes) ReadKnownDiscriminatorValues(string schemaText)
  {
    using var schema = JsonDocument.Parse(schemaText);
    var definitions = schema.RootElement.GetProperty("$defs");
    return (
      ReadEnum(definitions.GetProperty("ScenarioNode").GetProperty("properties").GetProperty("nodeType")),
      ReadEnum(definitions.GetProperty("ScenarioActingNpcInteractionDefinition").GetProperty("properties").GetProperty("interactionType")));
  }

  private static HashSet<string> ReadEnum(JsonElement property) => property.GetProperty("enum")
    .EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String)
    .Select(value => value.GetString()!).ToHashSet(StringComparer.Ordinal);

  private static void PrintUsage() => Console.Error.WriteLine(
    "Usage: dotnet run --project Tools/scenario-json-validator -- SCENARIO.json [--schema SCHEMA.json] [--format text|json]");
}
