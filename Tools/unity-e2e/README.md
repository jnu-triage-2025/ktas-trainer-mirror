# Unity 멀티플레이어 E2E

Unity Editor와 macOS/Windows 테스트 빌드를 로컬 MCP 서비스에서 제어합니다. 게임의 Legacy 입력 소비 지점은 `PlayerInput`으로 연결하고, UI Toolkit은 가상 마우스·키보드와 `InputSystemUIInputModule`을 사용합니다. 이동·다이얼로그를 성공시키기 위한 RPC 주입이나 순간이동 기능은 제공하지 않습니다.

## 실행 준비

Node.js 22.18 이상이 필요합니다. 실행 확인에는 Node.js 25.6.1 및 26.7.0을 사용했습니다.

```sh
cd Tools/unity-e2e
npm ci
cp config.example.json config.json
```

`config.json`의 빌드 실행 파일과 결과 저장 경로를 실제 환경에 맞춥니다. 상대 경로는 설정 파일이 있는 디렉터리를 기준으로 해석합니다. macOS의 실행 파일은 `.app/Contents/MacOS/KTASTrainer`입니다. 서비스는 설정에 등록된 실행 파일만 실행하며 셸 문자열을 받지 않습니다.

Unity에서 `Tools > E2E > Build macOS automation player`를 실행합니다. Windows 빌드 메뉴도 제공합니다. 빌드마다 `extraScriptingDefines`에 `UNITY_E2E`를 추가하므로 프로젝트 전체의 배포 심볼을 변경하지 않습니다. 일반 플레이어 빌드에서는 브리지·가상 입력·관측 코드가 컴파일에서 제외됩니다. 빌드 후 검사기는 컴파일된 플레이어 어셈블리를 읽어 일반 빌드에 자동화 런타임 타입·관측 메서드·입력 교체 필드·자동화 환경 변수 접근이 남으면 빌드를 실패시킵니다. 계측 빌드에서는 브리지뿐 아니라 입력·UI·이벤트·콘텐츠·fixture·지면 관측 구성요소도 확인합니다. 제거 검증용 일반 macOS 빌드는 `Tools > E2E > Build macOS ordinary player for removal verification`으로 생성할 수 있습니다. 에디터에는 같은 코드가 포함되지만 명시적으로 에디터 제어를 활성화해야 접속을 받습니다.

```sh
# 같은 값을 서비스와 MCP 프로세스에 전달합니다. 출력이나 저장소에 기록하지 않습니다.
export E2E_CONSOLE_TOKEN="$(node -e 'process.stdout.write(require("node:crypto").randomBytes(32).toString("hex"))')"
npm start -- config.json
```

서비스가 안내하는 `console-access.txt` 파일의 인증 링크를 엽니다. 링크 파일은 아티팩트 루트에 0600 권한으로 저장하며 토큰을 서비스 로그에 출력하지 않습니다. 인증 토큰은 URL fragment로 전달되며 브라우저가 즉시 주소 표시줄에서 지웁니다. 콘솔의 화면과 로그는 인증 이후에만 조회합니다. 현재 서비스는 `127.0.0.1`에만 바인딩합니다. 인증서와 공개 Origin을 설정하면 선택적으로 HTTPS 콘솔을 사용할 수 있습니다.

음성 합성을 검증하지 않는 실행은 설정 파일에 `"fixture": { "disableTts": true }`를 추가할 수 있습니다. 기존 게임 설정을 통해 TTS 모델 적재를 생략하며, 적용한 값은 각 실행의 fixture 증거에 남습니다. 기본값은 비활성화하지 않는 것이며, 이 옵션을 사용한 결과로 음성 기능이나 기본 설정의 성능을 검증했다고 판단해서는 안 됩니다.

