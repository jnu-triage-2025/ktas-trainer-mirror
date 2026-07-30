# MultiplayerInfrastructure.Scenario — JSON 파라미터 시그널

## 목적

`ScenarioInteractionSignals`는 기존의 완료 시그널 존재 여부를 유지하면서 JSON 문자열 파라미터를 함께 기록할 수 있다. 파라미터 값은 시그널 식별자와 발신 플레이어 조합별로 마지막 값 한 개만 보관한다.

## API

```csharp
ScenarioInteractionSignals.Raise("patient_assessed", "{\"patient\":\"patient_b\",\"level\":2}");
```

`parameterJson`은 JSON 표준의 모든 값(객체·배열·문자열·숫자·불리언·`null`)이 될 수 있다. 파라미터를 생략한 기존 `Raise("identifier")` 호출은 그대로 지원되며, 저장된 값에는 파라미터 없음으로 표시된다.

서버 코드가 발신 플레이어를 명시해야 하는 경우에는 `ScenarioNetworkRelay.RaiseAuthoritativeForPlayer`를 사용한다. 일반 게임플레이의 클라이언트 발신은 네트워크 연결 정보로 서버가 플레이어를 판별하므로 클라이언트가 플레이어 식별자를 제공하지 않는다.

## 저장·조회 규칙

- `ScenarioSignalParameterStore.TryGetLatest(identifier, out value)`는 같은 식별자 중 모든 플레이어를 통틀어 마지막에 발생한 값을 반환한다.
- `TryGetForPlayer(identifier, playerIdentifier, out value)`는 해당 플레이어의 마지막 값만 반환한다.
- `GetAll([identifier])`는 운영 화면과 도구에서 사용할 전체 목록을 반환한다.
- 저장 값은 `ScenarioSignalParameter`이며 시그널, 플레이어 UUID/표시 이름, 원본 JSON 문자열, UTC ticks, 서버 순번을 포함한다.

시나리오의 Validator·조건부 리스너는 기존처럼 시그널 존재 여부만 사용한다. JSON 비교 조건은 본 변경의 범위가 아니다.

## 서버 권위와 동기화

서버는 JSON 문법을 검사한 뒤에만 RuntimeState 시그널과 파라미터 값을 기록한다. 유효하지 않은 JSON은 기록·시그널 발생 모두 거부하며 발신 채팅과 서버 로그에 다음 메시지를 남긴다.

`시그널 (identifier)의 매개변수 (parameter)는 올바른 JSON 형식이 아닙니다.`

서버는 발생값을 모든 클라이언트에 미러링하고, 늦게 접속한 클라이언트에는 현재의 마지막 값 스냅샷을 전송한다. 클라이언트의 저장소는 서버 값의 읽기 전용 미러다. 파라미터 문자열은 최대 4096자이며 시그널 식별자는 기존처럼 최대 256자이고 제어문자를 포함할 수 없다. 자원 고갈을 막기 위해 세션 전체는 최대 512개, 플레이어별로는 최대 128개의 서로 다른 `(signal, player)` 값을 보관한다. 이미 존재하는 키의 갱신은 계속 허용된다. 한도 초과 시 파라미터 기록만 거부되며, 시나리오 진행에 필요한 완료 시그널은 계속 발생한다.

## 수명과 초기화

새 시나리오 시작 시 파라미터 저장소를 flush한다. 이는 기존 RuntimeState 완료 시그널 초기화 시점과 일치하며, 시나리오 재생 상태 저장/복원은 지원하지 않는다. `Clear(signal)`은 게이트 상태만 내리고, 감사·조회용 마지막 파라미터 값은 flush 전까지 유지한다.

운영자는 `/signal flush`로 파라미터 값만 수동 초기화할 수 있다.

## 운영 명령

| 명령 | 설명 |
| --- | --- |
| `/signal raise <identifier> [json]` | JSON 파라미터를 포함해 시그널을 발생한다. |
| `/signal get <identifier>` | 플레이어를 구분하지 않은 최신 값을 확인한다. |
| `/signal player <player> <identifier>` | 특정 표시 이름 또는 UUID의 최신 값을 확인한다. |
| `/signal list [identifier]` | 저장된 값을 최대 20개 표시한다. |
| `/signal flush` | 모든 파라미터 값을 초기화한다. |

발생·거부·조회·flush는 세션 로그에 남고, JSON 원문은 개행 위조를 막기 위해 로그에서 이스케이프되어 기록된다. 명령어 실행 자체도 기존 명령 로그에 기록된다.
