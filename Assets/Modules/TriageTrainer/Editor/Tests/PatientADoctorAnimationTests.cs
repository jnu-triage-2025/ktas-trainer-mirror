using System.IO;
using System.Linq;
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
