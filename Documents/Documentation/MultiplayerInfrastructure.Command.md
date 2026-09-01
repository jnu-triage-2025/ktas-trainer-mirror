# <a id="MultiplayerInfrastructure_Command"></a> Namespace MultiplayerInfrastructure.Command

### Classes

 [ChatCommandCompletionService](MultiplayerInfrastructure.Command.ChatCommandCompletionService.md)

채팅 입력창의 Tab 키 자동완성을 담당하는 서비스.

<p>
핵심 설계: Tab Cycling 시 <xref href="MultiplayerInfrastructure.Command.ChatCommandCompletionService._completionOriginalText" data-throw-if-not-resolved="false"></xref>를 보존하여
자동완성된 텍스트가 다음 검색 쿼리로 사용되는 것을 방지합니다.
</p>

 [ChatCommandHelp](MultiplayerInfrastructure.Command.ChatCommandHelp.md)

 [ChatCommandService](MultiplayerInfrastructure.Command.ChatCommandService.md)

 [CommandDefinition\_Character](MultiplayerInfrastructure.Command.CommandDefinition\_Character.md)

 [CommandDefinition\_Clean](MultiplayerInfrastructure.Command.CommandDefinition\_Clean.md)

 [CommandDefinition\_ConnGate](MultiplayerInfrastructure.Command.CommandDefinition\_ConnGate.md)

서버의 외부 접속 허용/불가(커넥션 게이트)를 전환하는 채팅 명령어.
사용법: /conngate open | /conngate close

 [CommandDefinition\_EntityPreset](MultiplayerInfrastructure.Command.CommandDefinition\_EntityPreset.md)

 [CommandDefinition\_Gamemode](MultiplayerInfrastructure.Command.CommandDefinition\_Gamemode.md)

 [CommandDefinition\_Gamerule](MultiplayerInfrastructure.Command.CommandDefinition\_Gamerule.md)

서버 공통 게임 규칙을 조회하거나 변경한다.

 [CommandDefinition\_Give](MultiplayerInfrastructure.Command.CommandDefinition\_Give.md)

 [CommandDefinition\_Help](MultiplayerInfrastructure.Command.CommandDefinition\_Help.md)

 [CommandDefinition\_Item](MultiplayerInfrastructure.Command.CommandDefinition\_Item.md)

Groups item-related commands.

 [CommandDefinition\_Kick](MultiplayerInfrastructure.Command.CommandDefinition\_Kick.md)

 [CommandDefinition\_Log](MultiplayerInfrastructure.Command.CommandDefinition\_Log.md)

/log 커맨드 - 게임 로그 파일 관리.

서브커맨드:
  /log folder          — 로그 폴더를 OS 파일 탐색기로 연다
  /log list            — 저장된 로그 파일 목록을 채팅에 출력
  /log export [path]   — 현재 세션 로그를 텍스트 파일로 내보내기
                         (path 생략 시 로그 폴더에 -export.log 접미사로 저장)
  /log flush           — 현재 로그 버퍼를 강제 플러시
  /log info            — 현재 세션/로그 파일 정보 출력

 [CommandDefinition\_Permission](MultiplayerInfrastructure.Command.CommandDefinition\_Permission.md)

/permission 커맨드 — 권한(role/user) 관리.

서브커맨드 목록:
  role list
  role info &lt;role&gt;
  role add &lt;role&gt;
  role remove &lt;role&gt;
  role default [role]
  role set &lt;role&gt; perm add &lt;permission&gt;
  role set &lt;role&gt; perm remove &lt;permission&gt;
  role set &lt;role&gt; contains add &lt;role2&gt;
  role set &lt;role&gt; contains remove &lt;role2&gt;
  user get &lt;player&gt;
  user set &lt;player&gt; &lt;role&gt;
  reset

 [CommandDefinition\_ProblemSheet](MultiplayerInfrastructure.Command.CommandDefinition\_ProblemSheet.md)

 [CommandDefinition\_Scenario](MultiplayerInfrastructure.Command.CommandDefinition\_Scenario.md)

 [CommandDefinition\_ScenarioAlias](MultiplayerInfrastructure.Command.CommandDefinition\_ScenarioAlias.md)

 [CommandDefinition\_Scoreboard](MultiplayerInfrastructure.Command.CommandDefinition\_Scoreboard.md)

 [CommandDefinition\_Server](MultiplayerInfrastructure.Command.CommandDefinition\_Server.md)

 [CommandDefinition\_ServerAlias](MultiplayerInfrastructure.Command.CommandDefinition\_ServerAlias.md)

 [CommandDefinition\_Signal](MultiplayerInfrastructure.Command.CommandDefinition\_Signal.md)

