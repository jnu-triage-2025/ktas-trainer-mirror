using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.InteractableEntity
{
  /// <summary>
  /// </summary>
  [DisallowMultipleComponent]
  public class PlayerInteractionResolver : MonoBehaviour
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
          Debug.LogWarning($"{eachSource.name} does not implement IPlayerInteractive interface", eachSource);
      }
    }

    public void Resolve(PlayerInteractableModel model, Transform interactor)
    {
      if (model == null)
      {
        Debug.LogWarning($"TriageTrainer.PlayerInteractiveResolver.Resolve: model is null", this);
        return;
      }

      foreach (var handler in handlers)
      {
        handler.Interact(interactor);
        return;
      }
    
      Debug.LogWarning($"No handler processed {model.DisplayText}. {model}");
    }
  }
}