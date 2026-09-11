using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using TriageTrainer.Editor.Utils;
using TriageTrainer.Entity;
using TriageTrainer.Entity.LineConnection;
using TriageTrainer.Entity.OxyLine;
using TriageTrainer.Entity.PatientMonitor.Models;
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
    public void TriageInteractionsCarryOnlyTheirOwnPatientBriefing()
    {
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(
        Path.Combine(Application.dataPath,
          "Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json")));

      foreach (var expectation in new[]
               {
                 (Patient: "patient_b", DistinctText: "좌측 상완 개방성 골절"),
                 (Patient: "patient_c", DistinctText: "우측 상완 개방성 골절"),
                 (Patient: "patient_dummy_d_b", DistinctText: "사지 찰과상 외 양호")
               })
      {
        var definition = graph.Interactions.Single(value =>
          value.Entity.Identifier == expectation.Patient
          && value.InteractionIdentifier == PatientController.InteractIdTriage);
        Assert.That(definition.GetExtra("triageBriefing"), Does.Contain(expectation.DistinctText));
        Assert.That(definition.VisibilityConditions.Single().Tag, Is.EqualTo("nurse_a"));
      }

      Assert.That(graph.Nodes["TRIAGE_A_Q"].NextIdentifier, Is.EqualTo("TRIAGE_ENABLE_B"));
      Assert.That(graph.Nodes["TRIAGE_ENABLE_B"].NextIdentifier, Is.EqualTo("TRIAGE_ENABLE_C"));
      Assert.That(graph.Nodes["TRIAGE_ENABLE_C"].NextIdentifier, Is.EqualTo("TRIAGE_ENABLE_D"));
      Assert.That(graph.Nodes["TRIAGE_ENABLE_D"].NextIdentifier, Is.EqualTo("TRIAGE_WAIT_ALL"));
      Assert.That(graph.Nodes["TRIAGE_B_CONFIRMED"].NextIdentifier, Is.EqualTo("TRIAGE_A_REMOVE"));
      Assert.That(graph.Nodes["TRIAGE_D_CONFIRMED"].NextIdentifier, Is.EqualTo("TRIAGE_CHECK_CORRECT"));
    }

    [Test]
    public void FixedExteriorDoorHasNoUninitializedTriggerHandler()
    {
      var previousSetup = EditorSceneManager.GetSceneManagerSetup();
      try
      {
        EditorSceneManager.OpenScene(OverworldScenePath, OpenSceneMode.Single);
        var fixedDoor = GameObject.Find("autoDoor_6");
        Assert.That(fixedDoor, Is.Not.Null);
        Assert.That(fixedDoor.GetComponent<autoDoorSlide>(), Is.Null,
          "Disabled vendor scripts still receive Unity trigger callbacks before Start initializes their state.");
        var barrier = fixedDoor.GetComponent<BoxCollider>();
        Assert.That(barrier, Is.Not.Null);
        Assert.That(barrier.enabled, Is.True, "Keep the exterior barrier solid.");
        Assert.That(barrier.isTrigger, Is.False);
        Assert.That(fixedDoor.GetComponent<AudioSource>().enabled, Is.False);
        Assert.That(GameObject.Find("autoDoor_6 (1)").GetComponent<autoDoorSlide>().enabled, Is.True,
          "The working interior automatic door must remain enabled.");
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
    public void CtTransportWaitAllowsSequentialFourPlayerBedTrips()
    {
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(
        Path.Combine(Application.dataPath,
          "Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json")));

      foreach (var identifier in new[] { "CT_A_WAIT", "CT_B_WAIT", "CT_C_WAIT", "CT_D_WAIT" })
      {
        var gate = (ScenarioValidatorNode)graph.Nodes[identifier];
        Assert.That(gate.WaitTimeoutSeconds, Is.GreaterThanOrEqualTo(600f),
          $"{identifier} must cover two sequential four-player CT transports and the return walk.");
      }
    }

    [Test]
    public void PatientBCAutoAdvanceIsLimitedToInteractionResultFeedback()
    {
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(
        Path.Combine(Application.dataPath,
          "Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json")));
      var automaticFeedbackNodes = new HashSet<string>
      {
        "TRIAGE_WRONG", "TRIAGE_RESET_REMOVE", "TRIAGE_REENABLE_B",
        "A_AVPU_WRONG_A", "A_AVPU_WRONG_P", "A_AVPU_WRONG_U", "A_AVPU_CORRECT",
        "A_E_WRONG", "A_E_CORRECT", "A_V_WRONG", "A_V_CORRECT", "A_M_WRONG", "A_M_CORRECT",
        "B_RR_WRONG", "B_RR_CORRECT", "B_BP_WRONG", "B_BP_CORRECT", "B_TEMP_WRONG", "B_TEMP_CORRECT",
        "D_NASAL_DONE", "D_OXY_REPORT", "D_GAUZE_DONE", "D_PLASTER_DONE",
        "C_A_AVPU_WRONG_A", "C_A_AVPU_WRONG_P", "C_A_AVPU_WRONG_U", "C_A_AVPU_CORRECT",
        "C_A_E_WRONG", "C_A_E_CORRECT", "C_A_V_WRONG", "C_A_V_CORRECT", "C_A_M_WRONG", "C_A_M_CORRECT",
        "C_B_RR_WRONG", "C_B_RR_CORRECT", "C_B_BP_WRONG", "C_B_BP_CORRECT", "C_B_TEMP_WRONG", "C_B_TEMP_CORRECT",
        "C_D_NASAL_DONE", "C_D_OXY_REPORT", "C_D_GAUZE_DONE", "C_D_PLASTER_DONE"
      };

      var autoAdvanceNodes = graph.Nodes.Values
        .OfType<ScenarioDialogueNode>()
        .Where(node => node.AutoAdvanceSeconds.GetValueOrDefault() > 0f)
        .ToArray();

      Assert.That(autoAdvanceNodes.Select(node => node.Identifier),
        Is.EquivalentTo(automaticFeedbackNodes));
      Assert.That(autoAdvanceNodes.All(node => node.AutoAdvanceSeconds == 4f), Is.True,
        "자동 진행 피드백은 기본 타이핑 속도로 전체 문장이 출력될 시간을 보장해야 한다.");
    }

    [TestCase("TRIAGE_WAIT_ALL")]
    [TestCase("TRIAGE_CHECK_CORRECT")]
    [TestCase("TRIAGE_A_REMOVE")]
    public void MissingTriageSubmissionExitsWithoutClaimingCorrectClassification(string identifier)
    {
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(
        Path.Combine(Application.dataPath, "Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json")));
      var gate = (ScenarioValidatorNode)graph.Nodes[identifier];
      Assert.That(gate.WaitForCondition, Is.True);
      Assert.That(gate.OnWaitTimeout, Is.EqualTo(ScenarioValidatorWaitTimeoutBehavior.FailBranch));
      Assert.That(gate.FailureNextIdentifier, Is.EqualTo("TRIAGE_A_FINAL_REMOVE"));
      var exit = (ScenarioQuestControlNode)graph.Nodes[gate.FailureNextIdentifier];
      Assert.That(exit.Operation, Is.EqualTo(ScenarioQuestOperationType.Remove));
      Assert.That(exit.NextIdentifier, Is.EqualTo("CC_TRIAGE_A"));
      Assert.That(gate.NextIdentifier, Is.Not.EqualTo(gate.FailureNextIdentifier),
        "Successful submissions retain their clinical evaluation path.");
    }

    [Test]
    public void CareZoneRegistersUnattachedEquipmentWithoutActiveColliders()
    {
      var root = new GameObject("CareZoneUnattachedEquipmentTest");
      root.SetActive(false);
      root.transform.position = new Vector3(10000f, 10000f, 10000f);
      try
      {
        var zoneObject = new GameObject("CareZone");
        zoneObject.transform.SetParent(root.transform);
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        zone.ConfigureArea(Vector3.zero, new Vector3(10f, 10f, 10f));

        var suctionObject = new GameObject("wall_suction");
        suctionObject.transform.SetParent(root.transform);
        suctionObject.AddComponent<WallAttachedWallSuction>();

        var flowmeterObject = new GameObject("oxyflowmeter");
        flowmeterObject.transform.SetParent(root.transform);
        flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();

        typeof(PatientCareDescriptionZone).GetMethod(
            "RefreshEquipment", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.Invoke(zone, null);

        Assert.That(GetPrivateField<int>(zone, "_wallSuctionInZoneCount"), Is.EqualTo(1));
        Assert.That(GetPrivateField<int>(zone, "_oxyflowmeterInZoneCount"), Is.EqualTo(1));
        Assert.That(zone.WallSuction, Has.Count.EqualTo(1));
        Assert.That(zone.Oxyflowmeters, Has.Count.EqualTo(1));
      }
      finally
      {
        Object.DestroyImmediate(root);
      }
    }

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
      var zoneObject = new GameObject("VitalMonitorCloseCareZoneTest");
      try
      {
        // 오프라인 닫기 완료도 케어존 검증을 통과해야 하므로 환자/모니터를 포함한 존을 구성한다.
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        zone.ConfigureArea(Vector3.zero, new Vector3(10f, 10f, 10f));
        var bootstrap = bootstrapObject.AddComponent<TriageScenarioEventBootstrap>();
        var monitor = monitorObject.AddComponent<SinglePatientMonitorController>();
        var patientObject = new GameObject(completionSignal.EndsWith("_b") ? "patient_b" : "patient_c");
        patientObject.SetActive(false);
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier(completionSignal.EndsWith("_b") ? "patient_b" : "patient_c");
        SetPrivateField(zone, "_activePatient", patient);
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
          configure.Invoke(bootstrap, new object[] { monitor, patient, completionSignal });
          requestClose.Invoke(monitor, null);
          requestClose.Invoke(monitor, null);
          Assert.That(signalCount, Is.EqualTo(1),
            "arm 상태는 첫 닫기에서 소진되어야 한다.");
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
        Object.DestroyImmediate(zoneObject);
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
      string scenarioJson = File.ReadAllText(path);
      StringAssert.DoesNotContain("환자 B", scenarioJson);
      StringAssert.DoesNotContain("환자 C", scenarioJson);
      var graph = ScenarioGraphLoader.LoadFromJson(scenarioJson, validateWithSchema: true);

      Assert.That(graph.DefaultEntrypoint, Is.EqualTo("GIVE_CHECKLIST_PAPER_IF_MISSING"));
      Assert.That(graph.Nodes, Has.Count.EqualTo(362));
      var checklistPaperGrant = graph.Nodes["GIVE_CHECKLIST_PAPER_IF_MISSING"] as ScenarioExecuteCommandNode;
      Assert.That(checklistPaperGrant, Is.Not.Null);
      Assert.That(checklistPaperGrant.CommandLine, Is.EqualTo("give-if-missing checklist_paper @a"));
      Assert.That(checklistPaperGrant.NextIdentifier, Is.EqualTo("SPAWN_B"));
      // sig.interact_oxyflow_wall_* 는 설치된 산소 유량계를 클릭한 클라이언트에서만 발신된다
      // (WallAttachedOxyflowmeter.Interact). 이 신호가 서버에 도달하지 못하면
      // PatientCareDescriptionZone 이 산소 라인을 만들지 못해 환자 B/C 산소 처치가 진행되지 않으므로
      // client-origin 접두사로 선언한다.
      Assert.That(graph.ClientSignalPrefixes, Is.EqualTo(new[]
      {
        "sig.quest_arrival_triage_area_",
        "sig.interact_oxyflow_wall_"
      }));
      // 환자 모니터 선택(select_patient_*)도 상호작용한 클라이언트에서만 발신된다
      // (PatientController.RaisePatientInteractionSignals).
      Assert.That(graph.ClientSignalIdentifiers, Is.EqualTo(new[]
      {
        "sig.patient_b_pupil_checked",
        "sig.patient_c_pupil_checked",
        "sig.select_patient_b",
        "sig.select_patient_c"
      }));
      Assert.That(graph.ActingNpcs, Has.Count.EqualTo(1));
      Assert.That(graph.ActingNpcs.Single().Identifier, Is.EqualTo("npc-doctor-patient-b-c-ct"));
      Assert.That(graph.ActingNpcs.Single().PresetIdentifier, Is.EqualTo("npc_doctor_preset"));

      foreach (var expectation in new[]
               {
                 (Patient: "patient_b", Interaction: "pupil_check", Role: "nurse_a"),
                 (Patient: "patient_b", Interaction: "intravenous_line_cannula", Role: "nurse_a"),
                 (Patient: "patient_b", Interaction: "normal_saline_connect", Role: "nurse_a"),
                 (Patient: "patient_b", Interaction: "patient_bc_nasal_cannula", Role: "nurse_c"),
                 (Patient: "patient_c", Interaction: "pupil_check", Role: "nurse_b"),
                 (Patient: "patient_c", Interaction: "intravenous_line_cannula", Role: "nurse_b"),
                 (Patient: "patient_c", Interaction: "normal_saline_connect", Role: "nurse_b"),
                 (Patient: "patient_c", Interaction: "patient_bc_nasal_cannula", Role: "nurse_d")
               })
      {
        var definition = graph.Interactions.Single(value =>
          value.Entity.Identifier == expectation.Patient
          && value.InteractionIdentifier == expectation.Interaction);
        Assert.That(definition.VisibilityConditions, Has.Count.EqualTo(1),
          $"{expectation.Patient}/{expectation.Interaction}");
        Assert.That(definition.VisibilityConditions[0].Tag, Is.EqualTo(expectation.Role),
          $"{expectation.Patient}/{expectation.Interaction}");
      }

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
      Assert.That(doctorMove.DestinationType, Is.EqualTo(ScenarioMoveDestinationType.WaypointSet));
      Assert.That(doctorMove.DestinationIdentifier,
        Is.EqualTo(OverworldGameObjectInitializer.DoctorRouteWaypointSetIdentifier));
      Assert.That(graph.Nodes["P_MOVE"].NextIdentifier, Is.EqualTo("MOVE_DOCTOR_TO_CARE_AREA"));
      Assert.That(graph.Nodes["care_patient_c"].NextIdentifier, Is.EqualTo("C_DOC_C"));
      Assert.That(graph.Nodes["MOVE_DOCTOR_TO_CARE_AREA"].NextIdentifier, Is.EqualTo("P_B_C_CARE"));
      Assert.That(graph.Nodes["care_patient_b"].NextIdentifier, Is.EqualTo("DOC_C"));
      Assert.That(graph.Nodes.ContainsKey("C_ARRIVAL"), Is.False);
      Assert.That(graph.Nodes["C_DOC_C"].NextIdentifier, Is.EqualTo("C_DOC_D"));
      Assert.That(graph.Nodes["C_DOC_D"].NextIdentifier, Is.EqualTo("P_B_C_TREATMENT"));
      Assert.That(graph.Nodes.ContainsKey("P_C_CARE"), Is.True);
      Assert.That(graph.Nodes.ContainsKey("P_B_C_CARE"), Is.True);
      Assert.That(graph.Nodes.ContainsKey("P_B_TREATMENT"), Is.True);
      Assert.That(graph.Nodes.ContainsKey("P_C_TREATMENT"), Is.True);
      var bothPatientCare = graph.Nodes["P_B_C_CARE"] as ScenarioParallelNode;
      Assert.That(bothPatientCare, Is.Not.Null);
      Assert.That(bothPatientCare.AllocationType, Is.EqualTo(ScenarioParallelAllocationType.ByRole));
      Assert.That(bothPatientCare.WaitMode, Is.EqualTo(ScenarioWaitMode.All));
      Assert.That(bothPatientCare.NextIdentifier, Is.EqualTo("P_B_C_WAIT_REMOVE"));
      Assert.That(bothPatientCare.Branches.Select(branch =>
        (branch.Identifier, Tag: branch.RequiredPlayerTags.Single(), branch.CompletionConditionIdentifier)),
        Is.EqualTo(new[]
        {
          ("A_RECOG_Q", "nurse_a", "CC_B_INITIAL_A"),
          ("C_A_RECOG_Q", "nurse_b", "CC_C_INITIAL_B"),
          ("B_VITAL_Q", "nurse_c", "CC_B_INITIAL_C"),
          ("C_B_VITAL_Q", "nurse_d", "CC_C_INITIAL_D")
        }));
      var combinedTreatment = graph.Nodes["P_B_C_TREATMENT"] as ScenarioParallelNode;
      Assert.That(combinedTreatment, Is.Not.Null);
      Assert.That(combinedTreatment.Branches.Select(branch =>
        (branch.Identifier, Tag: branch.RequiredPlayerTags.Single())),
        Is.EqualTo(new[]
        {
          ("C_PUPIL_Q", "nurse_a"),
          ("C_C_PUPIL_Q", "nurse_b"),
          ("D_OXY_Q", "nurse_c"),
          ("C_D_OXY_Q", "nurse_d")
        }));
      var combinedTreatmentRemove = graph.Nodes["P_B_C_TREATMENT_REMOVE"] as ScenarioParallelNode;
      Assert.That(combinedTreatmentRemove, Is.Not.Null);
      Assert.That(combinedTreatmentRemove.Branches.Select(branch =>
        (branch.Identifier, Tag: branch.RequiredPlayerTags.Single())),
        Is.EqualTo(new[]
        {
          ("C_PUPIL_REMOVE", "nurse_a"),
          ("C_C_PUPIL_REMOVE", "nurse_b"),
          ("D_REMOVE", "nurse_c"),
          ("C_D_REMOVE", "nurse_d")
        }));
      Assert.That(combinedTreatmentRemove.NextIdentifier, Is.EqualTo("move_patients"));
      Assert.That(graph.Nodes["P_B_CARE"].NextIdentifier, Is.EqualTo("P_B_WAIT_REMOVE"));
      Assert.That(graph.Nodes["P_B_WAIT_REMOVE"].NextIdentifier, Is.EqualTo("DOC_C"));
      Assert.That(graph.Nodes["P_C_CARE"].NextIdentifier, Is.EqualTo("P_C_WAIT_REMOVE"));
      Assert.That(graph.Nodes["P_C_WAIT_REMOVE"].NextIdentifier, Is.EqualTo("C_DOC_C"));
      foreach (var expectation in new[]
               {
                 (Node: "P_B_CARE", FirstRole: "nurse_a", SecondRole: "nurse_c"),
                 (Node: "P_B_TREATMENT", FirstRole: "nurse_a", SecondRole: "nurse_c"),
                 (Node: "P_C_CARE", FirstRole: "nurse_b", SecondRole: "nurse_d"),
                 (Node: "P_C_TREATMENT", FirstRole: "nurse_b", SecondRole: "nurse_d")
               })
      {
        var carePhase = graph.Nodes[expectation.Node] as ScenarioParallelNode;
        Assert.That(carePhase, Is.Not.Null, expectation.Node);
        Assert.That(carePhase.WaitMode, Is.EqualTo(ScenarioWaitMode.All), expectation.Node);
        Assert.That(carePhase.Branches.Select(branch => branch.RequiredPlayerTags.Single()),
          Is.EqualTo(new[] { expectation.FirstRole, expectation.SecondRole }), expectation.Node);
      }
      var movePatients = graph.Nodes["move_patients"] as ScenarioManualEntrypointNode;
      Assert.That(movePatients, Is.Not.Null);
      Assert.That(movePatients.EntrypointIdentifier, Is.EqualTo("move_patients"));
      Assert.That(movePatients.ManualEnterSetupIdentifier, Is.EqualTo("SETUP_MOVE_PATIENTS_SNAP_BED_B"));
      Assert.That(movePatients.NextIdentifier, Is.EqualTo("CT_DOCTOR_MARK_SHOW"));

      var doctorMarkShow = graph.Nodes["CT_DOCTOR_MARK_SHOW"] as ScenarioQuestMarkNode;
      Assert.That(doctorMarkShow, Is.Not.Null,
        "CT 지시 단계에서는 그래프 노드가 의사 NPC 머리 위 퀘스트 마크를 명시적으로 켠다.");
      Assert.That(doctorMarkShow.Operation, Is.EqualTo(ScenarioQuestMarkOperationType.Show));
      Assert.That(doctorMarkShow.TargetType, Is.EqualTo(QuestPresentationTargetType.Npc));
      Assert.That(doctorMarkShow.EntityIdentifier, Is.EqualTo("npc-doctor-patient-b-c-ct"));
      Assert.That(doctorMarkShow.IconIdentifier, Is.EqualTo("quest-marker"));
      Assert.That(doctorMarkShow.NextIdentifier, Is.EqualTo("CT_DELAY"));

      Assert.That(graph.Nodes["CT_DOCTOR_ORDER"].NextIdentifier, Is.EqualTo("CT_DOCTOR_MARK_HIDE"));
      var doctorMarkHide = graph.Nodes["CT_DOCTOR_MARK_HIDE"] as ScenarioQuestMarkNode;
      Assert.That(doctorMarkHide, Is.Not.Null,
        "CT 이송이 시작되면 의사 NPC 마크를 같은 노드 종류로 다시 끈다.");
      Assert.That(doctorMarkHide.Operation, Is.EqualTo(ScenarioQuestMarkOperationType.Hide));
      Assert.That(doctorMarkHide.EntityIdentifier, Is.EqualTo("npc-doctor-patient-b-c-ct"));
      Assert.That(doctorMarkHide.NextIdentifier, Is.EqualTo("P_CT_TRANSPORT"));
      var movePatientsSetup = graph.Nodes["SETUP_MOVE_PATIENTS"] as ScenarioInvokeEventNode;
      Assert.That(movePatientsSetup?.EventIdentifier, Is.EqualTo("setup_move_patients"));
      Assert.That(graph.Nodes["SETUP_MOVE_PATIENTS_RETURN"], Is.TypeOf<ScenarioReturnToOriginNode>());
      Assert.That((graph.Nodes["SETUP_MOVE_PATIENTS_SNAP_BED_B"] as ScenarioBedSnapNode)?.NextIdentifier,
        Is.EqualTo("SETUP_MOVE_PATIENTS_SNAP_BED_C"));
      Assert.That((graph.Nodes["SETUP_MOVE_PATIENTS_SNAP_BED_C"] as ScenarioBedSnapNode)?.NextIdentifier,
        Is.EqualTo("SETUP_MOVE_PATIENTS_MOVE_DOCTOR"));
      Assert.That((graph.Nodes["SETUP_MOVE_PATIENTS_MOVE_DOCTOR"] as ScenarioNPCControlNode)?.NextIdentifier,
        Is.EqualTo("SETUP_MOVE_PATIENTS"));
      Assert.That(graph.Nodes.ContainsKey("P_CT_TRANSPORT"), Is.True);
      Assert.That(graph.Nodes["P_CT_TRANSPORT"].NextIdentifier, Is.EqualTo("CT_DETACH_BEDS"));
      var detachBeds = graph.Nodes["CT_DETACH_BEDS"] as ScenarioInvokeEventNode;
      Assert.That(detachBeds, Is.Not.Null);
      Assert.That(detachBeds.EventIdentifier, Is.EqualTo("detach_patient_b_c_beds"));
      Assert.That(detachBeds.NextIdentifier, Is.EqualTo("CT_FINAL_DELAY"));
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

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_Triage_Lead", out var triageQuest), Is.True);
      Assert.That(triageQuest.PresentationBindings, Has.Count.EqualTo(3));
      Assert.That(triageQuest.PresentationBindings.Select(binding => binding.EntityIdentifier), Is.EqualTo(new[]
      {
        "patient_b",
        "patient_c",
        "patient_dummy_d_b"
      }));
      Assert.That(triageQuest.PresentationBindings.All(binding =>
        binding.InteractionIdentifier == PatientController.InteractIdTriage
        && binding.IconIdentifier == "quest-interaction"), Is.True);

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_Move_BC", out var moveQuest), Is.True);
      Assert.That(moveQuest.IsOrdinal, Is.True);
      Assert.That(moveQuest.Tasks.Select(task => task.Identifier), Is.EqualTo(new[]
      {
        "move-patient-b",
        "move-patient-c"
      }));
      Assert.That(moveQuest.Tasks.Select(task => task.SignalId), Is.EqualTo(new[]
      {
        "patient_bed_positioning_point_latched_bed_b",
        "patient_bed_positioning_point_latched_bed_c"
      }), "환자 이동 완료는 환자 콜라이더의 구역 진입 여부가 아니라 각 환자 침대의 스냅으로 판정해야 한다.");
      Assert.That(moveQuest.PresentationBindings.Select(binding =>
        (binding.EntityIdentifier, binding.InteractionIdentifier)), Is.EqualTo(new[]
      {
        ("bed_b", MovingPatientBedController.InteractionIdentifierMoveBed),
        ("bed_c", MovingPatientBedController.InteractionIdentifierMoveBed)
      }));
      foreach (string waitIdentifier in new[] { "MOVE_A_WAIT", "MOVE_B_WAIT", "MOVE_C_WAIT", "MOVE_D_WAIT" })
      {
        var moveWait = graph.Nodes[waitIdentifier] as ScenarioValidatorNode;
        Assert.That(moveWait, Is.Not.Null, waitIdentifier);
        Assert.That(moveWait.RootConditions.Single().ValidationRules.Select(rule => rule.RegistryIdentifier),
          Is.EquivalentTo(new[]
          {
            "sig.patient_bed_positioning_point_latched_bed_b",
            "sig.patient_bed_positioning_point_latched_bed_c"
          }), $"{waitIdentifier}는 각 환자 침대가 스냅된 시점에 다음 단계로 진행해야 한다.");
      }

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_Transport_BC_To_CT", out var transportQuest), Is.True);
      Assert.That(transportQuest.Tasks.Select(task => task.Identifier), Is.EqualTo(new[]
      {
        "ct-patient-b",
        "ct-patient-c",
        "ct-room"
      }), "침대 마크를 항목별로 붙이려면 CT 이송 항목에도 식별자가 있어야 한다.");
      Assert.That(transportQuest.Tasks.Single(task => task.Identifier == "ct-room").WaypointIdentifier,
        Is.EqualTo("ct:ctroom"), "CT실 이동 task는 initializer가 생성하는 waypoint를 가리켜야 한다.");
      Assert.That(transportQuest.PresentationBindings.Select(binding =>
        (binding.CompletionCriteriaIdentifier, binding.EntityIdentifier, binding.InteractionIdentifier)),
        Is.EqualTo(new[]
      {
        ("ct-patient-b", "bed_b", MovingPatientBedController.InteractionIdentifierMoveBed),
        ("ct-patient-c", "bed_c", MovingPatientBedController.InteractionIdentifierMoveBed)
      }));
      Assert.That(transportQuest.PresentationBindings.All(binding =>
        binding.Activation == QuestPresentationActivation.CompletionCriteria
        && binding.IconIdentifier == "quest-interaction"), Is.True,
        "해당 환자의 이송 항목이 끝나면 침대 마크도 함께 사라져야 한다.");

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_B_Recognition", out var recognitionB), Is.True);
      Assert.That(recognitionB.PresentationBindings.Select(binding =>
        (binding.EntityIdentifier, binding.InteractionIdentifier, binding.IconIdentifier)), Is.EqualTo(new[]
      {
        ("patient_b", "recognition_check", "quest-interaction")
      }));

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_C_Recognition", out var recognitionC), Is.True);
      Assert.That(recognitionC.PresentationBindings.Select(binding =>
        (binding.EntityIdentifier, binding.InteractionIdentifier, binding.IconIdentifier)), Is.EqualTo(new[]
      {
        ("patient_c", "recognition_check", "quest-interaction")
      }));

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_B_Strength", out var strengthQuest), Is.True);
      Assert.That(strengthQuest.PresentationBindings.Single().InteractionIdentifier, Is.EqualTo("recognition_check"));
      Assert.That(strengthQuest.Tasks.Single().SignalId, Is.EqualTo("patient_b_strength_checked"));

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_B_Vital", out var vitalQuest), Is.True);
      Assert.That(vitalQuest.IsOrdinal, Is.True);
      Assert.That(vitalQuest.PresentationBindings.Select(binding =>
        (binding.EntityIdentifier, binding.InteractionIdentifier)), Is.EqualTo(new[]
      {
        (PatientMonitorController.MonitorPresentationEntityIdentifier, "select_patient_mode"),
        ("patient_b", "monitor_select"),
        ("patient_b", "detail_overlay")
      }), "선택 마크는 모든 환자 모니터에, 자세히 보기 마크는 그 환자를 추적하는 모니터에만 붙는다.");
      Assert.That(vitalQuest.Tasks.Select(task => task.SignalId), Is.EqualTo(new[]
      {
        "select_patient_b", "close_vital_ui_b"
      }));

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_C_Vital", out var vitalQuestC), Is.True);
      Assert.That(vitalQuestC.IsOrdinal, Is.True);
      Assert.That(vitalQuestC.PresentationBindings.Select(binding =>
        (binding.EntityIdentifier, binding.InteractionIdentifier)), Is.EqualTo(new[]
      {
        (PatientMonitorController.MonitorPresentationEntityIdentifier, "select_patient_mode"),
        ("patient_c", "monitor_select"),
        ("patient_c", "detail_overlay")
      }), "여성 환자 활력징후도 남성 환자와 같은 순서로 마크를 넘긴다.");
      Assert.That(vitalQuestC.PresentationBindings.Select(binding =>
        binding.CompletionCriteriaIdentifier), Is.EqualTo(new[]
      {
        "select-patient-c", "select-patient-c", "close-vital-ui-c"
      }));
      Assert.That(vitalQuestC.Tasks.Select(task => (task.Identifier, task.SignalId)), Is.EqualTo(new[]
      {
        ("select-patient-c", "select_patient_c"),
        ("close-vital-ui-c", "close_vital_ui_c")
      }));

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_B_Pupil_IV", out var pupilQuest), Is.True);
      Assert.That(pupilQuest.PresentationBindings.Select(binding =>
        (binding.EntityIdentifier, binding.InteractionIdentifier)), Is.EqualTo(new[]
      {
        ("patient_b", "recognition_check"),
        ("patient_b", PatientController.InteractIdIntravenousLineCannula),
        ("patient_b", PatientController.InteractIdNormalSalineConnect)
      }));
      Assert.That(pupilQuest.PresentationBindings[0].CompletionCriteriaIdentifier,
        Is.EqualTo("patient-b-pupil-checked"));
      var ivMark = pupilQuest.PresentationBindings[1];
      Assert.That(ivMark.Activation, Is.EqualTo(QuestPresentationActivation.CompletionCriteria));
      Assert.That(ivMark.CompletionCriteriaIdentifier, Is.EqualTo("patient-b-iv-secured"));
      Assert.That(ivMark.IconIdentifier, Is.EqualTo("quest-interaction"));
      Assert.That(ivMark.IconMode, Is.EqualTo(QuestPresentationIconMode.ReplacePrimaryIcon));
      Assert.That(pupilQuest.Tasks.Select(task => (task.Identifier, task.SignalId)), Is.EqualTo(new[]
      {
        ("patient-b-pupil-checked", "patient_b_pupil_checked"),
        ("patient-b-iv-secured", "insert_iv_patient_b_right")
      }), "정맥로 확보까지 목표에 남아야 퀘스트가 완료 처리되지 않고, 정맥 라인 확보 마크도 유지된다.");

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_C_Pupil_IV", out var pupilQuestC), Is.True);
      Assert.That(pupilQuestC.PresentationBindings.Select(binding =>
        (binding.EntityIdentifier, binding.InteractionIdentifier)), Is.EqualTo(new[]
      {
        ("patient_c", "recognition_check"),
        ("patient_c", PatientController.InteractIdIntravenousLineCannula),
        ("patient_c", PatientController.InteractIdNormalSalineConnect)
      }));
      Assert.That(pupilQuestC.Tasks.Select(task => (task.Identifier, task.SignalId)), Is.EqualTo(new[]
      {
        ("patient-c-pupil-checked", "patient_c_pupil_checked"),
        ("patient-c-iv-secured", "insert_iv_patient_c_left")
      }), "여성 환자는 좌측 팔에 20G를 삽입하므로 정맥로 확보 신호도 좌측이다.");

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_C_Normal_Saline", out var ivQuestC), Is.True);
      Assert.That(ivQuestC.Tasks.Single().SignalId, Is.EqualTo("connect_cannula_and_ns1_patient_c"),
        "이미 올라간 신호를 목표로 두면 이 정의가 걸리는 순간 퀘스트가 완료로 판정되고, "
        + "완료된 퀘스트는 표시 바인딩을 내주지 않아 마크가 뜨지 않는다.");

      // 생리식염수 연결 마크는 침대 걸이에 N/S 가 걸려 인터랙션이 살아 있을 때만 보인다.
      foreach (var salineQuest in new[]
               {
                 (Definition: "Quest_B_Pupil_IV", Entity: "patient_b"),
                 (Definition: "Quest_B_Normal_Saline", Entity: "patient_b"),
                 (Definition: "Quest_C_Pupil_IV", Entity: "patient_c"),
                 (Definition: "Quest_C_Normal_Saline", Entity: "patient_c")
               })
      {
        Assert.That(QuestDefinitionRegistry.TryGetGlobal(salineQuest.Definition, out var definition), Is.True,
          salineQuest.Definition);
        Assert.That(definition.PresentationBindings.Any(binding =>
            binding.EntityIdentifier == salineQuest.Entity
            && binding.InteractionIdentifier == PatientController.InteractIdNormalSalineConnect
            && binding.Activation == QuestPresentationActivation.WholeQuest), Is.True, salineQuest.Definition);
      }

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_C_Strength", out var strengthQuestC), Is.True);
      Assert.That(strengthQuestC.Tasks.Single().SignalId, Is.EqualTo("patient_c_strength_checked"));
      Assert.That(strengthQuestC.PresentationBindings.Select(binding =>
        (binding.EntityIdentifier, binding.InteractionIdentifier)), Is.EqualTo(new[]
      {
        ("patient_c", "recognition_check")
      }), "근력 확인은 의식 확인과 같은 인터랙션을 쓴다.");

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_C_Oxygen_Bleeding", out var oxygenQuestC), Is.True);
      Assert.That(oxygenQuestC.Tasks.Select(task => (task.Identifier, task.SignalId)), Is.EqualTo(new[]
      {
        ("patient-c-oxygen-supplied", "equipment_connected_oxyflowmeter_patient_c"),
        ("patient-c-bleeding-controlled", "apply_plaster_on_gauze_patient_c")
      }), "목표가 하나도 없는 퀘스트는 완료 판정을 받지 못한다. 산소 공급과 지혈을 목표로 남긴다.");
      Assert.That(oxygenQuestC.PresentationBindings.Select(binding =>
        (binding.CompletionCriteriaIdentifier, binding.EntityIdentifier, binding.InteractionIdentifier)),
        Is.EqualTo(new[]
      {
        ("patient-c-oxygen-supplied", "patient_c", "patient_bc_nasal_cannula"),
        ("patient-c-oxygen-supplied", "zone_0:oxyflowmeter", WallAttachedOxyflowmeter.QuestPresentationInteractionIdentifier),
        ("patient-c-oxygen-supplied", "zone_1:oxyflowmeter", WallAttachedOxyflowmeter.QuestPresentationInteractionIdentifier),
        ("patient-c-oxygen-supplied", "zone_2:oxyflowmeter", WallAttachedOxyflowmeter.QuestPresentationInteractionIdentifier),
        ("patient-c-oxygen-supplied", "zone_3:oxyflowmeter", WallAttachedOxyflowmeter.QuestPresentationInteractionIdentifier),
        ("patient-c-bleeding-controlled", "patient_c", PatientController.InteractIdItemApply)
      }));

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_B_Oxygen_Bleeding", out var oxygenQuest), Is.True);
      Assert.That(oxygenQuest.IsOrdinal, Is.True);
      Assert.That(oxygenQuest.PresentationBindings.Select(binding =>
        (binding.CompletionCriteriaIdentifier, binding.EntityIdentifier, binding.InteractionIdentifier)),
        Is.EqualTo(new[]
      {
        ("patient-b-oxygen-supplied", "patient_b", "patient_bc_nasal_cannula"),
        ("patient-b-oxygen-supplied", "zone_0:oxyflowmeter", WallAttachedOxyflowmeter.QuestPresentationInteractionIdentifier),
        ("patient-b-oxygen-supplied", "zone_1:oxyflowmeter", WallAttachedOxyflowmeter.QuestPresentationInteractionIdentifier),
        ("patient-b-oxygen-supplied", "zone_2:oxyflowmeter", WallAttachedOxyflowmeter.QuestPresentationInteractionIdentifier),
        ("patient-b-oxygen-supplied", "zone_3:oxyflowmeter", WallAttachedOxyflowmeter.QuestPresentationInteractionIdentifier),
        ("patient-b-bleeding-controlled", "patient_b", PatientController.InteractIdItemApply)
      }), "남성 환자도 여성 환자와 같이 목표 단위로 마크를 넘기고, 지혈 마크까지 갖춘다.");
      Assert.That(oxygenQuest.PresentationBindings.Any(binding =>
          binding.InteractionIdentifier == WallAttachedOxyflowmeter.DetachInteractionIdentifier), Is.False,
        "산소 유량계 회수에는 퀘스트 마크가 붙어서는 안 된다.");
      Assert.That(oxygenQuest.Tasks.Select(task => (task.Identifier, task.SignalId)), Is.EqualTo(new[]
      {
        ("patient-b-oxygen-supplied", "equipment_connected_oxyflowmeter_patient_b"),
        ("patient-b-bleeding-controlled", "apply_plaster_on_gauze_patient_b")
      }), "산소 공급과 지혈은 별도 목표로 표기한다.");
      Assert.That(oxygenQuest.Tasks.Select(task => task.DisplayTextContent), Is.EqualTo(new[]
      {
        "많이 다친 남성 환자에게 산소 공급하기",
        "많이 다친 남성 환자 지혈하기"
      }));

      Assert.That(QuestDefinitionRegistry.TryGetGlobal("Quest_B_Normal_Saline", out var ivQuest), Is.True);
      Assert.That(ivQuest.Tasks.Single().SignalId, Is.EqualTo("connect_cannula_and_ns1_patient_b"),
        "이미 올라간 신호를 목표로 두면 이 정의가 걸리는 순간 퀘스트가 완료로 판정되고, "
        + "완료된 퀘스트는 표시 바인딩을 내주지 않아 마크가 뜨지 않는다.");

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
        Is.EqualTo("TRIAGE_C_CONFIRMED"));
      Assert.That(graph.Nodes["TRIAGE_A_REMOVE"].NextIdentifier,
        Is.EqualTo("TRIAGE_REENABLE_D"));
      Assert.That(graph.Nodes["TRIAGE_REENABLE_D"].NextIdentifier,
        Is.EqualTo("TRIAGE_D_CONFIRMED"));
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
      Assert.That(patientCRightGrade?.CorrectOptionIndex, Is.EqualTo(5));

      Assert.That((graph.Nodes["A_STRENGTH_TRANSITION"] as ScenarioDialogueNode)?.DialogueContent,
        Is.EqualTo("근력은 어떻지..?"));
      Assert.That((graph.Nodes["C_A_STRENGTH_TRANSITION"] as ScenarioDialogueNode)?.DialogueContent,
        Is.EqualTo("근력은 어떻지..?"));
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
      string questJson = File.ReadAllText(questPath);
      StringAssert.DoesNotContain("환자 B", questJson);
      StringAssert.DoesNotContain("환자 C", questJson);
      using var questDocument = JsonDocument.Parse(questJson);
      var questDefinitions = questDocument.RootElement.GetProperty("definitions")
        .EnumerateArray()
        .ToDictionary(definition => definition.GetProperty("identifier").GetString());
      Assert.That(questDefinitions, Has.Count.EqualTo(21));
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
                 (Wait: "C_IV_WAIT", Update: "C_NS_QUEST_UPDATE", Notice: "C_NS_CONNECT_NOTICE", Definition: "Quest_B_Normal_Saline", Content: "많이 다친 남성 환자에게 생리식염수 연결하기"),
                 (Wait: "C_C_IV_WAIT", Update: "C_C_NS_QUEST_UPDATE", Notice: "C_C_NS_CONNECT_NOTICE", Definition: "Quest_C_Normal_Saline", Content: "많이 다친 여성 환자에게 생리식염수 연결하기")
               })
      {
        Assert.That(graph.Nodes[expectation.Wait].NextIdentifier, Is.EqualTo(expectation.Update));
        var update = graph.Nodes[expectation.Update] as ScenarioQuestControlNode;
        Assert.That(update?.Operation, Is.EqualTo(ScenarioQuestOperationType.Update));
        Assert.That(update?.Quest?.DefinitionIdentifier, Is.EqualTo(expectation.Definition));
        var notice = graph.Nodes[expectation.Notice] as ScenarioDialogueNode;
        Assert.That(notice?.SpeakerName, Is.EqualTo("@s"));
        Assert.That(notice?.DialogueContent, Is.EqualTo("(환자에게 생리식염수를 연결해두자.)"));
        Assert.That(questDefinitions[expectation.Definition].GetProperty("questContent").GetString(),
          Is.EqualTo(expectation.Content));
      }

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
      var oxyPointField = typeof(PatientTypeBMaleState).GetField(
        "oxyLineConnectionPoint", BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(oxyPointField, Is.Not.Null);
      var oxyPoints = prefab.GetComponentsInChildren<TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint>(true);
      Assert.That(oxyPoints, Has.Length.EqualTo(1));
      Assert.That(oxyPointField.GetValue(patientState), Is.SameAs(oxyPoints[0]));

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
    public void PatientDummyDPrefabsSupportPatientVisualsAndInteractions(
      string prefabPath,
      string identifier)
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

      Assert.That(prefab, Is.Not.Null);
      var patient = prefab.GetComponent<PatientController>();
      var patientState = prefab.GetComponent<PatientDummyDState>();
      var visual = prefab.GetComponentInChildren<Renderer>(true);
      var animator = prefab.GetComponentInChildren<Animator>(true);

      Assert.That(patient, Is.Not.Null);
      Assert.That(patientState, Is.Not.Null);
      Assert.That(prefab.GetComponent<PatientTypeBFemaleState>(), Is.Null);
      Assert.That(patient.Identifier, Is.EqualTo(identifier));
      Assert.That(prefab.GetComponent<CapsuleCollider>(), Is.Not.Null);
      Assert.That(prefab.GetComponent<NetworkObject>(), Is.Not.Null);
      Assert.That(visual, Is.Not.Null, "the triage dummy must include its dedicated visual model");
      // 더미 D 는 분류 전용 static/Generic-rig 모델이다. 향후 모델이 Humanoid
      // 애니메이션으로 업그레이드되면, 일부만 설정된 Animator 를 받아들이는 대신
      // 완전한 애니메이션 명세를 요구한다.
      if (animator != null)
      {
        Assert.That(animator.avatar, Is.Not.Null,
          "an animated dummy must use its source model Humanoid Avatar");
        Assert.That(animator.runtimeAnimatorController, Is.Not.Null,
          "an animated dummy must have the shared patient animation controller");
      }
      Assert.That(patient.CarryAttachPoint, Is.Not.SameAs(patient.transform),
        "the dummy must have a dedicated carry attachment point");
      Assert.That(patient.Interacts, Has.Length.GreaterThanOrEqualTo(3));
    }

    [TestCase("Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBMale.prefab")]
    [TestCase("Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBFemale.prefab")]
    [TestCase(PatientDummyDAPrefabPath)]
    [TestCase(PatientDummyDBPrefabPath)]
    public void PatientBCScenarioPrefabsDoNotSerializeInteractionDataAndHideStandardAssessActions(string prefabPath)
    {
      string yaml = File.ReadAllText(prefabPath);
      foreach (string field in new[]
               {
                 "_assessActions:", "_interactConfigs:", "_liftDisplayText:", "_carryDisplayText:",
                 "_monitorSelectDisplayText:"
               })
      {
        Assert.That(yaml, Does.Not.Contain(field),
          $"{Path.GetFileName(prefabPath)}: 인터렉션 데이터('{field}')는 프리팹이 아니라 코드 리터럴·시나리오 데이터가 정의합니다.");
      }

      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = Object.Instantiate(prefab);
      try
      {
        var patient = instance.GetComponent<PatientController>();
        Assert.That(patient, Is.Not.Null);
        var identifierField = FindInstanceField(typeof(PatientController), "_identifier");
        Assert.That(identifierField, Is.Not.Null);
        identifierField.SetValue(patient, "test-bc-code-declaration-patient");
        Assert.That(patient.Interacts, Is.Not.Empty);

        var declared = patient.DeclareInteractions().ToDictionary(
          declaration => declaration.Definition.InteractionIdentifier,
          declaration => declaration.Definition);
        foreach (string identifier in new[] { "assess_vital", "assess_avpu_gcs", "assess_pulse", "assess_gcs" })
        {
          Assert.That(declared, Contains.Key(identifier));
          Assert.That(declared[identifier].InitialVisible, Is.False,
            $"표준 사정 동작 '{identifier}'은 시나리오 데이터가 열기 전에는 숨겨져야 합니다.");
        }
      }
      finally
      {
        Object.DestroyImmediate(instance);
      }
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
    public void TriageAssessableControlDelegatesToServerFromNonHostClients()
    {
      // 호환 실행 경로에서 nurse_a 브랜치의 TriageAssessControl 노드와 재시도 이벤트는 담당 클라이언트에서만
      // 실행된다. 담당자가 호스트가 아니어도 서버 권위 SyncVar 가 갱신되려면 ServerRpc 위임 경로가 있어야 한다.
      var setAssessable = typeof(PatientController).GetMethod(
        "CmdSetTriageAssessable",
        BindingFlags.Instance | BindingFlags.NonPublic);
      var resetForRetry = typeof(PatientController).GetMethod(
        "CmdResetTriageAssessmentForRetry",
        BindingFlags.Instance | BindingFlags.NonPublic);

      Assert.That(setAssessable, Is.Not.Null,
        "nurse_a 가 서버 호스트가 아니어도 중증도 분류 인터랙션이 열리려면 활성화 요청을 서버에 위임해야 합니다.");
      Assert.That(resetForRetry, Is.Not.Null,
        "재시도 초기화도 담당 클라이언트가 호스트가 아닐 때 서버에 위임해야 합니다.");
      Assert.That(setAssessable.GetCustomAttribute<ServerRpcAttribute>(), Is.Not.Null);
      Assert.That(resetForRetry.GetCustomAttribute<ServerRpcAttribute>(), Is.Not.Null);
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

    [TestCase("patient_b")]
    [TestCase("patient_c")]
    public void PatientBCOxygenCreditRaisesThePatientSpecificQuestSignal(string patientIdentifier)
    {
      var patientObject = new GameObject(patientIdentifier);
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier(patientIdentifier);
        patient.ActivatePatientBCNurseDStage();
        InvokePrivate(patient, "NotifyPatientBCItemApplied", "nasalcannula");

        Assert.That(InvokePrivate<bool>(patient, "ShouldCreditPatientBCEquipmentConnection",
          PatientController.EquipmentTypeOxyflowmeter), Is.True);
        InvokePrivate(patient, "RaisePatientBCOxygenSuppliedSignals");

        Assert.That(ScenarioInteractionSignals.IsRaised(
          $"equipment_connected_oxyflowmeter_{patientIdentifier}"), Is.True);
      }
      finally
      {
        ScenarioInteractionSignals.Clear($"equipment_connected_oxyflowmeter_{patientIdentifier}");
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void OxyflowmeterAttachmentBindingRearmsAfterSignalClear()
    {
      const string signal = "oxyflowmeter_attached_patient_b";
      var patientObject = new GameObject("patient_b");
      var flowmeterObject = new GameObject("oxyflowmeter");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        var flowmeter = flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();

        Assert.That(ScenarioEntityStateSignalBindings.Register(
          "test_b_oxy_raw",
          patient,
          PatientController.StateEventOxyflowmeterAttachmentChanged,
          "Attached",
          signal,
          consumeOnce: true), Is.True);

        InvokePrivate(patient, "ReportOxyflowmeterAttachment", flowmeter, true);
        Assert.That(ScenarioInteractionSignals.IsRaised(signal), Is.True);

        ScenarioInteractionSignals.Clear(signal);
        InvokePrivate(patient, "ReportOxyflowmeterAttachment", flowmeter, false);
        InvokePrivate(patient, "ReportOxyflowmeterAttachment", flowmeter, true);

        Assert.That(ScenarioInteractionSignals.IsRaised(signal), Is.True,
          "신호를 지운 뒤에는 consumeOnce 바인딩이 다시 활성화되어 재설치도 처리해야 합니다.");
      }
      finally
      {
        ScenarioEntityStateSignalBindings.ClearAll();
        ScenarioInteractionSignals.Clear(signal);
        Object.DestroyImmediate(flowmeterObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [TestCase("patient_b")]
    [TestCase("patient_c")]
    public void PatientBCBleedingStagesRaisePatientSpecificQuestSignals(string patientIdentifier)
    {
      var patientObject = new GameObject(patientIdentifier);
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier(patientIdentifier);
        patient.ActivatePatientBCNurseDStage();
        InvokePrivate(patient, "NotifyPatientBCItemApplied", "nasalcannula");
        Assert.That(InvokePrivate<bool>(patient, "ShouldCreditPatientBCEquipmentConnection",
          PatientController.EquipmentTypeOxyflowmeter), Is.True);

        InvokePrivate(patient, "NotifyPatientBCItemApplied", "gauze");
        Assert.That(ScenarioInteractionSignals.IsRaised($"apply_gauze_{patientIdentifier}"), Is.True);

        InvokePrivate(patient, "NotifyPatientBCItemApplied", "plaster");
        Assert.That(ScenarioInteractionSignals.IsRaised(
          $"apply_plaster_on_gauze_{patientIdentifier}"), Is.True);
      }
      finally
      {
        ScenarioInteractionSignals.Clear($"apply_gauze_{patientIdentifier}");
        ScenarioInteractionSignals.Clear($"apply_plaster_on_gauze_{patientIdentifier}");
        Object.DestroyImmediate(patientObject);
      }
    }

    [TestCase("patient_b")]
    [TestCase("patient_c")]
    public void PatientBCNasalCannulaRaisesThePatientSpecificOrderSignal(string patientIdentifier)
    {
      var patientObject = new GameObject(patientIdentifier);
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier(patientIdentifier);
        patient.ActivatePatientBCNurseDStage();
        Assert.That(ScenarioInteractionSignals.IsRaised($"apply_nasal_cannula_{patientIdentifier}"), Is.False,
          "단계 활성화는 이전 세션의 신호를 지운다.");

        InvokePrivate(patient, "NotifyPatientBCItemApplied", "nasalcannula");

        Assert.That(ScenarioInteractionSignals.IsRaised($"apply_nasal_cannula_{patientIdentifier}"), Is.True,
          "nurse D 순서 게이트(*_D_ORDER_GATE)를 여는 신호다. EntityStateSignalBinding 은 consumeOnce 라 "
          + "세션 중 한 번 소비되면 다시 발신되지 않으므로, 권위 경로에서 직접 올려야 게이트가 막히지 않는다.");
      }
      finally
      {
        ScenarioInteractionSignals.Clear($"apply_nasal_cannula_{patientIdentifier}");
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

        patient.SetConnectedOxyflowmeter(flowmeter);
        Assert.That(patient.ActivatePatientBCNurseDStage(), Is.False,
          "미설치 유량계 참조만으로는 사전 설치 경고를 표시하면 안 됩니다.");
        patient.ClearConnectedOxyflowmeter(flowmeter);

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
        typeof(PatientCareDescriptionZone).GetMethod(
            "OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.Invoke(zone, null);
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
        var zone = zoneObject.GetComponent<PatientCareDescriptionZone>();
        typeof(PatientCareDescriptionZone).GetMethod(
            "OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.Invoke(zone, null);
        Object.DestroyImmediate(suctionObject);
        Object.DestroyImmediate(flowmeterObject);
        Object.DestroyImmediate(patientObject);
        Object.DestroyImmediate(zoneObject);
      }
    }

    [Test]
    public void FlowmeterOperationConnectsOxygenLineToActiveNasalCannula()
    {
      var serviceObject = new GameObject("line-service");
      var zoneObject = new GameObject("care-zone");
      var patientObject = new GameObject("patient_b");
      var patientPortObject = new GameObject("nasal-cannula-port");
      var flowmeterObject = new GameObject("flowmeter");
      var flowmeterPortObject = new GameObject("flowmeter-port");
      try
      {
        serviceObject.AddComponent<LineConnectionService>();
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        var patient = patientObject.AddComponent<PatientController>();
        var patientPort = patientPortObject.AddComponent<OxyLineConnectionPoint>();
        var flowmeter = flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();
        var flowmeterPort = flowmeterPortObject.AddComponent<OxyLineConnectionPoint>();

        patient.ApplySpawnedEntityIdentifier("patient_b");
        patientPortObject.transform.SetParent(patientObject.transform, false);
        flowmeter.ApplyShownFromNetwork();
        SetPrivateField(flowmeter, "_oxyLineConnectionPoint", flowmeterPort);
        SetPrivateField(flowmeter, "_attachedInteractSignal", "test_flowmeter_operation_connects_oxygen_line");
        SetPrivateField(zone, "_activePatient", patient);

        zone.TryReconcileOxygenLineFor(flowmeter);
        Assert.That(flowmeterPort.IsPhysicallyConnectedTo(patientPort), Is.False,
          "유량계 조작 전에는 산소 라인을 만들면 안 됩니다.");

        ScenarioInteractionSignals.Raise("test_flowmeter_operation_connects_oxygen_line");
        zone.TryReconcileOxygenLineFor(flowmeter);

        Assert.That(flowmeterPort.IsPhysicallyConnectedTo(patientPort), Is.True,
          "유량계 조작 완료 후 활성 비강 캐뉼라 포트와 산소 라인이 연결되어야 합니다.");
        Assert.That(patient.ConnectedOxyflowmeter, Is.SameAs(flowmeter),
          "유량계 조작은 같은 CareZone의 환자에게 장비 연결 상태도 반영해야 합니다.");
      }
      finally
      {
        ScenarioInteractionSignals.Clear("test_flowmeter_operation_connects_oxygen_line");
        Object.DestroyImmediate(flowmeterPortObject);
        Object.DestroyImmediate(flowmeterObject);
        Object.DestroyImmediate(patientPortObject);
        Object.DestroyImmediate(patientObject);
        Object.DestroyImmediate(zoneObject);
        Object.DestroyImmediate(serviceObject);
      }
    }

    [Test]
    public void NasalCannulaActivationReconcilesAnAlreadyOperatedFlowmeter()
    {
      var serviceObject = new GameObject("line-service");
      var zoneObject = new GameObject("care-zone");
      var patientObject = new GameObject("patient_b");
      var patientPortObject = new GameObject("nasal-cannula-port");
      var flowmeterObject = new GameObject("flowmeter");
      var flowmeterPortObject = new GameObject("flowmeter-port");
      const string operationSignal = "test_operated_before_nasal_cannula";
      try
      {
        serviceObject.AddComponent<LineConnectionService>();
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        var patient = patientObject.AddComponent<PatientController>();
        var patientPort = patientPortObject.AddComponent<OxyLineConnectionPoint>();
        var flowmeter = flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();
        var flowmeterPort = flowmeterPortObject.AddComponent<OxyLineConnectionPoint>();

        patient.ApplySpawnedEntityIdentifier("patient_b");
        patientPortObject.transform.SetParent(patientObject.transform, false);
        patientPortObject.SetActive(false);
        flowmeter.ApplyShownFromNetwork();
        SetPrivateField(flowmeter, "_oxyLineConnectionPoint", flowmeterPort);
        SetPrivateField(flowmeter, "_attachedInteractSignal", operationSignal);
        SetPrivateField(zone, "_activePatient", patient);
        patient.SetConnectedOxyflowmeter(flowmeter);

        ScenarioInteractionSignals.Raise(operationSignal);
        zone.TryReconcileOxygenLineFor(flowmeter);
        Assert.That(flowmeterPort.IsPhysicallyConnectedTo(patientPort), Is.False,
          "비강 캐뉼라 포트가 비활성인 동안에는 산소 라인을 만들면 안 됩니다.");

        patientPortObject.SetActive(true);
        InvokePrivate(patient, "ReconcilePatientBCOxygenLine");

        Assert.That(flowmeterPort.IsPhysicallyConnectedTo(patientPort), Is.True,
          "유량계를 먼저 조작했어도 비강 캐뉼라 적용 뒤에는 산소 라인을 다시 판정해야 합니다.");
      }
      finally
      {
        ScenarioInteractionSignals.Clear(operationSignal);
        Object.DestroyImmediate(flowmeterPortObject);
        Object.DestroyImmediate(flowmeterObject);
        Object.DestroyImmediate(patientPortObject);
        Object.DestroyImmediate(patientObject);
        Object.DestroyImmediate(zoneObject);
        Object.DestroyImmediate(serviceObject);
      }
    }

    [Test]
    public void UnattachedFlowmeterAcceptsHeldOxyflowmeterForInstallation()
    {
      var playerObject = new GameObject("player");
      var flowmeterObject = new GameObject("oxyflowmeter");
      try
      {
        var player = playerObject.AddComponent<MultiplayerInfrastructure.Player.PlayerController>();
        var flowmeter = flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();
        var item = (MultiplayerInfrastructure.ItemSystem.Item)System.Activator.CreateInstance(
          typeof(TriageTrainer.ItemDefinitions.Oxyflowmeter));
        item.CurrentStackCount = 1;
        player.HandlingItem = item;
        SetPrivateField(flowmeter, "_toggleByActiveState", true);
        flowmeter.Hide();

        Assert.That(flowmeter.IsAttached, Is.False);
        Assert.That(flowmeter.gameObject.activeInHierarchy, Is.True,
          "미설치 유량계는 렌더러만 숨기고 설치 상호작용용 오브젝트는 활성 상태로 남아야 합니다.");
        Assert.That(flowmeter.CanInteract(player.transform), Is.True,
          "산소 유량계를 손에 든 플레이어는 미설치 유량계 가까이에서 설치 상호작용을 볼 수 있어야 합니다.");
      }
      finally
      {
        Object.DestroyImmediate(flowmeterObject);
        Object.DestroyImmediate(playerObject);
      }
    }

    [Test]
    public void ReinstalledOxyflowmeterOffersTheOperateInteractionAgain()
    {
      var flowmeterObject = new GameObject("oxyflowmeter");
      const string identifier = "zone_1:oxyflowmeter";
      try
      {
        var flowmeter = flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();
        flowmeter.SetEntityIdentifier(identifier);
        string operateSignal = flowmeter.ResolveAttachedInteractSignal();
        Assert.That(operateSignal, Is.EqualTo($"interact_oxyflow_wall_{identifier}"));

        flowmeter.OnShownConfirmed();
        Assert.That(flowmeter.IsDetachInteraction, Is.False,
          "갓 설치한 유량계의 첫 상호작용은 조작이어야 한다.");

        ScenarioInteractionSignals.Raise(operateSignal);
        Assert.That(flowmeter.IsDetachInteraction, Is.True,
          "조작을 마치면 다음 상호작용은 회수로 넘어간다.");

        flowmeter.Detach();
        flowmeter.OnShownConfirmed();
        Assert.That(ScenarioInteractionSignals.IsRaised(operateSignal), Is.False);
        Assert.That(flowmeter.IsDetachInteraction, Is.False,
          "회수한 뒤 다시 설치하면 조작 단계를 처음부터 다시 수행할 수 있어야 한다.");
      }
      finally
      {
        ScenarioInteractionSignals.Clear($"interact_oxyflow_wall_{identifier}");
        Object.DestroyImmediate(flowmeterObject);
      }
    }

    [TestCase("patient_b", true)]
    [TestCase("patient_c", true)]
    [TestCase("patient_dummy_d_b", false)]
    [TestCase(null, false)]
    public void OxyflowmeterQuestMarkCoversBothTreatmentPatients(string patientIdentifier, bool expected)
    {
      var method = typeof(WallAttachedOxyflowmeter).GetMethod(
        "IsOxygenTreatmentPatient",
        BindingFlags.Static | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null);

      Assert.That((bool)method.Invoke(null, new object[] { patientIdentifier }), Is.EqualTo(expected),
        "산소 공급 목표를 받는 환자는 남성·여성 둘 다이며, 유량계 퀘스트 마크도 둘 다에 붙어야 한다.");
    }

    [Test]
    public void StaticOxyflowmeterPrefabHasNoFishNetNetworkObject()
    {
      const string prefabPath =
        "Assets/Modules/TriageTrainer/Prefabs/StaticAttachmentDisplayments/WallAttachedOxyflowmeter/oxyflowmeter.prefab";
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

      Assert.That(prefab, Is.Not.Null);
      Assert.That(prefab.GetComponent<NetworkObject>(), Is.Null,
        "A static flowmeter must not carry a FishNet NetworkObject.");
    }

    [Test]
    public void LineConnectionPointsDoNotRequireFishNetNetworkBehaviour()
    {
      Assert.That(typeof(NetworkBehaviour).IsAssignableFrom(typeof(LineConnectionPoint)), Is.False);
      Assert.That(typeof(NetworkBehaviour).IsAssignableFrom(typeof(OxyLineConnectionPoint)), Is.False);
      Assert.That(typeof(NetworkBehaviour).IsAssignableFrom(
        typeof(TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint)), Is.False);
      Assert.That(typeof(NetworkBehaviour).IsAssignableFrom(
        typeof(TriageTrainer.Entity.SuctionLine.SuctionLineConnectionPoint)), Is.False);
      Assert.That(typeof(NetworkBehaviour).IsAssignableFrom(
        typeof(TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint)), Is.False);
    }

    [Test]
    public void FlowmeterIdentifierChangeReregistersItsStaticDisplayment()
    {
      var flowmeterObject = new GameObject("oxyflowmeter");
      const string identifier = "test:flowmeter:registration";
      try
      {
        var flowmeter = flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();
        flowmeter.SetEntityIdentifier(identifier);

        Assert.That(flowmeter.EntityIdentifier, Is.EqualTo(identifier));
        Assert.That(MultiplayerInfrastructure.Registry.Registry.TryGetEntity(identifier, out var descriptor), Is.True);
        Assert.That(descriptor.GameObject, Is.SameAs(flowmeterObject));
      }
      finally
      {
        Object.DestroyImmediate(flowmeterObject);
      }
    }

    [Test]
    public void FlowmeterUsesQuestPresentationIdentifierOnlyForItsActivePatientZone()
    {
      const string oxygenConnectedSignal = "equipment_connected_oxyflowmeter_patient_b";
      var zoneObject = new GameObject("care-zone");
      var patientObject = new GameObject("patient_b");
      var flowmeterObject = new GameObject("oxyflowmeter");
      try
      {
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        zone.ConfigureArea(Vector3.zero, new Vector3(10f, 10f, 10f));
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        var flowmeter = flowmeterObject.AddComponent<WallAttachedOxyflowmeter>();
        SetPrivateField(zone, "_activePatient", patient);

        ScenarioInteractionSignals.Clear(oxygenConnectedSignal);
        Assert.That(flowmeter.InteractionIdentifier,
          Is.EqualTo(WallAttachedOxyflowmeter.QuestPresentationInteractionIdentifier));

        ScenarioInteractionSignals.Raise(oxygenConnectedSignal);
        Assert.That(flowmeter.InteractionIdentifier,
          Is.EqualTo(WallAttachedOxyflowmeter.UnmarkedInteractionIdentifier));
      }
      finally
      {
        ScenarioInteractionSignals.Clear(oxygenConnectedSignal);
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
        flowmeter.ApplyShownFromNetwork();

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
        flowmeter.ApplyShownFromNetwork();

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
    public void CareEntrypointSetupChainRestoresBedsAndDoctor()
    {
      string path = Path.Combine(
        Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);

      foreach (string entrypoint in new[] { "care_patient_b", "care_patient_c" })
      {
        var node = graph.Nodes[entrypoint] as ScenarioManualEntrypointNode;
        Assert.That(node, Is.Not.Null, entrypoint);
        Assert.That(node.ManualEnterSetupIdentifier, Is.EqualTo("SETUP_CARE_SNAP_BED_B"), entrypoint);
      }

      var snapB = graph.Nodes["SETUP_CARE_SNAP_BED_B"] as ScenarioBedSnapNode;
      var snapC = graph.Nodes["SETUP_CARE_SNAP_BED_C"] as ScenarioBedSnapNode;
      Assert.That(snapB?.BedEntityIdentifier, Is.EqualTo("bed_b"));
      Assert.That(snapC?.BedEntityIdentifier, Is.EqualTo("bed_c"));
      Assert.That(snapC.NextIdentifier, Is.EqualTo("SETUP_CARE_MOVE_DOCTOR"));

      // 준비 체인의 의사 위치는 본 흐름의 이동 노드가 쓰는 목적지를 그대로 따른다.
      var doctorMove = graph.Nodes["MOVE_DOCTOR_TO_CARE_AREA"] as ScenarioNPCControlNode;
      var doctorTeleport = graph.Nodes["SETUP_CARE_MOVE_DOCTOR"] as ScenarioNPCControlNode;
      Assert.That(doctorTeleport, Is.Not.Null);
      Assert.That(doctorTeleport.Mode, Is.EqualTo(ScenarioNPCControlMode.Control));
      Assert.That(doctorTeleport.MoveMode, Is.EqualTo(ScenarioMoveMode.Instant),
        "준비 체인은 건너뛴 구간을 메우는 자리라 걸어가지 않고 즉시 배치한다.");
      Assert.That(doctorTeleport.NPCIdentifier, Is.EqualTo(doctorMove.NPCIdentifier));
      Assert.That(doctorTeleport.DestinationType, Is.EqualTo(doctorMove.DestinationType));
      Assert.That(doctorTeleport.DestinationIdentifier, Is.EqualTo(doctorMove.DestinationIdentifier));
      Assert.That(doctorTeleport.NextIdentifier, Is.EqualTo("SETUP_CARE_RETURN"));
      Assert.That(graph.Nodes["SETUP_CARE_RETURN"], Is.TypeOf<ScenarioReturnToOriginNode>());
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

    [TestCase("patient_b")]
    [TestCase("patient_c")]
    public void PatientBCIvInteractionOpensForBothTreatmentPatients(string identifier)
    {
      var patientObject = new GameObject(identifier);
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier(identifier);
        patient.IntravenousLineCannulaSupported = true;

        patient.ActivatePatientBCNurseCStage();
        Assert.That(patient.IntravenousLineCannulaInteractable, Is.True,
          "여성 환자도 남성 환자와 같이 정맥로 확보 처치를 받는다.");
        Assert.That(patient.CanInteractIntravenousLineCannula, Is.False,
          "동공반사를 확인하기 전에는 정맥로 확보를 노출하지 않는다.");
        InvokePrivate(patient, "NotifyPatientBCPupilCompleted");
        Assert.That(patient.CanInteractIntravenousLineCannula, Is.True);
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PatientBCIvInteractionRemainsAvailableAfterMissingCannulaRetry()
    {
      var patientObject = new GameObject("patient_b");
      var playerObject = new GameObject("nurse-a");
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");
        patient.IntravenousLineCannulaSupported = true;
        patient.ActivatePatientBCNurseCStage();
        InvokePrivate(patient, "NotifyPatientBCPupilCompleted");

        var player = playerObject.AddComponent<MultiplayerInfrastructure.Player.PlayerController>();
        Assert.That(player.CountItemInInventory("cannula_20g"), Is.Zero);

        InvokePrivate(patient, "PerformIntravenousLineCannulaInsertion", player.transform);
        InvokePrivate(patient, "PerformIntravenousLineCannulaInsertion", player.transform);

        Assert.That(patient.CanInteractIntravenousLineCannula, Is.True,
          "캐뉼라가 없는 시도는 정맥로 단계를 소비하거나 이후 재시도를 막으면 안 됩니다.");
      }
      finally
      {
        Object.DestroyImmediate(playerObject);
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

    [TestCase("patient_b", "nurse_a")]
    [TestCase("patient_c", "nurse_b")]
    public void PatientBCPrimaryTreatmentUsesTheAssignedRole(
      string patientIdentifier,
      string expectedRoleTag)
    {
      var patientObject = new GameObject(patientIdentifier);
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier(patientIdentifier);

        Assert.That(
          GetPrivateProperty<string>(patient, "PatientBCPrimaryTreatmentRoleTag"),
          Is.EqualTo(expectedRoleTag));
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [TestCase("patient_b", "nurse_c")]
    [TestCase("patient_c", "nurse_d")]
    public void PatientBCSecondaryTreatmentUsesTheAssignedRole(
      string patientIdentifier,
      string expectedRoleTag)
    {
      var patientObject = new GameObject(patientIdentifier);
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier(patientIdentifier);

        Assert.That(
          GetPrivateProperty<string>(patient, "PatientBCSecondaryTreatmentRoleTag"),
          Is.EqualTo(expectedRoleTag));
      }
      finally
      {
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
        var field = FindInstanceField(typeof(PatientController), fieldName);
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
          OverworldGameObjectInitializer.TriageArrivalEnterSignals,
          OverworldGameObjectInitializer.TriageArrivalPerEntitySignalTemplate,
          true);
        AssertSignalZone(
          zones,
          OverworldGameObjectInitializer.CtPatientBTargetPositionWaypointIdentifier,
          OverworldGameObjectInitializer.CtPatientArrivalEnterSignals,
          OverworldGameObjectInitializer.CtPatientArrivalPerEntitySignalTemplate,
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
    public void DefaultDoctorRouteUsesOrderedGroundedWaypoints()
    {
      var route = OverworldGameObjectInitializer.DefaultDoctorRouteWaypointSet;
      Assert.That(route.identifier, Is.EqualTo(OverworldGameObjectInitializer.DoctorRouteWaypointSetIdentifier));
      Assert.That(route.waypoints.Select(waypoint => waypoint.position), Is.EqualTo(new[]
      {
        new Vector3(-83f, 1f, -32f),
        new Vector3(-77f, 1f, -32f),
        new Vector3(-68f, 1f, -32f),
        new Vector3(-68f, 1f, -17.5f),
      }));
      Assert.That(route.waypoints.All(
        waypoint => waypoint.position.y >= 0f && waypoint.position.y <= 2.7f), Is.True);
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
      var serializedBed = new SerializedObject(bed);
      var participantAttachPoints = serializedBed.FindProperty("_playerAttachPoints");
      Assert.That(participantAttachPoints, Is.Not.Null);
      Assert.That(participantAttachPoints.arraySize, Is.EqualTo(4));
      Assert.That(prefab.GetComponentsInChildren<RidableAttachPointObject>(true), Has.Length.EqualTo(4));

      var allowedProperty = serializedBed.FindProperty("_allowedPositioningPointIdentifiers");
      var allowedIdentifiers = Enumerable.Range(0, allowedProperty.arraySize)
        .Select(index => allowedProperty.GetArrayElementAtIndex(index).stringValue)
        .ToArray();

      Assert.That(allowedIdentifiers, Is.EquivalentTo(layoutIdentifiers));
      Assert.That(allowedIdentifiers, Has.Length.EqualTo(4));
    }

    [Test]
    public void MovingBedPositioningPointReleasesParticipantsOnSnapByDefault()
    {
      var pointObject = new GameObject("MovingBedPositioningPointDefaultTest");
      try
      {
        var point = pointObject.AddComponent<MovingPatientBedPositioningPoint>();
        var serializedPoint = new SerializedObject(point);
        var releaseParticipants = serializedPoint.FindProperty("_releaseParticipantsOnSnap");

        Assert.That(releaseParticipants, Is.Not.Null,
          "the per-positioning-point auto-release option must remain serialized");
        Assert.That(point.ReleaseParticipantsOnSnap, Is.True);

        releaseParticipants.boolValue = false;
        serializedPoint.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(point.ReleaseParticipantsOnSnap, Is.False,
          "each positioning point must be able to disable automatic release");
      }
      finally
      {
        Object.DestroyImmediate(pointObject);
      }
    }

    [Test]
    public void MovingBedSyncedPositioningPointIdentifierResolvesLocalReference()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MovingBedPrefabPath);
      var bedObject = Object.Instantiate(prefab, new Vector3(12000f, 0f, 12000f), Quaternion.identity);
      var pointObject = new GameObject("MovingBedSyncedPointResolutionTest");
      const string pointIdentifier = "synced_positioning_point_test";
      try
      {
        var bed = bedObject.GetComponent<MovingPatientBedController>();
        var point = pointObject.AddComponent<MovingPatientBedPositioningPoint>();
        point.SetIdentifier(pointIdentifier);
        var apply = typeof(MovingPatientBedController).GetMethod(
          "OnPositioningPointIdentifierChanged",
          BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(apply, Is.Not.Null);
        apply.Invoke(bed, new object[] { string.Empty, pointIdentifier, false });
        Assert.That(bed.LatchedPositioningPoint, Is.SameAs(point));

        apply.Invoke(bed, new object[] { pointIdentifier, string.Empty, false });
        Assert.That(bed.LatchedPositioningPoint, Is.Null);
      }
      finally
      {
        Object.DestroyImmediate(pointObject);
        Object.DestroyImmediate(bedObject);
      }
    }

    [Test]
    public void MovingBedSnapReleasesPartialOccupancyBeforePublishingReachedSignal()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MovingBedPrefabPath);
      var bedObject = Object.Instantiate(prefab, new Vector3(10000f, 0f, 10000f), Quaternion.identity);
      var pointObject = new GameObject("MovingBedAutoReleaseSnapTest");
      pointObject.transform.SetPositionAndRotation(bedObject.transform.position, bedObject.transform.rotation);
      const string pointIdentifier = "auto_release_snap_test";
      const string reachedSignal = "sig.patient_bed_position_reached_auto_release_snap_test";
      try
      {
        var bed = bedObject.GetComponent<MovingPatientBedController>();
        var point = pointObject.AddComponent<MovingPatientBedPositioningPoint>();
        point.SetIdentifier(pointIdentifier);
        bed.EnsureAllowedPositioningPointIdentifier(pointIdentifier);
        AddOfflineParticipantForTest(bed, 101);

        bool? hadParticipantsWhenReachedWasPublished = null;
        void Capture(string signal)
        {
          if (signal == reachedSignal)
            hadParticipantsWhenReachedWasPublished = bed.HasParticipants;
        }

        ScenarioInteractionSignals.Clear(reachedSignal);
        ScenarioInteractionSignals.Clear(bed.DismountCompletionSignal);
        ScenarioInteractionSignals.OnSignalRegistered += Capture;
        try
        {
          var snap = typeof(MovingPatientBedController).GetMethod(
            "TrySnapToPositioningPointWithSignalContext",
            BindingFlags.Instance | BindingFlags.NonPublic);
          Assert.That(snap, Is.Not.Null);
          snap.Invoke(bed, null);
        }
        finally
        {
          ScenarioInteractionSignals.OnSignalRegistered -= Capture;
        }

        Assert.That(bed.HasParticipants, Is.False);
        Assert.That(hadParticipantsWhenReachedWasPublished, Is.False,
          "snap observers must see the fully detached state");
        Assert.That(ScenarioInteractionSignals.IsRaised(bed.DismountCompletionSignal), Is.True,
          "forced release must complete even when occupancy is below bed capacity");
      }
      finally
      {
        ScenarioInteractionSignals.Clear(reachedSignal);
        Object.DestroyImmediate(pointObject);
        Object.DestroyImmediate(bedObject);
      }
    }

    [Test]
    public void MovingBedSnapKeepsParticipantsWhenPointAutoReleaseIsDisabled()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MovingBedPrefabPath);
      var bedObject = Object.Instantiate(prefab, new Vector3(11000f, 0f, 11000f), Quaternion.identity);
      var pointObject = new GameObject("MovingBedDisabledAutoReleaseSnapTest");
      pointObject.transform.SetPositionAndRotation(bedObject.transform.position, bedObject.transform.rotation);
      const string pointIdentifier = "disabled_auto_release_snap_test";
      try
      {
        var bed = bedObject.GetComponent<MovingPatientBedController>();
        var point = pointObject.AddComponent<MovingPatientBedPositioningPoint>();
        point.SetIdentifier(pointIdentifier);
        var serializedPoint = new SerializedObject(point);
        serializedPoint.FindProperty("_releaseParticipantsOnSnap").boolValue = false;
        serializedPoint.ApplyModifiedPropertiesWithoutUndo();
        bed.EnsureAllowedPositioningPointIdentifier(pointIdentifier);
        AddOfflineParticipantForTest(bed, 102);

        var snap = typeof(MovingPatientBedController).GetMethod(
          "TrySnapToPositioningPointWithSignalContext",
          BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(snap, Is.Not.Null);
        snap.Invoke(bed, null);

        Assert.That(bed.HasParticipants, Is.True);
      }
      finally
      {
        Object.DestroyImmediate(pointObject);
        Object.DestroyImmediate(bedObject);
      }
    }

    private static void AddOfflineParticipantForTest(MinecraftBoatLikeControl controller, int key)
    {
      var participantType = typeof(MinecraftBoatLikeControl).GetNestedType(
        "LocalParticipant", BindingFlags.NonPublic);
      var participantsField = typeof(MinecraftBoatLikeControl).GetField(
        "_localParticipants", BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(participantType, Is.Not.Null);
      Assert.That(participantsField, Is.Not.Null);

      object participant = System.Activator.CreateInstance(participantType, nonPublic: true);
      var participants = participantsField.GetValue(controller) as System.Collections.IDictionary;
      Assert.That(participants, Is.Not.Null);
      participants.Add(key, participant);
    }

    [Test]
    public void MovingBedToggleAcknowledgementClearsPendingRequestAfterServerRejection()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MovingBedPrefabPath);
      var instance = Object.Instantiate(prefab);
      try
      {
        var bed = instance.GetComponent<MovingPatientBedController>();
        var pending = typeof(MinecraftBoatLikeControl).GetField(
          "_togglePending", BindingFlags.Instance | BindingFlags.NonPublic);
        var acknowledge = typeof(MinecraftBoatLikeControl).GetMethod(
          "CompleteToggleRequest", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(pending, Is.Not.Null);
        Assert.That(acknowledge, Is.Not.Null);
        pending.SetValue(bed, true);
        acknowledge.Invoke(bed, null);
        Assert.That(pending.GetValue(bed), Is.False);
      }
      finally
      {
        Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void ExplicitRidableAttachPointListIsNotExpandedByChildDiscovery()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MovingBedPrefabPath);
      var instance = Object.Instantiate(prefab);
      try
      {
        var bed = instance.GetComponent<MovingPatientBedController>();
        var attachPointsField = typeof(MinecraftBoatLikeControl).GetField(
          "_playerAttachPoints", BindingFlags.Instance | BindingFlags.NonPublic);
        var resolveAttachPoints = typeof(MinecraftBoatLikeControl).GetMethod(
          "ResolveAttachPoints", BindingFlags.Instance | BindingFlags.NonPublic);
        var attachPoints = attachPointsField?.GetValue(bed) as List<Transform>;
        var explicitPoint = instance.GetComponentsInChildren<RidableAttachPointObject>(true).First().transform;

        Assert.That(attachPoints, Is.Not.Null);
        Assert.That(resolveAttachPoints, Is.Not.Null);
        attachPoints.Clear();
        attachPoints.Add(explicitPoint);
        resolveAttachPoints.Invoke(bed, null);

        Assert.That(attachPoints, Is.EqualTo(new[] { explicitPoint }));
      }
      finally
      {
        Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void MovingBedForceReleaseHasNoUnownedClientRpcEntryPoint()
    {
      Assert.That(typeof(MovingPatientBedController).GetMethod(
        "CmdForceReleaseAllParticipants", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
      Assert.That(typeof(MovingPatientBedController).GetMethod(
        "RpcForceReleaseLocalParticipants", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
    }

    [Test]
    public void DetachBedResolutionWaitHasFiniteTimeout()
    {
      var timeout = typeof(TriageScenarioEventBootstrap).GetField(
        "DetachBedResolutionTimeoutSeconds", BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(timeout, Is.Not.Null);
      Assert.That((float)timeout.GetRawConstantValue(), Is.GreaterThan(0f));
    }

    [Test]
    public void AttachBedResolutionWaitHasFiniteTimeout()
    {
      var timeout = typeof(TriageScenarioEventBootstrap).GetField(
        "AttachPatientBedPairsResolutionTimeoutSeconds", BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(timeout, Is.Not.Null);
      Assert.That((float)timeout.GetRawConstantValue(), Is.GreaterThan(0f));
    }

    [Test]
    public void MovingBedDisconnectCleanupReleasesSlotAndRemovesCachedInput()
    {
      const int disconnectedClientId = 17;
      var participants = new List<int> { 8, disconnectedClientId, -1 };
      var inputs = new Dictionary<int, Vector2>
      {
        [8] = Vector2.left,
        [disconnectedClientId] = Vector2.one
      };
      var remove = typeof(MinecraftBoatLikeControl).GetMethod(
        "RemoveParticipantState", BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(remove, Is.Not.Null);
      remove.Invoke(null, new object[] { participants, inputs, disconnectedClientId });

      Assert.That(participants, Is.EqualTo(new[] { 8, -1, -1 }));
      Assert.That(inputs.ContainsKey(disconnectedClientId), Is.False);
      Assert.That(inputs.ContainsKey(8), Is.True);
    }

    [Test]
    public void DetachBedParticipantsReportsWhetherControllerWasResolved()
    {
      var detach = typeof(TriageScenarioEventBootstrap).GetMethod(
        "DetachBedParticipants", BindingFlags.Static | BindingFlags.NonPublic);
      var missing = new GameObject("MissingMovingBedController");
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MovingBedPrefabPath);
      var instance = Object.Instantiate(prefab);
      try
      {
        Assert.That(detach, Is.Not.Null);
        Assert.That(detach.Invoke(null, new object[] { missing }), Is.False);
        Assert.That(detach.Invoke(null, new object[] { instance }), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(missing);
      }
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
        // 활성 확인 항목은 이제 완료 신호로 구분한다. 역할 태그를 비우면 역할을 제한하지 않는다.
        patient.ActivateRecognitionCheck(RecognitionDistanceProbeSignal, false, string.Empty);

        var complete = typeof(PatientController).GetMethod(
          "TryCompleteRecognitionCheckAuthoritative",
          BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(complete, Is.Not.Null);
        Assert.That(
          complete.Invoke(patient, new object[] { player, false, RecognitionDistanceProbeSignal }),
          Is.EqualTo(expected));
        Assert.That(IsRecognitionCheckActive(patient, RecognitionDistanceProbeSignal), Is.EqualTo(!expected));
      }
      finally
      {
        ScenarioInteractionSignals.Clear(RecognitionDistanceProbeSignal);
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    private const string RecognitionDistanceProbeSignal = "recognition_distance_probe";

    private static bool IsRecognitionCheckActive(PatientController patient, string completionSignal)
    {
      var indexOf = typeof(PatientController).GetMethod(
        "IndexOfRecognitionCheck",
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(indexOf, Is.Not.Null);
      return (int)indexOf.Invoke(patient, new object[] { completionSignal }) >= 0;
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
      var parameters = method.GetParameters();
      var arguments = new object[parameters.Length];
      for (int i = 0; i < parameters.Length; i++)
      {
        Assert.That(parameters[i].IsOptional, Is.True,
          $"{methodName} parameter '{parameters[i].Name}' requires an explicit test value.");
        arguments[i] = System.Type.Missing;
      }
      return (T)method.Invoke(patient, arguments);
    }

    private static void InvokePrivate(PatientController patient, string methodName, object argument = null)
    {
      var method = typeof(PatientController).GetMethod(
        methodName,
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null, methodName);
      method.Invoke(patient, argument == null ? null : new[] { argument });
    }

    private static void InvokePrivate(
      PatientController patient,
      string methodName,
      object firstArgument,
      object secondArgument)
    {
      var method = typeof(PatientController).GetMethod(
        methodName,
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null, methodName);
      method.Invoke(patient, new[] { firstArgument, secondArgument });
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
      var field = FindInstanceField(target.GetType(), fieldName);
      Assert.That(field, Is.Not.Null, fieldName);
      field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
      var field = FindInstanceField(target.GetType(), fieldName);
      Assert.That(field, Is.Not.Null, fieldName);
      return (T)field.GetValue(target);
    }

    private static T GetPrivateProperty<T>(object target, string propertyName)
    {
      var property = target.GetType().GetProperty(
        propertyName,
        BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(property, Is.Not.Null, propertyName);
      return (T)property.GetValue(target);
    }

    private static FieldInfo FindInstanceField(System.Type type, string fieldName)
    {
      for (System.Type current = type; current != null; current = current.BaseType)
      {
        var field = current.GetField(fieldName,
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        if (field != null)
          return field;
      }
      return null;
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
      var field = FindInstanceField(patient.GetType(), fieldName);
      Assert.That(field, Is.Not.Null);
      var syncVar = field.GetValue(patient);
      var valueProperty = syncVar.GetType().GetProperty("Value");
      Assert.That(valueProperty, Is.Not.Null);
      valueProperty.SetValue(syncVar, value);
    }

    private static T GetSyncVarValue<T>(PatientController patient, string fieldName)
    {
      var field = FindInstanceField(patient.GetType(), fieldName);
      Assert.That(field, Is.Not.Null);
      var syncVar = field.GetValue(patient);
      var valueProperty = syncVar.GetType().GetProperty("Value");
      Assert.That(valueProperty, Is.Not.Null);
      return (T)valueProperty.GetValue(syncVar);
    }
  }
}
