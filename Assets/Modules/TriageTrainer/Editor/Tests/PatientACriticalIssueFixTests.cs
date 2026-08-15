using System;
using System.Collections.Generic;
using System.IO;
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
      "Assets/Modules/TriageTrainer/Prefabs/Entities/level1_rapid_infuser.prefab";
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
    public void RapidInfuserCanPassBloodTransfusionSetWithoutPlasmaWhenConfigured()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RapidInfuserPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      var instance = UnityEngine.Object.Instantiate(prefab);
      try
      {
        var controller = instance.GetComponent<Level1RapidInfuserController>();
        var policyField = typeof(Level1RapidInfuserController).GetField(
          "_bloodTransfusionSetWithoutPlasmaPolicy", BindingFlags.Instance | BindingFlags.NonPublic);
        var canAdd = typeof(Level1RapidInfuserController).GetMethod(
          "CanAddFluid", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(policyField, Is.Not.Null);
        Assert.That(canAdd, Is.Not.Null);
        policyField.SetValue(controller, BloodTransfusionSetWithoutPlasmaPolicy.Pass);

        Assert.That((bool)canAdd.Invoke(controller, new[] { GetRapidInfuserKind("BloodTransfusionSet") }), Is.True);
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
        Assert.That((bool)canAttempt.Invoke(controller, new[] { GetRapidInfuserKind("BloodTransfusionSet") }), Is.True);
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

        foreach (var fieldName in new[] { "_normalSalineDisplay", "_plasmaSolutionDisplay", "_bloodTransfusionSetDisplay" })
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
    public void PatientAMedicationDialoguesStayAlignedAcrossScenarioVariants()
    {
      string scenarioDirectory = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario");
      string[] scenarioFiles = Directory.GetFiles(scenarioDirectory, PatientAScenarioGlob);
      Assert.That(scenarioFiles, Has.Length.GreaterThanOrEqualTo(21),
        "Patient A의 기본 시나리오와 현재 배포된 변형을 모두 검증해야 합니다.");
      Assert.That(File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName,
        "Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json")),
        Is.True, "Patient A 기본 런타임 시나리오가 누락되었습니다.");

      foreach (string file in scenarioFiles)
      {
        string json = File.ReadAllText(file);
        string d031 = ExtractNode(json, "D031");
        string d0311 = ExtractNode(json, "D031_1");

        Assert.That(ExtractValue(d031, "speakerName"), Is.EqualTo("간호사 C"), file);
        Assert.That(ExtractValue(d0311, "speakerName"), Is.EqualTo("간호사 C"), file);
        Assert.That(ExtractValue(d0311, "autoAdvanceSeconds"), Is.EqualTo("4.0"), file);
      }

      string sourcePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
        PatientAScenarioSourcePath);
      Assert.That(File.Exists(sourcePath), Is.True, "Patient A 시나리오 원본 문서가 누락되었습니다.");
      string source = File.ReadAllText(sourcePath);
      string d031Source = ExtractSourceNode(source, "D031");
      string d0311Source = ExtractSourceNode(source, "D031_1");
      StringAssert.Contains("간호사 C", d031Source);
      StringAssert.Contains("간호사 C", d0311Source);
      StringAssert.Contains("Duration", d0311Source);
      StringAssert.Contains("4.0", d0311Source);
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
