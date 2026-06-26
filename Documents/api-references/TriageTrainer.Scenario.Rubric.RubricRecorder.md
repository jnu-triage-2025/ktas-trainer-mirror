# TriageTrainer.Scenario.Rubric.RubricRecorder

평가 루브릭 수행/미수행 기록 코어(G-3 첫 증분)입니다. 시나리오 진행 중 각 루브릭 항목의
수행/미수행을 자동 판정하여 세션 단위로 누적합니다. 관찰자 모드 UI와 영속화(파일 저장)는
본 증분 범위 밖이며, 본 컴포넌트는 인메모리 누적 + CSV 내보내기까지 제공합니다.

## 1. 구성 요소

| 타입 | 역할 |
|---|---|
| `RubricItemDefinition` | 루브릭 항목 1개 정의(id, area, title, gateNodeIdentifier?, autoSignal?, perPlayer) |
| `RubricDefinitionSet` | 항목 묶음(데이터팩 JSON 역직렬화 대상) |
| `RubricResult` | 항목×대상(플레이어/팀)의 기록(status, retries, updatedAtUtc, note) |
| `RubricResultStore` | 세션 단위 결과 저장소(메모리) + CSV 내보내기 |
| `RubricRecorder` (MonoBehaviour) | `ScenarioController` 이벤트 구독 → 자동 판정·기록 |

## 2. 판정 규칙

`RubricRecorder`는 `ScenarioController.Instance`의 이벤트를 구독합니다.

| 이벤트 | 처리 |
|---|---|
| `OnScenarioStarted` | 새 세션 시작(새 `RubricResultStore` 생성) |
| `OnNodeChanged` | 매핑된 Validator 게이트 노드 진입 → 보류. 그 게이트가 아닌 다른 노드로 진입 = 게이트 통과 → **Performed** 기록 |
| `OnValidatorWaitTimeout` | 매핑된 게이트가 `ForceAdvance`/`FailBranch`로 타임아웃 → **NotPerformed** 기록 (G-6 연계). `WarnAndKeepWaiting`/`KeepWaiting`은 계속 대기이므로 미수행 확정하지 않음 |
| `OnScenarioEnded` | 세션 종료. `_logDecisions`면 CSV 콘솔 출력 |

- **수행 우선**: `RubricResultStore.Record`는 이미 `Performed`로 확정된 항목을 `NotPerformed`로 덮어쓰지 않습니다.
- 자동 신호가 없는 항목(`pass_*`, 신체 사정 등)은 자동 판정하지 않으며 `MarkManual`로 관찰자 수동 표기만 받습니다.
- 본 증분은 **팀 단위(플레이어 미구분, `playerId=null`)** 기록입니다. `PerPlayer` 항목의 플레이어 분배는
  후속(관찰자 모드/권한 연동)에서 확장합니다.
- **병렬(Parallel) 동시 게이트의 Performed 판정 한계**: Performed는 단일 보류 슬롯(`_pendingGateNodeIdentifier`)
  기반이라, 여러 브랜치 게이트가 동시에 진행되면 일부 Performed 마크가 누락될 수 있습니다. 반면
  **NotPerformed(타임아웃)는 이벤트가 해당 Validator 노드를 직접 전달**하므로 동시성과 무관하게 정확합니다.
  병렬 Performed 정밀화는 후속에서 게이트별 상태 추적으로 개선합니다.

## 3. 데이터팩(루브릭 정의) 형식

`_rubricDefinition`(TextAsset)으로 주입하는 JSON. 예: `Resources/Scenario/rubric_definition_disaster.json`.

```json
{
  "identifier": "rubric_definition_disaster",
  "items": [
    {
      "id": "rubric.c.bleeding_control_gauze",
      "area": "Circulation",
      "title": "지혈(거즈 압박, 환자 A)",
      "gateNodeIdentifier": "V016_2",
      "autoSignal": "apply_gauze",
      "perPlayer": false
    }
  ]
}
```

- `area`: `Triage` | `Airway` | `Breathing` | `Circulation` | `Disability` | `Exposure` | `General`.
- `gateNodeIdentifier`: 자동 수행 판정용 Validator 노드 ID(시나리오 JSON의 노드 식별자와 일치해야 함).
- `autoSignal`: 참고용 신호 조건명(현재 판정은 게이트 노드 통과/타임아웃 기준). 비워도 됨.

## 4. 공개 API

```csharp
public RubricResultStore Store { get; }            // 현재 세션 저장소(세션 전 null)
public void MarkManual(string itemId, string playerId, RubricStatus status, string note = "manual");
public void RecordRetry(string itemId, string playerId);   // 사정 퀴즈 오답/재응시 누적
```

`RubricResultStore`:

```csharp
RubricResult Record(string itemId, string playerId, RubricStatus status, string note = null);
void IncrementRetry(string itemId, string playerId);
bool TryGet(string itemId, string playerId, out RubricResult result);
IReadOnlyCollection<RubricResult> Snapshot();
string ExportCsv();   // sessionId,playerId,itemId,status,retries,updatedAtUtc,note
```

## 5. 의존성·전제

- `ScenarioController.OnValidatorWaitTimeout` 이벤트(게이트 타임아웃, G-6)에 의존합니다.
  타임아웃 정책이 `ForceAdvance`/`FailBranch`로 지정된 게이트만 "미수행"으로 자동 기록됩니다.
  → 게이트 타임아웃 설정: [validator-gate-timeout-setup-guide.md](../requirements/content-definitions/scenario/validator-gate-timeout-setup-guide.md)
- 게이트가 무한 대기(KeepWaiting)면 미수행이 자동 기록되지 않습니다(설계상 정상).

## 관련 문서

- [평가 루브릭 수행/미수행 기록 시스템(요구사항)](../requirements/content-definitions/scenario/evaluation-rubric-recording-system.md)
- [Validator 게이트 타임아웃 설정 가이드](../requirements/content-definitions/scenario/validator-gate-timeout-setup-guide.md)
- [MultiplayerInfrastructure.Scenario.ScenarioController.md](MultiplayerInfrastructure.Scenario.ScenarioController.md) — `OnValidatorWaitTimeout` 이벤트(§7-1)
