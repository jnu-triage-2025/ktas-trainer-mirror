using Input = MultiplayerInfrastructure.Automation.PlayerInput;
using System.Collections.Generic;
using FishNet;
using MultiplayerInfrastructure.InteractableEntity;
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
      public Vector3 EntryPosition;
      public Quaternion EntryRotation;
      public bool DebugEscaped;
      public bool ReturnedToEntry;
    }

    private readonly struct AnimationRootPose
    {
      public readonly Vector3 LocalPosition;
      public readonly Quaternion LocalRotation;

      public AnimationRootPose(Transform target)
      {
        LocalPosition = target.localPosition;
        LocalRotation = target.localRotation;
      }
    }

    private readonly struct AnimationPoseAnchor
    {
      public readonly Transform Transform;
      public readonly Transform ReferenceFrame;
      public readonly Vector3 BasePosition;

      public AnimationPoseAnchor(Transform transform, Transform referenceFrame,
        Vector3 basePosition)
      {
        Transform = transform;
        ReferenceFrame = referenceFrame;
        BasePosition = basePosition;
      }
    }

    /// <summary>
    /// CPR PlayableGraph가 Animator를 점유하기 직전의 Animator Controller 재생 상태.
    /// 그래프를 제거한 뒤 Rebind로 컨트롤러 재생을 되돌릴 때 파라미터와 상태를 되돌리는 데 사용한다.
    /// </summary>
    private readonly struct AnimatorPlaybackParameter
    {
      private readonly int _nameHash;
      private readonly AnimatorControllerParameterType _type;
      private readonly float _floatValue;
      private readonly int _intValue;
      private readonly bool _boolValue;

      public AnimatorPlaybackParameter(Animator animator, AnimatorControllerParameter parameter)
      {
        _nameHash = parameter.nameHash;
        _type = parameter.type;
        _floatValue = parameter.type == AnimatorControllerParameterType.Float
          ? animator.GetFloat(parameter.nameHash)
          : 0f;
        _intValue = parameter.type == AnimatorControllerParameterType.Int
          ? animator.GetInteger(parameter.nameHash)
          : 0;
        _boolValue = parameter.type == AnimatorControllerParameterType.Bool
                     && animator.GetBool(parameter.nameHash);
      }

      public void ApplyTo(Animator animator)
      {
        switch (_type)
        {
          case AnimatorControllerParameterType.Float:
            animator.SetFloat(_nameHash, _floatValue);
            break;
          case AnimatorControllerParameterType.Int:
            animator.SetInteger(_nameHash, _intValue);
            break;
          case AnimatorControllerParameterType.Bool:
            animator.SetBool(_nameHash, _boolValue);
            break;
        }
      }
    }

    private sealed class AnimatorPlaybackState
    {
      public int StateHash;
      public float NormalizedTime;
      public AnimatorPlaybackParameter[] Parameters;
    }

    private const string PatientACprRoundOneNodeIdentifier = "E028";
    private const string PatientACprRoundTwoNodeIdentifier = "E033";
    private const string PatientACprRoundOneNurseTag = "nurse_b";
    private const string PatientACprRoundTwoNurseTag = "nurse_a";
    private const string PatientACprModelOffsetRootName = "CPRModelOffsetRoot";
    private const int PatientACprAnimatorLayer = 0;

    /// <summary>
    /// CPR 진입 위치가 환자(=침대 중앙)와 이만큼도 떨어져 있지 않으면 퇴장 위치로 쓸 수 없다.
    /// 플레이어 CharacterController 반지름(0.5)과 환자 누움 콜라이더 반지름(0.3)을 합친 값이다.
    /// </summary>
    private const float PatientACprPerformerExitClearance = 0.9f;
    private static readonly Vector3 PatientACprPerformerPositionDelta =
      new(0f, 0.15f, -0.3f);
    private static readonly Vector3 PatientACprPerformerRotationDelta =
      new(0f, -90f, 0f);

    private readonly Dictionary<Animator, PlayableGraph> _patientACprAnimationGraphs = new();
    private readonly Dictionary<Animator, AnimationClip> _patientACprAnimationClips = new();
    private readonly Dictionary<Animator, Transform> _patientACprAnimationOffsetRoots = new();
    private readonly Dictionary<Animator, Vector3> _patientACprAnimationOffsetBasePositions = new();
    private readonly Dictionary<Animator, AnimationRootPose> _patientACprAnimationBaseRootPoses = new();
    private readonly Dictionary<Animator, AnimationPoseAnchor> _patientACprAnimationPoseAnchors = new();
    private readonly Dictionary<Animator, AnimatorPlaybackState> _patientACprAnimatorPlaybackStates = new();
    private readonly Dictionary<PlayerController, PatientACprPerformerState> _patientACprPerformers = new();

    private void PlayPatientAChestCompressionAnimation()
      => PlayPatientAChestCompressionAnimation(ResolvePatientACprNurseTag());

    /// <summary>
    /// 지정한 역할이 가슴압박을 수행하는 CPR 연출을 이 피어에서 재생한다.
    /// 연출 전용 피어는 <c>CurrentNode</c> 로 라운드를 판정할 수 없으므로, 권위 피어가 판정한
    /// 역할 태그를 받아서 그대로 사용한다.
    /// </summary>
    private void PlayPatientAChestCompressionAnimation(string nurseTag)
    {
      ResolveRuntimeReferencesIfNeeded();
      PlayPatientACprNurseAnimation(nurseTag);
      PlayLoopingClip(ResolveAnimator(_patientAObject), _cprReceivingPatientAnimationClip, "patient A",
        _cprReceivingPatientPositionOffset);
    }

    /// <summary>
    /// 그래프를 순회하는 피어에서 현재 CPR 라운드의 가슴압박 역할 태그를 판정한다.
    /// 표시 전용 피어에는 InvokeEvent 노드가 <c>CurrentNode</c> 로 남지 않으므로 이 판정을 쓸 수 없다.
    /// </summary>
    private static string ResolvePatientACprNurseTag()
    {
      string currentNodeIdentifier = ScenarioController.Instance?.CurrentNode?.Identifier;
      return currentNodeIdentifier switch
      {
        PatientACprRoundTwoNodeIdentifier => PatientACprRoundTwoNurseTag,
        PatientACprRoundOneNodeIdentifier => PatientACprRoundOneNurseTag,
        _ => PatientACprRoundOneNurseTag
      };
    }

    /// <summary>
    /// 권위 피어가 판정한 라운드를 표시 피어에 전달하기 위한 연출 이벤트 식별자를 고른다.
    /// </summary>
    private static string ResolvePatientACprRoundPresentationEventIdentifier()
      => ResolvePatientACprNurseTag() == PatientACprRoundTwoNurseTag
        ? PatientACprRoundTwoPresentationEventIdentifier
        : PatientACprRoundOnePresentationEventIdentifier;

    /// <summary>
    /// 레지스트리 항목이 수행되었을 때. 환자 A 의 가슴압박 액션(1주기 <c>click_to_start_comp</c>, 2주기 <c>interact_chest</c>)이면
    /// 수행자를 환자 옆에 세우고 압박 애니메이션을 재생한다. 액션은 시나리오 데이터가 정의하고 레지스트리가 범용 핸들러로 수행한다.
    /// </summary>
    private void HandleRegistryInteractionCompleted(InteractionRegistryEntry entry, PlayerController player)
    {
      if (entry == null || player == null)
        return;

      string interaction = entry.Address.InteractionIdentifier;
      if (interaction != "click_to_start_comp" && interaction != "interact_chest")
        return;

      ResolveRuntimeReferencesIfNeeded();
      PatientController patient = null;
      if (MultiplayerInfrastructure.Registry.Registry.TryGetEntity(entry.Address.EntityIdentifier, out var descriptor)
          && descriptor?.GameObject != null)
        patient = descriptor.GameObject.GetComponentInChildren<PatientController>(true);
      if (patient == null && _patientAObject != null)
        patient = _patientAObject.GetComponentInChildren<PatientController>(true);

      string nurseTag = interaction == "interact_chest" ? PatientACprRoundTwoNurseTag : PatientACprRoundOneNurseTag;
      PositionPatientACprPerformer(player, patient);
      PlayPatientACprNurseAnimation(player, nurseTag);
      PlayLoopingClip(ResolveAnimator(_patientAObject), _cprReceivingPatientAnimationClip, "patient A",
        _cprReceivingPatientPositionOffset);
    }

    private void PlayPatientACprNurseAnimation(string nurseTag)
    {
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
        RestorePatientACprPerformerFromDebugEscape(player);
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
        RestorePatientACprPerformerFromDebugEscape(player);
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
      var animators = new List<Animator>(_patientACprAnimationGraphs.Keys);
      for (int i = 0; i < animators.Count; i++)
      {
        Animator animator = animators[i];
        StopAndRestoreAnimation(animator);
      }

      _patientACprAnimationGraphs.Clear();
      _patientACprAnimationClips.Clear();
      _patientACprAnimationOffsetRoots.Clear();
      _patientACprAnimationOffsetBasePositions.Clear();
      _patientACprAnimationBaseRootPoses.Clear();
      _patientACprAnimationPoseAnchors.Clear();
      _patientACprAnimatorPlaybackStates.Clear();
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
          Anchor = anchorObject.transform,
          EntryPosition = ResolvePatientACprPerformerEntryPosition(player, patient),
          EntryRotation = player.transform.rotation
        };
        _patientACprPerformers[player] = state;
      }
      else
      {
        state.Patient = patient;

        // 이미 퇴장 처리된 수행자가 다시 가슴압박에 진입하면 그 시점의 위치가 새 진입 위치다.
        // (디버그 Escape 이후 다른 곳에서 재진입한 경우를 포함한다.)
        if (state.ReturnedToEntry)
        {
          state.EntryPosition = ResolvePatientACprPerformerEntryPosition(player, patient);
          state.EntryRotation = player.transform.rotation;
          state.ReturnedToEntry = false;
        }

        // 새 가슴압박 진입은 퇴장 위치 복귀 여부와 무관하게 항상 디버그 Escape 상태를 초기화한다.
        // 이전 주기의 Escape가 남으면 TryDebugEscapePatientACprPerformer 가 곧바로 실패하고
        // LateUpdate 의 매 프레임 정렬도 건너뛰므로, 진입 시 한 번 걸린 앵커가 해제되지 않아
        // 수행자가 CPR 좌표에 못박힌 채 조작 불능이 된다.
        state.DebugEscaped = false;
      }

      AlignPatientACprPerformer(state);
      player.SetMovementSuppressed(state.Anchor, true);
      player.SetRidableExitSuppressed(state.Anchor, true);
    }

    private void LateUpdate()
    {
      StabilizePatientACprAnimationRootPoses();

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
    }

    /// <summary>
    /// Humanoid CPR 클립의 RootT는 Animator Transform이 아니라 Hips에 적용된다. 그래프 평가 후
    /// Hips가 재생 전 기준점에 남도록 전용 모델 루트를 보정한다. 시작 시 적용한 모델 오프셋은
    /// RootT 침하를 상쇄하기 위한 초기값일 뿐 목표 높이에 다시 더하지 않는다.
    /// 기준점을 침대에 붙은 부모 좌표계에 기록하므로 침대가 이동하거나 회전해도 함께 따라간다.
    /// </summary>
    private void StabilizePatientACprAnimationRootPoses()
    {
      foreach (var pair in _patientACprAnimationBaseRootPoses)
      {
        Animator animator = pair.Key;
        if (animator == null
            || !_patientACprAnimationGraphs.TryGetValue(animator, out var graph)
            || !graph.IsValid())
          continue;

        AnimationRootPose rootPose = pair.Value;
        animator.transform.SetLocalPositionAndRotation(rootPose.LocalPosition, rootPose.LocalRotation);

        if (!_patientACprAnimationPoseAnchors.TryGetValue(animator, out AnimationPoseAnchor anchor)
            || anchor.Transform == null || anchor.ReferenceFrame == null
            || !_patientACprAnimationOffsetRoots.TryGetValue(animator, out Transform offsetRoot)
            || offsetRoot == null)
          continue;

        Vector3 expectedPosition = anchor.BasePosition;
        Vector3 currentPosition = anchor.ReferenceFrame.InverseTransformPoint(anchor.Transform.position);
        offsetRoot.localPosition += expectedPosition - currentPosition;
      }
    }

    /// <summary>
    /// 단독 디버깅 중인 로컬 수행자의 표현과 위치 고정만 해제한다.
    /// 이 동작은 CPR 완료 신호를 발생시키거나 수행자 상태를 제거하지 않는다. 정상 CPR 종료가
    /// 도착할 때까지 <see cref="PatientACprPerformerState.DebugEscaped"/>를 유지하되, Escape 순간의
    /// 위치는 기록하거나 종료 위치로 사용하지 않는다.
    /// 앵커만 풀면 수행자가 환자·침대 콜라이더 안에 남아 CharacterController가 Y로 밀어 올리므로,
    /// 정상 종료와 동일하게 CPR 진입 직전 위치(=침대 밖 X/Z)로 되돌린 뒤 자유 이동을 넘긴다.
    /// </summary>
    private bool TryDebugEscapePatientACprPerformer(PatientACprPerformerState state)
    {
      if (state?.Player == null || state.Anchor == null || state.DebugEscaped
          || (!InstanceFinder.IsOffline && !state.Player.IsOwner)
          || !ScenarioGameRules.DEBUG_INT_CPR_PLAYING_ESCAPE_KEY
          || !Input.GetKeyDown(KeyCode.LeftShift))
        return false;

      state.DebugEscaped = true;
      state.Player.ClearForcedFollowAnchor(state.Anchor);
      state.Player.SetMovementSuppressed(state.Anchor, false);
      state.Player.SetRidableExitSuppressed(state.Anchor, false);
      ReturnPatientACprPerformerToEntry(state);
      StopPatientACprPerformerAnimation(state.Player);
      return true;
    }

    /// <summary>
    /// 수행자의 가슴압박 연출이 다시 재생되면 디버그 Escape 상태를 되돌린다.
    /// Escape는 재생 중인 연출을 잠시 해제하는 디버그 수단일 뿐이므로, 새로 시작된 연출을 넘어
    /// 유지되어서는 안 된다. 유지되면 Escape 입력과 매 프레임 정렬이 함께 막혀 수행자가 CPR
    /// 좌표에 고정된 채로 남는다. 시나리오 이벤트(E028/E033)가 상호작용과 별개로 연출을 다시
    /// 거는 경로가 있으므로, 진입 처리뿐 아니라 연출 재생 경로에서도 상태를 맞춰야 한다.
    /// </summary>
    private void RestorePatientACprPerformerFromDebugEscape(PlayerController player)
    {
      if (player == null
          || !_patientACprPerformers.TryGetValue(player, out PatientACprPerformerState state)
          || state == null || !state.DebugEscaped || state.Anchor == null)
        return;

      state.DebugEscaped = false;
      AlignPatientACprPerformer(state);
      player.SetMovementSuppressed(state.Anchor, true);
      player.SetRidableExitSuppressed(state.Anchor, true);
    }

    /// <summary>
    /// 수행자를 CPR 진입 직전 위치·회전으로 되돌린다. 이미 되돌린 수행자는 다시 옮기지 않는다.
    /// (디버그 Escape로 퇴장한 뒤 스스로 이동한 플레이어를 정상 종료 시점에 다시 끌어오지 않기 위함)
    /// </summary>
    private static void ReturnPatientACprPerformerToEntry(PatientACprPerformerState state)
    {
      if (state?.Player == null || state.ReturnedToEntry)
        return;

      state.ReturnedToEntry = true;
      state.Player.MoveToPositionPreservingForcedFollowAnchor(state.EntryPosition);
      state.Player.transform.rotation = state.EntryRotation;
    }

    /// <summary>
    /// CPR 진입 위치를 기록한다. 앵커에 이미 고정된 상태에서 상태가 새로 만들어지는 등의 이유로
    /// 환자(=침대 중앙)와 겹치는 좌표가 들어오면 그대로 기록하지 않고, 환자로부터 멀어지는 방향으로
    /// X/Z를 밀어낸 좌표를 진입 위치로 삼는다. 침대 안에서 해제되어 Y로 솟아오르는 것을 막는다.
    /// </summary>
    private static Vector3 ResolvePatientACprPerformerEntryPosition(
      PlayerController player, PatientController patient)
    {
      Vector3 entryPosition = player.transform.position;
      if (patient == null)
        return entryPosition;

      Vector3 patientPosition = patient.transform.position;
      var horizontal = new Vector2(
        entryPosition.x - patientPosition.x, entryPosition.z - patientPosition.z);
      if (horizontal.sqrMagnitude
          >= PatientACprPerformerExitClearance * PatientACprPerformerExitClearance)
        return entryPosition;

      Vector3 awayDirection = horizontal.sqrMagnitude > 0.0001f
        ? new Vector3(horizontal.x, 0f, horizontal.y).normalized
        : -patient.transform.right;
      Vector3 clearedPosition =
        patientPosition + awayDirection * PatientACprPerformerExitClearance;
      return new Vector3(clearedPosition.x, entryPosition.y, clearedPosition.z);
    }

    private void StopPatientACprPerformerAnimation(PlayerController player)
    {
      Animator animator = ResolvePlayerCharacterAnimator(player);
      if (animator == null)
        return;

      StopAndRestoreAnimation(animator);
    }

    private void AlignPatientACprPerformer(PatientACprPerformerState state)
    {
      if (state?.Player == null || state.Patient == null || state.Anchor == null)
        return;

      Vector3 patientPosition = state.Patient.transform.position;
      Vector3 anchorPosition =
        new Vector3(patientPosition.x, patientPosition.y + _cprPerformingPlayerHeightOffset,
          patientPosition.z) + PatientACprPerformerPositionDelta;
      Vector3 anchorRotation =
        new Vector3(0f, state.Patient.transform.eulerAngles.y + 180f, 0f)
        + PatientACprPerformerRotationDelta;
      state.Anchor.SetPositionAndRotation(
        anchorPosition,
        Quaternion.Euler(anchorRotation));

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
          state.Player.ClearForcedFollowAnchor(state.Anchor);
          // 디버그 Escape 순간의 CPR 앵커 좌표는 퇴장 위치로 사용하지 않는다. 정상 종료와
          // 디버그 Escape 모두 CPR 진입 직전 위치로 복귀하여 침대 중앙과의 중첩을 피한다.
          // (Escape 시점에 이미 복귀했다면 그 뒤 스스로 이동한 위치를 존중한다.)
          ReturnPatientACprPerformerToEntry(state);
          state.Player.SetMovementSuppressed(state.Anchor, false);
          state.Player.SetRidableExitSuppressed(state.Anchor, false);
        }

        if (state?.Anchor != null)
          Destroy(state.Anchor.gameObject);
      }

      _patientACprPerformers.Clear();
    }

    private void CompletePatientACprCycle()
    {
      // stop_ambu_and_comp(E031/E035)는 수행자뿐 아니라 환자의 CPR 연출도 종료한다.
      // 그래프 종료와 모델 Y 오프셋 복원을 반드시 한 경로에서 함께 수행해야 한다.
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

      // 시나리오 전용 앰부 표시는 정적으로 작성된 오브젝트다. Animator 를 늦게
      // 붙여 컨트롤러 없이 블렌드셰이프 클립을 구동할 수 있게 한다.
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

      bool replacedPreviousGraph = _patientACprAnimationGraphs.TryGetValue(animator, out var previous);

      // 같은 연출이 이미 재생 중이면 다시 시작하지 않는다. 가슴압박에 상호작용한 피어는 연출을
      // 로컬로 시작하고 곧이어 서버의 연출 통지도 받으므로, 막지 않으면 그 피어에서만 클립이 한 번
      // 처음으로 되감긴다.
      if (replacedPreviousGraph && previous.IsValid()
          && _patientACprAnimationClips.TryGetValue(animator, out AnimationClip playingClip)
          && playingClip == clip)
        return;

      if (replacedPreviousGraph)
      {
        if (previous.IsValid())
          previous.Destroy();

        _patientACprAnimationGraphs.Remove(animator);
        _patientACprAnimationClips.Remove(animator);
        RestoreAnimationPositionOffset(animator);
      }

      Transform offsetRoot = null;
      if (positionOffset != Vector3.zero)
      {
        offsetRoot = ResolvePatientACprModelOffsetRoot(animator);
        if (offsetRoot == null)
        {
          // 전용 루트가 없는 환자나 모델에는 루트 위치 곡선이 포함된 CPR 클립을 적용하지 않는다.
          // Animator Transform을 직접 보정하는 fallback은 침대 attachment와 다시 충돌한다.
          Debug.LogWarning(
            $"[TriageScenarioEventBootstrap] CPR model offset root was not found ({targetDescription}).",
            animator);

          // 새 그래프를 걸지 않고 빠져나가므로, 방금 제거한 그래프가 점유하던 Animator는
          // 여기서 컨트롤러 재생으로 되돌려 놓아야 CPR 자세가 남지 않는다.
          if (replacedPreviousGraph)
            RestorePatientACprAnimatorPlayback(animator);

          return;
        }
      }

      // CPR 표현이 이미 걸려 있는 Animator라면 기준 자세와 오프셋 기준값을 다시 잡지 않는다.
      // 그래프가 덮어쓴 CPR 자세를 "원래 자세"로 기록하면 종료 시 그 자세로 복원되어 버린다.
      if (!_patientACprAnimationBaseRootPoses.ContainsKey(animator))
        _patientACprAnimationBaseRootPoses[animator] = new AnimationRootPose(animator.transform);

      if (offsetRoot != null && !_patientACprAnimationPoseAnchors.ContainsKey(animator))
      {
        Transform referenceFrame = offsetRoot.parent;
        Transform poseAnchor = animator.isHuman
          ? animator.GetBoneTransform(HumanBodyBones.Hips)
          : animator.transform;
        if (referenceFrame != null && poseAnchor != null)
        {
          _patientACprAnimationPoseAnchors[animator] = new AnimationPoseAnchor(
            poseAnchor, referenceFrame, referenceFrame.InverseTransformPoint(poseAnchor.position));
        }
      }

      CapturePatientACprAnimatorPlayback(animator);

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
        if (!_patientACprAnimationOffsetRoots.ContainsKey(animator))
        {
          _patientACprAnimationOffsetRoots[animator] = offsetRoot;
          _patientACprAnimationOffsetBasePositions[animator] = offsetRoot.localPosition;
          offsetRoot.localPosition += positionOffset;
        }

        output.SetSourcePlayable(playable);
      }
      graph.Play();
      _patientACprAnimationGraphs[animator] = graph;
      _patientACprAnimationClips[animator] = clip;
    }

    private static Transform ResolvePatientACprModelOffsetRoot(Animator animator)
    {
      for (Transform current = animator != null ? animator.transform.parent : null;
           current != null;
           current = current.parent)
        if (current.name == PatientACprModelOffsetRootName)
          return current;

      return null;
    }

    private void RestoreAnimationPositionOffset(Animator animator)
    {
      if (animator != null
          && _patientACprAnimationBaseRootPoses.TryGetValue(animator, out var rootPose))
      {
        animator.transform.SetLocalPositionAndRotation(rootPose.LocalPosition, rootPose.LocalRotation);
      }

      if (animator != null
          && _patientACprAnimationOffsetRoots.TryGetValue(animator, out var offsetRoot)
          && offsetRoot != null
          && _patientACprAnimationOffsetBasePositions.TryGetValue(animator, out var basePosition))
        offsetRoot.localPosition = basePosition;

      _patientACprAnimationOffsetRoots.Remove(animator);
      _patientACprAnimationOffsetBasePositions.Remove(animator);
      _patientACprAnimationBaseRootPoses.Remove(animator);
      _patientACprAnimationPoseAnchors.Remove(animator);
    }

    private void StopAndRestoreAnimation(Animator animator)
    {
      if (animator == null)
        return;

      if (_patientACprAnimationGraphs.TryGetValue(animator, out var graph) && graph.IsValid())
        graph.Destroy();

      _patientACprAnimationGraphs.Remove(animator);
      _patientACprAnimationClips.Remove(animator);

      // 그래프를 제거하는 것만으로는 Animator가 자기 Controller 재생으로 돌아오지 않고 마지막 CPR
      // 자세가 그대로 남는다. 자세가 남은 채로 전용 Y 오프셋만 제거하면 환자가 침대 아래로 내려앉으므로,
      // 먼저 컨트롤러 재생을 실제로 되돌린 뒤에 CPR RootT/RootQ와 Y 오프셋을 복원해야 한다.
      RestorePatientACprAnimatorPlayback(animator);
      RestoreAnimationPositionOffset(animator);
    }

    /// <summary>
    /// CPR PlayableGraph가 점유하기 직전의 Animator Controller 재생 상태를 기록한다.
    /// 이미 CPR 표현이 진행 중인 Animator는 다시 기록하지 않는다. CPR 재생 중의 상태를 "원래 상태"로
    /// 덮어써 버리면 종료 시 되돌아갈 자세를 잃는다.
    /// </summary>
    private void CapturePatientACprAnimatorPlayback(Animator animator)
    {
      if (animator == null || animator.runtimeAnimatorController == null
          || !animator.isActiveAndEnabled || animator.layerCount <= PatientACprAnimatorLayer
          || _patientACprAnimatorPlaybackStates.ContainsKey(animator))
        return;

      var parameters = animator.parameters;
      var capturedParameters = new List<AnimatorPlaybackParameter>(parameters.Length);
      for (int i = 0; i < parameters.Length; i++)
      {
        AnimatorControllerParameter parameter = parameters[i];
        if (parameter.type == AnimatorControllerParameterType.Trigger
            || animator.IsParameterControlledByCurve(parameter.nameHash))
          continue;

        capturedParameters.Add(new AnimatorPlaybackParameter(animator, parameter));
      }

      AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(PatientACprAnimatorLayer);
      _patientACprAnimatorPlaybackStates[animator] = new AnimatorPlaybackState
      {
        StateHash = stateInfo.fullPathHash,
        NormalizedTime = Mathf.Repeat(stateInfo.normalizedTime, 1f),
        Parameters = capturedParameters.ToArray()
      };
    }

    /// <summary>
    /// CPR 그래프 종료 이후 Animator를 자기 Controller 재생으로 되돌린다.
    /// Rebind로 그래프 점유 흔적을 지운 뒤 기록해 둔 파라미터와 상태를 복구하고 즉시 한 번 평가하여,
    /// CPR 골격 자세가 남은 채로 높이 보정만 사라지는 상태를 만들지 않는다.
    /// </summary>
    private void RestorePatientACprAnimatorPlayback(Animator animator)
    {
      // 기록이 없다는 것은 Controller 재생을 빼앗은 적이 없다는 뜻이므로 되돌릴 것도 없다.
      // (연출 전용으로 Animator만 붙인 오브젝트가 여기에 해당한다.)
      if (animator == null
          || !_patientACprAnimatorPlaybackStates.TryGetValue(animator, out AnimatorPlaybackState playback))
        return;

      _patientACprAnimatorPlaybackStates.Remove(animator);

      if (animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
      {
        Debug.LogWarning(
          "[TriageScenarioEventBootstrap] CPR animation stopped, but the animator cannot resume "
          + "controller playback; the last CPR pose may remain.", animator);
        return;
      }

      animator.Rebind();

      for (int i = 0; i < playback.Parameters.Length; i++)
        playback.Parameters[i].ApplyTo(animator);

      if (playback.StateHash != 0)
        animator.Play(playback.StateHash, PatientACprAnimatorLayer, playback.NormalizedTime);

      // PlayableGraph를 제거한 프레임에는 마지막 CPR 골격 자세가 화면에 남을 수 있다.
      // 복구한 Animator Controller를 즉시 한 번 평가하여 같은 프레임에 원래 자세로 되돌린다.
      animator.Update(0f);
    }
  }
}
