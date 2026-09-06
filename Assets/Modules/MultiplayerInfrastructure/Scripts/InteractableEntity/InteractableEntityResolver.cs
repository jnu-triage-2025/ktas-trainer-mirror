using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// 플레이어가 고른 <see cref="IInteract"/> 를 실행한다. 핸들러 목록은 프리팹이 아니라 인터렉션 레지스트리가 관리한다.
  /// </summary>
  [DisallowMultipleComponent]
  public class InteractableEntityResolver : MonoBehaviour
  {

    public void Resolve(IInteract interact, Transform interactor)
    {
      if (interact == null)
      {
        Debug.LogWarning($"InteractableEntityResolver.Resolve: interact is null", this);
        return;
      }

      interact.Interact(interactor);
    }

    public void Resolve(IInteractable interactable, Transform interactor, int interactIndex = 0)
    {
      if (interactable == null)
      {
        Debug.LogWarning($"InteractableEntityResolver.Resolve: interactable is null", this);
        return;
      }

      var interacts = interactable.Interacts;
      if (interacts == null || interacts.Length == 0)
      {
        Debug.LogWarning($"InteractableEntityResolver.Resolve: interactable has no interacts", this);
        return;
      }

      if (interactIndex < 0 || interactIndex >= interacts.Length)
      {
        Debug.LogWarning($"InteractableEntityResolver.Resolve: invalid interact index {interactIndex}", this);
        return;
      }

      Resolve(interacts[interactIndex], interactor);
    }
  }
}
