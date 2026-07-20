using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario.Preflight;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  /// <summary>Scenario graph만 읽어 immutable inferred requirement manifest를 만든다.</summary>
  public static class ScenarioRequirementCompiler
  {
    public static ScenarioRequirementManifest Compile(
      ScenarioGraph graph,
      byte[] scenarioSourceBytes,
      ScenarioRequirementsDocument sidecar,
      ScenarioRequirementCompilationContext context)
    {
      if (scenarioSourceBytes == null) throw new ArgumentNullException(nameof(scenarioSourceBytes));
      var inferred = CompileInferred(graph);
      var sourceHash = ScenarioRequirementSourceHasher.ComputeSha256(scenarioSourceBytes);
      var merged = ScenarioRequirementMergePolicy.Merge(inferred, sourceHash, sidecar, context);
      return new ScenarioRequirementManifest(merged.ScenarioIdentifier, sourceHash, ScenarioGraphFingerprint.Compute(graph), merged.Requirements, merged.Diagnostics);
    }

    public static ScenarioRequirementManifest CompileInferred(ScenarioGraph graph)
    {
      if (graph == null) throw new ArgumentNullException(nameof(graph));

      ScenarioNodeRuntimeLookupRegistry.ValidateCanonicalExtractorCoverage(
        ScenarioNodeRequirementExtractors.RegisteredNodeTypes);
      var builder = new ScenarioRequirementBuilder(graph.Identifier);
      ScenarioGraphStructuralValidator.Validate(graph, builder);
      var nodes = graph.Nodes.Values
        .OrderBy(node => node?.Identifier ?? string.Empty, StringComparer.Ordinal)
        .ThenBy(node => node == null ? int.MaxValue : (int)node.NodeType)
        .ThenBy(node => node?.GetType().FullName ?? string.Empty, StringComparer.Ordinal);

      foreach (var node in nodes)
      {
        if (node == null)
        {
          builder.AddRegistrationDiagnostic(null, "Scenario graph contains a null node.");
          continue;
        }

        if (!ScenarioNodeRuntimeLookupRegistry.TryGet(node.NodeType, out var registration)
            || !registration.RuntimeNodeType.IsInstanceOfType(node))
        {
          builder.AddRegistrationDiagnostic(node, "Node type and CLR registration do not match.");
          continue;
        }

        if (!ScenarioNodeRequirementExtractors.TryExtract(node, builder))
        {
          builder.AddRegistrationDiagnostic(node, "No canonical extractor matches the node type and CLR model.");
        }
      }

      return builder.Build();
    }
  }

  internal sealed class ScenarioRequirementBuilder
  {
    private sealed class PendingDescriptor
    {
      public readonly HashSet<ScenarioRequirementCapability> Capabilities = new HashSet<ScenarioRequirementCapability>();
      public readonly Dictionary<string, ScenarioRequirementOccurrence> Occurrences = new Dictionary<string, ScenarioRequirementOccurrence>(StringComparer.Ordinal);
      public ScenarioRequirementAuthority? Authority;
    }

    private readonly string _scenarioIdentifier;
    private readonly Dictionary<ScenarioRequirementKey, PendingDescriptor> _requirements = new Dictionary<ScenarioRequirementKey, PendingDescriptor>();
    private readonly List<ScenarioRequirementDiagnostic> _diagnostics = new List<ScenarioRequirementDiagnostic>();

    public ScenarioRequirementBuilder(string scenarioIdentifier)
    {
      _scenarioIdentifier = scenarioIdentifier ?? string.Empty;
    }

    public void Add(
      IScenarioNode node,
      string fieldPath,
      string usage,
      ScenarioRequirementKind kind,
      string rawIdentifier,
      bool required,
      ScenarioRequirementAvailability availability,
      ScenarioRequirementExpectedSupply expectedSupply,
      ScenarioRequirementDirection direction = ScenarioRequirementDirection.Consumes,
      params ScenarioRequirementCapability[] capabilities)
      => AddWithAuthority(node, fieldPath, usage, kind, rawIdentifier, required, availability,
        expectedSupply, direction, ScenarioRequirementAuthority.Any, null, capabilities);

    public void AddWithResolver(
      IScenarioNode node,
      string fieldPath,
      string usage,
      ScenarioRequirementKind kind,
      string rawIdentifier,
      bool required,
      ScenarioRequirementAvailability availability,
      ScenarioRequirementExpectedSupply expectedSupply,
      ScenarioRequirementDirection direction,
      string resolverDiscriminator,
      params ScenarioRequirementCapability[] capabilities)
      => AddWithAuthority(node, fieldPath, usage, kind, rawIdentifier, required, availability,
        expectedSupply, direction, ScenarioRequirementAuthority.Any, resolverDiscriminator, capabilities);

    public void AddWithAuthority(
      IScenarioNode node,
      string fieldPath,
      string usage,
      ScenarioRequirementKind kind,
      string rawIdentifier,
      bool required,
      ScenarioRequirementAvailability availability,
      ScenarioRequirementExpectedSupply expectedSupply,
      ScenarioRequirementDirection direction,
      ScenarioRequirementAuthority authority,
      string resolverDiscriminator = null,
      params ScenarioRequirementCapability[] capabilities)
    {
      var occurrence = new ScenarioRequirementOccurrence(
        _scenarioIdentifier,
        node?.Identifier,
        node?.NodeType ?? default,
        fieldPath,
        usage,
        direction,
        availability,
        expectedSupply,
        resolverDiscriminator);

      if (rawIdentifier == null)
      {
        if (required)
        {
          _diagnostics.Add(new ScenarioRequirementDiagnostic(
            "SIR100",
            "InvalidIdentifier",
            ScenarioRequirementDiagnosticSeverity.Error,
            $"Required identifier at '{fieldPath}' is null, empty, or whitespace.",
            occurrence,
            rawIdentifier,
            "Provide a non-empty identifier."));
        }
        return;
      }

      if (string.IsNullOrWhiteSpace(rawIdentifier))
      {
        _diagnostics.Add(new ScenarioRequirementDiagnostic(
          "SIR100",
          "InvalidIdentifier",
          ScenarioRequirementDiagnosticSeverity.Error,
          $"Identifier at '{fieldPath}' is empty or whitespace.",
          occurrence,
          rawIdentifier,
          "Remove the optional field or provide a non-empty identifier."));
        return;
      }

      var key = new ScenarioRequirementKey(kind, rawIdentifier.Trim());
      if (!_requirements.TryGetValue(key, out var pending))
      {
        pending = new PendingDescriptor();
        _requirements.Add(key, pending);
      }

      foreach (var capability in capabilities ?? Array.Empty<ScenarioRequirementCapability>())
      {
        pending.Capabilities.Add(capability);
      }
      if (!pending.Authority.HasValue || pending.Authority.Value == ScenarioRequirementAuthority.Any)
      {
        pending.Authority = authority;
      }
      else if (authority == ScenarioRequirementAuthority.Any || pending.Authority.Value == authority)
      {
        // Any is compatible with an existing fixed authority; preserve the narrower constraint.
      }
      else
      {
        _diagnostics.Add(new ScenarioRequirementDiagnostic(
          "SIR206", "AuthorityConflict", ScenarioRequirementDiagnosticSeverity.Error,
          $"Requirement '{key}' has incompatible inferred authorities '{pending.Authority}' and '{authority}'.",
          occurrence, rawIdentifier, "Split the identifiers or align node execution authority.", key));
      }
      pending.Occurrences[occurrence.StableIdentity] = occurrence;
    }

    public void AddService(
      IScenarioNode node,
      string fieldPath,
      string usage,
      string identifier,
      ScenarioRequirementAvailability availability,
      ScenarioRequirementAuthority authority = ScenarioRequirementAuthority.Any)
      => AddWithAuthority(node, fieldPath, usage, ScenarioRequirementKind.Service, identifier, true,
        availability, ScenarioRequirementExpectedSupply.External, ScenarioRequirementDirection.Consumes, authority);

    public void AddRegistrationDiagnostic(IScenarioNode node, string message)
    {
      var occurrence = new ScenarioRequirementOccurrence(
        _scenarioIdentifier,
        node?.Identifier,
        node?.NodeType ?? default,
        "$node",
        "node-registration",
        ScenarioRequirementDirection.Consumes,
        ScenarioRequirementAvailability.NotConsumed,
        ScenarioRequirementExpectedSupply.Scenario);
      _diagnostics.Add(new ScenarioRequirementDiagnostic(
        "SIR102", "InvalidNodeRegistration", ScenarioRequirementDiagnosticSeverity.Error,
        message, occurrence, string.Empty, "Use a registered node type and matching CLR model."));
    }

    public void AddStructuralDiagnostic(
      string code,
      string name,
      IScenarioNode node,
      string fieldPath,
      string rawIdentifier,
      string message,
      string fixHint)
    {
      var occurrence = new ScenarioRequirementOccurrence(
        _scenarioIdentifier,
        node?.Identifier,
        node?.NodeType ?? default,
        fieldPath,
        "graph-structure",
        ScenarioRequirementDirection.Consumes,
        ScenarioRequirementAvailability.NotConsumed,
        ScenarioRequirementExpectedSupply.Scenario);
      _diagnostics.Add(new ScenarioRequirementDiagnostic(
        code, name, ScenarioRequirementDiagnosticSeverity.Error,
        message, occurrence, rawIdentifier, fixHint));
    }

    public ScenarioRequirementManifest Build()
    {
      var descriptors = _requirements
        .OrderBy(pair => pair.Key)
        .Select(pair => new ScenarioRequirementDescriptor(
          pair.Key,
          GetDefaultCardinality(pair.Key.Kind),
          GetEffectiveAvailability(pair.Value.Occurrences.Values),
          pair.Value.Authority ?? ScenarioRequirementAuthority.Any,
          pair.Value.Capabilities,
          pair.Value.Occurrences.Values.OrderBy(OccurrenceSortKey, StringComparer.Ordinal)))
        .ToList();

      foreach (var group in descriptors.GroupBy(
                 descriptor => ((int)descriptor.Kind) + "\u001f" + descriptor.Identifier,
                 StringComparer.OrdinalIgnoreCase))
      {
        var variants = group.OrderBy(value => value.Identifier, StringComparer.Ordinal).ToArray();
        if (variants.Length < 2) continue;

        var first = variants[0];
        _diagnostics.Add(new ScenarioRequirementDiagnostic(
          "SIR101",
          "CaseVariantIdentifier",
          ScenarioRequirementDiagnosticSeverity.Warning,
          "Case-only identifier variants are separate requirements: " + string.Join(", ", variants.Select(value => value.Key.ToString())),
          first.Occurrences.FirstOrDefault(),
          first.Identifier,
          "Use one consistent identifier casing.",
          first.Key));
      }

      var diagnostics = _diagnostics
        .OrderBy(diagnostic => diagnostic.HasRequirementKey ? 0 : 1)
        .ThenBy(diagnostic => diagnostic.HasRequirementKey ? diagnostic.RequirementKey.ToString() : string.Empty, StringComparer.Ordinal)
        .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
        .ThenBy(diagnostic => diagnostic.Source?.NodeIdentifier ?? string.Empty, StringComparer.Ordinal)
        .ThenBy(diagnostic => diagnostic.Source?.FieldPath ?? string.Empty, StringComparer.Ordinal)
        .ThenBy(diagnostic => diagnostic.RawIdentifier, StringComparer.Ordinal)
        .ToArray();

      return new ScenarioRequirementManifest(_scenarioIdentifier, descriptors, diagnostics);
    }

    private static string OccurrenceSortKey(ScenarioRequirementOccurrence value) => value.StableIdentity;

    private static ScenarioRequirementAvailability GetEffectiveAvailability(IEnumerable<ScenarioRequirementOccurrence> occurrences)
    {
      var consumers = occurrences.Where(value => value.Direction == ScenarioRequirementDirection.Consumes).ToArray();
      if (consumers.Length == 0) return ScenarioRequirementAvailability.NotConsumed;
      if (consumers.Any(value => value.Availability == ScenarioRequirementAvailability.BeforeScenarioStart)) return ScenarioRequirementAvailability.BeforeScenarioStart;
      if (consumers.Any(value => value.Availability == ScenarioRequirementAvailability.WhenNodeReached)) return ScenarioRequirementAvailability.WhenNodeReached;
      if (consumers.Any(value => value.Availability == ScenarioRequirementAvailability.OptionalFallback)) return ScenarioRequirementAvailability.OptionalFallback;
      return ScenarioRequirementAvailability.NotConsumed;
    }

    private static ScenarioRequirementCardinality GetDefaultCardinality(ScenarioRequirementKind kind)
    {
      switch (kind)
      {
        case ScenarioRequirementKind.EventHandler:
          return ScenarioRequirementCardinality.AtLeastOne;
        case ScenarioRequirementKind.RuntimeSignal:
        case ScenarioRequirementKind.PlayerTagState:
        case ScenarioRequirementKind.RuntimeEntityReference:
          return ScenarioRequirementCardinality.NotValidated;
        default:
          return ScenarioRequirementCardinality.ExactlyOne;
      }
    }

  }

  internal static class ScenarioNodeRequirementExtractors
  {
    private sealed class Registration
    {
      public ScenarioNodeType NodeType { get; }
      public Type RuntimeNodeType { get; }
      public Action<IScenarioNode, ScenarioRequirementBuilder> Extractor { get; }

      public Registration(
        ScenarioNodeType nodeType,
        Type runtimeNodeType,
        Action<IScenarioNode, ScenarioRequirementBuilder> extractor)
      {
        NodeType = nodeType;
        RuntimeNodeType = runtimeNodeType;
        Extractor = extractor;
      }
    }

    private const string DialogueService = "mi.service.dialogue-ui";
    private const string TtsService = "mi.service.tts";
    private const string TtsAudioSourceService = "mi.service.tts-audio-source";
    private const string QuestManagerService = "mi.service.quest-manager";
    private const string PlayerTagService = "mi.service.player-tag";
    private const string UserDescriptorService = "mi.service.user-descriptor";
    private const string ChatService = "mi.service.chat";

    private static readonly IReadOnlyDictionary<ScenarioNodeType, Registration> Registrations =
      CreateRegistrations();

    internal static IReadOnlyDictionary<ScenarioNodeType, Type> RegisteredNodeTypes { get; } =
      Registrations.ToDictionary(pair => pair.Key, pair => pair.Value.RuntimeNodeType);

    public static bool TryExtract(IScenarioNode node, ScenarioRequirementBuilder output)
    {
      if (node == null
          || !Registrations.TryGetValue(node.NodeType, out var registration)
          || !registration.RuntimeNodeType.IsInstanceOfType(node))
      {
        return false;
      }

      registration.Extractor(node, output);
      return true;
    }

    private static IReadOnlyDictionary<ScenarioNodeType, Registration> CreateRegistrations()
    {
      var registrations = new[]
      {
        Register<ScenarioDialogueNode>(ScenarioNodeType.Dialogue, ExtractDialogue),
        Register<ScenarioChoiceNode>(ScenarioNodeType.Choice, ExtractChoice),
        Register<ScenarioSoundNode>(ScenarioNodeType.Sound, (value, output) => AddResource(value, output, "soundResourceIdentifier", "sound", ScenarioRequirementKind.AudioResource, value.SoundResourceIdentifier, true, ScenarioRequirementAvailability.WhenNodeReached)),
        Register<ScenarioPlayerMoveNode>(ScenarioNodeType.PlayerMove, ExtractPlayerMove),
        Register<ScenarioNPCMoveNode>(ScenarioNodeType.NPCMove, ExtractNpcMove),
        Register<ScenarioCameraTargetNode>(ScenarioNodeType.CameraTarget, (value, output) => output.Add(value, "targetObjectIdentifier", "camera-target-not-consumed", ScenarioRequirementKind.Entity, value.TargetObjectIdentifier, false, ScenarioRequirementAvailability.NotConsumed, ScenarioRequirementExpectedSupply.Scene)),
        Register<ScenarioParallelNode>(ScenarioNodeType.Parallel, ExtractParallel),
        Register<ScenarioInvokeEventNode>(ScenarioNodeType.InvokeEvent, (value, output) => AddEvent(value, output, "eventIdentifier", "event-invocation", value.EventIdentifier, true)),
        Register<ScenarioServerInternalSignalNode>(ScenarioNodeType.ServerInternalSignal, NoRequirements),
        Register<ScenarioSignalListenerNode>(ScenarioNodeType.SignalListener, ExtractSignalListener),
        Register<ScenarioEntityStateSignalBindingNode>(ScenarioNodeType.EntityStateSignalBinding, ExtractEntityStateSignalBinding),
        Register<ScenarioValidatorNode>(ScenarioNodeType.Validator, ExtractValidator),
        Register<ScenarioQuestControlNode>(ScenarioNodeType.QuestControl, ExtractQuest),
        Register<ScenarioQuestWaypointHighlightNode>(ScenarioNodeType.QuestWaypointHighlight, (value, output) => output.Add(value, "waypointIdentifier", "waypoint-highlight", ScenarioRequirementKind.SpatialAnchor, value.WaypointIdentifier, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scene, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.HighlightableWaypoint)),
        Register<ScenarioDelayNode>(ScenarioNodeType.Delay, NoRequirements),
        Register<ScenarioInteractionNode>(ScenarioNodeType.Interaction, ExtractInteraction),
        Register<ScenarioCombineItemNode>(ScenarioNodeType.CombineItem, ExtractCombine),
        Register<ScenarioQuizNode>(ScenarioNodeType.Quiz, ExtractQuiz),
        Register<ScenarioStateUpdateNode>(ScenarioNodeType.StateUpdate, NoRequirements),
        Register<ScenarioPlayTTSNode>(ScenarioNodeType.PlayTTS, ExtractPlayTts),
        Register<ScenarioPlayerTagNode>(ScenarioNodeType.PlayerTag, ExtractPlayerTag),
        Register<ScenarioEntityPresetSpawnNode>(ScenarioNodeType.EntityPresetSpawn, ExtractPresetSpawn),
        Register<ScenarioEntityTagNode>(ScenarioNodeType.EntityTag, ExtractEntityTag),
        Register<ScenarioEntityInitNode>(ScenarioNodeType.EntityInit, ExtractEntityInit),
        Register<ScenarioTriageAssessControlNode>(ScenarioNodeType.TriageAssessControl, (value, output) => output.Add(value, "targetEntityIdentifier", "triage-assess-target", ScenarioRequirementKind.Entity, value.TargetEntityIdentifier, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scene, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.ScenarioTriageAssessTarget)),
        Register<ScenarioPatientMedicalStatePresetNode>(ScenarioNodeType.PatientMedicalStatePreset, ExtractPatient),
        Register<ScenarioItemSubmissionConfigNode>(ScenarioNodeType.ItemSubmissionConfig, ExtractItemSubmission),
        Register<ScenarioNpcInteractControlNode>(ScenarioNodeType.NpcInteractControl, ExtractNpcInteract),
        Register<ScenarioChatPrintNode>(ScenarioNodeType.ChatPrint, ExtractChat),
        Register<ScenarioExecuteCommandNode>(ScenarioNodeType.ExecuteCommand, (value, output) => output.AddService(value, "$service.chat", "command-service", ChatService, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementAuthority.Server)),
        Register<ScenarioTimeControlNode>(ScenarioNodeType.TimeControl, NoRequirements),
        Register<ScenarioDisinteractableDialogueNode>(ScenarioNodeType.DisinteractableDialogue, ExtractDisinteractableDialogue)
      };

      var result = new Dictionary<ScenarioNodeType, Registration>();
      foreach (var registration in registrations)
      {
        if (!result.TryAdd(registration.NodeType, registration))
        {
          throw new InvalidOperationException($"Duplicate canonical extractor for '{registration.NodeType}'.");
        }
      }
      return result;
    }

    private static Registration Register<TNode>(
      ScenarioNodeType nodeType,
      Action<TNode, ScenarioRequirementBuilder> extractor)
      where TNode : class, IScenarioNode
      => new Registration(nodeType, typeof(TNode), (node, output) => extractor((TNode)node, output));

    private static void NoRequirements<TNode>(TNode node, ScenarioRequirementBuilder output)
      where TNode : class, IScenarioNode
    {
    }

    private static void ExtractDialogue(ScenarioDialogueNode node, ScenarioRequirementBuilder output)
    {
      AddResource(node, output, "portraitSpriteIdentifier", "dialogue-portrait", ScenarioRequirementKind.SpriteResource, node.PortraitSpriteIdentifier, false, ScenarioRequirementAvailability.OptionalFallback);
      output.AddService(node, "$service.dialogue-ui", "dialogue-ui", DialogueService, ScenarioRequirementAvailability.WhenNodeReached);
      if (!node.PlayTTS) return;
      output.AddService(node, "$service.tts", "inline-tts", TtsService, ScenarioRequirementAvailability.OptionalFallback);
      AddResource(node, output, "ttsVoiceIdentifier", "tts-voice", ScenarioRequirementKind.TtsVoice, node.TtsVoiceIdentifier, false, ScenarioRequirementAvailability.OptionalFallback);
    }

    private static void ExtractDisinteractableDialogue(ScenarioDisinteractableDialogueNode node, ScenarioRequirementBuilder output)
    {
      AddResource(node, output, "portraitSpriteIdentifier", "dialogue-portrait", ScenarioRequirementKind.SpriteResource, node.PortraitSpriteIdentifier, false, ScenarioRequirementAvailability.OptionalFallback);
      output.AddService(node, "$service.dialogue-ui", "dialogue-ui", DialogueService, ScenarioRequirementAvailability.OptionalFallback);
    }

    private static void ExtractChoice(ScenarioChoiceNode node, ScenarioRequirementBuilder output)
    {
      AddResource(node, output, "portraitSpriteIdentifier", "choice-portrait", ScenarioRequirementKind.SpriteResource, node.PortraitSpriteIdentifier, false, ScenarioRequirementAvailability.OptionalFallback);
      if (node.Options != null)
      {
        for (var index = 0; index < node.Options.Count; index++)
        {
          var option = node.Options[index];
          if (option != null) AddResource(node, output, $"options[{index}].displayIconIdentifier", "choice-icon", ScenarioRequirementKind.SpriteResource, option.DisplayIconIdentifier, false, ScenarioRequirementAvailability.OptionalFallback);
        }
      }
      output.AddService(node, "$service.dialogue-ui", "dialogue-ui", DialogueService, ScenarioRequirementAvailability.OptionalFallback);
      if (node.PlayTTS)
      {
        output.AddService(node, "$service.tts", "inline-tts", TtsService, ScenarioRequirementAvailability.OptionalFallback);
        AddResource(node, output, "ttsVoiceIdentifier", "tts-voice", ScenarioRequirementKind.TtsVoice, node.TtsVoiceIdentifier, false, ScenarioRequirementAvailability.OptionalFallback);
      }
    }

    private static void ExtractQuiz(ScenarioQuizNode node, ScenarioRequirementBuilder output)
    {
      output.AddService(node, "$service.dialogue-ui", "dialogue-ui", DialogueService, ScenarioRequirementAvailability.OptionalFallback);
      if (node.PlayTTS)
      {
        output.AddService(node, "$service.tts", "inline-tts", TtsService, ScenarioRequirementAvailability.OptionalFallback);
        AddResource(node, output, "ttsVoiceIdentifier", "tts-voice", ScenarioRequirementKind.TtsVoice, node.TtsVoiceIdentifier, false, ScenarioRequirementAvailability.OptionalFallback);
      }
    }

    private static void ExtractPlayTts(ScenarioPlayTTSNode node, ScenarioRequirementBuilder output)
    {
      AddResource(node, output, "transcriptIdentifier", "tts-transcript", ScenarioRequirementKind.TtsTranscript, node.TranscriptIdentifier, true, ScenarioRequirementAvailability.WhenNodeReached);
      AddResource(node, output, "ttsVoiceIdentifier", "tts-voice", ScenarioRequirementKind.TtsVoice, node.TtsVoiceIdentifier, false, ScenarioRequirementAvailability.OptionalFallback);
      output.AddService(node, "$service.tts", "tts-service", TtsService, ScenarioRequirementAvailability.WhenNodeReached);
      output.AddService(node, "$service.tts-audio-source", "tts-audio-source", TtsAudioSourceService, ScenarioRequirementAvailability.WhenNodeReached);
    }

    private static void ExtractPlayerMove(ScenarioPlayerMoveNode node, ScenarioRequirementBuilder output)
    {
      if (node.DestinationType == ScenarioMoveDestinationType.Waypoint)
        output.Add(node, "destinationIdentifier", "movement-destination", ScenarioRequirementKind.SpatialAnchor, node.DestinationIdentifier, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scene, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.ProvidesPosition);
    }

    private static void ExtractNpcMove(ScenarioNPCMoveNode node, ScenarioRequirementBuilder output)
    {
      output.Add(node, "npcIdentifier", "npc-move-target", ScenarioRequirementKind.Npc, node.NPCIdentifier, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scene, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.ResolvableNpcMoveTarget, ScenarioRequirementCapability.ProvidesPosition);
      if (node.DestinationType == ScenarioMoveDestinationType.Waypoint)
        output.Add(node, "destinationIdentifier", "movement-destination", ScenarioRequirementKind.SpatialAnchor, node.DestinationIdentifier, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scene, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.ProvidesPosition);
    }

    private static void ExtractParallel(ScenarioParallelNode node, ScenarioRequirementBuilder output)
    {
      var hasTags = false;
      if (node.Branches != null)
      {
        for (var branchIndex = 0; branchIndex < node.Branches.Count; branchIndex++)
        {
          var branch = node.Branches[branchIndex];
          if (branch == null) continue;
          hasTags |= AddTags(node, output, branch.RequiredPlayerTags, $"branches[{branchIndex}].requiredPlayerTags", "parallel-required-tag");
          hasTags |= AddTags(node, output, branch.ForbiddenPlayerTags, $"branches[{branchIndex}].forbiddenPlayerTags", "parallel-forbidden-tag");
        }
      }
      if (hasTags) AddPlayerTagServices(node, output);
    }

    private static void ExtractInteraction(ScenarioInteractionNode node, ScenarioRequirementBuilder output)
    {
      output.Add(node, "targetIdentifier", "interaction-target-not-consumed", ScenarioRequirementKind.Interactable, node.TargetIdentifier, false, ScenarioRequirementAvailability.NotConsumed, ScenarioRequirementExpectedSupply.Scene, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.Interactable);
      output.Add(node, "requiredItemIdentifier", "interaction-item-not-consumed", ScenarioRequirementKind.ItemDefinition, node.RequiredItemIdentifier, false, ScenarioRequirementAvailability.NotConsumed, ScenarioRequirementExpectedSupply.External);
      AddEvent(node, output, "completionConditionIdentifier", "interaction-completion", node.CompletionConditionIdentifier, false);
    }

    private static void ExtractSignalListener(ScenarioSignalListenerNode node, ScenarioRequirementBuilder output)
    {
      AddRuntimeSignal(node, output, "sourceSignalIdentifier", "signal-listener-source", node.SourceSignalIdentifier, ScenarioRequirementDirection.Consumes, ScenarioRequirementExpectedSupply.Gameplay);
      AddRuntimeSignal(node, output, "outputSignalIdentifier", "signal-listener-output", node.OutputSignalIdentifier, ScenarioRequirementDirection.Produces, ScenarioRequirementExpectedSupply.Scenario);
      if (node.RequiredSignalIdentifiers == null) return;
      for (var index = 0; index < node.RequiredSignalIdentifiers.Count; index++)
        AddRuntimeSignal(node, output, $"requiredSignalIdentifiers[{index}]", "signal-listener-required", node.RequiredSignalIdentifiers[index], ScenarioRequirementDirection.Consumes, ScenarioRequirementExpectedSupply.Gameplay);
    }

    private static void ExtractEntityStateSignalBinding(ScenarioEntityStateSignalBindingNode node, ScenarioRequirementBuilder output)
    {
      // Register 시에만 대상 엔티티가 필요하다(Unregister 는 식별자만으로 해제).
      bool requiresTarget = node.Operation == ScenarioEntityStateSignalBindingOperation.Register;

      // 대상 엔티티는 씬에 존재해야 하며, 소비되지 않는다(상태 이벤트를 관찰만 한다).
      output.Add(node, "targetEntityIdentifier", "entity-state-binding-target-not-consumed",
        ScenarioRequirementKind.Entity, node.TargetEntityIdentifier, requiresTarget,
        ScenarioRequirementAvailability.NotConsumed, ScenarioRequirementExpectedSupply.Scene,
        ScenarioRequirementDirection.Consumes);

      // 출력 신호는 이 노드가 시나리오 신호로 생산한다.
      AddRuntimeSignal(node, output, "outputSignalIdentifier", "entity-state-binding-output",
        node.OutputSignalIdentifier, ScenarioRequirementDirection.Produces,
        ScenarioRequirementExpectedSupply.Scenario);
    }

    private static void ExtractCombine(ScenarioCombineItemNode node, ScenarioRequirementBuilder output)
    {
      if (node.InputItemIdentifiers != null)
      {
        for (var index = 0; index < node.InputItemIdentifiers.Count; index++)
          output.Add(node, $"inputItemIdentifiers[{index}]", "combine-input-item-not-consumed", ScenarioRequirementKind.ItemDefinition, node.InputItemIdentifiers[index], false, ScenarioRequirementAvailability.NotConsumed, ScenarioRequirementExpectedSupply.External);
      }
      output.Add(node, "outputItemIdentifier", "combine-output-item-not-consumed", ScenarioRequirementKind.ItemDefinition, node.OutputItemIdentifier, true, ScenarioRequirementAvailability.NotConsumed, ScenarioRequirementExpectedSupply.External);
      if (!node.AutoCombine) AddEvent(node, output, "outputItemIdentifier", "combine-event", node.OutputItemIdentifier, true);
    }

    private static void ExtractQuest(ScenarioQuestControlNode node, ScenarioRequirementBuilder output)
    {
      output.AddService(node, "$service.quest-manager", "quest-manager", QuestManagerService, ScenarioRequirementAvailability.WhenNodeReached);
      if (node.Operation == ScenarioQuestOperationType.Remove) return;
      var identifier = !string.IsNullOrWhiteSpace(node.Quest?.DefinitionIdentifier)
        ? node.Quest.DefinitionIdentifier
        : node.QuestDefinitionIdentifier;
      output.Add(node, !string.IsNullOrWhiteSpace(node.Quest?.DefinitionIdentifier) ? "quest.definitionIdentifier" : "questDefinitionIdentifier", "quest-definition", ScenarioRequirementKind.QuestDefinition, identifier, false, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.External, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.ResolvableQuestDefinition);
    }

    private static void ExtractPlayerTag(ScenarioPlayerTagNode node, ScenarioRequirementBuilder output)
    {
      if (node.Scope == ScenarioPlayerTagScope.ByTag && node.Operation != ScenarioPlayerTagOperationType.Swap)
        AddTag(node, output, "tag", "player-tag-selector", node.Tag, false, ScenarioRequirementDirection.Consumes);

      switch (node.Operation)
      {
        case ScenarioPlayerTagOperationType.Add:
          AddTag(node, output, "tag", "player-tag-add", node.Tag, true, ScenarioRequirementDirection.Produces);
          break;
        case ScenarioPlayerTagOperationType.Remove:
          AddTag(node, output, "tag", "player-tag-remove", node.Tag, true, ScenarioRequirementDirection.Consumes);
          break;
        case ScenarioPlayerTagOperationType.Change:
          AddTag(node, output, "fromTag", "player-tag-change-from", node.FromTag, true, ScenarioRequirementDirection.Consumes);
          AddTag(node, output, "toTag", "player-tag-change-to", node.ToTag, true, ScenarioRequirementDirection.Produces);
          break;
        case ScenarioPlayerTagOperationType.Swap:
          AddTag(node, output, "swapTagA", "player-tag-swap-a-consume", node.SwapTagA, true, ScenarioRequirementDirection.Consumes);
          AddTag(node, output, "swapTagA", "player-tag-swap-a-produce", node.SwapTagA, true, ScenarioRequirementDirection.Produces);
          AddTag(node, output, "swapTagB", "player-tag-swap-b-consume", node.SwapTagB, true, ScenarioRequirementDirection.Consumes);
          AddTag(node, output, "swapTagB", "player-tag-swap-b-produce", node.SwapTagB, true, ScenarioRequirementDirection.Produces);
          break;
      }
      AddPlayerTagServices(node, output);
    }

    private static void ExtractPresetSpawn(ScenarioEntityPresetSpawnNode node, ScenarioRequirementBuilder output)
    {
      AddPreset(node, output, "presetIdentifier", node.PresetIdentifier, true, ScenarioRequirementAuthority.Any);
      AddPositionSource(node, output, node.PositionSourceEntityIdentifier);
      AddRuntimeEntityReferenceProducer(node, output, node.ResultStateKey, $"{node.Identifier}.spawnedEntityIdentifier", "spawned-entity-reference", ScenarioRequirementAuthority.Any);
    }

    private static void ExtractEntityTag(ScenarioEntityTagNode node, ScenarioRequirementBuilder output)
    {
      if (!string.IsNullOrWhiteSpace(node.TargetEntityIdentifier))
        AddEntity(node, output, "targetEntityIdentifier", "entity-tag-target", node.TargetEntityIdentifier, true, ScenarioRequirementCapability.RegisteredEntity);
      else
        output.Add(node, "targetEntityStateKey", "entity-tag-state-target", ScenarioRequirementKind.RuntimeEntityReference, node.TargetEntityStateKey, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scenario, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.RegisteredEntity);
    }

    private static void ExtractEntityInit(ScenarioEntityInitNode node, ScenarioRequirementBuilder output)
    {
      if (!string.IsNullOrWhiteSpace(node.PresetIdentifier))
      {
        var spawnRequiresDisplayTarget = node.StateOperations != null && node.StateOperations.Any(operation => operation != null && operation.Kind == ScenarioEntityStateOperationKind.DisplayState);
        AddPreset(node, output, "presetIdentifier", node.PresetIdentifier, true, ScenarioRequirementAuthority.Server);
        AddPositionSource(node, output, node.PositionSourceEntityIdentifier);
        AddRuntimeEntityReferenceProducer(node, output, node.ResultStateKey, $"{node.Identifier}.entityIdentifier", "entity-init-reference",
          ScenarioRequirementAuthority.Server,
          spawnRequiresDisplayTarget ? new[] { ScenarioRequirementCapability.ScenarioEntityInitTarget } : Array.Empty<ScenarioRequirementCapability>());
        return;
      }

      var hasDisplayState = node.StateOperations != null && node.StateOperations.Any(operation => operation != null && operation.Kind == ScenarioEntityStateOperationKind.DisplayState);
      var capabilities = hasDisplayState
        ? new[] { ScenarioRequirementCapability.RegisteredEntity, ScenarioRequirementCapability.ScenarioEntityInitTarget }
        : new[] { ScenarioRequirementCapability.RegisteredEntity };
      if (!string.IsNullOrWhiteSpace(node.TargetEntityIdentifier))
        AddEntity(node, output, "targetEntityIdentifier", "entity-init-target", node.TargetEntityIdentifier, true, capabilities);
      else if (!string.IsNullOrWhiteSpace(node.EntityIdentifier))
        AddEntity(node, output, "entityIdentifier", "entity-init-target", node.EntityIdentifier, true, capabilities);
      else
        output.Add(node, "targetEntityStateKey", "entity-init-state-target", ScenarioRequirementKind.RuntimeEntityReference, node.TargetEntityStateKey, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scenario, ScenarioRequirementDirection.Consumes, capabilities);
      AddRuntimeEntityReferenceProducer(node, output, node.ResultStateKey, $"{node.Identifier}.entityIdentifier", "entity-init-reference", ScenarioRequirementAuthority.Any);
    }

    private static void ExtractPatient(ScenarioPatientMedicalStatePresetNode node, ScenarioRequirementBuilder output)
    {
      if (!string.IsNullOrWhiteSpace(node.TargetEntityIdentifier))
        AddEntity(node, output, "targetEntityIdentifier", "patient-medical-state-target", node.TargetEntityIdentifier, true, ScenarioRequirementCapability.PatientMedicalStateTarget);
      else
        output.Add(node, "targetEntityStateKey", "patient-medical-state-reference", ScenarioRequirementKind.RuntimeEntityReference, node.TargetEntityStateKey, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scenario, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.PatientMedicalStateTarget);
    }

    private static void ExtractItemSubmission(ScenarioItemSubmissionConfigNode node, ScenarioRequirementBuilder output)
    {
      if (!string.IsNullOrWhiteSpace(node.PresetIdentifier))
      {
        AddPreset(node, output, "presetIdentifier", node.PresetIdentifier, true, ScenarioRequirementAuthority.Server);
        AddPositionSource(node, output, node.PositionSourceEntityIdentifier);
      }
      else if (!string.IsNullOrWhiteSpace(node.TargetIdentifier))
        output.Add(node, "targetIdentifier", "item-submission-target", ScenarioRequirementKind.Interactable, node.TargetIdentifier, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scene, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.ItemSubmissionTarget);
      else
        output.Add(node, "targetStateKey", "item-submission-state-target", ScenarioRequirementKind.RuntimeEntityReference, node.TargetStateKey, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scenario, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.ItemSubmissionTarget);

      if (node.RequiredItems != null)
      {
        for (var index = 0; index < node.RequiredItems.Count; index++)
        {
          var item = node.RequiredItems[index];
          if (item != null) output.Add(node, $"requiredItems[{index}].itemIdentifier", "item-submission-required-item", ScenarioRequirementKind.ItemDefinition, item.ItemIdentifier, false, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.External);
        }
      }
      var completionSignal = string.IsNullOrWhiteSpace(node.CompletionSignalIdentifier)
        ? node.CompletionSignalIdentifier
        : NormalizeRuntimeSignal(node.CompletionSignalIdentifier);
      output.Add(node, "completionSignalIdentifier", "item-submission-completion-signal", ScenarioRequirementKind.RuntimeSignal, completionSignal, false, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scenario, ScenarioRequirementDirection.Produces);
      AddRuntimeEntityReferenceProducer(node, output, node.ResultStateKey, $"{node.Identifier}.submissionEntityIdentifier", "item-submission-reference",
        string.IsNullOrWhiteSpace(node.PresetIdentifier) ? ScenarioRequirementAuthority.Any : ScenarioRequirementAuthority.Server,
        string.IsNullOrWhiteSpace(node.PresetIdentifier)
          ? Array.Empty<ScenarioRequirementCapability>()
          : new[] { ScenarioRequirementCapability.ItemSubmissionTarget });
    }

    private static void ExtractNpcInteract(ScenarioNpcInteractControlNode node, ScenarioRequirementBuilder output)
    {
      output.Add(node, "npcIdentifier", "npc-interact-target", ScenarioRequirementKind.Npc, node.NpcIdentifier, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scene, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.RegisteredNpcComponent);
      var availability = node.Operation == ScenarioNpcInteractControlOperation.Remove ? ScenarioRequirementAvailability.OptionalFallback : ScenarioRequirementAvailability.WhenNodeReached;
      var capability = node.Operation == ScenarioNpcInteractControlOperation.Enable || node.Operation == ScenarioNpcInteractControlOperation.Disable
        ? ScenarioRequirementCapability.ToggleableInteractable
        : ScenarioRequirementCapability.Interactable;
      output.Add(node, "interactableIdentifier", "npc-interactable", ScenarioRequirementKind.Interactable, node.InteractableIdentifier, true, availability, ScenarioRequirementExpectedSupply.Scene, ScenarioRequirementDirection.Consumes, capability);
    }

    private static void ExtractChat(ScenarioChatPrintNode node, ScenarioRequirementBuilder output)
    {
      if ((node.Targets & ScenarioChatPrintTarget.InGameChat) != 0 && node.Broadcast)
        output.AddService(node, "$service.chat", "chat-broadcast", ChatService, ScenarioRequirementAvailability.OptionalFallback);
    }

    private static void ExtractValidator(ScenarioValidatorNode node, ScenarioRequirementBuilder output)
    {
      var hasPlayerTag = false;
      if (node.RootConditions == null) return;
      for (var rootIndex = 0; rootIndex < node.RootConditions.Count; rootIndex++)
      {
        var root = node.RootConditions[rootIndex];
        if (root == null) continue;
        if (root.Condition == ScenarioValidatorCondition.PlayerAssignedTag)
        {
          hasPlayerTag = true;
          output.Add(node, $"rootConditions[{rootIndex}].playerTag", "validator-player-tag", ScenarioRequirementKind.PlayerTagState, root.PlayerTag, true, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.External);
          continue;
        }
        if (root.Condition != ScenarioValidatorCondition.RegistryContains || root.ValidationRules == null) continue;
        for (var ruleIndex = 0; ruleIndex < root.ValidationRules.Count; ruleIndex++)
        {
          var rule = root.ValidationRules[ruleIndex];
          if (rule == null || rule.Type != ScenarioValidatorRuleType.Registry || rule.Condition != ScenarioValidatorRuleCondition.Contains) continue;
          AddRegistryRule(node, output, $"rootConditions[{rootIndex}].validationRules[{ruleIndex}].registryIdentifier", rule);
        }
      }
      if (hasPlayerTag) AddPlayerTagServices(node, output);
    }

    private static void AddRegistryRule(IScenarioNode node, ScenarioRequirementBuilder output, string fieldPath, ScenarioValidatorRule rule)
    {
      var kind = ScenarioRequirementKind.RegistryEntry;
      var capabilities = Array.Empty<ScenarioRequirementCapability>();
      var supply = ScenarioRequirementExpectedSupply.Scene;
      var identifier = rule.RegistryIdentifier;
      var resolverDiscriminator = rule.RegistryType.ToString();
      switch (rule.RegistryType)
      {
        case RegistryType.Item: kind = ScenarioRequirementKind.ItemDefinition; supply = ScenarioRequirementExpectedSupply.External; break;
        case RegistryType.ScenarioEvent: kind = ScenarioRequirementKind.EventHandler; capabilities = new[] { ScenarioRequirementCapability.InvokableEventHandler }; break;
        case RegistryType.IconSprite: kind = ScenarioRequirementKind.SpriteResource; capabilities = new[] { ScenarioRequirementCapability.LoadableResource }; break;
        case RegistryType.Npc: kind = ScenarioRequirementKind.Npc; capabilities = new[] { ScenarioRequirementCapability.RegisteredNpcComponent }; break;
        case RegistryType.Waypoint: kind = ScenarioRequirementKind.SpatialAnchor; capabilities = new[] { ScenarioRequirementCapability.ProvidesPosition }; break;
        case RegistryType.SpawnPoint: kind = ScenarioRequirementKind.SpawnPoint; break;
        case RegistryType.Entity: kind = ScenarioRequirementKind.Entity; capabilities = new[] { ScenarioRequirementCapability.RegisteredEntity }; break;
        case RegistryType.RuntimeState:
          // RuntimeState is gameplay-produced state regardless of its naming
          // convention.  A missing `sig.` prefix must not turn it into a
          // static exactly-one registry requirement.
          kind = ScenarioRequirementKind.RuntimeSignal;
          supply = ScenarioRequirementExpectedSupply.Gameplay;
          break;
        case RegistryType.InteractableEntity: kind = ScenarioRequirementKind.Interactable; capabilities = new[] { ScenarioRequirementCapability.Interactable }; break;
        case RegistryType.EntityPreset: kind = ScenarioRequirementKind.EntityPreset; capabilities = new[] { ScenarioRequirementCapability.SpawnablePreset }; break;
      }
      output.AddWithResolver(node, fieldPath, "validator-registry-contains", kind, identifier, true,
        ScenarioRequirementAvailability.WhenNodeReached, supply, ScenarioRequirementDirection.Consumes,
        resolverDiscriminator, capabilities);
    }

    private static bool AddTags(IScenarioNode node, ScenarioRequirementBuilder output, IReadOnlyList<string> tags, string fieldPrefix, string usage)
    {
      if (tags == null) return false;
      var found = false;
      for (var index = 0; index < tags.Count; index++)
      {
        if (string.IsNullOrWhiteSpace(tags[index])) continue;
        found = true;
        output.Add(node, $"{fieldPrefix}[{index}]", usage, ScenarioRequirementKind.PlayerTagState, tags[index], false, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.External);
      }
      return found;
    }

    private static void AddTag(IScenarioNode node, ScenarioRequirementBuilder output, string field, string usage, string identifier, bool required, ScenarioRequirementDirection direction)
      => output.Add(node, field, usage, ScenarioRequirementKind.PlayerTagState, identifier, required, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.External, direction);

    private static void AddPlayerTagServices(IScenarioNode node, ScenarioRequirementBuilder output)
    {
      output.AddService(node, "$service.player-tag", "player-tag-service", PlayerTagService, ScenarioRequirementAvailability.WhenNodeReached);
      output.AddService(node, "$service.user-descriptor", "user-descriptor-service", UserDescriptorService, ScenarioRequirementAvailability.WhenNodeReached);
    }

    private static void AddResource(IScenarioNode node, ScenarioRequirementBuilder output, string field, string usage, ScenarioRequirementKind kind, string identifier, bool required, ScenarioRequirementAvailability availability)
      => output.Add(node, field, usage, kind, identifier, required, availability, ScenarioRequirementExpectedSupply.External, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.LoadableResource);

    private static void AddEvent(IScenarioNode node, ScenarioRequirementBuilder output, string field, string usage, string identifier, bool required)
      => output.Add(node, field, usage, ScenarioRequirementKind.EventHandler, identifier, required, ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scene, ScenarioRequirementDirection.Consumes, ScenarioRequirementCapability.InvokableEventHandler);

    private static void AddPreset(
      IScenarioNode node,
      ScenarioRequirementBuilder output,
      string field,
      string identifier,
      bool required,
      ScenarioRequirementAuthority authority)
    {
      output.AddWithAuthority(node, field, "entity-preset", ScenarioRequirementKind.EntityPreset, identifier, required,
        ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scene,
        ScenarioRequirementDirection.Consumes, authority, null, ScenarioRequirementCapability.SpawnablePreset);
    }

    private static void AddPositionSource(IScenarioNode node, ScenarioRequirementBuilder output, string identifier)
      => AddEntity(node, output, "positionSourceEntityIdentifier", "position-source", identifier, false, ScenarioRequirementCapability.ProvidesPosition);

    private static void AddEntity(IScenarioNode node, ScenarioRequirementBuilder output, string field, string usage, string identifier, bool required, params ScenarioRequirementCapability[] capabilities)
      => output.Add(node, field, usage, ScenarioRequirementKind.Entity, identifier, required, usage == "position-source" ? ScenarioRequirementAvailability.OptionalFallback : ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scene, ScenarioRequirementDirection.Consumes, capabilities);

    private static void AddRuntimeEntityReferenceProducer(
      IScenarioNode node,
      ScenarioRequirementBuilder output,
      string explicitStateKey,
      string defaultStateKey,
      string usage,
      ScenarioRequirementAuthority authority,
      params ScenarioRequirementCapability[] capabilities)
    {
      var stateKey = string.IsNullOrWhiteSpace(explicitStateKey) ? defaultStateKey : explicitStateKey;
      output.AddWithAuthority(node, string.IsNullOrWhiteSpace(explicitStateKey) ? "$defaultResultStateKey" : "resultStateKey",
        usage, ScenarioRequirementKind.RuntimeEntityReference, stateKey, true,
        ScenarioRequirementAvailability.WhenNodeReached, ScenarioRequirementExpectedSupply.Scenario,
        ScenarioRequirementDirection.Produces, authority, null, capabilities);
    }

    private static void AddRuntimeSignal(IScenarioNode node, ScenarioRequirementBuilder output, string fieldPath, string usage, string identifier, ScenarioRequirementDirection direction, ScenarioRequirementExpectedSupply supply)
      => output.Add(node, fieldPath, usage, ScenarioRequirementKind.RuntimeSignal, NormalizeRuntimeSignal(identifier), false, ScenarioRequirementAvailability.WhenNodeReached, supply, direction);

    private static string NormalizeRuntimeSignal(string identifier)
    {
      // Unregister 리스너 노드처럼 source/output 이 비어 있을 수 있다. null/공백은 그대로
      // 넘겨 Add 가 SIR100 진단 처리(required=false 시 무시)하도록 한다.
      if (string.IsNullOrWhiteSpace(identifier)) return identifier;
      var trimmed = identifier.Trim();
      return trimmed.StartsWith("sig.", StringComparison.Ordinal) ? trimmed : "sig." + trimmed;
    }
  }
}
