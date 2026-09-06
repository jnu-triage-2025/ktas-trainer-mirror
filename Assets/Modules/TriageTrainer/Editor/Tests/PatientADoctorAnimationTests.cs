using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Tag;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Scenario;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;

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
    private const string PatientAScenarioPath =
      "Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json";
    private const string IngameScenePath = "Assets/Scenes/IngameScene.unity";
    private const string OverworldScenePath = "Assets/Scenes/OverworldScene.unity";
    private const string IngameSceneBootstrapperSourcePath =
      "Assets/Modules/UnitySceneSupports/IngameScene/Scripts/IngameSceneBootstrapper.cs";
    private const string ScenarioEventBootstrapSourcePath =
      "Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs";
    private const string ScenarioEventBootstrapScriptGuid = "35d5ee3a6e42c417da2b33a6fa829dbd";

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

      GameObject instance = UnityEngine.Object.Instantiate(prefab);
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
        UnityEngine.Object.DestroyImmediate(instance);
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
      GameObject instance = UnityEngine.Object.Instantiate(prefab);
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
        UnityEngine.Object.DestroyImmediate(instance);
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
        UnityEngine.Object.DestroyImmediate(bootstrapObject);
      }
    }

    [Test]
    public void PatientCprPlaybackKeepsModelHeightStableAndRestoresItsPose()
    {
      GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PatientPrefabPath);
      AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ReceivingCprClipPath);
      GameObject patient = UnityEngine.Object.Instantiate(prefab);
      var bootstrapObject = new GameObject("PatientACprPlaybackStabilityTest");
      try
      {
        var bootstrap = bootstrapObject.AddComponent<TriageScenarioEventBootstrap>();
        Animator animator = patient.GetComponentInChildren<Animator>(true);
        Transform offsetRoot = patient.transform.Find("CPRModelOffsetRoot");
        Assert.That(animator, Is.Not.Null);
        Assert.That(offsetRoot, Is.Not.Null);
        Assert.That(animator.isHuman, Is.True);
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        Assert.That(hips, Is.Not.Null);

        animator.Rebind();
        animator.Update(0f);
        Vector3 animatorStartLocalPosition = animator.transform.localPosition;
        Quaternion animatorStartLocalRotation = animator.transform.localRotation;
        Transform referenceFrame = offsetRoot.parent;
        Vector3 hipsStartPosition = referenceFrame.InverseTransformPoint(hips.position);

        MethodInfo play = typeof(TriageScenarioEventBootstrap).GetMethod(
          "PlayLoopingClip", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(play, Is.Not.Null);
        play.Invoke(bootstrap, new object[] { animator, clip, "patient A test", new Vector3(0f, 0.425f, 0f) });

        FieldInfo graphsField = typeof(TriageScenarioEventBootstrap).GetField(
          "_patientACprAnimationGraphs", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(graphsField, Is.Not.Null);
        var graphs = (System.Collections.IDictionary)graphsField.GetValue(bootstrap);
        var graph = (PlayableGraph)graphs[animator];
        MethodInfo lateUpdate = typeof(TriageScenarioEventBootstrap).GetMethod(
          "LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(lateUpdate, Is.Not.Null);

        for (int i = 0; i < 12; i++)
        {
          graph.Evaluate(0.25f);
          lateUpdate.Invoke(bootstrap, null);
          Vector3 hipsPosition = referenceFrame.InverseTransformPoint(hips.position);
          Assert.That(Vector3.Distance(hipsPosition, hipsStartPosition), Is.LessThan(0.001f),
            $"CPR 클립의 RootT가 {i + 1}번째 평가에서 Hips를 침하시키거나 공중으로 띄우면 안 됩니다.");
        }

        MethodInfo stop = typeof(TriageScenarioEventBootstrap).GetMethod(
          "StopAndRestoreAnimation", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(stop, Is.Not.Null);
        stop.Invoke(bootstrap, new object[] { animator });

        Assert.That(offsetRoot.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(animator.transform.localPosition, Is.EqualTo(animatorStartLocalPosition));
        Assert.That(Quaternion.Angle(animator.transform.localRotation, animatorStartLocalRotation),
          Is.LessThan(0.01f));
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(bootstrapObject);
        UnityEngine.Object.DestroyImmediate(patient);
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
      StringAssert.Contains("EntryPosition = ResolvePatientACprPerformerEntryPosition(player, patient)",
        source, "CPR 종료 시 침대 중앙이 아닌 진입 전 X/Z 위치로 복귀해야 합니다.");
      StringAssert.Contains("Vector3 entryPosition = player.transform.position", source,
        "진입 위치의 기준은 CPR 진입 직전 플레이어 위치여야 합니다.");
      StringAssert.Contains("MoveToPositionPreservingForcedFollowAnchor(state.EntryPosition)", source);
      Assert.That(source, Does.Not.Contain("DebugEscapePosition"),
        "Left Shift 순간의 좌표는 CPR 종료 위치로 기록하거나 사용해서는 안 됩니다.");
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
        UnityEngine.Object.DestroyImmediate(bootstrapObject);
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
        UnityEngine.Object.DestroyImmediate(playerObject);
        UnityEngine.Object.DestroyImmediate(firstOwner);
        UnityEngine.Object.DestroyImmediate(secondOwner);
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
        UnityEngine.Object.DestroyImmediate(playerObject);
        UnityEngine.Object.DestroyImmediate(firstOwner);
        UnityEngine.Object.DestroyImmediate(secondOwner);
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
      StringAssert.Contains("StopPatientACprPerformerAnimation(state.Player)", source);
      StringAssert.Contains("MoveToPositionPreservingForcedFollowAnchor(state.EntryPosition)", source,
        "정상 CPR 종료 시에는 Escape 순간이 아니라 CPR 진입 직전 위치로 복귀해야 합니다.");
      Assert.That(source, Does.Not.Contain("DebugEscapePosition"));

      int escapeIndex = source.IndexOf("state.DebugEscaped = true", StringComparison.Ordinal);
      int escapeReturnIndex = source.IndexOf(
        "ReturnPatientACprPerformerToEntry(state)", escapeIndex, StringComparison.Ordinal);
      Assert.That(escapeReturnIndex, Is.GreaterThan(escapeIndex),
        "Escape로 앵커만 풀면 수행자가 침대 안에 남아 Y로 솟아오르므로, 진입 직전 위치로 되돌려야 합니다.");
      StringAssert.Contains("state.ReturnedToEntry", source,
        "이미 퇴장한 수행자를 정상 종료 시점에 다시 끌어와서는 안 됩니다.");
      StringAssert.Contains("ResolvePatientACprPerformerEntryPosition(player, patient)", source,
        "환자·침대와 겹치는 좌표는 퇴장 위치로 기록하지 않고 X/Z를 밀어내야 합니다.");
      Assert.That(source, Does.Not.Contain("state.Player.SetForcedFollowAnchor(state.Anchor);\n          }"),
        "디버그 복귀가 이후에 설정된 다른 이동 시스템의 앵커를 덮어써서는 안 됩니다.");
      StringAssert.Contains("ResolvePatientACprModelOffsetRoot(animator)", source,
        "RootT 곡선과 침대 attachment가 충돌하지 않도록 전용 부모 루트에 오프셋을 적용해야 합니다.");
      Assert.That(source, Does.Not.Contain("TryDebugEscapePatientACprPerformer(state);\n      _patientACprPerformers.Clear"),
        "디버그 Escape가 시스템상 CPR 수행 상태를 제거해서는 안 됩니다.");
    }

    /// <summary>
    /// 디버그 Escape 상태는 새로 시작된 가슴압박 연출을 넘어 유지되면 안 된다.
    /// 유지되면 <c>TryDebugEscapePatientACprPerformer</c> 가 곧바로 실패하고 <c>LateUpdate</c> 의
    /// 매 프레임 정렬도 건너뛰므로, 진입 시 한 번 걸린 앵커가 해제되지 않아 수행자가 CPR 좌표에
    /// 고정된 채 조작 불능이 된다. (1주기 nurse_b 로 Escape 한 뒤 2주기 nurse_a 로 재진입하는 상황)
    /// </summary>
    [Test]
    public void DebugCprEscapeDoesNotSurviveANewChestCompressionPresentation()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string source = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.PatientACprAnimations.cs"));

      int reuseIndex = source.IndexOf("state.Patient = patient;", StringComparison.Ordinal);
      Assert.That(reuseIndex, Is.GreaterThan(0),
        "기존 수행자 상태를 재사용하는 분기를 찾지 못했습니다.");
      int reuseResetIndex = source.IndexOf(
        "state.DebugEscaped = false;", reuseIndex, StringComparison.Ordinal);
      Assert.That(reuseResetIndex, Is.GreaterThan(reuseIndex),
        "기존 수행자 상태를 재사용해 다시 진입할 때 디버그 Escape 상태를 초기화해야 합니다.");

      int returnedToEntryResetIndex = source.IndexOf(
        "state.ReturnedToEntry = false;", reuseIndex, StringComparison.Ordinal);
      Assert.That(returnedToEntryResetIndex, Is.LessThan(reuseResetIndex),
        "Escape 상태 초기화가 퇴장 위치 복귀 여부(ReturnedToEntry) 분기 안에 갇혀서는 안 됩니다.");

      StringAssert.Contains("private void RestorePatientACprPerformerFromDebugEscape(PlayerController player)",
        source,
        "상호작용과 별개로 연출을 다시 거는 경로(E028/E033)에서도 Escape 상태를 되돌려야 합니다.");
      int restoreCallCount = source.Split(
        new[] { "RestorePatientACprPerformerFromDebugEscape(player);" },
        StringSplitOptions.None).Length - 1;
      Assert.That(restoreCallCount, Is.GreaterThanOrEqualTo(2),
        "태그 기준 연출 경로와 수행자 기준 연출 경로 모두에서 Escape 상태를 되돌려야 합니다.");
    }

    /// <summary>
    /// CPR 수행자 고정과 연출은 가슴압박에 상호작용한 피어가 로컬로 시작한다. 반면 종료 이벤트인
    /// <c>stop_ambu_and_comp</c>(E031/E035)는 역할 브랜치 밖의 InvokeEvent 노드라서 그래프를
    /// 순회하는 권위 피어에서만 실행된다. 종료를 전 피어에 전달하지 않으면 상호작용한 클라이언트에서
    /// 앵커 고정과 연출이 풀리지 않고, 다음 주기가 이전 주기의 수행자 상태를 그대로 재사용한다.
    /// </summary>
    [Test]
    public void CprStopReachesEveryPeerThatStartedThePresentationLocally()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string stopEventSource = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.stop_ambu_and_comp.cs"));
      string relaySource = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioNetworkRelay.cs"));
      string controllerSource = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs"));

      StringAssert.Contains(
        "ScenarioNetworkRelay.InvokePresentationEventAuthoritative(StopAmbuAndCompEventIdentifier)",
        stopEventSource,
        "CPR 종료는 권위 피어뿐 아니라 연출을 로컬로 시작한 피어에도 도달해야 합니다.");

      StringAssert.Contains("public static void InvokePresentationEventAuthoritative(string eventIdentifier)",
        relaySource);
      StringAssert.Contains("!InstanceFinder.IsServerStarted", relaySource,
        "연출 이벤트 통지는 서버 컨텍스트에서만 나가야 합니다.");
      int broadcastIndex = relaySource.IndexOf(
        "public static void InvokePresentationEventAuthoritative", StringComparison.Ordinal);
      int excludeServerIndex = relaySource.IndexOf(
        "[ObserversRpc(BufferLast = false, ExcludeServer = true)]", broadcastIndex, StringComparison.Ordinal);
      int rpcIndex = relaySource.IndexOf(
        "private void ObserversInvokePresentationEvent", broadcastIndex, StringComparison.Ordinal);
      Assert.That(excludeServerIndex, Is.GreaterThan(broadcastIndex).And.LessThan(rpcIndex),
        "호스트가 같은 연출 종료를 두 번 적용하지 않도록 ExcludeServer 로 보내야 합니다.");

      StringAssert.Contains("public void RunPresentationEvent(string graphIdentifier, string eventIdentifier)",
        controllerSource);
      int runIndex = controllerSource.IndexOf(
        "public void RunPresentationEvent(", StringComparison.Ordinal);
      int guardIndex = controllerSource.IndexOf(
        "_executionMode != ExecutionMode.ClientPresentation", runIndex, StringComparison.Ordinal);
      Assert.That(guardIndex, Is.GreaterThan(runIndex),
        "그래프를 직접 순회하는 피어는 통지받은 연출 이벤트를 다시 실행해서는 안 됩니다.");
    }

    /// <summary>
    /// CPR 시작 연출도 모든 피어에 도달해야 한다. 라운드마다 가슴압박 역할이 다른데 표시 피어에는
    /// InvokeEvent 노드가 <c>CurrentNode</c> 로 남지 않으므로, 라운드 판정을 표시 피어에 맡기면
    /// 2주기에도 1주기 역할의 연출이 재생된다. 권위 피어가 판정한 라운드를 라운드별 연출 이벤트로
    /// 구분해서 통지해야 한다.
    /// </summary>
    [Test]
    public void CprStartReachesEveryPeerWithTheRoundResolvedByTheAuthority()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string startSource = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.start_chest_compression.cs"));
      string ambuSource = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.Event.start_ambubagging.cs"));
      string animationSource = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.PatientACprAnimations.cs"));

      StringAssert.Contains("\"present_patient_a_cpr_round_one\"", startSource);
      StringAssert.Contains("\"present_patient_a_cpr_round_two\"", startSource);
      Assert.That(
        startSource.Split(new[] { "\"start_chest_compression\"" }, StringSplitOptions.None).Length - 1,
        Is.EqualTo(1),
        "라운드별 연출 이벤트가 시나리오 이벤트와 같은 식별자를 쓰면 표시 피어가 라운드를 구분하지 못합니다.");
      Assert.That(animationSource, Does.Not.Contain("PatientACprRoundOnePresentationEventIdentifier ="),
        "라운드별 연출 이벤트 식별자는 한 곳에서만 선언해야 합니다.");
      Assert.That(animationSource, Does.Not.Contain("PatientACprRoundTwoPresentationEventIdentifier ="),
        "라운드별 연출 이벤트 식별자는 한 곳에서만 선언해야 합니다.");

      StringAssert.Contains(
        "Register(PatientACprRoundOnePresentationEventIdentifier, Event_PresentPatientACprRoundOne)",
        startSource);
      StringAssert.Contains(
        "Register(PatientACprRoundTwoPresentationEventIdentifier, Event_PresentPatientACprRoundTwo)",
        startSource);
      StringAssert.Contains("ScenarioNetworkRelay.InvokePresentationEventAuthoritative(", startSource,
        "가슴압박 시작도 연출을 보지 못하는 피어에 도달해야 합니다.");
      StringAssert.Contains("ResolvePatientACprRoundPresentationEventIdentifier())", startSource,
        "통지에는 권위 피어가 판정한 라운드가 실려야 합니다.");
      StringAssert.Contains(
        "ScenarioNetworkRelay.InvokePresentationEventAuthoritative(StartAmbuBaggingEventIdentifier)",
        ambuSource,
        "앰부배깅 애니메이터는 피어마다 따로 있으므로 시작도 전 피어에 도달해야 합니다.");

      // 표시 피어는 수행자 고정을 만들지 않는다. 위치는 NetworkTransform 이 복제하므로, 원격에서
      // 앵커를 걸면 복제 위치와 충돌한다.
      Assert.That(startSource, Does.Not.Contain("PositionPatientACprPerformer"),
        "통지받은 연출 경로가 원격 피어에서 수행자 위치 고정을 만들어서는 안 됩니다.");

      StringAssert.Contains("_patientACprAnimationClips", animationSource,
        "같은 연출의 중복 시작을 걸러내려면 재생 중인 클립을 알아야 합니다.");
      int guardIndex = animationSource.IndexOf("playingClip == clip", StringComparison.Ordinal);
      Assert.That(guardIndex, Is.GreaterThan(0),
        "상호작용한 피어는 로컬 시작 직후 같은 연출 통지를 받으므로, 재시작으로 클립이 되감기면 안 됩니다.");
    }

    [Test]
    public void CprStopEndsPatientPresentationAndRestoresOffsetTogether()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string source = File.ReadAllText(Path.Combine(projectRoot,
        "Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.PatientACprAnimations.cs"));

      StringAssert.Contains("StopPatientACprAnimations()", source);
      StringAssert.Contains("StopAndRestoreAnimation(animator)", source,
        "CPR 그래프 종료와 환자 높이 복원은 분리되지 않은 하나의 경로여야 합니다.");
      StringAssert.Contains("graph.Destroy()", source,
        "stop_ambu_and_comp에서 환자 CPR PlayableGraph를 실제로 종료해야 합니다.");
      StringAssert.Contains("animator.Update(0f)", source,
        "그래프 종료 직후 Animator Controller를 평가하여 CPR 골격 자세가 남지 않게 해야 합니다.");
      StringAssert.Contains("animator.Rebind()", source,
        "그래프를 제거하는 것만으로는 Animator가 자기 Controller 재생으로 돌아오지 않습니다.");
      StringAssert.Contains("RestoreAnimationPositionOffset(animator)", source,
        "CPR 애니메이션 종료 후 RootT/RootQ와 모델 Y 오프셋을 원래 값으로 복원해야 합니다.");

      int stopIndex = source.IndexOf(
        "private void StopAndRestoreAnimation(Animator animator)", StringComparison.Ordinal);
      Assert.That(stopIndex, Is.GreaterThanOrEqualTo(0));
      int playbackRestoreIndex = source.IndexOf(
        "RestorePatientACprAnimatorPlayback(animator)", stopIndex, StringComparison.Ordinal);
      int offsetRestoreIndex = source.IndexOf(
        "RestoreAnimationPositionOffset(animator)", stopIndex, StringComparison.Ordinal);
      Assert.That(playbackRestoreIndex, Is.GreaterThanOrEqualTo(0));
      Assert.That(playbackRestoreIndex, Is.LessThan(offsetRestoreIndex),
        "Y 보정 제거는 CPR 자세 종료(컨트롤러 재생 복귀) 이후에만 이뤄져야 합니다. "
        + "순서가 뒤바뀌면 환자가 CPR 자세를 유지한 채 침대 아래로 내려앉습니다.");
      StringAssert.Contains("ReleasePatientACprPerformers()", source,
        "CPR 종료 시 수행자의 위치 고정도 해제해야 합니다.");
      Assert.That(source, Does.Not.Contain("preservePatientAnimation"),
        "E031에서 환자 CPR 애니메이션만 남기는 예외 경로를 두어서는 안 됩니다.");
    }

    /// <summary>
    /// IngameScene 은 OverworldScene 을 Additive 로 로드한다. 두 씬이 모두 부트스트랩을 들고 있으면
    /// 상호작용 static 이벤트는 두 인스턴스가 모두 처리하는 반면 시나리오 이벤트는 마지막 등록만
    /// 남으므로, CPR PlayableGraph 와 모델 Y 오프셋을 두 번 걸고 한 번만 해제하게 된다.
    /// </summary>
    [Test]
    public void OnlyOverworldSceneCarriesTheScenarioEventBootstrap()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string scriptReference =
        $"m_Script: {{fileID: 11500000, guid: {ScenarioEventBootstrapScriptGuid}, type: 3}}";

      string ingameScene = File.ReadAllText(Path.Combine(projectRoot, IngameScenePath));
      string overworldScene = File.ReadAllText(Path.Combine(projectRoot, OverworldScenePath));

      Assert.That(CountOccurrences(ingameScene, scriptReference), Is.EqualTo(0),
        "IngameScene 은 OverworldScene 을 Additive 로 로드하므로, 여기에도 부트스트랩을 두면 "
        + "인스턴스가 둘이 되어 CPR 연출 정리가 한쪽에서만 실행됩니다.");
      Assert.That(CountOccurrences(overworldScene, scriptReference), Is.EqualTo(1),
        "환자 연출 참조를 보유한 OverworldScene 의 부트스트랩 하나만 남아 있어야 합니다.");

      string ingameBootstrapperSource =
        File.ReadAllText(Path.Combine(projectRoot, IngameSceneBootstrapperSourcePath));
      Assert.That(ingameBootstrapperSource,
        Does.Not.Contain("AddComponent<TriageScenarioEventBootstrap>"),
        "런타임에 부트스트랩을 덧붙이면 씬에서 제거해도 인스턴스가 다시 둘이 됩니다.");
    }

    /// <summary>
    /// 씬 구성이 다시 어긋나더라도 인스턴스가 둘로 늘어나지 않도록, 나중에 활성화된 인스턴스는
    /// 아무것도 구독·등록하지 않은 채 스스로 비활성화되어야 한다.
    /// </summary>
    [Test]
    public void ScenarioEventBootstrapKeepsOnlyOneActiveInstance()
    {
      string projectRoot = Directory.GetParent(Application.dataPath).FullName;
      string source = File.ReadAllText(Path.Combine(projectRoot, ScenarioEventBootstrapSourcePath));

      StringAssert.Contains("private static TriageScenarioEventBootstrap _activeInstance;", source,
        "활성 인스턴스를 판별할 기준값이 있어야 중복을 걸러낼 수 있습니다.");

      int enableIndex = source.IndexOf("private void OnEnable()", StringComparison.Ordinal);
      Assert.That(enableIndex, Is.GreaterThanOrEqualTo(0));
      int claimIndex = source.IndexOf("TryBecomeActiveInstance()", enableIndex, StringComparison.Ordinal);
      int subscribeIndex = source.IndexOf(
        "InteractionRegistry.Interacted +=", enableIndex, StringComparison.Ordinal);
      Assert.That(claimIndex, Is.GreaterThanOrEqualTo(0));
      Assert.That(subscribeIndex, Is.GreaterThanOrEqualTo(0));
      Assert.That(claimIndex, Is.LessThan(subscribeIndex),
        "중복 판정은 static 상호작용 이벤트를 구독하기 전에 끝나야 합니다. "
        + "순서가 뒤바뀌면 중복 인스턴스도 CPR PlayableGraph 를 만들게 됩니다.");

      int disableIndex = source.IndexOf("private void OnDisable()", StringComparison.Ordinal);
      Assert.That(disableIndex, Is.GreaterThanOrEqualTo(0));
      int guardIndex = source.IndexOf("_activeInstance != this", disableIndex, StringComparison.Ordinal);
      int unsubscribeIndex = source.IndexOf(
        "InteractionRegistry.Interacted -=", disableIndex, StringComparison.Ordinal);
      Assert.That(guardIndex, Is.GreaterThanOrEqualTo(0));
      Assert.That(unsubscribeIndex, Is.GreaterThanOrEqualTo(0));
      Assert.That(guardIndex, Is.LessThan(unsubscribeIndex),
        "중복으로 판정된 인스턴스의 OnDisable 은 해제를 수행하면 안 됩니다. "
        + "활성 인스턴스가 등록해 둔 시나리오 이벤트까지 함께 지워집니다.");
    }

    private static int CountOccurrences(string source, string value)
      => source.Split(new[] { value }, StringSplitOptions.None).Length - 1;

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
      var graph = LoadPatientAGraph();
      var roundOne = FindPatientAInteraction(graph, "click_to_start_comp");
      var roundTwo = FindPatientAInteraction(graph, "interact_chest");

      Assert.That(roundOne.Display.Text, Is.EqualTo("가슴압박 수행"));
      Assert.That(RequiredPlayerTags(roundOne), Is.EqualTo(new[] { "nurse_b" }));
      Assert.That(roundTwo.Display.Text, Is.EqualTo("가슴압박 수행"));
      Assert.That(RequiredPlayerTags(roundTwo), Is.EqualTo(new[] { "nurse_a" }));
    }

    [Test]
    public void ChestCompressionInteractionsAreVisibleOnlyToTheirAssignedRole()
    {
      const string nurseAIdentifier = "test-cpr-nurse-a";
      const string nurseBIdentifier = "test-cpr-nurse-b";
      try
      {
        PlayerTagService.ReplaceTags(nurseAIdentifier, new[] { "nurse_a" });
        PlayerTagService.ReplaceTags(nurseBIdentifier, new[] { "nurse_b" });

        var graph = LoadPatientAGraph();
        var roundOne = FindPatientAInteraction(graph, "click_to_start_comp");
        var roundTwo = FindPatientAInteraction(graph, "interact_chest");

        Assert.That(TagConditionsPass(roundOne, nurseBIdentifier), Is.True);
        Assert.That(TagConditionsPass(roundOne, nurseAIdentifier), Is.False,
          "1주기 가슴압박은 nurse_b가 아닌 플레이어에게 노출되면 안 됩니다.");
        Assert.That(TagConditionsPass(roundTwo, nurseAIdentifier), Is.True);
        Assert.That(TagConditionsPass(roundTwo, nurseBIdentifier), Is.False,
          "교대 후 가슴압박은 nurse_a가 아닌 플레이어에게 노출되면 안 됩니다.");
      }
      finally
      {
        PlayerTagService.ClearTags(nurseAIdentifier);
        PlayerTagService.ClearTags(nurseBIdentifier);
      }
    }

    private static ScenarioGraph LoadPatientAGraph()
      => ScenarioGraphLoader.LoadFromJson(File.ReadAllText(PatientAScenarioPath), validateWithSchema: true);

    private static InteractionDefinition FindPatientAInteraction(ScenarioGraph graph, string interactionIdentifier)
    {
      var definition = graph.Interactions.FirstOrDefault(each =>
        each?.Entity != null && each.Entity.Identifier == "patient_a"
        && each.InteractionIdentifier == interactionIdentifier);
      Assert.That(definition, Is.Not.Null, $"시나리오 데이터에 'patient_a/{interactionIdentifier}' 정의가 없습니다.");
      return definition;
    }

    private static string[] RequiredPlayerTags(InteractionDefinition definition)
      => definition.VisibilityConditions
        .Where(each => each.Type == ScenarioConditionType.PlayerHasTag && !each.Negate)
        .Select(each => each.Tag)
        .ToArray();

    /// <summary>태그 조건만 떼어 판정한다. 퀘스트 조건은 별도 퀘스트 상태가 필요하므로 여기서는 다루지 않는다.</summary>
    private static bool TagConditionsPass(InteractionDefinition definition, string playerIdentifier)
    {
      var tagConditions = definition.VisibilityConditions
        .Where(each => each.Type == ScenarioConditionType.PlayerHasTag)
        .ToList();
      Assert.That(tagConditions, Is.Not.Empty);
      return ScenarioConditionEvaluator.Evaluate(
        tagConditions, ScenarioConditionMatchMode.All,
        ScenarioConditionContext.ForPlayerIdentifier(playerIdentifier), out _);
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
