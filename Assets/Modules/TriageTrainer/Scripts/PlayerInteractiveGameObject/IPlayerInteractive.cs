using UnityEngine;

namespace Modules.TriageTrainer.Scripts.PlayerInteractiveGameObject
{
  public interface IPlayerInteractive
  {
    void Interact(PlayerInteractiveModel model, Transform interactor);
  }
}
