using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// </summary>
  [DisallowMultipleComponent]
  public class InteractableEntityResolver : MonoBehaviour
  {
    [SerializeField] private List<MonoBehaviour> handlerSources = new List<MonoBehaviour>();
    private readonly List<IInteractable> handlers = new List<IInteractable>();

    private void Awake()
    {
      handlers.Clear();
      foreach (var eachSource in handlerSources)
      {
        if (eachSource is IInteractable handler)
          handlers.Add(handler);
        else if (eachSource != null)
          Debug.LogWarning($"{eachSource.name} does not implement IInteractable interface", eachSource);
      }
    }

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
