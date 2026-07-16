using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public enum ScenarioRequirementKind
  {
    Entity,
    Npc,
    Interactable,
    SpatialAnchor,
    SpawnPoint,
    EntityPreset,
    ItemDefinition,
    EventHandler,
    RegistryEntry,
    RuntimeSignal,
    AudioResource,
    SpriteResource,
    TtsTranscript,
    TtsVoice,
    QuestDefinition,
    PlayerTagState,
    RuntimeEntityReference,
    Service
  }

  public enum ScenarioRequirementCapability
  {
    RegisteredEntity,
    ResolvableNpcMoveTarget,
    RegisteredNpcComponent,
    ProvidesPosition,
    HighlightableWaypoint,
    Interactable,
    ToggleableInteractable,
    ItemSubmissionTarget,
    ScenarioEntityInitTarget,
    ScenarioTriageAssessTarget,
    PatientMedicalStateTarget,
    SpawnablePreset,
    InvokableEventHandler,
    LoadableResource,
    ResolvableQuestDefinition
  }

  public enum ScenarioRequirementAvailability
  {
    BeforeScenarioStart,
    WhenNodeReached,
    OptionalFallback,
    NotConsumed
  }

  public enum ScenarioRequirementDirection { Consumes, Produces }
  public enum ScenarioRequirementExpectedSupply { Scene, Scenario, Gameplay, External }
  public enum ScenarioRequirementSourceOrigin { Inferred, AiImported, Declared, Override, Suppressed }
  public enum ScenarioRequirementScope { AnyLoadedScene, Bootstrap, SystemOverlay, Overworld, Content, DontDestroyOnLoad }
  public enum ScenarioRequirementAuthority { Any, Server, Client, HostOnly }
  public enum ScenarioRequirementDiagnosticSeverity { Info, Warning, Error, Fatal }
  public enum ScenarioRequirementDeclarationOperation { Declare, Override }
  public enum ScenarioRequirementBindingMode
  {
    ExistingSceneObject,
    GeneratedSceneObject,
    PrefabInstance,
    RegistryProvided,
    RuntimeProduced,
    External
  }

  public sealed class ScenarioRequirementBindingHint
  {
    public ScenarioRequirementBindingMode Mode { get; }
    public string FactoryIdentifier { get; }
    public string ProviderIdentifier { get; }

    public ScenarioRequirementBindingHint(
      ScenarioRequirementBindingMode mode,
      string factoryIdentifier,
      string providerIdentifier)
    {
      Mode = mode;
      FactoryIdentifier = factoryIdentifier ?? string.Empty;
      ProviderIdentifier = providerIdentifier ?? string.Empty;
    }
  }

  public sealed class ScenarioRequirementSuppression
  {
    public string Reason { get; }
    public string Owner { get; }
    public DateTime ExpiresOnUtc { get; }

    public ScenarioRequirementSuppression(string reason, string owner, DateTime expiresOnUtc)
    {
      Reason = reason ?? string.Empty;
      Owner = owner ?? string.Empty;
      ExpiresOnUtc = expiresOnUtc.Date;
    }
  }

  public sealed class ScenarioRequirementDeclarationSource
  {
    public ScenarioRequirementSourceOrigin Origin { get; }
    public ScenarioRequirementDeclarationOperation Operation { get; }
    public string NodeIdentifier { get; }
    public string FieldPath { get; }
    public int DocumentIndex { get; }

    public ScenarioRequirementDeclarationSource(
      ScenarioRequirementSourceOrigin origin,
      ScenarioRequirementDeclarationOperation operation,
      string nodeIdentifier,
      string fieldPath,
      int documentIndex)
    {
      Origin = origin;
      Operation = operation;
      NodeIdentifier = nodeIdentifier ?? string.Empty;
      FieldPath = fieldPath ?? string.Empty;
      DocumentIndex = documentIndex;
    }
  }

  public readonly struct ScenarioRequirementKey : IEquatable<ScenarioRequirementKey>, IComparable<ScenarioRequirementKey>
  {
    public ScenarioRequirementKind Kind { get; }
    public string Identifier { get; }

    public ScenarioRequirementKey(ScenarioRequirementKind kind, string normalizedIdentifier)
    {
      if (string.IsNullOrWhiteSpace(normalizedIdentifier)
          || !string.Equals(normalizedIdentifier, normalizedIdentifier.Trim(), StringComparison.Ordinal))
      {
        throw new ArgumentException("Requirement identifier must be non-empty and already trimmed.", nameof(normalizedIdentifier));
      }

      Kind = kind;
      Identifier = normalizedIdentifier;
    }

    public int CompareTo(ScenarioRequirementKey other)
    {
      var kindComparison = Kind.CompareTo(other.Kind);
      return kindComparison != 0
        ? kindComparison
        : StringComparer.Ordinal.Compare(Identifier, other.Identifier);
    }

    public bool Equals(ScenarioRequirementKey other)
      => Kind == other.Kind && string.Equals(Identifier, other.Identifier, StringComparison.Ordinal);

    public override bool Equals(object obj) => obj is ScenarioRequirementKey other && Equals(other);

    public override int GetHashCode()
    {
      unchecked
      {
        return ((int)Kind * 397) ^ StringComparer.Ordinal.GetHashCode(Identifier ?? string.Empty);
      }
    }

    public override string ToString() => Kind + ":" + EncodeIdentifier(Identifier ?? string.Empty);

    private static string EncodeIdentifier(string value)
    {
      var bytes = Encoding.UTF8.GetBytes(value);
      var builder = new StringBuilder(bytes.Length);
      foreach (var valueByte in bytes)
      {
        var isUnreserved = valueByte >= (byte)'A' && valueByte <= (byte)'Z'
                           || valueByte >= (byte)'a' && valueByte <= (byte)'z'
                           || valueByte >= (byte)'0' && valueByte <= (byte)'9'
                           || valueByte == (byte)'-' || valueByte == (byte)'.'
                           || valueByte == (byte)'_' || valueByte == (byte)'~';
        if (isUnreserved)
        {
          builder.Append((char)valueByte);
        }
        else
        {
          builder.Append('%').Append(valueByte.ToString("X2"));
        }
      }

      return builder.ToString();
    }
  }

  public readonly struct ScenarioRequirementCardinality : IEquatable<ScenarioRequirementCardinality>
  {
    public int Minimum { get; }
    public int? Maximum { get; }
    public bool IsValidated { get; }

    public ScenarioRequirementCardinality(int minimum, int? maximum, bool isValidated = true)
    {
      if (minimum < 0 || maximum < 0 || isValidated && maximum.HasValue && minimum > maximum.Value)
      {
        throw new ArgumentOutOfRangeException(nameof(minimum), "Cardinality bounds are invalid.");
      }

      Minimum = minimum;
      Maximum = maximum;
      IsValidated = isValidated;
    }

    public static ScenarioRequirementCardinality ExactlyOne => new ScenarioRequirementCardinality(1, 1);
    public static ScenarioRequirementCardinality AtLeastOne => new ScenarioRequirementCardinality(1, null);
    public static ScenarioRequirementCardinality NotValidated => new ScenarioRequirementCardinality(0, null, false);

    public bool Equals(ScenarioRequirementCardinality other)
      => Minimum == other.Minimum && Maximum == other.Maximum && IsValidated == other.IsValidated;

    public override bool Equals(object obj) => obj is ScenarioRequirementCardinality other && Equals(other);
    public override int GetHashCode() => (Minimum << 16) ^ ((Maximum ?? -1) << 1) ^ (IsValidated ? 1 : 0);
  }

  public sealed class ScenarioRequirementOccurrence
  {
    public string ScenarioIdentifier { get; }
    public string NodeIdentifier { get; }
    public ScenarioNodeType NodeType { get; }
    public string FieldPath { get; }
    public string Usage { get; }
    public ScenarioRequirementSourceOrigin SourceOrigin { get; }
    public ScenarioRequirementDirection Direction { get; }
    public ScenarioRequirementAvailability Availability { get; }
    public ScenarioRequirementExpectedSupply ExpectedSupply { get; }
    public string ResolverDiscriminator { get; }

    internal ScenarioRequirementOccurrence(
      string scenarioIdentifier,
      string nodeIdentifier,
      ScenarioNodeType nodeType,
      string fieldPath,
      string usage,
      ScenarioRequirementDirection direction,
      ScenarioRequirementAvailability availability,
      ScenarioRequirementExpectedSupply expectedSupply,
      string resolverDiscriminator = null)
    {
      ScenarioIdentifier = scenarioIdentifier ?? string.Empty;
      NodeIdentifier = nodeIdentifier ?? string.Empty;
      NodeType = nodeType;
      FieldPath = fieldPath ?? string.Empty;
      Usage = usage ?? string.Empty;
      SourceOrigin = ScenarioRequirementSourceOrigin.Inferred;
      Direction = direction;
      Availability = availability;
      ExpectedSupply = expectedSupply;
      ResolverDiscriminator = resolverDiscriminator ?? string.Empty;
    }

    internal string StableIdentity => string.Join("\u001f", new[]
    {
      ScenarioIdentifier,
      NodeIdentifier,
      ((int)NodeType).ToString(),
      FieldPath,
      Usage,
      ((int)SourceOrigin).ToString(),
      ((int)Direction).ToString(),
      ((int)Availability).ToString(),
      ((int)ExpectedSupply).ToString(),
      ResolverDiscriminator
    });

    internal ScenarioRequirementOccurrence WithAvailability(ScenarioRequirementAvailability availability)
      => new ScenarioRequirementOccurrence(
        ScenarioIdentifier, NodeIdentifier, NodeType, FieldPath, Usage, Direction,
        availability, ExpectedSupply, ResolverDiscriminator);
  }

  public sealed class ScenarioRequirementDescriptor
  {
    public ScenarioRequirementKey Key { get; }
    public ScenarioRequirementKind Kind => Key.Kind;
    public string Identifier => Key.Identifier;
    public ScenarioRequirementScope Scope { get; }
    public ScenarioRequirementAuthority Authority { get; }
    public ScenarioRequirementCardinality Cardinality { get; }
    public ScenarioRequirementAvailability EffectiveAvailability { get; }
    public IReadOnlyList<ScenarioRequirementCapability> Capabilities { get; }
    public IReadOnlyList<ScenarioRequirementOccurrence> Occurrences { get; }
    public ScenarioRequirementBindingHint BindingHint { get; }
    public bool MustProve { get; }
    public bool IsSuppressed => Suppression != null;
    public ScenarioRequirementSuppression Suppression { get; }
    public IReadOnlyList<ScenarioRequirementDeclarationSource> DeclarationSources { get; }

    public ScenarioRequirementDescriptor(
      ScenarioRequirementKey key,
      ScenarioRequirementCardinality cardinality,
      ScenarioRequirementAvailability effectiveAvailability,
      ScenarioRequirementAuthority authority,
      IEnumerable<ScenarioRequirementCapability> capabilities,
      IEnumerable<ScenarioRequirementOccurrence> occurrences)
      : this(key, ScenarioRequirementScope.AnyLoadedScene, authority, cardinality,
        effectiveAvailability, capabilities, occurrences, null, false, null,
        Array.Empty<ScenarioRequirementDeclarationSource>())
    {
    }

    public ScenarioRequirementDescriptor(
      ScenarioRequirementKey key,
      ScenarioRequirementScope scope,
      ScenarioRequirementAuthority authority,
      ScenarioRequirementCardinality cardinality,
      ScenarioRequirementAvailability effectiveAvailability,
      IEnumerable<ScenarioRequirementCapability> capabilities,
      IEnumerable<ScenarioRequirementOccurrence> occurrences,
      ScenarioRequirementBindingHint bindingHint,
      bool mustProve,
      ScenarioRequirementSuppression suppression,
      IEnumerable<ScenarioRequirementDeclarationSource> declarationSources)
    {
      Key = key;
      Scope = scope;
      Authority = authority;
      Cardinality = cardinality;
      EffectiveAvailability = effectiveAvailability;
      Capabilities = Array.AsReadOnly(capabilities.Distinct().OrderBy(value => value).ToArray());
      Occurrences = Array.AsReadOnly(occurrences.ToArray());
      BindingHint = bindingHint;
      MustProve = mustProve;
      Suppression = suppression;
      DeclarationSources = Array.AsReadOnly((declarationSources ?? Array.Empty<ScenarioRequirementDeclarationSource>())
        .OrderBy(value => value.DocumentIndex).ToArray());
    }
  }

  public sealed class ScenarioRequirementDiagnostic
  {
    public string Code { get; }
    public string Name { get; }
    public ScenarioRequirementDiagnosticSeverity Severity { get; }
    public string Message { get; }
    public bool HasRequirementKey { get; }
    public ScenarioRequirementKey RequirementKey { get; }
    public ScenarioRequirementOccurrence Source { get; }
    public string RawIdentifier { get; }
    public string FixHint { get; }

    internal ScenarioRequirementDiagnostic(
      string code,
      string name,
      ScenarioRequirementDiagnosticSeverity severity,
      string message,
      ScenarioRequirementOccurrence source,
      string rawIdentifier,
      string fixHint,
      ScenarioRequirementKey? requirementKey = null)
    {
      Code = code;
      Name = name;
      Severity = severity;
      Message = message;
      Source = source;
      RawIdentifier = rawIdentifier ?? string.Empty;
      FixHint = fixHint ?? string.Empty;
      HasRequirementKey = requirementKey.HasValue;
      RequirementKey = requirementKey.GetValueOrDefault();
    }
  }

  public sealed class ScenarioRequirementManifest
  {
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; }
    public string ScenarioIdentifier { get; }
    public string ScenarioSha256 { get; }
    public string GraphFingerprint { get; }
    public IReadOnlyList<ScenarioRequirementDescriptor> Requirements { get; }
    public IReadOnlyList<ScenarioRequirementDiagnostic> Diagnostics { get; }
    public bool HasErrors { get; }
    public bool IsValid => !HasErrors;

    internal ScenarioRequirementManifest(
      string scenarioIdentifier,
      IEnumerable<ScenarioRequirementDescriptor> requirements,
      IEnumerable<ScenarioRequirementDiagnostic> diagnostics)
      : this(scenarioIdentifier, string.Empty, requirements, diagnostics)
    {
    }

    internal ScenarioRequirementManifest(
      string scenarioIdentifier,
      string scenarioSha256,
      IEnumerable<ScenarioRequirementDescriptor> requirements,
      IEnumerable<ScenarioRequirementDiagnostic> diagnostics)
    {
      SchemaVersion = CurrentSchemaVersion;
      ScenarioIdentifier = scenarioIdentifier ?? string.Empty;
      ScenarioSha256 = scenarioSha256 ?? string.Empty;
      // A source hash is not a graph fingerprint: inferred manifests have no
      // source bytes, and callers must not accidentally use their hash as a
      // runtime manifest lookup key.
      GraphFingerprint = string.Empty;
      Requirements = new ReadOnlyCollection<ScenarioRequirementDescriptor>(requirements.ToArray());
      Diagnostics = new ReadOnlyCollection<ScenarioRequirementDiagnostic>(diagnostics.ToArray());
      HasErrors = Diagnostics.Any(diagnostic => diagnostic.Severity >= ScenarioRequirementDiagnosticSeverity.Error);
    }

    internal ScenarioRequirementManifest(
      string scenarioIdentifier,
      string scenarioSha256,
      string graphFingerprint,
      IEnumerable<ScenarioRequirementDescriptor> requirements,
      IEnumerable<ScenarioRequirementDiagnostic> diagnostics)
      : this(scenarioIdentifier, scenarioSha256, requirements, diagnostics)
    {
      GraphFingerprint = graphFingerprint ?? string.Empty;
    }
  }
}
