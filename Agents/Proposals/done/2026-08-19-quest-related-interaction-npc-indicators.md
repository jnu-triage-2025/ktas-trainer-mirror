# 퀘스트 관련 Interaction 및 NPC 표시

> 상태: **작성 대책 제안(proposed, 2026-08-19)**. 구현 전 데이터 형식과 다중 퀘스트 우선순위를 확정해야 한다.

### 개요

진행 중인 퀘스트가 특정 Interaction 또는 NPC와 관계있다면, 상호작용 힌트의 기본 아이콘을 퀘스트용 아이콘으로 임시 대체하고 NPC 머리 위에도 퀘스트 아이콘을 표시한다. 퀘스트가 완료·삭제되면 별도 복구 명령 없이 기존 표시로 돌아간다. 시나리오가 끝날 때는 해당 시나리오가 소유한 표시를 정리하되, 세션 유지 퀘스트의 표시는 퀘스트 유지 정책에 따라 다시 계산한다.

### 현재 구조에서 확인한 제약

- 상호작용 힌트는 `IInteract.DisplayIcon`과 `IInteractDisplayIcons.DisplayIcons`를 UI를 다시 그릴 때 직접 읽는다. 표시 변경 이벤트나 임시 오버라이드 계층은 없다.
- 하나의 `IInteractable`이 여러 `IInteract`를 노출할 수 있으나, `IInteract`에는 안정적인 식별자가 없다. NPC가 만드는 런타임 상호작용 객체는 목록 재구축 때 인스턴스가 달라질 수도 있다.
- `QuestManager`는 퀘스트 추가·갱신·완료·세션 종료를 알고 있으며 플레이어 범위 퀘스트는 소유 클라이언트에만 반영된다. 따라서 표시 수명주기를 판단하기에 가장 적합하다.
- `EntityOverheadLabelUIController`는 월드 앵커 하나당 엘리먼트 하나만 보관한다. NPC 이름표와 퀘스트 아이콘을 같은 앵커에 등록하면 서로 덮어쓴다.
- `IconSprite` 레지스트리와 `IconSpriteReference`가 이미 있으므로 시나리오 JSON에는 `Sprite`가 아닌 아이콘 식별자를 저장할 수 있다.

### 제안하는 데이터 형식

`QuestDefinition`과 `QuestData`에 선택 속성인 `presentationBindings`를 추가한다. 퀘스트 정의에 넣으면 `QuestControl Add` 시 런타임 `QuestData`로 복제되며 인라인 퀘스트도 같은 형식을 쓴다. 각 바인딩은 표시 수명, 대상 주소, 아이콘 적용 방식을 함께 선언한다.

```json
{
  "identifier": "Quest_ReportToDoctor",
  "title": "의사에게 보고",
  "presentationBindings": [
    {
      "activation": "WholeQuest",
      "targetType": "Interaction",
      "entityIdentifier": "npc-er-doctor",
      "interactionIdentifier": "report",
      "iconIdentifier": "quest-interaction",
      "iconMode": "ReplacePrimaryIcon"
    },
    {
      "activation": "CompletionCriteria",
      "completionCriteriaIdentifier": "report-to-doctor",
      "targetType": "Npc",
      "entityIdentifier": "npc-er-doctor",
      "iconIdentifier": "quest-marker"
    }
  ]
}
```

각 항목은 다음 필드를 갖는다.

- `activation`: `WholeQuest` 또는 `CompletionCriteria`. 후자는 지정한 완료 조건이 현재 진행 대상일 때만 표시한다.
- `completionCriteriaIdentifier`: `activation`이 `CompletionCriteria`일 때 사용할 완료 조건 식별자.
- `targetType`: `Interaction` 또는 `Npc`.
- `entityIdentifier`: 대상 엔티티 식별자. NPC는 `RegistryType.Npc`, 그 밖의 엔티티는 Entity registry에서 찾는다.
- `interactionIdentifier`: `targetType`이 `Interaction`일 때 엔티티 내부의 메뉴 행을 구분하는 식별자.
- `iconIdentifier`: `RegistryType.IconSprite`에서 찾을 아이콘 식별자.
- `iconMode`: 초기 구현은 `ReplacePrimaryIcon`만 허용한다. 기존 상태 오버레이와 보조 아이콘은 유지한다.
- `priority`: 선택 정수. 한 대상에 여러 퀘스트가 겹칠 때 사용할 우선순위이며 기본값은 `0`이다.
- `showWhenUntracked`: 선택 불리언. 기본값은 `true`로 두어 추적 여부와 퀘스트 진행 여부를 혼동하지 않게 한다.

