# Quest Waypoint Highlights Guide

## Overview
This feature adds automatic waypoint highlighting when a quest is assigned to a player and displays the waypoint identifier in the quest UI for clear visual reference.

## Changes Made

### 1. QuestManager Enhancement
- **File**: `Assets/Modules/MultiplayerInfrastructure/Scripts/Quest/QuestManager.cs`
- Added a new flag `FeatureFlags.HighlightAssignedWaypoint`.
- When a quest is assigned to a player, the manager checks this flag.
- If set, it calls the waypoint highlight system **once** to flash the assigned waypoint on the map/HUD.

### 2. UI Element Updates
#### QuestPanelElement
- **File**: `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/QuestPanelElement.cs`
- The quest list entry now explicitly shows `QuestData.WaypointIdentifier` (if present) alongside the quest title and description.
- This lets players see which map marker corresponds to the quest objective without opening the map.

#### QuestPreviewHudElement
- **File**: `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/QuestPreviewHudElement.cs`
- The small HUD that tracks the active quest also displays the waypoint identifier.
- When the waypoint is highlighted (via the flag), the HUD entry pulses or changes color to draw attention.

## How It Works (Step‑by‑Step)

1. **Quest Assignment**
   - A quest is granted via `/quest give`, a datapack event, or a scripted trigger.
   - `QuestManager.AddQuestToPlayer(playerId, questId)` is called.

2. **Flag Check**
   - Inside `QuestManager`, after the quest is added, the code evaluates:
     ```csharp
     if (FeatureFlags.HasFlag(FeatureFlags.HighlightAssignedWaypoint))
     {
         var waypoint = questData.WaypointIdentifier;
         if (!string.IsNullOrEmpty(waypoint))
         {
             WaypointHighlightService.FlashOnce(waypoint);
         }
     }
     ```
   - `WaypointHighlightService.FlashOnce` triggers a brief visual pulse on the target waypoint.

3. **UI Update**
   - Both `QuestPanelElement` and `QuestPreviewHemElement` rebind their text fields to show `questData.WaypointIdentifier`.
   - If the quest has no waypoint, the field shows “—” or is hidden per UI configuration.

## Configuration

The feature flag is managed via the existing `FeatureFlags` system (see `FeatureFlags.cs`). To enable or disable the automatic highlight:

- **Enable** (default in current builds):
  ```csharp
  FeatureSets.Enable(FeatureFlags.HighlightAssignedWaypoint);
  ```
- **Disable**:
  ```csharp
  FeatureSets.Disable(FeatureFlags.HighlightAssignedWaypoint);
  ```

You can toggle this at runtime via the console command:
```
/feature toggle HighlightAssignedWaypoint
```

## Verification Steps

1. Ensure the flag is enabled (default).
2. Grant a quest that has a `WaypointIdentifier` defined in its `QuestData` (e.g., via `/quest give rescue_villager`).
3. Observe:
   - The waypoint on the map/HUD flashes briefly upon quest receipt.
   - The quest entry in the Quest List (`J` or equivalent UI) shows the waypoint ID next to the quest title.
   - The active quest HUD (top‑right corner) also displays the same identifier.
4. If the quest has no waypoint, no flash occurs and the UI shows a placeholder (e.g., “—”).

## Customizing Quest Data

When creating or editing a quest asset (`.quest` file or ScriptableObject), set the `WaypointIdentifier` field to the exact string used by the waypoint system (matches the ID passed to `WaypointManager.RegisterWaypoint`).

Example (pseudo‑JSON):
```json
{
  "id": "rescue_villager",
  "title": "Rescue the Villager",
  "description": "Find and escort the villager to safety.",
  "waypoint_identifier": "villager_camp_marker",
  "objectives": [...]
}
```

## Compatibility

- Works with both server‑hosted and peer‑hosted sessions.
- Does not interfere with manual waypoint toggling via the map UI.
- If the waypoint ID does not exist, the highlight call is a no‑op (silent fail).

## Related Documentation

- [Quest System Overview](../quest/quest-system-overview.md)
- [Waypoint Manager API](../multiplayerinfrastructure/waypoint/waypointmanager.md)
- [Feature Flags Reference](../misc/featureflags.md)

