using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports
{
  public abstract class PlayerCharacterModelBase : MonoBehaviour, IPlayerCharacterModelObject
  {
    [SerializeField] private Animator _animator;

    public virtual Vector3 CharacterControllerCenter => new Vector3(0f, 1f, 0f);
    public Animator Animator => _animator;

    protected virtual void Awake()
    {
      DisableRootMotionIfAvailable();
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
      DisableRootMotionIfAvailable();
    }
#endif

    protected void DisableRootMotionIfAvailable()
    {
      if (_animator == null)
        return;

      _animator.applyRootMotion = false;
    }
  }
}