표시 여부는 `QuestData.Completed == false`이면서 퀘스트가 `QuestManager`에 존재하고 바인딩의 `activation` 조건을 만족하는 동안으로 정의한다. `IsTracked`는 HUD 선택 상태이므로 기본 수명주기 조건으로 쓰지 않는다. `CompletionCriteria` 활성화는 `QuestCompletionCriteria`에 추가하는 안정적인 `Identifier`와 런타임 완료 상태를 사용한다. 순차 퀘스트에서는 완료되지 않은 첫 조건만 현재 진행 대상으로 본다.

### 런타임 구성

#### 1. Interaction의 안정적인 표시 식별자

`IQuestPresentationTarget` 같은 작은 선택 인터페이스를 추가해 `InteractionIdentifier`와 소유 엔티티 식별자를 제공한다. 퀘스트 대상이 될 상호작용만 이를 구현하면 된다.

- `Interactable`에는 직렬화 가능한 `_interactionIdentifier`를 추가한다.
- `ScenarioNpcInteract`, `ScenarioActingNpcSignalInteract`, `ItemSubmissionInteractable` 등 런타임 상호작용은 기존 정의의 identifier를 반환한다.
- 한 엔티티가 여러 상호작용을 제공해도 정확한 메뉴 행 하나만 지정할 수 있다.
- 유일성 범위는 전역이 아니라 `(entityIdentifier, interactionIdentifier)` 쌍이다. 식별자가 비어 있거나 같은 엔티티 안에서 중복되면 Graph 진단과 개발 빌드 경고로 잡는다. 객체 참조나 `DisplayText`는 식별 수단으로 쓰지 않는다.

#### 2. 원본을 건드리지 않는 아이콘 오버라이드

로컬 서비스 `QuestPresentationService`를 두고 현재 활성 퀘스트의 대상별 오버라이드를 계산한다. `InteractableObjectHintListElement.Bind`는 원본 아이콘을 읽기 전에 이 서비스에 표시 아이콘을 질의한다.

```text
QuestManager 상태 변경
  -> QuestPresentationService.Reconcile(active quests)
  -> 대상별 icon override 갱신
  -> Interactable hint UI refresh
  -> Bind 시 기본 아이콘만 override하고 기존 상태/보조 아이콘 유지
```

원본 `Sprite` 필드나 `IInteract` 객체를 변경하지 않는다. 퀘스트가 사라지면 서비스의 항목만 제거되므로 다음 UI 갱신에서 기존 아이콘이 자동으로 다시 보인다. `ReplacePrimaryIcon`은 `DisplayIcon` 또는 `DisplayIcons`의 기본 아이콘 슬롯만 교체하고 Clear/Fail, 보유 아이템 등 상태 오버레이와 보조 아이콘은 그대로 둔다. 어떤 슬롯이 기본 아이콘인지 구현체마다 추측하지 않도록 UI에 전달하는 표시 모델을 `PrimaryIcon`과 `OverlayIcons`로 정규화한다.

같은 대상에 여러 퀘스트가 걸리면 `priority`, 등록 순서, quest id 순으로 결정하는 안정적인 규칙을 둔다. 초기 구현은 가장 높은 하나만 표시하고 여러 퀘스트 아이콘 합성은 후속 범위로 남긴다.

서비스 변경 이벤트는 `InteractableGameObjectHintUIController`가 구독한다. 감지 범위 안에 이미 들어온 상호작용도 즉시 다시 그려야 하므로 기존 `OnNewInteractableAdded/Removed`에만 의존하지 않는다.

#### 3. NPC 머리 위 아이콘

`EntityOverheadLabelUIController`를 채널형 등록으로 확장한다. 키를 `Transform` 하나가 아니라 `(Transform, channelId)`로 바꾸고 표시 내용에 선택 `Sprite Icon`을 추가한다. NPC는 `IOverheadPresentationAnchorProvider`를 구현해 머리 위 표시 전용 앵커를 제공한다. 서비스는 NPC의 루트 `Transform`을 표시 위치로 추측하지 않는다.

- 기존 `SetLabel(Transform, LabelContent)`는 `default` 채널을 쓰는 호환 API로 유지한다.
- NPC 이름표는 `npc-name`, 퀘스트 표시는 `quest:{questId}` 채널을 사용한다.
- 같은 앵커의 채널은 `channelOrder`, 고정 간격, 최대 표시 개수에 따라 하나의 세로 스택으로 배치한다. 이름표와 퀘스트 아이콘이 같은 화면 좌표에 겹치지 않아야 한다.
- `QuestPresentationService`가 NPC 레지스트리 식별자를 로컬 `GameObject`로 해석하고 `IOverheadPresentationAnchorProvider`에서 앵커를 얻어 채널을 등록한다.
- Registry에 항목 등록·해제 이벤트를 추가한다. NPC가 늦게 스폰되면 등록 이벤트에서 표시를 붙이고, 해제되거나 파괴되면 채널을 제거한다. 주기적인 폴링은 사용하지 않는다.

