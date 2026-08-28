using System.Collections.Generic;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Tag;
using TriageTrainer.Entity;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private const string PatientACriticalDoctorIdentifier = "npc-doctor-patient-a-critical";
    private const string PatientADoctorWalkParameter = "walk";
    private const string PatientADoctorCprParameter = "cpr";
    private const string PatientACprRoundOneNodeIdentifier = "E028";
    private const string PatientACprRoundTwoNodeIdentifier = "E033";
    private const string PatientACprRoundOneNurseTag = "nurse_b";
    private const string PatientACprRoundTwoNurseTag = "nurse_a";

    private readonly Dictionary<Animator, PlayableGraph> _patientACprAnimationGraphs = new();

    private void PlayPatientAChestCompressionAnimation()
    {
      ResolveRuntimeReferencesIfNeeded();
      SetPatientADoctorCprAnimation(true);
      PlayPatientACprNurseAnimation();
      PlayLoopingClip(ResolveAnimator(_patientAObject), _cprReceivingPatientAnimationClip, "patient A");
    }

    private void HandleScenarioActionInteractionCompleted(
      ScenarioActionInteractable action, PlayerController player)
    {
      if (action == null || player == null)
        return;

      string signal = action.CompletionSignal;
      if (signal != "click_to_start_comp" && signal != "interact_chest")
        return;

      ResolveRuntimeReferencesIfNeeded();
      SetPatientADoctorCprAnimation(true);
      PlayPatientACprNurseAnimation(player, action.RequiredPlayerTag);
      PlayLoopingClip(ResolveAnimator(_patientAObject), _cprReceivingPatientAnimationClip, "patient A");
    }

    private void PlayPatientACprNurseAnimation()
    {
      string currentNodeIdentifier = ScenarioController.Instance?.CurrentNode?.Identifier;
      string nurseTag = currentNodeIdentifier switch
      {
        PatientACprRoundTwoNodeIdentifier => PatientACprRoundTwoNurseTag,
        PatientACprRoundOneNodeIdentifier => PatientACprRoundOneNurseTag,
        _ => PatientACprRoundOneNurseTag
      };

      var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      bool found = false;
      for (int i = 0; i < players.Length; i++)
      {
        PlayerController player = players[i];
        if (player == null || string.IsNullOrWhiteSpace(player.UserIdentifier)
                           || !PlayerTagService.HasTag(player.UserIdentifier, nurseTag))
          continue;

        Animator animator = ResolvePlayerCharacterAnimator(player);
        if (animator == null || !animator.isHuman)
          continue;

        PlayLoopingClip(animator, _chestCompressionAnimationClip, $"{nurseTag} player");
        found = true;
      }

      if (!found)
      {
        Debug.LogWarning(
          $"[TriageScenarioEventBootstrap] A Humanoid player model assigned to '{nurseTag}' was not found for CPR.",
          this);
      }
    }

    private void PlayPatientACprNurseAnimation(PlayerController player, string nurseTag)
    {
      Animator animator = ResolvePlayerCharacterAnimator(player);
      if (animator != null && animator.isHuman)
      {
        PlayLoopingClip(animator, _chestCompressionAnimationClip,
          $"{(string.IsNullOrWhiteSpace(nurseTag) ? "CPR nurse" : nurseTag)} player");
        return;
      }

      Debug.LogWarning(
        $"[TriageScenarioEventBootstrap] The player who performed '{nurseTag}' CPR has no compatible Humanoid model.",
        this);
    }

    private static Animator ResolvePlayerCharacterAnimator(PlayerController player)
    {
      if (player == null)
        return null;

      var modelComponents = player.GetComponentsInChildren<MonoBehaviour>(true);
      for (int i = 0; i < modelComponents.Length; i++)
      {
        if (modelComponents[i] is IPlayerCharacterModelObject model && model.Animator != null)
          return model.Animator;
      }

      return null;
    }

    private void StopPatientACprAnimations()
    {
      SetPatientADoctorCprAnimation(false);

      foreach (var pair in _patientACprAnimationGraphs)
      {
        if (pair.Value.IsValid())
          pair.Value.Destroy();
      }

      _patientACprAnimationGraphs.Clear();
    }

    private void CompletePatientACprCycle()
    {
      StopPatientACprAnimations();
    }

    private void SetPatientADoctorCprAnimation(bool active)
    {
      GameObject doctor = ResolveEntityObject(null, PatientACriticalDoctorIdentifier);
      HumanoidAnimationController animation =
        doctor != null ? doctor.GetComponentInChildren<HumanoidAnimationController>(true) : null;
      if (animation == null || !animation.HasAnimator)
      {
        if (active)
        {
          Debug.LogWarning(
            $"[TriageScenarioEventBootstrap] Doctor animation controller was not found for '{PatientACriticalDoctorIdentifier}'.",
            this);
        }
        return;
      }

      if (active)
        animation.SetBool(PatientADoctorWalkParameter, false);
      animation.SetBool(PatientADoctorCprParameter, active);
    }

    private Animator ResolveAnimator(GameObject target)
      => target != null ? ResolveAnimator(target.transform) : null;

    private static Animator ResolveAnimator(Transform target)
    {
      if (target == null)
        return null;

      var animator = target.GetComponentInChildren<Animator>(true);
      if (animator != null)
        return animator;

      // The scenario-only ambu display is a static authored object. An Animator is
      // attached lazily so the blend-shape clip can be driven without a controller.
      return target.gameObject.AddComponent<Animator>();
    }

    private void PlayLoopingClip(Animator animator, AnimationClip clip, string targetDescription)
    {
      if (animator == null || clip == null)
      {
        Debug.LogWarning(
          $"[TriageScenarioEventBootstrap] CPR animation target or clip is missing ({targetDescription}).",
          this);
        return;
      }

      if (_patientACprAnimationGraphs.TryGetValue(animator, out var previous) && previous.IsValid())
        previous.Destroy();

      var graph = PlayableGraph.Create($"patient_a_critical:{clip.name}:{animator.GetInstanceID()}");
      graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
      var playable = AnimationClipPlayable.Create(graph, clip);
      playable.SetApplyFootIK(false);
      playable.SetApplyPlayableIK(false);
      var output = AnimationPlayableOutput.Create(graph, clip.name, animator);
      output.SetSourcePlayable(playable);
      graph.Play();
      _patientACprAnimationGraphs[animator] = graph;
    }
  }
}
