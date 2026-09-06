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
    [SerializeField] private PatientAnimatorRootObject _animatorObject;

    private HumanoidAnimationController _animationControllerCache;
    private Animator _animatorCache;
    private bool _hasAnimationParameterState;
    private bool _currentLyingAnimationParameter;
    private bool _hasValidatedAnimationParameters;
    private bool _hasRequiredAnimationParameters;
    private bool _hasForceLyingState;
#if !UNITY_EDITOR
    private bool _hasWarnedMissingRuntimeAnimatorController;
#endif

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

      if (_animatorObject == null)
      {
        PatientAnimatorRootObject[] animatorObjects = GetComponentsInChildren<PatientAnimatorRootObject>(true);
        if (animatorObjects.Length == 1)
          _animatorObject = animatorObjects[0];
      }

      if (_animatorObject != null)
        _animatorCache = _animatorObject.GetComponentInChildren<Animator>(true);

      var animationController = ResolveAnimationController();
      if (_animatorCache == null && animationController != null)
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

      // 한 번 lying-idle 에 들어가면 애니메이터를 lying-idle 로 유지한다.
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

    /// <summary>
    /// 직렬화된 애니메이터 컨트롤러 참조가 비어 있으면 에디터에서 기본 애셋으로 채운다.
    ///
    /// <para>
    /// <b>이 폴백은 에디터 전용이다.</b> AssetDatabase 는 빌드에 존재하지 않으므로, 프리팹에
    /// 참조가 직렬화되지 않은 채 빌드되면 런타임에 폴백이 동작하지 않는다. 환자가 누운 자세로
    /// 전환되지 않으면 훈련 상황 자체가 성립하지 않으므로, 빌드에서는 한 번만 오류를 남긴다.
    /// </para>
    /// </summary>
    private void EnsureDefaultRuntimeAnimatorController()
    {
      if (_runtimeAnimatorController != null)
        return;

#if UNITY_EDITOR
      _runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DefaultRuntimeAnimatorControllerAssetPath);
      if (_runtimeAnimatorController == null)
      {
        Debug.LogError(
          $"[PatientController] Default animator controller was not found at '{DefaultRuntimeAnimatorControllerAssetPath}'. "
          + "Assign it on the prefab; builds have no AssetDatabase fallback.", this);
      }
#else
      if (!_hasWarnedMissingRuntimeAnimatorController)
      {
        _hasWarnedMissingRuntimeAnimatorController = true;
        Debug.LogError(
          "[PatientController] Runtime animator controller is not assigned on this prefab, so the patient "
          + "will not switch to its lying pose. The editor-only AssetDatabase fallback does not exist in builds; "
          + "assign the controller on the prefab.", this);
      }
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
      if (FishNet.InstanceFinder.IsServerStarted)
        PruneExpiredPendingItemUses();
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
