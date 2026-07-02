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

    private void Awake_Animation()
    {
      EnsureDefaultRuntimeAnimatorController();
      _hasAnimationParameterState = false;
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
      return _jumpAnimationRequestedThisFrame;
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

        // Set parameters for any other logic that may read them
        _characterModelAnimator.SetBool(WalkAnimationParameterName, isWalk);
        _characterModelAnimator.SetBool(JumpAnimationParameterName, isJump);

        // Immediately cross‑fade to the target state to avoid waiting for exit time
        string targetState = isJump ? "Jump" : (isWalk ? "Walk" : "Idle");
        _characterModelAnimator.CrossFade(targetState, 0f, 0);

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
