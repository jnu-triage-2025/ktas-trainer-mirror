# 2026-08-29 데디케이티드 서버 빌드

## 변경 목적

서버 역할을 화면과 입력 장치가 없는 독립 실행 파일로 빌드하고 실행할 수 있게 한다. 사람이 조작하는 호스트 클라이언트 없이 세션을 유지하는 것이 목표다.

## 빌드 경로

| 항목 | 내용 |
|---|---|
| 환경 변수 | `BUILD_SUBTARGET`(`Player` 기본값, `Server` 선택) |
| 산출 위치 | `build/<BuildTarget>-Server/` |
| 산출물 보존 | `KEEP_BUILD_OUTPUT=1`을 지정하면 빌드 후 삭제하지 않는다 |
| 배치 진입점 | `GitLabBuild.Build` 또는 `GitLabBuild.BuildDedicatedServer` |
| CI 작업 | `buildDedicatedServer` 매개변수로 `BuildDedicatedServer` 작업 실행 |

`GitLabBuild`는 `EditorUserBuildSettings.standaloneBuildSubtarget`과 `BuildPlayerOptions.subtarget`을 함께 설정한다. 에디터 상태에 서브타겟이 반영되어야 `UNITY_SERVER` 스크립팅 정의가 적용된 상태로 스크립트가 컴파일되기 때문이다. 빌드가 끝나면 이전 서브타겟을 복원하므로 클라이언트 빌드 경로에는 영향을 주지 않는다.

빌드에는 해당 플랫폼의 Dedicated Server Build Support 모듈이 필요하다. 모듈이 없으면 빌드가 실패하며, 실패 메시지에 필요한 모듈을 안내한다.

## 런타임 경로

`MultiplayerInfrastructure.Server`에 두 타입을 추가했다.

- `DedicatedServerOptions`: 커맨드라인 인자를 해석한다. Unity API에 의존하지 않으므로 EditMode 테스트로 직접 검증한다.
- `DedicatedServerRuntime`: `[RuntimeInitializeOnLoadMethod]`로 동작하며, 데디케이티드 모드를 판별하고 실행 정보를 런타임 레지스트리에 등록한 뒤 시작 씬으로 전환한다.

데디케이티드 모드는 서버 서브타겟 빌드(`UNITY_SERVER`)이거나 `-dedicatedServer` 인자가 주어진 경우에 활성화된다. 후자는 Dedicated Server 모듈이 없는 환경에서 일반 빌드로 서버 시작 흐름을 점검할 때 사용한다.

## 세션 시작 분기

| 대상 | 변경 내용 |
|---|---|
| `FishNetSupport.StartSession` | `startLocalClient` 매개변수 추가(기본값 `true`). 기존 호출부의 동작은 유지된다 |
| `FishNetSupport.StartDedicatedServer` | 바인딩 주소를 설정하고 로컬 클라이언트 없이 서버만 개방한다 |
| `FishNetSupport.ConfigureServerBindAddress` | `Transport.SetServerBindAddress`로 수신 인터페이스를 지정한다 |
| `IngameSceneBootstrapper` | 데디케이티드 모드에서 `NetworkSessionFailureScene` 오버레이를 요구하지 않는다 |
| `SceneUIIntroSceneController` | 데디케이티드 모드에서 IntroScene UI를 구성하지 않는다 |
| `RegistryGlobalKeys` | `IsDedicatedServer` 키 추가 |
| `DedicatedServerRuntime` | 씬이 로드될 때마다 FishNet `ServerManager.SetStartOnHeadless(false)`를 적용한다 |
| `RecognitionCheckMicrophoneInput` | 데디케이티드 모드에서는 시작 시 마이크 권한을 요청하지 않는다 |

FishNet의 `NetworkManager`는 서버 빌드에서 `Start` 시점에 서버를 자동으로 개방한다. 그 시점에는 포트와 바인딩 주소가 아직 적용되지 않았기 때문에, 데디케이티드 모드에서는 자동 개방을 차단하고 세션 부트스트랩이 전송 설정을 마친 뒤에 서버를 개방한다.

## 데이터팩

데디케이티드 서버도 클라이언트와 동일한 경로로 데이터팩을 불러온다. 내장 데이터팩(`Assets/StreamingAssets/DataPacks/`)은 실행할 때마다 런타임 폴더로 복사되고, 외장 데이터팩은 같은 폴더에서 함께 읽힌다. 둘 다 JSON의 `packId` 식별자로 활성화한다.

| 항목 | 내용 |
|---|---|
| 폴더 기본값 | 실행 파일과 같은 위치의 `DataPacks`. 에디터에서는 기존 경로(`persistentDataPath/DataPacks`)를 유지한다 |
| 폴더 지정 | `-datapacksPath <경로>`. 폴더를 만들 수 없으면 경고를 남기고 기본 경로를 사용한다 |
| 활성화 | `-datapacks identifier-a,identifier-b` |
| 활성화 기본값 | `session.config.json`의 `datapacks`(현재 `usability`, `debugging`) |
| 전체 해제 | `-noDatapacks` |

`DatapackRuntimeService`에 `DatapackRootPath`와 `TrySetDatapackRootOverride`를 추가하여 폴더 경로를 지정할 수 있게 했다. 지정하지 않으면 기존 경로를 그대로 사용하므로 클라이언트 동작은 달라지지 않는다. 선택 목록은 비어 있더라도 레지스트리에 등록한다. 등록하지 않으면 세션 설정 파일의 목록으로 대체되어 `-noDatapacks`의 의도가 사라지기 때문이다.

## 지원 인자

`-dedicatedServer`(`-server`), `-port`, `-bindAddress`(`-bind`), `-sessionName`, `-datapacks`, `-noDatapacks`, `-datapacksPath`, `-lanBroadcast`, `-noLanBroadcast`, `-targetFrameRate`(`-fps`), `-startScene`를 지원한다. 인자는 대소문자를 구분하지 않으며, `-port 37891`과 `-port=37891` 형식을 모두 허용한다. 값을 해석하지 못하면 기본값을 사용하고 경고를 기록한다. 기본값은 `Assets/StreamingAssets/Session/session.config.json`의 값을 우선 사용한다.

## 검증 결과

- `Assembly-CSharp`, `Assembly-CSharp-Editor` 컴파일 오류 0건
- `DedicatedServerOptionsTests` 14종 추가
- 인자 해석 로직 35개 항목 통과

## 후속 확인 항목

- Dedicated Server Build Support 모듈이 설치된 환경에서 실제 서버 실행 파일을 산출하고 실행 로그를 확인한다.
- 서버 전용 실행에서 클라이언트 존재를 전제하는 씬 객체가 경고를 발생시키는지 로그로 확인한다.

## 참고 문서

- [데디케이티드 서버 (헤드리스 서버)](../guide/DedicatedServer.md)
