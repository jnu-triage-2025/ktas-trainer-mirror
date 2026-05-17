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
        Debug.LogWarning($"Animator '{_characterModelAnimator.name}' is missing required animation parameters. Walk parameter: '{WalkAnimationParameterName}', Jump parameter: '{JumpAnimationParameterName}'.", this);

      bool isJump = IsJumpingAnimationState();
      bool isWalk = !isJump && IsWalkingAnimationState();

      ApplyAnimationParameters(isWalk, isJump);
    }

    private bool IsJumpingAnimationState()
    {
      if (_characterController == null)
        _characterController = GetComponent<CharacterController>();

      return _characterController != null && !_characterController.isGrounded;
    }

    private bool IsWalkingAnimationState()
    {
      if (!canMove)
        return false;

      if (_characterController == null)
        _characterController = GetComponent<CharacterController>();

      if (_characterController == null || !_characterController.isGrounded)
        return false;

      Vector3 planarMoveDirection = _moveDirection;
      planarMoveDirection.y = 0f;
      return planarMoveDirection.sqrMagnitude > 0.0001f;
    }

    private void SetCharacterModelAnimator(Animator animator)
    {
      _characterModelAnimator = animator;
      _hasAnimationParameterState = false;
      _currentWalkAnimationParameter = false;
      _currentJumpAnimationParameter = false;
      _hasValidatedAnimationParameters = false;
      _hasRequiredAnimationParameters = false;

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
      Debug.Log($"Applying animation parameters: isWalk={isWalk}, isJump={isJump}", this);
      if (_characterModelAnimator == null)
        return;

      bool changed = !_hasAnimationParameterState
        || _currentWalkAnimationParameter != isWalk
        || _currentJumpAnimationParameter != isJump;

      if (!changed)
        return;

      _characterModelAnimator.SetBool(WalkAnimationParameterName, isWalk);
      _characterModelAnimator.SetBool(JumpAnimationParameterName, isJump);

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
