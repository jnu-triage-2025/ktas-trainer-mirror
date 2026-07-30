### 개요

시나리오 상호작용 신호에 JSON 문자열 파라미터를 안전하게 부착하고, 서버가 신호 식별자와 발신 플레이어별 마지막 값을 권위적으로 보관·배포하는 기능이다. 기존의 "신호가 발생했는가"라는 sticky 게이트 의미는 유지하면서, 루브릭·운영 도구·후속 시나리오 기능이 마지막 행동의 세부 데이터를 조회할 수 있게 한다.

### 해결하려는 문제 상황

현재 `ScenarioInteractionSignals`는 문자열 식별자의 존재 여부만 저장한다. 환자, 장비, 수치, 선택 결과처럼 행동의 부가 정보를 함께 기록할 수 없으며, 다인 플레이에서 어느 플레이어가 마지막으로 어떤 값을 보고했는지도 알 수 없다. 클라이언트가 임의 문자열을 서버에 보낼 수 있는 경로이므로 JSON 문법 검증 없이 값을 도입하면 로그·도구·후속 소비자가 잘못된 데이터를 신뢰할 위험도 있다.

### 사용자 경험 목표

게임플레이 코드는 `Raise(identifier, parameterJson)`으로 JSON 값을 전달할 수 있다. 서버는 유효한 JSON 값만 수락하고, 수락한 마지막 값을 늦게 접속한 클라이언트까지 동일하게 보이게 한다. 운영자는 `/signal get`과 `/signal player`로 전체 마지막 값과 특정 플레이어의 마지막 값을 확인하고, `/signal flush`로 상태를 초기화할 수 있다.

### 제안

- `ScenarioSignalParameterStore`를 서버 권위 저장소로 추가한다. 키는 정규화된 신호 식별자와 발신 플레이어 식별자이며, 각 키에는 마지막 JSON 문자열, 발생 시각, 발신자 표시 이름을 보관한다.
- 식별자 단위 조회는 모든 플레이어 기록 가운데 가장 최근 값을 반환한다. 플레이어 지정 조회는 해당 플레이어의 마지막 값을 반환한다.
- `ScenarioInteractionSignals.Raise`와 네트워크 중계 RPC에 선택적 JSON 문자열 파라미터를 추가한다. 서버는 JSON 파싱에 성공한 값만 기록·미러링하며, 실패 시 콘솔과 발신 채팅에 `시그널 (identifier)의 매개변수 (parameter)는 올바른 JSON 형식이 아닙니다.`를 보고한다.
- 기존 파라미터 없는 신호는 호환성을 위해 계속 동작하며, 값 저장소에는 JSON `null`로 기록하지 않고 "파라미터 없음"으로 표현한다.
- 서버는 접속 시점에 현재 저장소 스냅샷을 TargetRpc로 전송한다. 이후 발생·flush는 모든 클라이언트에 미러링한다. 클라이언트의 저장소는 읽기 전용 미러다.
- `ScenarioController`의 새 시나리오 시작 경로에서 신호 파라미터 저장소를 flush한다. 현재 시나리오 그래프는 RuntimeState 신호를 시작 경계에서 이미 비우므로, 저장소도 같은 세션 경계로 맞춘다. 그래프 저장/복원 기능은 범위에 포함하지 않는다.
- `/signal get <identifier>`, `/signal player <player> <identifier>`, `/signal list [identifier]`, `/signal flush`, `/signal raise <identifier> [json]` 명령을 추가한다. 기존 `/scenario signal`은 호환 명령으로 유지한다.
- 수락·거부·flush 및 조회 결과는 세션 로그에 구조화된 문자열로 기록한다.

### 자세한 달성 목표

- JSON 문법은 객체·배열뿐 아니라 JSON 표준의 모든 최상위 값(문자열, 숫자, 불리언, null)을 허용한다.
- RPC 입력에는 기존 식별자 길이 제한과 별도 파라미터 바이트/문자 수 상한을 둔다.
- 서버는 RPC의 `NetworkConnection`으로 발신 플레이어를 판별하며, 클라이언트 제공 플레이어 이름을 신뢰하지 않는다.
- 서버 내부에서 발생한 신호는 `server` 발신자로 기록한다.
- 같은 식별자에 대한 기존 sticky RuntimeState 동작, 조건부 리스너, 카운터, Validator는 파라미터 유무와 무관하게 유지한다.

### 문서화

- 공용 API 레퍼런스에 데이터 모델, JSON 형식, 명령어, 권한·수명 규칙을 한국어로 작성한다.
- 운영 가이드에 late join 동기화와 `/signal flush`의 영향 범위를 설명한다.

### 가용성과 테스트

- 유효/무효 JSON, 식별자별 최신값, 플레이어별 최신값, flush, 파라미터 없는 하위 호환성을 EditMode 테스트로 검증한다.
- 서버 수락·클라이언트 미러·늦은 접속자 스냅샷은 네트워크 통합 검증 또는 중계기 단위 검증으로 확인한다.
- 무효 JSON은 RuntimeState·파라미터 저장소를 변경하지 않는지 확인한다.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 유효 JSON 파라미터 신호는 서버에 한 번 기록되고, 모든 관측 클라이언트와 늦은 접속자에서 동일하게 조회된다.
- 동일 식별자의 기본 조회는 전체 플레이어 중 가장 최근 신호를, 플레이어 지정 조회는 그 플레이어의 최신 신호를 반환한다.
- 무효 JSON은 정확한 오류 문구를 채팅과 로그에 남기고 저장·게이트를 발생시키지 않는다.
- 새 시나리오 시작과 `/signal flush`는 파라미터 저장소와 미러를 모두 비운다.

### 링크, 참고사항

- `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioInteractionSignals.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioNetworkRelay.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandDefinitions/CommandDefinition.Scenario.cs`
