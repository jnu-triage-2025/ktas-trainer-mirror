# <a id="MultiplayerInfrastructure_Quest"></a> Namespace MultiplayerInfrastructure.Quest

### Classes

 [BasicMovementControlTutorialQuestResolver](MultiplayerInfrastructure.Quest.BasicMovementControlTutorialQuestResolver.md)

기본 이동 조작 튜토리얼 전용 퀘스트 처리기.
WASD와 마우스 기본 조작이 있었던 프레임의 시간을 누적한다. 총 3초 이상 조작하고
키보드와 마우스를 각각 한 번 이상 조작해야 이동 조작 퀘스트를 완료한다.
이 세부 조건은 퀘스트 표시 항목으로 노출하지 않는다.

 [PlayerQuestStateFlagService](MultiplayerInfrastructure.Quest.PlayerQuestStateFlagService.md)

플레이어별 퀘스트 상태 플래그 풀(Quest State Flag Pool)을 관리하는 정적 서비스.

<p>
풀은 플레이어 한 명당 문자열 집합 하나다. 퀘스트 진행이 "지금 이 플레이어에게 무엇이 요구되는가"를
표현해야 할 때 임의로 정한 식별자를 이 집합에 넣고, 그것을 소비하는 쪽(상호작용 노출 판정 등)이
보유 여부를 묻는다. 값은 PlayerQuestStateFlag 레지스트리에 UserDescriptor.Identifier(UUID)를
키로 저장한다.
</p>

<p>
역할 태그(<xref href="MultiplayerInfrastructure.Tag.PlayerTagService" data-throw-if-not-resolved="false"></xref>)와 저장 구조는 같지만 쓰임이 다르다. 역할 태그는 세션 내내
유지되는 배역이고, 이 풀은 퀘스트 단계마다 켜졌다 꺼지는 진행 상태다. 두 저장소를 나눠 두어야
병렬 브랜치 배정(requiredPlayerTags)이 퀘스트 상태 문자열에 영향을 받지 않는다.
</p>

<p>
변경은 서버 권위다. 서버가 값을 바꾸면 소유 플레이어의 <xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref>가 전체
옵저버에게 스냅샷을 복제하므로, 각 피어는 모든 플레이어의 플래그를 읽을 수 있다. 네트워크가
꺼진 오프라인 컨텍스트에서는 로컬 저장소만 갱신한다.
</p>

 [QuestCompletionCriteria](MultiplayerInfrastructure.Quest.QuestCompletionCriteria.md)

 [QuestCriteriaEvaluator](MultiplayerInfrastructure.Quest.QuestCriteriaEvaluator.md)

 [QuestData](MultiplayerInfrastructure.Quest.QuestData.md)

 [QuestDefinition](MultiplayerInfrastructure.Quest.QuestDefinition.md)

 [QuestDefinitionRegistry](MultiplayerInfrastructure.Quest.QuestDefinitionRegistry.md)

 [QuestDefinitionRegistryPayload](MultiplayerInfrastructure.Quest.QuestDefinitionRegistryPayload.md)

 [QuestManager](MultiplayerInfrastructure.Quest.QuestManager.md)

Manages quest lifecycle and tracked selections shared by UI controllers.

 [QuestPresentationBinding](MultiplayerInfrastructure.Quest.QuestPresentationBinding.md)

 [QuestPresentationService](MultiplayerInfrastructure.Quest.QuestPresentationService.md)

 [QuestProgressValue](MultiplayerInfrastructure.Quest.QuestProgressValue.md)

### Structs

 [QuestCriteriaEvaluationResult](MultiplayerInfrastructure.Quest.QuestCriteriaEvaluationResult.md)

### Enums

 [QuestManager.FeatureFlags](MultiplayerInfrastructure.Quest.QuestManager.FeatureFlags.md)

 [QuestCompletionCriteriaType](MultiplayerInfrastructure.Quest.QuestCompletionCriteriaType.md)

 [QuestPresentationActivation](MultiplayerInfrastructure.Quest.QuestPresentationActivation.md)

 [QuestPresentationIconMode](MultiplayerInfrastructure.Quest.QuestPresentationIconMode.md)

 [QuestPresentationTargetType](MultiplayerInfrastructure.Quest.QuestPresentationTargetType.md)

 [QuestScopeType](MultiplayerInfrastructure.Quest.QuestScopeType.md)

