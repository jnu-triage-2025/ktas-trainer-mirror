using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Scenario.Preflight
{
  /// <summary>
  /// 시나리오 노드 필드가 현재 runtime에서 어떤 종류의 값으로 사용되는지 나타낸다.
  /// Phase 1 canonical requirement 모델과 독립적인 runtime lookup 행렬용 분류다.
  /// </summary>
  public enum ScenarioRuntimeLookupClassification
  {
    External,
    RuntimeProduced,
    Internal,
    NotConsumed,
    SelfProvisioned
  }

  /// <summary>기존 preflight collector가 해당 노드의 일부 요구사항을 추출하는지 나타낸다.</summary>
  public enum ScenarioRequirementExtractionSupport
  {
    None,
    Legacy
  }

  /// <summary>하나의 node field에 대응하는 실제 runtime lookup 계약.</summary>
  public sealed class ScenarioNodeRuntimeLookup
  {
    public string FieldPath { get; }
    public string ResolverIdentifier { get; }
    public string Condition { get; }
    public ScenarioRuntimeLookupClassification Classification { get; }

    internal ScenarioNodeRuntimeLookup(
      string fieldPath,
      string resolverIdentifier,
      string condition,
      ScenarioRuntimeLookupClassification classification)
    {
      FieldPath = fieldPath ?? string.Empty;
      ResolverIdentifier = resolverIdentifier ?? string.Empty;
      Condition = condition ?? string.Empty;
      Classification = classification;
    }
  }

  /// <summary>한 node type의 CLR 타입, JSON discriminator와 runtime lookup 목록.</summary>
  public sealed class ScenarioNodeRuntimeLookupRegistration
  {
    private readonly ReadOnlyCollection<string> _serializedNodeTypeNames;
    private readonly ReadOnlyCollection<ScenarioNodeRuntimeLookup> _lookups;

    public ScenarioNodeType NodeType { get; }
    public Type RuntimeNodeType { get; }
    public IReadOnlyList<string> SerializedNodeTypeNames => _serializedNodeTypeNames;
    public IReadOnlyList<ScenarioNodeRuntimeLookup> Lookups => _lookups;
    public ScenarioRequirementExtractionSupport ExtractionSupport { get; }

    internal Action<IScenarioNode, ScenarioLegacyRequirementSink> LegacyCollector { get; }

    internal ScenarioNodeRuntimeLookupRegistration(
      ScenarioNodeType nodeType,
      Type runtimeNodeType,
      IEnumerable<string> serializedNodeTypeNames,
      IEnumerable<ScenarioNodeRuntimeLookup> lookups,
      Action<IScenarioNode, ScenarioLegacyRequirementSink> legacyCollector)
    {
      NodeType = nodeType;
      RuntimeNodeType = runtimeNodeType ?? throw new ArgumentNullException(nameof(runtimeNodeType));
      _serializedNodeTypeNames = Array.AsReadOnly((serializedNodeTypeNames ?? throw new ArgumentNullException(nameof(serializedNodeTypeNames))).ToArray());
      _lookups = Array.AsReadOnly((lookups ?? throw new ArgumentNullException(nameof(lookups))).ToArray());
      LegacyCollector = legacyCollector;
      ExtractionSupport = legacyCollector == null
        ? ScenarioRequirementExtractionSupport.None
        : ScenarioRequirementExtractionSupport.Legacy;
    }
  }

  internal sealed class ScenarioLegacyRequirementSink
  {
    private readonly List<ScenarioRequirement> _requirements;
    private readonly HashSet<string> _seen;

    public ScenarioLegacyRequirementSink(List<ScenarioRequirement> requirements, HashSet<string> seen)
    {
      _requirements = requirements ?? throw new ArgumentNullException(nameof(requirements));
      _seen = seen ?? throw new ArgumentNullException(nameof(seen));
    }

    public void Add(ScenarioRequirementKind kind, string identifier, string sourceNodeIdentifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
      {
        return;
      }

      var normalizedIdentifier = identifier.Trim();
      var dedupeKey = kind + "\u0001" + normalizedIdentifier;
      if (!_seen.Add(dedupeKey))
      {
        return;
      }

      _requirements.Add(new ScenarioRequirement(kind, normalizedIdentifier, sourceNodeIdentifier));
    }
  }

  /// <summary>
  /// 모든 <see cref="ScenarioNodeType"/>의 실제 runtime lookup 의미를 열거하는 immutable catalog.
  /// Scene이나 Registry를 조회하지 않으며 Phase 1 extractor와 validation rule의 등록 정본으로 사용한다.
  /// </summary>
  public static class ScenarioNodeRuntimeLookupRegistry
  {
    private static readonly ReadOnlyCollection<ScenarioNodeRuntimeLookupRegistration> Registrations;
    private static readonly IReadOnlyDictionary<ScenarioNodeType, ScenarioNodeRuntimeLookupRegistration> ByNodeType;

    static ScenarioNodeRuntimeLookupRegistry()
    {
      var registrations = CreateRegistrations();
      ValidateRegistrations(registrations);

      Registrations = Array.AsReadOnly(registrations);
      ByNodeType = new ReadOnlyDictionary<ScenarioNodeType, ScenarioNodeRuntimeLookupRegistration>(
        registrations.ToDictionary(registration => registration.NodeType));
    }

    public static IReadOnlyList<ScenarioNodeRuntimeLookupRegistration> All => Registrations;

    public static bool TryGet(
      ScenarioNodeType nodeType,
      out ScenarioNodeRuntimeLookupRegistration registration)
    {
      return ByNodeType.TryGetValue(nodeType, out registration);
    }

    public static ScenarioNodeRuntimeLookupRegistration Get(ScenarioNodeType nodeType)
    {
      if (!TryGet(nodeType, out var registration))
      {
        throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, "Unregistered scenario node type.");
      }

      return registration;
    }

    internal static void ValidateCanonicalExtractorCoverage(
      IReadOnlyDictionary<ScenarioNodeType, Type> extractorTypes)
    {
      if (extractorTypes == null) throw new ArgumentNullException(nameof(extractorTypes));

      var missing = ByNodeType.Keys.Except(extractorTypes.Keys).OrderBy(value => value).ToArray();
      var unexpected = extractorTypes.Keys.Except(ByNodeType.Keys).OrderBy(value => value).ToArray();
      var mismatches = ByNodeType.Keys.Intersect(extractorTypes.Keys)
        .Where(nodeType => ByNodeType[nodeType].RuntimeNodeType != extractorTypes[nodeType])
        .OrderBy(value => value)
        .Select(nodeType => $"{nodeType}: lookup={ByNodeType[nodeType].RuntimeNodeType.FullName}, extractor={extractorTypes[nodeType].FullName}")
        .ToArray();

      if (missing.Length == 0 && unexpected.Length == 0 && mismatches.Length == 0) return;

      throw new InvalidOperationException(
        $"Canonical scenario extractor catalog mismatch. Missing=[{string.Join(", ", missing)}], "
        + $"Unexpected=[{string.Join(", ", unexpected)}], CLR mismatches=[{string.Join("; ", mismatches)}].");
    }

    internal static void CollectLegacy(IScenarioNode node, ScenarioLegacyRequirementSink sink)
    {
      if (node == null)
      {
        return;
      }

      if (!TryGet(node.NodeType, out var registration))
      {
        return;
      }
      if (!registration.RuntimeNodeType.IsInstanceOfType(node))
      {
        return;
      }

      registration.LegacyCollector?.Invoke(node, sink);
    }

    private static ScenarioNodeRuntimeLookupRegistration[] CreateRegistrations()
    {
      return new[]
      {
        Register<ScenarioDialogueNode>(ScenarioNodeType.Dialogue, Rows(
          Row("portraitSpriteIdentifier", "Resources.Load<Sprite>", "non-empty", ScenarioRuntimeLookupClassification.External),
          Row("ttsVoiceIdentifier", "TTSService.NamedVoiceThenDefault", "playTTS && non-empty", ScenarioRuntimeLookupClassification.External),
          Row("playTTS", "ScenarioController.ResolveTTSService", "true", ScenarioRuntimeLookupClassification.SelfProvisioned))),

        Register<ScenarioChoiceNode>(ScenarioNodeType.Choice, Rows(
          Row("portraitSpriteIdentifier", "Resources.Load<Sprite>", "non-empty", ScenarioRuntimeLookupClassification.External),
          Row("options[].displayIconIdentifier", "Resources.Load<Sprite>", "non-empty", ScenarioRuntimeLookupClassification.External),
          Row("ttsVoiceIdentifier", "TTSService.NamedVoiceThenDefault", "playTTS && non-empty", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioSoundNode>(ScenarioNodeType.Sound, Rows(
          Row("soundResourceIdentifier", "Resources.Load<AudioClip>:SoundThenRoot", "always", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioPlayerMoveNode>(ScenarioNodeType.PlayerMove, Rows(
          Row("destinationIdentifier", "Registry:WaypointThenInteractableEntity<Vector3>", "destinationType=Waypoint", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioNPCMoveNode>(ScenarioNodeType.NPCMove, Rows(
          Row("npcIdentifier", "Registry:NpcThenEntity<GameObject>", "always", ScenarioRuntimeLookupClassification.External),
          Row("destinationIdentifier", "Registry:WaypointThenInteractableEntity<Vector3>", "destinationType=Waypoint", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioCameraTargetNode>(ScenarioNodeType.CameraTarget, Rows(
          Row("targetObjectIdentifier", "None", "current runtime does not resolve this field", ScenarioRuntimeLookupClassification.NotConsumed))),

        Register<ScenarioParallelNode>(ScenarioNodeType.Parallel, Rows(
          Row("branches[].requiredPlayerTags[]", "PlayerTagService+UserDescriptorService", "non-empty", ScenarioRuntimeLookupClassification.External),
          Row("branches[].forbiddenPlayerTags[]", "PlayerTagService+UserDescriptorService", "non-empty", ScenarioRuntimeLookupClassification.External),
          Row("branches[].identifier", "ScenarioGraph.Nodes", "always", ScenarioRuntimeLookupClassification.Internal),
          Row("branches[].completionConditionIdentifier", "ParallelBranchCompletion", "non-empty", ScenarioRuntimeLookupClassification.Internal))),

        Register<ScenarioInvokeEventNode>(ScenarioNodeType.InvokeEvent, Rows(
          Row("eventIdentifier", "ScenarioEventIdentifierRegistry.TryGetHandler", "non-empty; optional fallback", ScenarioRuntimeLookupClassification.External)), CollectInvokeEvent),

        Register<ScenarioServerInternalSignalNode>(ScenarioNodeType.ServerInternalSignal, Rows(
          Row("targetIdentifier", "ScenarioInteractionSignals", "always", ScenarioRuntimeLookupClassification.Internal),
          Row("signalIdentifier", "ScenarioInteractionSignals", "always", ScenarioRuntimeLookupClassification.Internal))),

        Register<ScenarioSignalListenerNode>(ScenarioNodeType.SignalListener, Rows(
          Row("sourceSignalIdentifier", "ScenarioInteractionSignals.OnSignalRegistered", "non-empty", ScenarioRuntimeLookupClassification.External),
          Row("requiredSignalIdentifiers[]", "Registry.Contains(RuntimeState)", "non-empty", ScenarioRuntimeLookupClassification.External),
          Row("outputSignalIdentifier", "ScenarioInteractionSignals.Raise", "non-empty", ScenarioRuntimeLookupClassification.Internal))),

        Register<ScenarioEntityStateSignalBindingNode>(ScenarioNodeType.EntityStateSignalBinding, Rows(
          Row("targetEntityIdentifier", "Registry.TryGetEntity", "operation=Register && non-empty", ScenarioRuntimeLookupClassification.External),
          Row("targetEntityStateKey", "ScenarioController.StateStore", "operation=Register && targetEntityIdentifier empty", ScenarioRuntimeLookupClassification.External),
          Row("eventName", "IScenarioEntityStateEventSource.RegisterStateEventListener", "operation=Register", ScenarioRuntimeLookupClassification.External),
          Row("outputSignalIdentifier", "ScenarioInteractionSignals.Raise", "non-empty", ScenarioRuntimeLookupClassification.Internal))),

        Register<ScenarioValidatorNode>(ScenarioNodeType.Validator, Rows(
          Row("rootConditions[].validationRules[].registryIdentifier", "Registry.Contains<rule.registryType>", "RegistryContains/Registry/Contains", ScenarioRuntimeLookupClassification.External),
          Row("rootConditions[].playerTag", "PlayerTagService+UserDescriptorService", "PlayerAssignedTag", ScenarioRuntimeLookupClassification.External)), CollectValidator),

        Register<ScenarioQuestControlNode>(ScenarioNodeType.QuestControl, Rows(
          Row("quest.id", "QuestManager.ActiveQuest", "operation=Remove or definition fallback", ScenarioRuntimeLookupClassification.External),
          Row("quest.definitionIdentifier", "QuestDefinitionRegistryThenResources", "operation=Add/Update && non-empty", ScenarioRuntimeLookupClassification.External),
          Row("questDefinitionIdentifier", "QuestDefinitionRegistryThenResources", "operation=Add/Update and selected by precedence", ScenarioRuntimeLookupClassification.External),
          Row("operation", "Registry:Service<QuestManager>", "always; missing service skips", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioQuestWaypointHighlightNode>(ScenarioNodeType.QuestWaypointHighlight, Rows(
          Row("waypointIdentifier", "WaypointAnchor.TryGet", "always", ScenarioRuntimeLookupClassification.External)), CollectWaypoint),

        Register<ScenarioDelayNode>(ScenarioNodeType.Delay, Rows()),

        Register<ScenarioInteractionNode>(ScenarioNodeType.Interaction, Rows(
          Row("targetIdentifier", "None", "current runtime does not resolve this field", ScenarioRuntimeLookupClassification.NotConsumed),
          Row("requiredItemIdentifier", "None", "current runtime does not resolve this field", ScenarioRuntimeLookupClassification.NotConsumed),
          Row("completionConditionIdentifier", "ScenarioEventIdentifierRegistry.TryGetHandler", "non-empty; optional fallback", ScenarioRuntimeLookupClassification.External)), CollectInteraction),

        Register<ScenarioCombineItemNode>(ScenarioNodeType.CombineItem, Rows(
          Row("inputItemIdentifiers[]", "None", "current runtime does not resolve these fields", ScenarioRuntimeLookupClassification.NotConsumed),
          Row("outputItemIdentifier", "ScenarioEventIdentifierRegistry.TryGetHandler", "!autoCombine; optional fallback", ScenarioRuntimeLookupClassification.External)), CollectCombineItem),

        Register<ScenarioQuizNode>(ScenarioNodeType.Quiz, Rows(
          Row("ttsVoiceIdentifier", "TTSService.NamedVoiceThenDefault", "playTTS && non-empty", ScenarioRuntimeLookupClassification.External),
          Row("$service.dialogue-ui", "Registry:UI<DialoguePanelUIController>", "always", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioStateUpdateNode>(ScenarioNodeType.StateUpdate, Rows(
          Row("targetEntityIdentifier", "ScenarioController.StateStoreKeyPrefix", "no entity lookup", ScenarioRuntimeLookupClassification.Internal),
          Row("stateKey", "ScenarioController.StateStore", "always", ScenarioRuntimeLookupClassification.RuntimeProduced))),

        Register<ScenarioPlayTTSNode>(ScenarioNodeType.PlayTTS, Rows(
          Row("transcriptIdentifier", "TTSService.TranscriptSegmentMap", "always", ScenarioRuntimeLookupClassification.External),
          Row("ttsVoiceIdentifier", "TTSService.NamedVoiceThenDefault", "non-empty", ScenarioRuntimeLookupClassification.External),
          Row("$service.tts", "ScenarioController.ResolveTTSService", "always", ScenarioRuntimeLookupClassification.SelfProvisioned))),

        Register<ScenarioPlayerTagNode>(ScenarioNodeType.PlayerTag, new[] { "PlayerTag", "TagModification" }, Rows(
          Row("tag", "PlayerTagService+UserDescriptorService", "operation=Add/Remove", ScenarioRuntimeLookupClassification.External),
          Row("fromTag", "PlayerTagService+UserDescriptorService", "operation=Change", ScenarioRuntimeLookupClassification.External),
          Row("toTag", "PlayerTagService", "operation=Change", ScenarioRuntimeLookupClassification.RuntimeProduced),
          Row("swapTagA", "PlayerTagService+UserDescriptorService", "operation=Swap", ScenarioRuntimeLookupClassification.External),
          Row("swapTagB", "PlayerTagService+UserDescriptorService", "operation=Swap", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioEntityPresetSpawnNode>(ScenarioNodeType.EntityPresetSpawn, Rows(
          Row("presetIdentifier", "Registry:EntityPreset<EntityPresetDefinition>", "always", ScenarioRuntimeLookupClassification.External),
          Row("positionSourceEntityIdentifier", "Registry:Entity<GameObject>", "non-empty; coordinates fallback", ScenarioRuntimeLookupClassification.External),
          Row("spawnedEntityIdentifier", "SpawnedEntityRegistration", "non-empty", ScenarioRuntimeLookupClassification.RuntimeProduced)), CollectEntityPresetSpawn),

        Register<ScenarioEntityTagNode>(ScenarioNodeType.EntityTag, Rows(
          Row("targetEntityIdentifier", "Registry:Entity<GameObject>", "direct target precedence", ScenarioRuntimeLookupClassification.External),
          Row("targetEntityStateKey", "ScenarioController.StateStoreThenRegistry:Entity<GameObject>", "direct target empty", ScenarioRuntimeLookupClassification.External),
          Row("tag/fromTag/toTag", "PlayerTagService", "operation-specific", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioEntityInitNode>(ScenarioNodeType.EntityInit, Rows(
          Row("presetIdentifier", "Registry:EntityPreset<EntityPresetDefinition>", "non-empty; spawn path", ScenarioRuntimeLookupClassification.External),
          Row("positionSourceEntityIdentifier", "Registry:Entity<GameObject>", "preset path && non-empty", ScenarioRuntimeLookupClassification.External),
          Row("targetEntityIdentifier", "Registry:Entity<GameObject>", "preset empty; first existing-target precedence", ScenarioRuntimeLookupClassification.External),
          Row("entityIdentifier", "Registry:Entity<GameObject>", "preset and direct target empty", ScenarioRuntimeLookupClassification.External),
          Row("targetEntityStateKey", "ScenarioController.StateStoreThenRegistry:Entity<GameObject>", "other target fields empty", ScenarioRuntimeLookupClassification.External),
          Row("stateOperations[DisplayState]", "IScenarioEntityInitTarget", "DisplayState operation", ScenarioRuntimeLookupClassification.External),
          Row("resultStateKey", "ScenarioController.StateStore", "non-empty", ScenarioRuntimeLookupClassification.RuntimeProduced)), CollectEntityInit),

        Register<ScenarioTriageAssessControlNode>(ScenarioNodeType.TriageAssessControl, Rows(
          Row("targetEntityIdentifier", "Registry:Entity<GameObject>ThenIScenarioTriageAssessTarget", "always", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioPatientMedicalStatePresetNode>(ScenarioNodeType.PatientMedicalStatePreset, Rows(
          Row("targetEntityIdentifier", "Registry:Entity<GameObject>ThenPatientController", "direct target precedence", ScenarioRuntimeLookupClassification.External),
          Row("targetEntityStateKey", "ScenarioController.StateStoreThenRegistry:Entity<GameObject>ThenPatientController", "direct target empty", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioItemSubmissionConfigNode>(ScenarioNodeType.ItemSubmissionConfig, Rows(
          Row("presetIdentifier", "Registry:EntityPreset<EntityPresetDefinition>", "non-empty; spawn path", ScenarioRuntimeLookupClassification.External),
          Row("positionSourceEntityIdentifier", "Registry:Entity<GameObject>", "preset path && non-empty", ScenarioRuntimeLookupClassification.External),
          Row("targetIdentifier", "Registry:InteractableEntity<ItemSubmissionInteractable>", "preset empty; direct target precedence", ScenarioRuntimeLookupClassification.External),
          Row("targetStateKey", "ScenarioController.StateStoreThenRegistry:InteractableEntity<ItemSubmissionInteractable>", "preset and direct target empty", ScenarioRuntimeLookupClassification.External),
          Row("requiredItems[].itemIdentifier", "ItemCatalog:Indeterminate", "non-empty; copied to runtime configuration", ScenarioRuntimeLookupClassification.External),
          Row("completionSignalIdentifier", "ScenarioInteractionSignals", "non-empty; deferred", ScenarioRuntimeLookupClassification.RuntimeProduced)), CollectItemSubmission),

        Register<ScenarioNpcInteractControlNode>(ScenarioNodeType.NpcInteractControl, Rows(
          Row("npcIdentifier", "Registry:Npc<GameObject>ThenNpcComponent", "always", ScenarioRuntimeLookupClassification.External),
          Row("interactableIdentifier", "Registry:InteractableEntity<MonoBehaviour>ThenEntityChild<IInteract>", "operation=Add", ScenarioRuntimeLookupClassification.External),
          Row("interactableIdentifier", "Registry:InteractableEntity<MonoBehaviour>ThenEntityChild<IInteract>", "operation=Remove; absence is silent", ScenarioRuntimeLookupClassification.External),
          Row("interactableIdentifier", "IInteractToggleable", "operation=Enable/Disable", ScenarioRuntimeLookupClassification.External)), CollectNpcInteractControl),

        Register<ScenarioChatPrintNode>(ScenarioNodeType.ChatPrint, Rows(
          Row("targets", "Registry:UI<ChatUIController>", "InGameChat && !broadcast; optional fallback", ScenarioRuntimeLookupClassification.External),
          Row("broadcast", "Registry:Service<ChatService>ThenLocalUI", "true; optional fallback", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioExecuteCommandNode>(ScenarioNodeType.ExecuteCommand, Rows(
          Row("commandLine", "OpaqueCommandLine", "identifiers are not statically parsed", ScenarioRuntimeLookupClassification.NotConsumed),
          Row("commandLine", "Registry:Service<ChatService>", "server/offline execution", ScenarioRuntimeLookupClassification.External))),

        Register<ScenarioTimeControlNode>(ScenarioNodeType.TimeControl, Rows(
          Row("timerId", "ScenarioTimeRelay", "operation-specific", ScenarioRuntimeLookupClassification.Internal))),

        Register<ScenarioDisinteractableDialogueNode>(ScenarioNodeType.DisinteractableDialogue, Rows(
          Row("portraitSpriteIdentifier", "Resources.Load<Sprite>", "non-empty", ScenarioRuntimeLookupClassification.External),
          Row("$service.dialogue-ui", "Registry:UI<DialoguePanelUIController>", "always; timing continues without UI", ScenarioRuntimeLookupClassification.External)))
      };
    }

    private static ScenarioNodeRuntimeLookupRegistration Register<TNode>(
      ScenarioNodeType nodeType,
      ScenarioNodeRuntimeLookup[] lookups,
      Action<TNode, ScenarioLegacyRequirementSink> legacyCollector = null)
      where TNode : class, IScenarioNode
    {
      return Register(nodeType, new[] { nodeType.ToString() }, lookups, legacyCollector);
    }

    private static ScenarioNodeRuntimeLookupRegistration Register<TNode>(
      ScenarioNodeType nodeType,
      string[] serializedNodeTypeNames,
      ScenarioNodeRuntimeLookup[] lookups,
      Action<TNode, ScenarioLegacyRequirementSink> legacyCollector = null)
      where TNode : class, IScenarioNode
    {
      Action<IScenarioNode, ScenarioLegacyRequirementSink> adapter = legacyCollector == null
        ? null
        : (node, sink) => legacyCollector((TNode)node, sink);

      return new ScenarioNodeRuntimeLookupRegistration(
        nodeType,
        typeof(TNode),
        serializedNodeTypeNames,
        lookups,
        adapter);
    }

    private static ScenarioNodeRuntimeLookup Row(
      string fieldPath,
      string resolverIdentifier,
      string condition,
      ScenarioRuntimeLookupClassification classification)
    {
      return new ScenarioNodeRuntimeLookup(fieldPath, resolverIdentifier, condition, classification);
    }

    private static ScenarioNodeRuntimeLookup[] Rows(params ScenarioNodeRuntimeLookup[] rows)
    {
      return rows;
    }

    private static void ValidateRegistrations(ScenarioNodeRuntimeLookupRegistration[] registrations)
    {
      var expectedNodeTypes = new HashSet<ScenarioNodeType>(
        (ScenarioNodeType[])Enum.GetValues(typeof(ScenarioNodeType)));
      var registeredNodeTypes = new HashSet<ScenarioNodeType>();
      var serializedNames = new HashSet<string>(StringComparer.Ordinal);

      foreach (var registration in registrations)
      {
        if (!registeredNodeTypes.Add(registration.NodeType))
        {
          throw new InvalidOperationException($"Duplicate runtime lookup registration for '{registration.NodeType}'.");
        }

        if (!typeof(IScenarioNode).IsAssignableFrom(registration.RuntimeNodeType))
        {
          throw new InvalidOperationException($"'{registration.RuntimeNodeType}' does not implement IScenarioNode.");
        }

        if (!(Activator.CreateInstance(registration.RuntimeNodeType) is IScenarioNode node)
            || node.NodeType != registration.NodeType)
        {
          throw new InvalidOperationException(
            $"CLR type '{registration.RuntimeNodeType}' does not report node type '{registration.NodeType}'.");
        }

        if (registration.SerializedNodeTypeNames.Count == 0)
        {
          throw new InvalidOperationException($"'{registration.NodeType}' has no JSON discriminator.");
        }

        foreach (var serializedName in registration.SerializedNodeTypeNames)
        {
          if (string.IsNullOrWhiteSpace(serializedName) || !serializedNames.Add(serializedName))
          {
            throw new InvalidOperationException($"Invalid or duplicate scenario JSON discriminator '{serializedName}'.");
          }
        }
      }

      if (!expectedNodeTypes.SetEquals(registeredNodeTypes))
      {
        var missing = expectedNodeTypes.Except(registeredNodeTypes).OrderBy(value => value).ToArray();
        var unexpected = registeredNodeTypes.Except(expectedNodeTypes).OrderBy(value => value).ToArray();
        throw new InvalidOperationException(
          $"Scenario runtime lookup catalog mismatch. Missing=[{string.Join(", ", missing)}], "
          + $"Unexpected=[{string.Join(", ", unexpected)}].");
      }

      if (serializedNames.Count != expectedNodeTypes.Count + 1
          || !serializedNames.Contains("PlayerTag")
          || !serializedNames.Contains("TagModification"))
      {
        throw new InvalidOperationException(
          "Scenario JSON discriminator catalog must contain every node type plus the TagModification alias.");
      }
    }

    private static void CollectInteraction(ScenarioInteractionNode node, ScenarioLegacyRequirementSink sink)
    {
      sink.Add(ScenarioRequirementKind.InteractionTarget, node.TargetIdentifier, node.Identifier);
      sink.Add(ScenarioRequirementKind.EventHandler, node.CompletionConditionIdentifier, node.Identifier);
    }

    private static void CollectInvokeEvent(ScenarioInvokeEventNode node, ScenarioLegacyRequirementSink sink)
    {
      sink.Add(ScenarioRequirementKind.EventHandler, node.EventIdentifier, node.Identifier);
    }

    private static void CollectWaypoint(ScenarioQuestWaypointHighlightNode node, ScenarioLegacyRequirementSink sink)
    {
      sink.Add(ScenarioRequirementKind.Waypoint, node.WaypointIdentifier, node.Identifier);
    }

    private static void CollectEntityPresetSpawn(ScenarioEntityPresetSpawnNode node, ScenarioLegacyRequirementSink sink)
    {
      sink.Add(ScenarioRequirementKind.EntityPreset, node.PresetIdentifier, node.Identifier);
    }

    private static void CollectEntityInit(ScenarioEntityInitNode node, ScenarioLegacyRequirementSink sink)
    {
      sink.Add(ScenarioRequirementKind.EntityPreset, node.PresetIdentifier, node.Identifier);
      sink.Add(ScenarioRequirementKind.InteractionTarget, node.TargetEntityIdentifier, node.Identifier);
    }

    private static void CollectCombineItem(ScenarioCombineItemNode node, ScenarioLegacyRequirementSink sink)
    {
      if (node.InputItemIdentifiers != null)
      {
        foreach (var identifier in node.InputItemIdentifiers)
        {
          sink.Add(ScenarioRequirementKind.Item, identifier, node.Identifier);
        }
      }

      sink.Add(ScenarioRequirementKind.Item, node.OutputItemIdentifier, node.Identifier);
      if (!node.AutoCombine)
      {
        sink.Add(ScenarioRequirementKind.EventHandler, node.OutputItemIdentifier, node.Identifier);
      }
    }

    private static void CollectValidator(ScenarioValidatorNode node, ScenarioLegacyRequirementSink sink)
    {
      if (node.RootConditions == null)
      {
        return;
      }

      foreach (var rootCondition in node.RootConditions)
      {
        if (rootCondition == null
            || rootCondition.Condition != ScenarioValidatorCondition.RegistryContains
            || rootCondition.ValidationRules == null)
        {
          continue;
        }

        foreach (var rule in rootCondition.ValidationRules)
        {
          if (rule != null && rule.RegistryType == RegistryType.RuntimeState)
          {
            sink.Add(ScenarioRequirementKind.Signal, rule.RegistryIdentifier, node.Identifier);
          }
        }
      }
    }

    private static void CollectItemSubmission(ScenarioItemSubmissionConfigNode node, ScenarioLegacyRequirementSink sink)
    {
      sink.Add(ScenarioRequirementKind.EntityPreset, node.PresetIdentifier, node.Identifier);
      sink.Add(ScenarioRequirementKind.InteractionTarget, node.TargetIdentifier, node.Identifier);

      if (node.RequiredItems == null)
      {
        return;
      }

      foreach (var requirement in node.RequiredItems)
      {
        if (requirement != null)
        {
          sink.Add(ScenarioRequirementKind.Item, requirement.ItemIdentifier, node.Identifier);
        }
      }
    }

    private static void CollectNpcInteractControl(ScenarioNpcInteractControlNode node, ScenarioLegacyRequirementSink sink)
    {
      sink.Add(ScenarioRequirementKind.InteractionTarget, node.NpcIdentifier, node.Identifier);
      sink.Add(ScenarioRequirementKind.InteractionTarget, node.InteractableIdentifier, node.Identifier);
    }
  }
}
