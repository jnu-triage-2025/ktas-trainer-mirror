using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using NUnit.Framework;
using TriageTrainer.Scenario;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class PatientAInteractionOrderingTests
  {
    [TearDown]
    public void TearDown()
    {
      PatientACriticalQuestStateFlags.Disarm();
    }

    [Test]
    public void AmbuBaggingStartIsOrderedFirstForPatientACritical()
    {
      PatientACriticalQuestStateFlags.ArmFor(PatientACriticalQuestStateFlags.ScenarioIdentifier);
      var interacts = new List<IInteract>
      {
        new FakeInteract("nearby-a"),
        new FakeInteract("ambu", PatientACriticalQuestStateFlags.GetInteractionDisplayPriority(
          "patient_a", "start_ambu_r1")),
        new FakeInteract("nearby-b")
      };

      PlayerController.OrderInteractsByDisplayPriority(interacts);

      Assert.That(interacts[0].DisplayText, Is.EqualTo("ambu"));
    }

    [Test]
    public void ClothingRemovalIsWithinFirstThreeForPatientACritical()
    {
      PatientACriticalQuestStateFlags.ArmFor(PatientACriticalQuestStateFlags.ScenarioIdentifier);
      var interacts = new List<IInteract>
      {
        new FakeInteract("nearby-a"),
        new FakeInteract("nearby-b"),
        new FakeInteract("nearby-c"),
        new FakeInteract("clothing", PatientACriticalQuestStateFlags.GetInteractionDisplayPriority(
          "patient_a", "remove_patient_clothing")),
        new FakeInteract("nearby-d")
      };

      PlayerController.OrderInteractsByDisplayPriority(interacts);

      Assert.That(interacts.FindIndex(interact => interact.DisplayText == "clothing"), Is.InRange(0, 2));
    }

    [Test]
    public void PatientAOrderingDoesNotApplyOutsidePatientACritical()
    {
      PatientACriticalQuestStateFlags.ArmFor("another_scenario");

      Assert.That(PatientACriticalQuestStateFlags.GetInteractionDisplayPriority(
        "patient_a", "start_ambu_r1"), Is.Zero);
      Assert.That(PatientACriticalQuestStateFlags.GetInteractionDisplayPriority(
        "patient_a", "remove_patient_clothing"), Is.Zero);
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
