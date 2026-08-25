using UnityEngine;

namespace TriageTrainer.Entity
{
  public partial class PatientController
  {
    private const string DefaultRuntimeAnimatorControllerAssetPath = "Assets/Modules/Mixamo/AnimationControllers/PatientCharacterModel.controller";
    private const string LyingAnimationParameterName = "lying";
    private const string LyingIdleStateName = "laying-idle";

    [Header("Animation")]
    [SerializeField] private RuntimeAnimatorController _runtimeAnimatorController;

    private HumanoidAnimationController _animationControllerCache;
    private Animator _animatorCache;
    private bool _hasAnimationParameterState;
    private bool _currentLyingAnimationParameter;
    private bool _hasValidatedAnimationParameters;
    private bool _hasRequiredAnimationParameters;
    private bool _hasForceLyingState;

    public HumanoidAnimationController AnimationController => ResolveAnimationController();

    private void Awake_Animation()
    {
      EnsureDefaultRuntimeAnimatorController();
      _hasAnimationParameterState = false;
      _hasForceLyingState = false;
      EnsureRootMotionDisabled();
      ApplyRuntimeAnimatorController();
      ApplyAnimationParameters(isLying: false);
    }

    private void OnValidate_Animation()
    {
      EnsureDefaultRuntimeAnimatorController();
      EnsureRootMotionDisabled();
      ApplyRuntimeAnimatorController();
    }

    private void Update_Animation()
    {
      bool isLying = IsLyingAnimationState();
      ApplyColliderForAnimationState(isLying);

      var animator = ResolveAnimator();
      if (!CanPlayAnimator(animator))
        return;

      if (animator.applyRootMotion)
        animator.applyRootMotion = false;

      if (!HasRequiredAnimationParameters(animator))
        return;

      ApplyAnimationParameters(isLying);

      if (_hasForceLyingState && _currentLyingAnimationParameter)
        ForceLyingIdleState(animator);
    }

    private HumanoidAnimationController ResolveAnimationController()
    {
      if (_animationControllerCache != null)
        return _animationControllerCache;

      _animationControllerCache = GetComponent<HumanoidAnimationController>();
      if (_animationControllerCache == null)
        _animationControllerCache = GetComponentInChildren<HumanoidAnimationController>(true);

      return _animationControllerCache;
    }

    private Animator ResolveAnimator()
    {
      if (_animatorCache != null)
        return _animatorCache;

      var animationController = ResolveAnimationController();
      if (animationController != null)
        _animatorCache = animationController.Animator;

      if (_animatorCache == null)
        _animatorCache = GetComponent<Animator>();

      if (_animatorCache == null)
        _animatorCache = GetComponentInChildren<Animator>(true);

      return _animatorCache;
    }

    private bool IsLyingAnimationState()
    {
      return CurrentBed != null || _isMovingPatientBedAttached;
    }

    private void ApplyRuntimeAnimatorController()
    {
      if (_runtimeAnimatorController == null)
        return;

      var animationController = ResolveAnimationController();
      var animator = ResolveAnimator();
      if (animationController == null && animator == null)
        return;

      if (animationController != null)
        animationController.SetController(_runtimeAnimatorController);
      else
        animator.runtimeAnimatorController = _runtimeAnimatorController;

      _hasAnimationParameterState = false;
      _hasValidatedAnimationParameters = false;
      _hasRequiredAnimationParameters = false;
      _hasForceLyingState = false;
    }

    private void ApplyAnimationParameters(bool isLying)
    {
      var animationController = ResolveAnimationController();
      var animator = ResolveAnimator();
      if (animationController == null && animator == null)
        return;

      // Once lying-idle is entered, keep the animator in lying-idle.
      bool effectiveIsLying = isLying || _currentLyingAnimationParameter;

      bool changed = !_hasAnimationParameterState
        || _currentLyingAnimationParameter != effectiveIsLying;

      if (!changed)
        return;

      if (animationController != null)
        animationController.SetBool(LyingAnimationParameterName, effectiveIsLying);
      else
        animator.SetBool(Animator.StringToHash(LyingAnimationParameterName), effectiveIsLying);

      _currentLyingAnimationParameter = effectiveIsLying;
      _hasAnimationParameterState = true;

      if (effectiveIsLying)
        _hasForceLyingState = true;
    }

    private void EnsureDefaultRuntimeAnimatorController()
    {
      if (_runtimeAnimatorController != null)
        return;

#if UNITY_EDITOR
      _runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DefaultRuntimeAnimatorControllerAssetPath);
#endif
    }

