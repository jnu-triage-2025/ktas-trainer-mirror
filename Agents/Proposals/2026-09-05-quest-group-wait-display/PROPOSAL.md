# 여러 명이 함께 완료해야 넘어가는 퀘스트의 대기 표시

- 제안일: 2026-09-05
- 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs`,
  `ScenarioNetworkRelay.cs`, `ScenarioGroupGateState.cs`, `ScenarioGroupGateTracker.cs`,
  `Quest/QuestManager.cs`, `Quest/QuestData.cs`, `Quest/QuestGroupWaitStatus.cs`,
  `UI/VisualElements/QuestPanelElement.cs`, `UI/VisualElements/QuestPreviewHudElement.cs`,
  `UI/Controllers/QuestPreviewHudUIController.cs`
- 관련 콘텐츠: `patient_a_critical`, `patient_b_c_ct` (ByRole 병렬 노드 전 구간)
- 상태: 구현 포함(본 저장소 커밋에 구현과 EditMode 테스트를 함께 포함)

### 개요

운영 시나리오는 대부분 `allocationType: ByRole`, `waitMode: All` 병렬 노드로 이루어져 있어서,
한 담당자가 자기 분기를 끝내도 나머지 담당자가 모두 끝날 때까지 다음 노드로 넘어가지 않습니다.
그런데 퀘스트 HUD 는 각 피어의 QuestManager 만 보기 때문에, 자기 몫을 끝낸 참여자는 무엇을
기다리는지 알 수 없었습니다. 이 제안은 서버가 분기 완료를 집계해 각 참여자에게 내려 주고,
퀘스트 HUD 와 저널이 그 상태를 표시하게 합니다.

### 해결하려는 문제 상황

1. 담당자가 자기 퀘스트를 끝내면 퀘스트가 회수되거나 마지막 목표가 그대로 남아 있어, 다른 참여자를
   기다리는 중인지 시나리오가 멈춘 것인지 구분할 수 없습니다.
2. 콘텐츠 작성자는 이를 우회하기 위해 `Quest_Wait_Others_Initial_PatientA` 와 같은 "대기용 퀘스트"를
   분기마다 손으로 넣고 있지만, 누가 아직 끝내지 않았는지는 표시할 수 없습니다.
3. 분기가 발행한 퀘스트를 분기 안에서 모두 회수하는 패턴(`patient_a_critical` 의 P004, P005 등)에서는
   기다리는 동안 HUD 에 아무것도 남지 않습니다.

### 제안

1. 서버 집계(`ScenarioGroupGateTracker`)
   - 병렬 노드의 배정표가 서로 다른 두 명 이상을 포함하고 `waitMode: All` 이면 공동 진행 게이트로 본다.
   - 분기 체인 컨텍스트(`BranchChainContext.GroupGateParticipant`)를 통해 분기 안의 QuestControl 노드가
     발행(Add/Update)하고 회수(Remove)한 퀘스트 식별자를 참여자별로 기록한다.
   - 분기 체인이 끝나면 참여자를 완료로 표시한다. 이탈로 취소된 분기는 완료이면서 `Left` 로 구분한다.
   - 같은 담당자가 분기를 여럿 맡으면 한 참여자로 합치고, 모든 분기가 끝나야 완료로 본다.
2. 전달(`ScenarioNetworkRelay.PublishGroupGate`, `ScenarioGroupGateState`)
   - 스냅샷(`ScenarioGroupGateSnapshot`)을 JSON 으로 직렬화해 ObserversRpc 로 표시 클라이언트에 보낸다.
     호스트는 호출부에서 로컬 상태에 먼저 반영하므로 RPC 에서 제외한다.
   - 병렬 노드가 끝나면 닫힘 스냅샷(`active: false`)을 보내 대기 표시를 내린다. 시나리오 종료와
     수동 진입 중단에서도 그래프의 게이트를 모두 닫는다.
3. 퀘스트 모델(`QuestManager`, `QuestData.GroupWait`)
   - QuestManager 는 스냅샷을 내보낼 때 로컬 참여자의 분기가 발행한 퀘스트에 `QuestGroupWaitStatus` 를
     붙인다. 게이트 스냅샷이 바뀌면 목록·추적 구독자에게 다시 알린다.
   - 로컬 참여자가 끝냈는데 분기 퀘스트가 모두 회수되어 상태를 실을 퀘스트가 없으면
     `다른 플레이어 기다리기` 자리 표시 퀘스트를 합성해 추적 목록 맨 앞에 넣는다.
4. 표시(`QuestPreviewHudElement`, `QuestPanelElement`)
   - 나는 끝냈지만 다른 참여자가 남아 있으면 HUD 카드와 저널 목록·상세의 현재 목표 줄에
     `다른 플레이어가 완료할 때까지 기다리기(n/N)` 를 표시한다.
   - 저널 상세에 "함께 완료해야 하는 참여자" 목록을 추가한다. 끝낸 참여자는
     `~~플레이어 a~~ 완료함`, 이탈한 참여자는 `~~플레이어 c~~ 이탈함`, 진행 중인 참여자는 이름만
     표시한다. 참여자 이름은 서버가 `UserDescriptorService` 로 찾은 DisplayName 이고, 없으면 역할 태그,
     그다음 클라이언트 번호를 쓴다.
   - 대기 문구의 숫자만 바뀔 때는 HUD 의 목표 달성 취소선 연출을 하지 않는다.

### 자세한 달성 목표

- n 은 자기 몫을 끝낸(이탈 포함) 참여자 수, N 은 게이트에 배정된 참여자 수다.
- 참여자 목록은 내 몫이 끝나기 전에도 게이트에 속한 퀘스트의 상세에서 볼 수 있다.
- 콘텐츠 데이터 변경은 필요 없다. 기존 "대기용 퀘스트"는 그대로 대기 문구의 운반체가 된다.
- 서버 권위 실행이 아닌 로컬 실행에서는 로컬 상태에만 반영되며, 참여자가 한 명이면 게이트가 만들어지지 않는다.

### 이번 제안에서 다루지 않는 항목

- 게이트 진행 중에 새로 접속(재접속)한 클라이언트에 현재 스냅샷을 다시 보내는 처리.
  재접속자는 새 클라이언트 식별자를 받으므로 배정과 함께 별도 복구 설계가 필요합니다.
- 호환 실행 경로(피어마다 독립 상태기)에서의 참여자 간 완료 동기화. 이 경로는 각 피어가 자기 분기만
  실행하므로 서버 집계가 성립하지 않습니다.

### 가용성과 테스트

- 위험도: 낮음. 병렬 노드 실행 순서와 게이트 판정은 바꾸지 않고, 집계와 표시만 덧붙였습니다.
  게이트가 없으면 QuestData.GroupWait 는 null 이고 기존 표시와 같습니다.
- 추가 테스트:
  - `Editor/Scenario/Tests/ScenarioGroupGateTrackerTests.cs`: 게이트 생성 조건, 퀘스트 발행/회수 기록,
    같은 담당자 분기 합산, 이탈 표시, 닫힘 스냅샷, 표시 이름 대체 규칙.
  - `Editor/Scenario/Tests/ScenarioGroupGateStateTests.cs`: 로컬 참여자 기준 대기 판정, JSON 왕복,
    닫힘 처리, 변경 알림, 로컬 클라이언트가 없을 때의 동작.
  - `Editor/Quest/QuestManagerGroupWaitTests.cs`: 스냅샷에 붙는 대기 상태, 자리 표시 퀘스트 합성과 제거,
    구독자 알림, 참여자 행 문구.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 수용 기준:
  1. 4인 세션에서 한 명이 자기 분기를 끝내면 그 화면의 HUD 카드에 `다른 플레이어가 완료할 때까지 기다리기(1/4)` 가 보인다.
  2. 저널에서 해당 임무를 열면 참여자 네 명이 나열되고, 끝낸 사람은 취소선과 "완료함"으로 표시된다.
  3. 마지막 참여자가 끝나면 대기 표시가 사라지고 다음 노드가 진행된다.
- 측정: EditMode 테스트(`MultiplayerInfrastructure.Tests.Scenario.*`, `MultiplayerInfrastructure.Tests.Quest.*`)
  통과와 4인 세션 수동 확인.

### 링크, 참고사항

- 관련 제안: `Agents/Proposals/done/2026-06-24-scenario-parallel-execution`,
  `Agents/Proposals/done/2026-06-29-quest-acquisition-flow`,
  `Agents/Proposals/2026-09-05-scenario-gate-skip-command`
