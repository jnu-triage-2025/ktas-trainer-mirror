### 개요

Scenario 그래프가 게임플레이 행동 완료 신호를 직접 생성하지 않고도, 이미 발생한 행동 이벤트를 조건부로 다음 신호로 변환할 수 있게 한다. 이를 위해 그래프에 리스너 등록·제거 노드와 공통 조건 평가기를 추가한다.

### 해결하려는 문제 상황

시나리오의 Validator는 `RuntimeState` 신호를 기다리지만, 대상 프리팹의 상호작용 완료 이벤트를 특정 시나리오 단계에서만 관찰해 신호로 바꾸는 공통 경로가 없다. 각 기능이 개별 `Raise` 호출을 중복 구현하면 조건과 수명 관리가 불일치한다.

### 사용자 경험 목표

시나리오 작성자는 행동의 원본 이벤트, 추가 전제 조건, 발생시킬 신호와 유효 구간을 JSON 노드로 선언한다. 플레이어가 실제 행동을 완료했을 때만 후속 Validator가 열린다.

### 제안

- `SignalListener` 노드에 Register/Unregister 연산을 제공한다.
- 등록은 원본 RuntimeSignal을 관찰하고, 공통 `ScenarioSignalConditionEvaluator`가 필요한 RuntimeState 조건을 모두 확인한 경우에만 출력 신호를 Raise한다.
- 리스너는 시나리오 종료 시 제거되며, 동일 식별자 등록은 교체한다.
- Requirements Supports에는 원본·출력·선행 RuntimeSignal을 Gameplay 공급 계약으로 기록한다.

### 자세한 달성 목표

- JSON loader, schema, runtime lookup, requirements compiler가 새 노드를 지원한다.
- ScenarioController가 Register/Unregister를 실행한다.
- 원본 신호 자체를 자동으로 만들지 않으며, 실제 gameplay handler가 올린 이벤트만 변환한다.
- 조건 평가는 Validator의 RuntimeState `Contains`와 같은 정규화 기준을 사용한다.

### 문서화

- Scenario 신호 통합 가이드에 리스너 노드의 등록·해제 및 조건 예시를 추가한다.

### 가용성과 테스트

- 중복 등록, 조건 미충족, 조건 충족, 제거 후 미발생, 시나리오 종료 정리를 EditMode 테스트로 검증한다.
- 기존 JSON nodeType 역직렬화와 schema 검증이 유지돼야 한다.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 등록된 리스너는 원본 행동 신호가 발생하고 모든 선행 조건이 충족될 때만 출력 신호를 한 번 발생시킨다.
- 제거된 리스너와 종료된 시나리오는 이후 이벤트에 반응하지 않는다.
- 새 노드가 포함된 Scenario JSON이 loader와 requirements compiler를 통과한다.

### 링크, 참고사항

- `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioInteractionSignals.cs`
- `Documents/working-guide/features/scenario/interaction-signal-integration-spec.md`
