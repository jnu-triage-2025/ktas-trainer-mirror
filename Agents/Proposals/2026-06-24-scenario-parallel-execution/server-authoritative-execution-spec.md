# 설계 명세 — G-8: 서버 권한 시나리오 실행(Server-Authoritative Scenario Execution)

## 문제 (현행 모델 분석)

현재 시나리오는 **클라이언트마다 로컬로 실행**된다.

1. 디스패치: `ChatService.TryDispatchScenario`(서버) → 대상별 `TargetRunScenario`([TargetRpc], `ChatService.cs:108`).
2. 실행: 각 클라이언트의 `ScenarioController.Instance.StartScenario(graph, null, owner)`.
   - `ScenarioController` 는 **순수 `MonoBehaviour` 싱글톤**(`ScenarioController.cs:25`) — 네트워크 식별자 없음.
3. 플레이어 풀: `GetActivePlayerIds()`(`ScenarioController.cs:1491`) 는
   - 호스트(서버+클라)에서는 `ServerManager.Clients` 전체,
   - **순수 클라이언트에서는 로컬 연결 1개만** 반환.
4. 완료 신호: `ScenarioInteractionSignals.Raise` → `Registry.Register(RegistryType.RuntimeState, ...)`.
   `Registry` 는 **정적·비네트워크 저장소**(`Registry.cs:10`). 인터랙션(`Item.OnGet` 등)은
   소유 클라이언트 컨텍스트에서 실행(`PlayerController.Network.cs:571`)되므로 신호도 **그 클라이언트 로컬**.

### 결과적 결함
- `ByRole`/`SpreadOrdinary` 등 다인 분배가 순수 클라이언트에서 **항상 1인(본인)** 으로 붕괴.
  → 본 PR 에서 추가한 `ByRole`(다인 협력)이 실질적으로 동작하지 않는다.
- 한 플레이어의 인터랙션 완료가 **다른 플레이어 브랜치의 게이트**(Validator `waitForCondition`)에 반영되지 않는다.
- 각 클라이언트가 그래프를 독립 진행 → 진행/분기/타이밍이 클라이언트마다 어긋날 수 있다(상태 비일관).

> 요약: 본 PR(G-1~G-3)의 엔진 로직은 "단일 권위 실행 + 채워진 플레이어 풀 + 공유 신호" 전제 하에서 정확하다.
> G-8 은 그 전제를 제공하는 **실행/네트워킹 계층** 작업이다.

## 제안: 서버 권한 단일 실행 + 클라이언트 표현/보고 (방안 8-A, 권장)

### 핵심 아이디어
- 시나리오 그래프는 **서버에서 단 한 번** 진행(권위 상태기). 서버가 노드 순회·분배·게이트 판정을 소유.
- 클라이언트는 "표현(presentation)"과 "입력 보고(report)"만 담당:
  - 서버 → 클라: Dialogue/Choice/UI/Sound 표시, 인터랙션 요청(누가 무엇을), 카메라/이동 연출.
  - 클라 → 서버: 선택지 선택, 인터랙션 완료 신호(`sig.*`), 구역 진입 등.

### 구성요소

#### A. ScenarioController 를 NetworkBehaviour 화(또는 네트워크 릴레이 동반)
두 가지 방식 중 택1:
- (A-1) `ScenarioController` 를 `NetworkBehaviour` 로 승격하고 서버에서만 그래프 루프를 돌린다.
  표현 메서드(`DisplayDialogue`/`DisplayChoice`/사운드 등)는 `ObserversRpc`/`TargetRpc` 로 클라에 전달.
- (A-2) `ScenarioController` 는 `MonoBehaviour` 유지하되, 신규 `ScenarioNetworkRelay : NetworkBehaviour` 를
  도입하여 서버 실행 결과를 RPC 로 중계(기존 싱글톤 침습 최소화, 권장).

#### B. 권위 플레이어 풀
- `GetActivePlayerIds()` 를 서버 실행 컨텍스트에서 `ServerManager.Clients` 기준으로만 계산하도록 보장.
- 클라이언트는 분배/할당 판정을 하지 않는다(서버가 결정해 TargetRpc 로 "당신의 브랜치는 X" 통보).

