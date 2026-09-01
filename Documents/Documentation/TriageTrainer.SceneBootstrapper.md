# <a id="TriageTrainer_SceneBootstrapper"></a> Namespace TriageTrainer.SceneBootstrapper

### Classes

 [IndevConnectionFailureOverlay](TriageTrainer.SceneBootstrapper.IndevConnectionFailureOverlay.md)

사전에 애디티브 로드된 네트워크 세션 종료 씬의 UXML/USS UI 컨트롤러입니다.
모든 세션 부트스트래퍼가 같은 씬을 미리 로드하므로, 장애 표시 시 추가 씬 로딩이 없습니다.

 [IndevSceneBootstrapper](TriageTrainer.SceneBootstrapper.IndevSceneBootstrapper.md)

IndevScene용 자동 부트스트래퍼입니다.
IntroScene 흐름 없이 씬을 직접 재생하면, 자동으로 호스트(서버+클라이언트) 세션을 시작하고
SystemOverlayScene만 애디티브 로드합니다.
이후 프리팹 게임 오브젝트로 씬에 배치하여 사용합니다.

IndevScene은 자체 월드/스폰포인트를 갖춘 개발 씬이므로 OverworldScene을 로드하지 않습니다.
OverworldScene을 함께 로드하면 동명 스폰포인트(spawnpoint-commons)가 PlayerSpawnPointRegistry에서
IndevScene 스폰포인트를 덮어써 플레이어가 OverworldScene 위로 스폰되고, 월드 콘텐츠가 중복됩니다.

 [NetworkSessionFailure](TriageTrainer.SceneBootstrapper.NetworkSessionFailure.md)

네트워크 세션 실패의 상태 코드와 상세 정보를 나타냅니다.
상태 코드 범위: 1xxx=정당한 사유, 2xxx=예기치 않은 오류, 9xxx=미분류.

 [TutorialSceneBootstrapper](TriageTrainer.SceneBootstrapper.TutorialSceneBootstrapper.md)

TutorialScene용 자동 부트스트래퍼입니다.
IntroScene 흐름 없이 씬을 직접 재생하면, 자동으로 호스트(서버+클라이언트) 세션을 시작하고
SystemOverlayScene을 애디티브 로드합니다.
이후 프리팹 게임 오브젝트로 씬에 배치하여 사용합니다.

### Enums

 [NetworkSessionFailure.StatusCode](TriageTrainer.SceneBootstrapper.NetworkSessionFailure.StatusCode.md)

