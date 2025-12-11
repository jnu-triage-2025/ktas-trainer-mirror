using System.Collections.Generic;
using UnityEngine;

namespace Modules.TriageTrainer.Scripts.PlayerInteractiveGameObject
{
  /// <summary>
  /// </summary>
  [DisallowMultipleComponent]
  public class PlayerInteractionResolver : MonoBehaviour
  {
    [SerializeField] private List<MonoBehaviour> handlerSources = new List<MonoBehaviour>();
    private readonly List<IPlayerInteractive> handlers = new List<IPlayerInteractive>();

    void Awake()
    {
      handlers.Clear();
      foreach (var eachSource in handlerSources)
      {
        if (eachSource is IPlayerInteractive handler)
          handlers.Add(handler);
        else if (eachSource != null)
          Debug.LogWarning($"{eachSource.name} does not implement IPlayerInteractive interface", eachSource);
      }
    }

    public void Resolve(PlayerInteractiveModel model, Transform interactor)
    {
      if (model == null)
      {
        Debug.LogWarning($"TriageTrainer.Scripts.PlayerInteractiveResolver.Resolve: model is null", this);
        return;
      }

      foreach (var handler in handlers)
      {
        handler.Interact(model, interactor);
        return;
      }
    
      Debug.LogWarning($"No handler processed {model.DisplayText}.", model);
    }
  }
}