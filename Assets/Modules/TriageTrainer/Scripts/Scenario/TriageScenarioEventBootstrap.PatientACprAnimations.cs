using System.Collections.Generic;
using FishNet;
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
    private sealed class PatientACprPerformerState
    {
      public PlayerController Player;
      public PatientController Patient;
      public Transform Anchor;
      public bool DebugEscaped;
      public Vector3 DebugEscapePosition;
    }

    private const string PatientACprRoundOneNodeIdentifier = "E028";
    private const string PatientACprRoundTwoNodeIdentifier = "E033";
    private const string PatientACprRoundOneNurseTag = "nurse_b";
    private const string PatientACprRoundTwoNurseTag = "nurse_a";

    private readonly Dictionary<Animator, PlayableGraph> _patientACprAnimationGraphs = new();
    private readonly Dictionary<Animator, Vector3> _patientACprAnimationBasePositions = new();
    private readonly Dictionary<Animator, Vector3> _patientACprAnimationPositionOffsets = new();
    private readonly Dictionary<PlayerController, PatientACprPerformerState> _patientACprPerformers = new();

    private void PlayPatientAChestCompressionAnimation()
    {
      ResolveRuntimeReferencesIfNeeded();
      PlayPatientACprNurseAnimation();
      PlayLoopingClip(ResolveAnimator(_patientAObject), _cprReceivingPatientAnimationClip, "patient A",
        _cprReceivingPatientPositionOffset);
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
      PatientController patient = action.GetComponentInParent<PatientController>(true);
      if (patient == null && _patientAObject != null)
        patient = _patientAObject.GetComponentInChildren<PatientController>(true);

      PositionPatientACprPerformer(player, patient);
      PlayPatientACprNurseAnimation(player, action.RequiredPlayerTag);
      PlayLoopingClip(ResolveAnimator(_patientAObject), _cprReceivingPatientAnimationClip, "patient A",
        _cprReceivingPatientPositionOffset);
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
      foreach (var pair in _patientACprAnimationGraphs)
      {
        if (pair.Value.IsValid())
          pair.Value.Destroy();

        RestoreAnimationBasePosition(pair.Key);
      }

      _patientACprAnimationGraphs.Clear();
      _patientACprAnimationBasePositions.Clear();
      _patientACprAnimationPositionOffsets.Clear();
      ReleasePatientACprPerformers();
    }

    private void PositionPatientACprPerformer(PlayerController player, PatientController patient)
    {
      if (player == null || patient == null)
        return;

      if (!_patientACprPerformers.TryGetValue(player, out var state) || state?.Anchor == null)
      {
        var anchorObject = new GameObject($"PatientACprPlayerAnchor:{player.GetInstanceID()}");
        state = new PatientACprPerformerState
        {
          Player = player,
          Patient = patient,
          Anchor = anchorObject.transform
        };
        _patientACprPerformers[player] = state;
      }
      else
      {
        state.Patient = patient;
      }

      AlignPatientACprPerformer(state);
      player.SetMovementSuppressed(state.Anchor, true);
    }

    private void LateUpdate()
    {
      foreach (var pair in _patientACprPerformers)
      {
        PatientACprPerformerState state = pair.Value;
        if (state == null)
          continue;

        if (TryDebugEscapePatientACprPerformer(state))
          continue;

        if (!state.DebugEscaped)
          AlignPatientACprPerformer(state);
      }

      ApplyPatientACprAnimationPositionOffsets();
    }

    /// <summary>
    /// 단독 디버깅 중인 로컬 수행자의 표현과 위치 고정만 해제한다.
    /// 이 동작은 CPR 완료 신호를 발생시키거나 수행자 상태를 제거하지 않는다. 정상 CPR 종료가
    /// 도착할 때까지 <see cref="PatientACprPerformerState.DebugEscaped"/>를 유지해야 종료 시점에
    /// 디버그 해제 지점으로 복귀할 수 있다.
    /// </summary>
    private bool TryDebugEscapePatientACprPerformer(PatientACprPerformerState state)
    {
      if (state?.Player == null || state.Anchor == null || state.DebugEscaped
          || (!InstanceFinder.IsOffline && !state.Player.IsOwner)
          || !ScenarioGameRules.DEBUG_INT_CPR_PLAYING_ESCAPE_KEY
          || !Input.GetKeyDown(KeyCode.LeftShift))
        return false;

      state.DebugEscaped = true;
      state.DebugEscapePosition = state.Player.transform.position;
      state.Player.ClearForcedFollowAnchor(state.Anchor);
      state.Player.SetMovementSuppressed(state.Anchor, false);
      StopPatientACprPerformerAnimation(state.Player);
      return true;
    }

    private void StopPatientACprPerformerAnimation(PlayerController player)
    {
      Animator animator = ResolvePlayerCharacterAnimator(player);
      if (animator == null)
        return;

      if (_patientACprAnimationGraphs.TryGetValue(animator, out var graph) && graph.IsValid())
        graph.Destroy();

      _patientACprAnimationGraphs.Remove(animator);
      RestoreAnimationBasePosition(animator);
    }

    private void AlignPatientACprPerformer(PatientACprPerformerState state)
    {
      if (state?.Player == null || state.Patient == null || state.Anchor == null)
        return;

      Vector3 patientPosition = state.Patient.transform.position;
      state.Anchor.SetPositionAndRotation(
        new Vector3(patientPosition.x, patientPosition.y + _cprPerformingPlayerHeightOffset,
          patientPosition.z),
        Quaternion.Euler(0f, state.Patient.transform.eulerAngles.y + 180f, 0f));

      state.Player.AlignYawTo(state.Anchor.forward);
      state.Player.SetForcedFollowAnchor(state.Anchor);
    }

    private void ReleasePatientACprPerformers()
    {
      foreach (var pair in _patientACprPerformers)
      {
        PatientACprPerformerState state = pair.Value;
        if (state?.Player != null)
        {
          // 디버그 Escape는 시스템상 CPR을 끝내지 않는다. 따라서 정상 종료가 도착하면
          // 이동 중인 현재 위치가 아니라, 디버그로 위치 고정을 해제했던 장소로 먼저 복귀시킨다.
          if (state.DebugEscaped && state.Anchor != null)
          {
            // 디버그 이탈 중 다른 이동 시스템이 앵커를 획득했을 수 있으므로 CPR 앵커를
            // 재설정하지 않는다. CharacterController만 안전하게 우회하여 저장 위치로 복귀한다.
            state.Player.MoveToPositionPreservingForcedFollowAnchor(state.DebugEscapePosition);
          }

          state.Player.ClearForcedFollowAnchor(state.Anchor);
          state.Player.SetMovementSuppressed(state.Anchor, false);
        }

        if (state?.Anchor != null)
          Destroy(state.Anchor.gameObject);
      }

      _patientACprPerformers.Clear();
    }

    private void CompletePatientACprCycle()
    {
      StopPatientACprAnimations();
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

    private void PlayLoopingClip(Animator animator, AnimationClip clip, string targetDescription,
      Vector3 positionOffset = default)
    {
      if (animator == null || clip == null)
      {
        Debug.LogWarning(
          $"[TriageScenarioEventBootstrap] CPR animation target or clip is missing ({targetDescription}).",
          this);
        return;
      }

      if (_patientACprAnimationGraphs.TryGetValue(animator, out var previous) && previous.IsValid())
      {
        previous.Destroy();
        RestoreAnimationBasePosition(animator);
      }

      var graph = PlayableGraph.Create($"patient_a_critical:{clip.name}:{animator.GetInstanceID()}");
      graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
      var playable = AnimationClipPlayable.Create(graph, clip);
      playable.SetApplyFootIK(false);
      playable.SetApplyPlayableIK(false);
      var output = AnimationPlayableOutput.Create(graph, clip.name, animator);
      if (positionOffset == Vector3.zero)
      {
        output.SetSourcePlayable(playable);
      }
      else
      {
        // AnimationOffsetPlayable is no longer publicly accessible in Unity 6.
        // Apply the equivalent local root offset to the authored target instead.
        _patientACprAnimationBasePositions[animator] = animator.transform.localPosition;
        _patientACprAnimationPositionOffsets[animator] = positionOffset;
        output.SetSourcePlayable(playable);
      }
      graph.Play();
      _patientACprAnimationGraphs[animator] = graph;
    }

    private void ApplyPatientACprAnimationPositionOffsets()
    {
      // CPR 클립에는 RootT 곡선이 있으므로 재생 전에 한 번 이동하는 방식은 그래프 평가 때
      // 덮어써진다. Animator 평가가 끝난 LateUpdate에서 기준 위치와 오프셋을 계속 유지한다.
      foreach (var pair in _patientACprAnimationPositionOffsets)
      {
        Animator animator = pair.Key;
        if (animator != null && _patientACprAnimationBasePositions.TryGetValue(animator, out var basePosition))
          animator.transform.localPosition = basePosition + pair.Value;
      }
    }

    private void RestoreAnimationBasePosition(Animator animator)
    {
      if (animator != null && _patientACprAnimationBasePositions.TryGetValue(animator, out var basePosition))
        animator.transform.localPosition = basePosition;

      _patientACprAnimationBasePositions.Remove(animator);
      _patientACprAnimationPositionOffsets.Remove(animator);
    }
  }
}
