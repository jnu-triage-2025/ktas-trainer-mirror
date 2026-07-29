# API 레퍼런스: `TriageTrainer.SceneBootstrapper.IndevConnectionFailureOverlay`

파일 위치: `Assets/Modules/TriageTrainer/Scripts/SceneBootstrapper/IndevConnectionFailureOverlay.cs`

## 역할

모든 네트워크 세션의 FishNet 로컬 클라이언트 연결 상태를 감시하고, 접속 실패 또는 연결된 서버의 종료를 독립 씬의 UXML/USS 전체 화면 UI로 표시한다. `NetworkSessionFailureScene`은 각 세션 부트스트랩 시작 시 애디티브로 미리 로드되므로 장애 시 추가 씬 로딩이 없다.

## 주요 API

### `BeginConnectionAttempt(string address, ushort port)`

부트스트래퍼가 서버 또는 기존 서버에 연결을 시작하기 직전에 호출한다. 엔드포인트를 저장하고 이전 오류 상태를 지운다.

### `ShowConnectionError(string message)`

FishNet 세션 시작 자체가 실패한 경우 즉시 오류 화면을 표시한다.

## 상태별 표시

| 상태 | 표시 내용 |
|---|---|
| 연결 성공 후 `Stopped` | 서버가 종료되었습니다. |
| 최초 연결 실패 | 오류가 발생했습니다: 오류 메시지 또는 서버에 연결할 수 없습니다. |
| 모든 오류 | 자세한 내용은 세션 로그를 참조하세요. 및 로그 파일 경로 |

상태 전환 시 엔드포인트와 최종 Unity 오류/예외 메시지를 `GameLogService.WriteSystem`으로 기록한다.

## 자동 사용

`IndevSceneBootstrapper`, `TutorialSceneBootstrapper`, `IngameSceneBootstrapper`가 `NetworkSessionFailureScene`을 미리 로드한다. 씬의 `UIDocument`에 이 컨트롤러가 배치되어 있으므로 부트스트래퍼 오브젝트에 컴포넌트를 수동으로 추가할 필요가 없다.
