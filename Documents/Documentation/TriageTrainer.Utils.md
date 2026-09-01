# <a id="TriageTrainer_Utils"></a> Namespace TriageTrainer.Utils

### Classes

 [EntityPresetDebugger](TriageTrainer.Utils.EntityPresetDebugger.md)

개발 환경(IndevScene)에서 EntityPreset 의 스폰 여부/동작 여부를 빠르게 확인하기 위한 디버그 도구.

사용 목적은 "프리셋이 등록되었는가 / 스폰이 성공하는가 / 자식 분리(ungroup)가 동작하는가 / 결과 엔티티가
레지스트리에 올라오는가" 정도의 확인이다. 정식 게임플레이 경로가 아니라 디버그 보조용이다.

사용법:
 - 이 컴포넌트를 IndevScene 의 빈 GameObject 에 붙이고 인스펙터에 SO 와 (선택)스폰 기준점을 지정한다.
 - 플레이모드(호스트/서버) 진입 후, 컴포넌트 우클릭 ContextMenu 또는 화면 좌상단 디버그 오버레이 버튼을 사용한다.
 - 네트워크 프리셋은 서버 컨텍스트에서만 복제 스폰되므로, 호스트/서버로 실행한 상태에서 확인한다.

 [GeneratedByOverworldGameObjectInitializerEditor](TriageTrainer.Utils.GeneratedByOverworldGameObjectInitializerEditor.md)

 [OverworldSpawnPoint](TriageTrainer.Utils.OverworldSpawnPoint.md)