월드 스페이스 Canvas를 NPC 프리팹마다 추가하는 방식은 피한다. 현재 오버헤드 UI의 카메라 투영, 정렬 순서, 포인터 비차단 처리를 그대로 활용하는 편이 프리팹 변경 범위와 클라이언트별 상태 분리를 줄인다.

#### 4. 퀘스트 수명주기와 네트워크 범위

`QuestPresentationService`는 `QuestManager.OnQuestListChanged`, Registry 항목 등록·해제, `ScenarioController.OnScenarioEnded`를 구독해 전체 상태를 재조정한다. 서비스가 활성화될 때 현재 퀘스트와 이미 등록된 대상을 즉시 한 번 조회해 초기 이벤트를 놓치지 않게 한다. 명령형 `Show`/`Hide` 노드를 짝으로 배치하지 않는다.

- `Add`/`Update`: 미완료 퀘스트의 표시 대상을 생성 또는 갱신한다.
- 완료: 해당 퀘스트가 만든 Interaction 오버라이드와 NPC 채널을 제거한다.
- 삭제·전체 초기화·세션 종료: 남은 표시를 모두 제거한다.
- 시나리오 실패·교체: 종료된 시나리오의 `sourceScenarioIdentifier`로 등록된 표시 상태를 먼저 해제한 다음, `QuestManager`에 남은 퀘스트로 다시 계산한다. 세션 유지 퀘스트가 남아 있다면 유효한 표시가 다시 등록된다.
- 플레이어 범위 퀘스트: 해당 퀘스트가 실제로 존재하는 로컬 클라이언트에만 보인다.
- 전역 퀘스트: 퀘스트가 반영된 모든 클라이언트에 동일하게 보인다.

각 런타임 바인딩에는 `sourceScenarioIdentifier`를 기록한다. 이 값은 작성자가 JSON에 직접 넣지 않고 `QuestControl`을 실행한 `ScenarioController`가 주입한다. 표시 자체는 로컬 표현 상태이므로 별도 SyncVar나 RPC를 만들지 않는다. 서버가 이미 수행하는 퀘스트 노드의 대상별 프레젠테이션 경로를 그대로 따른다.

### 시나리오 그래프 및 에디터 변경

- `QuestDefinition`, `QuestData`, clone/merge 경로에 `PresentationBindings`를 추가한다.
- `QuestCompletionCriteria`에 작성용 `Identifier`를 추가하고 같은 퀘스트 안에서 중복되지 않게 검증한다.
- 퀘스트 데이터팩 JSON schema와 시나리오 schema의 인라인 Quest 정의를 함께 갱신한다.
- Scenario Graph Editor의 Quest Definitions 및 `QuestControl` 인라인 편집기에 대상 목록 UI를 추가한다.
- Interaction 대상은 식별자, NPC 대상은 acting NPC 및 Registry 식별자 후보를 제시하되 자유 입력도 허용한다.
- 진단기는 빈 식별자, 빈 아이콘, 엔티티 내부 Interaction 식별자 중복, 존재하지 않는 완료 조건, 동일 우선순위 충돌, 그래프 내부 acting NPC를 찾을 수 없는 경우를 보고한다. 외부 씬 대상은 정적 검증할 수 없으므로 경고로만 남긴다.
- 검색기에 `presentationBindings[].entityIdentifier`, `interactionIdentifier`, `completionCriteriaIdentifier`, `iconIdentifier`를 포함한다.

새 ScenarioNodeType은 추가하지 않는다. 표시 수명이 퀘스트 생명주기와 같으므로 별도 제어 노드는 종료 경로 누락을 만들기 쉽고 병렬 분기마다 `Hide`를 배치해야 한다. 퀘스트와 무관한 임시 마커가 필요해지는 경우에만 범용 `PresentationControl` 노드를 별도 제안한다.

### 구현 순서

1. 데이터 모델, 완료 조건 식별자, DTO/schema, clone/round-trip부터 추가한다.
2. Interaction 복합 주소와 엔티티 내부 중복 검증을 추가한다.
3. Registry 등록·해제 이벤트와 NPC 오버헤드 앵커 API를 추가한다.
4. `QuestPresentationService`의 수명주기, 단계별 활성화, 아이콘 우선순위 계산을 구현한다.
5. 상호작용 표시 모델을 기본 아이콘과 상태/보조 아이콘으로 정규화하고 UI가 서비스의 해석 결과를 사용하게 한다.
6. 오버헤드 라벨을 다중 채널, 세로 스택 및 Sprite 표시로 확장한다.
7. Quest Editor, 검색, 진단 UI를 보완한다.
8. 대표 시나리오 하나에 적용해 멀티클라이언트 PlayMode로 검증한다.

### 실패 처리 원칙

