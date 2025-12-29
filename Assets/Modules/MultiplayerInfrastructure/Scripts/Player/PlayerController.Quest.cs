using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;
using Unity.VisualScripting;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [SerializeField] private QuestUIController _questUIController;
    [SerializeField] private QuestPreviewHudUIController _questPreviewHudUIController;
    
    void OnStartClient_Quest()
    {
      if (!IsOwner) return;

      // QuestUIController 찾기 (Inspector에서 할당되지 않은 경우)
      if (_questUIController.IsUnityNull())
        _questUIController = FindQuestUIController();

      // QuestPreviewHudUIController 찾기 (Inspector에서 할당되지 않은 경우)
      if (_questPreviewHudUIController.IsUnityNull())
        _questPreviewHudUIController = FindQuestPreviewHudUIController();
    }

    private QuestUIController FindQuestUIController()
    {
      var controller = UIControlRegistry.Get<QuestUIController>();
      if (!controller.IsUnityNull())
        return controller;

      return FindFirstObjectByType<QuestUIController>(FindObjectsInactive.Exclude);
    }

    private QuestPreviewHudUIController FindQuestPreviewHudUIController()
    {
      var controller = UIControlRegistry.Get<QuestPreviewHudUIController>();
      if (!controller.IsUnityNull())
        return controller;

      return FindFirstObjectByType<QuestPreviewHudUIController>(FindObjectsInactive.Exclude);
    }
  }
}
