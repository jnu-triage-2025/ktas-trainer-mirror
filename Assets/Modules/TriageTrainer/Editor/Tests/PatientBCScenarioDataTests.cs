using System.IO;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Patient;
using UnityEditor;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class PatientBCScenarioDataTests
  {
    [Test]
    public void PatientBCScenarioLoadsWithSchemaAndStopsAfterPatientB()
    {
      string path = Path.Combine(
        Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);

      Assert.That(graph.DefaultEntrypoint, Is.EqualTo("SPAWN_B"));
      Assert.That(graph.Nodes, Has.Count.EqualTo(162));
      Assert.That(graph.Nodes.ContainsKey("B_COMPLETE"), Is.True);
      Assert.That(graph.Nodes.Keys.Any(value => value.Contains("CT_")), Is.False);
      Assert.That(graph.Nodes.Values.OfType<ScenarioChoiceNode>()
        .Count(value => !string.IsNullOrWhiteSpace(value.AssessmentIdentifier)), Is.EqualTo(9));

      foreach (string nodeIdentifier in new[]
               {
                 "BIND_TRIAGE_B_CORRECT",
                 "BIND_TRIAGE_C_CORRECT",
                 "BIND_TRIAGE_D_CORRECT"
               })
      {
        var binding = graph.Nodes[nodeIdentifier] as ScenarioEntityStateSignalBindingNode;
        Assert.That(binding, Is.Not.Null, nodeIdentifier);
        Assert.That(binding.ConsumeOnce, Is.False, $"{nodeIdentifier} must survive a retry reset");
      }

      var vitalOpen = graph.Nodes["B_VITAL_OPEN"] as ScenarioInvokeEventNode;
      Assert.That(vitalOpen, Is.Not.Null);
      Assert.That(vitalOpen.InvokeOnRoleClient, Is.True);
    }

    [Test]
    public void PatientBPrefabSupportsRightArmIntravenousLine()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBMale.prefab");
      Assert.That(prefab, Is.Not.Null);

      var patient = prefab.GetComponent<PatientController>();
      Assert.That(patient, Is.Not.Null);
      Assert.That(patient.IntravenousLineCannulaSupported, Is.True);

      var patientState = prefab.GetComponentInChildren<PatientTypeBMaleState>(true);
      Assert.That(patientState, Is.Not.Null);
      Assert.That(patientState.TreatmentDisplayState.DisplaySupports.Syringe20GInsertedIntoRightArm, Is.True);
      Assert.That(patientState.TreatmentDisplayState.DisplaySupports.Syringe20GInsertedIntoLeftArm, Is.False);

      var instance = Object.Instantiate(prefab);
      try
      {
        var runtimePatient = instance.GetComponent<PatientController>();
        var runtimeState = instance.GetComponentInChildren<PatientTypeBMaleState>(true);
        runtimePatient.SetTreatmentDisplayNetworked(
          PatientController.TreatmentDisplay.Syringe20GInsertedIntoRightArm,
          true);
        Assert.That(runtimeState.TreatmentDisplayState.DisplayState.Syringe20GInsertedIntoRightArm, Is.True);
      }
      finally
      {
        Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void RecognitionRulesRejectConfigurationWithoutAnyInputPath()
    {
      try
      {
        Assert.That(ScenarioGameRules.TrySetUseMicInRecognitionCheck(true, out _), Is.True);
        Assert.That(ScenarioGameRules.TrySetDisableInteractionInRecognitionCheck(true, out _), Is.True);
        Assert.That(ScenarioGameRules.TrySetUseMicInRecognitionCheck(false, out var error), Is.False);
        Assert.That(error, Does.Contain("cannot be false"));
        Assert.That(ScenarioGameRules.UseMicInRecognitionCheck, Is.True);
      }
      finally
      {
        ScenarioGameRules.TrySetDisableInteractionInRecognitionCheck(false, out _);
        ScenarioGameRules.TrySetUseMicInRecognitionCheck(false, out _);
      }
    }

    [Test]
    public void TagTextSelectorUsesLiteralFallbackWhenTagIsMissing()
    {
      Assert.That(
        ScenarioTextResolver.Resolve("@t=[definitely_missing_tag, ???]선생님"),
        Is.EqualTo("???선생님"));
    }
  }
}
