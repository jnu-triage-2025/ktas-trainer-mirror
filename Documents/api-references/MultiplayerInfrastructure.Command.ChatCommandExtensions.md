# MultiplayerInfrastructure.Command.ChatCommandExtensions

## 0. 문서 목적

이 문서는 2026-02-18 기준으로 추가된 채팅 커맨드 확장 내용을 정리합니다.
주요 대상은 `/give`, `/clean`, 그리고 플레이어 입력 외 시스템 호출 경로입니다.

---

## 1. 변경 요약

- 신규 커맨드 추가
  - `/give <item_identifier> [count=1] [player_identifier]`
  - `/clean [item_identifier] [count]`
- `ChatService`에 시스템 커맨드 실행 API 추가
  - `TryExecuteSystemCommand(string commandLine, out string result)`

관련 파일:
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandDefinition.Give.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandDefinition.Clean.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandService.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Chat/ChatService.cs`

---

## 2. /give 동작 명세

### 구문

`/give (item identifier: 필수) (count: 선택, 기본 1) (player identifier: 선택, 기본 호출자)`

### 실행 흐름

1. `RegistryType.Item`에 item identifier 등록 여부 확인
2. 대상 플레이어 해석
   - 기본: 호출자 본인
   - 지원: `@s`, `fish:<clientId>`, `<clientId>`
3. 템플릿 `Item`의 `ItemData`를 복제하여 지급 수량 설정
4. 인벤토리 추가 시도 (`TryAddItemToInventory(..., out leftover)`)
5. 남은 수량(`leftover`)이 있으면 대상 플레이어 앞에 월드 드롭 시도

### 주의점

- 시스템 호출(sender 없음)에서 대상을 생략하면 실패합니다.
- 아이템이 레지스트리에 없거나 템플릿 데이터가 없으면 실패합니다.

---

## 3. /clean 동작 명세

### 구문

`/clean (item identifier: 선택) (count: 선택)`

### 실행 규칙

1. item identifier 미지정
   - 인벤토리 전체 초기화
2. item identifier 지정 + count 미지정
   - 해당 아이템 전부 제거
3. item identifier 지정 + count 지정
   - `min(보유 수량, count)`만큼 제거
4. item identifier 없이 count만 오는 구문
   - 오류 처리
5. item identifier 미등록
   - 오류 반환 후 종료

### 주의점

- 현재 구현에서 `/clean`은 호출자 플레이어 컨텍스트가 필요합니다.
- 시스템 호출(sender 없음)에서는 플레이어를 찾을 수 없어 실패합니다.

---

## 4. ChatService 시스템 실행 API

### 추가 API

`bool TryExecuteSystemCommand(string commandLine, out string result)`

- 입력 문자열을 일반 커맨드 파서와 동일하게 처리합니다.
- sender가 `null`인 실행이므로, sender 의존 커맨드는 커맨드 구현에서 별도 처리해야 합니다.
- sender가 없을 때 `SendSystemMessage`는 타겟 RPC 대신 서버 로그로 출력됩니다.

---

## 5. 권장 테스트 시나리오

1. 플레이어 채팅 입력
   - `/give bandage`
   - `/give bandage 10`
   - `/clean`
   - `/clean bandage`
   - `/clean bandage 3`
2. 시스템 실행
   - `TryExecuteSystemCommand("give bandage 1 fish:2", out _)`
   - `TryExecuteSystemCommand("clean", out _)` (실패 기대)
3. 인벤토리 풀 상태
   - `/give`로 overflow 발생 시 월드 드롭 여부 확인

---

## 6. 요약

이번 확장은 “채팅 커맨드 = 플레이어 전용” 제약을 완화하여 시스템에서 재사용 가능한 실행 경로를 만든 것이 핵심입니다.
`/give`, `/clean`은 인벤토리 조작 API와 결합되어 관리형 동작(검증, 부분 제거, overflow 처리)을 제공합니다.
