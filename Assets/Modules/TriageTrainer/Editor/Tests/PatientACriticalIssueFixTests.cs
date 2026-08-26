using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
      "Assets/Modules/TriageTrainer/Prefabs/Entities/MinecraftBoatLikes/level1_rapid_infuser.prefab";
    private const string PatientAScenarioPath =
      "Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json";
    private const string PatientAScenarioGlob = "patient_a_critical*.scenario.json";
    private const string PatientAScenarioSourcePath =
      "Documents/requirements/content-definitions/scenario/patient_a_critical.md";

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
                   "defibrillatorpad_midaxillary_A",
                   "defibrillatorpad_subclavicle_A",
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

    [Test]
    public void PatientAPrefabProvidesDedicatedRightArmIvConnectionPoint()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientAPrefabPath);
      Assert.That(prefab, Is.Not.Null);

      var points = prefab.GetComponentsInChildren<
        TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint>(true);
      Assert.That(Array.Exists(points,
        point => point != null && point.Identifier == "patient_a_cannula_right_port"), Is.True);
    }

    [Test]
    public void PatientAScenarioWaitsForRightArmPlasmaAndCLineConnections()
    {
      string json = File.ReadAllText(PatientAScenarioPath);
      StringAssert.Contains("\"registryIdentifier\": \"sig.connect_ps1_right\"", json);
      StringAssert.Contains("\"registryIdentifier\": \"sig.connect_cline_to_lv1\"", json);
      StringAssert.IsMatch("(?s)\"E021\".*?\"nextIdentifier\": \"V017_4\"", json);
      StringAssert.IsMatch("(?s)\"V017_4\".*?\"nextIdentifier\": \"E022\"", json);
      StringAssert.IsMatch("(?s)\"V020\".*?\"nextIdentifier\": \"N014_1\"", json);
      StringAssert.IsMatch("(?s)\"V020_1\".*?\"nextIdentifier\": \"E024\"", json);
      StringAssert.IsMatch("(?s)\"D019\".*?@t=\\[nurse_d, @s\\]", json);
      StringAssert.IsMatch("(?s)\"D020\".*?@t=\\[nurse_d, @s\\]", json);
      StringAssert.IsMatch("(?s)\"D021\".*?\"speakerName\": \"@s\"", json);
    }

    [TestCase("PlasmaSolution", "sig.connect_ps1_to_lv1")]
    [TestCase("BloodBag", "sig.connect_blood_to_lv1")]
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
    [TestCase("BloodBag", "blood_bag")]
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
    public void RapidInfuserRejectsLegacyBloodTransfusionSetItem()
    {
      Assert.That(IsRapidInfuserItemAccepted("BloodBag", "blood_transfusion_set"), Is.False);
    }

    [Test]
    public void RapidInfuserRequiresPlasmaBeforeBloodBag()
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
        var kind = GetRapidInfuserKind("BloodBag");

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
    public void RapidInfuserCanPassBloodBagWithoutPlasmaWhenConfigured()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RapidInfuserPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = UnityEngine.Object.Instantiate(prefab);
      try
      {
        var controller = instance.GetComponent<Level1RapidInfuserController>();
        var policyField = typeof(Level1RapidInfuserController).GetField(
          "_bloodBagWithoutPlasmaPolicy", BindingFlags.Instance | BindingFlags.NonPublic);
        var canAdd = typeof(Level1RapidInfuserController).GetMethod(
          "CanAddFluid", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(policyField, Is.Not.Null);
        Assert.That(canAdd, Is.Not.Null);
        policyField.SetValue(controller, BloodBagWithoutPlasmaPolicy.Pass);

        Assert.That((bool)canAdd.Invoke(controller, new[] { GetRapidInfuserKind("BloodBag") }), Is.True);
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void RapidInfuserAllowsBloodAttemptBeforePlasmaSoCancellationCanBeReported()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RapidInfuserPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = UnityEngine.Object.Instantiate(prefab);
      try
      {
        var controller = instance.GetComponent<Level1RapidInfuserController>();
        var canAttempt = typeof(Level1RapidInfuserController).GetMethod(
          "CanAttemptFluid", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(canAttempt, Is.Not.Null);
        Assert.That((bool)canAttempt.Invoke(controller, new[] { GetRapidInfuserKind("BloodBag") }), Is.True);
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void RapidInfuserCaptureStateIncludesBloodBag()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RapidInfuserPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = UnityEngine.Object.Instantiate(prefab);
      try
      {
        var controller = instance.GetComponent<Level1RapidInfuserController>();
        typeof(Level1RapidInfuserController).GetField(
            "_initialHasBloodBag", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(controller, true);

        Assert.That(controller.CaptureState().HasBloodBag, Is.True);
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
    public void RapidInfuserPrefabWiresIvConnectionPointAndFluidDisplays()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RapidInfuserPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = UnityEngine.Object.Instantiate(prefab);
      try
      {
        var controller = instance.GetComponent<Level1RapidInfuserController>();
        Assert.That(controller, Is.Not.Null);

        var controllerType = typeof(Level1RapidInfuserController);
        var ivPoint = controllerType.GetField("_ivConnectionPoint", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.GetValue(controller) as TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint;
        Assert.That(ivPoint, Is.Not.Null,
          "level1_rapid_infuser 프리팹에 _ivConnectionPoint 가 배선되어야 합니다(환자 라인 연결).");
        Assert.That(ivPoint.GetComponent<SphereCollider>(), Is.Not.Null,
          "IV 연결 지점에 접근 가능한 SphereCollider 가 있어야 합니다.");
        Assert.That(ivPoint.GetComponent<SphereCollider>().isTrigger, Is.True);

        foreach (var fieldName in new[] { "_normalSalineDisplay", "_plasmaSolutionDisplay", "_bloodBagDisplay" })
        {
          var display = controllerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(controller) as GameObject;
          Assert.That(display, Is.Not.Null, $"{fieldName} 가 프리팹에 배선되어야 합니다.");
          Assert.That(display.activeSelf, Is.False,
            $"{fieldName} 표시 오브젝트는 기본 비활성이어야 합니다(유체 추가 시 ApplyDisplays 가 표시).");
        }
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void GloveEquipViaRightClickRaisesWearGloveSignal()
    {
      var playerObject = new GameObject("glove-equip-player");
      var hotbarObject = new GameObject("glove-equip-hotbar");
      var raised = new List<string>();
      void Capture(string signal) => raised.Add(signal);
      ScenarioInteractionSignals.OnSignalRegistered += Capture;
      try
      {
        var player = playerObject.AddComponent<MultiplayerInfrastructure.Player.PlayerController>();
        var hotbar = hotbarObject.AddComponent<MultiplayerInfrastructure.UI.HotbarUIController>();

        var gloveItem = (MultiplayerInfrastructure.ItemSystem.Item)Activator.CreateInstance(
          typeof(TriageTrainer.ItemDefinitions.SterileGloves));
        gloveItem.CurrentStackCount = 1;

        var heldSlot = new InventorySlotModelDTO();
        heldSlot.SetItem(gloveItem);
        var slots = new List<InventorySlotModelDTO> { heldSlot };
        var equipmentSlots = new List<MultiplayerInfrastructure.Player.EquipmentSlotModelDTO>
        {
          new MultiplayerInfrastructure.Player.EquipmentSlotModelDTO(
            MultiplayerInfrastructure.Player.EquipmentSlotType.Glove)
        };

        var playerType = typeof(MultiplayerInfrastructure.Player.PlayerController);
        playerType.GetField("_slots", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(player, slots);
        playerType.GetField("_equipmentSlots", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(player, equipmentSlots);
        playerType.GetField("_hotbarUI", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(player, hotbar);
        player.HandlingItem = gloveItem;

        ScenarioInteractionSignals.Clear("wear_glove");
        var equip = playerType.GetMethod(
          "TryEquipHandlingItem", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(equip, Is.Not.Null);
        bool equipped = (bool)equip.Invoke(
          player, new object[] { MultiplayerInfrastructure.Player.EquipmentSlotType.Glove });

        Assert.That(equipped, Is.True, "빈 장갑 슬롯에 장갑 장착이 성공해야 합니다.");
        Assert.That(equipmentSlots[0].IsEmpty, Is.False, "장갑 슬롯에 아이템이 있어야 합니다.");
        Assert.That(raised, Does.Contain("sig.wear_glove"),
          "장갑 장착 성공 시 sig.wear_glove 신호가 1회 올라와야 합니다(V016_1 게이트).");
      }
      finally
      {
        ScenarioInteractionSignals.OnSignalRegistered -= Capture;
        ScenarioInteractionSignals.Clear("wear_glove");
        UnityEngine.Object.DestroyImmediate(hotbarObject);
        UnityEngine.Object.DestroyImmediate(playerObject);
      }
    }

    [Test]
    public void PatientATPieceUsesOxygenLineAndRequiredItems()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientAPrefabPath);
      Assert.That(prefab, Is.Not.Null);

      var tPiecePort = Array.Find(
        prefab.GetComponentsInChildren<Transform>(true),
        each => each != null && each.name == "TPieceLinePort");
      Assert.That(tPiecePort, Is.Not.Null);
      Assert.That(
        tPiecePort.GetComponent<TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint>(),
        Is.Not.Null,
        "T-piece는 수액 연결점이 아니라 산소줄 연결점을 사용해야 합니다.");
      Assert.That(
        tPiecePort.GetComponent<TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionPoint>(),
        Is.Null);
      Assert.That(
        tPiecePort.GetComponent<TriageTrainer.Entity.OxyLine.OxyLinePairInteractable>(),
        Is.Not.Null);

      var scenarioActions = prefab.GetComponentsInChildren<ScenarioActionInteractable>(true);
      var tPieceAction = Array.Find(
        scenarioActions,
        each => each != null && each.CompletionSignal == "interact_tpiece");
      Assert.That(tPieceAction, Is.Not.Null);
      Assert.That(typeof(ScenarioActionInteractable).GetField(
          "_requiredItemIdentifier", BindingFlags.Instance | BindingFlags.NonPublic)
        ?.GetValue(tPieceAction), Is.EqualTo("tpiece_set"));
      Assert.That(typeof(ScenarioActionInteractable).GetField(
          "_consumeRequiredItemCount", BindingFlags.Instance | BindingFlags.NonPublic)
        ?.GetValue(tPieceAction), Is.EqualTo(1));
    }

    [Test]
    public void PatientAP004SeparatesIntubationAndOxygenBranches()
    {
      string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
        "Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json");
      string json = File.ReadAllText(path);
      int p004Start = json.IndexOf("\"P004\":", StringComparison.Ordinal);
      int p005Start = json.IndexOf("\"P005\":", p004Start, StringComparison.Ordinal);
      Assert.That(p004Start, Is.GreaterThanOrEqualTo(0));
      Assert.That(p005Start, Is.GreaterThan(p004Start));
      string p004 = json.Substring(p004Start, p005Start - p004Start);

      StringAssert.Contains("CC_B_intubation_patient_a", p004);
      StringAssert.Contains("CC_A_oxygen_patient_a", p004);
      StringAssert.Contains("CC_C_stopbleeding_patient_a", p004);
      StringAssert.Contains("CC_D_iv_patient_a", p004);
      StringAssert.Contains("\"identifier\": \"Q011\"", p004);
      StringAssert.Contains("patient_a_intubation_complete", json);
      StringAssert.Contains("\"operation\": \"Resolve\"", json);
      StringAssert.Contains("\"operation\": \"Register\"", json);
      StringAssert.DoesNotContain("\"eventIdentifier\": \"insert_et_tube\"", json);
      StringAssert.DoesNotContain("\"eventIdentifier\": \"remove_stylet\"", json);

      string treatmentSignalSource = File.ReadAllText(Path.Combine(
        Directory.GetParent(Application.dataPath).FullName,
        "Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.PatientATreatmentSignals.cs"));
      StringAssert.Contains("sig.pass_et_tube_ready", treatmentSignalSource);
      StringAssert.Contains("sig.remove_intu_stylet", treatmentSignalSource);
      StringAssert.Contains("endotracheal_tube_stylet_inserted", treatmentSignalSource);
      StringAssert.Contains("endotracheal_tube_insert_done", treatmentSignalSource);
    }

    [Test]
    public void PatientAStartWaitsForEveryActiveNurseArrival()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string scenario = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json"));
      string quests = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Resources/Quest/patient_a_critical.quests.quest.json"));

      StringAssert.Contains("\"sourceSignalPrefix\": \"quest_arrival_patient_a_\"", scenario);
      StringAssert.Contains("\"useActiveRoleRosterThreshold\": true", scenario);
      StringAssert.Contains("\"outputSignalIdentifier\": \"all_nurses_arrived_patient_a\"", scenario);
      StringAssert.Contains("\"identifier\": \"P_START_A\"", scenario);
      StringAssert.Contains("\"identifier\": \"P_ARRIVAL_A\"", scenario);
      foreach (string role in new[] { "nurse_a", "nurse_b", "nurse_c", "nurse_d" })
        StringAssert.Contains($"\"requiredPlayerTags\": [\"{role}\"]", scenario);

      StringAssert.Contains("\"identifier\": \"Quest_Arrive_PatientA\"", quests);
      StringAssert.Contains("\"waypointIdentifier\": \"scen_a:quest_arrival_patient_a\"", quests);
      StringAssert.Contains("\"type\": \"WaypointReached\"", quests);
    }

    [Test]
    public void PatientA636GraphLoadsWithSchemaValidation()
    {
      string path = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);

      Assert.That(graph.DefaultEntrypoint, Is.EqualTo("SPAWN_A"));
      Assert.That(graph.ClientSignalPrefixes,
        Does.Contain("sig.quest_arrival_patient_a_"));
      Assert.That(graph.Nodes, Contains.Key("COUNT_ARRIVAL_A"));
      Assert.That(graph.Nodes, Contains.Key("P_START_A"));
      Assert.That(graph.Nodes, Contains.Key("P_ARRIVAL_A"));
    }

    [Test]
    public void PatientA636RequiredItemsAreCheckedAtTheirInteractions()
    {
      var patientPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientAPrefabPath);
      var patient = patientPrefab.GetComponent<PatientController>();
      var assessActions = typeof(PatientController).GetField(
          "_assessActions", BindingFlags.Instance | BindingFlags.NonPublic)
        ?.GetValue(patient) as System.Collections.IEnumerable;
      object vitalAssess = null;
      foreach (object action in assessActions ?? Array.Empty<object>())
      {
        string identifier = action?.GetType().GetProperty("Identifier")?.GetValue(action) as string;
        if (identifier == "assess_vital")
          vitalAssess = action;
      }
      Assert.That(vitalAssess, Is.Not.Null);
      Assert.That(vitalAssess.GetType().GetProperty("RequiredItemIdentifier")?.GetValue(vitalAssess),
        Is.EqualTo("vital_set"));

      string suctionSource = File.ReadAllText(Path.Combine(
        Directory.GetParent(Application.dataPath).FullName,
        "Assets/Modules/TriageTrainer/Scripts/Entities/WallAttachedWallSuction/WallAttachedWallSuction.cs"));
      StringAssert.Contains("yankauer_suction_ready", suctionSource);
      StringAssert.Contains("connect_wall_component_and_yankauer", suctionSource);
      StringAssert.Contains("LineRenderer", suctionSource);
      StringAssert.Contains("DisconnectYankauer", suctionSource);
    }

    [Test]
    public void PatientA636NarrativeMilestonesArePresentWithoutLegacyPlaybackEvents()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string scenario = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json"));
      string quests = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Resources/Quest/patient_a_critical.quests.quest.json"));

      foreach (string expected in new[]
               {
                 "scen_a:patient_spawnpoint_a",
                 "scen_a:doctor_treatment_room_waypoint",
                 "흉부 관통상 환자 한 명 이송. 처치실 담당자는 지금 바로 와주세요.",
                 "상태 확인부터 하겠습니다. @t=[nurse_b, ???]선생님은 활력징후 측정해 주시고, @t=[nurse_c, ???]선생님은 의식 상태 사정해 주세요.",
                 "AVPU 중 P이며, GCS는 E2 / V2 / M4로 총 8점입니다.",
                 "경추 고정, 구강 흡인 완료했습니다.",
                 "기도 확보를 위해 intubation을 시행하겠습니다. @t=[nurse_b, ???]선생님은 보조해주세요.",
                 "들어갔습니다. 스타일렛 빼주세요.",
                 "삽입된 깊이 23cm, 기관내관 고정되었습니다.",
                 "산소 투여 시작했습니다.",
                 "지혈 중입니다. 거즈 고정했습니다."
               })
        StringAssert.Contains(expected, scenario);

      foreach (string forbiddenEvent in new[]
               {
                 "\"eventIdentifier\": \"activate_vital_monitor_ui_patient_a\"",
                 "\"eventIdentifier\": \"vitalinfo_1_patient_a\"",
                 "\"eventIdentifier\": \"insert_et_tube\"",
                 "\"eventIdentifier\": \"remove_stylet\"",
                 "\"eventIdentifier\": \"connect_tpiece_ready\""
               })
        StringAssert.DoesNotContain(forbiddenEvent, scenario);

      foreach (string assessment in new[]
               {
                 "patient_a_initial_avpu",
                 "patient_a_initial_gcs_eye",
                 "patient_a_initial_gcs_verbal",
                 "patient_a_initial_gcs_motor",
                 "patient_a_oxygen_flow_lpm"
               })
        StringAssert.Contains(assessment, scenario);

      foreach (string taskSignal in new[]
               {
                 "show_vital_patient_a", "close_vital_ui_a", "check_avpu_gcs_patient_a",
                 "apply_stabilizer_patient_a", "connect_wall_component_1",
                 "connect_wall_component_and_yankauer", "suction_patient_a",
                 "pass_laryngoscope", "pass_et_tube_ready", "remove_intu_stylet",
                 "pass_syringe", "apply_plaster_on_intu", "connect_wall_component_2",
                 "interact_tpiece", "connect_tpiece_and_oxyflow", "interact_oxyflow_wall",
                 "wear_glove", "apply_gauze", "apply_plaster_on_gauze"
               })
        StringAssert.Contains($"\"signalId\": \"{taskSignal}\"", quests);
    }

    [Test]
    public void PatientA862PulseQuestTargetsFirstArrestPulseAssessment()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string quests = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Resources/Quest/patient_a_critical.quests.quest.json"));

      StringAssert.Contains("\"identifier\": \"Quest_Check_Pulse\"", quests);
      StringAssert.Contains("\"signalId\": \"check_pulse_patient_a_r1\"", quests);
      StringAssert.Contains("\"entityIdentifier\": \"patient_a\"", quests);
      StringAssert.Contains("\"interactionIdentifier\": \"assess_pulse_r1\"", quests);

      string scenario = File.ReadAllText(Path.Combine(projectRoot, PatientAScenarioPath));
      StringAssert.Contains("\"identifier\": \"arrest\"", scenario);
      StringAssert.Contains("\"nodeType\": \"ManualEntrypoint\"", scenario);
      StringAssert.IsMatch("(?s)\"P004\".*?\"nextIdentifier\": \"arrest\"", scenario);

      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientAPrefabPath);
      var patient = prefab.GetComponent<PatientController>();
      var actions = typeof(PatientController).GetField(
          "_assessActions", BindingFlags.Instance | BindingFlags.NonPublic)
        ?.GetValue(patient) as System.Collections.IEnumerable;
      PatientController.AssessActionConfig pulseAction = null;
      foreach (object action in actions ?? Array.Empty<object>())
      {
        if (action is PatientController.AssessActionConfig config && config.Identifier == "assess_pulse_r1")
          pulseAction = config;
      }

      Assert.That(pulseAction, Is.Not.Null);
      Assert.That(pulseAction.ActionDialogue, Is.EqualTo("(환자의 목에 손을 대고 경동맥을 촉지한다.)"));
      Assert.That(pulseAction.ResultDialogue, Is.EqualTo("(아무것도 느껴지지 않는다.)"));
    }

    [Test]
    public void PatientA862CrashEventPersistsPeaMonitorStateOnPatient()
    {
      var patientPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientAPrefabPath);
      var patientObject = UnityEngine.Object.Instantiate(patientPrefab);
      var bootstrapObject = new GameObject("patient-a-crash-event-test");
      try
      {
        var patient = patientObject.GetComponent<PatientController>();
        var bootstrap = bootstrapObject.AddComponent<TriageScenarioEventBootstrap>();
        typeof(TriageScenarioEventBootstrap).GetField(
            "_patientAObject", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(bootstrap, patientObject);
        var routine = typeof(TriageScenarioEventBootstrap).GetMethod(
            "Event_PatientCrashUi", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.Invoke(bootstrap, null) as System.Collections.IEnumerator;

        Assert.That(routine, Is.Not.Null);
        Assert.That(routine.MoveNext(), Is.True);
        var state = patient.MedicalState;
        float unavailable = TriageTrainer.Entity.Patient.PatientMedicalState.MonitorValueUnavailable;
        Assert.That(patient.MedicalStateIsCardiacArrest, Is.True);
        Assert.That(state.ecg.bpm, Is.EqualTo(80f));
        Assert.That(state.numerics.bpm, Is.EqualTo(80f));
        Assert.That(state.pleth.bpm, Is.EqualTo(unavailable));
        Assert.That(state.pleth.spo2, Is.EqualTo(unavailable));
        Assert.That(state.numerics.pulseRate, Is.EqualTo(unavailable));
        Assert.That(state.numerics.spo2, Is.EqualTo(unavailable));
        Assert.That(state.nibp.systolic, Is.EqualTo(unavailable));
        Assert.That(state.temperature.t1, Is.EqualTo(unavailable));
        Assert.That(state.art.bpm, Is.EqualTo(unavailable));
        Assert.That(state.cvp.mean, Is.EqualTo(unavailable));
        Assert.That(state.stLeads.ii, Is.EqualTo(unavailable));
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(bootstrapObject);
        UnityEngine.Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PatientAMedicationDialoguesStayAlignedWithSourceDocument()
    {
      string scenarioPath = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json");
      string json = File.ReadAllText(scenarioPath);

      // CPR 2주기 에피네프린 재투여 브랜치의 투여 보고 대사와 자동 진행 시간은
      // 줄글 시나리오 원본(투여 보고, 4초 대기)과 계속 일치해야 한다.
      string d031 = ExtractNode(json, "D031");
      string d0311 = ExtractNode(json, "D031_1");

      Assert.That(ExtractValue(d031, "speakerName"), Is.EqualTo("@s"));
      Assert.That(ExtractValue(d031, "dialogueContent"), Is.EqualTo("에피네프린 1mg 투여했습니다."));
      Assert.That(ExtractValue(d0311, "speakerName"), Is.EqualTo("@s"));
      Assert.That(ExtractValue(d0311, "dialogueContent"), Is.EqualTo("생리식염수 20cc 투여했습니다."));
      Assert.That(ExtractValue(d0311, "autoAdvanceSeconds"), Is.EqualTo("4.0"));

      string sourcePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
        PatientAScenarioSourcePath);
      Assert.That(File.Exists(sourcePath), Is.True, "Patient A 시나리오 원본 문서가 누락되었습니다.");
      string source = File.ReadAllText(sourcePath);
      StringAssert.Contains("에피네프린 1mg 투여했습니다.", source);
      StringAssert.Contains("생리식염수 20cc 투여했습니다.", source);
    }

    [Test]
    public void PatientARoscFollowupAssignsNurseRolesAndWaitQuestForNurseC()
    {
      string path = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);

      var p007 = (ScenarioParallelNode)graph.Nodes["P007"];
      var expectedRoles = new[]
      {
        ("N026", "nurse_a"),
        ("N027", "nurse_b"),
        ("Q_WAIT_ROSC_C", "nurse_c"),
        ("N028", "nurse_d")
      };
      Assert.That(p007.Branches, Has.Count.EqualTo(expectedRoles.Length),
        "ROSC 후속 조치는 nurse_a/b/c/d 네 역할 분기를 모두 정의해야 합니다.");
      foreach (var (branchIdentifier, role) in expectedRoles)
      {
        var branch = System.Array.Find(p007.Branches.ToArray(), each => each.Identifier == branchIdentifier);
        Assert.That(branch, Is.Not.Null, $"P007에는 {branchIdentifier} 분기가 있어야 합니다.");
        Assert.That(branch.RequiredPlayerTags, Does.Contain(role));
        Assert.That(branch.RequiredPlayerTagsMatchMode, Is.EqualTo(ScenarioPlayerTagMatchMode.All));
      }

      Assert.That(p007.NextIdentifier, Is.EqualTo("P_REMOVE_ROSC_WAIT"));

      // nurse_c 대기 퀘스트는 병렬 합류 뒤 nurse_c 클라이언트에서 제거된다.
      var removeWait = (ScenarioParallelNode)graph.Nodes["P_REMOVE_ROSC_WAIT"];
      Assert.That(removeWait.Branches, Has.Count.EqualTo(1));
      Assert.That(removeWait.Branches[0].RequiredPlayerTags, Does.Contain("nurse_c"));
      Assert.That(graph.Nodes["QC_ROSC_WAIT_C_REMOVE"], Is.InstanceOf<ScenarioQuestControlNode>());
      Assert.That(((ScenarioQuestControlNode)graph.Nodes["QC_ROSC_WAIT_C_REMOVE"]).QuestDefinitionIdentifier,
        Is.EqualTo("Quest_Wait_Others_Rosc_PatientA"));
      Assert.That(removeWait.NextIdentifier, Is.EqualTo("D037"));

      string questsPath = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Quest/patient_a_critical.quests.quest.json");
      StringAssert.Contains("\"identifier\": \"Quest_Wait_Others_Rosc_PatientA\"", File.ReadAllText(questsPath));
      StringAssert.Contains("\"title\": \"처치 정리\"", File.ReadAllText(questsPath));
    }

    [Test]
    public void PatientAP004IntubationAndIvBranchesDeclareAnyModeFallbackRoles()
    {
      string path = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);

      var p004 = (ScenarioParallelNode)graph.Nodes["P004"];
      var intubation = System.Array.Find(p004.Branches.ToArray(), each => each.Identifier == "Q010");
      Assert.That(intubation.RequiredPlayerTags, Is.EqualTo(new[] { "nurse_b", "nurse_a" }),
        "nurse_b 부재 시 nurse_a가 삽관 브랜치를 대신 수행해야 합니다.");
      Assert.That(intubation.RequiredPlayerTagsMatchMode, Is.EqualTo(ScenarioPlayerTagMatchMode.Any));

      var ivLine = System.Array.Find(p004.Branches.ToArray(), each => each.Identifier == "N011");
      Assert.That(ivLine.RequiredPlayerTags, Is.EqualTo(new[] { "nurse_d", "nurse_c" }),
        "nurse_d 부재 시 nurse_c가 IV 라인 브랜치를 대신 수행해야 합니다.");
      Assert.That(ivLine.RequiredPlayerTagsMatchMode, Is.EqualTo(ScenarioPlayerTagMatchMode.Any));
    }

    [Test]
    public void PatientATriageReturnGateIsScopedToTheNurseAHolder()
    {
      string scenarioPath = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(scenarioPath), validateWithSchema: true);

      // 퀘스트 수령자(nurse_a)가 아닌 다른 플레이어의 구역 진입으로 완료되지 않아야 하므로,
      // 게이트 앞에서 nurse_a 홀더 전용 도착 신호로 한정하는 이벤트를 실행한다.
      Assert.That(((ScenarioQuestControlNode)graph.Nodes["Q028"]).NextIdentifier,
        Is.EqualTo("PREPARE_TRIAGE_RETURN_A"));
      var armGate = (ScenarioInvokeEventNode)graph.Nodes["PREPARE_TRIAGE_RETURN_A"];
      Assert.That(armGate.EventIdentifier, Is.EqualTo("arm_patient_a_triage_return"));
      Assert.That(armGate.NextIdentifier, Is.EqualTo("V032"));

      var gate = (ScenarioValidatorNode)graph.Nodes["V032"];
      string json = File.ReadAllText(scenarioPath);
      StringAssert.Contains("\"registryIdentifier\": \"sig.arrive_triagearea_patient_a\"", json);
      StringAssert.DoesNotContain(
        "\"registryIdentifier\": \"sig.arrive_triagearea\"",
        json.Replace("\"registryIdentifier\": \"sig.arrive_triagearea_patient_a\"", string.Empty),
        "누구든 진입하면 통과되는 공용 신호 게이트가 남아 있으면 안 됩니다.");

      // 전용 서버에서 클라이언트가 올리는 플레이어별 도착 신호도 인가받아야 한다
      // (patient_b_c_ct 와 동일한 접두사 인가 규약).
      Assert.That(graph.ClientSignalPrefixes, Does.Contain("sig.quest_arrival_triage_area_"));

      Assert.That(typeof(TriageScenarioEventBootstrap).GetMethod(
        "Event_ArmPatientATriageReturn",
        BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null,
        "arm_patient_a_triage_return 이벤트 핸들러가 구현되어 있어야 합니다.");
    }

    [Test]
    public void PatientAEpiCDoctorDialoguePlaysBeforeIngredientGates()
    {
      string path = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);

      // 의사의 4분 경과 지시는 퀘스트 발행과 함께 먼저 재생되어야 한다(줄글 시나리오 1233~1236행).
      Assert.That(((ScenarioQuestControlNode)graph.Nodes["Q025"]).NextIdentifier, Is.EqualTo("D030"));
      Assert.That(((ScenarioDialogueNode)graph.Nodes["D030"]).NextIdentifier, Is.EqualTo("V029"));
      Assert.That(((ScenarioValidatorNode)graph.Nodes["V029_1"]).NextIdentifier, Is.EqualTo("N023_2"));
    }

    [Test]
    public void PatientALegacyInventoryCheckStagesStayRemovedFromGraphData()
    {
      string path = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json");
      string json = File.ReadAllText(path);

      // 줄글 시나리오 기술 노트로 제거된 단계들(측정도구 획득 대기 등)은 그래프 데이터에도 남지 않는다.
      foreach (string legacyIdentifier in new[]
               {
                 "V011", "V013", "V014", "V015", "V016",
                 "N005", "N005_4", "N006", "N007", "E007"
               })
        StringAssert.DoesNotContain($"\"identifier\": \"{legacyIdentifier}\"", json);
      StringAssert.DoesNotContain("\"registryIdentifier\": \"sig.click_vital_set\"", json);
      StringAssert.DoesNotContain("\"eventIdentifier\": \"show_suction_checklist_ui\"", json);
    }

    [Test]
    public void PatientADefibPadPrefabsProvideAedConnectionPointsForCartLine()
    {
      // 패드 부착 처리는 환자 쪽 패드 두 개와 카트 쪽 지점 두 개를 LineConnectionService 로 연결한다.
      const string aedScriptGuid = "c82bda2cb54644c889440723d1d8bedf";
      string padsFolder = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeA");

      foreach (string padPrefab in new[]
               {
                 "defibrillatorpad_midaxillary_A 1.prefab",
                 "defibrillatorpad_subclavicle_A.prefab"
               })
      {
        string prefabText = File.ReadAllText(Path.Combine(padsFolder, padPrefab));
        Assert.That(Regex.Matches(prefabText, aedScriptGuid).Count, Is.EqualTo(1),
          $"{padPrefab} 에 AEDLineConnectionPoint 가 정확히 한 개 배선되어야 합니다.");
      }

      string cartPrefabText = File.ReadAllText(Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Prefabs/Entities/MinecraftBoatLikes/Defibrillator.prefab"));
      Assert.That(Regex.Matches(cartPrefabText, aedScriptGuid).Count, Is.EqualTo(2),
        "제세동 카트 프리팹에는 AEDLineConnectionPoint 두 개가 배선되어야 합니다.");

      Assert.That(typeof(PatientController).GetProperty("AedConnectionPoints"), Is.Not.Null,
        "환자 컨트롤러에 AED 연결 지점 접근자(ref + fallback)가 있어야 합니다.");
      Assert.That(typeof(DefibrillatorCartController).GetProperty("AedConnectionPoints"), Is.Not.Null,
        "카트 컨트롤러에 AED 연결 지점 접근자(ref + fallback)가 있어야 합니다.");
      Assert.That(typeof(TriageScenarioEventBootstrap).GetMethod(
        "ConnectPatientADefibrillatorPads",
        BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null,
        "패드 부착 이벤트가 AED 라인 연결 처리를 수행해야 합니다.");
    }

    private static string ExtractNode(string json, string identifier)
    {
      Match match = Regex.Match(json,
        "\\\"" + Regex.Escape(identifier) + "\\\"\\s*:\\s*\\{(?<node>.*?)\\n\\s*\\},",
        RegexOptions.Singleline);
      Assert.That(match.Success, Is.True, $"Missing scenario node {identifier}.");
      return match.Groups["node"].Value;
    }

    private static string ExtractValue(string node, string key)
    {
      Match match = Regex.Match(node,
        "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*(?:\\\"(?<quoted>[^\\\"]*)\\\"|(?<raw>[^,\\n]+))");
      Assert.That(match.Success, Is.True, $"Missing {key} in scenario node.");
      return match.Groups["quoted"].Success
        ? match.Groups["quoted"].Value
        : match.Groups["raw"].Value.Trim();
    }

    private static string ExtractSourceNode(string markdown, string identifier)
    {
      Match match = Regex.Match(markdown,
        "### \\[" + Regex.Escape(identifier) + "\\](?<node>.*?)(?=\\n### |\\z)",
        RegexOptions.Singleline);
      Assert.That(match.Success, Is.True, $"Missing source scenario node {identifier}.");
      return match.Groups["node"].Value;
    }

    [Test]
    public void InstalledOxyflowmeterFirstClickRaisesInteractSignalWithoutDetach()
    {
      var oxyObject = new GameObject("oxyflowmeter-v015-4");
      var interactorObject = new GameObject("oxyflowmeter-interactor");
      var raised = new List<string>();
      void Capture(string signal) => raised.Add(signal);
      ScenarioInteractionSignals.OnSignalRegistered += Capture;
      try
      {
        oxyObject.AddComponent<BoxCollider>();
        var oxyflowmeter = oxyObject.AddComponent<WallAttachedOxyflowmeter>();
        typeof(WallAttachedOxyflowmeter).GetField(
            "_attachedInteractSignal", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(oxyflowmeter, "interact_oxyflow_wall");
        interactorObject.AddComponent<MultiplayerInfrastructure.Player.PlayerController>();

        // 설치(표시) 확정 시점에는 시나리오 신호를 올리지 않는다(설치 래치 제거 확인).
        oxyflowmeter.ApplyShownFromNetwork();
        Assert.That(oxyflowmeter.IsAttached, Is.True);
        Assert.That(raised, Does.Not.Contain("sig.interact_oxyflow_wall"),
          "설치 시점에 sig.interact_oxyflow_wall 가 올라가면 V015_4가 자동 통과(스킵)됩니다.");

        // 설치 상태에서의 첫 상호작용은 신호만 올리고 회수(분리)하지 않는다.
        // 힌트 문구도 실제 동작(신호 발행)과 일치해야 한다.
        ScenarioInteractionSignals.Clear("interact_oxyflow_wall");
        Assert.That(oxyflowmeter.DisplayText, Is.EqualTo("산소 유량계 조작"),
          "신호 미발행 상태의 설치 유량계 힌트는 '회수'가 아니라 '조작'이어야 합니다.");
        oxyflowmeter.Interact(interactorObject.transform);
        Assert.That(raised, Does.Contain("sig.interact_oxyflow_wall"),
          "설치된 유량계 클릭 시 sig.interact_oxyflow_wall 가 올라와야 합니다(V015_4 게이트).");
        Assert.That(oxyflowmeter.IsAttached, Is.True,
          "첫 상호작용은 신호만 올리고 유량계를 회수하지 않아야 합니다.");
        Assert.That(oxyflowmeter.DisplayText, Is.EqualTo("산소 유량계 회수"),
          "신호 발행 후에는 힌트가 회수 안내로 돌아와야 합니다.");
      }
      finally
      {
        ScenarioInteractionSignals.OnSignalRegistered -= Capture;
        ScenarioInteractionSignals.Clear("interact_oxyflow_wall");
        UnityEngine.Object.DestroyImmediate(interactorObject);
        UnityEngine.Object.DestroyImmediate(oxyObject);
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
        Assert.That(secondRoundAction.CanInteract(interactor.transform), Is.False,
          "2차 가슴압박은 시나리오 활성화 전에는 노출되지 않아야 합니다.");

        secondRoundAction.SetEnabled(true);
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
    public void PatientAStageGatedActionsStartDisabled()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientAPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = UnityEngine.Object.Instantiate(prefab);
      var interactor = new GameObject("stage-gate-interactor");
      try
      {
        interactor.AddComponent<MultiplayerInfrastructure.Player.PlayerController>();
        string[] stageSignals =
        {
          "click_to_start_comp", "interact_chest", "start_ambu_r1", "start_ambu_r2",
          "interact_patient_chest", "remove_tpiece", "remove_patient_clothing",
          "interact_tpiece", "remove_intu_stylet"
        };
        foreach (string signal in stageSignals)
        {
          var action = System.Array.Find(
            instance.GetComponentsInChildren<ScenarioActionInteractable>(true),
            each => each != null && each.CompletionSignal == signal);
          Assert.That(action, Is.Not.Null, $"신호 '{signal}' 상호작용을 프리팹에서 찾지 못했습니다.");
          Assert.That(action.CanInteract(interactor.transform), Is.False,
            $"상호작용 '{signal}'은 시나리오 활성화 전에는 노출되면 안 됩니다.");
        }

        var controller = instance.GetComponent<PatientController>();
        Assert.That(controller, Is.Not.Null);
        var getAssessAction = typeof(PatientController).GetMethod(
          "GetAssessAction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(getAssessAction, Is.Not.Null);
        var pulseAction = getAssessAction.Invoke(controller, new object[] { "assess_pulse_r1" })
          as PatientController.AssessActionConfig;
        Assert.That(pulseAction, Is.Not.Null);
        Assert.That(pulseAction.Enabled, Is.False,
          "맥박 확인(r1)은 심정지 구간 활성화 전에는 노출되면 안 됩니다.");
      }
      finally
      {
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
