using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// IInteractable 모든 상호작용 가능 객체의 컨트롤러에서 구현해야합니다. 이후에 InteractableEntityResolver에 의해 활용됩니다. 
  /// </summary>
  public interface IInteractable
  {
    IInteract[] Interacts { get; }
  }
}
