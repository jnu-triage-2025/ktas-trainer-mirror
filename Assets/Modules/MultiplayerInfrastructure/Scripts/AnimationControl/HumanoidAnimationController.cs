using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.Entity
{
  public sealed class HumanoidAnimationController : MonoBehaviour
  {
    [Header("Animator")]
    [SerializeField] private Animator _animator;

    private readonly Dictionary<string, int> _parameterHashes = new(StringComparer.Ordinal);

    public Animator Animator => ResolveAnimator();
    public bool HasAnimator => ResolveAnimator() != null;

    private void Reset()
    {
      ResolveAnimator();
    }

    private void OnValidate()
    {
      ResolveAnimator();
    }

    private Animator ResolveAnimator()
    {
      if (_animator != null)
        return _animator;

      _animator = GetComponent<Animator>();
      if (_animator == null)
        _animator = GetComponentInChildren<Animator>(true);

      return _animator;
    }

    private int GetHash(string parameterOrState)
    {
      if (string.IsNullOrWhiteSpace(parameterOrState))
        return 0;

      if (_parameterHashes.TryGetValue(parameterOrState, out int hash))
        return hash;

      hash = Animator.StringToHash(parameterOrState);
      _parameterHashes[parameterOrState] = hash;
      return hash;
    }

    public void SetController(RuntimeAnimatorController controller)
    {
      if (controller == null)
        return;

      var animator = ResolveAnimator();
      if (animator == null)
        return;

      animator.runtimeAnimatorController = controller;
    }

    public void SetBool(string parameter, bool value)
    {
      var animator = ResolveAnimator();
      if (animator == null || string.IsNullOrWhiteSpace(parameter))
        return;

      animator.SetBool(GetHash(parameter), value);
    }

    public void SetFloat(string parameter, float value)
    {
      var animator = ResolveAnimator();
      if (animator == null || string.IsNullOrWhiteSpace(parameter))
        return;

      animator.SetFloat(GetHash(parameter), value);
    }

    public void SetInt(string parameter, int value)
    {
      var animator = ResolveAnimator();
      if (animator == null || string.IsNullOrWhiteSpace(parameter))
        return;

      animator.SetInteger(GetHash(parameter), value);
    }

    public void SetTrigger(string parameter)
    {
      var animator = ResolveAnimator();
      if (animator == null || string.IsNullOrWhiteSpace(parameter))
        return;

      animator.SetTrigger(GetHash(parameter));
    }

    public void ResetTrigger(string parameter)
    {
      var animator = ResolveAnimator();
      if (animator == null || string.IsNullOrWhiteSpace(parameter))
        return;

      animator.ResetTrigger(GetHash(parameter));
    }

    public void Play(string stateName, int layer = 0, float normalizedTime = 0f)
    {
      var animator = ResolveAnimator();
      if (animator == null || string.IsNullOrWhiteSpace(stateName))
        return;

      animator.Play(GetHash(stateName), layer, normalizedTime);
    }

    public void CrossFade(string stateName, float transitionDuration, int layer = 0, float normalizedTime = 0f)
    {
      var animator = ResolveAnimator();
      if (animator == null || string.IsNullOrWhiteSpace(stateName))
        return;

      animator.CrossFadeInFixedTime(GetHash(stateName), transitionDuration, layer, normalizedTime);
    }
  }
}
