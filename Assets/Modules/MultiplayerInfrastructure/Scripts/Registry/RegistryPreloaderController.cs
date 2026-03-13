using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// RegistryPreloader는 게임 시작 시점에 필요한 레지스트리 항목을 미리 로드할 수 있도록 합니다.
  /// 
  /// 레지스트리에 등록할 개별 항목들에 Registry 로직이 있음에도 별도로 작성한 것은,
  /// 이들 로직은 Unity Life Cycle 과정 내에서 호출되도록 되어있어, 하이어라키에 존재하지 않으면
  /// 레지스터되지 않기 때문입니다.
  /// 
  /// 실제로는 ScriptableObject를 로드하여 등록합니다:
  /// 특정한 씬에 의존하거나, 관리가 적절히 되지 않을 수 있을 우려를 제거하기 위함입니다.
  /// </summary>
  public class RegistryPreloaderController : MonoBehaviour
  {
    public RegistryPreloadScenarioGraphSO preloadScenarioGraphSO;
    public RegistryPreloadIconSpriteSO preloadIconSpriteSO;
    public RegistryPreloadNpcSO preloadNpcSO;
    public RegistryPreloadWaypointSO preloadWaypointSO;
    public RegistryPreloadEntitySO preloadEntitySO;
    public RegistryPreloadInteractableEntitySO preloadInteractableEntitySO;
    public RegistryPreloadUIControllerSO preloadUIControllerSO;

    private void Awake()
    {
      PreloadScenarioGraphs();
      PreloadIconSprites();
      PreloadNpcs();
      PreloadWaypoints();
      PreloadEntities();
      PreloadInteractableEntities();
      PreloadUIControllers();
    }

    private void PreloadScenarioGraphs()
    {
      if (preloadScenarioGraphSO == null || preloadScenarioGraphSO.scenarioGraphRegistryRequirements == null)
        return;

      foreach (var req in preloadScenarioGraphSO.scenarioGraphRegistryRequirements)
      {
        if (string.IsNullOrWhiteSpace(req.identifier) || req.scenarioGraphAsset == null)
          continue;

        Registry.Register(RegistryType.ScenarioGraph, req.identifier, req.scenarioGraphAsset);
      }
    }

    private void PreloadIconSprites()
    {
      if (preloadIconSpriteSO == null || preloadIconSpriteSO.iconSpriteRegistryRequirements == null)
        return;

      foreach (var req in preloadIconSpriteSO.iconSpriteRegistryRequirements)
      {
        if (string.IsNullOrWhiteSpace(req.identifier) || req.sprite == null)
          continue;

        Registry.RegisterIconSprite(req.identifier, req.sprite);
      }
    }

    private void PreloadNpcs()
    {
      if (preloadNpcSO == null || preloadNpcSO.npcRegistryRequirements == null)
        return;

      foreach (var req in preloadNpcSO.npcRegistryRequirements)
      {
        if (string.IsNullOrWhiteSpace(req.Identifier) || req.GameObjectRef == null)
          continue;

        Registry.Register(RegistryType.Npc, req.Identifier, req.GameObjectRef);
      }
    }

    private void PreloadWaypoints()
    {
      if (preloadWaypointSO == null || preloadWaypointSO.waypointRequirements == null)
        return;

      foreach (var req in preloadWaypointSO.waypointRequirements)
      {
        if (string.IsNullOrWhiteSpace(req.Identifier))
          continue;

        Registry.Register(RegistryType.Waypoint, req.Identifier, req.Position);
      }
    }

    private void PreloadEntities()
    {
      if (preloadEntitySO == null || preloadEntitySO.entityRegistryRequirements == null)
        return;

      foreach (var req in preloadEntitySO.entityRegistryRequirements)
      {
        if (string.IsNullOrWhiteSpace(req.identifier) || req.objectRef == null)
          continue;

        Registry.Register(RegistryType.Service, req.identifier, req.objectRef);
      }
    }

    private void PreloadInteractableEntities()
    {
      if (preloadInteractableEntitySO == null || preloadInteractableEntitySO.interactableEntityRequirements == null)
        return;

      foreach (var req in preloadInteractableEntitySO.interactableEntityRequirements)
      {
        if (string.IsNullOrWhiteSpace(req.Identifier))
          continue;

        Registry.Register(RegistryType.InteractableEntity, req.Identifier, req.Position);
      }
    }

    private void PreloadUIControllers()
    {
      if (preloadUIControllerSO == null || preloadUIControllerSO.uiControllerRegistryRequirements == null)
        return;

      foreach (var req in preloadUIControllerSO.uiControllerRegistryRequirements)
      {
        if (req.controllerRef == null)
          continue;

        string key = string.IsNullOrWhiteSpace(req.identifier)
          ? Registry.TypeKey(req.controllerRef.GetType())
          : req.identifier;

        Registry.Register(RegistryType.UI, key, req.controllerRef);
      }
    }
  }
}
