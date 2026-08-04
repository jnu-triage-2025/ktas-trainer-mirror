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
    public void PatientBCScenarioLoadsWithSchemaAndCompletesAfterCTTransport()
    {
      string path = Path.Combine(
        Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);

      Assert.That(graph.DefaultEntrypoint, Is.EqualTo("SPAWN_B"));
      Assert.That(graph.Nodes, Has.Count.EqualTo(284));
      Assert.That(graph.ActingNpcs, Has.Count.EqualTo(1));
      Assert.That(graph.ActingNpcs.Single().Identifier, Is.EqualTo("npc-doctor-patient-b-c-ct"));
      Assert.That(graph.ActingNpcs.Single().PresetIdentifier, Is.EqualTo("npc_doctor_preset"));

      var doctorSpawn = graph.Nodes["SPAWN_DOCTOR"] as ScenarioEntityPresetSpawnNode;
      Assert.That(doctorSpawn, Is.Not.Null);
      Assert.That(doctorSpawn.ActingNpcIdentifier, Is.EqualTo("npc-doctor-patient-b-c-ct"));
      Assert.That(doctorSpawn.PositionSourceEntityIdentifier, Is.EqualTo("scen_b:doctor_spawnpoint"));

      var doctorMove = graph.Nodes["MOVE_DOCTOR_TO_CARE_AREA"] as ScenarioNPCControlNode;
      Assert.That(doctorMove, Is.Not.Null);
      Assert.That(doctorMove.Mode, Is.EqualTo(ScenarioNPCControlMode.Control));
      Assert.That(doctorMove.NPCIdentifier, Is.EqualTo("npc-doctor-patient-b-c-ct"));
      Assert.That(doctorMove.DestinationType, Is.EqualTo(ScenarioMoveDestinationType.Waypoint));
      Assert.That(doctorMove.DestinationIdentifier, Is.EqualTo("scen_b:doctor_care_area_waypoint"));
      Assert.That(graph.Nodes["P_MOVE"].NextIdentifier, Is.EqualTo("MOVE_DOCTOR_TO_CARE_AREA"));
      Assert.That(graph.Nodes.ContainsKey("B_COMPLETE"), Is.True);
      Assert.That(graph.Nodes["B_COMPLETE"].NextIdentifier, Is.EqualTo("C_ARRIVAL"));
      Assert.That(graph.Nodes["C_ARRIVAL"].NextIdentifier, Is.EqualTo("C_DOC_C"));
      Assert.That(graph.Nodes["C_DOC_C"].NextIdentifier, Is.EqualTo("C_DOC_D"));
      Assert.That(graph.Nodes["C_DOC_D"].NextIdentifier, Is.EqualTo("P_C_CARE"));
      Assert.That(graph.Nodes.ContainsKey("P_C_CARE"), Is.True);
      Assert.That(graph.Nodes.ContainsKey("C_COMPLETE"), Is.True);
      Assert.That(graph.Nodes["C_COMPLETE"].NextIdentifier, Is.EqualTo("CT_DELAY"));
      Assert.That(graph.Nodes.ContainsKey("P_CT_TRANSPORT"), Is.True);
      Assert.That(graph.Nodes["P_CT_TRANSPORT"].NextIdentifier, Is.EqualTo("CT_SCENARIO_COMPLETE"));
      Assert.That(graph.Nodes["CT_SCENARIO_COMPLETE"].NextIdentifier, Is.Null);

      foreach (string nodeIdentifier in new[] { "CT_A_WAIT", "CT_B_WAIT", "CT_C_WAIT", "CT_D_WAIT" })
      {
        var transportWait = graph.Nodes[nodeIdentifier] as ScenarioValidatorNode;
        Assert.That(transportWait, Is.Not.Null, nodeIdentifier);
        var signalIdentifiers = transportWait.RootConditions.Single().ValidationRules
          .Select(rule => rule.RegistryIdentifier);
        Assert.That(signalIdentifiers, Is.EquivalentTo(new[]
        {
          "sig.ct_patient_arrived_patient_b",
          "sig.ct_patient_arrived_patient_c"
        }));
      }
      Assert.That(graph.Nodes.Values.OfType<ScenarioChoiceNode>()
        .Count(value => !string.IsNullOrWhiteSpace(value.AssessmentIdentifier)), Is.EqualTo(18));

      var nurseArrivalCounter = graph.Nodes["COUNT_NURSE_ARRIVAL"] as ScenarioSignalCounterNode;
      Assert.That(nurseArrivalCounter, Is.Not.Null);
      Assert.That(nurseArrivalCounter.CounterIdentifier, Is.EqualTo("scen_b_nurse_arrivals"));
      Assert.That(nurseArrivalCounter.SourceSignalPrefix, Is.EqualTo("quest_arrival_triage_area_"));
      Assert.That(nurseArrivalCounter.Threshold, Is.EqualTo(4));
      Assert.That(nurseArrivalCounter.OutputSignalIdentifier, Is.EqualTo("all_nurses_arrived_triage"));

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

      var patientCVitalOpen = graph.Nodes["C_B_VITAL_OPEN"] as ScenarioInvokeEventNode;
      Assert.That(patientCVitalOpen, Is.Not.Null);
      Assert.That(patientCVitalOpen.EventIdentifier, Is.EqualTo("activate_vital_monitor_ui_patient_c"));
      Assert.That(patientCVitalOpen.InvokeOnRoleClient, Is.True);

      var patientCLeftGrade = graph.Nodes["C_A_LEFT_GRADE"] as ScenarioChoiceNode;
      var patientCRightGrade = graph.Nodes["C_A_RIGHT_GRADE"] as ScenarioChoiceNode;
      Assert.That(patientCLeftGrade?.CorrectOptionIndex, Is.EqualTo(0));
      Assert.That(patientCRightGrade?.CorrectOptionIndex, Is.EqualTo(2));

      var patientCIvWait = graph.Nodes["C_C_IV_WAIT"] as ScenarioValidatorNode;
      Assert.That(patientCIvWait, Is.Not.Null);
      Assert.That(patientCIvWait.RootConditions.Single().ValidationRules.Single().RegistryIdentifier,
        Is.EqualTo("sig.insert_iv_patient_c_left"));
    }

    [Test]
    public void PatientCPrefabSupportsLeftArmIntravenousLine()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBFemale.prefab");
      Assert.That(prefab, Is.Not.Null);

      var patient = prefab.GetComponent<PatientController>();
      Assert.That(patient, Is.Not.Null);
      Assert.That(patient.IntravenousLineCannulaSupported, Is.True);

      var patientState = prefab.GetComponentInChildren<PatientTypeBFemaleState>(true);
      Assert.That(patientState, Is.Not.Null);
      Assert.That(patientState.TreatmentDisplayState.DisplaySupports.Syringe20GInsertedIntoLeftArm, Is.True);
      Assert.That(patientState.TreatmentDisplayState.DisplaySupports.Syringe20GInsertedIntoRightArm, Is.False);

      var instance = Object.Instantiate(prefab);
      try
      {
        var runtimePatient = instance.GetComponent<PatientController>();
        var runtimeState = instance.GetComponentInChildren<PatientTypeBFemaleState>(true);
        runtimePatient.SetTreatmentDisplayNetworked(
          PatientController.TreatmentDisplay.Syringe20GInsertedIntoLeftArm,
          true);
        Assert.That(runtimeState.TreatmentDisplayState.DisplayState.Syringe20GInsertedIntoLeftArm, Is.True);
      }
      finally
      {
        Object.DestroyImmediate(instance);
      }
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
