# ByRole 병렬 브랜치의 Any 모드 다중 역할 폴백 지원

- 제안일: 2026-08-25
- 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs`
- 관련 콘텐츠: `patient_a_critical` 시나리오(P004 의사 지시·역할별 처치)
- 상태: 구현 예시 포함(본 저장소 커밋에 예시 구현과 테스트를 함께 포함)

### 개요

ByRole 병렬 노드의 브랜치 하나가 복수의 활성 역할 태그(`activeRoleTags`)를 요구할 수 있도록
확장하는 기능입니다. 브랜치가 `requiredPlayerTagsMatchMode=Any`로 여러 역할을 선언하면,
선언된 순서대로 홀더가 연결된 첫 번째 역할의 플레이어에게 브랜치가 배정됩니다. 어느 역할의
홀더도 연결되어 있지 않으면 기존 단일 역할 브랜치와 동일한 부재 정책(`skipAbsentRoleBranches`)을
따릅니다.

### 해결하려는 문제 상황

`patient_a_critical` 줄글 시나리오는 삽관 보조 브랜치에 대해 "`nurse_b`가 배정되지 않았다면
`nurse_a`가 이 브랜치를 대신 수행한다(`requiredPlayerTagsMatchMode=Any`)"라고 명시하고, IV 라인
브랜치에 대해서도 "`nurse_d`가 배정되지 않았다면 `nurse_c`가 대신 수행한다"고 명시합니다.

현재 구현은 `TryAssignActiveRoleBranches`에서 모든 ByRole 브랜치가 정확히 하나의 활성 역할 태그만
요구하도록 강제합니다. 두 개의 역할 태그를 선언한 브랜치는 배정 단계에서 즉시 실패하고,
`TryAllocateParallel`이 false를 반환하여 시나리오 전체가 종료(EndScenario)됩니다. 즉, 요구사항이
지정한 표현 방식 그대로 그래프를 작성하면 재생이 불가능합니다. 이는 3인 이상 인원 부족 세션에서
삽관·IV 브랜치를 아예 실행할 수 없다는 문제이기도 합니다(단일 역할로 표현하면 부재 역할 분기가
스킵되어 내부 완료 신호가 영원히 오지 않는 교착이 발생).

### 사용자 경험 목표

- 4명 미만 인원으로 patient_a_critical을 진행해도, 부재한 담당 역할의 업무가 지정된 대체 역할
  플레이어에게 자동으로 넘어간다.
- 운영자는 그래프 데이터만으로 역할 폴백 규칙을 선언한다. 코드 수정 없이 우선순위(nurse_b 우선,
  없으면 nurse_a)를 데이터로 표현한다.

### 제안

1. `TryAssignActiveRoleBranches`(ScenarioController) 확장
   - 브랜치가 참조하는 활성 역할 태그가 정확히 1개면 기존 로직 유지.
   - 2개 이상인 경우:
     - `requiredPlayerTagsMatchMode != Any`이면 기존과 같이 "must require exactly one activeRoleTag"
       오류로 실패(All 모드 다중 태그는 계속 금지).
     - `Any`이면 선언 순서를 우선순위로 보고, 홀더가 연결되어 있고 브랜치 적합성 검사
       (`isHolderEligible`, 태트 게이트 바이패스 없음)를 통과하는 첫 번째 역할의 홀더에게 배정.
     - 홀더가 있는데도 적합성 검사를 통과한 역할이 없으면 오류(엄격 유지).
     - 어느 역할도 홀더가 없으면 `skipAbsentRoleBranches=true`일 때 null 배정(스킵), false일 때 오류.
2. `IsDeclaredRoleAbsent` 확장
   - 복수 역할(Any) 브랜치는 "선언된 모든 역할의 홀더가 부재"일 때에만 부재 브랜치로 판정한다.
     이 판정이 false면 실행 루프가 미배정(null) 브랜치를 서버 컨텍스트에서 임의로 실행하는
     문제를 막는다.
3. 콘텐츠 반영 예시(patient_a_critical)
   - P004 삽관 분기 Q010: `["nurse_b", "nurse_a"]`, `Any`.
   - P004 IV 분기 N011: `["nurse_d", "nurse_c"]`, `Any`.

### 자세한 달성 목표

- 한 플레이어가 여러 브랜치를 받게 되는 경우(예: nurse_a가 산소 공급과 삽관을 함께 담당) 기존
  정책대로 같은 클라이언트에 중복 배정되며, 실행 단계에서 그래프 선언 순서대로 순차 실행된다.
  (기존 `RunSequentially` 경로 재사용, 추가 변경 없음)
- 시나리오 릴레이(`PublishParallelAssignments`)의 null 배정 직렬화는 단일 역할 스킵과 동일한
  기존 경로를 사용하므로 변경이 없다.

### 문서화

- 코드 공개 API 변화 없음(모두 private/static 헬퍼). 별도 api-references 갱신 불필요.
- 그래프 작성자용 제약 조건 변화는 본 제안서와 ScenarioGraphEditor 진단 메시지로 안내한다.

### 가용성과 테스트

- 위험도: 낮음. 단일 역할 브랜치 경로는 `TryAssignSingleRoleBranch` 헬퍼로 추출했을 뿐 동작이
  동일하고, 신규 경로는 `matchMode=Any` + 다중 태그 조건에서만 활성화된다.
- 추가 테스트(MultiplayerInfrastructure/Editor/Scenario/Tests/ScenarioControllerParallelExecutionTests):
  - Any 모드에서 두 역할 모두 있을 때 첫 역할 홀더 배정
  - 첫 역할 부재 시 두 번째 역할 홀더 배정
  - 모든 역할 부재 + skip=true → null 배정(스킵)
  - 모든 역할 부재 + skip=false → 실패
  - All 모드 다중 태그는 기존과 같이 실패
- 회귀 범위: patient_b_c_ct 등 기존 그래프는 전부 단일 역할 브랜치만 사용하므로 영향 없음.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 수용 기준:
  1. `requiredPlayerTagsMatchMode=Any`와 복수 활성 역할 태그를 가진 ByRole 브랜치가 우선순위대로
     배정된다.
  2. 기존 단일 역할 브랜치 및 All 모드 동작은 기존 테스트 전부와 호환된다.
  3. patient_a_critical의 P004/P007 병렬 노드가 역할 부재 시나리오에서 EndScenario 없이 진행된다.
- 측정: 상단 테스트 추가 후 EditMode 테스트 실행 통과.

### 링크, 참고사항

- 요구사항 출처: `Documents/requirements/content-definitions/scenario/patient_a_critical.md` 421행,
  644행(역할 폴백 기술 노트)
- 관련 구현: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs`
  (`TryAssignActiveRoleBranches`, `IsDeclaredRoleAbsent`)
