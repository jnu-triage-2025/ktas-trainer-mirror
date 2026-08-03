# KTAS Trainer

![](https://img.shields.io/badge/Unity_Engine-6000.2.8f1-white?logo=unity)

> [!WARNING]  
> 이 프로젝트에서는 유니티의 실험적 API를 사용하는 부분이 있습니다. 이러한 API는 다른 버전에서 미구현, 제거, 변경될 가능성이 있습니다. 유니티 버전을 변경해야 한다면, 이들 API가 여전히 사용 가능한지, 실험적 플래그가 제거되어 네임스페이스를 변경해야 하는지 확인하시기 바랍니다.
> 관계 모듈: `MultiplayerInfrastructure.Editor`

## 수동 배치 빌드

GitLab 빌드는 자동으로 실행되지 않습니다. `web` 또는 상위 파이프라인에서 시작한
파이프라인의 `build-macos`/`build-windows` 작업을 수동 실행합니다. 두 작업 모두
macOS 로컬 Unity Runner에서 실행됩니다. Windows Player는 macOS에서 생성합니다.

스크립트는 `ProjectSettings/ProjectVersion.txt`에서 프로젝트 Unity 버전을 읽고 표준
Unity Hub 설치 위치에서 실행 파일을 자동으로 찾습니다. 필요 모듈이 없으면 빌드 전에
명확한 오류를 내고 중단합니다. 비표준 위치에서는 로컬 Runner에만
`UNITY_EXECUTABLE`을 설정해 덮어쓸 수 있으며, 이 값은 레포에 동기화하지 않습니다.
빌드 결과는 `build/`에 생성되며 Git과 GitLab 아티팩트에서 제외됩니다.
