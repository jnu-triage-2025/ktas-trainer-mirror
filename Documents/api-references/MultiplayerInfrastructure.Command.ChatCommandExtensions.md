# MultiplayerInfrastructure.Command.ChatCommandExtensions

## 0. 문서 목적

이 문서는 채팅 커맨드 확장 내용을 정리합니다.
주요 대상은 `/give`, `/clean`, `/title`, `/problemsheet`, 대상 선택자 파싱, 파이프라인 반환 규약입니다.

---

## 1. 변경 요약

- 신규/확장 커맨드
   - `/give <item_identifier> [count=1] [target_identifier]`
   - `/clean [item_identifier] [count]`
   - `/title <target> (clear|reset)`
   - `/title <target> (title|subtitle|actionbar) <text>`
   - `/title <target> times <fadeIn> <stay> <fadeOut>`
   - `/problemsheet list`
   - `/problemsheet <target> <problem-identifier> [problem-index]`
- 공통 대상 선택자 파싱 추가
   - `@p`, `@a`, `@r`, `@s`, `@e`, `@n`
   - `x,y,z,distance,dx,dy,dz,tag,type` 인자 지원
- `ChatService`에 시스템 커맨드 실행 API
   - `TryExecuteSystemCommand(string commandLine, out string result)`

관련 파일:
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/TargetSelectorResolver.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandDefinitions/CommandDefinition.Give.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandDefinitions/CommandDefinition.Clean.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandDefinitions/CommandDefinition.Title.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandService.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Chat/ChatService.cs`

---

## 2. /give 동작 명세

### 구문

`/give (item identifier: 필수) (count: 선택, 기본 1) (target identifier: 선택, 기본 호출자)`

### 실행 흐름

1. `RegistryType.Item`에 item identifier 등록 여부 확인
2. 대상 해석
   - 기본: 호출자 본인
   - 지원: `@s`, `fish:<clientId>`, `<clientId>`
   - 선택자: `@p/@a/@r/@s/@e/@n` + 인자(`distance`, `tag`, 등)
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

## 4. /title 동작 명세

### 구문

`/title <target> (clear|reset)`

`/title <target> (title|subtitle|actionbar) <text>`

`/title <target> times <fadeIn> <stay> <fadeOut>`

### 실행 규칙

1. `<target>`을 선택자 파서로 해석하여 대상 연결을 찾습니다.
2. `title`/`subtitle`/`actionbar`는 클라이언트 타이틀 UI에 표시합니다.
3. `times`는 클라이언트 로컬에 저장되는 페이드 타이밍(틱)을 갱신합니다.
4. `clear`는 타이틀/서브타이틀/액션바를 제거합니다.
5. `reset`은 타이밍을 기본값으로 되돌리고 서브타이틀을 초기화합니다.

---

## 5. 대상 선택자 지원

### 선택자

- `@p`: 가장 가까운 플레이어
- `@a`: 모든 플레이어
- `@r`: 무작위 플레이어
- `@s`: 실행자
- `@e`: 모든 엔티티 (현재 `type=player`만 허용)
- `@n`: 가장 가까운 엔티티 (현재 `type=player`만 허용)

### 인자

- `x,y,z` (좌표)
- `distance` (구간)
- `dx,dy,dz` (직육면체 범위)
- `tag` (태그)
- `type` (엔티티 타입)

### 제한

- 엔티티 대상은 아직 지원하지 않으며 `@e/@n` 사용 시 `type=player`만 허용됩니다.
- 단일 대상이 필요한 커맨드에서 다수 매칭이면 실패합니다.

---

## 6. ChatService 시스템 실행 API

### 추가 API

`bool TryExecuteSystemCommand(string commandLine, out string result)`

- 입력 문자열을 일반 커맨드 파서와 동일하게 처리합니다.
- sender가 `null`인 실행이므로, sender 의존 커맨드는 커맨드 구현에서 별도 처리해야 합니다.
- sender가 없을 때 `SendSystemMessage`는 타겟 RPC 대신 서버 로그로 출력됩니다.

---

## 7. /problemsheet 동작 명세

### 구문

`/problemsheet list`

`/problemsheet <target> <problem-identifier> [problem-index]`

### 실행 규칙

1. `list`는 Registry + `Resources/Problems`를 기반으로 사용 가능한 문제세트 식별자를 출력합니다.
2. 실행 구문에서 `problem-index`를 주면 해당 문제만 단일 모드로 엽니다(1-based).
3. `problem-index` 생략 시 전체 세트 모드로 시작하며, 정답 시 다음 문제 진행 UI를 사용합니다.

### 파이프라인 반환 규약

- `/problemsheet`는 `IChatCommandPipelineCommand`를 구현합니다.
- 반환값은 마지막 채점 코드 1개입니다.
  - `0`: 정답
  - `1`: 오답
- 이 값은 서버가 연결 단위로 유지하는 마지막 문제 판정 결과를 기준으로 생성됩니다.

---

## 7. 권장 테스트 시나리오

1. 플레이어 채팅 입력
   - `/give bandage`
   - `/give bandage 10`
   - `/title @s title Mission Start`
   - `/title @a[distance=..20] actionbar Get ready`
   - `/clean`
   - `/clean bandage`
   - `/clean bandage 3`
2. 시스템 실행
   - `TryExecuteSystemCommand("give bandage 1 fish:2", out _)`
   - `TryExecuteSystemCommand("clean", out _)` (실패 기대)
3. 인벤토리 풀 상태
   - `/give`로 overflow 발생 시 월드 드롭 여부 확인

---

## 8. 요약

이번 확장은 “채팅 커맨드 = 플레이어 전용” 제약을 완화하여 시스템에서 재사용 가능한 실행 경로를 만든 것이 핵심입니다.
`/give`, `/clean`은 인벤토리 조작 API와 결합되어 관리형 동작(검증, 부분 제거, overflow 처리)을 제공합니다.