    private bool HasRequiredAnimationParameters(Animator animator)
    {
      if (_hasValidatedAnimationParameters)
        return _hasRequiredAnimationParameters;

      _hasValidatedAnimationParameters = true;
      _hasRequiredAnimationParameters = HasBoolParameter(animator, LyingAnimationParameterName);
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

    private void EnsureRootMotionDisabled()
    {
      var animator = ResolveAnimator();
      if (animator == null)
        return;

      if (animator.applyRootMotion)
        animator.applyRootMotion = false;
    }

    private void Update()
    {
      Update_Animation();
    }

    public bool TrySetAnimationController(RuntimeAnimatorController controller)
    {
      if (controller == null)
        return false;

      var animationController = ResolveAnimationController();
      var animator = ResolveAnimator();
      if (animationController == null && animator == null)
        return false;

      _runtimeAnimatorController = controller;
      if (animationController != null)
        animationController.SetController(controller);
      else
        animator.runtimeAnimatorController = controller;

      _hasAnimationParameterState = false;
      _hasValidatedAnimationParameters = false;
      _hasRequiredAnimationParameters = false;
      _hasForceLyingState = false;
      return true;
    }

    private static void ForceLyingIdleState(Animator animator)
    {
      if (animator == null)
        return;

      int layer = 0;
      int stateHash = Animator.StringToHash(LyingIdleStateName);
      var current = animator.GetCurrentAnimatorStateInfo(layer);
      var next = animator.GetNextAnimatorStateInfo(layer);

      bool alreadyInState = current.shortNameHash == stateHash || current.fullPathHash == stateHash;
      bool movingToState = animator.IsInTransition(layer)
        && (next.shortNameHash == stateHash || next.fullPathHash == stateHash);

      if (alreadyInState || movingToState)
        return;

      animator.Play(stateHash, layer, 0f);
    }

    public bool TryPlayAnimation(string stateName, float transitionSeconds = 0f, int layer = 0, float normalizedTime = 0f)
    {
      var animationController = ResolveAnimationController();
      var animator = ResolveAnimator();
      if (animationController == null && animator == null)
        return false;

      if (transitionSeconds <= 0f)
      {
        if (animationController != null)
          animationController.Play(stateName, layer, normalizedTime);
        else
          animator.Play(Animator.StringToHash(stateName), layer, normalizedTime);
      }
      else
      {
        if (animationController != null)
          animationController.CrossFade(stateName, transitionSeconds, layer, normalizedTime);
        else
          animator.CrossFadeInFixedTime(Animator.StringToHash(stateName), transitionSeconds, layer, normalizedTime);
      }

      return true;
    }

    public bool TrySetAnimationBool(string parameter, bool value)
    {
      var animationController = ResolveAnimationController();
      var animator = ResolveAnimator();
      if (animationController == null && animator == null)
        return false;

      if (animationController != null)
        animationController.SetBool(parameter, value);
      else
        animator.SetBool(Animator.StringToHash(parameter), value);

      return true;
    }

    public bool TrySetAnimationFloat(string parameter, float value)
    {
      var animationController = ResolveAnimationController();
      var animator = ResolveAnimator();
      if (animationController == null && animator == null)
        return false;

      if (animationController != null)
        animationController.SetFloat(parameter, value);
      else
        animator.SetFloat(Animator.StringToHash(parameter), value);

      return true;
    }

    public bool TrySetAnimationInt(string parameter, int value)
    {
      var animationController = ResolveAnimationController();
      var animator = ResolveAnimator();
      if (animationController == null && animator == null)
        return false;

      if (animationController != null)
        animationController.SetInt(parameter, value);
      else
        animator.SetInteger(Animator.StringToHash(parameter), value);

      return true;
    }

    public bool TrySetAnimationTrigger(string parameter)
    {
      var animationController = ResolveAnimationController();
      var animator = ResolveAnimator();
      if (animationController == null && animator == null)
        return false;

      if (animationController != null)
        animationController.SetTrigger(parameter);
      else
        animator.SetTrigger(Animator.StringToHash(parameter));

      return true;
    }

    public bool TryResetAnimationTrigger(string parameter)
    {
      var animationController = ResolveAnimationController();
      var animator = ResolveAnimator();
      if (animationController == null && animator == null)
        return false;

      if (animationController != null)
        animationController.ResetTrigger(parameter);
      else
        animator.ResetTrigger(Animator.StringToHash(parameter));

      return true;
    }
  }
}
