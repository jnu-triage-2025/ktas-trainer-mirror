using System.IO;
using System.Linq;
using System.Text.Json;
using System.Reflection;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Registry;
using NUnit.Framework;
using System.Collections.Generic;
using TriageTrainer.Entity;
using TriageTrainer.Entity.PatientMonitor.Models;
using TriageTrainer.Editor.Utils;
using TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects;
using TriageTrainer.Patient;
using TriageTrainer.Scenario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace TriageTrainer.Tests
{
  public sealed class PatientBCScenarioDataTests
  {
    private const string PatientDummyDBPrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeDDummyB.prefab";
    private const string PatientDummyDAPrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeDDummyA.prefab";

    [Test]
    public void PatientVitalMonitorResolvesFromSharedCareZoneWithoutSceneReference()
    {
      var root = new GameObject("PatientBCMonitorResolutionTest");
      root.SetActive(false);
      try
      {
        var zoneObject = new GameObject("CareZone");
        zoneObject.transform.SetParent(root.transform);
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        zone.ConfigureArea(Vector3.zero, new Vector3(10f, 10f, 10f));

        var patientObject = new GameObject("patient_b");
        patientObject.transform.SetParent(root.transform);
        var patient = patientObject.AddComponent<PatientController>();

        var monitorObject = new GameObject("CareZoneMonitor");
        monitorObject.transform.SetParent(root.transform);
        monitorObject.transform.localPosition = Vector3.right;
        monitorObject.SetActive(false);
        var monitor = monitorObject.AddComponent<SinglePatientMonitorController>();

        root.SetActive(true);
        var findMonitor = typeof(TriageScenarioEventBootstrap).GetMethod(
          "FindPatientVitalMonitor",
          BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(findMonitor, Is.Not.Null);
        Assert.That(findMonitor.Invoke(null, new object[] { patient }), Is.SameAs(monitor));
      }
      finally
      {
        Object.DestroyImmediate(root);
      }
    }

    [TestCase("close_vital_ui_b")]
    [TestCase("close_vital_ui_c")]
    public void VitalMonitorCloseRaisesExpectedSignalOnlyOnce(string completionSignal)
    {
      var bootstrapObject = new GameObject("TriageScenarioEventBootstrapTest");
      bootstrapObject.SetActive(false);
      var monitorObject = new GameObject("PatientMonitorTest");
      monitorObject.SetActive(false);
      try
      {
        var bootstrap = bootstrapObject.AddComponent<TriageScenarioEventBootstrap>();
        var monitor = monitorObject.AddComponent<SinglePatientMonitorController>();
        var patientObject = new GameObject(completionSignal.EndsWith("_b") ? "patient_b" : "patient_c");
        patientObject.SetActive(false);
        var patient = patientObject.AddComponent<PatientController>();
        typeof(PatientController).GetField("_identifier", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(patient, completionSignal.EndsWith("_b") ? "patient_b" : "patient_c");
        monitor.SetPresentationPatient(patient);
        var configure = typeof(TriageScenarioEventBootstrap).GetMethod(
          "ConfigureVitalMonitorClose",
          BindingFlags.Instance | BindingFlags.NonPublic);
        var requestClose = typeof(PatientMonitorController).GetMethod(
          "RequestClose",
          BindingFlags.Instance | BindingFlags.NonPublic);
        int signalCount = 0;
        void Capture(string signal)
        {
          if (signal == "sig." + completionSignal)
            signalCount++;
        }

        ScenarioInteractionSignals.OnSignalRegistered += Capture;
        try
        {
          Assert.That(configure, Is.Not.Null);
          Assert.That(requestClose, Is.Not.Null);
          configure.Invoke(bootstrap, new object[] { monitor, patient, monitorObject, null, completionSignal });
          requestClose.Invoke(monitor, null);
          requestClose.Invoke(monitor, null);
          Assert.That(signalCount, Is.EqualTo(1));
        }
        finally
        {
          ScenarioInteractionSignals.OnSignalRegistered -= Capture;
          ScenarioInteractionSignals.Clear(completionSignal);
          Object.DestroyImmediate(patientObject);
        }
      }
      finally
      {
        Object.DestroyImmediate(monitorObject);
        Object.DestroyImmediate(bootstrapObject);
      }
    }

    [Test]
    public void DedicatedServerVitalMonitorCloseAcceptsActivePatientCareZoneWithoutMonitoringPatient()
    {
      var zoneObject = new GameObject("dedicated-server-care-zone");
      var patientObject = new GameObject("patient_b");
      var monitorObject = new GameObject("patient-b-monitor");
      LogAssert.ignoreFailingMessages = true;
      try
      {
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        zone.ConfigureArea(Vector3.zero, new Vector3(10f, 10f, 10f));
        var patient = patientObject.AddComponent<PatientController>();
        SetPrivateField(patient, "_identifier", "patient_b");
        var monitor = monitorObject.AddComponent<SinglePatientMonitorController>();
        SetPrivateField(zone, "_activePatient", patient);

        Assert.That(monitor.MonitoringPatient, Is.Null,
          "a dedicated server does not execute client-local presentation binding");
        Assert.That(IsAuthoritativeCareZoneMonitorForPatient(monitor, patient), Is.True);
      }
      finally
      {
        LogAssert.ignoreFailingMessages = false;
        Object.DestroyImmediate(monitorObject);
        Object.DestroyImmediate(patientObject);
        Object.DestroyImmediate(zoneObject);
      }
    }

    [Test]
    public void DedicatedServerVitalMonitorCloseRejectsWrongMonitorAndPatientCareZone()
    {
      var zoneBObject = new GameObject("patient-b-zone");
      var zoneCObject = new GameObject("patient-c-zone");
      var patientBObject = new GameObject("patient_b");
      var patientCObject = new GameObject("patient_c");
      var monitorBObject = new GameObject("patient-b-monitor");
      var monitorCObject = new GameObject("patient-c-monitor");
      LogAssert.ignoreFailingMessages = true;
      try
      {
        var zoneB = zoneBObject.AddComponent<PatientCareDescriptionZone>();
        zoneB.ConfigureArea(Vector3.zero, new Vector3(4f, 4f, 4f));
        var zoneC = zoneCObject.AddComponent<PatientCareDescriptionZone>();
        zoneCObject.transform.position = Vector3.right * 10f;
        zoneC.ConfigureArea(Vector3.zero, new Vector3(4f, 4f, 4f));

        var patientB = patientBObject.AddComponent<PatientController>();
        SetPrivateField(patientB, "_identifier", "patient_b");
        var patientC = patientCObject.AddComponent<PatientController>();
        SetPrivateField(patientC, "_identifier", "patient_c");
        patientCObject.transform.position = zoneCObject.transform.position;
        var monitorB = monitorBObject.AddComponent<SinglePatientMonitorController>();
        var monitorC = monitorCObject.AddComponent<SinglePatientMonitorController>();
        monitorCObject.transform.position = zoneCObject.transform.position;
        SetPrivateField(zoneB, "_activePatient", patientB);
        SetPrivateField(zoneC, "_activePatient", patientC);

        Assert.That(IsAuthoritativeCareZoneMonitorForPatient(monitorB, patientB), Is.True);
        Assert.That(IsAuthoritativeCareZoneMonitorForPatient(monitorC, patientC), Is.True);
        Assert.That(IsAuthoritativeCareZoneMonitorForPatient(monitorB, patientC),
          Is.False, "patient B's monitor must not close patient C");
        Assert.That(IsAuthoritativeCareZoneMonitorForPatient(monitorC, patientB),
          Is.False, "patient C's monitor must not close patient B");
      }
      finally
      {
        LogAssert.ignoreFailingMessages = false;
        Object.DestroyImmediate(monitorCObject);
        Object.DestroyImmediate(monitorBObject);
        Object.DestroyImmediate(patientCObject);
        Object.DestroyImmediate(patientBObject);
        Object.DestroyImmediate(zoneCObject);
        Object.DestroyImmediate(zoneBObject);
      }
    }

    private const string OverworldScenePath = "Assets/Scenes/OverworldScene.unity";
    private const string StaticLayoutPath =
      "Assets/Modules/TriageTrainer/ScriptableObjects/StaticEntityLayouts/OverworldPatientSupports.asset";
    private const string MovingBedPrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/PatientMovingBed.prefab";

    [Test]
    public void PatientBCScenarioLoadsWithSchemaAndCompletesAfterCTTransport()
    {
      string path = Path.Combine(
        Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);

       Assert.That(graph.DefaultEntrypoint, Is.EqualTo("SPAWN_B"));
       Assert.That(graph.Nodes, Has.Count.EqualTo(310));
       Assert.That(graph.ClientSignalPrefixes, Is.EqualTo(new[] { "sig.quest_arrival_triage_area_" }));
      Assert.That(graph.ActingNpcs, Has.Count.EqualTo(1));
      Assert.That(graph.ActingNpcs.Single().Identifier, Is.EqualTo("npc-doctor-patient-b-c-ct"));
      Assert.That(graph.ActingNpcs.Single().PresetIdentifier, Is.EqualTo("npc_doctor_preset"));

      var patientBSpawn = graph.Nodes["SPAWN_B"] as ScenarioEntityPresetSpawnNode;
      var patientCSpawn = graph.Nodes["SPAWN_C"] as ScenarioEntityPresetSpawnNode;
      Assert.That(patientBSpawn, Is.Not.Null);
      Assert.That(patientCSpawn, Is.Not.Null);
      Assert.That(patientBSpawn.RotationY, Is.EqualTo(-90f));
       Assert.That(patientCSpawn.RotationY, Is.EqualTo(-90f));
       var attachSpawnedBeds = graph.Nodes["ATTACH_SPAWNED_PATIENT_BEDS"] as ScenarioInvokeEventNode;
       Assert.That(attachSpawnedBeds, Is.Not.Null);
       Assert.That(attachSpawnedBeds.EventIdentifier, Is.EqualTo("attach_patient_bed_pairs"));
       Assert.That(attachSpawnedBeds.NextIdentifier, Is.EqualTo("SPAWN_DOCTOR"));
       Assert.That(patientCSpawn.NextIdentifier, Is.EqualTo("SPAWN_DUMMY"));
       Assert.That((graph.Nodes["SPAWN_DUMMY"] as ScenarioEntityPresetSpawnNode)?.NextIdentifier,
         Is.EqualTo("ATTACH_SPAWNED_PATIENT_BEDS"));

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
      Assert.That(graph.Nodes["P_B_CARE"].NextIdentifier, Is.EqualTo("P_B_WAIT_REMOVE"));
      Assert.That(graph.Nodes["P_B_WAIT_REMOVE"].NextIdentifier, Is.EqualTo("B_COMPLETE"));
      Assert.That(graph.Nodes["P_C_CARE"].NextIdentifier, Is.EqualTo("P_C_WAIT_REMOVE"));
      Assert.That(graph.Nodes["P_C_WAIT_REMOVE"].NextIdentifier, Is.EqualTo("C_COMPLETE"));
      Assert.That(graph.Nodes["C_COMPLETE"].NextIdentifier, Is.EqualTo("CT_DELAY"));
      Assert.That(graph.Nodes.ContainsKey("P_CT_TRANSPORT"), Is.True);
      Assert.That(graph.Nodes["P_CT_TRANSPORT"].NextIdentifier, Is.EqualTo("CT_FINAL_DELAY"));
      var finalDelay = graph.Nodes["CT_FINAL_DELAY"] as ScenarioDelayNode;
      Assert.That(finalDelay, Is.Not.Null);
      Assert.That(finalDelay.Duration.ToSeconds(), Is.EqualTo(1d));
      Assert.That(finalDelay.NextIdentifier, Is.EqualTo("CT_SCENARIO_COMPLETE"));
      Assert.That(graph.Nodes["CT_SCENARIO_COMPLETE"].NextIdentifier, Is.Null);

      var announcementAndArrival = graph.Nodes["P_ANNOUNCE_ARRIVAL"] as ScenarioParallelNode;
      Assert.That(announcementAndArrival, Is.Not.Null);
      Assert.That(announcementAndArrival.AllocationType, Is.EqualTo(ScenarioParallelAllocationType.SelfAll));
      // 도착 퀘스트는 안내 방송이 끝난 뒤에만 발행되어야 한다. 그렇지 않으면 방송 스킵과
      // 다음 병렬 흐름의 퀘스트 표시가 같은 UI 상태를 동시에 갱신한다.
      Assert.That(announcementAndArrival.WaitMode, Is.EqualTo(ScenarioWaitMode.All));
      Assert.That(announcementAndArrival.Branches.Select(branch => branch.Identifier),
        Is.EqualTo(new[] { "ANNOUNCE", "P_ARRIVAL" }));
       Assert.That(graph.Nodes["COUNT_NURSE_ARRIVAL"].NextIdentifier, Is.EqualTo("P_ANNOUNCE_ARRIVAL"));
      Assert.That(graph.Nodes["P_ANNOUNCE_ARRIVAL"].NextIdentifier, Is.EqualTo("ARRIVAL_QUEST_COMPLETION_DELAY"));
      Assert.That(graph.Nodes["ANNOUNCE"].NextIdentifier, Is.EqualTo("CC_ANNOUNCE"));
      Assert.That(graph.Nodes["P_ARRIVAL"].NextIdentifier, Is.EqualTo("CC_ARRIVAL"));
       var arrivalQuestCompletionDelay = graph.Nodes["ARRIVAL_QUEST_COMPLETION_DELAY"] as ScenarioDelayNode;
       Assert.That(arrivalQuestCompletionDelay, Is.Not.Null);
       Assert.That(arrivalQuestCompletionDelay.Duration.ToSeconds(), Is.EqualTo(0.25d));
       Assert.That(arrivalQuestCompletionDelay.NextIdentifier, Is.EqualTo("P_TRIAGE"));

      foreach (string role in new[] { "A", "B", "C", "D" })
      {
        var arrivalQuest = graph.Nodes[$"ARR_{role}_Q"] as ScenarioQuestControlNode;
        var arrivalWait = graph.Nodes[$"ARR_{role}_WAIT"] as ScenarioValidatorNode;
        Assert.That(arrivalQuest, Is.Not.Null, role);
        Assert.That(arrivalQuest.Operation, Is.EqualTo(ScenarioQuestOperationType.Add), role);
        Assert.That(arrivalQuest.QuestDefinitionIdentifier, Is.EqualTo("Quest_Arrive_Triage"), role);
        Assert.That(arrivalQuest.NextIdentifier, Is.EqualTo($"ARR_{role}_WAIT"), role);
        Assert.That(arrivalWait, Is.Not.Null, role);
        Assert.That(arrivalWait.WaitForCondition, Is.True, role);
        Assert.That(arrivalWait.RootConditions.Single().ValidationRules.Single().RegistryIdentifier,
          Is.EqualTo("sig.all_nurses_arrived_triage"), role);
      }

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
      Assert.That(nurseArrivalCounter.UseActiveRoleRosterThreshold, Is.True,
        "도착 완료는 고정 역할 수가 아니라 현재 활성 플레이어의 도착 신호를 기준으로 해야 한다.");
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

      Assert.That(graph.Nodes["TRIAGE_WAIT_ALL"].NextIdentifier, Is.EqualTo("TRIAGE_EVALUATE_CURRENT"));
      foreach (var expectation in new[]
               {
                 (Node: "TRIAGE_EVALUATE_CURRENT", Signal: "sig.triage_correct_patient_b", Wrong: "TRIAGE_WRONG"),
                 (Node: "TRIAGE_C_EVALUATE_CURRENT", Signal: "sig.triage_correct_patient_c", Wrong: "TRIAGE_RESET_REMOVE"),
                 (Node: "TRIAGE_REENABLE_D", Signal: "sig.triage_correct_patient_dummy_d_b", Wrong: "TRIAGE_REENABLE_B")
               })
      {
        var evaluation = graph.Nodes[expectation.Node] as ScenarioValidatorNode;
        Assert.That(evaluation, Is.Not.Null, expectation.Node);
        Assert.That(evaluation.RootConditions.Single().ValidationRules.Single().RegistryIdentifier,
          Is.EqualTo(expectation.Signal), expectation.Node);
        Assert.That(evaluation.FailureNextIdentifier, Is.EqualTo(expectation.Wrong), expectation.Node);
      }
      Assert.That((graph.Nodes["TRIAGE_RESET"] as ScenarioInvokeEventNode)?.EventIdentifier,
        Is.EqualTo("reset_patient_b_triage_attempt"));
      Assert.That((graph.Nodes["TRIAGE_RESET_ADD"] as ScenarioInvokeEventNode)?.EventIdentifier,
        Is.EqualTo("reset_patient_c_triage_attempt"));
      Assert.That((graph.Nodes["TRIAGE_REENABLE_C"] as ScenarioInvokeEventNode)?.EventIdentifier,
        Is.EqualTo("reset_patient_dummy_d_b_triage_attempt"));
      Assert.That(graph.Nodes["TRIAGE_CHECK_CORRECT"].NextIdentifier,
        Is.EqualTo("TRIAGE_C_EVALUATE_CURRENT"));
      Assert.That(graph.Nodes["TRIAGE_C_EVALUATE_CURRENT"].NextIdentifier,
        Is.EqualTo("TRIAGE_A_REMOVE"));
      Assert.That(graph.Nodes["TRIAGE_A_REMOVE"].NextIdentifier,
        Is.EqualTo("TRIAGE_REENABLE_D"));
      Assert.That(graph.Nodes["TRIAGE_REENABLE_D"].NextIdentifier,
        Is.EqualTo("TRIAGE_COMPLETE_EVENT"));
      Assert.That(graph.Nodes["TRIAGE_COMPLETE_EVENT"].NextIdentifier,
        Is.EqualTo("TRIAGE_A_FINAL_REMOVE"));
      Assert.That(graph.Nodes["TRIAGE_A_FINAL_REMOVE"].NextIdentifier,
        Is.EqualTo("CC_TRIAGE_A"));
      var triageCorrectCheck = graph.Nodes["TRIAGE_CHECK_CORRECT"] as ScenarioValidatorNode;
      Assert.That(triageCorrectCheck, Is.Not.Null);
      Assert.That(triageCorrectCheck.RootConditions.Single().ValidationRules
        .Select(rule => rule.RegistryIdentifier), Is.EqualTo(new[]
      {
        "sig.triage_submitted_patient_c"
      }));

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

      Assert.That((graph.Nodes["A_STRENGTH_TRANSITION"] as ScenarioDialogueNode)?.DialogueContent,
        Is.EqualTo("이제 근력을 확인해 보자."));
      Assert.That((graph.Nodes["C_A_STRENGTH_TRANSITION"] as ScenarioDialogueNode)?.DialogueContent,
        Is.EqualTo("이제 근력을 확인해 보자."));
      Assert.That((graph.Nodes["CT_DOCTOR_B_SUMMARY"] as ScenarioDialogueNode)?.DialogueContent,
        Does.Contain("근력은 우측 5점, 좌측 3점"));

      foreach (var expectation in new[]
               {
                 (Node: "A_STRENGTH_WAIT_UPDATE", Quest: "Quest_B_Strength", Definition: "Quest_B_Wait"),
                 (Node: "B_VITAL_WAIT_UPDATE", Quest: "Quest_B_Vital", Definition: "Quest_B_Wait"),
                 (Node: "C_PUPIL_WAIT_UPDATE", Quest: "Quest_B_Pupil_IV", Definition: "Quest_B_Wait"),
                 (Node: "D_WAIT_UPDATE", Quest: "Quest_B_Oxygen_Bleeding", Definition: "Quest_B_Wait"),
                 (Node: "C_A_STRENGTH_WAIT_UPDATE", Quest: "Quest_C_Strength", Definition: "Quest_C_Wait"),
                 (Node: "C_B_VITAL_WAIT_UPDATE", Quest: "Quest_C_Vital", Definition: "Quest_C_Wait"),
                 (Node: "C_C_PUPIL_WAIT_UPDATE", Quest: "Quest_C_Pupil_IV", Definition: "Quest_C_Wait"),
                 (Node: "C_D_WAIT_UPDATE", Quest: "Quest_C_Oxygen_Bleeding", Definition: "Quest_C_Wait")
               })
      {
        var update = graph.Nodes[expectation.Node] as ScenarioQuestControlNode;
        Assert.That(update, Is.Not.Null, expectation.Node);
        Assert.That(update.Operation, Is.EqualTo(ScenarioQuestOperationType.Update), expectation.Node);
        Assert.That(update.Quest?.Id, Is.EqualTo(expectation.Quest), expectation.Node);
        Assert.That(update.Quest?.DefinitionIdentifier, Is.EqualTo(expectation.Definition), expectation.Node);
        Assert.That(update.NextIdentifier, Does.StartWith("CC_"), expectation.Node);
      }

      string questPath = Path.Combine(
        Application.dataPath,
        "Modules/TriageTrainer/Resources/Quest/patient_b_c_ct.quests.quest.json");
      using var questDocument = JsonDocument.Parse(File.ReadAllText(questPath));
      var questDefinitions = questDocument.RootElement.GetProperty("definitions")
        .EnumerateArray()
        .ToDictionary(definition => definition.GetProperty("identifier").GetString());
      Assert.That(questDefinitions, Has.Count.EqualTo(19));
      foreach (string waitDefinition in new[] { "Quest_B_Wait", "Quest_C_Wait" })
      {
        Assert.That(questDefinitions.ContainsKey(waitDefinition), Is.True, waitDefinition);
        Assert.That(questDefinitions[waitDefinition].GetProperty("questContent").GetString(),
          Is.EqualTo("다른 담당자들이 처치를 마칠 때까지 기다리기"), waitDefinition);
        Assert.That(questDefinitions[waitDefinition].GetProperty("tasks").GetArrayLength(),
          Is.Zero, waitDefinition);
      }

      var patientCIvWait = graph.Nodes["C_C_IV_WAIT"] as ScenarioValidatorNode;
      Assert.That(patientCIvWait, Is.Not.Null);
      Assert.That(patientCIvWait.RootConditions.Single().ValidationRules.Single().RegistryIdentifier,
        Is.EqualTo("sig.insert_iv_patient_c_left"));

      foreach (var expectation in new[]
               {
                 (Node: "C_TREATMENT_ACTIVATE", Event: "activate_patient_b_nurse_c_treatment"),
                 (Node: "D_TREATMENT_ACTIVATE", Event: "activate_patient_b_nurse_d_treatment"),
                 (Node: "C_C_TREATMENT_ACTIVATE", Event: "activate_patient_c_nurse_c_treatment"),
                 (Node: "C_D_TREATMENT_ACTIVATE", Event: "activate_patient_c_nurse_d_treatment")
               })
      {
        var activation = graph.Nodes[expectation.Node] as ScenarioInvokeEventNode;
        Assert.That(activation, Is.Not.Null, expectation.Node);
        Assert.That(activation.EventIdentifier, Is.EqualTo(expectation.Event), expectation.Node);
      }
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
      Assert.That(patientState.TreatmentDisplayState.DisplaySupports.GauzePatchedOnRightArm, Is.True);
      Assert.That(patientState.TreatmentDisplayState.DisplaySupports.GauzeDressingDoneOnRightArm, Is.True);

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
      Assert.That(patientState.TreatmentDisplayState.DisplaySupports.GauzePatchedOnLeftArm, Is.True);
      Assert.That(patientState.TreatmentDisplayState.DisplaySupports.GauzeDressingDoneOnLeftArm, Is.True);

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
    public void PatientDummyDBPrefabIsFunctionalTriagePatientAndFishNetSpawnable()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientDummyDBPrefabPath);
      var presetRegistry = AssetDatabase.LoadAssetAtPath<EntityPresetRegistryRequirementsSO>(
        "Assets/Modules/TriageTrainer/ScriptableObjects/EntityPreset Registry Requirements SO.asset");
      var fishNetRegistry = AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>(
        "Assets/DefaultPrefabObjects.asset");

      Assert.That(prefab, Is.Not.Null);
      Assert.That(presetRegistry, Is.Not.Null);
      Assert.That(fishNetRegistry, Is.Not.Null);

      var patient = prefab.GetComponent<PatientController>();
      var networkObject = prefab.GetComponent<NetworkObject>();
      var patientState = prefab.GetComponent<PatientDummyDState>();
      Assert.That(patient, Is.Not.Null);
      Assert.That(prefab.GetComponent<CapsuleCollider>(), Is.Not.Null);
      Assert.That(networkObject, Is.Not.Null);
      Assert.That(patientState, Is.Not.Null);
      Assert.That(patientState.TreatmentDisplayState.PatientModelGameObject, Is.Null);
      Assert.That(patientState.TreatmentDisplayState.DisplaySupports,
        Is.EqualTo(new PatientTreatmentDisplayModel()));
      Assert.That(patient.IntravenousLineCannulaSupported, Is.False);

      Assert.That(networkObject.NetworkBehaviours, Has.Count.EqualTo(1));
      Assert.That(networkObject.NetworkBehaviours.Single(), Is.SameAs(patient));
      Assert.That(networkObject.PrefabId, Is.Not.EqualTo(NetworkObject.UNSET_PREFABID_VALUE));
      Assert.That(fishNetRegistry.Prefabs, Does.Contain(networkObject));

      var requirement = presetRegistry.entityPresetRegistryRequirements.Single(
        value => value.identifier == "patient_dummy_d_b");
       Assert.That(requirement.prefab, Is.SameAs(prefab));
       Assert.That(requirement.isNetworked, Is.True);
       Assert.That(requirement.childReferences.Single().childPresetIdentifier, Is.EqualTo("bed_d_b"));

       var dummyBedRequirement = presetRegistry.entityPresetRegistryRequirements.Single(
         value => value.identifier == "bed_d_b");
       Assert.That(dummyBedRequirement.prefab, Is.Not.Null);
    }

    [TestCase(PatientDummyDAPrefabPath, "patient_dummy_d_a")]
    [TestCase(PatientDummyDBPrefabPath, "patient_dummy_d_b")]
    public void PatientDummyDPrefabsSupportPatientAnimationAndInteractions(
      string prefabPath,
      string identifier)
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

      Assert.That(prefab, Is.Not.Null);
      var patient = prefab.GetComponent<PatientController>();
      var patientState = prefab.GetComponent<PatientDummyDState>();
      var animator = prefab.GetComponent<Animator>();

      Assert.That(patient, Is.Not.Null);
      Assert.That(patientState, Is.Not.Null);
      Assert.That(prefab.GetComponent<PatientTypeBFemaleState>(), Is.Null);
      Assert.That(patient.Identifier, Is.EqualTo(identifier));
      Assert.That(prefab.GetComponent<CapsuleCollider>(), Is.Not.Null);
      Assert.That(prefab.GetComponent<NetworkObject>(), Is.Not.Null);
      Assert.That(animator, Is.Not.Null, "the dummy root must expose an Animator");
      Assert.That(animator.avatar, Is.Not.Null,
        "the dummy Animator must use its source model Humanoid Avatar");
      Assert.That(animator.runtimeAnimatorController, Is.Not.Null,
        "the dummy Animator must have the shared patient animation controller");
      Assert.That(patient.CarryAttachPoint, Is.Not.SameAs(patient.transform),
        "the dummy must have a dedicated carry attachment point");
      Assert.That(patient.Interacts, Has.Length.GreaterThanOrEqualTo(3));
    }

    [TestCase("Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBMale.prefab")]
    [TestCase("Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBFemale.prefab")]
    [TestCase(PatientDummyDAPrefabPath)]
    [TestCase(PatientDummyDBPrefabPath)]
    public void PatientBCScenarioPrefabsDisableStandardAssessActions(string prefabPath)
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      var patient = prefab != null ? prefab.GetComponent<PatientController>() : null;
      var field = typeof(PatientController).GetField(
        "_assessActions",
        BindingFlags.Instance | BindingFlags.NonPublic);

      Assert.That(patient, Is.Not.Null);
      Assert.That(field, Is.Not.Null);
      var actions = field.GetValue(patient) as List<PatientController.AssessActionConfig>;
      Assert.That(actions, Has.Count.EqualTo(4));
      Assert.That(actions.All(action => !action.Enabled), Is.True);
    }

    [Test]
    public void PatientDummyDBSupportsMedicalPresetAndTriageStateEvents()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientDummyDBPrefabPath);
      var instance = Object.Instantiate(prefab);
      try
      {
        var patient = instance.GetComponent<PatientController>();
        Assert.That(patient.Interacts, Has.Length.GreaterThanOrEqualTo(4));
        patient.ApplyMedicalStatePreset(new ScenarioPatientMedicalStatePresetNode
        {
          Name = "Dummy D",
          Age = 41,
          IntendedTriage = TriageTrainer.Entity.Patient.TriageLevel.Level3,
          PulseRate = 88,
        });

        Assert.That(patient.Descriptor.name, Is.EqualTo("Dummy D"));
        Assert.That(patient.Descriptor.age, Is.EqualTo(41));
        Assert.That(patient.IntendedTriage, Is.EqualTo(TriageTrainer.Entity.Patient.TriageLevel.Level3));
        Assert.That(patient.MedicalStatePulse.rate, Is.EqualTo(88));

        string submittedKey = null;
        Assert.That(patient.RegisterStateEventListener(
          PatientController.StateEventTriageSubmitted,
          TriageTrainer.Entity.Patient.TriageLevel.Level3.ToString(),
          key => submittedKey = key), Is.True);

        patient.SetTriageAssessable(true);
        Assert.That(patient.EffectiveAssessable, Is.True);
        patient.SubmitTriageAssessment(TriageTrainer.Entity.Patient.TriageLevel.Level3);

        Assert.That(patient.Descriptor.assessedTriage,
          Is.EqualTo(TriageTrainer.Entity.Patient.TriageLevel.Level3));
        Assert.That(submittedKey, Is.EqualTo(TriageTrainer.Entity.Patient.TriageLevel.Level3.ToString()));
        Assert.That(patient.EffectiveAssessable, Is.False,
          "DisableOnIntendedOnly should close triage after the correct submission");
      }
      finally
      {
        Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void PatientBCTriageCorrectnessUsesCurrentAssignments()
    {
      var targets = new[]
      {
        CreatePatientWithTriage("patient_b", TriageTrainer.Entity.Patient.TriageLevel.Level2),
        CreatePatientWithTriage("patient_c", TriageTrainer.Entity.Patient.TriageLevel.Level2),
        CreatePatientWithTriage("patient_dummy_d_b", TriageTrainer.Entity.Patient.TriageLevel.Level5)
      };

      try
      {
        var evaluate = typeof(TriageScenarioEventBootstrap).GetMethod(
          "ArePatientBCTriageAssignmentsCorrect",
          BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(evaluate, Is.Not.Null);
        Assert.That(evaluate.Invoke(null, new object[] { targets }), Is.True);

        SetAssessedTriage(targets[0].GetComponent<PatientController>(),
          TriageTrainer.Entity.Patient.TriageLevel.Level3);

        Assert.That(evaluate.Invoke(null, new object[] { targets }), Is.False,
          "a historical correct submission must not mask a later triage change");
      }
      finally
      {
        foreach (var target in targets)
          Object.DestroyImmediate(target);
      }
    }

    [Test]
    public void TriageRetryResetClearsPreviousAssessmentAndReenablesInteraction()
    {
      var target = CreatePatientWithTriage("patient_b", TriageTrainer.Entity.Patient.TriageLevel.Level2);
      var patient = target.GetComponent<PatientController>();

      try
      {
        patient.SetTriageAssessable(false);
        patient.ResetTriageAssessmentForRetry();

        Assert.That(patient.AssessedTriage, Is.EqualTo(TriageTrainer.Entity.Patient.TriageLevel.Unassessed));
        Assert.That(patient.Descriptor.assessedTriage, Is.EqualTo(TriageTrainer.Entity.Patient.TriageLevel.Unassessed));
        Assert.That(patient.EffectiveAssessable, Is.True);
      }
      finally
      {
        Object.DestroyImmediate(target);
      }
    }

    [Test]
    public void PatientSpecificTriageRetryResolvesTargetAfterRefreshingRuntimeReferences()
    {
      var retry = typeof(TriageScenarioEventBootstrap).GetMethod(
        "Event_ResetPatientBCTriageAttempt",
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        new[] { typeof(string) },
        null);

      Assert.That(retry, Is.Not.Null,
        "환자별 재시도 이벤트는 갱신 전 GameObject를 캡처하지 않고 식별자로 대상을 다시 해석해야 합니다.");
    }

    [Test]
    public void TriageAssessableSyncRefreshesLocalInteractionHints()
    {
      var callback = typeof(PatientController).GetMethod(
        "OnTriageAssessableChanged",
        BindingFlags.Instance | BindingFlags.NonPublic);

      Assert.That(callback, Is.Not.Null,
        "서버가 재시도 평가를 다시 열면 복제된 클라이언트의 상호작용 힌트도 갱신되어야 합니다.");
    }

    [TestCase(
      "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBMale.prefab",
      "patient_b",
      PatientController.TreatmentDisplay.GauzePatchedOnLeftArm,
      PatientController.TreatmentDisplay.GauzeDressingDoneOnLeftArm)]
    [TestCase(
      "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBFemale.prefab",
      "patient_c",
      PatientController.TreatmentDisplay.GauzePatchedOnRightArm,
      PatientController.TreatmentDisplay.GauzeDressingDoneOnRightArm)]
    public void PatientBCDressingUsesPrefabSupportedArm(
      string prefabPath,
      string patientIdentifier,
      PatientController.TreatmentDisplay expectedGauze,
      PatientController.TreatmentDisplay expectedPlaster)
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      var instance = Object.Instantiate(prefab);
      try
      {
        var patient = instance.GetComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier(patientIdentifier);

        Assert.That(InvokePrivate<PatientController.TreatmentDisplay>(patient,
          "ResolveTreatmentDisplayForPatient",
          PatientController.TreatmentDisplay.GauzePatchedOnThorax), Is.EqualTo(expectedGauze));
        Assert.That(InvokePrivate<PatientController.TreatmentDisplay>(patient,
          "ResolveTreatmentDisplayForPatient",
          PatientController.TreatmentDisplay.GauzeDressingDoneOnThorax), Is.EqualTo(expectedPlaster));
      }
      finally
      {
        Object.DestroyImmediate(instance);
      }
    }

    [TestCase("patient_b")]
    [TestCase("patient_c")]
    public void PatientBCIntravenousLineAcceptsOnly20G(string patientIdentifier)
    {
      var patientObject = new GameObject(patientIdentifier);
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier(patientIdentifier);

        Assert.That(InvokePrivate<bool>(patient, "IsCannulaGaugeAllowed", "cannula_20g"), Is.True);
        Assert.That(InvokePrivate<bool>(patient, "IsCannulaGaugeAllowed", "cannula_18g"), Is.False);
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void OtherPatientIntravenousLineStillAccepts18G()
    {
      var patientObject = new GameObject("patient_a");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_a");

        Assert.That(InvokePrivate<bool>(patient, "IsCannulaGaugeAllowed", "cannula_18g"), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PatientBCTreatmentItemsAreStageGatedAndSequential()
    {
      var patientObject = new GameObject("patient_b");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");

        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "nasalcannula"), Is.False);
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.False);
        patient.ActivatePatientBCNurseDStage();
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "nasalcannula"), Is.True);
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.False);
        InvokePrivate(patient, "NotifyPatientBCItemApplied", "nasalcannula");
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.False);
        Assert.That(InvokePrivate<bool>(patient, "ShouldCreditPatientBCEquipmentConnection",
          PatientController.EquipmentTypeOxyflowmeter), Is.True);
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.True);
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "plaster"), Is.False);
        InvokePrivate(patient, "NotifyPatientBCItemApplied", "gauze");
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "plaster"), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PatientBCPreinstalledOxygenRequiresObservedDetachAndFreshReinstall()
    {
      var patientObject = new GameObject("patient_b");
      var flowmeterObject = new GameObject("preinstalled-flowmeter");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        flowmeterObject.AddComponent<BoxCollider>();
        var flowmeter = flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();
        flowmeter.ApplyShownFromNetwork();
        patient.SetConnectedOxyflowmeter(flowmeter);

        Assert.That(patient.ActivatePatientBCNurseDStage(), Is.True);
        InvokePrivate(patient, "NotifyPatientBCItemApplied", "nasalcannula");
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.False,
          "nasal application must not synthesize oxygen credit from the preinstalled device");
        Assert.That(InvokePrivate<bool>(patient, "ShouldCreditPatientBCEquipmentConnection",
          PatientController.EquipmentTypeOxyflowmeter), Is.False,
          "the stale connection must remain ineligible before a detach is observed");

        flowmeter.ApplyHiddenFromNetwork();
        patient.ClearConnectedOxyflowmeter(flowmeter);
        Assert.That(flowmeter.IsAttached, Is.False);
        Assert.That(flowmeter.IsVisible, Is.False);
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.False);
        flowmeter.ApplyShownFromNetwork();
        patient.SetConnectedOxyflowmeter(flowmeter);

        Assert.That(flowmeter.IsAttached, Is.True);
        Assert.That(flowmeter.IsVisible, Is.True);
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.True,
          "only the connection after the observed detach is fresh enough for credit");
      }
      finally
      {
        Object.DestroyImmediate(flowmeterObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void CareZoneImmediatelyClearsDetachedEquipmentAndReconnectsFreshInstall()
    {
      var zoneObject = new GameObject("care-zone");
      var patientObject = new GameObject("patient_b");
      var flowmeterObject = new GameObject("flowmeter");
      var suctionObject = new GameObject("wall-suction");
      try
      {
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        zone.ConfigureArea(Vector3.zero, new Vector3(10f, 10f, 10f));
        SetPrivateField(zone, "_requireBedSnapForPatient", false);
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        SetPrivateField(zone, "_activePatient", patient);
        flowmeterObject.AddComponent<BoxCollider>();
        suctionObject.AddComponent<BoxCollider>();
        var flowmeter = flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();
        var suction = suctionObject.AddComponent<WallAttachedWallSuction>();

        flowmeter.ApplyShownFromNetwork();
        suction.ApplyShownFromNetwork();
        Assert.That(patient.ConnectedOxyflowmeter, Is.SameAs(flowmeter));
        Assert.That(patient.ConnectedWallSuction, Is.SameAs(suction));
        Assert.That(patient.ActivatePatientBCNurseDStage(), Is.True);
        InvokePrivate(patient, "NotifyPatientBCItemApplied", "nasalcannula");

        flowmeter.Detach();
        suction.Detach();
        Assert.That(patient.ConnectedOxyflowmeter, Is.Null);
        Assert.That(patient.ConnectedWallSuction, Is.Null);
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.False);

        flowmeter.ApplyShownFromNetwork();
        suction.ApplyShownFromNetwork();
        Assert.That(patient.ConnectedOxyflowmeter, Is.SameAs(flowmeter));
        Assert.That(patient.ConnectedWallSuction, Is.SameAs(suction));
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.True,
          "authoritative detach must be observed before the fresh zone reconnect is credited");
      }
      finally
      {
        Object.DestroyImmediate(suctionObject);
        Object.DestroyImmediate(flowmeterObject);
        Object.DestroyImmediate(patientObject);
        Object.DestroyImmediate(zoneObject);
      }
    }

    [Test]
    public void PatientBCFreshInstallBeforeNasalRemainsValid()
    {
      var patientObject = new GameObject("patient_c");
      var flowmeterObject = new GameObject("fresh-flowmeter");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_c");
        flowmeterObject.AddComponent<BoxCollider>();
        var flowmeter = flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();

        Assert.That(patient.ActivatePatientBCNurseDStage(), Is.False);
        patient.SetConnectedOxyflowmeter(flowmeter);
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.False);
        InvokePrivate(patient, "NotifyPatientBCItemApplied", "nasalcannula");

        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(flowmeterObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PatientBCNasalBeforeFreshInstallRemainsValid()
    {
      var patientObject = new GameObject("patient_b");
      var flowmeterObject = new GameObject("fresh-flowmeter");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        flowmeterObject.AddComponent<BoxCollider>();
        var flowmeter = flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();

        Assert.That(patient.ActivatePatientBCNurseDStage(), Is.False);
        InvokePrivate(patient, "NotifyPatientBCItemApplied", "nasalcannula");
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.False);
        patient.SetConnectedOxyflowmeter(flowmeter);

        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(flowmeterObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PatientBCPreinstalledOxygenWarningIsExactKoreanText()
    {
      Assert.That(TriageScenarioEventBootstrap.PreinstalledOxygenWarning,
        Is.EqualTo("이미 산소장치가 설치되어있다. 이것을 해제하고 새로 설치하자."));
    }

    [Test]
    public void PatientBCIvAndNormalSalineRequireActivatedPupilSequence()
    {
      var patientObject = new GameObject("patient_c");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_c");

        Assert.That(InvokePrivate<bool>(patient, "CanPerformPatientBCIv"), Is.False);
        Assert.That(patient.TryCompletePatientBCNormalSalineConnection(), Is.False);
        patient.ActivatePatientBCNurseCStage();
        Assert.That(InvokePrivate<bool>(patient, "CanPerformPatientBCIv"), Is.False);
        InvokePrivate(patient, "NotifyPatientBCPupilCompleted");
        Assert.That(InvokePrivate<bool>(patient, "CanPerformPatientBCIv"), Is.True);
        Assert.That(patient.TryCompletePatientBCNormalSalineConnection(), Is.False);
        Assert.That(InvokePrivate<bool>(patient, "TryAdvancePatientBCIvStageAuthoritative"), Is.True);
        Assert.That(patient.TryCompletePatientBCNormalSalineConnection(), Is.False,
          "completion must require a registered physical saline line");
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PatientBCTreatmentActorMustBeKnownNearbyAndCorrectRole()
    {
      var patientObject = new GameObject("patient_b");
      var playerObject = new GameObject("remote-player");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        var player = playerObject.AddComponent<MultiplayerInfrastructure.Player.PlayerController>();

        player.transform.position = patient.transform.position;
        Assert.That(InvokeActorPolicy(patient, player, actorKnown: false, hasRequiredRole: true),
          Is.False, "unknown/hostile actors must be rejected");
        Assert.That(InvokeActorPolicy(patient, player, actorKnown: true, hasRequiredRole: false),
          Is.False, "wrong-role actors must be rejected");
        player.transform.position = patient.transform.position + Vector3.right * 3.01f;
        Assert.That(InvokeActorPolicy(patient, player, actorKnown: true, hasRequiredRole: true),
          Is.False, "remote actors must be rejected");
      }
      finally
      {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PatientBCTreatmentRejectsMissingItemAndMissingPhysicalEquipment()
    {
      var patientObject = new GameObject("patient_c");
      var playerObject = new GameObject("nurse-c");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_c");
        var player = playerObject.AddComponent<MultiplayerInfrastructure.Player.PlayerController>();
        player.transform.position = patient.transform.position;

        Assert.That(player.CountItemInInventory("gauze"), Is.Zero);
        Assert.That(InvokePrivate<bool>(patient, "HasPhysicalPatientBCNormalSalineConnection"), Is.False);
        Assert.That(patient.ConnectedOxyflowmeter, Is.Null);
        Assert.That(patient.TryCompletePatientBCNormalSalineConnection(), Is.False);
      }
      finally
      {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PatientBCTreatmentStagesUseReplicatedSyncVars()
    {
      foreach (string fieldName in new[] { "_patientBCNurseCStage", "_patientBCNurseDStage" })
      {
        var field = typeof(PatientController).GetField(
          fieldName,
          BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        Assert.That(field.FieldType.IsGenericType, Is.True, fieldName);
        Assert.That(field.FieldType.GetGenericTypeDefinition(), Is.EqualTo(typeof(SyncVar<>)), fieldName);
      }
    }

    [Test]
    public void OtherPatientTreatmentActionsRemainUngated()
    {
      var patientObject = new GameObject("patient_a");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_a");

        Assert.That(InvokePrivate<bool>(patient, "CanPerformPatientBCIv"), Is.True);
        Assert.That(InvokePrivate<bool>(patient, "CanApplyPatientBCItem", "gauze"), Is.True);
        Assert.That(patient.TryCompletePatientBCNormalSalineConnection(), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void OverworldSceneContainsPatientBCAnchorsAndSignalZones()
    {
      var previousSetup = EditorSceneManager.GetSceneManagerSetup();
      try
      {
        EditorSceneManager.OpenScene(OverworldScenePath, OpenSceneMode.Single);

        var anchors = UnityEngine.Object.FindObjectsByType<WaypointAnchor>(
          FindObjectsInactive.Include, FindObjectsSortMode.None);
        var identifiers = anchors.Select(anchor => anchor.Identifier).ToArray();
        Assert.That(identifiers, Does.Contain(OverworldGameObjectInitializer.PatientBSpawnWaypointIdentifier));
        Assert.That(identifiers, Does.Contain(OverworldGameObjectInitializer.PatientCSpawnWaypointIdentifier));
        Assert.That(identifiers, Does.Contain(OverworldGameObjectInitializer.PatientDummyDBSpawnWaypointIdentifier));
        Assert.That(identifiers, Does.Contain(OverworldGameObjectInitializer.TriageArrivalWaypointIdentifier));
        Assert.That(identifiers, Does.Contain(OverworldGameObjectInitializer.DoctorSpawnWaypointIdentifier));
        Assert.That(identifiers, Does.Contain(OverworldGameObjectInitializer.DoctorCareAreaWaypointIdentifier));
        Assert.That(identifiers, Does.Contain(OverworldGameObjectInitializer.CtPatientBTargetPositionWaypointIdentifier));
        Assert.That(identifiers, Does.Contain(OverworldGameObjectInitializer.CtPatientCTargetPositionWaypointIdentifier));

        foreach (string patientSpawnIdentifier in new[]
                 {
                   OverworldGameObjectInitializer.PatientBSpawnWaypointIdentifier,
                   OverworldGameObjectInitializer.PatientCSpawnWaypointIdentifier,
                   OverworldGameObjectInitializer.PatientDummyDBSpawnWaypointIdentifier
                 })
        {
          var patientSpawnAnchor = anchors.Single(anchor => anchor.Identifier == patientSpawnIdentifier);
          Assert.That(patientSpawnAnchor.transform.position.y, Is.EqualTo(0f).Within(0.001f),
            $"Patient/bed preset spawn anchor '{patientSpawnIdentifier}' must remain on the floor.");
        }

        var zones = UnityEngine.Object.FindObjectsByType<ScenarioTriggerZone>(
          FindObjectsInactive.Include, FindObjectsSortMode.None);
        AssertSignalZone(
          zones,
          OverworldGameObjectInitializer.TriageArrivalWaypointIdentifier,
          new[] { "quest_arrival_triage_area" },
          "quest_arrival_triage_area_{id}",
          true);
        AssertSignalZone(
          zones,
          OverworldGameObjectInitializer.CtPatientBTargetPositionWaypointIdentifier,
          System.Array.Empty<string>(),
          "ct_patient_arrived_{id}",
          false);
      }
      finally
      {
        if (previousSetup.Length > 0)
          EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        else
          EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
      }
    }

    [Test]
    public void MovingBedSnapAllowListMatchesOverworldStaticLayout()
    {
      var layout = AssetDatabase.LoadAssetAtPath<StaticEntityLayoutDefinition>(StaticLayoutPath);
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MovingBedPrefabPath);
      Assert.That(layout, Is.Not.Null);
      Assert.That(prefab, Is.Not.Null);

      var layoutIdentifiers = layout.groups
        .SelectMany(group => group.entities ?? new List<StaticEntityTransformDefinition>())
        .Where(entity => entity.type == StaticEntityLayoutType.MovingPatientBedPositioningPoint)
        .Select(entity => entity.identifier)
        .ToArray();
      var bed = prefab.GetComponent<MovingPatientBedController>();
      Assert.That(bed, Is.Not.Null);
      var allowedProperty = new SerializedObject(bed).FindProperty("_allowedPositioningPointIdentifiers");
      var allowedIdentifiers = Enumerable.Range(0, allowedProperty.arraySize)
        .Select(index => allowedProperty.GetArrayElementAtIndex(index).stringValue)
        .ToArray();

      Assert.That(allowedIdentifiers, Is.EquivalentTo(layoutIdentifiers));
      Assert.That(allowedIdentifiers, Has.Length.EqualTo(4));
    }

    [Test]
    public void TriageArrivalZoneIsTriggerOnly()
    {
      var previousSetup = EditorSceneManager.GetSceneManagerSetup();
      try
      {
        EditorSceneManager.OpenScene(OverworldScenePath, OpenSceneMode.Single);
        var zone = UnityEngine.Object.FindObjectsByType<ScenarioTriggerZone>(
          FindObjectsInactive.Include, FindObjectsSortMode.None)
          .Single(candidate => candidate.Identifier == OverworldGameObjectInitializer.TriageArrivalWaypointIdentifier);
        var collider = zone.GetComponent<BoxCollider>();

        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.isTrigger, Is.True);
      }
      finally
      {
        if (previousSetup.Length > 0)
          EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        else
          EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
      }
    }

    private static void AssertSignalZone(
      IEnumerable<ScenarioTriggerZone> zones,
      string identifier,
      string[] enterSignals,
      string perEntitySignalTemplate,
      bool perEntityPlayersOnly)
    {
      var zone = zones.SingleOrDefault(candidate => candidate.Identifier == identifier);
      Assert.That(zone, Is.Not.Null, $"Missing ScenarioTriggerZone '{identifier}'");
      var serializedZone = new SerializedObject(zone);
      var signalsProperty = serializedZone.FindProperty("_raiseSignalsOnEnter");
      var actualSignals = Enumerable.Range(0, signalsProperty.arraySize)
        .Select(index => signalsProperty.GetArrayElementAtIndex(index).stringValue)
        .ToArray();
      Assert.That(actualSignals, Is.EqualTo(enterSignals), identifier);
      Assert.That(serializedZone.FindProperty("_perEntitySignalTemplate").stringValue,
        Is.EqualTo(perEntitySignalTemplate), identifier);
      Assert.That(serializedZone.FindProperty("_perEntityRaiseOncePerEntity").boolValue,
        Is.False, identifier);
      Assert.That(serializedZone.FindProperty("_perEntityPlayersOnly").boolValue,
        Is.EqualTo(perEntityPlayersOnly), identifier);
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

    [TestCase(false, true, true)]
    [TestCase(false, false, true)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    public void RecognitionInteractionRemainsEnabledWhenStageHasNoMicrophone(
      bool microphoneEnabled,
      bool interactionDisabled,
      bool expected)
    {
      var resolveInteraction = typeof(PatientController).GetMethod(
        "ShouldEnableRecognitionInteraction",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

      Assert.That(resolveInteraction, Is.Not.Null);
      Assert.That(
        (bool)resolveInteraction.Invoke(null, new object[] { microphoneEnabled, interactionDisabled }),
        Is.EqualTo(expected));
    }

    [TestCase(RecognitionCheckMicrophoneInput.Availability.Pending, false)]
    [TestCase(RecognitionCheckMicrophoneInput.Availability.Ready, false)]
    [TestCase(RecognitionCheckMicrophoneInput.Availability.PermissionDenied, true)]
    [TestCase(RecognitionCheckMicrophoneInput.Availability.NoDevice, true)]
    [TestCase(RecognitionCheckMicrophoneInput.Availability.RecordingFailed, true)]
    public void RecognitionMicrophoneUnavailableStatesExposeFallback(
      RecognitionCheckMicrophoneInput.Availability availability,
      bool expected)
    {
      Assert.That(RecognitionCheckMicrophoneInput.IsUnavailableState(availability), Is.EqualTo(expected));
      if (expected)
        Assert.That(RecognitionCheckMicrophoneInput.GetUnavailableGuidance(availability), Does.Contain("말 걸기"));
    }

    [TestCase(0f, true)]
    [TestCase(10f, false)]
    public void RecognitionCompletionAcceptsNearbyAndRejectsRemotePlayers(float playerDistance, bool expected)
    {
      var patientObject = new GameObject("recognition-distance-patient");
      var playerObject = new GameObject("recognition-distance-player");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        var player = playerObject.AddComponent<MultiplayerInfrastructure.Player.PlayerController>();
        playerObject.transform.position = patientObject.transform.position + Vector3.right * playerDistance;
        SetSyncVarValue(patient, "_recognitionCheckActive", true);
        SetSyncVarValue(patient, "_recognitionInteractionEnabled", true);

        var complete = typeof(PatientController).GetMethod(
          "TryCompleteRecognitionCheckAuthoritative",
          BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(complete, Is.Not.Null);
        Assert.That(complete.Invoke(patient, new object[] { player, false }), Is.EqualTo(expected));
        Assert.That(GetSyncVarValue<bool>(patient, "_recognitionCheckActive"), Is.EqualTo(!expected));
      }
      finally
      {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void TagTextSelectorUsesLiteralFallbackWhenTagIsMissing()
    {
      Assert.That(
        ScenarioTextResolver.Resolve("@t=[definitely_missing_tag, ???]선생님"),
        Is.EqualTo("???선생님"));
    }

    private static T InvokePrivate<T>(PatientController patient, string methodName, object argument)
    {
      var method = typeof(PatientController).GetMethod(
        methodName,
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null, methodName);
      return (T)method.Invoke(patient, new[] { argument });
    }

    private static T InvokePrivate<T>(
      PatientController patient,
      string methodName,
      object firstArgument,
      object secondArgument)
    {
      var method = typeof(PatientController).GetMethod(
        methodName,
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        new[] { typeof(MultiplayerInfrastructure.Player.PlayerController), typeof(string) },
        null);
      Assert.That(method, Is.Not.Null, methodName);
      return (T)method.Invoke(patient, new[] { firstArgument, secondArgument });
    }

    private static bool InvokeActorPolicy(
      PatientController patient,
      MultiplayerInfrastructure.Player.PlayerController player,
      bool actorKnown,
      bool hasRequiredRole)
    {
      var method = typeof(PatientController).GetMethod(
        "IsPatientBCTreatmentActorValid",
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null);
      return (bool)method.Invoke(patient, new object[] { player, actorKnown, hasRequiredRole });
    }

    private static T InvokePrivate<T>(PatientController patient, string methodName)
    {
      var method = typeof(PatientController).GetMethod(
        methodName,
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null, methodName);
      return (T)method.Invoke(patient, null);
    }

    private static void InvokePrivate(PatientController patient, string methodName, object argument = null)
    {
      var method = typeof(PatientController).GetMethod(
        methodName,
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null, methodName);
      method.Invoke(patient, argument == null ? null : new[] { argument });
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
      var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(field, Is.Not.Null, fieldName);
      field.SetValue(target, value);
    }

    private static bool IsAuthoritativeCareZoneMonitorForPatient(
      PatientMonitorController monitor,
      PatientController patient)
    {
      var method = typeof(PatientMonitorController).GetMethod(
        "IsAuthoritativeCareZoneMonitorForPatient",
        BindingFlags.Static | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null);
      return (bool)method.Invoke(null, new object[] { monitor, patient });
    }

    private static GameObject CreatePatientWithTriage(
      string identifier,
      TriageTrainer.Entity.Patient.TriageLevel intendedTriage)
    {
      var target = new GameObject(identifier);
      var patient = target.AddComponent<PatientController>();
      patient.ApplyMedicalStatePreset(new ScenarioPatientMedicalStatePresetNode
      {
        IntendedTriage = intendedTriage
      });
      SetAssessedTriage(patient, intendedTriage);
      return target;
    }

    private static void SetAssessedTriage(
      PatientController patient,
      TriageTrainer.Entity.Patient.TriageLevel triage)
      => SetSyncVarValue(patient, "_assessedTriage", triage);

    private static void SetSyncVarValue<T>(PatientController patient, string fieldName, T value)
    {
      var field = typeof(PatientController).GetField(
        fieldName,
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(field, Is.Not.Null);
      var syncVar = field.GetValue(patient);
      var valueProperty = syncVar.GetType().GetProperty("Value");
      Assert.That(valueProperty, Is.Not.Null);
      valueProperty.SetValue(syncVar, value);
    }

    private static T GetSyncVarValue<T>(PatientController patient, string fieldName)
    {
      var field = typeof(PatientController).GetField(
        fieldName,
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(field, Is.Not.Null);
      var syncVar = field.GetValue(patient);
      var valueProperty = syncVar.GetType().GetProperty("Value");
      Assert.That(valueProperty, Is.Not.Null);
      return (T)valueProperty.GetValue(syncVar);
    }
  }
}