시나리오 신호의 JSON 파라미터 값을 조회·발신·초기화하는 운영 명령.

 [CommandDefinition\_Speed](MultiplayerInfrastructure.Command.CommandDefinition\_Speed.md)

 [CommandDefinition\_Tag](MultiplayerInfrastructure.Command.CommandDefinition\_Tag.md)

/tag 명령어.

/tag add [@self|target] {tag}        — 태그 추가
/tag remove {target} {tag}            — 태그 제거
/tag change {target} {from} {to}      — 태그 변경
/tag change {target} {from} {to} --force — 태그가 없어도 강제 추가
/tag show {target}                    — 태그 목록 출력

 [CommandDefinition\_TimeSync](MultiplayerInfrastructure.Command.CommandDefinition\_TimeSync.md)

시간 표시(스톱워치/카운트다운)의 주기적 재동기화 밀도를 조정하는 명령어.

밀도는 tick / ms / seconds 단위로 지정하며, 기본값은 1초에 1회이다.
이 설정은 서버 권위이므로 서버(호스트/콘솔)에서만 조정할 수 있다.
(모든 명령 실행은 서버에서 수행되며, PermissionService 권한으로 접근을 제한한다.)

 [CommandDefinition\_Title](MultiplayerInfrastructure.Command.CommandDefinition\_Title.md)

 [CommandDefinition\_Tp](MultiplayerInfrastructure.Command.CommandDefinition\_Tp.md)

/tp 커맨드 — 순간이동.

지원하는 형태:
  /tp x y z                   — 자기 자신을 (x, y, z) 로 이동
  /tp ~x ~y ~z                — 자기 자신의 현재 위치를 기준으로 이동
  /tp &lt;player&gt; x y z          — player 를 (x, y, z) 로 이동
  /tp &lt;player&gt;                — 자기 자신을 player 위치로 이동
  /tp &lt;player1&gt; &lt;player2&gt;      — player1 을 player2 위치로 이동
  /tp &lt;waypoint&gt;              — 자기 자신을 waypoint 위치로 이동
  /tp &lt;player&gt; &lt;waypoint&gt;     — player 를 waypoint 위치로 이동

 [TargetSelectorResolver](MultiplayerInfrastructure.Command.TargetSelectorResolver.md)

### Structs

 [UsageLine](MultiplayerInfrastructure.Command.UsageLine.md)

A single usage row: a syntax fragment (subcommand + arguments) on the
left and its explanation on the right. Rendered as an aligned two-column
row under the command name.

### Interfaces

 [IChatCommandCompletion](MultiplayerInfrastructure.Command.IChatCommandCompletion.md)

명령어가 Tab 키 자동완성 후보를 직접 제공할 수 있게 하는 선택적 인터페이스.

<p>
구현하지 않은 명령어도 Usage 정의에 기반한 하위 명령어·일반 인수
자동완성을 지원합니다. 이 인터페이스는 런타임 데이터나 복잡한
맥락처럼 Usage만으로 표현할 수 없는 후보를 제공할 때 사용합니다.
</p>

 [IChatCommandModel](MultiplayerInfrastructure.Command.IChatCommandModel.md)

 [IChatCommandPipelineCommand](MultiplayerInfrastructure.Command.IChatCommandPipelineCommand.md)

 [IChatCommandUsage](MultiplayerInfrastructure.Command.IChatCommandUsage.md)

Optional interface for commands that expose structured, multi-line usage.
Rendered as:
  /command
      syntax1     description1
      syntax2     description2
When not implemented, help falls back to <xref href="MultiplayerInfrastructure.Command.IChatCommandModel.Description" data-throw-if-not-resolved="false"></xref>.

