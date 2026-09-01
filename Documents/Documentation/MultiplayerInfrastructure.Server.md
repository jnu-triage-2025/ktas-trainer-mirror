# <a id="MultiplayerInfrastructure_Server"></a> Namespace MultiplayerInfrastructure.Server

### Classes

 [DedicatedServerOptions](MultiplayerInfrastructure.Server.DedicatedServerOptions.md)

데디케이티드 서버(헤드리스) 실행 옵션입니다.
커맨드라인 인자를 해석하여 서버 세션을 시작하는 데 필요한 값을 담습니다.
이 타입은 Unity API에 의존하지 않으므로 EditMode 테스트에서 그대로 검증할 수 있습니다.

 [DedicatedServerRuntime](MultiplayerInfrastructure.Server.DedicatedServerRuntime.md)

데디케이티드 서버(헤드리스) 실행을 부트스트랩합니다.

Dedicated Server 서브타겟으로 빌드하면 <code>UNITY_SERVER</code>가 정의되어 자동으로 활성화되며,
일반 플레이어 빌드에서도 <code>-dedicatedServer</code>(또는 <code>-server</code>) 인자를 주면 활성화됩니다.
활성화되면 IntroScene의 UI 흐름을 건너뛰고, 커맨드라인 옵션으로 구성한 세션 정보를
런타임 레지스트리에 등록한 뒤 시작 씬(<xref href="MultiplayerInfrastructure.Server.DedicatedServerOptions.StartScene" data-throw-if-not-resolved="false"></xref>)으로 진입합니다.
이후 세션 시작은 IngameSceneBootstrapper가 담당합니다.

 [ServerBanService](MultiplayerInfrastructure.Server.ServerBanService.md)

서버 재시작 후에도 유지되는 표시 이름 기반 차단 목록입니다.

