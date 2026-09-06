# Feature Proposal - Dedicated Server Build

> 상태: **구현 완료(2026-08-29), 검토 대기**. 빌드 스크립트(`GitLabBuild`), CI 스크립트, 런타임 부트스트랩(`DedicatedServerRuntime`), EditMode 테스트, 문서까지 이 제안서의 범위에 포함되어 있습니다.

## 개요

데디케이티드 서버 빌드 기능은 KTAS Trainer의 서버 역할을 화면과 입력 장치가 없는 독립 실행 파일로 배포하기 위한 구현입니다. 이 기능은 Unity의 Dedicated Server 서브타겟(`StandaloneBuildSubtarget.Server`)으로 플레이어를 빌드하고, 실행 시점에 커맨드라인 인자로 세션을 구성하는 방식을 통해, 진행자가 클라이언트를 직접 조작하지 않아도 훈련 세션을 상시 유지할 수 있게 합니다. 실습실 서버 컴퓨터에 서버만 설치해 두고 학습자 컴퓨터에서는 클라이언트만 접속하는 형태로 활용하는 것이 의도되었습니다.

- 빌드: `BUILD_SUBTARGET=Server` 환경 변수 하나로 기존 빌드 경로를 그대로 재사용하여 헤드리스 서버를 산출합니다.
- 실행: `UNITY_SERVER` 정의 또는 `-dedicatedServer` 인자로 데디케이티드 모드를 감지하고, IntroScene의 UI 흐름을 건너뛰어 곧바로 서버를 개방합니다.
- 세션 구성: 포트, 바인딩 주소, 세션 이름, 데이터팩, LAN 브로드캐스트 여부를 커맨드라인으로 지정합니다.
- 기술적 제약: 렌더링과 오디오, UI 입력에 의존하는 코드가 서버에서 실행되지 않도록 시작 경로를 분리해야 합니다.

## 해결하려는 문제 상황

나는 훈련 세션을 운영하는 진행자로서, 사람이 조작하는 호스트 클라이언트 없이도 서버가 유지되기를 원한다. 왜냐하면 현재 구조에서는 호스트가 곧 하나의 클라이언트이기 때문에, 호스트 담당자가 게임을 종료하거나 컴퓨터에 문제가 생기면 세션 전체가 함께 끊어지기 때문이다.

또한 나는 개발자로서 서버를 화면이 없는 컴퓨터에서 실행하기를 원한다. 왜냐하면 호스트 클라이언트는 렌더링과 오디오에 자원을 소모하므로, 참가자 수가 늘어날수록 서버 역할에 필요한 여유 자원이 부족해지기 때문이다.

## 사용자 경험 목표

- 진행자는 서버 컴퓨터에서 실행 파일 하나를 실행하는 것만으로 세션을 개방할 수 있고, 화면에 창이 뜨지 않아도 로그 파일로 상태를 확인할 수 있습니다.
- 학습자는 기존과 동일하게 LAN 목록이나 직접 접속으로 서버에 참가하며, 접속 절차에는 아무런 변화가 없습니다.
- 개발자는 기존 클라이언트 빌드 명령과 동일한 스크립트에 환경 변수 하나만 추가하여 서버 빌드를 산출할 수 있습니다.

## 제안

### 1. 빌드 경로

`Assets/Editor/CI/BuildScript.cs`의 `GitLabBuild`에 `BUILD_SUBTARGET`(`Player` 또는 `Server`) 해석을 추가합니다. 서버 서브타겟이 지정되면 `EditorUserBuildSettings.standaloneBuildSubtarget`과 `BuildPlayerOptions.subtarget`을 함께 설정합니다. 두 값을 모두 설정하는 이유는, 에디터 상태에 서브타겟이 반영되어야 `UNITY_SERVER` 스크립팅 정의가 적용된 상태로 스크립트가 컴파일되기 때문입니다. 빌드 산출물은 `build/<BuildTarget>-Server/`에 분리하여 클라이언트 빌드와 섞이지 않게 합니다.

`Tools/CI/build-unity.ps1`과 `Tools/CI/build-unity.sh`는 `BUILD_SUBTARGET`을 검증하고 정규화한 뒤 Unity에 `-standaloneBuildSubtarget` 인자로 전달합니다. 두 스크립트는 작업 공간 용량을 관리하기 위해 빌드 산출물을 삭제해 왔는데, 서버 빌드에서는 산출물 자체가 배포 대상이므로 `KEEP_BUILD_OUTPUT=1`로 보존할 수 있게 했습니다.

### 2. 런타임 부트스트랩

`MultiplayerInfrastructure.Server` 네임스페이스에 두 타입을 추가합니다.

