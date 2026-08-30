using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports
{
  public abstract class PlayerCharacterModelBase : MonoBehaviour, IPlayerCharacterModelObject
  {
    [SerializeField] private PlayerCharacterModelAnimatorControllerObject _animatorControllerObject;
    [SerializeField] private PlayerCharacterModelHoldingItemAttachPoint _heldItemAttachPointObject;

    public virtual Vector3 CharacterControllerCenter => new Vector3(0f, 1f, 0f);
    public Animator Animator => _animatorControllerObject != null
      ? _animatorControllerObject.GetComponent<Animator>()
      : null;
    public PlayerCharacterModelAnimatorControllerObject AnimatorControllerObject => _animatorControllerObject;
    public Transform HeldItemAttachPoint => _heldItemAttachPointObject != null
      ? _heldItemAttachPointObject.transform
      : null;
    public PlayerCharacterModelHoldingItemAttachPoint HeldItemAttachPointObject => _heldItemAttachPointObject;

    protected virtual void Reset()
    {
      _animatorControllerObject = GetComponentInChildren<PlayerCharacterModelAnimatorControllerObject>(true);
      _heldItemAttachPointObject = GetComponentInChildren<PlayerCharacterModelHoldingItemAttachPoint>(true);
    }

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
      Animator animator = Animator;
      if (animator == null)
        return;

      animator.applyRootMotion = false;
    }
  }
}
