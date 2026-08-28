# 데디케이티드 서버 (헤드리스 서버)

이 문서는 KTAS Trainer의 서버를 화면 없이 동작하는 독립 실행 파일로 빌드하고 실행하는 방법을 설명합니다. 데디케이티드 서버는 사람이 조작하는 호스트 클라이언트 없이 세션을 유지하기 위한 실행 형태이며, 화면 출력과 입력 장치를 사용하지 않습니다.

## 준비 사항

Unity Hub에서 프로젝트가 사용하는 에디터 버전(`ProjectSettings/ProjectVersion.txt`에 기재)에 **Dedicated Server Build Support** 모듈을 추가로 설치해야 합니다. 배포 대상 운영체제에 맞추어 Windows, Linux, macOS 중 필요한 모듈을 설치합니다. 이 모듈이 없으면 빌드가 실패하며, 빌드 로그에 필요한 모듈을 안내하는 메시지가 기록됩니다.

## 빌드하기

### 명령줄에서 빌드

기존 빌드 스크립트에 `BUILD_SUBTARGET=Server` 환경 변수를 추가하면 데디케이티드 서버가 산출됩니다.

Windows(PowerShell):

```powershell
$env:BUILD_TARGET = 'StandaloneWindows64'
$env:BUILD_SUBTARGET = 'Server'
$env:BUILD_NAME = 'ktas-trainer-server'
$env:KEEP_BUILD_OUTPUT = '1'
.\Tools\CI\build-unity.ps1
```

macOS 및 Linux(bash):

```bash
BUILD_TARGET=StandaloneLinux64 BUILD_SUBTARGET=Server BUILD_NAME=ktas-trainer-server KEEP_BUILD_OUTPUT=1 Tools/CI/build-unity.sh
```

산출물은 `build/<BuildTarget>-Server/` 아래에 생성됩니다. 예를 들어 위 Windows 예시에서는 `build/StandaloneWindows64-Server/ktas-trainer-server.exe`가 만들어집니다.

`KEEP_BUILD_OUTPUT`을 지정하지 않으면 빌드가 끝난 뒤 산출물이 삭제됩니다. 이는 CI 에이전트의 작업 공간 용량을 관리하기 위한 기존 동작이므로, 실행 파일을 사용해야 한다면 반드시 `KEEP_BUILD_OUTPUT=1`을 지정해야 합니다.

### 에디터에서 빌드

Unity 에디터의 Build Profiles 창에서 플랫폼을 **Dedicated Server**로 전환한 뒤 빌드해도 동일한 결과를 얻을 수 있습니다. 배치 모드에서 진입점을 직접 지정하려면 `-executeMethod GitLabBuild.BuildDedicatedServer`를 사용합니다.

### CI에서 빌드

Azure Pipelines 실행 시 **Build Windows dedicated server** 매개변수를 켜면 `BuildDedicatedServer` 작업이 실행됩니다. 이 작업은 빌드가 성공하는지 확인하고 로그를 아티팩트로 게시하며, 실행 파일 자체는 보관하지 않습니다. 빌드 에이전트에도 Dedicated Server Build Support 모듈이 설치되어 있어야 합니다.

## 실행하기

서버 실행 파일에 커맨드라인 인자를 붙여 실행합니다.

```bash
./ktas-trainer-server -batchmode -nographics -logFile server.log -port 37891 -sessionName "KTAS 실습 1실"
```

Windows에서는 다음과 같이 실행합니다.

```powershell
.\ktas-trainer-server.exe -batchmode -nographics -logFile server.log -port 37891 -sessionName "KTAS 실습 1실"
```

서버 서브타겟으로 빌드한 실행 파일은 인자가 없어도 데디케이티드 모드로 동작합니다. 반면 일반 클라이언트 빌드를 서버처럼 실행하려면 `-dedicatedServer` 인자를 반드시 지정해야 합니다. 이 방식은 Dedicated Server 모듈을 설치하지 않은 컴퓨터에서 서버 시작 흐름을 점검할 때 유용합니다.

