# Feature Proposal: 시나리오 트리거 존 활성 시나리오 게이팅(Active-Scenario Gating)

- 작성일: 2026-08-14
- 대상 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/`
- 선행/유사 패턴: `ScenarioInteractionSignals`(신호 계측), `IScenarioIdentifiedEntity`(대상별 신호, SIGNAL-BC-2), `ScenarioController.IsActive`(상태 기반 활성 여부)
- 관련 증상 로그:

  ```
  [ScenarioTriggerZone] Scenario graph is null and no enter-signals configured; nothing to trigger.
  MultiplayerInfrastructure.Scenario.ScenarioTriggerZone:TryTrigger (UnityEngine.GameObject)
  MultiplayerInfrastructure.Scenario.ScenarioTriggerZone:OnTriggerEnter (UnityEngine.Collider)
  ```

> 상태: **구현 반영됨(본 브랜치)**. 인간 작업자 검토 후 `done` 이동 예정.
> 반영 파일: `ScenarioController.cs`(공개 프로퍼티 추가), `ScenarioTriggerZone.cs`(감지 게이팅 + 오탐 오류 제거),
> 문서(`api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md`,
> `working-guide/features/scenario/interaction-signal-integration-spec.md`).
> 파일별 예시 코드는 같은 폴더의 [`example-implementation.md`](./example-implementation.md) 참조.

### 개요

`시나리오 트리거 존 활성 시나리오 게이팅`은 **재생 중인 시나리오가 없을 때 `ScenarioTriggerZone` 이
감지/신호 발신을 수행하지 않도록** 제한하는 변경이다. 이를 위해 "활성화된 시나리오 그래프가
존재하는지(재생 중인지)"를 확인하는 공개 API `ScenarioController.HasActiveScenario` 를 추가하고,
존의 진입 처리(`OnTriggerEnter`/`OnTriggerEnter2D` → `TryTrigger`/`TryRaisePerEntitySignal`)가
이 API 를 기준으로 동작하도록 조정한다.

- 요약:
  - `ScenarioController.HasActiveScenario`(=`_currentGraph != null`) 공개 프로퍼티 추가.
  - 신호 계열 존(그래프 미지정)은 시나리오 재생 중에만 감지한다. 재생 중이 아니면 진입을 무시한다.
  - 시나리오 시작용 존(그래프 지정)은 재생 중이 아닐 때 동작해야 하는 유일한 존이므로 게이팅 예외다.
  - 대상별 신호 전용 존(`_perEntitySignalTemplate` 만 설정, 그래프/진입 신호 없음)은 정상 구성이므로
    `TryTrigger` 경로에서 오류를 남기지 않는다(기존 오탐 오류 제거).
- 의도/목표: 시나리오가 재생되지 않는 구간(로비/자유 이동 등)에서 존 진입 시 발생하는
  `LogError` 노이즈와 무의미한 신호 발신을 제거하고, 재생 중에는 기존과 동일하게 게이팅 신호가
  정상 계측되도록 한다.
- 주요 맥락: OverworldScene 의 존은 전부 신호 전용 존이다(`scen_b:quest_arrival_triage_area` —
  진입 신호 + 대상별 신호, `ct:patient_target_pos_b` — 대상별 신호만). 이 중 대상별 신호 전용 존은
  플레이어(`Player` 태그)가 진입할 때마다 `TryTrigger` 가 "그래프도 진입 신호도 없음" 오류를
  남겼다. 재생 여부와 무관하게 발생하는 오탐이며, 재생 중이 아닐 때는 신호 자체가 무의미하다
  (시나리오 시작 시 `ScenarioInteractionSignals.ClearAllRaisedSignals()` 로 전부 초기화된다).
- 기술적 제약:
  - 하위호환 필수. 시나리오 시작용 존(그래프 지정, 예: `Researchs/TrialsScenarioGraph.unity`)의
    자동 시작 동작은 유지되어야 한다.
  - 재생 중 동작은 기존과 동일해야 한다(신호 계측, 대상별 계측, 쿨다운/1회 트리거 시맨틱 유지).
  - 재생 여부 판정은 `_state` 기반 `IsActive` 가 아니라 `_currentGraph` 기반이어야 한다.
    `IsActive` 는 노드 실행 상태 기반이라 클라이언트 표시(ClientPresentation) 모드에서는 대부분
    `Inactive` 이고, 서버에서도 즉시 진행 노드 체인 사이에서 `Inactive` 일 수 있어, 이를 쓰면
    재생 중에도 존이 감지를 멈추는 회귀가 발생한다.

### 해결하려는 문제 상황

나는 **시나리오 콘텐츠 운영자/개발자**로서, 시나리오가 재생되지 않는 동안에는 트리거 존이
아무것도 감지/발신하지 않기를 원한다. 왜냐하면:

1. 재생 중이 아닐 때 올린 신호는 어차피 다음 시나리오 시작 시 초기화되어 소비되지 않는다.
2. 대상별 신호 전용 존(예: `ct:patient_target_pos_b`)에 플레이어가 진입할 때마다
   `Scenario graph is null and no enter-signals configured; nothing to trigger.` 오류가 콘솔에
   스팸되어 실제 오류를 가린다. 이 존은 정상 구성(대상별 신호 전용)인데도 오류로 표시된다.
3. `_perEntityRaiseOncePerEntity=true` 존은 재생 전 진입으로 엔티티 식별자가 미리 기록되면,
   정작 시나리오 재생 중에는 재발신되지 않는 잠재 오염이 있다.

### 사용자 경험 목표

- 운영자: 시나리오 미재생 구간에서 존을 지나도 콘솔에 오류가 남지 않는다.
- 운영자: 시나리오 재생 중에는 기존과 동일하게 진입 신호/대상별 신호가 계측되어 게이트·
  카운터가 정상 동작한다.
- 개발자: "시나리오 재생 중인가"를 판정하는 공개 API(`HasActiveScenario`)를 다른 시스템에서도
  재사용할 수 있다.

### 제안

#### 1. 재생 여부 판정 API 추가 (`ScenarioController.cs`)

```csharp
/// <summary>활성화된(재생 중인) 시나리오 그래프가 존재하는지 여부.</summary>
public bool HasActiveScenario => _currentGraph != null;
```

- `_currentGraph` 는 세 실행 모드(Local/ServerAuthoritative/ClientPresentation) 모두에서 시작 시
  설정되고 종료(`EndScenario`/`EndPresentationScenario`) 시 해제된다. 따라서 서버/클라이언트/
  오프라인 어디서든 "재생 중"을 일관되게 판정할 수 있다.
- `IsActive`(`_state != State.Inactive`)와의 차이를 XML 주석과 API 레퍼런스에 명시한다.

#### 2. 존 감지 게이팅 (`ScenarioTriggerZone.cs`)

| 존 구성 | 재생 중 | 미재생 |
|---|---|---|
| 그래프 지정(시나리오 시작용) | 기존 동작(컨트롤러가 중복 시작 거부) | 기존 동작(시나리오 자동 시작) |
| 진입 신호/대상별 신호 존 | 기존 동작(신호 계측) | **무시(감지 안 함)** |
| 대상별 신호 전용 존 | 기존 동작 + `TryTrigger` 경로 오탐 오류 제거 | **무시(감지 안 함)** |
| 완전 미구성(그래프/신호/템플릿 모두 없음) | 기존 오류 유지(실제 설정 오류) | 무시 |

- `TryRaisePerEntitySignal`: 재생 중이 아니면 발신하지 않는다(대상별 신호는 재생 중 시나리오의
  게이트/카운터에서만 소비된다). 부수 효과로 `_perEntityRaised` 재생 전 오염도 제거된다.
- `TryTrigger`: 그래프가 없는 존은 재생 중이 아니면 조기 반환한다(쿨다운/1회 트리거 상태를
  소비하지 않는다). 그래프가 있는 존은 게이팅 예외로 두어 자동 시작을 유지한다.
- `TryTrigger` 의 "아무것도 트리거할 것이 없음" 오류 조건에 `_perEntitySignalTemplate` 설정을
  정상 구성으로 포함시켜, 대상별 신호 전용 존의 오탐 오류를 제거한다.

### 자세한 달성 목표

- 재생 중 신호 계측 경로(`_raiseSignalsOnEnter`, `_perEntitySignalTemplate`, SignalCounter 연동)
  는 동작이 변하지 않아야 한다.
- `Researchs/TrialsScenarioGraph.unity` 의 그래프 지정 존은 미재생 상태에서 진입 시 시나리오를
  자동 시작해야 한다(기존 기능 유지).
- `ct:patient_target_pos_b` 존에 플레이어가 진입해도(재생/미재생 무관) 오류가 발생하지 않아야
  한다. 환자(`IScenarioIdentifiedEntity`) 진입 시 대상별 신호는 재생 중에만 발신된다.

### 문서화

- `Documents/api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md`:
  §4 프로퍼티에 `HasActiveScenario` 추가, `IsActive` 와의 차이 명시.
- `Documents/working-guide/features/scenario/interaction-signal-integration-spec.md`:
  구역 진입/대상별 신호 절에 "재생 중인 시나리오가 없으면 존이 감지/발신하지 않는다"는
  게이팅 동작을 명시한다(기존 "시나리오 시작 여부와 독립" 문구는 '존의 시나리오 시작 기능
  유무'와 무관하다는 의미였으므로, 재생 게이팅과 혼동되지 않게 보강한다).

### 가용성과 테스트

- 위험: 낮음. 변경은 `ScenarioTriggerZone` 진입 처리와 읽기 전용 프로퍼티 추가에 한정된다.
- 기존 에디터 테스트(`PatientBCScenarioDataTests` 의 존 구성 검증)는 직렬화 필드만 검사하므로
  영향이 없다.
- 수동 검증 절차:
  1. 미재생 상태에서 `ct:patient_target_pos_b` / `scen_b:quest_arrival_triage_area` 존 진입 →
     오류/신호 발신 없음.
  2. 시나리오 재생 중 동일 존 진입 → 신호 계측 정상(`sig.*` RuntimeState 확인).
  3. `TrialsScenarioGraph` 씬에서 그래프 지정 존 진입 → 시나리오 자동 시작.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표: 미재생 구간의 존 진입에서 `LogError` 가 더 이상 발생하지 않는다.
- 수용 기준:
  - `ScenarioController.HasActiveScenario` 가 서버/클라이언트/오프라인 모두에서 재생 구간 동안
    `true` 를 반환한다.
  - 재생 중 존 신호 계측이 기존과 동일하게 동작한다.
  - 그래프 지정 존의 자동 시작이 유지된다.

### 링크, 참고사항

- `Documents/api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md`
- `Documents/working-guide/features/scenario/interaction-signal-integration-spec.md`
- `Documents/api-references/MultiplayerInfrastructure.Entity.IScenarioIdentifiedEntity.md`
