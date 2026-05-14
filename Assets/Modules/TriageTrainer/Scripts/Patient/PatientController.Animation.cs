using UnityEngine;

namespace TriageTrainer.Entity
{
  public partial class PatientController
  {
    private HumanoidAnimationController _animationControllerCache;

    public HumanoidAnimationController AnimationController => ResolveAnimationController();

    private HumanoidAnimationController ResolveAnimationController()
    {
      if (_animationControllerCache != null)
        return _animationControllerCache;

      _animationControllerCache = GetComponent<HumanoidAnimationController>();
      if (_animationControllerCache == null)
        _animationControllerCache = GetComponentInChildren<HumanoidAnimationController>(true);

      return _animationControllerCache;
    }

    public bool TrySetAnimationController(RuntimeAnimatorController controller)
    {
      if (controller == null)
        return false;

      var animationController = ResolveAnimationController();
      if (animationController == null)
        return false;

      animationController.SetController(controller);
      return true;
    }

    public bool TryPlayAnimation(string stateName, float transitionSeconds = 0f, int layer = 0, float normalizedTime = 0f)
    {
      var animationController = ResolveAnimationController();
      if (animationController == null)
        return false;

      if (transitionSeconds <= 0f)
        animationController.Play(stateName, layer, normalizedTime);
      else
        animationController.CrossFade(stateName, transitionSeconds, layer, normalizedTime);

      return true;
    }

    public bool TrySetAnimationBool(string parameter, bool value)
    {
      var animationController = ResolveAnimationController();
      if (animationController == null)
        return false;

      animationController.SetBool(parameter, value);
      return true;
    }

    public bool TrySetAnimationFloat(string parameter, float value)
    {
      var animationController = ResolveAnimationController();
      if (animationController == null)
        return false;

      animationController.SetFloat(parameter, value);
      return true;
    }

    public bool TrySetAnimationInt(string parameter, int value)
    {
      var animationController = ResolveAnimationController();
      if (animationController == null)
        return false;

      animationController.SetInt(parameter, value);
      return true;
    }

    public bool TrySetAnimationTrigger(string parameter)
    {
      var animationController = ResolveAnimationController();
      if (animationController == null)
        return false;

      animationController.SetTrigger(parameter);
      return true;
    }

    public bool TryResetAnimationTrigger(string parameter)
    {
      var animationController = ResolveAnimationController();
      if (animationController == null)
        return false;

      animationController.ResetTrigger(parameter);
      return true;
    }
  }
}
