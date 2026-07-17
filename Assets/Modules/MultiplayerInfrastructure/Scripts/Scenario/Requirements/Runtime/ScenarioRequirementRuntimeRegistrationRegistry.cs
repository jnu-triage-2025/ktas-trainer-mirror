using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public readonly struct ScenarioRequirementRuntimeRegistrationToken : IEquatable<ScenarioRequirementRuntimeRegistrationToken>
  {
    internal ScenarioRequirementRuntimeRegistrationToken(int generation, int ownerInstanceId, long sequence) { Generation = generation; OwnerInstanceId = ownerInstanceId; Sequence = sequence; }
    public int Generation { get; }
    public int OwnerInstanceId { get; }
    public long Sequence { get; }
    public bool IsValid => Sequence > 0;
    public bool Equals(ScenarioRequirementRuntimeRegistrationToken other) => Generation == other.Generation && OwnerInstanceId == other.OwnerInstanceId && Sequence == other.Sequence;
    public override bool Equals(object obj) => obj is ScenarioRequirementRuntimeRegistrationToken other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Generation, OwnerInstanceId, Sequence);
  }

  public sealed class ScenarioRequirementRuntimeRegistration
  {
    public ScenarioRequirementRuntimeRegistrationToken Token { get; }
    public Registry.RegistryType RegistryType { get; }
    public ScenarioRequirementKind Kind { get; }
    public string Identifier { get; }
    public string OwnerKey { get; }
    public int OwnerInstanceId { get; }
    public int SceneHandle { get; }
    public string ScenePath { get; }
    public string HierarchyPath { get; }
    public IReadOnlyList<ScenarioRequirementCapability> Capabilities { get; }
    public ScenarioRequirementProviderOrigin Origin { get; }
    public UnityEngine.Object Owner { get; }
    public bool IsActive { get; }
    public bool IsEnabled { get; }
    public string LogicalIdentity => string.Join("|", RegistryType, Kind, Identifier, OwnerKey, Origin);

    internal ScenarioRequirementRuntimeRegistration(ScenarioRequirementRuntimeRegistrationToken token, Registry.RegistryType registryType, ScenarioRequirementKind kind, string identifier, string ownerKey, int ownerInstanceId, Component owner, IEnumerable<ScenarioRequirementCapability> capabilities, ScenarioRequirementProviderOrigin origin)
    {
      Token = token; RegistryType = registryType; Kind = kind; Identifier = identifier ?? string.Empty; OwnerKey = ownerKey ?? string.Empty; OwnerInstanceId = ownerInstanceId; SceneHandle = owner != null ? owner.gameObject.scene.handle : 0; ScenePath = owner != null ? owner.gameObject.scene.path : string.Empty; HierarchyPath = owner != null ? GetHierarchyPath(owner.transform) : string.Empty; Capabilities = (capabilities ?? Array.Empty<ScenarioRequirementCapability>()).Distinct().OrderBy(value => value).ToArray(); Origin = origin; Owner = owner; IsActive = owner == null || owner.gameObject.activeInHierarchy; IsEnabled = owner == null || !(owner is Behaviour behaviour) || behaviour.enabled;
    }

    private static string GetHierarchyPath(Transform transform) { var stack = new Stack<string>(); for (var cursor = transform; cursor != null; cursor = cursor.parent) stack.Push(cursor.name); return string.Join("/", stack); }
  }

  public readonly struct ScenarioRequirementRuntimeRegistrationHandle : IDisposable
  {
    private readonly ScenarioRequirementRuntimeRegistrationToken _token;
    internal ScenarioRequirementRuntimeRegistrationHandle(ScenarioRequirementRuntimeRegistrationToken token) => _token = token;
    public bool IsValid => _token.IsValid;
    public void Dispose() => ScenarioRequirementRuntimeRegistrationRegistry.Unregister(_token);
  }

  public static class ScenarioRequirementRuntimeRegistrationRegistry
  {
    private static readonly Dictionary<ScenarioRequirementRuntimeRegistrationToken, ScenarioRequirementRuntimeRegistration> Records = new Dictionary<ScenarioRequirementRuntimeRegistrationToken, ScenarioRequirementRuntimeRegistration>();
    private static readonly Dictionary<string, ScenarioRequirementRuntimeRegistrationToken> LogicalTokens = new Dictionary<string, ScenarioRequirementRuntimeRegistrationToken>(StringComparer.Ordinal);
    private static int _generation;
    private static long _sequence;
    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration() => Reset();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() { if (_initialized) return; _initialized = true; SceneManager.sceneUnloaded -= OnSceneUnloaded; SceneManager.sceneUnloaded += OnSceneUnloaded; }

    public static void Reset() { Records.Clear(); LogicalTokens.Clear(); _generation++; _sequence = 0; _initialized = false; }

    public static ScenarioRequirementRuntimeRegistrationHandle Register(Registry.RegistryType registryType, ScenarioRequirementKind kind, string identifier, Component owner, IEnumerable<ScenarioRequirementCapability> capabilities, ScenarioRequirementProviderOrigin origin = ScenarioRequirementProviderOrigin.SceneComponent)
    {
      Initialize();
      if (owner == null || string.IsNullOrWhiteSpace(identifier)) return default;
      var ownerKey = "component:" + owner.GetType().AssemblyQualifiedName + ":" + owner.GetInstanceID();
      var logicalIdentity = string.Join("|", registryType, kind, identifier.Trim(), ownerKey, origin);
      if (LogicalTokens.TryGetValue(logicalIdentity, out var existing) && Records.ContainsKey(existing)) return new ScenarioRequirementRuntimeRegistrationHandle(existing);
      var token = new ScenarioRequirementRuntimeRegistrationToken(_generation, owner.GetInstanceID(), ++_sequence);
      var record = new ScenarioRequirementRuntimeRegistration(token, registryType, kind, identifier.Trim(), ownerKey, owner.GetInstanceID(), owner, capabilities, origin);
      Records.Add(token, record); LogicalTokens[logicalIdentity] = token;
      return new ScenarioRequirementRuntimeRegistrationHandle(token);
    }

    public static ScenarioRequirementRuntimeRegistrationHandle RegisterExternal(Registry.RegistryType registryType, ScenarioRequirementKind kind, string identifier, string ownerKey, IEnumerable<ScenarioRequirementCapability> capabilities, ScenarioRequirementProviderOrigin origin = ScenarioRequirementProviderOrigin.RegistryPreloaderDeclaration)
    {
      Initialize();
      if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(ownerKey)) return default;
      var logicalIdentity = string.Join("|", registryType, kind, identifier.Trim(), ownerKey, origin);
      if (LogicalTokens.TryGetValue(logicalIdentity, out var existing) && Records.ContainsKey(existing)) return new ScenarioRequirementRuntimeRegistrationHandle(existing);
      var token = new ScenarioRequirementRuntimeRegistrationToken(_generation, 0, ++_sequence);
      var record = new ScenarioRequirementRuntimeRegistration(token, registryType, kind, identifier.Trim(), ownerKey, 0, null, capabilities, origin);
      Records.Add(token, record); LogicalTokens[logicalIdentity] = token;
      return new ScenarioRequirementRuntimeRegistrationHandle(token);
    }

    public static IReadOnlyList<ScenarioRequirementRuntimeRegistration> GetAll()
      => Records.Values.OrderBy(value => value.Kind).ThenBy(value => value.Identifier, StringComparer.Ordinal).ThenBy(value => value.Token.Sequence).ToArray();

    public static void UnregisterExternal(Registry.RegistryType registryType, ScenarioRequirementKind kind, string identifier, string ownerKey, ScenarioRequirementProviderOrigin origin = ScenarioRequirementProviderOrigin.RegistryPreloaderDeclaration)
    {
      var logicalIdentity = string.Join("|", registryType, kind, identifier?.Trim(), ownerKey, origin);
      if (LogicalTokens.TryGetValue(logicalIdentity, out var token)) Unregister(token);
    }

    internal static void Unregister(ScenarioRequirementRuntimeRegistrationToken token)
    {
      if (!token.IsValid || token.Generation != _generation || !Records.TryGetValue(token, out var record)) return;
      Records.Remove(token); LogicalTokens.Remove(record.LogicalIdentity);
    }

    private static void OnSceneUnloaded(Scene scene)
    {
      foreach (var token in Records.Values.Where(value => value.SceneHandle == scene.handle && value.Owner != null).Select(value => value.Token).ToArray()) Unregister(token);
    }
  }
}
