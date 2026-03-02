# MultiplayerInfrastructure.Datapack.DatapackRuntimeService

## 0. 문서 목적

이 문서는 데이터팩 런타임(`DatapackRuntimeService`)의 책임, JSON 구조, 등록/해제 흐름을 설명합니다.
현재 범위는 다음 두 기능입니다.

1. 주기적 명령 실행
2. Scenario 이벤트 핸들러 주입

---

## 1. 클래스 개요

대상 파일:
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Datapack/DatapackRuntimeService.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Datapack/DatapackModels.cs`

핵심 역할:
- JSON 데이터팩을 파싱/검증 후 등록
- 주기 명령 코루틴 실행 관리
- `ScenarioEventIdentifierRegistry`에 핸들러 주입/복원
- `ChatService.TryExecuteSystemCommand(...)`로 시스템 커맨드 실행

---

## 2. JSON 모델

### DatapackDefinition

- `packId: string` (필수)
- `periodicCommands: DatapackPeriodicCommand[]` (선택)
- `eventHandlers: DatapackEventCommandHandler[]` (선택)

### DatapackPeriodicCommand

- `command: string`
- `intervalSeconds: float` (최소 0.05로 보정)
- `runImmediately: bool`

### DatapackEventCommandHandler

- `eventIdentifier: string`
- `command: string`

샘플:
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Datapack/DatapackSample.json`

---

## 3. 등록/해제 흐름

### RegisterDatapackFromJson

1. JSON 공백/파싱 오류 검증
2. `packId` 필수 검증
3. 이미 로드된 `packId` 중복 검증
4. 주기 명령 코루틴 시작
5. 이벤트 핸들러 주입
   - 기존 핸들러가 있으면 백업 후 덮어쓰기
6. 로드된 데이터팩 딕셔너리에 저장

### UnregisterDatapack

1. 주기 명령 코루틴 중지
2. 주입된 이벤트 핸들러 제거
3. 이전 핸들러가 있으면 복원
4. 로드 목록에서 제거

---

## 4. 명령 실행 방식

- 데이터팩 커맨드는 내부적으로 선행 `/`를 제거 후 실행합니다.
- 실행 경로는 항상 `ChatService.TryExecuteSystemCommand(...)`를 사용합니다.
- 실행 결과는 로그로 기록됩니다.

예:
- `[Datapack:pack-id] periodic:30s -> Executed /help.`

---

## 5. 부트스트랩 로딩

`_bootstrapDatapacks`(TextAsset 목록)에 등록된 JSON은 `Start()`에서 자동 로드됩니다.
이 동작은 임시/개발 단계 기본 팩 로드에 유용합니다.

---

## 6. 제약 및 주의점

1. 현재는 런타임 서비스/모델만 제공되며, 인게임 등록 UI/명령은 별도 구현 필요
2. 이벤트 핸들러 주입은 동일 `eventIdentifier`를 덮어쓰므로 충돌 정책 설계 필요
3. 커맨드 실행 성공/실패 의미는 실제 커맨드 구현(sender 의존 여부)에 영향을 받음

---

## 7. 권장 확장

- `/datapack load <json>` / `/datapack unload <packId>` / `/datapack list`
- 데이터팩 스키마 버전 필드 추가
- 핸들러 충돌 정책(덮어쓰기/체인/거부) 옵션화
- 로드 실패 상세 리포트 구조화

---

## 8. 요약

`DatapackRuntimeService`는 “데이터 기반 명령 자동화”를 위한 런타임 코어입니다.
현재 구현은 주기 실행 + 이벤트 주입의 최소 기능을 제공하며, 이후 인게임 등록/관리 UX를 붙일 수 있도록 분리 설계되어 있습니다.
