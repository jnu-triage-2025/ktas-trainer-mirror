# 2026-02-18 Quest Waypoint Highlights

## 변경 개요
- `QuestManager`에 `FeatureFlags.HighlightAssignedWaypoint` 마스크를 추가하고, 새 퀘스트가 플레이어에게 할당될 때 웨이포인트 하이라이트를 자동으로 최초 1회 호출하는 흐름을 구현했습니다.
- 퀘스트 리스트(`QuestPanelElement`)와 미리보기 HUD(`QuestPreviewHudElement`)에서 `QuestData.WaypointIdentifier`를 명시적으로 보여줘 현재 목표 지점을 육안으로도 확인할 수 있도록 변경했습니다.

## 참조 파일
- Assets/Modules/MultiplayerInfrastructure/Scripts/Quest/QuestManager.cs
- Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/QuestPreviewHudElement.cs
- Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/QuestPanelElement.cs