#### C. 공유 완료 신호(Authoritative RuntimeState)
- 인터랙션 완료 시 클라이언트는 `CmdRaiseScenarioSignal(signalId)`([ServerRpc, RequireOwnership=false]) 로 보고.
- 서버가 단일 권위 `RuntimeState` 에 `Register` → Validator(`RegistryContains`/`SignalRaised`)가 서버에서 판정.
- `ScenarioInteractionSignals.Raise` 의 시그니처는 유지하되, 내부에서
  "서버면 직접 Register / 클라면 Cmd 로 서버 보고" 로 분기(호출부 변경 최소화).
- 사이클 반복 시 서버가 `Clear` 권위적으로 수행(현재 CPR 2사이클 등).

#### D. 표현 동기화
- Dialogue/Choice/Sound/Delay 표시는 `ObserversRpc`(전원) 또는 `TargetRpc`(특정 역할 플레이어)로.
- 브랜치별 안내(예: "간호사 D 는 석션")는 해당 역할 플레이어에게 `TargetRpc`.
- Choice/Quiz 선택은 권위 결정 주체(보통 owner 또는 역할 플레이어)만 `Cmd` 로 제출.

### 하위호환·범위
- 단일 플레이어(호스트 단독)에서는 기존 동작과 동일(서버=클라).
- 신규 RPC 는 시나리오 전용 경로에만 추가. 기존 `disaster_intro` 등은 동일하게 동작(브랜치 없는 선형 그래프는 영향 없음).
- 범위 한정: 본 명세는 **실행 권위 이전 + 신호 공유 + 표현 RPC** 까지. 정교한 예측/롤백(rollback) 은 비범위.

## 대안: 클라이언트 로컬 + 공유 상태 복제 (방안 8-B)

- 각 클라가 그래프를 계속 로컬 실행하되, 플레이어 목록·완료 신호·브랜치 할당만 서버 권위로 복제(SyncVar/Registry 복제).
- 장점: `ScenarioController` 루프 구조 변경 최소.
- 단점: N개 로컬 상태기의 분기/타이밍 동기화가 본질적으로 취약(Choice/Delay/Random 분배 시 분기 분기). 권장하지 않음.

## 단계적 구현 계획(위험 최소화)

1. **P1 — 공유 신호(C)만 우선**: `ScenarioInteractionSignals` 에 서버 보고 분기 + `ScenarioNetworkRelay.CmdRaiseScenarioSignal`.
   효과: 호스트가 시나리오를 단독 실행하고 다른 클라가 인터랙션하는 구성에서 게이트가 동작.
2. **P2 — 권위 플레이어 풀(B)**: 분배를 서버에서만 수행하고 결과를 TargetRpc 통보.
3. **P3 — 표현 RPC(D) + 실행 권위 이전(A-2)**: 그래프 루프를 서버 단일화, 표시를 RPC 로.

각 단계는 독립적으로 머지 가능하며, P1 만으로도 "호스트 실행 + 다인 인터랙션" 시나리오의 핵심 결함을 완화한다.

## 직렬화/검증/테스트
- 신규 RPC 단위: `CmdRaiseScenarioSignal`, `TargetAssignBranch`, `ObserversDisplayDialogue` 등.
- 통합: 2인 이상 클라 환경에서 P003(B/C/D 동시) ByRole 분배 → 각자 인터랙션 → 서버 신호 누적 → WaitAll 합류.
- 회귀: 단일 플레이어/선형 그래프(`disaster_intro`) 동작 불변.

## 영향
- 본 PR(G-1~G-3)의 다인 협력·게이팅이 **실제 멀티플레이에서 의도대로** 동작하게 된다.
- `MultiplayerInfrastructure` 네트워킹 계층 변경이므로 루트 `AGENTS.md` 정책에 따라 본 명세 선행 후 단계적 구현.

## 링크
- 상위 제안: [`Feature Proposal - Scenario Parallel Execution & Gating.md`](./Feature%20Proposal%20-%20Scenario%20Parallel%20Execution%20&%20Gating.md)
- 엔진 근거: `ScenarioController.cs`(`StartScenario`, `GetActivePlayerIds:1491`, `TryAllocateParallel:1518`), `ChatService.cs`(`TryDispatchScenario:257`, `TargetRunScenario:108`), `Registry.cs`, `ScenarioInteractionSignals.cs`
