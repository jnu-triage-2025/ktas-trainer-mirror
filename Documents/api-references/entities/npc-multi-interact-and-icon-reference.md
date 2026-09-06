# NPC Multi-Interact & Icon Reference API

> **2026-09-06 갱신 안내.** NPC 상호작용 정의는 더 이상 `NPCBaseModelSO`·`Npc` 인스펙터 목록
> (`_scenarioInteracts`, `_submissionInteracts`, `_customInteractSources`)에서 오지 않습니다. 시나리오 JSON 최상위
> `interactions` 구역과 전역 카탈로그 `Resources/Interactions/*.json`이 정의하고, 인터렉션 레지스트리가 노출을 판정합니다.
> 이 문서의 2절~7절은 아이콘 참조(`IconSpriteReference`) 부분만 유효하며, 인터렉션 목록에 관한 서술은 이력입니다.
> 현재 구조는 [changes/2026-09-06-interaction-registry-visibility.md](../../changes/2026-09-06-interaction-registry-visibility.md)와
> [MultiplayerInfrastructure.InteractableEntity.md](../MultiplayerInfrastructure.InteractableEntity.md) 8절을 보세요.

## 0. 이 문서의 목적

이 문서는 `Npc`가 제공하는 상호작용 시스템을
“왜 이렇게 설계되었는지(메커니즘)”와 “실제로 어떻게 쓰는지(사용법)” 관점에서 설명합니다.

대상 독자:
- Unity/C#를 막 익히는 학부생
- NPC 대화/퀘스트/훈련 시작 액션을 구현하려는 팀원

---

## 1. 핵심 개념

### 1-1. IInteractable vs IInteract

- `IInteractable`: 상호작용 가능한 오브젝트(컨테이너)
- `IInteract`: 실제 실행 가능한 액션(항목)

즉, `Npc`는 `IInteractable`이고,
그 안에 여러 `IInteract`를 담아서 UI에 보여주고 실행합니다.

비유하면:
- `IInteractable` = 메뉴판
- `IInteract` = 메뉴 항목 하나

### 1-2. NPC의 현재 구조

`Npc`는 `Interactable`을 상속하며, 내부적으로 다음 두 종류의 액션을 합쳐서 `Interacts`로 제공합니다.

1) 인터렉션 레지스트리 항목
- 시나리오 JSON `interactions` 구역과 전역 카탈로그가 이 NPC 식별자로 정의한 항목의 핸들러
  (`InteractionRegistry.CollectInteractsForEntity`)

(이전의 `scenarioInteracts`/`customInteractSources` 목록은 2026-09-06에 제거됐습니다.)

---

## 2. 관련 타입 정리

## 2-1. Npc

파일:
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/Npc.cs`

중요 필드:
- `_identifier`, `_npcBaseModel`(식별자·표시 이름·설명만)

중요 동작:
- `Interacts`: 레지스트리 항목을 모아 `IInteract[]` 반환(`InteractionRegistry.Changed` 시 재구성)
- `Interact(Transform)`: 기본적으로 `Interacts[0]` 실행

## 2-2. NPCBaseModelSO

파일:
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/NPCBaseModelSO.cs`

중요 필드:
- `identifier`
- `displayName`
- `description`
- `scenarioInteracts: List<NPCScenarioInteractDefinition>`

역할:
- NPC의 기본 데이터 템플릿
- 인스턴스 생성/적용 시 `Npc`가 해당 리스트를 복제해 사용

## 2-3. NPCScenarioInteractDefinition

파일:
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/NPCScenarioInteractDefinition.cs`

중요 필드:
- `_displayText`
- `_displayIcon: IconSpriteReference`
- `_displayColor`
- `_scenarioIdentifier`
- `_scenarioStartNodeIdentifier`

역할:
- “NPC가 시작할 시나리오 액션 1개”를 정의

## 2-4. IconSpriteReference (공용)

파일:
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Commons/IconSpriteReference.cs`

중요 필드:
- `_sprite`
- `_iconRegistryIdentifier`
- `_iconDefinition: IconSpriteDefinitions`

역할:
- 아이콘을 다형적으로 참조(직접/레지스트리/정의 enum)

---

## 3. 아이콘 해석 메커니즘

`IconSpriteReference.Resolve()`는 다음 우선순위로 아이콘을 찾습니다.

1. `_sprite`가 있으면 즉시 사용
2. `_iconDefinition != None`이면 enum을 registry identifier로 변환 후 조회
3. `_iconRegistryIdentifier`가 있으면 registry 조회
4. 모두 실패하면 `null`

Registry 조회는 `RegistryType.IconSprite`를 사용합니다.

