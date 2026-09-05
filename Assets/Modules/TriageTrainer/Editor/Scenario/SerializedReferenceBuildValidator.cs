using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TriageTrainer.Editor
{
  /// <summary>
  /// Validates authored direct Unity object fields before Unity can normalize invalid references.
  /// Does not load scenes or prefabs, invoke their lifecycle methods, or modify their contents.
  /// </summary>
  public sealed class SerializedReferenceBuildValidator : BuildPlayerProcessor
  {
    public override int callbackOrder => -10000;

    public override void PrepareForBuild(BuildPlayerContext context)
      => ValidateBuildScenes(context.BuildPlayerOptions.scenes);

    [MenuItem("Tools/Triage Trainer/Validate Serialized Reference Types")]
    public static void ValidateProject()
    {
      ValidateBuildScenes(EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path));
    }

    private static void ValidateBuildScenes(IEnumerable<string> scenes)
    {
      var paths = CollectBuildAssets(scenes,
        AssetDatabase.GetAllAssetPaths().Where(path => !AssetDatabase.IsValidFolder(path)),
        PlayerSettings.GetPreloadedAssets().Where(asset => asset != null).Select(AssetDatabase.GetAssetPath),
        roots => AssetDatabase.GetDependencies(roots, true));
      var validator = new ReferenceYamlValidator(
        File.ReadAllText, AssetDatabase.GUIDToAssetPath,
        CreateScriptTypeResolver(),
        ResolveImportedObjectType);
      var errors = validator.Validate(paths);
      if (errors.Count > 0)
        throw new BuildFailedException("Serialized reference validation failed:\n" + string.Join("\n", errors));
      Debug.Log($"[SerializedReferenceValidation] Validated direct object fields and prefab overrides in {paths.Length} scene/prefab assets.");
    }

    public static string[] CollectBuildAssets(IEnumerable<string> scenes, IEnumerable<string> assetPaths,
      IEnumerable<string> preloadedAssets, Func<string[], string[]> dependencies)
    {
      bool IsRuntimePath(string path) => !string.IsNullOrEmpty(path)
        && !path.Contains("/Editor/") && !path.Contains("/StreamingAssets/");
      var roots = (scenes ?? Array.Empty<string>())
        .Concat(assetPaths.Where(path => IsRuntimePath(path) && path.Contains("/Resources/")))
        .Concat(preloadedAssets).Where(IsRuntimePath).Distinct().ToArray();
      return (roots.Length == 0 ? roots : dependencies(roots).Concat(roots))
        .Where(IsRuntimePath)
        .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
          || path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        .Distinct().OrderBy(path => path, StringComparer.Ordinal).ToArray();
    }

    private static Func<string, string, Type> CreateScriptTypeResolver()
    {
      Dictionary<string, Type> builtins = null;
      return (guid, id) =>
      {
        if (guid == "0000000000000000e000000000000000")
        {
          if (builtins == null)
          {
            builtins = new Dictionary<string, Type>();
            foreach (var script in MonoImporter.GetAllRuntimeMonoScripts())
              if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out string scriptGuid, out long scriptId)
                && scriptGuid == guid)
                builtins[scriptId.ToString(System.Globalization.CultureInfo.InvariantCulture)] = script.GetClass();
          }
          return builtins.TryGetValue(id, out var type) ? type : null;
        }
        return AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid))?.GetClass();
      };
    }

    private static Type ResolveImportedObjectType(string path, string id)
    {
      foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        if (asset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long localId)
          && localId.ToString(System.Globalization.CultureInfo.InvariantCulture) == id)
          return asset.GetType();
      return null;
    }
  }

  // File and script resolution are injected so regressions can be tested without importing corrupt assets.
  public sealed class ReferenceYamlValidator
  {
    private static readonly Regex Documents = new(
      @"^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\z)", RegexOptions.Multiline | RegexOptions.Singleline);
    private static readonly Regex References = new(
      @"^  (\w+): (\{fileID: [^\r\n]+\})", RegexOptions.Multiline);
    private static readonly Regex Overrides = new(
      @"^    - target: (\{[^\r\n]+\})\r?\n      propertyPath: (\w+)\r?\n      value:[^\r\n]*\r?\n      objectReference: (\{[^\r\n]+\})", RegexOptions.Multiline);
    private readonly Func<string, string> read;
    private readonly Func<string, string> assetPath;
    private readonly Func<string, string, Type> scriptType;
    private readonly Func<string, string, Type> importedType;
    private readonly Dictionary<string, Type> importedTypes = new();
    private readonly Dictionary<string, Dictionary<string, Document>> files = new();
    private readonly Dictionary<string, Type> scripts = new();

    private sealed class Document
    {
      public string Path, Id, ClassId, Body;
    }

    public ReferenceYamlValidator(Func<string, string> read, Func<string, string> assetPath,
      Func<string, string, Type> scriptType, Func<string, string, Type> importedType = null)
    {
      this.read = read;
      this.assetPath = assetPath;
      this.scriptType = scriptType;
      this.importedType = importedType;
    }

    public List<string> Validate(IEnumerable<string> paths)
    {
      files.Clear();
      scripts.Clear();
      importedTypes.Clear();
      var errors = new List<string>();
      foreach (var path in paths)
      {
        try
        {
          foreach (var doc in ReadDocuments(path).Values)
          {
            if (doc.ClassId == "114")
            {
              var type = GetObjectType(doc, new HashSet<string>());
              if (!IsOwnedBehaviour(type))
                continue;
              foreach (Match reference in References.Matches(doc.Body))
                ValidateField(doc, type, reference.Groups[1].Value, reference.Groups[2].Value, errors);
            }
            if (doc.ClassId == "1001")
            {
              foreach (Match modification in Overrides.Matches(doc.Body))
              {
                var target = Resolve(doc.Path, modification.Groups[1].Value);
                var type = GetObjectType(target, new HashSet<string>());
                if (IsOwnedBehaviour(type))
                  ValidateField(doc, type, modification.Groups[2].Value, modification.Groups[3].Value, errors);
              }
            }
          }
        }
        catch (Exception ex)
        {
          errors.Add($"{path}: validation could not complete: {ex.Message}");
        }
      }
      return errors;
    }

    private void ValidateField(Document owner, Type ownerType, string fieldName, string reference, List<string> errors)
    {
      var field = FindField(ownerType, fieldName);
      if (field == null || field.IsStatic || field.IsNotSerialized
        || (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true))
        || !typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
        return;
      // Unassigned optional fields are valid; nonzero missing references are not.
      if (FileId(reference) == "0")
        return;
      var target = Resolve(owner.Path, reference);
      var actualType = GetObjectType(target, new HashSet<string>());
      if (actualType != null && field.FieldType.IsAssignableFrom(actualType))
        return;
      var name = Regex.Match(owner.Body, @"^  m_Name: (.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
      string gameObjectReference = Regex.Match(owner.Body, @"m_GameObject: (\{[^\r\n]+\})").Groups[1].Value;
      var gameObject = Resolve(owner.Path, gameObjectReference);
      if (gameObject != null)
        name = Regex.Match(gameObject.Body, @"^  m_Name: (.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
      errors.Add($"{owner.Path} [object {owner.Id}{(name.Length == 0 ? "" : " '" + name + "'")}, {ownerType.Name}.{fieldName}]: "
        + $"expected {field.FieldType.Name}, got {actualType?.Name ?? "missing/unresolved reference"} ({reference}).");
    }

    private static FieldInfo FindField(Type type, string name)
    {
      for (; type != null; type = type.BaseType)
      {
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        if (field != null)
          return field;
      }
      return null;
    }

    private static bool IsOwnedBehaviour(Type type)
      => type != null && typeof(MonoBehaviour).IsAssignableFrom(type)
        && (type.Namespace == "TriageTrainer" || type.Namespace?.StartsWith("TriageTrainer.", StringComparison.Ordinal) == true
          || type.Namespace == "MultiplayerInfrastructure" || type.Namespace?.StartsWith("MultiplayerInfrastructure.", StringComparison.Ordinal) == true);

    private Dictionary<string, Document> ReadDocuments(string path)
    {
      if (files.TryGetValue(path, out var found))
        return found;
      string text = read(path);
      if (!text.Contains("--- !u!"))
        throw new InvalidDataException("Expected Unity text serialization; save the asset in Force Text mode before building.");
      var result = new Dictionary<string, Document>();
      foreach (Match match in Documents.Matches(text))
      {
        var doc = new Document { Path = path, ClassId = match.Groups[1].Value, Id = match.Groups[2].Value, Body = match.Groups[3].Value };
        result.Add(doc.Id, doc);
      }
      if (result.Count == 0)
        throw new InvalidDataException("No readable Unity YAML documents.");
      files.Add(path, result);
      return result;
    }

    private Document Resolve(string localPath, string reference)
    {
      string id = FileId(reference);
      if (id == "0" || id.Length == 0)
        return null;
      string guid = Regex.Match(reference, @"guid: (\w+)").Groups[1].Value;
      string path = guid.Length == 0 ? localPath : assetPath(guid);
      if (string.IsNullOrEmpty(path))
        return null;
      // Native imported assets are resolved by their extension below, never deserialized as scene objects.
      if (path != localPath && Path.GetExtension(path) != ".prefab" && Path.GetExtension(path) != ".unity")
        return new Document { Path = path, Id = id, ClassId = "asset", Body = "" };
      return ReadDocuments(path).TryGetValue(id, out var doc) ? doc : null;
    }

    private Type GetObjectType(Document doc, HashSet<string> visited)
    {
      if (doc == null || !visited.Add(doc.Path + ":" + doc.Id))
        return null;
      if (doc.ClassId == "asset")
      {
        string key = doc.Path + ":" + doc.Id;
        if (!importedTypes.TryGetValue(key, out var assetType))
          importedTypes[key] = assetType = importedType?.Invoke(doc.Path, doc.Id);
        return assetType;
      }
      // A stripped document's local m_Script can itself be stale. Trust the source object.
      string source = Regex.Match(doc.Body, @"m_CorrespondingSourceObject: (\{[^\r\n]+\})").Groups[1].Value;
      if (FileId(source) != "0" && FileId(source).Length > 0)
        return GetObjectType(Resolve(doc.Path, source), visited);
      if (doc.ClassId == "114")
      {
        string guid = Regex.Match(doc.Body, @"m_Script: \{fileID: [^,]+, guid: (\w+)").Groups[1].Value;
        if (guid.Length > 0)
        {
          string id = FileId(Regex.Match(doc.Body, @"m_Script: (\{[^\r\n]+\})").Groups[1].Value);
          string key = guid + ":" + id;
          if (!scripts.TryGetValue(key, out var type))
            scripts[key] = type = scriptType(guid, id);
          return type;
        }
      }
      string className = Regex.Match(doc.Body, @"^(\w+):").Groups[1].Value;
      return AppDomain.CurrentDomain.GetAssemblies()
        .Select(assembly => assembly.GetType("UnityEngine." + className, false))
        .FirstOrDefault(type => type != null && typeof(UnityEngine.Object).IsAssignableFrom(type));
    }

    private static string FileId(string reference)
      => Regex.Match(reference, @"fileID: (-?\d+)").Groups[1].Value;
  }
}
