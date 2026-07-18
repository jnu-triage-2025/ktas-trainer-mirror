# Feature Proposal: 아이템 획득형 퀘스트 흐름 및 Quest UI 확장

- 작성일: 2026-06-29
- 상태: done (현재 구현 대조 완료: 2026-07-18)
- 대상 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/`, `Assets/Modules/MultiplayerInfrastructure/Scripts/Quest/`, `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/`
- 관련 콘텐츠: `Documents/requirements/content-definitions/scenario/disaster_intro.md` (`N003_1`, `V006` 구간)

### 개요

현재 시나리오에서 안내 문구(예: "생리식염수 1L 수액백과 수액세트를 각각 클릭해 획득하세요")가 Dialogue로 표시될 때,
플레이어가 실제 아이템을 클릭/획득하기 위해 필요한 입력 전환이 보장되지 않아 진행이 막히는 문제가 있다.

이번 제안은 이를 해결하기 위해 다음을 하나의 시스템 설계로 통합한다.

- Dialogue와 인게임 상호작용의 입력 우선순위 제어
- 아이템 획득형 Quest 등록/진행/완료 처리
- QuestPreview HUD 및 전용 Quest UI(패널) 동시 반영
- Quest 정의의 콘텐츠 선언화(identifier 중심)
- 진행형 Quest 진행도(`M/N`) 표현 타입 표준화

### 해결하려는 문제 상황

- 운영자/학습자는 지시 문구를 읽은 뒤 즉시 월드에서 아이템을 클릭해야 하지만, Dialogue가 닫히지 않아 상호작용이 차단된다.
- 현재 흐름은 Validator 조건(`Click_ns1`, `Click_iv_set`)에 의존하지만, 사용자 입장에서는 "왜 진행이 안 되는지"를 UI에서 파악하기 어렵다.
- Quest가 시나리오 노드에서 일회성으로만 다뤄져, QuestPreview/HUD/패널 간 동기화 및 재사용 가능한 콘텐츠 정의가 부족하다.

### 사용자 경험 목표

- 지시 Dialogue 표시 후 즉시 "획득 가능한 상태"로 입력 전환된다.
- 플레이어는 HUD에서 현재 해야 할 획득 목표와 진행도(`2/2`, `1/2`)를 즉시 확인한다.
- 동일 Quest 정보가 전용 Quest UI에서도 확인 가능하며, 완료 시 HUD/패널이 동시에 갱신된다.

### 제안

1. **Dialogue-상호작용 전환 규약 추가**
   - 시나리오에서 "월드 상호작용이 필요한 안내"는 `blocking dialogue`가 아닌 `dismissible/auto-close dialogue`로 실행한다.
   - 최소 규약:
     - `InteractionRequired` 플래그가 있는 안내는 `Duration` 만료 또는 사용자 입력으로 닫힘.
     - 닫힘 시 `PlayerController` 입력 잠금 상태를 해제하고 Crosshair/InteractHint를 복원.
   - 하위호환:
     - 기존 Dialogue 기본 동작은 유지하고, 선택적 속성으로만 확장한다.

2. **아이템 획득형 Quest 등록 모델**
   - `QuestControlNode(Add)`에서 Quest 전체 구조를 인라인 작성하는 대신, `QuestDefinitionIdentifier`를 참조한다.
   - 런타임은 식별자로 Quest 정의를 조회하여 활성 퀘스트 인스턴스를 생성한다.

3. **Quest 완료 조건 엔진(Validation 스타일) 도입**
   - 개별 Quest 완료 판별은 Scenario `ValidatorNode`의 검증 방식과 유사한 구조로 표준화한다.
   - 예시 조건 타입:
     - `InventoryContains(itemId, count)`
     - `InteractionSignalReceived(signalId, minCount)`
     - `AllOf`, `AnyOf` 조합 조건
   - 목적:
     - 시나리오 밖(자유 퀘스트/튜토리얼)에서도 동일한 완료 판별 로직 재사용

4. **진행형 Quest Progress 타입 표준화**
   - 진행형 Quest를 위해 `QuestProgressValue`(현재값/목표값/표시 포맷) 타입을 추가한다.
   - HUD/패널 표기는 기본적으로 `current/target`(예: `2/5`)로 통일한다.
   - 단일 완료형은 `1/1` 또는 완료 체크 아이콘으로 호환 표기한다.

5. **UI 반영 범위 확장**
   - `QuestPreview`(HUD 요약)와 `QuestPanel`(전용 패널) 모두 동일 ViewModel을 사용한다.
   - 필수 표시 항목:
     - 제목(title)
     - 설명(description)
     - 진행도(progress text)
     - 완료 상태(completed)
     - 추적 여부(tracked)

6. **콘텐츠 정의 체계 추가**
   - 사전 등록 데이터(예: ScriptableObject/JSON Registry)에 아래 필드를 정의한다.
     - `identifier` (고유 키)
     - `title`
     - `description`
     - `completionCriteria`
     - `isTrackable`, `isAutoComplete`, `rewards`(선택)
   - 시나리오는 identifier만 다루고, 텍스트/조건 세부사항은 콘텐츠 정의에서 로딩한다.

### 자세한 달성 목표

- `disaster_intro`의 `N003_1 -> V006` 구간에서 Dialogue가 아이템 획득 동작을 막지 않는다.
- `Quest_Collect_NS1_And_IVSet`(가칭)을 등록하면 HUD와 Quest 패널에서 동일 진행도(`0/2`, `1/2`, `2/2`)가 보인다.
- 완료 조건은 수동 분기 if-else가 아닌 공통 검증 엔진 경로로 계산된다.
- Quest 정의는 identifier 기반으로 조회되며, 시나리오 문서/JSON은 중복 텍스트 없이 참조 중심으로 단순화된다.

### 구현 스케줄(초안)

1. **Phase 1 - 설계 고정 (0.5~1일)**
   - Dialogue 전환 규약 확정(`InteractionRequired`, auto-close 정책)
   - QuestDefinition 스키마/Progress 타입 확정
   - 완료 조건 인터페이스(Validation 유사 구조) 설계

2. **Phase 2 - 런타임/데이터 계층 구현 (1.5~2일)**
   - QuestDefinition Registry 로더/조회 구현
   - Quest 인스턴스 생성을 identifier 기반으로 전환
   - Progress 업데이트 및 완료 판정 엔진 구현

3. **Phase 3 - UI 연동 (1~1.5일)**
   - QuestPreview HUD 진행도 표기 추가
   - QuestPanel 전용 UI(목록/상세 최소 사양) 구현
   - 상태 갱신 이벤트(추가/갱신/완료/제거) 연결

4. **Phase 4 - 시나리오 반영 및 검증 (0.5~1일)**
   - `disaster_intro`에 아이템 획득 퀘스트 연결
   - 입력 잠금/해제, 획득 가능 여부, 완료 반영 확인
   - 멀티플레이 동기화 및 회귀 체크

총 예상: **3.5~5.5일**

### 가용성과 테스트

- 위험: Dialogue 정책 변경으로 기존 컷신형 대화가 조기 종료될 수 있음
  - 대응: 기본값은 기존 유지, 명시 플래그가 있는 경우만 상호작용 우선 전환
- 위험: Quest 판정이 Validator와 중복 구현될 수 있음
  - 대응: 공통 조건 인터페이스/평가기로 통합하고 어댑터 계층만 분리

테스트 기준:

- 아이템 획득 단계에서 Dialogue가 닫힌 뒤 실제 클릭/획득 가능
- 퀘스트 진행도 HUD/패널 동시 갱신
- 2개 중 1개 획득 시 `1/2`, 2개 획득 시 완료 처리
- 네트워크 세션에서 호스트/클라이언트 표시 일관성 유지

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표:
  - 운영자가 "대화가 안 닫혀 진행 불가" 이슈를 재현하지 못한다.
  - 아이템 획득형 과제의 현재 상태를 HUD에서 즉시 판단할 수 있다.
- 수용 기준:
  - `disaster_intro` 해당 구간에서 사용자 조작만으로 정상 진행 가능
  - identifier 기반 Quest 등록/조회가 동작
  - 진행형 Quest에서 `M/N` 표기와 완료 동작이 일치

### 링크, 참고사항

- `Documents/requirements/content-definitions/scenario/disaster_intro.md`
- `Documents/requirements/quest/quest-manager-requirements.md`
- `Documents/requirements/ui/ui-controllers-requirements.md`
- `.gitlab/issue_templates/Feature Proposal - detailed.md`