관련 등록 위치:
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/Registry.Register.cs`

예:
- `IconSpriteDefinitions.NPCMessage -> "message-circle"`

---

## 4. NPC 인스턴스화 시 데이터 로드 흐름

### 단계별

1. `Npc.Awake()`
2. `EnsureBaseModelApplied()` 호출
3. `ApplyBaseModel()`에서 `_npcBaseModel.scenarioInteracts`를 순회
4. 각 `NPCScenarioInteractDefinition`을 `Clone()`해서 `_scenarioInteracts`에 저장
5. `RebuildInteracts()`에서 런타임 실행 가능한 `IInteract` 목록 구성

핵심 포인트:
- ScriptableObject 원본을 직접 참조하지 않고 clone해서 사용
- 런타임 변경이 원본 에셋에 역오염되지 않음

### 최근 주의점

`IconSpriteReference`에 새 필드(`_iconDefinition`)가 추가되면,
`Clone()`에도 반드시 복제가 추가되어야 합니다.

복제가 빠지면:
- 에셋에는 값이 있는데
- NPC 런타임 인스턴스에서는 값이 사라지는 현상 발생

현재 코드는 이 복제 문제를 반영한 상태입니다.

---

## 5. Unity 에디터 사용법 (실무 절차)

## 5-1. 아이콘 등록 준비

1. 아이콘 스프라이트를 Resources 경로에 둡니다.
2. `Registry.Register.cs`의 `IconLiterals`에 식별자와 경로를 등록합니다.
   - 예: `("message-circle", "Textures/Icons/message-circle")`

## 5-2. NPCBaseModelSO 작성

1. `Create > Triage Trainer > NPC Base Model`로 에셋 생성
2. `scenarioInteracts`에 항목 추가
3. 항목마다 다음 입력
   - `displayText`
   - `displayColor`
   - `scenarioIdentifier`
   - `scenarioStartNodeIdentifier`(선택)
   - `displayIcon` (아래 3가지 중 택1)
     - Sprite 직접 할당
     - IconRegistryIdentifier 문자열 입력
     - IconDefinition(enum) 선택

## 5-3. NPC 프리팹 연결

1. NPC 프리팹의 `Npc` 컴포넌트에서 `_npcBaseModel` 할당
2. 상호작용은 시나리오 JSON `interactions`(`entity.id` = NPC 식별자) 또는 `Resources/Interactions/*.json`에 정의
3. Play 모드에서 상호작용 UI에 여러 항목이 노출되는지 확인(`Tools > Multiplayer Infrastructure > Interaction Registry`)

---

## 6. Custom Interact 확장 예시 (이력)

아래는 2026-09-06 이전의 `_customInteractSources` 방식입니다. 지금은 NPC 전용 코드 핸들러가 필요하면
`IInteractionHandlerFactory`를 구현한 컴포넌트가 `handlerKey`로 핸들러를 만들고, 정의는 `interactions` 구역에 둡니다.

```csharp
using MultiplayerInfrastructure.InteractableEntity;
using UnityEngine;

public class NpcWaveInteract : MonoBehaviour, IInteract
{
  [SerializeField] private string _text = "손 흔들기";
  [SerializeField] private Sprite _icon;

  public string DisplayText => _text;
  public Sprite DisplayIcon => _icon;
  public Color DisplayColor => Color.cyan;

  public void Interact(Transform interactor)
  {
    Debug.Log("NPC가 손을 흔듭니다.");
  }
}
```

(이력) 이 컴포넌트를 NPC의 `_customInteractSources`에 추가하면 시나리오 시작 액션과 나란히 UI에 표시됐습니다.

---

## 7. 디버깅 체크리스트

### 증상 A: 아이콘이 안 보임

확인 순서:
1. `_sprite`가 null인지
2. `_iconDefinition`이 None이 아닌데 매핑 문자열이 맞는지
3. `_iconRegistryIdentifier`가 실제 등록 키와 일치하는지
4. Registry의 경로가 `Resources.Load<Sprite>`로 로드 가능한지

### 증상 B: 에셋에는 아이콘 정의가 있는데 런타임엔 사라짐

- `Clone()`에서 신규 필드 복제 누락을 우선 의심

### 증상 C: 액션이 목록에 안 뜸

- 레지스트리 창(`Tools > Multiplayer Infrastructure > Interaction Registry`)에서 해당 주소의 판정 사유를 확인
- `visibility.initial`이 `false`인데 여는 조건 절이나 `InteractionVisibility` 노드가 없는지 확인

---

## 8. 시나리오 JSON과의 연결 힌트

`scenarioIdentifier`는 시나리오 그래프 Registry 키와 일치해야 합니다.
예를 들어 현재 리소스에 `scenario_graph_quest.json`이 있다면,
등록 키가 `scenario_graph_quest`인지 확인한 뒤
NPC scenario interact의 `scenarioIdentifier`에 동일 문자열을 넣어야 합니다.

파일 예시:
- `Assets/Modules/TriageTrainer/Resources/Scenario/scenario_graph_quest.json`

---

## 9. 결론

현재 NPC 상호작용 시스템은
- 다중 액션,
- 다중 아이콘 참조 방식,
- ScriptableObject 안전 복제
를 기준으로 설계되어 있습니다.

새 기능을 추가할 때는 항상
1) 인터페이스 규약(`IInteract`)을 맞추고,
2) clone 경로에 신규 필드가 반영되는지,
3) Registry 키와 리소스 경로가 맞는지
를 함께 점검하면 안정적으로 확장할 수 있습니다.
