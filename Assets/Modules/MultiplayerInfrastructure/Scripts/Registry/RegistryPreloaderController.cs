using UnityEngine;
using MultiplayerInfrastructure.Player;

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
    public RegistryPreloadPlayerCharacterSO preloadPlayerCharacterSO;
    public RegistryPreloadProblemSetSO preloadProblemSetSO;

    private void Awake()
    {
      PreloadScenarioGraphs();
      PreloadIconSprites();
      PreloadNpcs();
      PreloadWaypoints();
      PreloadEntities();
      PreloadInteractableEntities();
      PreloadUIControllers();
      PreloadPlayerCharacters();
      PreloadProblemSets();
    }

    private void PreloadScenarioGraphs()
    {
      // Always attempt bulk preload from Resources/Scenario so all available
      // scenario TextAssets can be discovered even without explicit SO wiring.
      // 시나리오 스키마는 Editor/CI 테스트에서 검증한다. 런타임 시작 시에는
      // 전체 Resources 항목을 다시 스키마 검증하지 않아 씬 로딩 정지를 피한다.
      Registry.PreloadScenarioGraphsFromResources(validateWithSchema: false);

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

    private void PreloadPlayerCharacters()
    {
      if (preloadPlayerCharacterSO == null || preloadPlayerCharacterSO.playerCharacterRegistryRequirements == null)
        return;

      foreach (var req in preloadPlayerCharacterSO.playerCharacterRegistryRequirements)
      {
        if (string.IsNullOrWhiteSpace(req.identifier) || req.prefab == null)
          continue;

        if (!req.prefab.TryGetComponent<IPlayerCharacterModelObject>(out _))
        {
          Debug.LogWarning(
            $"[RegistryPreloaderController] PlayerCharacter '{req.identifier}' does not implement IPlayerCharacterModelObject. Skipped.",
            req.prefab);
          continue;
        }

        Registry.Register(RegistryType.PlayerModel, req.identifier, req.prefab);
      }
    }

    private void PreloadProblemSets()
    {
      if (preloadProblemSetSO == null || preloadProblemSetSO.problemSetRegistryRequirements == null)
        return;

      foreach (var req in preloadProblemSetSO.problemSetRegistryRequirements)
      {
        if (string.IsNullOrWhiteSpace(req.identifier))
          continue;

        if (!Registry.PreloadProblemSet(req.identifier))
        {
          Debug.LogWarning(
            $"[RegistryPreloaderController] Failed to preload ProblemSet '{req.identifier}'. Ensure Resources/Problems path and manifest/json are valid.",
            preloadProblemSetSO);
        }
      }
    }
  }
}
