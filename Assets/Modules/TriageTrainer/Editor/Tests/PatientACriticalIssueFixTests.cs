using System;
using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Patient;
using TriageTrainer.Scenario;
using UnityEditor;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class PatientACriticalIssueFixTests
  {
    private const string PatientAPrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeA.prefab";
    private const string RapidInfuserPrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/level1_rapid_infuser.prefab";

    [Test]
    public void PatientAInstantiationAppliesInitialTreatmentDisplayState()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientAPrefabPath);
      Assert.That(prefab, Is.Not.Null);

      var instance = UnityEngine.Object.Instantiate(prefab);
      try
      {
        // EditMode에서 단순 Instantiate는 런타임 Awake 호출을 보장하지 않으므로,
        // Awake가 사용하는 두 초기화 루틴을 직접 실행해 결과를 검증한다.
        var controller = instance.GetComponent<PatientController>();
        Assert.That(controller, Is.Not.Null);
        typeof(PatientController).GetMethod(
          "InitializeTreatmentDisplaysFromConfiguredState",
          BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, null);
        var hiddenNames = (string[])typeof(PatientController).GetField(
          "_initiallyHiddenChildNames",
          BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller);
        typeof(PatientController).GetMethod(
          "SetNamedChildrenActive",
          BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, new object[] { hiddenNames, false });

        var patientState = instance.GetComponent<PatientStateABC>();
        Assert.That(patientState, Is.Not.Null);

        var treatmentState = patientState.TreatmentDisplayState;
        var flags = treatmentState.DisplayState;
        var children = treatmentState.ChildGameObjects;

        foreach (var flagField in typeof(PatientTreatmentDisplayModel).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
          if ((bool)flagField.GetValue(flags))
            continue;

          var childField = typeof(PatientTreatmentDisplayingChildGameObjects).GetField(flagField.Name);
          var child = childField?.GetValue(children) as GameObject;
          if (child != null)
            Assert.That(child.activeSelf, Is.False, $"{flagField.Name} must start hidden.");
        }

        foreach (var stagedName in new[]
                 {
                   "defibpad_midaxillary_A",
                   "defibpad_subclavicle_A",
                   "epinephrine_5cc_syringe",
                   "normal_saline_5cc_syringe"
                 })
        {
          var staged = Array.Find(
            instance.GetComponentsInChildren<Transform>(true),
            child => child != null && child.name == stagedName);
          Assert.That(staged, Is.Not.Null, $"Missing staged child {stagedName}.");
          Assert.That(staged.gameObject.activeSelf, Is.False, $"{stagedName} must start hidden.");
        }
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(instance);
      }
    }

    [TestCase("PlasmaSolution", "sig.connect_ps1_to_lv1")]
    [TestCase("BloodTransfusionSet", "sig.connect_blood_to_lv1")]
    public void RapidInfuserCompletionRaisesScenarioSignal(string kindName, string expectedSignal)
    {
      var raised = new List<string>();
      void Capture(string signal) => raised.Add(signal);
      ScenarioInteractionSignals.OnSignalRegistered += Capture;
      try
      {
        var kindType = typeof(Level1RapidInfuserController).GetNestedType("FluidKind", BindingFlags.NonPublic);
        var raiseMethod = typeof(Level1RapidInfuserController).GetMethod(
          "RaiseScenarioConnectionSignal",
          BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(kindType, Is.Not.Null);
        Assert.That(raiseMethod, Is.Not.Null);
        raiseMethod.Invoke(null, new[] { Enum.Parse(kindType, kindName) });

        Assert.That(raised, Does.Contain(expectedSignal));
      }
      finally
      {
        ScenarioInteractionSignals.OnSignalRegistered -= Capture;
        foreach (var signal in raised)
          ScenarioInteractionSignals.Clear(signal);
      }
    }

    [TestCase("PlasmaSolution", "plasma_solution_1000ml")]
    [TestCase("BloodTransfusionSet", "blood_transfusion_set")]
    public void RapidInfuserAcceptsScenarioItems(string kindName, string itemIdentifier)
    {
      var kindType = typeof(Level1RapidInfuserController).GetNestedType("FluidKind", BindingFlags.NonPublic);
      var familyMethod = typeof(Level1RapidInfuserController).GetMethod(
        "IsFluidFamily",
        BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(kindType, Is.Not.Null);
      Assert.That(familyMethod, Is.Not.Null);
      bool accepted = (bool)familyMethod.Invoke(null, new[] { itemIdentifier, Enum.Parse(kindType, kindName) });
      Assert.That(accepted, Is.True);
    }

    [Test]
    public void RapidInfuserRejectsBloodBagWhenScenarioRequiresTransfusionSet()
    {
      Assert.That(IsRapidInfuserItemAccepted("BloodTransfusionSet", "blood_bag"), Is.False);
    }

    [Test]
    public void RapidInfuserRequiresPlasmaBeforeBloodTransfusionSet()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RapidInfuserPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = UnityEngine.Object.Instantiate(prefab);
      try
      {
        var controller = instance.GetComponent<Level1RapidInfuserController>();
        Assert.That(controller, Is.Not.Null);
        var canAdd = typeof(Level1RapidInfuserController).GetMethod(
          "CanAddFluid", BindingFlags.Instance | BindingFlags.NonPublic);
        var kind = GetRapidInfuserKind("BloodTransfusionSet");

        Assert.That((bool)canAdd.Invoke(controller, new[] { kind }), Is.False);
        typeof(Level1RapidInfuserController).GetField(
            "_initialHasPlasmaSolution", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(controller, true);
        Assert.That((bool)canAdd.Invoke(controller, new[] { kind }), Is.True);
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void RapidInfuserCaptureStateIncludesBloodTransfusionSet()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RapidInfuserPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = UnityEngine.Object.Instantiate(prefab);
      try
      {
        var controller = instance.GetComponent<Level1RapidInfuserController>();
        typeof(Level1RapidInfuserController).GetField(
            "_initialHasBloodTransfusionSet", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(controller, true);

        Assert.That(controller.CaptureState().HasBloodTransfusionSet, Is.True);
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void ResuscitationMedicationSignalsAreScopedToPatientAAndCurrentRound()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientAPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = UnityEngine.Object.Instantiate(prefab);
      try
      {
        var controller = instance.GetComponent<PatientController>();
        var apply = typeof(PatientController).GetMethod(
          "ApplyItemUse", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(apply, Is.Not.Null);

        ScenarioInteractionSignals.Clear("push_epi_r1");
        ScenarioInteractionSignals.Clear("push_epi_r2");
        Assert.That((bool)apply.Invoke(controller, new object[] { "epinephrine_5cc_syringe" }), Is.True);
        Assert.That(ScenarioInteractionSignals.IsRaised("push_epi_r1"), Is.True);
        Assert.That(ScenarioInteractionSignals.IsRaised("push_epi_r2"), Is.False);

        ScenarioInteractionSignals.Clear("push_epi_r1");
        controller.SetResuscitationMedicationRound(2);
        Assert.That((bool)apply.Invoke(controller, new object[] { "epinephrine_5cc_syringe" }), Is.True);
        Assert.That(ScenarioInteractionSignals.IsRaised("push_epi_r2"), Is.True);

        ScenarioInteractionSignals.Clear("push_epi_r2");
        typeof(PatientController).GetField("_identifier", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(controller, "patient_b");
        Assert.That((bool)apply.Invoke(controller, new object[] { "epinephrine_5cc_syringe" }), Is.False);
        Assert.That(ScenarioInteractionSignals.IsRaised("push_epi_r2"), Is.False);
      }
      finally
      {
        ScenarioInteractionSignals.Clear("push_epi_r1");
        ScenarioInteractionSignals.Clear("push_epi_r2");
        UnityEngine.Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void SecondChestCompressionActionRequiresFirstRoundCompletion()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientAPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = UnityEngine.Object.Instantiate(prefab);
      var interactor = new GameObject("chest-compression-interactor");
      try
      {
        interactor.AddComponent<MultiplayerInfrastructure.Player.PlayerController>();
        var secondRoundAction = System.Array.Find(
          instance.GetComponentsInChildren<ScenarioActionInteractable>(true),
          action => action != null && action.CompletionSignal == "interact_chest");
        Assert.That(secondRoundAction, Is.Not.Null);

        ScenarioInteractionSignals.Clear("click_to_start_comp");
        Assert.That(secondRoundAction.CanInteract(interactor.transform), Is.False);

        ScenarioInteractionSignals.Raise("click_to_start_comp");
        Assert.That(secondRoundAction.CanInteract(interactor.transform), Is.True);
      }
      finally
      {
        ScenarioInteractionSignals.Clear("click_to_start_comp");
        UnityEngine.Object.DestroyImmediate(interactor);
        UnityEngine.Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void RoscGcsAssessmentStartsHidden()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientAPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = UnityEngine.Object.Instantiate(prefab);
      try
      {
        var controller = instance.GetComponent<PatientController>();
        Assert.That(controller, Is.Not.Null);
        var getAssessAction = typeof(PatientController).GetMethod(
          "GetAssessAction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(getAssessAction, Is.Not.Null);
        var action = getAssessAction.Invoke(controller, new object[] { "assess_gcs_rosc" })
          as PatientController.AssessActionConfig;
        Assert.That(action, Is.Not.Null);
        Assert.That(action.Enabled, Is.False);
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(instance);
      }
    }

    private static bool IsRapidInfuserItemAccepted(string kindName, string itemIdentifier)
    {
      var familyMethod = typeof(Level1RapidInfuserController).GetMethod(
        "IsFluidFamily", BindingFlags.Static | BindingFlags.NonPublic);
      return (bool)familyMethod.Invoke(null, new[] { itemIdentifier, GetRapidInfuserKind(kindName) });
    }

    private static object GetRapidInfuserKind(string kindName)
    {
      var kindType = typeof(Level1RapidInfuserController).GetNestedType("FluidKind", BindingFlags.NonPublic);
      Assert.That(kindType, Is.Not.Null);
      return Enum.Parse(kindType, kindName);
    }
  }
}
