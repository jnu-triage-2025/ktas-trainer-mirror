# Feature Proposal - ByRole Granted-Roles-Only Allocation

### 개요

`Parallel(ByRole)` 노드의 활성 역할(activeRoleTags) 배정에서, 한 플레이어가 여러 역할 태그를 부여받은 단일 플레이어 상황이라도 **실제로 부여된 역할의 브랜치만** 해당 플레이어에게 배정하도록 정정하는 변경입니다. 기존에는 접속자가 1명이기만 하면 보유 여부와 무관하게 모든 역할 브랜치가 그 플레이어에게 일괄 배정(`singlePlayerDebug` 경로)되었습니다.

변경 후에도 같은 클라이언트에 중복 배정된 브랜치들은 기존 순차 실행 기반(`RunSequentially`, 그래프 정의 순서)을 그대로 따릅니다. 즉 `nurse_a`~`nurse_d`를 모두 보유한 플레이어는 a 로직 처리 → a 퀘스트 완료 → b 로직 처리 → … → d 퀘스트 완료 순으로 진행하고, 일부 역할만 보유한 플레이어는 보유 역할의 브랜치만 같은 순서 규칙으로 진행합니다.

- 기능 요약: ByRole 활성 역할 배정을 "역할 홀더에게만 배정" 규칙으로 일원화
- 목적: 시나리오 정의(부여된 역할에 대해서만 처리)와 런타임 동작의 일치
- 기술적 제약: MultiplayerInfrastructure의 재사용 모듈이므로 다른 그래프/프로젝트에 대한 영향을 최소화

### 해결하려는 문제 상황

시나리오 운영자로서, 단일 플레이어 디버그/소규모 인원 세션에서 플레이어에게 부여된 역할의 퀘스트만 진행되기를 원한다. 왜냐하면 부여되지 않은 역할의 브랜치까지 실행되면 퀘스트 UI와 대사가 의도하지 않은 역할 내용까지 노출되어, 훈련 시나리오(예: `patient_b_c_ct`의 환자 B/C 처치)의 역할 분담 검증이 불가능하기 때문이다.

기존 동작(`TryAllocateActiveRoleParallel`의 `singlePlayerDebug`): roster에 포함된 클라이언트가 1명이면 4개 브랜치 전부를 `roster[0]`에 배정했다. 따라서 `nurse_a`만 부여된 플레이어에게도 B/C/D 브랜치의 퀘스트·다이얼로그가 실행되었다.

### 사용자 경험 목표

- 플레이어에게 `nurse_a`, `nurse_c`가 부여된 경우: A 브랜치 완료 후 C 브랜치가 순차 실행되고, B/D 브랜치는 표시·실행되지 않는다.
- 플레이어에게 네 역할이 모두 부여된 경우: 기존과 동일하게 a → b → c → d 순서로 각 체인이 완료까지 진행된 뒤 다음 역할로 넘어간다.
- 복수 클라이언트가 역할을 나눠 맡은 경우: 기존과 동일하게 클라이언트 간 병렬, 클라이언트 내 순차로 동작한다.

### 제안

`ScenarioController.TryAllocateActiveRoleParallel`에서 `singlePlayerDebug` 일괄 배정 특례를 제거하고, 배정 판정을 정적 헬퍼 `TryAssignActiveRoleBranches`로 추출한다.

- 각 브랜치는 정확히 하나의 activeRoleTag를 요구해야 한다(기존 검증 유지).
- 브랜치 역할의 홀더가 roster에 있으면 그 클라이언트에 배정한다(기존과 동일, 단일/다중 클라이언트 무관).
- 홀더가 없는 역할의 브랜치는 그래프의 `skipAbsentRoleBranches`가 true이면 `null` 배정으로 남겨 실행 단계에서 스킵하고, false이면 기존과 동일하게 오류(Panic 경로)로 처리한다.
- 홀더의 엄격 자격 검사(`IsPlayerEligibleForBranch`)는 기존 다중 클라이언트 경로와 동일하게 유지한다.

예상 영향 범위: `activeRoleTags` + `ByRole` + `skipAbsentRoleBranches=true` 조합의 그래프 중, 단일 접속자가 일부 역할만 보유한 경우에 한해 동작이 바뀐다(기존: 전부 실행 → 변경: 보유 역할만 실행). 네 역할을 모두 보유한 단일 플레이어 및 다중 클라이언트 세션은 동작이 동일하다. 현재 이 조합의 그래프는 `patient_b_c_ct`뿐이다.

### 자세한 달성 목표

- 단일 플레이어 다중 역할 시 브랜치 순차 실행은 기존 메커니즘(`ShouldAllowMultipleRoleBranches` + `routinesByClient` + `RunSequentially`)이 담당하며 본 변경으로 건드리지 않는다.
- 배정 규칙의 정합성: roster의 역할→홀더 매핑만이 배정 근거가 된다.
- 헬퍼를 순수 정적 메서드로 분리해 EditMode 테스트로 배정 규칙을 직접 고정한다.

### 문서화

- `Documents/requirements/content-definitions/scenario/patient_b_c_ct.md`에 단일 플레이어 다중 역할 처리 규칙(부여된 역할만, a→d 정의 순서로 순차)을 날짜 섹션으로 반영한다.

### 가용성과 테스트

- EditMode 테스트 추가: `ScenarioControllerParallelExecutionTests`
  - 네 역할 모두 보유한 단일 홀더 → 네 브랜치 모두 같은 클라이언트에 배정
  - 일부 역할만 보유한 단일 홀더 → 보유 역할만 배정, 미보유 역할은 null(스킵)
  - 홀더 부재 + skip 비활성 → 실패(오류 메시지에 브랜치 식별자 포함)
  - 홀더 자격 미충족 → 실패
  - activeRoleTag를 정확히 하나만 요구하지 않는 브랜치 → 실패
- 기존 병렬 실행 테스트(순차 실행 순서, WaitMode 게이트, roster 대기) 회귀 실행.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 구현체: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs`의 `TryAllocateActiveRoleParallel` / `TryAssignActiveRoleBranches`.
- 수용 기준:
  - `patient_b_c_ct`를 네 역할 태그를 모두 부여한 단일 플레이어로 실행하면 `P_B_CARE`/`P_C_CARE`가 A 체인 완료 후 B → C → D 순으로 진행된다(런타임 히스토리에 각 체인이 순차로 기록됨).
  - 일부 역할만 부여된 단일 플레이어 세션에서는 부여되지 않은 역할의 브랜치 노드가 런타임 히스토리에 나타나지 않는다.
  - 위 EditMode 테스트가 모두 통과한다.

### 링크, 참고사항

- 시나리오 정의: `Documents/requirements/content-definitions/scenario/patient_b_c_ct.md`
- 관련 선행 작업: `66d547e4 feat: support sequential single-player role branches`(클라이언트별 순차 실행), `e9ffa6b8 feat: support duplicate role branches`(중복 배정 허용)