- 아이콘 식별자를 찾지 못하면 fallback 아이콘으로 조용히 대체하지 않고 개발 빌드에서 경고한 뒤 해당 퀘스트 표시만 생략한다. 잘못된 퀘스트 표시를 정상 아이콘처럼 보이게 하지 않는다.
- Interaction 또는 NPC를 아직 찾지 못하면 퀘스트는 정상 진행시키고 표시만 미해결 상태로 둔다. Registry 등록 이벤트가 같은 복합 주소를 알리면 즉시 다시 해석한다.
- 대상이 끝내 나타나지 않더라도 시나리오 실행을 중단하지 않는다. 그래프 진단과 런타임 경고에서 원인을 확인할 수 있게 한다.
- 서비스가 비활성화되거나 파괴될 때 자신이 등록한 UI 채널과 오버라이드를 모두 해제한다.

### 테스트 및 완료 기준

- Quest 정의 및 인라인 Quest의 `presentationBindings`와 완료 조건 식별자가 저장-로드-저장 뒤 보존된다.
- 퀘스트 추가 직후 지정 Interaction 행의 기본 아이콘만 바뀌고 기존 Clear/Fail 및 보조 아이콘은 유지된다.
- 퀘스트 완료·삭제 뒤 원래 단일/다중 아이콘 구성이 복원된다. 시나리오 종료 때는 해당 시나리오의 표시가 정리되고 남은 퀘스트를 기준으로 재등록된다.
- 순차 퀘스트에서는 현재 완료 조건과 관계있는 대상만 표시되고 다음 조건으로 진행할 때 대상이 교체된다.
- 표시가 활성화된 상태에서 플레이어가 감지 범위에 들어오거나 대상이 늦게 스폰되어도 올바른 아이콘이 보인다.
- 같은 Interaction에 두 퀘스트가 겹치면 우선순위 규칙대로 표시되고 상위 퀘스트가 끝나면 다음 표시로 내려간다.
- NPC 이름표와 퀘스트 아이콘이 동시에 표시되며 서로 덮어쓰지 않는다.
- NPC 표시 앵커가 실제 렌더러 머리 높이를 따르며 루트 원점에 표시되지 않는다.
- NPC가 비활성화·파괴되거나 퀘스트가 끝나면 오버헤드 엘리먼트가 남지 않는다.
- 소유자가 다른 두 클라이언트에서 플레이어 범위 퀘스트 표시가 서로 새지 않는다.
- 서비스가 퀘스트나 NPC보다 늦게 활성화되어도 현재 상태를 복원한다.
- 기존 `presentationBindings`가 없는 JSON, 기존 `IInteract`, 기존 NPC 이름표 및 트리아지 라벨의 동작이 바뀌지 않는다.

### 범위에서 제외할 항목

- 목표 위치까지 이어지는 길찾기 선이나 거리 표시.
- 여러 퀘스트 아이콘을 한 Interaction 행에 동시에 나열하는 UI.
- 서버 권위 게임 상태로서의 마커 동기화.
- 모든 기존 상호작용에 식별자를 강제로 부여하는 일괄 마이그레이션.

### 수정이 예상되는 주요 파일

- `Quest/QuestData.cs`, 퀘스트 정의 로더와 schema
- `InteractableEntity/IInteract.cs`, `Interactable.cs` 및 퀘스트 대상으로 쓰는 런타임 Interaction 구현
- 신규 `Quest/QuestPresentationService.cs`
- `Registry/Registry.cs`의 항목 등록·해제 이벤트
- `Entity/Npc.cs` 및 신규 `IOverheadPresentationAnchorProvider`
- `UI/Controllers/InteractableGameObjectHintUIController.cs`
- `UI/VisualElements/InteractableObjectHintListElement.cs`
- `UI/Controllers/EntityOverheadLabelUIController.cs`
- `UI/VisualElements/EntityOverheadLabelElement.cs`
- Scenario Graph Editor의 Quest 편집·검색·진단 코드

### 결정이 필요한 사항

- 기본 Interaction 아이콘과 NPC 머리 위 아이콘을 같은 에셋으로 쓸지, 각각 다른 기본 식별자를 둘지.
- 퀘스트가 `Completed` 상태로 목록에 남아 있을 때 아이콘을 즉시 숨길지, 완료 연출이 끝날 때까지 유지할지. 권장값은 완료 판정 즉시 숨김이 아니라 Quest HUD 완료 전환이 끝난 시점에 숨김이다. 이를 위해 완료 판정과 표시 만료를 구분하는 이벤트가 필요하다.
- 같은 우선순위의 여러 퀘스트가 한 대상을 가리킬 때 최신 퀘스트를 우선할지, 먼저 등록된 퀘스트를 유지할지. 권장값은 먼저 등록된 퀘스트 유지다.
