### 개요

시나리오 역할 브랜치 입력·동적 텍스트·선택 평가 기능은 다인 시나리오에서 특정 역할의 플레이어에게 대화와 선택지를 전달하고, 태그 기반 이름을 본문에 표시하며, 교육 평가 선택을 세션 로그에 남기기 위한 공용 Scenario 확장이다.

### 해결하려는 문제 상황

현재 서버 권위 `Parallel(ByRole)` 브랜치는 원격 클라이언트에 노드 도착 사실은 전달하지만 역할 범위의 `Dialogue`/`Choice`를 표시하지 않는다. 서버의 브랜치 선택 대기 상태도 원격 선택 RPC와 연결되지 않아 호스트가 아닌 역할 플레이어가 선택지를 완료할 수 없다. 또한 시나리오 문자열은 `@t=[tag, fallback]`을 해석하지 않으며, `Choice` 로그에는 선택한 답과 의도된 답/정답 여부를 함께 기록할 데이터 계약이 없다.

### 사용자 경험 목표

- 각 간호사는 자신에게 배정된 역할 브랜치의 안내와 선택지만 본다.
- 원격 플레이어의 선택이 서버 권위 브랜치 하나에만 적용된다.
- `@t=[nurse_c, ???]`와 `@t=[nurse_c, @s]`가 접속자 태그를 기준으로 표시 이름을 만든다.
- 평가 선택은 평가 식별자, 선택 답, 의도 답, 정답 여부를 세션 로그에 남긴다.

### 제안

1. 서버 `ScenarioController`가 활성 역할 브랜치 선택 프롬프트를 `(clientId, graphId, nodeId)`로 등록하고 기존 선택 RPC를 해당 프롬프트로 라우팅한다. 프롬프트 잠금은 대상 client id별로 유지하여 서로 다른 역할의 선택지는 병렬 진행하고 같은 클라이언트의 UI만 직렬화한다.
2. 표시 전용 클라이언트가 `roleScoped` Dialogue/Choice도 렌더링하고, 자동 대화 종료 또는 선택 직후 해당 노드 UI만 닫아 월드 상호작용을 복원하도록 한다. 서버 호스트는 자기 client id에 배정되지 않은 역할 대화를 로컬에 중복 표시하지 않는다.
3. `ScenarioTextResolver`를 추가해 `@s`와 재귀 가능한 `@t=[tag, fallback]`을 해석한다.
4. `ScenarioChoiceNode`에 선택적인 `assessmentIdentifier`, `correctOptionIndex`를 추가하고 선택 시 구조화 가능한 한 줄 세션 로그를 기록한다.
5. 역할 플레이어의 로컬 UI를 여는 `InvokeEvent`에는 `invokeOnRoleClient`를 제공하여 원격 배정 클라이언트에서 이벤트 핸들러를 실행한다.

### 자세한 달성 목표

- 전역 선택과 역할 브랜치 선택은 기존 동작을 유지한다.
- 역할 브랜치 프롬프트는 배정된 client id 이외의 요청을 거부한다.
- `PlayerController`가 `IScenarioIdentifiedEntity`로 세션 사용자 식별자를 노출하여 역할 도착 존의 per-entity 신호를 안정적으로 구성한다.
- 동일 태그 보유자가 여러 명이면 Identifier 정렬상 첫 플레이어를 사용해 결정성을 보장한다.
- fallback은 일반 문자열, `@s`, 중첩 `@t`를 허용한다.
- 평가 필드가 없는 기존 Choice JSON은 기존 로그만 남긴다.

### 문서화

- Scenario graph node API에 Choice 평가 필드와 텍스트 지정자를 추가한다.
- `patient_b_c_ct.md`에 이번 데이터 변환에서 채택한 런타임 계약을 기록한다.
- JSON schema와 loader/serializer를 함께 갱신한다.

### 가용성과 테스트

- 원격 입력은 client/node/graph 일치 검사를 거치며 인덱스 범위를 서버에서 검증한다.
- 텍스트 해석기는 닫히지 않은 지정자를 원문 그대로 유지한다.
- 기존 Choice/Dialogue 역직렬화 및 전역 입력 회귀를 검사하고, 서버 권위 역할 브랜치는 호스트/원격 결합 PlayMode 검증 대상으로 남긴다.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- `patient_b_c_ct`의 nurse_a/nurse_b 역할 Choice가 각 배정 클라이언트에서 선택되어 병렬 합류한다.
- nurse_c가 있으면 실제 이름, 없으면 지정한 fallback이 표시된다.
- 로그에 `assessment=... selected=... intended=... correct=...`가 기록된다.
- schema validation과 Unity 컴파일/관련 테스트가 통과한다.

### 링크, 참고사항

- `Documents/requirements/content-definitions/scenario/patient_b_c_ct.md`
- `Agents/Proposals/done/2026-06-24-scenario-parallel-execution/`