- `DedicatedServerOptions`: 커맨드라인 인자를 해석합니다. Unity API에 의존하지 않으므로 EditMode 테스트로 직접 검증할 수 있습니다.
- `DedicatedServerRuntime`: `[RuntimeInitializeOnLoadMethod]`로 실행되며, 데디케이티드 모드를 감지하고 실행 정보를 런타임 레지스트리에 등록한 뒤 시작 씬으로 전환합니다.

데디케이티드 모드는 서버 서브타겟으로 빌드된 경우(`UNITY_SERVER`)이거나 `-dedicatedServer` 인자가 주어진 경우에 활성화됩니다. 후자를 지원하는 이유는, Dedicated Server 빌드 지원 모듈이 설치되지 않은 컴퓨터에서도 일반 플레이어 빌드를 `-batchmode -nographics -dedicatedServer`로 실행하여 동일한 시작 경로를 점검할 수 있게 하기 위해서입니다.

기존 흐름에서 IntroScene UI가 담당하던 실행 정보 등록(`SessionInformation`, `IsOpeningServer`, `UseLanDiscovery`, `LoadedFromIntroScene`)을 `DedicatedServerRuntime`이 대신 수행하므로, IngameSceneBootstrapper 이후의 세션 시작 흐름은 기존 구조를 그대로 사용합니다. 새로운 키 `IsDedicatedServer`를 추가하여 실행 모드를 조회할 수 있게 합니다.

### 3. 서버 전용 세션 시작

`FishNetSupport.StartSession`에 `startLocalClient` 매개변수(기본값 `true`)를 추가하여, 서버를 개방하되 로컬 클라이언트를 붙이지 않는 경로를 마련합니다. 기존 호출부는 기본값을 사용하므로 동작이 달라지지 않습니다. 그리고 `StartDedicatedServer`가 바인딩 주소를 설정한 뒤 이 경로를 호출합니다. 바인딩 주소는 `Transport.SetServerBindAddress`로 지정하며, 기본값 `0.0.0.0`은 모든 네트워크 인터페이스에서 접속을 수신한다는 뜻입니다.

FishNet의 `NetworkManager`는 서버 빌드에서 `Start` 시점에 서버를 자동으로 개방합니다(`ServerManager.StartOnHeadless`). 이 시점은 전송 설정보다 앞서기 때문에, 인자로 지정한 포트와 바인딩 주소가 반영되지 않은 채로 소켓이 열립니다. 그래서 `DedicatedServerRuntime`이 씬 로드마다 `SetStartOnHeadless(false)`를 적용하여 자동 개방을 차단하고, 전송 설정을 마친 세션 부트스트랩이 서버를 개방하도록 했습니다. 이 처리는 FishNet의 공개 API만 사용하므로 서드파티 모듈을 수정하지 않습니다.

`IngameSceneBootstrapper`는 데디케이티드 모드에서 연결 실패 오버레이 씬(`NetworkSessionFailureScene`)을 요구하지 않습니다. 이 오버레이는 접속에 실패한 사용자에게 상황을 안내하는 화면이므로, 표시 대상이 없는 서버에서는 필요하지 않습니다. `SceneUIIntroSceneController`도 데디케이티드 모드에서는 UI를 구성하지 않고 비활성화됩니다. 그리고 `RecognitionCheckMicrophoneInput`은 실행 초기에 운영체제의 마이크 권한을 요청하는데, 입력 장치도 대화 상자도 없는 서버에서는 이 요청을 건너뜁니다.

### 4. 데이터팩

데이터팩은 서버가 실행하는 주기 명령과 게임 규칙을 담고 있으므로, 데디케이티드 서버에서도 반드시 불러올 수 있어야 합니다. `DatapackRuntimeService`는 내장 데이터팩(`Assets/StreamingAssets/DataPacks/`)을 런타임 폴더로 복사한 뒤, 그 폴더에서 외장 데이터팩과 함께 읽고 식별자로 선택하는 구조를 이미 갖추고 있습니다. 다만 그 폴더가 `persistentDataPath` 아래에 있어서 운영자가 접근하기 어렵다는 문제가 있었습니다.

그래서 `DatapackRuntimeService`에 `DatapackRootPath`와 `TrySetDatapackRootOverride`를 추가하여 폴더 경로를 지정할 수 있게 했습니다. 데디케이티드 서버는 실행 파일과 같은 위치의 `DataPacks` 폴더를 사용하고, `-datapacksPath`로 다른 경로를 지정할 수도 있습니다. 경로를 지정하지 않는 클라이언트의 동작은 이전과 같습니다.