여러 프로세스의 CPU 작업 스레드 수를 제한하려면 빌드 설정의 `args`에 `["-job-worker-count", "2"]`처럼 지정할 수 있습니다. Unity는 독립 플레이어에서도 이 옵션을 지원합니다. 설정값은 `launch.json`의 `buildArguments`에 저장합니다. [Unity 공식 문서](https://docs.unity3d.com/kr/current/ScriptReference/Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount.html)

## MCP 연결

MCP 클라이언트에서 다음 프로세스를 stdio 서버로 등록합니다. `E2E_CONSOLE_TOKEN`에는 실행 중인 서비스와 같은 값을 넣습니다.

```json
{
  "command": "node",
  "args": ["/absolute/path/Tools/unity-e2e/src/mcp.ts"],
  "env": {
    "E2E_SERVICE_URL": "http://127.0.0.1:17890",
    "E2E_CONSOLE_TOKEN": "실행 시 생성한 토큰"
  }
}
```

주요 도구는 다음과 같습니다.

| 영역 | 도구 |
|---|---|
| 에디터 | `editor.observe`, `editor.play`, `editor.stop`, `instances.attach_editor` |
| 프로세스 | `instances.list`, `instances.launch`, `instances.join`, `instances.stop` |
| 준비 작업 | `operations.status`, `operations.cancel` |
| 관측 | `game.observe`, `game.catalogue`, `game.screenshot`, `ui.query`, `events.read` |
| 입력 | `input.execute`, `input.release_all`, `ui.activate`, `ui.pointer`, `ui.text` |
| 테스트 | `scenario.validate`, `scenario.start`, `scenario.status`, `scenario.cancel`, `conditions.wait` |
| 인계·기록 | `control.acquire`, `control.handoff`, `control.emergency_stop`, `control.heartbeat`, `recording.start`, `recording.stop`, `artifacts.list`, `artifacts.read` |

`instances.launch`는 프로세스 시작만 확인합니다. 멀티플레이 준비는 이어서 `instances.join`에 반환된 실행 ID를 전달하고 `operations.status`로 확인합니다. 준비 작업은 타이틀의 플레이·호스트·직접 연결 버튼을 실제 UI 입력으로 조작합니다. 호스트와 각 클라이언트의 로컬 플레이어 생성 이후 2초 동안 프레임이 진행되는 것을 확인하고 다음 참가자를 접속시킵니다. 준비 완료 시 `actors` 매핑을 반환합니다.

입력 예시는 다음과 같습니다.

```json
{
  "instanceId": "instances.list로 확인한 식별자",
  "payload": {
    "sequence": [
      { "operation": "lookDelta", "x": 10, "y": 0 },
      { "operation": "hold", "key": "W", "durationMs": 500 }
    ]
  }
}
```

키 이름은 Unity `KeyCode` 이름을 사용합니다. `lookDelta`는 기존 Legacy Mouse X/Y 축 단위이며, 화면 픽셀이나 카메라 각도를 직접 설정하는 API가 아닙니다. 이동 축은 이 프로젝트 InputManager의 sensitivity=3, gravity=3, snap 설정을 따릅니다. 이 설정이 바뀌면 어댑터와 테스트도 함께 검토해야 합니다.

## 에디터 제어

Unity에서 `Tools > E2E > Enable editor control`을 선택하면 해당 에디터 세션에 한해 제어 채널을 엽니다. 연결 정보는 `Temp/e2e-editor-connection.json`에 저장됩니다. 설정 파일의 `editorConnectionFile`이 이 파일을 가리켜야 합니다.

1. `editor.play`로 Play Mode를 시작합니다.
2. `instances.attach_editor`로 런타임 브리지 연결을 확인합니다.
3. 일반 관측·입력 도구를 사용합니다.
4. `editor.stop`으로 종료합니다.

에디터 컴파일·도메인 리로드 시 기존 연결과 입력은 유지되지 않습니다. 테스트를 자동으로 재개하지 않습니다. 사용이 끝나면 `Tools > E2E > Disable editor control`을 선택합니다. 기본 에디터 관리 포트는 17892, 런타임 포트는 17891입니다.

## E2E 정의와 실행

콘텐츠 `.scenario.json`과 별도로 `.e2e.json`을 사용합니다. `scenario.schema.json` 검증 후 단계·참여자 참조를 확인합니다. 지원하지 않는 필드와 단계는 거부합니다.

지원 단계는 `input`, `uiText`, `uiAction`, `interact`, `wait`, `assert`, `assertEventually`, `parallel`, `barrier`, `navigate`, `dialogueAdvance`, `dialogueChoose`, `controlAcquire`, `handoff`, `checkpoint`입니다. `assertNever`는 `event.occurred`에만 사용할 수 있습니다. 구간 전체의 순서 있는 이벤트를 읽고 금지 이벤트를 판정합니다. 기록이 유실되면 `inconclusive`로 처리합니다. 화면 상태를 드문 간격으로 확인하여 금지 조건을 통과시키는 방식은 허용하지 않습니다.

`barrier`는 병렬 분기마다 서로 다른 단계 ID를 사용하고 `args.barrierId`를 공유합니다. 모든 참가자가 도달해야 통과합니다. `handoff`는 탐색 모드의 최상위 단계에서만 허용합니다. 인계 뒤에는 이후 자동 단계가 실행되지 않습니다.

`input`에 `action: "chat"`, `"inventory"`, `"quest"`, `"interact"` 등을 지정하면 현재 플레이어가 사용하는 키를 읽어 한 번 누릅니다. `sequence`와 `action`은 동시에 지정할 수 없습니다. `uiText`와 `ui.text`는 `mode: "input_adapter"`를 명시합니다. 현재 Input System 1.14에서는 가상 키보드의 문자 이벤트가 UI Toolkit 입력칸에 전달되지 않아, 포커스된 입력칸의 정상 키 편집 이벤트를 사용합니다. 입력칸의 값은 직접 대입하지 않습니다.

`navigate`는 현재 씬의 `WaypointAnchor.Identifier`를 `target`으로 지정합니다. 위치·방향을 관측하며 카메라 델타와 W 입력으로 접근합니다. 이동 잠금, 스크립트 이동, 정체, 기한 초과는 실패로 처리합니다. `args.targetType: "npc"`를 지정하면 관측된 NPC 식별자로 접근합니다. `interact`는 `target`에 상호작용 식별자를 지정하며 선택 상태를 재확인한 뒤 정상 상호작용 키를 누릅니다. 장애물을 돌아가는 경로 탐색이나 순간이동 복구는 하지 않습니다.

`dialogueChoose`의 `choiceId`는 `노드식별자#선택지인덱스`입니다. 같은 다음 노드로 향하는 선택지도 구분합니다. 기존 `NextNodeIdentifier` 참조도 목적지가 하나일 때만 허용합니다. 선택지 이동은 `Equals`/`Minus`, 확정은 `KeypadEnter` 입력이며, 대화 표시 세대가 달라졌으면 확정을 거부합니다.

`content`에 `graphId`, `dataPacks`, 선택적으로 `contentHash`를 지정하면 빌드의 콘텐츠 목록·해시와 실제 활성 데이터팩을 검사합니다. 노드·선택지 참조도 확인합니다. `game.catalogue`는 각 그래프의 서버 권위 실행 지원 여부와 미지원 노드를 제공합니다. `scenario.mode` 조건으로 `Local`, `ServerAuthoritative`, `ClientPresentation`을 구분합니다.

씬 로딩 중 2초 이상 메인 루프가 정지하면 제어권이 만료될 수 있습니다. 로딩 이후에는 `controlAcquire` 단계를 명시하여 새 제어권을 취득합니다. 입력 실패를 자동 재전송하지 않습니다. 통신 응답을 받지 못한 명령은 실제 적용 여부를 확정할 수 없으므로 `outcome: "unknown"`으로 기록합니다. 콘텐츠 목록의 최초 로딩에는 최대 30초를 허용하며 일반 명령의 기본 제한은 3초입니다.

예제는 `examples/title-settings-handoff.e2e.json`, `examples/four-player-ready.e2e.json`, `examples/dedicated-four-player-ready.e2e.json`, `examples/tutorial-movement-validator.e2e.json`에 있습니다. CLI 회귀 실행에는 AI가 필요하지 않습니다.

```sh
node src/run.ts config.json examples/four-player-ready.e2e.json mac
node src/run.ts config.json examples/dedicated-four-player-ready.e2e.json mac
node src/repeat.ts config.json examples/four-player-ready.e2e.json mac 20
```

CLI는 실행·접속·테스트·프로세스 종료를 수행합니다. `repeat.ts`는 동일 정의를 반복하고 최초 실패에서 멈춥니다. 실패를 재시도 성공으로 덮지 않습니다. `shutdown.json`에는 종료된 프로세스 상태를 저장합니다. 같은 빌드의 독립 프로세스를 사용하며, 테스트마다 UDP 게임 포트를 분리합니다. 이름, 설정 JSON, 데이터 팩 복사본, 로그는 프로필 디렉터리에 저장합니다. 960×540·30FPS 설정을 초기 프로필에 넣는 준비 작업은 `fixture-pN.json`에 `setup_bypass`로 명시합니다.

## 증거와 검증

`artifactRoot` 아래에 실행 정의, 해시, 실행·프로필 정보, 상태, 이벤트, 실패 화면과 보고서를 저장합니다. `history.sqlite`에는 명령과 인스턴스별 주기 수집 이벤트를 저장합니다. `performance.sample`에는 게임 프레임 진행·실제 작업 스레드 수·Unity 메모리 사용량·그래픽 드라이버 할당량·텍스처 메모리·적용된 밉맵 제한을 기록하고, CLI의 `resources.json`에는 시스템 부하와 러너 지연을 기록합니다. `host`에는 2초 간격의 최근 300개 표본과 전체 표본에서 관측한 최저 가용 메모리·최대 1분 부하, 프로세스 정리 직전 상태를 남깁니다. 메인 루프가 지연되면 표본 간격도 늘어날 수 있으므로 이 값은 실제 순간 최저치나 자원 부족의 확정 판정이 아닙니다. 단일 이력 내보내기가 10만 건을 넘으면 일부만 반환하지 않고 관측 누락 오류를 보고합니다. 전체 원본은 SQLite에 남습니다. 보고서에는 실행 환경, 플레이어별 이력, 단계 결과, 최초 오류와 별도 정리 오류를 포함합니다. 콘솔에서 증거 목록을 열어 JSON과 실패 화면을 확인할 수 있습니다. 기록 기능이 생성하는 자료는 원시 입력 초안이며, 검증 의도가 완성된 테스트로 자동 승인하지 않습니다.

```sh
npm run typecheck
npm test
```

Unity 입력 프레임 테스트는 `Tools > E2E > Run input adapter tests`에서 실행합니다. 결과는 `artifacts/unity-e2e/input-tests.xml`에 저장됩니다.

실제 빌드 점검용 스크립트는 `test/live-smoke.ts`, `test/live-multiplayer.ts`, `test/live-protocol.ts`, `test/live-editor-text.ts`, `test/live-content.ts`입니다. `live-content.ts`의 NPC 접근 준비에는 명시적으로 기록되는 위치 fixture가 있으며, 이것을 일반 이동 검증 결과와 혼동하면 안 됩니다. 이 스크립트는 Unity 프로세스를 실행하고 종료하므로 일반 단위 테스트와 분리했습니다.

실행 ID와 실패 기록은 [실제 실행 검증 기록](VERIFICATION.md)에 정리했습니다.

## 현재 검증 범위와 남은 작업

실제 macOS 빌드에서 설정창 진입·스크린샷·인계 및 임대 만료, 호스트와 3개 클라이언트 및 전용 서버와 4개 클라이언트의 접속·스폰, 지정 클라이언트의 카메라 회전·수평 이동을 확인했습니다. 고정 JSON 러너의 튜토리얼 이동 테스트는 문자 명령 입력, 원격 플레이어의 정상 이동, 서버 시그널 수신, Validator 후속 전이, 증거 수집과 종료 정리까지 통과했습니다. 에디터에서는 Play Mode 제어, 설정 UI, 이름 입력·프로필 저장을 확인했습니다. 일반 macOS 빌드의 브리지 제거 검사도 통과했습니다.

입력 프로토콜의 실제 빌드 테스트는 동시 입력 거부, 긴급 해제 시 매크로 취소, 잘못된 시퀀스의 부분 실행 방지, 중복 명령 재실행 방지, 이전 제어권 명령 거부, 임대 만료 후 입력 해제를 확인합니다. 각 실행의 원본 증거는 로컬 `artifacts/unity-e2e`에 있습니다.

`authoritative-dialogue-fixture.e2e.json`은 자동화 빌드 전용 합성 그래프를 실행하는 검증 정의입니다. 이 그래프는 일반 빌드에 포함되지 않으며, 콘텐츠 목록에 `syntheticFixture: true`로 표시됩니다. 일반 채팅 명령으로 시작한 뒤 실제 대화·선택지 입력과 서버 노드 전이를 검사합니다. 기존 튜토리얼 콘텐츠가 정상이라는 증거로 사용해서는 안 됩니다.

현재 게임의 `tutorial` 그래프는 `InteractionVisibility` 노드 때문에 서버 권위 실행에서 호환 경로로 전환합니다. 회귀 예제는 이 `Local` 실행 모드를 명시적으로 확인합니다. 기존 서버 권위 지원 목록에는 `Parallel`도 포함되어 있지 않으므로, 역할 분기를 검증할 때는 실행 모드를 별도로 확인해야 합니다. 자동화 플랫폼은 이 동작을 바꾸거나 숨기지 않습니다.

전체 출시 기준을 충족한 상태는 아닙니다. 역할별 Dialogue/Choice/Validator/개인 체크리스트의 종단 테스트, 핵심 경로 20회 및 60분 지속 실행, 에디터와 빌드의 혼합 토폴로지, Windows·혼합 플랫폼·UDP 장애 주입, 원격 HTTPS 게이트웨이와 높은 프레임률 화면 전송은 추가 구현·검증이 필요합니다. 화면은 선택 플레이어의 저빈도 JPEG와 나머지 플레이어의 미리보기를 제공합니다.
초기 브리지 전송은 인증된 loopback HTTP 요청·응답입니다. 명세에서 제안한 WebSocket 대신 구현했으며, 큐·기한·제어권 검사는 Unity 메인 스레드에서 수행합니다. 영상은 최종 화면 JPEG 캡처입니다. UI 입력 경로는 [Unity 런타임 이벤트 시스템 문서](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-Runtime-Event-System.html)를 기준으로 구성했습니다. MCP는 [공식 TypeScript SDK](https://github.com/modelcontextprotocol/typescript-sdk)를 사용합니다.


지속 실행 검증은 `node src/soak.ts config.json mac 60`으로 실행합니다. 같은 방을 유지하면서 약 1초 간격으로 네 플레이어의 준비 상태·고유 소유자 ID, 서버 연결 수, 메인 프레임과 서버 틱 진행을 검사합니다. 관측 실패도 실행을 실패로 남기며, 입력을 자동 재시도하지 않습니다. 관측은 60개씩 나누어 저장하고, 중단·실패 시 화면과 종료 결과를 함께 남깁니다. 이 검사는 입력 없이 방을 유지하는 검증이며, 관측 사이에 발생한 짧은 상태 변화나 60분 동안의 플레이 동작을 모두 검증하지는 않습니다. 플레이 회귀에는 `repeat.ts`를 함께 사용합니다.

역할과 체크리스트 검증에는 다음 관측을 사용할 수 있습니다. `game.observe`의 플레이어별 `inventory.available`이 `true`일 때 인벤토리 슬롯과 체크리스트 완료 항목을 읽을 수 있습니다. 원격 클라이언트에서 다른 플레이어의 인벤토리를 읽을 수 없는 경우에는 빈 인벤토리로 판정하지 않습니다. `inventory.itemCount`는 지정 플레이어의 아이템 수량을, `checklist.itemCompleted`는 실제 체크리스트 인스턴스에 기록된 완료 항목을 검사합니다. 체크리스트가 없거나 여러 장이 있어 대상이 불명확하면 완료·미완료 어느 쪽도 통과하지 않습니다. 여러 장을 구분할 때는 `slot`을 지정합니다.

`role-branches-fixture.e2e.json`은 시작 전 기존 `/tag` 명령으로 네 플레이어의 역할을 준비한 뒤, 전용 합성 그래프에서 각자의 분기만 완료하도록 검사합니다. 역할 배정은 테스트 사전조건이며 역할 선택 UI를 검증한 것으로 취급하지 않습니다. `scenario.roleAllocation`은 서버의 호환 실행 배정표를 직접 읽으며, `dialogue.node`는 대상 클라이언트의 실제 대화 노드를 읽습니다. 이 그래프는 기존 `ByRole` 호환 실행 경로를 검증하며 `ServerAuthoritative` 실행으로 표시하지 않습니다. 일반 배포 콘텐츠의 전체 역할 시나리오를 검증한 결과로 대체하지 않습니다.

`config.example.json`에는 기본 `mac`과 비교용 `mac_direct`를 함께 제공합니다. `mac_direct`는 작업 스레드 2개와 단일 스레드 렌더링을 사용합니다. M4 Mac에서 수집한 정지 스택에 Metal 버퍼 대기가 나타나 이 설정을 비교하고 있습니다. 렌더링 방식을 바꾸므로 기본 설정의 실패를 해결한 것으로 합산하지 않으며, 보고서의 `buildId`와 `launch.json`의 실제 인자를 함께 확인해야 합니다. 해당 옵션의 의미는 [Unity 실행 옵션 문서](https://docs.unity3d.com/6000.2/Documentation/Manual/PlayerCommandLineArguments.html)에 설명돼 있습니다.

바닥 아이템은 `game.observe.worldItems`에서 서버가 부여한 엔티티 식별자, 아이템 식별자, 기준 위치, 바닥 배치 상태로 관측합니다. `navigate`에 `args.targetType: "item"`과 `target` 아이템 식별자를 지정하면 그 종류의 아이템이 정확히 하나 있을 때만 접근합니다. 여러 개 있거나 이동 중 사라지면 실패합니다. 자동 획득 반경에 진입하면 아이템이 사라질 수 있으므로, 접근 완료와 획득 성공은 구분하고 `inventory.itemCount` 및 `checklist.itemCompleted`로 획득 결과를 별도로 검증해야 합니다.

개인 체크리스트의 실제 4인 획득 경로는 `node test/live-checklist.ts config.json`으로 실행합니다. `mac_direct` 빌드를 사용하며, 채팅 명령으로 역할별 종이와 초기 혈액백을 준비한 뒤 정상 Q·이동 입력으로 버리기와 획득을 수행합니다. 네 화면의 종이 완료 항목과 혈액백 수량을 각각 검사하고 실행 종료 시 소유한 프로세스를 닫습니다.

`recording.stop`은 원시 기록의 `path`와 검토용 `draftPath`를 반환합니다. 초안은 시작·종료 씬을 상태 대기로 표현하고, 확인된 UI 활성화·텍스트 입력·입력 시퀀스를 E2E 단계로 변환합니다. 키 누름/해제 쌍의 유지 시간은 외부 기록 시각으로 추정하므로 검토가 필요합니다. 좌표 클릭, 겹친 키, 2초를 넘는 유지 입력, 실행 결과 불명, 이벤트 누락은 `blockers`에 기록하며 이 경우 `definition`은 null입니다. 변환 가능한 일부 단계는 `partialSteps`로 확인할 수 있습니다. 모든 초안은 `requiresReview: true`이며 자동 실행하거나 회귀 목록에 등록하지 않습니다. 종료 관측이 실패해도 원시 기록을 보존합니다.

기록 중 `game.observe`로 확인한 씬·입력 문맥 변화도 초안에 상태 대기로 반영합니다. `input.context` 판정은 `args.context`와 관측된 입력 문맥이 정확히 같은지 검사합니다. 키를 누른 상태에서 문맥이 바뀌면 키 해제까지 묶은 입력 단계 다음에 대기를 배치합니다. `scenario.node`는 선택적인 `graphId`를 지원하며 지정된 경우 노드명·실행 측·그래프가 모두 일치해야 합니다.

NPC 관측의 `groundProbe`는 목표 위치보다 2m 위에서 아래로 최대 10m를 검사합니다. 트리거·플레이어·NPC 콜라이더를 제외한 첫 충돌면의 위치·법선·높이 차이를 기록합니다. 바닥 유무와 잘못된 높이를 진단하는 정보이며, NavMesh 도달 가능성이나 보행 가능한 경사를 보장하지 않습니다.

`src/udp-fault-proxy.ts`는 게임 UDP 장애 주입을 위한 기반 모듈입니다. 목적지와 수신 주소는 localhost로 제한하고, 첫 클라이언트의 송신 포트를 고정합니다. 지연·지터·손실·단절·UDP 페이로드 대역폭을 설정할 수 있으며, 설정은 최대 5분 후 만료됩니다. 대기 큐는 256개·1MiB·5초로 제한하고 초과 패킷을 별도로 집계합니다. `instances.launch`의 `networkProxy: true`로 실행하면 각 원격 클라이언트에 개별 프록시가 연결됩니다. 단일 인스턴스 토폴로지는 지원하지 않습니다. `network.fault`에 대상 `instanceId`, `durationMs`, `rule`을 전달하고 `network.status`로 통계를 확인합니다. 호스트와 프록시 없이 실행한 세션은 장애 적용을 거부합니다. 실제 Tugboat의 2초 단절·복구 검증은 통과했습니다. 복합 설정의 실제 게임 이동·복구 검증도 통과했습니다. 자세한 설정과 측정 범위는 `VERIFICATION.md`를 참고하세요.

장애 설정 예시는 다음과 같습니다. 모든 값이 0이고 `disconnected`가 false이면 정상 전달로 돌아갑니다. 종료 시 지연 중인 패킷은 폐기되며, 시스템의 네트워크 설정은 바꾸지 않습니다.

```json
{"instanceId":"실행한-클라이언트-ID","durationMs":2000,"rule":{"delayMs":100,"jitterMs":20,"loss":0.1,"disconnected":false,"bytesPerSecond":0}}
```

실제 Tugboat 장애 검증 스크립트는 `node test/live-network-fault.ts config.json`입니다. 프록시를 활성화한 호스트+3명 구성에서 P2의 UDP를 2초 차단하고, 차단 중 HTTP 관측·입력 유지, 만료 후 패킷 전달·위치 동기화 회복, P3/P4의 장애 격리를 검사합니다. 실패 및 복구 상태와 종료 결과를 실행 디렉터리에 저장합니다. 실제 Unity 실행 `b9fec23d-0f0c-40dd-b6c9-146bc25a1550`이 통과했으며 자세한 증거는 `VERIFICATION.md`에 기록했습니다.

역할 경계 검증 정의로 `examples/role-absent-fixture.e2e.json`과 `examples/role-multiple-tags-fixture.e2e.json`을 추가했습니다. 전자는 부재 역할을 건너뛰는 전용 그래프가 포함된 새 빌드가 필요합니다. 후자는 P3가 두 역할을 차례로 수행하도록 요구합니다. 태그 부여는 명시적인 준비 명령이며, 역할 완료는 정상 선택지 입력으로 검증합니다. 두 정의의 실제 macOS 실행이 통과했으며 실행 식별자와 검증 범위는 `VERIFICATION.md`에 기록했습니다.

관찰용 자격이 필요하면 서비스 시작 전에 별도의 32자 이상 `E2E_OBSERVER_TOKEN`을 설정합니다. 이 토큰으로 `/` 주소의 fragment에 인증 정보를 전달하면 화면·상태·로그·증거를 조회할 수 있습니다. 입력, 프로세스 관리, 시나리오 실행, 기록 생성, UDP 장애 주입은 서버에서 거부합니다. 기존 `E2E_CONSOLE_TOKEN`은 전체 조작 권한을 유지합니다. 이는 관찰과 조작의 분리이며, 준비 명령과 일반 조작을 나누는 세부 권한은 아직 포함하지 않습니다.

HTTPS 콘솔을 사용하려면 설정에 다음 `gateway` 항목을 추가합니다. 인증서와 키 경로는 설정 파일을 기준으로 해석합니다. `publicOrigin`의 포트는 서비스의 `port`와 같아야 하며, 브라우저가 신뢰하는 해당 호스트의 인증서를 사용해야 합니다. 설정하지 않으면 기존 localhost HTTP 콘솔로 실행합니다.

```json
{
  "port": 17890,
  "gateway": {
    "bind": "0.0.0.0",
    "publicOrigin": "https://e2e.example.com:17890",
    "certFile": "./certificates/fullchain.pem",
    "keyFile": "./certificates/privkey.pem"
  }
}
```

HTTPS 모드도 인증 토큰과 Host·Origin 검사를 적용합니다. Unity 제어 브리지의 localhost 연결은 유지합니다. 위 예시는 기존 설정에 합칠 항목이며, 실제 도메인·인증서로 변경해야 합니다. 자동 테스트에서는 임시 인증서를 명시적으로 신뢰하는 클라이언트를 사용하며 인증서 검증을 끄지 않습니다. 다른 PC에서 접속하는 실환경 검증은 아직 남아 있습니다. HTTPS 테스트에는 `openssl` 실행 파일이 필요합니다.

MCP의 `E2E_SERVICE_URL`에도 HTTPS 공개 Origin을 지정할 수 있습니다. 사설 CA를 사용하는 경우 MCP 자식 프로세스를 시작할 때 `NODE_EXTRA_CA_CERTS`로 신뢰할 CA 파일을 지정합니다. 인증서 검증을 비활성화하지 마세요. 원격 평문 HTTP, 사용자 정보가 포함된 URL, 경로·쿼리·fragment가 붙은 주소는 거부하며 인증 요청은 리다이렉트를 따르지 않습니다. 관찰용 MCP 연결에는 `E2E_CONSOLE_TOKEN` 값으로 서비스의 관찰용 토큰을 전달합니다.

`game.catalogue`의 `eventHandlerDiagnostics`는 InvokeEvent 노드가 참조하는 이벤트와 현재 핸들러 등록 여부를 제공합니다. 등록 상태는 현재 씬과 초기화 단계에 따라 달라집니다. 해당 씬의 서비스가 준비된 뒤 확인해야 하며, 결과는 콘텐츠 해시에 포함하지 않습니다. 조회는 핸들러를 실행하지 않습니다.

역할 담당자의 이탈 검증은 `node test/live-role-disconnect.ts config.json`으로 실행합니다. P4가 선택을 기다리는 중 프로세스를 종료한 뒤 P1~P3가 합류하는지 검사합니다. Local 호환 모드의 합성 역할 그래프를 사용하며, 재접속 검증은 포함하지 않습니다.

늦은 참가 검증은 `node test/live-late-join.ts config.json`으로 실행합니다. P4를 타이틀 화면에 남긴 상태에서 세 플레이어를 접속시키고 P2를 이동시킨 뒤, P4를 실제 UI로 접속시켜 현재 위치와 중복 없는 4인 명단을 검사합니다. 진행 중인 시나리오의 역할 재배정까지 검증하는 스크립트는 아닙니다.

인증 자격의 유효 기간은 서비스 시작 시 `E2E_CREDENTIAL_EXPIRES_AT` 환경 변수로 지정할 수 있습니다. 값은 만료 시점의 Unix 밀리초 정수이며 과거 시각이나 잘못된 값이면 서비스 시작을 거부합니다. 지정하면 운영자·관찰자 토큰이 모두 해당 시각부터 HTTP 401로 거부되며, 만료 전에 시작했더라도 요청 본문 수신이 만료 후 끝나면 실행하지 않습니다. 지정하지 않으면 기존의 만료 없는 서비스 토큰 동작을 유지합니다. 이미 시작된 테스트를 자동 취소하거나 실행별 접근 대상을 제한하는 기능은 아니며, 실행별 자격 발급과 세부 권한 분리는 추가 구현이 필요합니다.

자동화 빌드의 채팅 명령은 `fixture.allowChatCommands: true`를 명시한 실행에서만 허용됩니다. 실행 관리자가 `UNITY_E2E_ALLOW_CHAT_COMMANDS`를 직접 설정하므로 상위 셸의 값으로 기본 차단을 우회하지 않습니다. 현재 저장된 `config.json`은 기존 fixture 테스트를 위해 허용을 명시합니다. 제한된 일반 플레이 실행을 준비할 때는 이 값을 생략하거나 `false`로 지정하세요. 이 설정은 일반 채팅 메시지를 막지 않으며, 자동화가 활성화되지 않은 일반 실행의 채팅 동작도 유지합니다. 적용하려면 변경된 Unity 코드로 자동화 빌드를 다시 만들어야 합니다. 현재 정책은 실행 전체에 적용되며 사용자별 fixture 권한 구분을 대신하지 않습니다.

복합 UDP 장애는 `node test/live-network-fault.ts config.json combined`로 실행합니다. 기본 인자 생략 시 기존 2초 단절 검증을 실행합니다. 복합 모드는 6초 동안 지연·지터·손실·대역폭 제한을 설정하고, 장애 중 관측 표본과 만료 후 위치 복구 증거를 저장합니다.

프로세스 크래시 검증은 `node test/live-process-crash.ts config.json`으로 실행합니다. 테스트에서 새로 만든 P2에만 SIGKILL을 주입하고, 정상 종료와 다른 CRASHED 상태 및 종료 신호를 증거로 남깁니다. 남은 참가자와 서버 진행도 검사하며 테스트가 끝나면 살아 있는 테스트 프로세스를 정리합니다.

일반 macOS 빌드 제거 검사는 Unity 에디터에서 열지 않은 별도 작업 공간에서 다음 명령으로 CI에 연결할 수 있습니다. Unity 라이선스와 해당 버전의 macOS 빌드 지원 모듈이 설치된 macOS 실행 노드가 필요합니다. 프로젝트를 이미 연 에디터와 같은 작업 공간에서 동시에 실행하지 마세요.

```sh
"/Applications/Unity/Hub/Editor/6000.2.8f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$CI_PROJECT_DIR" \
  -executeMethod MultiplayerInfrastructure.Automation.Editor.AutomationBuild.BuildOrdinaryMac \
  -logFile "$CI_PROJECT_DIR/ordinary-build.log"
```

빌드 검사에서 잔여 코드가 발견되면 `BuildFailedException`으로 실패합니다. 이 명령의 결과 빌드는 `Build/E2E-removal-check.app`이며, CI에서는 Unity 종료 코드와 빌드 로그를 보관해야 합니다.

선택한 화면은 상태 조회와 별도로 목표 12 FPS로 갱신하며, 실제 수신·해독 속도를 화면 아래에 표시합니다. 동시에 한 장만 캡처하고 숨겨진 탭에서는 자동 캡처를 중단합니다. 비선택 화면의 썸네일은 약 1초 주기로 요청합니다. 실제 속도는 Unity 렌더링·네트워크 부하에 따라 달라집니다.

### 실행별 단기 자격 증명

관리자는 `credentials.issue`에 실행 중인 프로세스 실행 ID인 `runId`, `capabilities`, `ttlMs`를 전달한다. `ttlMs`는 최대 900000ms(15분)이며 응답의 `token`을 해당 실행용 Bearer 토큰으로 사용한다. `credentials.revoke`에 발급 응답의 `id`를 `credentialId`로 전달하면 즉시 폐기된다. 재시작하면 발급된 단기 자격은 모두 사라진다.

권한은 독립적이므로 시나리오 실행에는 `observe`와 `control`을 함께 부여한다. `fixture.allowChatCommands`가 켜진 실행에서 입력 명령이나 시나리오를 실행하려면 `fixture`도 필요하다. 이 설정에서는 UI 입력으로 채팅 명령에 접근할 수 있으므로 전체 입력 경로에 해당 제한을 적용한다. `protocol`은 현재 네트워크 장애 주입 도구를 허용하며, 일반 제어 권한만으로는 이를 실행할 수 없다.

단기 자격은 해당 실행의 인스턴스 목록, 직접 관측·제어, 시나리오 시작·상태·취소, 접속 준비·상태·취소, 기록 시작·종료, 실행과 연결된 아티팩트 조회에 사용할 수 있다. 새 인스턴스 실행, Editor 연결·Play·Stop, 자격 발급·폐기는 관리자 경로를 사용한다. `conditions.wait`는 모든 대기 대상이 허용된 실행에 속할 때 사용할 수 있으며, 자격 만료·폐기 시 대기를 중단한다.

시나리오와 접속 준비는 실행 중에도 자격 유효성을 검사하며, 만료·폐기되면 취소 및 기존 정리 절차를 수행한다. 직접 제어의 기존 임대 만료·키 해제 규칙도 유지된다. `live-credential-expiry.ts`는 실제 Unity에서 W 키 입력 중 만료, 시나리오 취소, 키 해제와 제어권 반환을 검증한다.

### 콘솔의 자연어 요청과 MCP 처리

플레이어를 선택하고 오른쪽의 “AI에게 요청”에 작업을 입력하면 요청이 대기 상태로 등록된다. 연결된 AI 클라이언트는 `assistance.list`에 프로세스 실행 `runId`를 전달해 요청을 읽는다. `assistance.update`에 `requestId`, 현재 `revision`, `state: "running"`을 전달해 먼저 요청을 확보한 뒤, 관측과 제어 도구로 작업을 수행한다. 같은 리비전을 다른 클라이언트가 이미 변경했다면 갱신은 거부된다.

AI는 실제 실행 결과와 증거를 확인한 뒤 `completed` 또는 `failed` 상태와 `result`를 보고한다. 콘솔은 이를 “AI 완료 보고”로 표시한다. 요청 등록 자체가 AI를 실행하지는 않으며, 상시 작업자나 모델 API 연결은 별도로 필요하다. 콘솔 서비스는 요청 목록을 `artifactRoot/assistance-requests.json`에 저장하고 상태별 JSON 증거를 실행 폴더에 남긴다. 재시작하면 목록을 복원하되, 대기·처리 중이던 요청은 중단 이유가 포함된 실패 상태로 바뀐다. 이전 요청을 자동 재실행하지 않으며, 이전 리비전의 완료 보고도 거부한다. 완료·실패·취소 기록은 그대로 보존한다. 저장 파일이 손상되면 덮어쓰지 않고 서비스 시작을 중단한다. 복원된 과거 실행의 요청은 `assistance.list`에 해당 실행 ID를 지정해 조회한다. 실행 ID를 생략하면 관리자·전체 관찰 자격은 모든 요청 기록을 조회하고, 실행별 자격은 허용된 실행만 조회한다. 콘솔에서도 선택한 플레이어가 없으면 과거 요청과 실행 ID를 표시한다. `scenario.start`에 해당 `requestId`를 전달하면 요청과 E2E가 연결된다. 콘솔의 요청 취소 버튼이나 `assistance.update`의 `cancelled`·`failed` 전환은 연결된 실행에도 중단을 전달한다. 실행 중인 연결 E2E가 남아 있으면 완료 보고는 `ASSISTANCE_EXECUTION_ACTIVE`로 거부된다. 요청 ID 없이 시작한 실행과 개별 MCP 제어 명령은 이 연결 범위에 포함되지 않으며 `scenario.cancel` 또는 입력 긴급 해제로 별도 중단한다.

2026년 9월 8일 실제 Chrome에서 한국어 요청을 입력하고 Codex가 MCP stdio를 통해 이를 읽어 처리했다. 설정창 표시, `RemoteHuman` 인계, 브라우저 완료 보고를 확인했다. 근거는 프로세스 실행 `2dcdbf2f-9f3f-4d2e-b7ca-3c8f320028a0`의 `natural-request-mcp-evidence.json`과 시나리오 실행 `53894667-a998-4186-a6d8-afa0e4546945`에 있다. 검증용 프로세스는 종료했다.

역할 보유자가 중복된 경우는 `examples/role-duplicate-holder-fixture.e2e.json`으로 검증한다. P1과 P2에게 각각 `e2e_a`만 부여하여 한 플레이어의 다중 역할과 구분하고, 중복 역할을 이유로 시나리오 시작을 거부하는 로그와 네 플레이어의 Gameplay 입력 문맥을 확인한다. 이 경로는 배정 요청 이전의 채팅 명령 사전 검사에서 거부된다. 일반적인 ServerAuthoritative 모드까지 입증하는 테스트는 아니다. 첫 실행에서는 배정 단계의 거부를 기대했으나 시작 사전 검사에서 먼저 거부되어 테스트가 실패했다(`32ece596-45f6-4707-bd35-abef78511294`). 해당 증거를 보존하고 실제 정책에 맞게 수정했다. 재실행 `a721fe78-4735-40fb-b23f-3d4f257d2095`은 28단계가 모두 통과했고, 네 플레이어의 시나리오 비활성 상태와 프로세스 종료를 확인했다.

```sh
node src/run.ts config.json examples/role-duplicate-holder-fixture.e2e.json mac_direct
```

### 전용 서버에서 UI와 이동 검증

`node test/live-dedicated-play.ts config.json`은 전용 서버와 클라이언트 네 개를 실행하고, 실제 타이틀 UI로 접속한 뒤 인벤토리·퀘스트 UI 격리 14단계를 실행한다. 이어 P2에게 1초간 이동 입력을 보내고 나머지 클라이언트의 위치 복제, 서버 연결 수·틱·프레임 진행, 다섯 프로세스의 종료를 확인한다.

실행 `9f31691a-f02d-4a7a-a7ba-9ebdd0417ad9`에서 통과했다. P2는 4.74m 이동했고 다른 세 클라이언트의 위치 오차는 각각 약 0.01m였다. 서버 틱은 798에서 829로 증가했으며 정리 오류가 없었다. 이는 전용 서버의 UI·이동 검증이며, 전용 서버에서 전체 다이얼로그·역할 시나리오를 검증한 결과는 아니다.

### 서버 전용 시나리오 준비

`fixture.scenario_start`는 서버가 특정 플레이어를 대상으로 시나리오를 시작하는 테스트 준비 도구다. `fixture.allowScenarioFixtures: true`를 지정해 빌드를 실행해야 하며, 기본값은 비활성화다. 자식 프로세스에는 `UNITY_E2E_ALLOW_SCENARIO_FIXTURES`를 명시적으로 설정하므로 상위 셸의 값만으로 켜지지 않는다. 실행별 자격은 같은 실행의 `control`과 `fixture` 권한을 모두 가져야 한다.

인자는 `instanceId`, `payload.graphId`, `payload.ownerId`이며, 대상 서버의 제어권을 먼저 획득해야 한다. 연결된 플레이어만 대상으로 삼을 수 있고, 진행 중인 시나리오를 덮어쓰지 않는다. 일반 클라이언트에서는 거부된다. 그래프 ID는 영문·숫자·밑줄·점·하이픈으로 제한되며 임의 명령 문자열을 받지 않는다.

이 작업은 `setup_bypass`로 기록되는 준비 단계다. 성공 응답은 시작 요청의 수락만 뜻한다. 이후 다이얼로그 진행·선택지 입력과 서버 최종 상태 관측으로 테스트 결과를 판정해야 한다. 일반 클라이언트의 관리자용 채팅 명령 권한은 변경하지 않는다.

전용 서버 다이얼로그는 `node test/live-dedicated-dialogue.ts config.json`으로 검증한다. 이 스크립트는 해당 실행에만 준비 옵션을 켜고, 서버 준비 도구가 일반 클라이언트에서는 거부되는지 확인한다. 실행 `0bb54a35-25da-4138-bb58-590f2deb32ec`에서는 서버 준비 후 P2의 실제 진행·선택 입력으로 서버 `ServerAuthoritative`와 클라이언트 `ClientPresentation`이 모두 `accepted`에 도달했다. 다섯 프로세스의 정리 오류는 없었다.

`node test/live-fixture-disabled.ts config.json`은 준비 옵션을 끈 빌드의 Unity 브리지에 직접 요청하여 런타임 자체가 `FIXTURE_DISABLED`로 거부하는지 검사한다. 실행 `d62dbdfa-a28d-49bd-9267-932c6909fe5f`에서 거부 응답, 씬·시나리오 상태 유지, 프로세스 종료를 확인했다. 서비스 API 검사만으로 런타임 차단을 추정하지 않는다.

서버 시나리오 준비 기능 추가 후 일반 macOS 빌드도 다시 생성했다. 2026년 9월 8일 빌드 후 DLL 검사에서 자동화 타입·입력 재정의·관측 메서드·자동화 환경변수 문자열 잔존 검사를 통과했다. 근거는 `artifacts/unity-e2e/90d65eec-ad05-4e40-8ae2-39a746bb168b/ordinary-removal-report.json`이며, 실제 DLL과 검사 코드의 SHA-256을 포함한다. Windows 빌드나 원격 CI 검증을 대신하는 결과는 아니다.

### Editor 호스트와 빌드 세 개

`instances.launch`의 `editor_host_plus_3_clients` 구성은 이미 연결되어 방을 연 Editor의 실행 ID와 실제 게임 포트를 사용해 클라이언트 빌드 세 개를 시작한다. `editor.play` → `instances.attach_editor` → Editor의 정상 UI로 호스트 생성 → `instances.launch` → `instances.join` 순서로 사용한다. 하나의 Editor Play 세션에서는 한 그룹만 실행할 수 있으며, 중복 그룹 실행은 거부된다. Editor 연결 해제는 프로세스 종료가 아니므로, 테스트를 끝낼 때 `editor.stop`으로 Play 모드를 별도 해제한다.

전체 검증은 `node test/live-editor-multiplayer.ts config.json`으로 실행한다. 이미 Play 중이면 기존 세션을 변경하지 않고 중단하며, 직접 시작한 Play 세션만 마지막에 해제한다. 도메인 재로드 중 Play 응답이 유실되면 명령을 다시 보내지 않고 상태를 관측해 전환 여부를 확인한다. 좁은 Game 뷰에서는 스크롤 방향이 바뀔 때 입력량을 줄여 메뉴가 화면 위아래를 왕복하는 문제를 방지한다.

실행 `68a79c9b74cb4eb98f4d46edc99bda58`에서 Editor 호스트·세 빌드의 UI 격리 14단계와 P2의 4.74m 이동을 검증했다. 위치 복제 오차는 최대 약 0.06m였고 서버 틱은 871에서 901로 증가했다. 빌드 세 개가 종료됐으며 Editor는 IntroScene의 Play 해제 상태로 돌아왔다. 전체 다이얼로그·역할 콘텐츠를 Editor 결합 구성에서 검증한 결과는 아니다.

### 콘텐츠 신호 프로토콜 송신 도구

`protocol.signal_raise`는 원격 클라이언트의 실제 `CmdRaiseScenarioSignal` RPC를 호출합니다. 호스트·전용 서버·연결되지 않은 클라이언트에서는 사용할 수 없습니다. 서버 내부 핸들러를 직접 호출하지 않습니다.

사용하려면 실행 설정에 `fixture.allowProtocolTests: true`를 명시해야 합니다. 실행 관리자는 `UNITY_E2E_ALLOW_PROTOCOL_TESTS`를 설정값에 따라 강제로 지정하며, 기본값은 비활성화입니다. 실행 범위 자격 증명에는 `control`과 `protocol` 권한이 모두 필요하고, 현재 제어권도 확보해야 합니다.

```json
{"instanceId":"<remote-client>","controlEpoch":1,"payload":{"signalId":"sig.test.signal","count":31}}
```

`signalId`는 RPC에 그대로 전달합니다. 정상 형식 검증을 통과하려면 `sig.` 접두사가 필요합니다. 잘못된 형식의 거부를 시험할 때에는 의도적으로 접두사를 생략할 수 있습니다. `count`는 1~64이며 한 번의 처리에서 연속 송신합니다. `parameterJson`은 선택 사항인 원문 문자열이므로 잘못된 JSON에 대한 서버 검증도 시험할 수 있습니다. 응답의 `sent`와 `protocol.sent` 이벤트는 송신 호출 횟수만 뜻합니다. 서버 수락 여부는 서버 상태와 로그로 별도 판정해야 합니다.

현재 서버의 선언 검사는 정확한 identifier 또는 문자열 prefix를 허용합니다. 해당 검사 함수는 역할·현재 노드·실행 세대를 별도로 검사하지 않습니다. 이 동작을 테스트 결과 없이 정상이라고 판정해서는 안 됩니다. 도구의 권한·설정·입력 제한 테스트와 C# 컴파일이 통과했습니다. 실제 RPC 검증 결과는 다음과 같습니다.


`node test/live-signal-protocol.ts config.json`으로 호스트 1개와 클라이언트 3개를 실행합니다. `db093dcb-5450-4185-a63d-315f330ae093`에서 미선언 신호·prefix 범위 밖 신호·잘못된 JSON 거부, 정확한 identifier·prefix 수락, 31회 중 30회 수락, 다른 플레이어의 독립적인 수락, 1초 경과 후 회복이 통과했습니다. 30개 수락 이벤트의 서버 시각 차이는 약 1.5ms였습니다. 결과는 `signal-protocol-evidence.json`에 서버 이벤트와 상태로 저장하며, 네 프로세스 모두 종료했고 정리 오류는 없었습니다.

같은 신호를 반복 송신하면 서버 저장 순번이 증가하고 전송 한도도 소비됩니다. 따라서 콘텐츠 신호 자체의 중복 제거를 검증한 결과가 아닙니다. 역할·현재 노드는 별도 검증이 남아 있습니다. 이전 실행의 지연 신호는 아래 실행 세대 검증에서 다룹니다.

이 검증 과정에서 준비 도구도 보완했습니다. `fixture.scenario_start`는 등록되지 않았거나 파싱에 실패한 그래프를 `FIXTURE_GRAPH_INVALID`로 거부합니다. 미등록 그래프 때문에 전체 시나리오 리소스를 동기적으로 탐색하지 않도록 등록 여부를 먼저 검사합니다. 준비 명령 응답만으로 테스트를 진행하지 않고, 서버에서 목표 그래프의 활성화 상태를 확인해야 합니다. 위 실행에서는 미등록 그래프 거부와 정상 그래프 활성화를 모두 확인했습니다.

프로토콜 도구 추가 후 직접 브리지 비활성화 검증(`f207b80d-7c24-4b53-94fc-41bae58f4773`)도 통과했습니다. 서비스 API 검사를 거치지 않은 요청이 각각 `PROTOCOL_TESTS_DISABLED`, `FIXTURE_DISABLED`로 거부됐고 장면·시나리오 상태가 유지됐습니다. 일반 macOS 빌드 제거 검사 근거는 `artifacts/unity-e2e/4a29dc4a-53d9-46dc-bf58-deb5973e7d8e/ordinary-removal-report.json`입니다. 변경 후 DLL과 소스의 SHA-256을 기록했습니다. 이는 컴파일된 일반 플레이어의 제거 검사이며 일반 플레이어 실행·Windows·원격 CI 검증을 대신하지 않습니다.


### 이전 실행의 지연 신호 차단

`node test/live-signal-execution.ts config.json`은 P3의 게임 UDP를 1초 지연한 상태에서 신호를 송신하고, P2의 실제 다이얼로그 입력으로 기존 실행을 끝낸 뒤 같은 그래프를 재시작합니다. 실행 `4dc132eb-7bc1-429b-b8fe-a7be0d83dab0`에서 이전 실행 신호가 새 실행의 서버 저장소에 기록되는 결함을 재현했습니다.

신호 RPC에 송신 당시 실행 세대를 추가했습니다. 서버 시작과 시나리오 시작·종료 때 세대를 갱신하고, 모든 관찰 클라이언트와 늦은 참가자에게 현재 세대를 전달합니다. 서버는 raise·clear의 세대가 다르면 선언 검사·저장·한도 소비 전에 거부합니다. RPC 인자가 변경됐으므로 함께 접속하는 호스트와 클라이언트는 모두 새 빌드를 사용해야 합니다.

수정 후 `a2846825-46f2-4370-b932-b67b9f56a137`에서 이전 실행 신호의 명시적 거부 로그, 새 실행 저장소에 해당 신호가 없는 상태, 지연 해제 후 새 세대 신호의 정상 수락을 확인했습니다. `signal-execution-evidence.json`에 변경 전·후 실행 ID와 UDP 큐·서버 이벤트·최종 기록을 보존했습니다. 네 프로세스 모두 종료했고 정리 오류는 없었습니다. 기존 선언·JSON·30회 제한·격리·회복 검사도 새 빌드의 `3ba4098e-b52c-47ef-b7cd-8bb84a5213a5`에서 통과했습니다. clear 전용 capability가 있는 실행은 아직 별도 실기기 검증이 필요합니다. 늦은 참가자의 신호 송신 검증 결과는 아래에 기록했습니다.

실행 세대 수정 후 일반 macOS 빌드의 자동화 코드 제거 검사도 통과했습니다. 보고서는 `artifacts/unity-e2e/4195b83a-8304-42a9-82c0-36f561904eea/ordinary-removal-report.json`입니다. 외부 서비스 테스트는 4인 실행과 동시 수행할 때 1건의 서비스 시작 시간 초과가 있었고, 플레이어 종료 후 같은 기준으로 다시 실행해 88개 모두 통과했습니다. 타입 검사와 C# 컴파일도 통과했습니다.


`node test/live-signal-late-join.ts config.json`은 세 명이 접속한 상태에서 시나리오를 시작하고 신호 파라미터를 기록한 뒤, 네 번째 플레이어를 실제 타이틀 UI로 참가시킵니다. `9481ed31-caea-4704-812f-0d2e5ed76270`에서 접속 전 RPC 송신 거부, 늦은 참가자의 기존 신호 스냅샷 수신, 현재 세대의 신규 신호 수락, 실제 발신자 귀속, 서버와 네 플레이어의 파라미터 일치를 확인했습니다. 참가 전후 서버 실행 ID는 같았고 연결 수는 3명에서 4명으로 증가했습니다. 네 프로세스 모두 종료했고 정리 오류는 없었습니다. 근거는 `signal-late-join-evidence.json`이며, 늦은 참가자의 전체 콘텐츠 표현 검증을 뜻하지는 않습니다.

### 실제 콘솔의 요청 재시작 복구 검증

기존 콘솔의 완료 요청 1건을 `732d054d-80e5-4ddf-8239-6ff86248f356/request-migration.json`에 보존한 뒤 최신 서비스로 갱신했습니다. Chrome에서 실행 인스턴스가 없어도 이전 완료 요청을 확인했습니다.

`a206c01f-a92b-4ad6-af37-e5300462611e`에서는 Chrome으로 복구 검증 요청을 등록하고 실제 MCP로 `running` 상태 전환과 테스트 플레이어 제어권 확보를 수행했습니다. 서비스를 정상 종료한 뒤 테스트 플레이어가 사라진 것을 확인했고, 재시작 후 요청이 `failed`와 중단 사유로 복원됐습니다. 이전 완료 요청은 그대로 유지됐으며, Chrome에서도 두 결과가 표시됐습니다. 늦은 완료 보고는 `ASSISTANCE_STATE_CONFLICT`로 거부됐습니다. 요청 오류는 이제 구체적인 오류 코드를 HTTP·MCP까지 유지합니다.

근거는 해당 실행의 `request-recovery-report.json`, `request-running-ui.txt`, `request-restored-ui.txt`, `mcp-late-completion-rejected.json`입니다. 복구 파일의 권한은 `0600`이며, 전체 외부 서비스 테스트 88개와 상태 충돌 코드를 검사하는 HTTP 통합 테스트가 통과했습니다. 이 결과는 정상 서비스 재시작 검증입니다. 서비스 강제 종료나 연결된 AI 작업자의 갑작스러운 이탈 복구는 별도 검증이 필요합니다.


### 요청 취소와 연결된 E2E 중단

`scenario.start`의 `requestId`는 처리 중인 요청이어야 하며, 참여자에 그 요청의 대상 플레이어가 포함되고 모두 같은 프로세스 실행에 속해야 합니다. 연결 정보는 E2E 증거의 `assistance-request.json`에 남깁니다. 취소 시 해당 요청에 연결된 실행만 중단하고, 취소된 요청의 재실행·늦은 완료 보고는 거부합니다. 취소 응답은 중단 요청을 전달했다는 뜻이며 실제 정리 완료는 E2E 상태와 제어 상태로 확인합니다.

실행 `683c99cc-0093-4613-ae67-0590b28781f0`에서 Chrome 요청 등록 → 실제 MCP 처리 시작·E2E 연결 → Chrome 요청 취소를 검증했습니다. 연결된 E2E `d14cab39-cd14-46c0-9ccc-85dd36b519d9`는 180개 계획 입력 중 42개를 기록한 상태에서 `cancelled`가 됐고, W 입력이 해제되며 제어권이 `None`으로 정리됐습니다. 정리 오류는 없었고 검증용 플레이어도 종료했습니다. `request-cancellation-report.json`에 결과를 저장했습니다.

이 과정에서 요청 목록을 매초 다시 만들면서 취소 버튼 참조가 무효화되는 문제를 발견했습니다. 요청 내용이 달라질 때만 목록을 갱신하도록 수정하고 회귀 검사를 추가했습니다. 최초 60개 입력 실행은 버튼 조작 실패 후 자연 종료했으므로 취소 성공 근거로 사용하지 않았습니다.

### 카메라 관측과 입력 격리 검증

`game.observe`의 `camera.eulerAngles`로 현재 메인 카메라 회전을, 로컬 플레이어의 `cameraPitch`로 입력 처리 후 상하 각도를 확인할 수 있습니다. 원격 플레이어의 `cameraPitch`는 복제되지 않는 값이므로 `null`입니다. 이 관측 기능은 회전값을 변경하지 않습니다.

실행 `7321d28d-c2c6-4855-9b55-a910cd0b34b2`에서는 네 명이 접속한 상태에서 P2에 `lookDelta(20, -10)`을 전달했습니다. 감도 2에서 수평 40도·상하 20도 회전했고 다른 세 플레이어의 로컬 카메라는 유지됐습니다. 입력 종료 후 각도 유지와 네 프로세스 종료도 확인했습니다. 증거는 해당 실행의 `camera-report.json`과 전후 관측 JSON에 있습니다.

Chrome의 카메라 버튼으로 포인터 잠금과 Esc 해제는 확인했습니다. 다만 OS 자동화 드래그에서는 `lookDelta`가 기록되지 않아 브라우저 상대 마우스 입력부터 Unity 회전까지의 전체 경로는 통과로 판정하지 않았습니다. OS 자동화 이벤트와 브라우저 처리 중 어느 구간의 문제인지는 추가 확인이 필요합니다.

카메라 잠금 중에는 브라우저가 받은 마우스 이벤트 수, 0이 아닌 이동을 감지한 횟수, 최근 이동량을 표시합니다. 입력 수신 대기가 유지되면 이벤트 도착 여부를, 이벤트 수만 늘면 상대 이동량이 0인지 확인할 수 있습니다. 이 표시는 브라우저 수신 상태이며 Unity가 입력을 적용했다는 증거는 아닙니다. 실제 회전 여부는 `game.observe`의 카메라 각도와 함께 확인해야 합니다. 게임 화면 이외의 포인터 잠금과 유효하지 않은 이동량은 카메라 입력에서 제외합니다. 해당 분기 검증을 포함한 서비스 테스트 91개와 타입 검사가 통과했습니다.

추가 네이티브 진단 실행 `4b253387-f9a9-475f-8102-9b62cd5bdebf`에서는 Chrome 잠금 상태에서 OS 자동화 드래그 후에도 입력 수신 대기 표시가 유지됐습니다. 게임 화면의 `mousemove` 처리기에 이벤트가 도착하지 않은 상태로 확인했으며, 브라우저 코드와 OS 자동화 전달 중 원인은 아직 확정하지 않았습니다. Esc 해제와 진단 플레이어 종료는 확인했습니다.

최신 반복 실패 `3f6efb4d-3d58-45c9-8a15-c1b92c63c578`의 P3 진단에서는 가상 마우스·키보드가 비활성화돼 있었고, 요청 좌표 `(480, 295)`와 실제 장치 좌표 `(0, 0)`가 달랐습니다. `AutomationUI`는 입력을 넣기 전에 자신이 만든 두 가상 장치를 활성화하며, 활성화에 실패하면 명령을 거부하도록 보완했습니다. 전역 포커스 정책이나 물리 장치는 변경하지 않습니다. 다른 장치의 상태 보존을 포함한 Unity 입력 테스트 12개와 직접 C# 컴파일은 통과했습니다. 수정 빌드의 실제 스크롤과 20회 반복 재검증은 아직 남아 있습니다.

가상 장치 복구 후 반복 `a1a54d96-c09e-42f8-a972-afa911c6c536`은 2회 통과 후 3회차 P4의 `STALE_CONTROL_EPOCH`로 종료됐습니다. 마지막 하트비트 성공과 다음 거부 사이가 약 2.38초였고 러너 이벤트 루프 최대 지연은 약 1.53초였습니다. 네 프로세스는 모두 정리됐습니다. 수신 이벤트를 개별 동기 커밋하던 경로는 묶음 트랜잭션으로 변경했습니다. 순서·중복 제거·저장 실패 시 전체 롤백·재개방 후 보존을 검증했고, 전체 테스트 92개와 타입 검사가 통과했습니다. 합성 로그 1,000개의 로컬 저장 비교는 약 56ms에서 13ms로 줄었지만 실제 임대 만료 원인이나 복구를 입증하지는 않습니다.

회귀 `resources.json`의 `commandHistoryWrites`는 정상 HTTP 응답을 받은 명령 기록과 수신 이벤트 묶음을 동기 저장하는 데 걸린 횟수·총 시간·최대 시간을 기록합니다. 100ms 이상 작업은 최근 32건의 명령 종류, 인스턴스, 이벤트 수, 소요 시간을 보존합니다. 네트워크 대기와 구분하는 진단이며 모든 SQLite 접근이나 OS 스케줄링 지연을 포괄하지 않습니다. 제어권 임대나 실패 판정에는 영향을 주지 않습니다.

### 저장 스레드와 제어권 갱신 분리

SQLite는 별도 Node 작업 스레드에서 접근합니다. 일반 명령은 감사 기록 저장 완료를 기다리지만 메인 이벤트 루프를 점유하지 않습니다. 하트비트는 저장 요청을 큐에 넣고 브리지 응답을 즉시 처리하므로 느린 디스크 때문에 다음 갱신이 멈추지 않습니다. 저장 오류는 유지돼 다음 명령이나 종료에서 전달되며, 일반 명령을 계속 실행해 기록 손실을 숨기지 않습니다.

큐는 최대 2,048개 요청·32MiB이며 저장 요청의 대기 한도는 30초입니다. 상한이나 대기 시간을 넘으면 오류를 전달하고 저장 스레드를 종료합니다. 기록 조회·커서 조회·정상 종료는 먼저 큐에 넣은 저장 이후에 처리됩니다. 메인 프로세스가 강제 종료된 경우 미완료 큐의 보존을 보장하지 않습니다. 조회 결과가 기존 100,000건 한도를 넘는 `OBSERVATION_GAP`은 해당 조회만 거부합니다.

`commandHistoryWrites`는 이제 정상 HTTP 응답 이후 기록 저장을 기다린 경과 시간입니다. 메인 스레드가 같은 시간 동안 멈췄다는 의미가 아닙니다. 하트비트의 이 값은 저장 완료 시간이 아니라 큐에 넣는 시간에 해당합니다. 저장 잠금 중 메인 루프 진행, 하트비트 응답의 저장 대기 분리, 종료 후 보존·중복 제거, 큐 초과·작업 스레드 종료를 포함한 전체 테스트 96개와 타입 검사가 통과했습니다. 실제 Unity 20회 반복 게이트는 아직 미통과입니다.

접속 단계에서 상태 조회가 실패하면 프로세스 스택 진단은 당시 진행 중이던 참가자 한 명에만 수행합니다. 이미 취소된 접속에서는 스택 진단을 건너뜁니다. 각 참가자의 상태·UI·화면은 계속 가능한 범위에서 수집하며, 원래 접속 실패 원인은 그대로 보존합니다. 이 제한은 최대 45초의 네이티브 진단이 참가자 수만큼 누적되는 것을 방지합니다.

### 자연어 요청 워커

`node src/worker.ts AGENT_EXECUTABLE [ARGS...]`는 콘솔 요청을 한 번에 하나씩 처리합니다. 서비스 주소와 관리자 토큰은 기존 `E2E_SERVICE_URL`, `E2E_CONSOLE_TOKEN` 환경변수를 사용합니다. 지정한 실행 파일은 셸 해석 없이 시작되며, 표준 입력으로 요청 JSON을 받습니다. 어댑터는 환경변수의 실행 범위 제한 토큰으로 MCP를 사용하고, 작업과 인계를 마친 뒤 표준 출력에 `{"result":"처리 결과"}`만 반환해야 합니다. 시나리오는 `scenario.start`의 `requestId`로 요청에 연결해야 합니다. 표준 오류 출력은 현재 보존하지 않습니다.

워커는 기본 10분의 처리 기한과 임시 자격 증명을 사용합니다. `E2E_WORKER_TIMEOUT_MS`로 1~900000ms 범위의 기한을 지정할 수 있습니다. 기본 권한은 관측·제어이며, 명시적으로 `E2E_WORKER_ALLOW_FIXTURE=1`을 설정한 경우에만 fixture 권한을 추가합니다. 프로토콜 테스트 권한은 발급하지 않습니다. 취소나 시간 초과 시 어댑터에 SIGTERM을 보내고 2초 뒤에도 종료되지 않으면 해당 자식 프로세스에 SIGKILL을 보냅니다. 완료·실패 후에는 자격 증명을 회수합니다. 어댑터가 별도로 만든 자손 프로세스 전체를 종료하는 기능은 없습니다.

워커는 외부 어댑터 실행 파일을 사용하며, 저장소에는 아래의 Codex CLI 어댑터가 포함돼 있습니다. 실제 모델과 Unity를 연결한 검증은 아직 남아 있습니다. 통합 테스트에서는 실제 워커를 SIGKILL로 종료한 뒤 서비스 API가 요청을 실패로 전환하고 살아 있는 테스트 어댑터의 자격 증명을 회수하는 것을 확인했습니다. 워커에 SIGTERM을 보낸 별도 테스트에서는 종료 신호를 무시하는 어댑터가 SIGKILL로 정리되고 요청 실패·자격 증명 회수가 수행되는 것도 확인했습니다. 이 테스트들은 Unity 플레이어를 포함하지 않습니다. 워커는 요청을 맡을 때 서비스에 처리 기한을 설정하며, 서비스는 워커의 응답 없이도 기한 만료 시 요청에 연결된 시나리오를 취소하고 해당 요청의 자격 증명을 회수합니다. 즉각적인 연결 단절 감지가 아니라 처리 기한에 따른 정리입니다. 서비스 재시작 시에는 기존 복구 규칙에 따라 진행 중 요청을 실패로 기록합니다. 단위 테스트는 중복 선점 방지, 취소 전파, 어댑터 실패 상태 기록과 자격 증명 회수를 검증합니다.

### Codex CLI 어댑터

로컬 Codex CLI에 로그인한 환경에서는 다음 명령으로 워커와 어댑터를 연결합니다.

```sh
node src/worker.ts node src/codex-agent.ts
```

`E2E_SERVICE_URL`과 `E2E_CONSOLE_TOKEN`은 워커에 설정합니다. 워커는 어댑터에 실행 범위 제한 토큰을 전달합니다. Codex 실행 파일은 `E2E_CODEX_EXECUTABLE`, 모델은 `E2E_AI_MODEL`로 지정할 수 있습니다. 모델을 지정하지 않으면 CLI 기본값을 사용합니다. 어댑터는 사용자 설정을 로드하지 않는 일회성 실행을 사용하고, 읽기 전용 샌드박스에서 셸·서브에이전트·앱 기능을 끕니다. E2E MCP 도구 중 자격 증명 관리, 요청 상태 변경, 인스턴스 관리, 에디터 조작, 프로토콜·fixture·네트워크 장애 주입은 모델에 제공하지 않습니다.

AI는 현재 게임 상태를 관측하고 요청 결과를 검증한 뒤 `success`와 `result`를 반환하도록 지시받습니다. 실패 결과는 워커의 실패 상태로 반영됩니다. 모델의 성공 보고 자체가 별도 E2E 판정의 대체물은 아닙니다. 실제 게임과 연결한 검증은 아직 남아 있습니다.

구현 근거: [Codex 비대화형 실행](https://learn.chatgpt.com/docs/non-interactive-mode), [MCP 설정](https://learn.chatgpt.com/docs/extend/mcp?surface=cli). 로컬 CLI의 `exec --help`, `features list`, `login status`로 지원 옵션과 로그인 상태를 확인했습니다.

비상 정지는 관련 자동화 작업에 취소를 전달한 뒤, 작업 정리 완료를 기다리는 경로와 입력 해제 경로를 함께 시작합니다. 입력 해제 후에도 정리 오류는 `CLEANUP_FAILED`로 보고하며 성공으로 감추지 않습니다. 입력 해제 자체가 실패하면 그 오류를 우선 반환합니다. 정리가 지연되거나 실패하는 경우에도 입력 해제를 시도하는 단위 테스트를 포함합니다.

이력 저장소 오류가 발생해도 입력 해제와 `None`으로의 제어권 반환, 이에 필요한 상태 조회는 시도합니다. 저장 실패는 반환값의 `historyErrors`에 남깁니다. 이 예외는 새 입력이나 제어권 획득에 적용되지 않으며, 일반 조작은 저장소 오류로 계속 거부됩니다. 로컬 HTTP 브리지 테스트에서 실제 해제 요청 전송과 일반 입력 차단을 확인했습니다.

`maxConcurrentInstances`는 한 서비스가 새 실행을 허용할 때 사용하는 인스턴스 한도이며 기본값은 5입니다. 기존 활성 인스턴스와 아직 생성 중인 그룹의 예약 자리를 함께 계산합니다. 한도를 넘는 그룹은 일부만 실행하지 않고 `INSTANCE_CAPACITY_EXCEEDED`로 거부합니다. 실패하거나 종료된 인스턴스는 한도에서 제외합니다. 전용 서버+클라이언트 4명은 기본 한도 안에 포함됩니다. 여러 서비스 프로세스 사이의 전역 제한이나 메모리 여유 보장은 제공하지 않습니다.

서비스 종료가 시작되면 새 그룹 실행을 거부합니다. 종료 전에 접수됐어도 프로필 준비나 네트워크 프록시 준비 중이던 요청은 프로세스를 생성하기 직전에 종료 상태를 다시 확인합니다. 이 경우 `PLATFORM_CLOSING`으로 끝나며 예약 자리를 반환합니다. 종료와 준비 중 실행을 겹치는 테스트에서 새 프로세스가 남지 않는 것을 확인했습니다.

회귀 실행의 `resources.json`에는 macOS의 `memory_pressure -Q` 결과도 기록합니다. 10초 간격으로 비동기 측정하며, 한 번에 한 요청만 실행하고 2초를 넘기면 측정 프로세스를 종료합니다. 각 표본에는 실행 시작 후 측정 시점과 소요 시간을 포함합니다. 미지원 플랫폼·측정 실패는 0%로 바꾸지 않고 `available: false`로 기록합니다. 이 값은 `freeMemoryBytes`와 다른 OS 지표이며, 실행 허용 기준이나 실패 원인의 자동 판정에 사용하지 않습니다.

각 호스트 자원 표본에는 직전 표본 이후의 `intervalMs`, 2초 주기 초과분인 `samplingDelayMs`, 해당 구간의 러너 프로세스 CPU 사용 시간도 포함합니다. 이 값으로 긴 지연이 발생한 구간을 명령 시작·완료 시각과 대조할 수 있습니다. CPU 시간은 프로세스 전체의 누적 시간 차이이므로 여러 스레드가 실행되면 벽시계 간격보다 클 수 있으며, 측정 지연 자체가 CPU 병목의 증거는 아닙니다. 최초 표본과 종료 직전 표본의 간격은 정규 2초 주기와 다를 수 있습니다.

Windows 일반 빌드 제거 검사는 `AutomationBuild.BuildOrdinaryWindows` 진입점으로 실행합니다. Windows 빌드 지원 모듈과 Unity 라이선스를 준비한 실행 노드의 별도 작업 공간에서 다음 PowerShell 명령을 사용할 수 있습니다. `$UnityEditor`는 Unity 실행 파일 경로, `$ProjectDir`는 프로젝트 절대 경로입니다.

```powershell
& $UnityEditor -batchmode -quit -projectPath $ProjectDir `
  -executeMethod MultiplayerInfrastructure.Automation.Editor.AutomationBuild.BuildOrdinaryWindows `
  -logFile "$ProjectDir/ordinary-windows-build.log"
if ($LASTEXITCODE -ne 0) { throw "Ordinary Windows build failed: $LASTEXITCODE" }
```

결과는 `Build/E2E-removal-check/KTASTrainer.exe`이며 macOS와 같은 컴파일 DLL 제거 검사기를 거칩니다. 현재 로컬 Unity에는 `MacStandaloneSupport`만 설치돼 있어 Windows 빌드·실행은 검증하지 못했습니다. `forceSingleInstance: 0` 설정은 확인했지만, Windows에서 실제 4개 프로세스를 실행하는 검증을 대신하지 않습니다.

러너는 시나리오 실패를 기록한 직후, 제어 중인 모든 참가자에게 `input.release_all`을 병렬로 보냅니다. 프로세스 샘플과 화면 등 실패 증거는 입력 해제를 시도한 뒤 수집합니다. 해제 실패는 `cleanupErrors`에 추가하며 최초 시나리오 오류를 보존합니다. 이후 명시된 정리 단계와 제어권 반환도 실행합니다. `test/live-four-failure-release.ts`는 실제 4인 접속에서 각 플레이어의 W 입력 적용을 확인하고 의도적으로 assertion을 실패시킨 뒤, 증거 수집 전에 네 입력이 모두 해제됐는지 검사합니다.

입력 시퀀스의 `wait` 연산은 `durationMs` 1~2000ms 동안 입력을 추가하지 않고 프레임을 진행합니다. 대기 중에도 요청 기한과 제어권을 검사하며, 기존 키의 만료 시각이나 제어권 임대를 연장하지 않습니다. 키 조합 기록을 순서대로 재생하기 위한 기반이며, C# 컴파일·스키마 검사와 단일 macOS 플레이어의 실제 키 조합 기록·재생 검증이 통과했습니다.

수동 기록 초안은 처음 키를 누른 때부터 모든 키를 해제할 때까지 2초 이내인 키 조합을 하나의 입력 시퀀스로 변환합니다. 각 키의 눌림·해제 순서와 그 사이의 기록 시간 차이를 보존하고, 같은 키의 유지 갱신은 추가 눌림으로 만들지 않습니다. 해제 누락, 역순 시각, 긴급 해제, 128개 연산 초과 또는 키 유지 중 UI 조작이 섞이면 실행 가능한 정의를 반환하지 않습니다. 기록 시각은 Unity 적용 프레임과 다를 수 있으므로 초안 검토는 계속 필요합니다.

키 조합 유지 중의 `lookDelta`와 `scroll` 기록도 같은 시퀀스에 넣고 시간 순서를 보존합니다. 단일 키 입력을 `hold`로 축약할 때 마우스 입력이 사라지지 않도록 처리합니다. 실제 이동 중 카메라 기록·재생은 `node test/live-recorded-chord.ts config.json four-look`으로 검증할 수 있습니다.

같은 4인 방에서 플레이 회귀를 반복하려면 `node src/soak.ts config.json mac_direct 60 host_plus_3_clients examples/four-player-active-soak.e2e.json`을 사용합니다. 정의 파일을 생략하면 기존 `idle_room`, 지정하면 `active_regression` 모드입니다. 각 회귀가 끝날 때 연결·프레임·서버 틱을 확인하고 첫 실패에서 중단하며, 회차별 결과와 최종 정리를 저장합니다. 시간은 접속 준비 후부터 계산하고 진행 중 회귀는 끝까지 판정하므로 지정 시간을 넘길 수 있습니다. 현재 예제는 인벤토리·퀘스트·서버 권위 대화 선택을 반복하며 마지막 대화까지 완료합니다. 이동이나 모든 콘텐츠 경로를 포함한 60분 플레이 검증은 아닙니다. 60분 실측 결과는 별도 검증 기록을 확인해야 합니다.

러너는 시작 시 저장소 커서를 기록하고 그 이후에 저장된 참가자별 이력만 해당 회귀의 `history-*.json`과 보고서 입력 목록에 포함합니다. 원본 SQLite 이력은 지우지 않습니다. `historyAfterCursor`가 이 범위를 나타내며, 입력 목록은 저장 순서로 정렬합니다. 같은 방의 장시간 회귀에서 과거 이력을 매번 중복 내보내는 것을 방지합니다.

Automation heartbeat는 느린 응답 하나 때문에 다음 갱신을 건너뛰지 않도록 같은 제어권 세대에서 최대 두 요청까지 진행합니다. 전송 주기는 500ms이고 각 요청 TTL은 1초입니다. 두 요청이 모두 진행 중이면 추가 전송을 건너뛰며, 소유자·세대가 바뀌거나 명시적인 제어 오류가 오면 갱신을 중단합니다. Unity 임대 시간이나 키 유지 시간을 늘리지 않습니다.

좌표 클릭의 기록 변환은 브리지가 눌림·해제 시점에 같은 이름 있는 Button을 확인한 경우에만 `uiAction` 초안을 생성합니다. 여러 패널의 겹침, 버튼 이름 중복, 드래그, 2픽셀 초과 위치 변화, 2초 초과 유지, 해제 누락은 자동 변환하지 않습니다. 버튼 식별자는 응답의 `target`과 감사 기록의 `pointerTarget`에 보존합니다. 단일 macOS 플레이어에서 플레이 버튼의 실제 좌표 클릭 기록과 생성된 UI 조작 재생이 통과했습니다. 다른 버튼·패널 구성과 브라우저 실제 클릭 경로는 별도 검증이 필요합니다.

지속 실행의 `soak-resources.json`에는 준비·회귀 실행·연결 확인·실패 증거 수집·종료 단계의 자원 표본을 남깁니다. 2초 간격으로 최근 300개 표본을 보관하며 UTC 시각, CPU 사용량, Node 메모리, 시스템 여유 메모리와 부하, 콜백 지연 및 구간별 이벤트 루프 지연을 포함합니다. macOS 메모리 압력은 별도로 측정합니다. 기록은 정상 종료나 처리 가능한 실패의 정리 단계에서 저장되므로 강제 종료 시 보존은 보장하지 않습니다. 지연된 콜백이 단계 경계를 넘을 수 있으며, 시간상 일치만으로 제어권 상실의 원인을 확정할 수 없습니다.

`ownedProcessSamples`는 이 도구가 직접 실행해 생존 중인 Unity 프로세스만 대상으로 10초 간격으로 수집합니다. `ps`의 CPU 비율과 RSS 바이트를 저장하며, 지원하지 않는 OS·조회 시간 초과·사라진 PID를 구분합니다. CPU 비율의 집계 방식은 OS에 따르며, 전체 시스템 부하나 GPU 사용량을 뜻하지 않습니다. 사용자 앱이나 별도로 열린 Unity Editor의 프로세스는 이 표본에 포함하지 않습니다.

`game.observe`의 선택적 `payload.includeStaticItems:false`는 정적 아이템 목록 수집을 생략하고 `staticPlacedItems:null`을 반환합니다. 생략 시에는 기존 전체 관측을 유지합니다. 이동 루프는 정적 아이템을 목표로 삼을 때에만 해당 목록을 매번 수집하며, 다른 상태와 이동 목표의 실시간 관측은 유지합니다.

빌드 프로필의 선택적 `frameRateLimit`은 격리된 테스트 환경의 프레임 제한을 지정합니다. 10~120의 정수만 허용하며 생략하면 기존 30fps입니다. `live-patient-entry.ts config.json patient_b_c_ct move mac_direct_15fps`처럼 다섯 번째 인자로 비교 프로필을 지정할 수 있습니다. 낮은 프레임 제한의 통과 결과는 다른 그래픽 조건의 검증을 대체하지 않습니다.

빌드 프로필의 `renderScale`은 0.25~1 범위에서 테스트의 3D 렌더 비율을 지정합니다. 기본값은 기존 0.75이며, 창과 UI 해상도는 960×540을 유지합니다.

원격 실행 범위: HTTPS 서비스 URL은 다른 위치에서 콘솔/MCP 서비스에 접속하기 위한 설정입니다. 현재 Unity 프로세스는 서비스가 실행되는 장비에서 생성되며, 여러 장비에 프로세스를 나누어 배치하는 실행 노드 관리 기능은 아직 없습니다. `attach`도 localhost 브리지 연결에 한정됩니다. 따라서 HTTPS 설정만으로 4인 플레이의 장비 부하가 분산되지는 않습니다.
