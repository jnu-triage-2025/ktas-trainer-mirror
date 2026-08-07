# 시나리오 인라인 NPC ActingNpc 정의

> 상태: **반영 완료(done, 2026-08-07)**. `ScenarioGraph.ActingNpcs`, 로더/스키마 검증, `Npc.ConfigureScenarioActingNpc`, Graph Editor 편집 뷰(`ScenarioActingNpcEditorView`) 및 round-trip 테스트까지 구현됨.

### 개요

시나리오 JSON의 최상위 `actingNpcs`에서 NPC 인스턴스의 preset, 식별자, 위치, 회전,
기본 상호작용과 종료 정리 정책을 선언한다. 메시·Animator·NetworkObject 같은 Unity 기술
구성은 기존 EntityPreset catalog에 유지한다.

### 해결하려는 문제 상황

현재 그래프는 `EntityPresetSpawn`으로 이미 등록된 프리팹을 만들 수 있지만 NPC의 인스턴스
데이터와 상호작용은 `NPCBaseModelSO`, 씬 오브젝트 또는 별도 Interactable에 분산된다.
시나리오 작성자는 한 JSON만 보고 해당 시나리오가 생성하는 NPC와 행동 계약을 파악할 수 없다.

### 사용자 경험 목표

- 콘텐츠 작성자는 `actingNpcs` 한 곳에서 시나리오 소유 NPC를 선언한다.
- 시나리오 시작 전에 NPC가 생성·등록되어 첫 노드부터 `NPCMove`로 참조할 수 있다.
- 시나리오 종료 시 해당 시나리오가 소유한 NPC가 자동 정리된다.
- 아이템 제출 및 다른 시나리오 시작 상호작용을 별도 SO 없이 정의할 수 있다.

### 제안

- `ScenarioGraph.ActingNpcs`와 JSON DTO/schema를 추가한다.
- actingNpc는 `presetIdentifier`로 기존 EntityPreset을 참조한다.
- `Npc`가 `ISpawnedEntityIdentifierReceiver`를 구현하여 Entity/Npc registry 키를 일치시킨다.
- `ScenarioController`가 시작 전 actingNpc를 생성하고, 종료/실패/교체 시 소유 actingNpc를 정리한다.
- 상호작용은 `StartScenario`, `ItemSubmission`과 단순 런타임 신호를 발생시키는 `Signal`을 지원한다.
- Scenario Graph Editor의 Edit 탭에서 최상위 `actingNpcs`와 중첩 상호작용/필수 아이템을 수정한다.
- 네트워크 프리팹 등록과 에셋 전달은 기존 preset catalog의 책임으로 유지한다.

### 자세한 달성 목표

- actingNpc identifier 중복 및 알 수 없는 enum은 로드 오류로 거부한다.
- preset이 없거나 `Npc` 컴포넌트가 없으면 시나리오 시작을 거부하고 부분 생성물을 정리한다.
- 회전 Euler 값과 `despawnOnScenarioEnd`를 지원한다.
- 저장-로드-저장 과정에서 actingNpc 데이터가 보존된다.
- Graph Editor에서 추가·삭제·필드 수정한 actingNpc 데이터가 JSON 저장과 undo/redo에 포함된다.

### 문서화

- ScenarioGraph 가이드에 `actingNpcs` 계약과 예제를 추가한다.
- NPC API 문서에 런타임 식별자 주입과 인라인 상호작용 경로를 추가한다.

### 가용성과 테스트

- 기존 JSON에는 `actingNpcs`가 없으므로 하위호환된다.
- schema 및 round-trip EditMode 테스트를 추가한다.
- 오프라인과 서버 권위 스폰/종료를 PlayMode에서 확인한다.
- 런타임 AddComponent 상호작용은 actingNpc `NetworkObject` 참조를 포함한 Observers RPC로 원격
  클라이언트에 동일 구성한다.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 유효한 actingNpc JSON이 schema 검증과 round-trip 테스트를 통과한다.
- 시나리오 시작 직후 `RegistryType.Npc`에서 actingNpc identifier를 조회할 수 있다.
- 상호작용 UI에 선언된 항목이 나타나고 제출 완료 신호가 발생한다.
- 정상 종료와 시작 실패 뒤 actingNpc GameObject 및 registry 항목이 남지 않는다.

### 링크, 참고사항

- `ScenarioGraph`
- `ScenarioController`
- `Npc`
- `Registry.TrySpawnEntityPreset`
