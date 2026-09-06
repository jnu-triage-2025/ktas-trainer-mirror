using System.Collections.Generic;
using System.IO;
using System.Linq;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace TriageTrainer.Tests
{
  /// <summary>
  /// 표시 우선순위는 시나리오 데이터의 display.priority 가 정한다. 코드 리터럴은 우선순위를 갖지 않으므로
  /// 시나리오가 끝나 데이터 정의가 해제되면 감지 순서로 돌아간다.
  /// </summary>
  public sealed class PatientAInteractionOrderingTests
  {
    private const string PatientAScenarioPath =
      "Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json";

    [TestCase(false)]
    [TestCase(true)]
    public void PatientInteractionsIncludeScenarioHandlersBeforeAndAfterSpawn(bool definitionsBeforeSpawn)
    {
      const string entity = "patient-interaction-regression";
      const string scenario = "patient-interaction-regression-scenario";
      var instance = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBMale.prefab"));
      try
      {
        var patient = instance.GetComponent<TriageTrainer.Entity.PatientController>();
        typeof(TriageTrainer.Entity.PatientController).GetField("_identifier",
          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(patient, entity);
        var action = InteractionDefinition.Code(entity, "scenario_action", "action", true, InteractionKind.Action);
        var recognition = InteractionDefinition.Code(entity, "recognition_1", "recognition", true);
        recognition.HandlerKey = "recognition_check";
        recognition.CompletionSignal = "patient_b_recognition_1";
        if (!definitionsBeforeSpawn)
          MultiplayerInfrastructure.Registry.Registry.RegisterEntity(entity,
            MultiplayerInfrastructure.Registry.EntityType.Patient, instance);
        using (InteractionRegistry.BeginScenarioInitCycle(scenario))
          InteractionRegistry.ApplyScenarioDefinitions(scenario, new[] { action, recognition });
        if (definitionsBeforeSpawn)
        {
          Assert.That(InteractionRegistry.TryGet(new InteractionAddress(entity, "recognition_1"), out _), Is.False);
          MultiplayerInfrastructure.Registry.Registry.RegisterEntity(entity,
            MultiplayerInfrastructure.Registry.EntityType.Patient, instance);
        }
        foreach (string id in new[] { "scenario_action", "recognition_1" })
        {
          Assert.That(InteractionRegistry.TryGet(new InteractionAddress(entity, id), out var entry), Is.True);
          Assert.That(entry.Handler, Is.Not.Null);
          Assert.That(patient.Interacts.Count(each => ReferenceEquals(each, entry.Handler)), Is.EqualTo(1));
        }
        Assert.That(patient.Interacts.Any(each => each is IInteractionRegistryExempt), Is.True);
        InteractionRegistry.ClearScenarioDefinitions(scenario);
        Assert.That(patient.Interacts.OfType<IQuestPresentationTarget>().Any(each =>
          each.InteractionIdentifier == "scenario_action" || each.InteractionIdentifier == "recognition_1"), Is.False);
      }
      finally
      {
        InteractionRegistry.ClearScenarioDefinitions(scenario);
        MultiplayerInfrastructure.Registry.Registry.UnregisterEntity(entity);
        Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void AmbuBaggingStartIsOrderedFirstForPatientACritical()
    {
      var interacts = new List<IInteract>
      {
        new FakeInteract("nearby-a"),
        new FakeInteract("ambu", DataDisplayPriority("start_ambu_r1")),
        new FakeInteract("nearby-b")
      };

      PlayerController.OrderInteractsByDisplayPriority(interacts);

      Assert.That(interacts[0].DisplayText, Is.EqualTo("ambu"));
    }

    [Test]
    public void ClothingRemovalIsWithinFirstThreeForPatientACritical()
    {
      var interacts = new List<IInteract>
      {
        new FakeInteract("nearby-a"),
        new FakeInteract("nearby-b"),
        new FakeInteract("nearby-c"),
        new FakeInteract("clothing", DataDisplayPriority("remove_patient_clothing")),
        new FakeInteract("nearby-d")
      };

      PlayerController.OrderInteractsByDisplayPriority(interacts);

      Assert.That(interacts.FindIndex(interact => interact.DisplayText == "clothing"), Is.InRange(0, 2));
    }

    [Test]
    public void DisplayPriorityComesFromScenarioDataNotFromCode()
    {
      Assert.That(DataDisplayPriority("start_ambu_r1"), Is.GreaterThan(DataDisplayPriority("remove_patient_clothing")));
      Assert.That(DataDisplayPriority("remove_patient_clothing"), Is.GreaterThan(0));

      var code = InteractionDefinition.Code("patient_a", "start_ambu_r1", "앰부배깅 시작");
      Assert.That(code.Display.PrioritySpecified, Is.False, "코드 리터럴은 표시 우선순위를 지정하지 않습니다.");
      Assert.That(code.Display.Priority, Is.Zero);
    }

    [Test]
    public void EqualPriorityPreservesDetectionOrder()
    {
      var interacts = new List<IInteract>
      {
        new FakeInteract("first"),
        new FakeInteract("second"),
        new FakeInteract("third")
      };

      PlayerController.OrderInteractsByDisplayPriority(interacts);

      Assert.That(interacts.ConvertAll(interact => interact.DisplayText),
        Is.EqualTo(new[] { "first", "second", "third" }));
    }

    private static int DataDisplayPriority(string interactionIdentifier)
    {
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(PatientAScenarioPath), validateWithSchema: true);
      var definition = graph.Interactions.FirstOrDefault(each =>
        each?.Entity != null && each.Entity.Identifier == "patient_a"
        && each.InteractionIdentifier == interactionIdentifier);
      Assert.That(definition, Is.Not.Null, interactionIdentifier);
      Assert.That(definition.Display.PrioritySpecified, Is.True,
        $"'{interactionIdentifier}'의 표시 우선순위는 시나리오 데이터가 지정해야 합니다.");
      return definition.Display.Priority;
    }

    private sealed class FakeInteract : IInteract, IInteractDisplayPriority
    {
      public FakeInteract(string displayText, int priority = 0)
      {
        DisplayText = displayText;
        DisplayPriority = priority;
      }

      public string DisplayText { get; }
      public int DisplayPriority { get; }
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public void Interact(Transform interactor) { }
    }
  }
}