```bash
./ktas-trainer -batchmode -nographics -dedicatedServer -port 37891
```

### 지원하는 인자

| 인자 | 기본값 | 설명 |
|---|---|---|
| `-dedicatedServer`, `-server` | 없음 | 일반 빌드에서 데디케이티드 모드를 강제합니다. 서버 빌드에서는 지정하지 않아도 됩니다. |
| `-port <번호>` | `session.config.json`의 값 또는 `37891` | 서버가 수신할 포트를 지정합니다. |
| `-bindAddress <주소>`, `-bind <주소>` | `0.0.0.0` | 서버 소켓이 바인딩할 주소입니다. 기본값은 모든 네트워크 인터페이스에서 접속을 받습니다. |
| `-sessionName <이름>` | `session.config.json`의 값 또는 `KTAS Dedicated Server` | LAN 목록과 로그에 표시할 세션 이름입니다. |
| `-datapacks <목록>` | `session.config.json`의 값 | 쉼표로 구분한 데이터팩 식별자 목록입니다. |
| `-lanBroadcast <true\|false>`, `-noLanBroadcast` | `true` | LAN 검색 브로드캐스트 사용 여부입니다. |
| `-targetFrameRate <숫자>`, `-fps <숫자>` | `60` | 서버 루프의 목표 프레임 레이트입니다. 0 이하이면 제한하지 않습니다. |
| `-startScene <이름>` | `IngameScene` | 서버가 진입할 시작 씬입니다. 특별한 사정이 없으면 변경하지 않습니다. |

인자는 `-port 37891`처럼 값을 띄어 쓰는 형식과 `-port=37891`처럼 등호로 잇는 형식을 모두 지원하며, 대소문자를 구분하지 않습니다. 값을 해석하지 못하면 기본값을 사용하고 경고를 로그에 기록합니다.

`-batchmode`, `-nographics`, `-logFile`은 Unity가 직접 해석하는 인자입니다. 화면을 만들지 않고 로그를 파일로 남기려면 함께 지정하기를 권장합니다.

### 실행 확인

로그 파일에서 다음과 같은 항목을 확인할 수 있습니다.

```text
[DedicatedServer] 데디케이티드 서버 모드로 시작합니다. bind=0.0.0.0:37891, session='KTAS 실습 1실', ...
[FishNetSupport] Server bind address configured to 0.0.0.0 (IPv4).
[IngameSceneBootstrapper] Session bootstrap completed. Mode=DedicatedServer, Endpoint=0.0.0.0:37891, ...
```

`Mode=DedicatedServer`가 기록되면 서버가 로컬 클라이언트 없이 개방된 것입니다. 클라이언트는 기존과 동일하게 LAN 목록이나 직접 접속으로 참가하며, 접속 주소에는 서버 컴퓨터의 실제 IP 주소를 사용합니다.

## 동작 방식

1. `DedicatedServerRuntime`이 실행 초기에 커맨드라인 인자를 해석하고 데디케이티드 모드 여부를 판단합니다.
2. 데디케이티드 모드이면 IntroScene UI를 구성하지 않고, 세션 정보를 런타임 레지스트리에 등록한 뒤 `IngameScene`으로 전환합니다.
3. `IngameSceneBootstrapper`가 `OverworldScene`과 `SystemOverlayScene`을 불러온 다음, `FishNetSupport.StartDedicatedServer`를 호출합니다.
4. 서버는 개방되지만 로컬 클라이언트는 시작되지 않으므로, 서버 프로세스에는 플레이어가 스폰되지 않습니다.

## 알려진 제약

- 데디케이티드 서버는 화면과 오디오 출력을 사용하지 않습니다. 진행자용 화면이 필요하다면 별도의 클라이언트로 접속해야 합니다.
- 서버 실행 파일은 Dedicated Server Build Support 모듈이 설치된 환경에서만 빌드할 수 있습니다.
- 서버 종료는 프로세스 종료로 처리합니다. 종료 시 FishNet 연결이 정리되며, 접속 중이던 클라이언트는 연결이 끊어집니다.
