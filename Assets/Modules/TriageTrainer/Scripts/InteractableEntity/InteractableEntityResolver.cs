using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.InteractableEntity
{
  /// <summary>
  /// </summary>
  [DisallowMultipleComponent]
  public class InteractableEntityResolver : MonoBehaviour
  {
    [SerializeField] private List<MonoBehaviour> handlerSources = new List<MonoBehaviour>();
    private readonly List<IInteractable> handlers = new List<IInteractable>();

    void Awake()
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

    public void Resolve(IInteractable interactable, Transform interactor)
    {
      if (interactable == null)
      {
        Debug.LogWarning($"InteractableEntityResolver.Resolve: interactable is null", this);
        return;
      }

      interactable.Interact(interactor);
    }
  }
}