using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private const string DefaultRuntimeAnimatorControllerAssetPath = "Assets/Modules/Mixamo/AnimationControllers/PlayerCharacterModel.controller";
    private const string WalkAnimationParameterName = "walk";
    private const string JumpAnimationParameterName = "jump";

    [Header("Animation")]
    [SerializeField] private RuntimeAnimatorController _runtimeAnimatorController;

    private Animator _characterModelAnimator;
    private bool _hasAnimationParameterState;
    private bool _currentWalkAnimationParameter;
    private bool _currentJumpAnimationParameter;
    private bool _hasValidatedAnimationParameters;
    private bool _hasRequiredAnimationParameters;
    private bool _hasWarnedMissingAnimationParameters;
    // Hysteresis for walk detection to prevent jitter
    private bool _wasWalking;
    private const float WalkEnterThreshold = 0.1f;
    private const float WalkExitThreshold = 0.05f;
    // Jump 애니메이션이 재생 중인 동안 유지하는 상태 플래그
    private bool _isPlayingJumpAnimation;
    private const float JumpAnimationMinDuration = 0.4f; // 최소 점프 애니메이션 재생 시간(초)
    private float _jumpAnimationStartTime;

    private void Awake_Animation()
    {
      EnsureDefaultRuntimeAnimatorController();
      _hasAnimationParameterState = false;
      _isPlayingJumpAnimation = false;
      // _jumpAnimationStartTime은 _isPlayingJumpAnimation = true 시점에 항상 덮어쓰이므로 초기화 불필요
    }

#if UNITY_EDITOR
    private void Reset()
    {
      EnsureDefaultRuntimeAnimatorController();
    }

    private void OnValidate()
    {
      EnsureDefaultRuntimeAnimatorController();
      OnValidate_ReposableCarry();
    }
#endif

    private void Update_Animation()
    {
      if (!CanPlayAnimator(_characterModelAnimator))
        return;

      EnsureBaseLayerWeight(_characterModelAnimator);

      if (!HasRequiredAnimationParameters(_characterModelAnimator))
      {
        // 필수 파라미터가 없으면 SetBool/CrossFade 를 시도하지 않는다.
        // 경고는 최초 1회만 출력한다(매 프레임 로그 스팸 방지).
        if (!_hasWarnedMissingAnimationParameters)
        {
          _hasWarnedMissingAnimationParameters = true;
          Debug.LogWarning($"Animator '{_characterModelAnimator.name}' is missing required animation parameters. Walk parameter: '{WalkAnimationParameterName}', Jump parameter: '{JumpAnimationParameterName}'.", this);
        }
        return;
      }

      bool isJump = IsJumpingAnimationState();
      bool isWalk = !isJump && IsWalkingAnimationState();

      ApplyAnimationParameters(isWalk, isJump);
    }

    private bool IsJumpingAnimationState()
    {
      // 이번 프레임에 점프 입력이 들어왔으면 점프 애니메이션 시작
      if (_jumpAnimationRequestedThisFrame)
      {
        _isPlayingJumpAnimation = true;
        _jumpAnimationStartTime = Time.time;
        return true;
      }

      // 점프 애니메이션 최소 재생 시간이 지나지 않았으면 계속 점프 상태 유지
      if (_isPlayingJumpAnimation)
      {
        // _characterController가 null이면 방어적으로 착지 상태로 간주한다.
        // (character model 교체·씬 전환 등으로 null이 되면 영구 jump 상태 고착 방지)
        bool grounded = _characterController == null || _characterController.isGrounded;
        bool minDurationPassed = (Time.time - _jumpAnimationStartTime) >= JumpAnimationMinDuration;

        // 착지했고 최소 재생 시간이 지났을 때만 점프 애니메이션 종료
        if (grounded && minDurationPassed)
          _isPlayingJumpAnimation = false;
        else
          return true;
      }

      return false;
    }

