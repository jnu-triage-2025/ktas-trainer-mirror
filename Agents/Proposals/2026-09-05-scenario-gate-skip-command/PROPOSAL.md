# 운영자 게이트 건너뛰기 명령(`/scenario skip`)과 4인 세션 정지 지점 정리

- 제안일: 2026-09-05
- 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs`,
  `ScenarioNetworkRelay.cs`, `Command/CommandDefinitions/CommandDefinition.Scenario.cs`
- 관련 콘텐츠: `disaster_intro`, `patient_a_critical`, `patient_b_c_ct`
- 상태: 구현 포함(본 저장소 커밋에 구현과 EditMode 테스트를 함께 포함)

### 개요

네 명이 함께 진행하는 세션에서 시나리오가 더 이상 진행되지 않거나 실패하는 지점을 조사한 뒤,
확정된 정지 지점을 고치고 운영자가 어떤 게이트든 강제로 풀 수 있는 탈출구를 추가하는 제안입니다.

### 해결하려는 문제 상황

운영 그래프는 Parallel, InvokeEvent, QuestControl 노드를 포함하므로 서버 권위 실행 허용 목록을
통과하지 못하고 항상 호환 경로로 실행됩니다. 이 경로에서는 네 피어가 각자 자신을 owner로 하는
독립 상태기를 돌리고, 서버가 미러링하는 신호로만 서로의 진행을 반영합니다. 조사에서 확인된
정지 지점은 다음과 같습니다.

1. `disaster_intro`의 V007이 직전에 제거한 퀘스트(`collect_ns1_and_iv_set`)의 완료 키를 요구합니다.
   퀘스트를 제거하면 `quest.completed.*` 키가 레지스트리에서 지워지므로 게이트가 영원히 열리지
   않습니다. 정책이 WarnAndKeepWaiting이라 타임아웃으로도 넘어가지 않습니다.
2. `disaster_intro`의 V004, V005, V009가 기다리는 신호(`sig.move_patient_a`, `sig.enter_treatmentroom`,
   `sig.enter_triage_zone`)는 발신처가 저장소 어디에도 없습니다.
3. 환자 A 이송 이벤트(E005)의 침대 정박 대기 루프는 인스펙터 기본값이 0초라 무한 대기합니다.
   OverworldScene은 600초로 설정되어 있지만 IndevScene과 새 인스턴스는 무한 대기입니다.
4. B/C 활력 모니터 닫기 무장(arm)은 이벤트 시점에 한 번만 시도되고, 거부되면 로그 없이 반환합니다.
   앞 단계의 침대 정박 게이트가 타임아웃으로 넘어간 뒤에는 담당 간호사가 모니터를 닫아도 완료
   신호가 올라가지 않습니다.
5. 대기 중인 게이트를 강제로 넘기는 운영자 명령이 없습니다. RuntimeState 신호는
   `/scenario signal`로 올릴 수 있지만 퀘스트 완료 키와 서버 내부 신호는 올릴 수 없습니다.
   또한 서버 컨트롤러에 활성 그래프가 없으면(전용 서버, 운영자가 대상에서 빠진 세션)
   `/scenario end`와 `/scenario restart`가 피어에 전달되지 않습니다.

### 제안

1. `disaster_intro` 데이터 수정
   - V007의 퀘스트 완료 키를 `collect_ps1_and_iv_set`로 바로잡습니다.
   - V004, V005, V009의 `onWaitTimeout`을 `ForceAdvance`로 바꿔 운영 그래프와 같은 정책을 적용합니다.
   - OverworldScene의 트리아지 구역 트리거 존이 `enter_triage_zone` 신호도 올리도록 하여 V009에
     실제 발신처를 마련합니다.
2. E005 대기 타임아웃 기본값을 600초로 바꾸고 IndevScene 값도 맞춥니다.
3. 모니터 닫기 arm 거부 시 서버와 담당 클라이언트 양쪽에 사유를 남기고, 닫기 요청마다 arm을 다시
   보내 그 사이 조건이 갖춰졌으면 승인되게 합니다.
4. `ScenarioController.RequestGateSkip`을 추가합니다. 세대 번호(`_gateSkipGeneration`)를 올리면
   대기를 시작할 때 세대 번호를 기억해 둔 게이트(Validator 대기 게이트, 서버 내부 신호 대기)가
   조건과 무관하게 대기를 끝냅니다. 요청 이후에 시작한 게이트는 영향을 받지 않습니다.
5. `/scenario skip` 명령을 추가하고 `ScenarioNetworkRelay.BroadcastGateSkip`으로 모든 피어에
   전달합니다. `/scenario end`와 `/scenario restart`는 서버 컨트롤러에 그래프가 없어도 피어에
   브로드캐스트합니다.

### 자세한 달성 목표

- 메인 체인 Validator 게이트는 건너뛰기 요청을 받으면 `Advance()`로 다음 노드로 진행합니다.
- 브랜치 Validator 게이트는 대기를 끝내고 체인이 `NextIdentifier`로 이동하게 합니다.
- 서버 내부 신호 Register 대기는 타임아웃과 같은 방식으로 대기자를 지우고 진행합니다.
- 건너뛴 게이트는 콘솔 경고, 세션 로그, 방문 기록, 인게임 채팅에 미수행으로 남깁니다.
- 표시 전용 피어(ClientPresentation)는 요청을 거부하고 서버 컨트롤러가 처리합니다.

### 이번 제안에서 다루지 않는 항목

- 역할 선택 화면에서 이미 선택된 역할을 제외하는 기능은 Choice 노드에 옵션별 조건이 없어 엔진
  확장이 필요합니다. 현재는 시나리오 시작 시점의 로스터 검증이 중복 태그를 거부합니다.
- 전용 서버 구성에서 서버 전용 신호 출력(SignalCounter, EntityStateSignalBinding)이 발신되지 않는
  문제는 실행 모델 변경이 필요하므로 별도 제안으로 다룹니다.

### 가용성과 테스트

- 위험도: 낮음. 게이트 대기 조건에 세대 번호 비교를 덧붙였을 뿐이며, 요청이 없으면 기존 동작과
  같습니다.
- 추가 테스트(`Editor/Scenario/Tests/ScenarioControllerGateSkipTests.cs`):
  - 활성 그래프가 없거나 표시 전용 피어이면 요청을 거부한다.
  - 타임아웃 없는 브랜치 게이트가 요청 뒤에 풀린다.
  - 요청 이후에 시작한 게이트는 계속 기다린다.
  - 타임아웃이 남아 있는 게이트도 요청으로 즉시 풀린다.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 수용 기준:
  1. `disaster_intro`의 역할 A 흐름이 V007에서 멈추지 않는다.
  2. `/scenario skip` 한 번으로 네 피어의 대기 게이트가 모두 다음 노드로 진행한다.
  3. 서버 컨트롤러에 그래프가 없어도 `/scenario end`가 피어의 시나리오를 종료한다.
- 측정: EditMode 테스트(`MultiplayerInfrastructure.Tests.Scenario.*`) 통과와 4인 세션 수동 확인.

### 링크, 참고사항

- 조사 근거: `Documents/requirements/content-definitions/scenario/patient_a_critical.md` 1341행
  (게이트가 신호를 영원히 기다리는 조건에 대한 기술 노트)
- 관련 제안: `Agents/Proposals/done/2026-06-25-scenario-validator-gate-timeout`,
  `Agents/Proposals/done/2026-08-05-scenario-empty-active-role-roster-wait`