활성화 목록은 `-datapacks identifier-a,identifier-b`로 지정하며, 지정하지 않으면 `session.config.json`의 `datapacks` 항목이 기본값이 됩니다. 기본값까지 무시하려면 `-noDatapacks`를 사용합니다. 이때 빈 목록을 레지스트리에 등록해야 합니다. 등록하지 않으면 `DatapackRuntimeService`가 세션 설정 파일의 목록으로 대체하기 때문입니다.

## 자세한 달성 목표

| 목표 | 달성 방법 |
|---|---|
| 서버 빌드 산출 | `BUILD_SUBTARGET=Server`로 `Tools/CI/build-unity.*` 실행 |
| 클라이언트 빌드 영향 없음 | 서브타겟 기본값 `Player` 유지, 빌드 후 이전 서브타겟 복원 |
| 무인 실행 | `DedicatedServerRuntime`이 IntroScene UI 없이 세션 개방 |
| 운영 설정 | 포트, 바인딩 주소, 세션 이름, LAN 브로드캐스트, 프레임 레이트를 인자로 지정 |
| 데이터팩 | 실행 파일 옆 `DataPacks` 폴더에서 내장·외장 데이터팩을 식별자로 활성화 |
| CI 검증 | `buildDedicatedServer` 매개변수로 Windows 서버 빌드 작업 실행 |

## 문서화

- 운영 및 빌드 절차: `Documents/guide/DedicatedServer.md`를 신규 작성했습니다.
- 데이터팩 폴더 API: `Documents/api-references/MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md`에 폴더 관련 절을 추가했습니다.
- 변경 기록: `Documents/changes/2026-08-29-dedicated-server-build.md`를 신규 작성했습니다.
- 색인: `Documents/README.md`의 변경 기록 목록과 `Documents/changes/README.md`의 DOC-INDEX 블록을 갱신했습니다.

## 가용성과 테스트

이 변경은 기존 호스트 방식의 세션 시작 경로를 그대로 두고 분기만 추가하기 때문에, 클라이언트와 호스트의 동작에는 영향을 주지 않도록 설계했습니다. 다만 다음 위험 요소가 남아 있습니다.

- Dedicated Server 빌드 지원 모듈이 설치되지 않은 빌드 에이전트에서는 서버 빌드가 실패합니다. 그래서 빌드 실패 시 필요한 모듈을 안내하는 메시지를 추가했고, CI 작업은 기본적으로 비활성화된 매개변수로 두었습니다.
- 서버 전용 실행에서는 로컬 클라이언트가 없으므로, 클라이언트 존재를 전제하는 씬 객체가 있다면 경고가 발생할 수 있습니다. 실제 서버 실행 로그로 확인해야 하는 항목입니다.

검증 항목은 다음과 같습니다.

- EditMode 테스트 `DedicatedServerOptionsTests` 14종으로 인자 해석과 모드 판별을 검증합니다.
- 일반 플레이어 빌드를 `-batchmode -nographics -dedicatedServer`로 실행하여 서버 개방과 클라이언트 접속을 확인합니다.
- 서버 서브타겟 빌드를 실제로 산출하여 실행 파일이 생성되는지 확인합니다. 이 항목은 Dedicated Server 빌드 지원 모듈이 설치된 컴퓨터에서 수행해야 합니다.

## 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 수용 기준 1: `BUILD_SUBTARGET=Server`로 빌드하면 `build/StandaloneWindows64-Server/`에 실행 파일이 생성된다.
- 수용 기준 2: 생성된 실행 파일을 `-port 37891` 등의 인자와 함께 실행하면, 창이 뜨지 않은 상태로 서버가 개방되고 로그에 `[DedicatedServer]` 항목이 기록된다.
- 수용 기준 3: 클라이언트 빌드에서 해당 서버에 접속하면 플레이어가 스폰되고, 서버는 로컬 플레이어를 생성하지 않는다.
- 수용 기준 4: 기존 호스트 방식(IntroScene의 호스트 버튼)으로 세션을 여는 동작이 이전과 동일하게 유지된다.
- 수용 기준 5: 실행 파일 옆 `DataPacks` 폴더에 넣은 외장 데이터팩이 `-datapacks`로 활성화되고, 로그에 활성화 목록이 기록된다.
- 성공 지표: 진행자용 호스트 컴퓨터 없이 실습 세션을 운영할 수 있고, 서버 프로세스가 세션 도중 종료되지 않는다.

## 링크, 참고사항

- Unity 매뉴얼: Dedicated Server 플랫폼과 `UNITY_SERVER` 스크립팅 정의
- FishNet: `Transport.SetServerBindAddress`, `ServerManager.StartConnection`
- 관련 구현: `Assets/Modules/MultiplayerInfrastructure/Scripts/Server/`, `Assets/Editor/CI/BuildScript.cs`, `Tools/CI/build-unity.ps1`, `Tools/CI/build-unity.sh`, `azure-pipelines.yml`
