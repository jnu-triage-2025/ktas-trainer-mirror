# <a id="MultiplayerInfrastructure_Logging"></a> Namespace MultiplayerInfrastructure.Logging

### Classes

 [GameLogEntry](MultiplayerInfrastructure.Logging.GameLogEntry.md)

단일 로그 엔트리.
로컬 타임 기준 타임스탬프, 컨텍스트(서버/클라이언트), 카테고리, 메시지를 포함한다.

 [GameLogService](MultiplayerInfrastructure.Logging.GameLogService.md)

인게임 로그를 수집하고 애플리케이션 데이터 폴더에 저장하는 정적 서비스.

## 동작 원칙
- 서버/클라이언트 양측에서 각자 독립적으로 동작한다.
- 각 세션(session-uuid)별로 별도 로그 파일/폴더를 생성한다.
- 메모리에 최근 엔트리를 캐시(순환 버퍼 방식)하며, 주기적으로 파일에 플러시한다.
- 로컬 시간 기준으로 타임스탬프를 기록한다.

## 저장 경로
  {Application.persistentDataPath}/GameLogs/{sessionSlug}.log

## 커맨드
  /log folder  — 로그 폴더를 파일 탐색기로 연다 (에디터/빌드, 비배치 모드 전용)
  /log export  — 현재 세션 로그를 사용자 지정 경로로 복사/내보내기
  /log list    — 저장된 세션 로그 목록을 채팅에 출력
  /log flush   — 버퍼를 강제 플러시

 [GameSessionService](MultiplayerInfrastructure.Logging.GameSessionService.md)

게임 세션 식별자(session-uuid)를 관리하는 정적 서비스.

서버/클라이언트 모두에서 독립적으로 세션 UUID를 가진다.
- 서버  : 서버 프로세스 시작 시 자동으로 UUID를 생성한다.
- 클라이언트: 서버에서 세션 ID를 부여받거나, 독립적으로 로컬 세션 ID를 생성한다.

로그 파일 이름 형식: session-{uuid} / {localtime}

 [LogService](MultiplayerInfrastructure.Logging.LogService.md)

로그 서비스의 생명주기를 관리하는 MonoBehaviour.

씬에 배치하면 FishNet 서버/클라이언트 연결 상태에 따라
<xref href="MultiplayerInfrastructure.Logging.GameLogService" data-throw-if-not-resolved="false"></xref>를 자동으로 초기화·종료한다.

## 동작
- 서버 시작(Started) → context="server"로 GameLogService 초기화
- 클라이언트 시작(Started) → context="client:{uuid}"로 GameLogService 초기화
  (단, 서버와 같은 프로세스인 호스트는 서버 컨텍스트를 우선한다)
- 서버/클라이언트 중단(Stopped) → GameLogService 종료
- OnApplicationQuit → 안전하게 Shutdown 호출

## 씬 배치
씬의 적절한 오브젝트에 컴포넌트로 추가하거나, NetworkManager 오브젝트에 함께 붙인다.

### Structs

 [GameLogService.SessionLogClearResult](MultiplayerInfrastructure.Logging.GameLogService.SessionLogClearResult.md)

### Enums

 [GameLogCategory](MultiplayerInfrastructure.Logging.GameLogCategory.md)

로그 엔트리의 카테고리.