private bool IsWalkingAnimationState()
    {
        if (_characterController == null)
            return false;

        Vector3 planarMove = _characterController.velocity;
        planarMove.y = 0f;
        float speedSqr = planarMove.sqrMagnitude;
        if (!_wasWalking && speedSqr > WalkEnterThreshold * WalkEnterThreshold)
        {
            _wasWalking = true;
        }
        else if (_wasWalking && speedSqr < WalkExitThreshold * WalkExitThreshold)
        {
            _wasWalking = false;
        }
        return _wasWalking;
    }

    private void SetCharacterModelAnimator(Animator animator)
    {
      _characterModelAnimator = animator;
      _hasAnimationParameterState = false;
      _currentWalkAnimationParameter = false;
      _currentJumpAnimationParameter = false;
      _hasValidatedAnimationParameters = false;
      _hasRequiredAnimationParameters = false;
      _hasWarnedMissingAnimationParameters = false;
      _isPlayingJumpAnimation = false;
      // _jumpAnimationStartTime은 _isPlayingJumpAnimation = true 시점에 항상 덮어쓰이므로 초기화 불필요

      ApplyRuntimeAnimatorController(_characterModelAnimator);

      if (!CanPlayAnimator(_characterModelAnimator))
        return;

      EnsureBaseLayerWeight(_characterModelAnimator);

      if (!HasRequiredAnimationParameters(_characterModelAnimator))
        return;

      ApplyAnimationParameters(isWalk: false, isJump: false);
    }

    private void ApplyAnimationParameters(bool isWalk, bool isJump)
    {
        if (_characterModelAnimator == null)
            return;

        bool changed = !_hasAnimationParameterState
            || _currentWalkAnimationParameter != isWalk
            || _currentJumpAnimationParameter != isJump;

        if (!changed)
            return;

        // 파라미터를 먼저 설정해 Animator 상태 머신이 조건 기반 트랜지션을 올바르게 평가하도록 한다.
        _characterModelAnimator.SetBool(WalkAnimationParameterName, isWalk);
        _characterModelAnimator.SetBool(JumpAnimationParameterName, isJump);

        // jump 전환에만 CrossFade로 즉시 인터럽트한다.
        // idle ↔ walk 전환은 Animator 상태 머신의 조건 트랜지션에 맡겨
        // 불필요한 CrossFade 호출로 인한 재생 끊김을 방지한다.
        if (_currentJumpAnimationParameter && !isJump)
        {
            // 점프 종료 후 walk 또는 idle로 즉시 전환
            string targetState = isWalk ? "walk" : "idle";
            _characterModelAnimator.CrossFade(targetState, 0.15f, 0);
        }
        else if (!_currentJumpAnimationParameter && isJump)
        {
            // jump 진입: 이전에 점프 상태가 아니었던 모든 경우에 즉시 CrossFade
            _characterModelAnimator.CrossFade("jump", 0f, 0);
        }

        _currentWalkAnimationParameter = isWalk;
        _currentJumpAnimationParameter = isJump;
        _hasAnimationParameterState = true;
    }

    private void ApplyRuntimeAnimatorController(Animator animator)
    {
      if (animator == null)
        return;

      if (_runtimeAnimatorController == null)
        return;

      animator.runtimeAnimatorController = _runtimeAnimatorController;
    }

    private void EnsureDefaultRuntimeAnimatorController()
    {
      if (_runtimeAnimatorController != null)
        return;

#if UNITY_EDITOR
      _runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DefaultRuntimeAnimatorControllerAssetPath);
#endif
    }

    private static bool CanPlayAnimator(Animator animator)
    {
      if (animator == null)
        return false;

      if (!animator.isActiveAndEnabled)
        return false;

      if (animator.runtimeAnimatorController == null)
        return false;

      return true;
    }

    private bool HasRequiredAnimationParameters(Animator animator)
    {
      if (_hasValidatedAnimationParameters)
        return _hasRequiredAnimationParameters;

      _hasValidatedAnimationParameters = true;
      _hasRequiredAnimationParameters = HasBoolParameter(animator, WalkAnimationParameterName)
        && HasBoolParameter(animator, JumpAnimationParameterName);
      return _hasRequiredAnimationParameters;
    }

    private static bool HasBoolParameter(Animator animator, string parameterName)
    {
      if (animator == null)
        return false;

      var parameters = animator.parameters;
      for (int i = 0; i < parameters.Length; i++)
      {
        if (parameters[i].type != AnimatorControllerParameterType.Bool)
          continue;

        if (parameters[i].name == parameterName)
          return true;
      }

      return false;
    }

    private static void EnsureBaseLayerWeight(Animator animator)
    {
      if (animator == null)
        return;

      if (animator.layerCount <= 0)
        return;

      if (animator.GetLayerWeight(0) >= 0.999f)
        return;

      animator.SetLayerWeight(0, 1f);
    }
  }
}
