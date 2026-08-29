using System.IO;
using System.Linq;
using MultiplayerInfrastructure.Player;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Scenario;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class PatientADoctorAnimationTests
  {
    private const string DoctorPrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/NPC/DoctorNPCHat.prefab";
    private const string DoctorControllerPath =
      "Assets/Modules/TriageTrainer/Animations/Scenario/PatientADoctor.controller";
    private const string ChestCompressionClipPath =
      "Assets/Modules/TriageTrainer/Animations/Scenario/chest_compression.anim";
    private const string ReceivingCprClipPath =
      "Assets/Modules/TriageTrainer/Animations/Scenario/cpr_receiving_patient.anim";
    private const string ReceivingCprClipGuid = "f722625d3ff604a3d80eaa4bbd12b546";
    private const string ChestCompressionClipGuid = "d534f618ed2984abe9dbc1b5eb29b15a";
    private const string PatientPrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeA.prefab";

    [Test]
    public void DoctorPrefabUsesHumanoidDoctorWithScenarioController()
    {
      GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoctorPrefabPath);
      Assert.That(prefab, Is.Not.Null);
      Assert.That(prefab.transform.Find("DoctorAnimationRoot/Chr_Doctor_Male_01_Prefab"),
        Is.Not.Null, "의사 NPC는 실제 남성 의사 모델을 사용해야 합니다.");

      Animator animator = prefab.GetComponentInChildren<Animator>(true);
      Assert.That(animator, Is.Not.Null);
      Assert.That(animator.avatar, Is.Not.Null);
      Assert.That(animator.avatar.isValid, Is.True);
      Assert.That(animator.avatar.isHuman, Is.True);
      Assert.That(animator.applyRootMotion, Is.False);
      Assert.That(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),
        Is.EqualTo(DoctorControllerPath));

      HumanoidAnimationController animation =
        prefab.GetComponentInChildren<HumanoidAnimationController>(true);
      Assert.That(animation, Is.Not.Null);
      Assert.That(animation.Animator, Is.SameAs(animator));

      GameObject instance = Object.Instantiate(prefab);
      try
      {
        Animator runtimeAnimator = instance.GetComponentInChildren<Animator>(true);
        runtimeAnimator.Rebind();
        Assert.That(runtimeAnimator.isHuman, Is.True);
        Assert.That(runtimeAnimator.GetBoneTransform(HumanBodyBones.Hips), Is.Not.Null,
          "의사 Avatar가 실제 프리팹 골격의 Hips 본을 해석해야 합니다.");
      }
      finally
      {
        Object.DestroyImmediate(instance);
      }
    }

    [Test]
    public void DoctorControllerProvidesIdleWalkAndCprTransitions()
    {
      AnimatorController controller =
        AssetDatabase.LoadAssetAtPath<AnimatorController>(DoctorControllerPath);
      Assert.That(controller, Is.Not.Null);
      Assert.That(controller.parameters.Select(parameter => parameter.name),
        Is.EquivalentTo(new[] { "walk", "cpr" }));
      Assert.That(controller.parameters.All(parameter =>
        parameter.type == AnimatorControllerParameterType.Bool), Is.True);

      AnimatorStateMachine machine = controller.layers.Single().stateMachine;
      var states = machine.states.ToDictionary(child => child.state.name, child => child.state);
      Assert.That(states.Keys, Is.EquivalentTo(new[] { "idle", "walk", "cpr" }));
      Assert.That(machine.defaultState, Is.SameAs(states["idle"]));
      Assert.That(AssetDatabase.GetAssetPath(states["idle"].motion),
        Is.EqualTo("Assets/Modules/Mixamo/Animations/idle.anim"));
      Assert.That(AssetDatabase.GetAssetPath(states["walk"].motion),
        Is.EqualTo("Assets/Modules/Mixamo/Animations/walk.anim"));
      Assert.That(AssetDatabase.GetAssetPath(states["cpr"].motion),
        Is.EqualTo(ChestCompressionClipPath));

      Assert.That(machine.anyStateTransitions.Any(transition =>
        transition.destinationState == states["cpr"]
        && transition.conditions.Any(condition => condition.parameter == "cpr"
          && condition.mode == AnimatorConditionMode.If)), Is.True);
    }

    [Test]
    public void DoctorAnimatorSwitchesBetweenIdleWalkAndCprAtRuntime()
    {
      GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoctorPrefabPath);
      GameObject instance = Object.Instantiate(prefab);
      try
      {
        Animator animator = instance.GetComponentInChildren<Animator>(true);
        animator.Rebind();
        animator.Update(0f);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("idle"), Is.True);

        animator.SetBool("walk", true);
        AdvanceAnimator(animator);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("walk"), Is.True);

        animator.SetBool("cpr", true);
        AdvanceAnimator(animator);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("cpr"), Is.True);

        animator.SetBool("walk", false);
        animator.SetBool("cpr", false);
        AdvanceAnimator(animator);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("idle"), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(instance);
      }
    }

    private static void AdvanceAnimator(Animator animator)
    {
      for (int i = 0; i < 10; i++)
        animator.Update(0.05f);
    }

    [Test]
    public void CprClipsAreLoopingHumanoidMotions()
    {
      AnimationClip compression = AssetDatabase.LoadAssetAtPath<AnimationClip>(ChestCompressionClipPath);
      AnimationClip receiving = AssetDatabase.LoadAssetAtPath<AnimationClip>(ReceivingCprClipPath);
      Assert.That(compression, Is.Not.Null);
      Assert.That(receiving, Is.Not.Null);
      Assert.That(compression.isHumanMotion, Is.True);
      Assert.That(receiving.isHumanMotion, Is.True);
      Assert.That(compression.isLooping, Is.True);
      Assert.That(receiving.isLooping, Is.True);
      Assert.That(compression.length, Is.EqualTo(0.9666667f).Within(0.001f));
      Assert.That(receiving.length, Is.EqualTo(8.633334f).Within(0.001f));
    }

    [Test]
    public void PatientCprPositionOffsetUsesCalibratedBedHeight()
    {
      const float calibratedModelOffsetY = 0.425f;

      var bootstrapObject = new GameObject("PatientACprOffsetTest");
      try
      {
        var bootstrap = bootstrapObject.AddComponent<TriageScenarioEventBootstrap>();
        var serialized = new SerializedObject(bootstrap);
        Vector3 offset = serialized.FindProperty("_cprReceivingPatientPositionOffset").vector3Value;

        Assert.That(offset.x, Is.Zero);
        Assert.That(offset.y, Is.EqualTo(calibratedModelOffsetY).Within(0.0001f),
          "Patient A 모델은 간호사 아래이면서 침대 위인 CPR 높이를 유지해야 합니다.");
        Assert.That(offset.z, Is.Zero);
      }
      finally
      {
        Object.DestroyImmediate(bootstrapObject);
      }
    }

    [Test]
    public void PatientCprPerformerUsesInteractionPatientAnchorAndBoatStyleRelease()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string source = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.PatientACprAnimations.cs"));

      StringAssert.Contains("action.GetComponentInParent<PatientController>(true)", source,
        "CPR 수행자 위치는 상호작용을 발행한 환자 엔티티를 기준으로 정해야 합니다.");
      StringAssert.Contains("patientPosition.x, patientPosition.y + _cprPerformingPlayerHeightOffset", source);
      StringAssert.Contains("patientPosition.z", source);
      StringAssert.Contains("state.Patient.transform.eulerAngles.y + 180f", source,
        "CPR 수행자는 환자와 반대 방향을 바라보도록 Y 회전을 180도 보정해야 합니다.");
      StringAssert.Contains("SetMovementSuppressed(state.Anchor, true)", source);
      StringAssert.Contains("SetRidableExitSuppressed(state.Anchor, true)", source);
      StringAssert.Contains("ClearForcedFollowAnchor(state.Anchor)", source,
        "CPR 종료는 MinecraftBoatLikeControl 하차와 같은 앵커 해제 경로를 사용해야 합니다.");
      StringAssert.Contains("SetMovementSuppressed(state.Anchor, false)", source);
      StringAssert.Contains("SetRidableExitSuppressed(state.Anchor, false)", source);
      StringAssert.Contains("EntryPosition = player.transform.position", source,
        "CPR 종료 시 침대 중앙이 아닌 진입 전 X/Z 위치로 복귀해야 합니다.");
      StringAssert.Contains("? state.DebugEscapePosition", source);
      StringAssert.Contains(": state.EntryPosition", source);
      StringAssert.Contains("SetLocalPositionAndRotation(rootPose.LocalPosition, rootPose.LocalRotation)", source,
        "RootT/RootQ가 포함된 CPR 그래프를 제거할 때 Animator 루트 자세를 복원해야 합니다.");

      var bootstrapObject = new GameObject("PatientACprPerformerHeightTest");
      try
      {
        var bootstrap = bootstrapObject.AddComponent<TriageScenarioEventBootstrap>();
        var serialized = new SerializedObject(bootstrap);
        Assert.That(serialized.FindProperty("_cprPerformingPlayerHeightOffset").floatValue,
          Is.EqualTo(0.85f).Within(0.0001f));
      }
      finally
      {
        Object.DestroyImmediate(bootstrapObject);
      }
    }

    [Test]
    public void PlayerMovementSuppressionIsOwnerScoped()
    {
      var playerObject = new GameObject("MovementSuppressionTest");
      var firstOwner = new GameObject("FirstMovementOwner");
      var secondOwner = new GameObject("SecondMovementOwner");
      try
      {
        var player = playerObject.AddComponent<PlayerController>();
        player.SetMovementSuppressed(firstOwner, true);
        player.SetMovementSuppressed(secondOwner, true);
        Assert.That(player.IsMovementSuppressed, Is.True);

        player.SetMovementSuppressed(firstOwner, false);
        Assert.That(player.IsMovementSuppressed, Is.True,
          "한 시스템의 이동 제한 해제가 다른 시스템의 이동 제한까지 해제해서는 안 됩니다.");

        player.SetMovementSuppressed(secondOwner, false);
        Assert.That(player.IsMovementSuppressed, Is.False);
      }
      finally
      {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(firstOwner);
        Object.DestroyImmediate(secondOwner);
      }
    }

    [Test]
    public void PlayerRidableExitSuppressionIsOwnerScoped()
    {
      var playerObject = new GameObject("RidableExitSuppressionTest");
      var firstOwner = new GameObject("FirstRidableExitOwner");
      var secondOwner = new GameObject("SecondRidableExitOwner");
      try
      {
        var player = playerObject.AddComponent<PlayerController>();
        player.SetRidableExitSuppressed(firstOwner, true);
        player.SetRidableExitSuppressed(secondOwner, true);
        Assert.That(player.IsRidableExitSuppressed, Is.True);

        player.SetRidableExitSuppressed(firstOwner, false);
        Assert.That(player.IsRidableExitSuppressed, Is.True);

        player.SetRidableExitSuppressed(secondOwner, false);
        Assert.That(player.IsRidableExitSuppressed, Is.False);
      }
      finally
      {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(firstOwner);
        Object.DestroyImmediate(secondOwner);
      }
    }

    [Test]
    public void PatientACprOffsetRootIsIsolatedFromOtherPatientPrefabs()
    {
      const string patientAPath =
        "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeA.prefab";
      GameObject patientA = AssetDatabase.LoadAssetAtPath<GameObject>(patientAPath);
      Assert.That(patientA, Is.Not.Null);

      Transform offsetRoot = patientA.transform.Find("CPRModelOffsetRoot");
      Assert.That(offsetRoot, Is.Not.Null);
      Animator patientAAnimator = patientA.GetComponentInChildren<Animator>(true);
      Assert.That(patientAAnimator, Is.Not.Null);
      Assert.That(patientAAnimator.transform.IsChildOf(offsetRoot), Is.True,
        "Patient A의 CPR 오프셋은 엔티티 루트가 아닌 모델 계층에만 적용해야 합니다.");

      string[] unaffectedPatientPaths =
      {
        "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBFemale.prefab",
        "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBMale.prefab",
        "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeDDummyA.prefab"
      };
      foreach (string path in unaffectedPatientPaths)
      {
        GameObject patient = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(patient, Is.Not.Null, path);
        Assert.That(patient.transform.Find("CPRModelOffsetRoot"), Is.Null,
          $"Patient A 전용 CPR 루트가 다른 환자 프리팹에 추가되어서는 안 됩니다: {path}");
      }
    }

    [Test]
    public void DebugCprEscapeRuleIsOptInAndDebuggingDatapackEnablesIt()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string rulesSource = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioGameRules.cs"));
      string commandSource = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandDefinitions/CommandDefinition.Gamerule.cs"));
      string debuggingDatapack = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/StreamingAssets/DataPacks/debugging.datapack.json"));

      StringAssert.Contains("DEBUG_INT_CPR_PLAYING_ESCAPE_KEY { get; set; }", rulesSource,
        "명시적인 초기값이 없는 bool 게임룰은 기본적으로 false여야 합니다.");
      StringAssert.Contains("gamerule DEBUG_INT_CPR_PLAYING_ESCAPE_KEY [true|false]", commandSource);
      StringAssert.Contains("TrySetDebugIntCprPlayingEscapeKeyServer(enabled)", commandSource,
        "서버에서 변경한 디버그 게임룰은 소유 클라이언트에도 동기화해야 합니다.");
      StringAssert.Contains("\"name\": \"DEBUG_INT_CPR_PLAYING_ESCAPE_KEY\", \"value\": \"true\"",
        debuggingDatapack);
    }

    [Test]
    public void DebugCprEscapeOnlyReleasesLocalPresentationUntilSystemStop()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string source = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.PatientACprAnimations.cs"));

      StringAssert.Contains("state.Player.IsOwner", source);
      StringAssert.Contains("InstanceFinder.IsOffline", source,
        "오프라인 단독 디버깅에서는 네트워크 소유권 없이도 Escape 입력을 처리해야 합니다.");
      StringAssert.Contains("Input.GetKeyDown(KeyCode.LeftShift)", source);
      StringAssert.Contains("state.DebugEscaped = true", source);
      StringAssert.Contains("state.DebugEscapePosition = state.Player.transform.position", source);
      StringAssert.Contains("StopPatientACprPerformerAnimation(state.Player)", source);
      StringAssert.Contains("state.DebugEscapePosition", source,
        "정상 CPR 종료 시에는 디버그로 위치 고정을 해제했던 장소로 복귀해야 합니다.");
      Assert.That(source, Does.Not.Contain("state.Player.SetForcedFollowAnchor(state.Anchor);\n          }"),
        "디버그 복귀가 이후에 설정된 다른 이동 시스템의 앵커를 덮어써서는 안 됩니다.");
      StringAssert.Contains("ResolvePatientACprModelOffsetRoot(animator)", source,
        "RootT 곡선과 침대 attachment가 충돌하지 않도록 전용 부모 루트에 오프셋을 적용해야 합니다.");
      Assert.That(source, Does.Not.Contain("TryDebugEscapePatientACprPerformer(state);\n      _patientACprPerformers.Clear"),
        "디버그 Escape가 시스템상 CPR 수행 상태를 제거해서는 안 됩니다.");
    }

    [TestCase("lisa")]
    [TestCase("maya")]
    [TestCase("emma")]
    public void NursePlayerModelsAcceptHumanoidCprAnimation(string modelIdentifier)
    {
      string path = $"Assets/Modules/TriageTrainer/Resources/Models/PlayerCharacters/{modelIdentifier}.prefab";
      GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      Assert.That(prefab, Is.Not.Null);

      Animator animator = prefab.GetComponentInChildren<Animator>(true);
      Assert.That(animator, Is.Not.Null);
      Assert.That(animator.avatar, Is.Not.Null);
      Assert.That(animator.avatar.isValid, Is.True);
      Assert.That(animator.avatar.isHuman, Is.True,
        $"{modelIdentifier} 간호사 모델은 Humanoid CPR 클립과 호환되어야 합니다.");
    }

    [Test]
    public void ChestCompressionInteractionsAreExplicitAndRoleRestricted()
    {
      GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientPrefabPath);
      Assert.That(prefab, Is.Not.Null);

      ScenarioActionInteractable[] actions =
        prefab.GetComponentsInChildren<ScenarioActionInteractable>(true);
      ScenarioActionInteractable roundOne = actions.Single(action =>
        action.CompletionSignal == "click_to_start_comp");
      ScenarioActionInteractable roundTwo = actions.Single(action =>
        action.CompletionSignal == "interact_chest");

      Assert.That(roundOne.DisplayText, Is.EqualTo("가슴압박 수행"));
      Assert.That(roundOne.RequiredPlayerTag, Is.EqualTo("nurse_b"));
      Assert.That(roundTwo.DisplayText, Is.EqualTo("가슴압박 수행"));
      Assert.That(roundTwo.RequiredPlayerTag, Is.EqualTo("nurse_a"));
    }

    [TestCase("Assets/Scenes/OverworldScene.unity")]
    [TestCase("Assets/Scenes/IndevScene.unity")]
    [TestCase("Assets/Scenes/IngameScene.unity")]
    [TestCase("Assets/Scenes/Researchs/TrialsScenarioGraph.unity")]
    public void BootstrapSceneReferencesPatientReceivingCprClip(string scenePath)
    {
      string yaml = File.ReadAllText(scenePath);
      StringAssert.Contains(
        $"_chestCompressionAnimationClip: {{fileID: 7400000, guid: {ChestCompressionClipGuid}, type: 2}}",
        yaml);
      StringAssert.Contains(
        $"_cprReceivingPatientAnimationClip: {{fileID: 7400000, guid: {ReceivingCprClipGuid}, type: 2}}",
        yaml);
    }
  }
}
