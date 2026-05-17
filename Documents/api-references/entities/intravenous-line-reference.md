# API 레퍼런스: `TriageTrainer.Entity.IntravenousLine`

> 네임스페이스: `TriageTrainer.Entity.IntravenousLine`
>
> 파일 위치:
> - `Assets/Modules/TriageTrainer/Scripts/Entities/IntravenousLine/IntravenousLineConnectionPoint.cs`
> - `Assets/Modules/TriageTrainer/Scripts/Entities/IntravenousLine/IntravenousLineConnectionService.cs`

## 0. 개요

수액 라인 시스템은 두 개의 `IntravenousLineConnectionPoint`를 플레이어 상호작용으로 연결하고, 런타임 LineRenderer 오브젝트를 생성/시뮬레이션하는 기능이다.

- 포인트 컴포넌트: 인터랙션 진입점 + 연결 상태 보관
- 서비스 컴포넌트: 연결 모드 상태머신 + 아이템 소비 + 라인 생성/삭제

## 1. `IntravenousLineConnectionPoint`

### 1.1 책임

- 상호작용 액션 3종 노출
  - `intravenous_line_connect_mode_start`
  - `intravenous_line_connect_here`
  - `intravenous_line_disconnect`
- 포인트 식별자(`Identifier`) 자동 생성/중복 회피
- 연결된 라인 오브젝트 목록 관리

### 1.2 인터랙션 규칙

- `StartConnectionInteract`
  - 현재 포인트가 미연결이어야 함
  - 플레이어가 연결 모드가 아니어야 함
  - 플레이어 인벤토리에 필수 아이템이 하나 이상 있어야 함
- `ConnectHereInteract`
  - 플레이어가 연결 모드여야 함
  - 시작 포인트가 존재해야 함
  - 시작/종료 포인트가 서로 달라야 함
- `DisconnectInteract`
  - 현재 포인트에 연결된 라인이 하나 이상 있어야 함

### 1.3 주요 API

- `SetIdentifier(string identifier)`
- `HasAnyConnection`
- `RegisterConnectedLineObject(GameObject lineObject)`
- `UnregisterConnectedLineObject(GameObject lineObject)`
- `TryGetAnyConnectedLineObject(out GameObject lineObject)`
- `ResolveController()`

### 1.4 인스펙터 필드

- `connectionService`
- `_identifier`, `_autoGenerateIdentifier`
- `_displayIcon`
- `_interactConfigs`
- `_connectedLineObjects`(런타임 확인용)

## 2. `IntravenousLineConnectionService`

### 2.1 책임

- 플레이어별 연결 대기 상태(`PendingConnectionContext`) 관리
- 필수 아이템 검사/소비
- 연결 성공 시 라인 오브젝트 생성
- 해제 시 라인 오브젝트 삭제

### 2.2 연결 플로우

1. `BeginConnectionMode(player, startPoint)`
2. 플레이어가 다른 포인트에서 `TryCompleteConnection(player, endPoint)` 실행
3. 조건 검증 및 아이템 소비 후 라인 생성
4. 양쪽 포인트에 라인 오브젝트 등록
5. 연결 모드/대기 상태 해제

### 2.3 주요 API

- `HasAnyRequiredItem(PlayerController player)`
- `TryConsumeOneRequiredItem(PlayerController player)`
- `BeginConnectionMode(PlayerController player, IntravenousLineConnectionPoint startPoint)`
- `HasPendingStartPoint(PlayerController player, out IntravenousLineConnectionPoint startPoint)`
- `TryCompleteConnection(PlayerController player, IntravenousLineConnectionPoint endPoint)`
- `CancelConnectionMode(PlayerController player)`
- `DisconnectFromPoint(IntravenousLineConnectionPoint point)`

### 2.4 연결 제약

- 시작/종료 포인트 동일 연결 금지
- 이미 연결된 포인트 재연결 금지
- 가능하면 서로 다른 `NetworkObject` 소속 포인트 간 연결만 허용
- 필수 아이템(기본: `IntravenousSet.Identifier`)이 없으면 연결 거부

## 3. 라인 런타임 (`IntravenousLineConnectionRuntime`)

`IntravenousLineConnectionService`가 생성한 라인 오브젝트에 부착되며 `LateUpdate()`에서 선형을 갱신한다.

- 비물리 모드: 분석적 곡선(`_lineSagAmount`) 렌더링
- 물리 모드: Verlet 계열 세그먼트 시뮬레이션 + 거리 제약 + 충돌 보정

### 3.1 주요 바인딩 파라미터

- 형상: `_lineSegments`, `_lineSagAmount`
- 시뮬레이션: `_simulationStepsPerFrame`, `_solverIterations`, `_gravityScale`, `_velocityDamping`, `_slackLength`
- 충돌: `_collideWithWorld`, `_collisionRadius`, `_collisionMask`, `_triggerInteraction`

## 4. 인스펙터 핵심 필드

- Hierarchy: `_linesRoot`
- Line Visual: `_lineWidth`, `_lineColor`
- Line Shape: `_lineSegments`, `_lineSagAmount`
- Line Simulation: `_usePhysicsSimulation` 및 세부 파라미터
- Line Collision: 충돌 반경/레이어/트리거 처리
- Required Items: `_requiredItems`, `_requiredItemIdentifiers`

## 5. 운영 체크리스트

- 씬에 `IntravenousLineConnectionService`가 1개 이상 존재하는지 확인
- 각 포인트에 `SphereCollider`(Trigger) 반경이 유효한지 확인
- 필수 아이템 식별자가 인벤토리 아이템 식별자와 일치하는지 확인
- 연결 모드 UI/힌트 갱신이 플레이어 오너 기준으로 정상 동작하는지 확인

## 6. 관련 문서

- `Documents/requirements/interaction/triage-intravenous-line-requirements.md`
- `Documents/api-references/MultiplayerInfrastructure.Player.PlayerController.md`
