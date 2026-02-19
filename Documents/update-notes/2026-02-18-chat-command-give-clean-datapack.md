# 2026-02-18 변경 노트: /give, /clean 커맨드 + 시스템 커맨드 실행 + 데이터팩 런타임

## 요약

이번 변경은 MultiplayerInfrastructure의 커맨드/인벤토리 자동화 기반을 확장하는 작업입니다.
핵심은 다음 3가지입니다.

1. 신규 커맨드 `/give`, `/clean` 추가
2. 플레이어 입력 외 시스템 호출 경로(`TryExecuteSystemCommand`) 추가
3. 데이터팩 런타임(JSON) 추가: 주기 명령 실행 + 이벤트 핸들러 주입

---

## 1) 신규 커맨드

### /give

구문:
- `/give <item_identifier> [count=1] [player_identifier]`

동작:
1. `RegistryType.Item` 등록 여부 확인
2. 대상 플레이어 해석 (기본 호출자, 또는 `@s`/`fish:<id>`/`<id>`)
3. 인벤토리 추가 시도
4. 인벤토리 overflow가 발생하면 남는 수량을 대상 플레이어 앞에 월드 드롭

### /clean

구문:
- `/clean [item_identifier] [count]`

동작:
1. item 미지정: 전체 인벤토리 초기화
2. item 지정 + count 미지정: 해당 아이템 전량 제거
3. item 지정 + count 지정: `min(보유량, count)` 제거
4. item 없이 count만 사용: 구문 오류
5. item 미등록: 오류 반환 후 종료

---

## 2) 시스템 커맨드 실행 경로 추가

대상:
- `ChatService`

추가 API:
- `TryExecuteSystemCommand(string commandLine, out string result)`

의미:
- 채팅 UI 입력 경로와 동일한 파서를 재사용하면서, sender 없는 시스템 실행을 허용
- 향후 스케줄러/시나리오/서버 자동화에서 명령 재사용 가능

주의:
- sender 의존 커맨드(예: 호출자 인벤토리 필요)는 시스템 실행 시 실패 가능

---

## 3) 데이터팩 런타임 추가

대상:
- `DatapackModels.cs`
- `DatapackRuntimeService.cs`

지원 기능:
1. 주기 커맨드 실행
   - interval 기반 코루틴
   - 즉시 실행(runImmediately) 옵션
2. 이벤트 핸들러 주입
   - `ScenarioEventIdentifierRegistry`에 이벤트별 핸들러 등록
   - 기존 핸들러 백업/복원 지원

JSON 예시:
- `DatapackSample.json`

현 시점 범위:
- 런타임 코어 구현 완료
- 인게임 등록/로드 UX는 미구현(후속 정의 예정)

---

## 4) 인벤토리/아이템 유틸 확장

커맨드 지원을 위해 `PlayerController.Inventory`에 서버 조작용 API를 확장:

- `TryAddItemToInventory(..., out leftover)`
- `ClearInventory()`
- `RemoveItemFromInventory(...)`
- `RemoveAllOfItemFromInventory(...)`
- `CountItemInInventory(...)`
- `TryDropItemInFront(...)`

또한 월드 드롭 유틸 추가:
- `ItemSpawnUtility.TrySpawnDroppedItem(...)`
- `Item.ApplyRuntimeItemData(...)`

---

## 5) 검증 메모

- 변경 파일 기준 IDE 진단에서 컴파일 오류 없음 확인
- `/clean`은 호출자 컨텍스트 기반 설계이므로 시스템(sender 없음) 경로에서는 실패 처리됨

---

## 6) 관련 파일

- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandDefinition.Give.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandDefinition.Clean.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandService.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Chat/ChatService.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Datapack/DatapackModels.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Datapack/DatapackRuntimeService.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Inventory.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Item/ItemSpawnUtility.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Item/Item.RuntimeData.cs`
