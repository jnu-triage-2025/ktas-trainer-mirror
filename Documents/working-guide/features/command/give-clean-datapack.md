# Give / Clean Commands and Datapack Runtime Guide

## Overview
This guide covers the newly added `/give` and `/clean` chat commands, the system command execution pathway, and the datapack runtime feature introduced in the 2026‑02‑18 update.

---

## 1. /give Command
**Syntax**
```
/give <item_identifier> [count=1] [player_identifier]
```

**Parameters**
- `item_identifier` – Item ID registered in `RegistryType.Item`.
- `count` – Optional amount to give (default `1`).
- `player_identifier` – Optional target player. Accepts:
  - Omit → command issuer.
  - `@s` → issuer.
  - `fish:<id>` or raw `<id>` – specific player by fish ID or player ID.

**Behavior**
1. Verify the item exists in the item registry.
2. Resolve the target player.
3. Attempt to add the item(s) to the target’s inventory via `PlayerController.Inventory.TryAddItemToInventory`.
4. If the inventory would overflow, the excess items are spawned as a world drop in front of the target player.

**Example**
```
/give health_potion 5 @s
```
Gives the issuer five health potions, dropping any excess on the ground.

---

## 2. /clean Command
**Syntax**
```
/clean [item_identifier] [count]
```

**Parameters**
- Omit both arguments → clears the entire inventory of the issuer.
- Provide `item_identifier` only → removes all instances of that item from the issuer’s inventory.
- Provide both `item_identifier` and `count` → removes up to `count` of that item (actual removed = min(owned, count)).
- Providing only `count` is invalid and results in an error.

**Behavior**
- Delegates to `PlayerController.Inventory.ClearInventory()`, `RemoveItemFromInventory()`, or `RemoveAllOfItemFromInventory()` as appropriate.
- No world drop occurs; removed items are simply deleted.

**Example**
```
/clean bandage
```
Removes all bandages from the issuer’s inventory.

---

## 3. System Command Execution Path
The chat system now exposes a way for non‑player systems (e.g., timers, scenario events) to execute chat‑style commands without a sender.

**API**
```csharp
bool ChatService.TryExecuteSystemCommand(string commandLine, out string result)
```
- `commandLine`: Full command text (e.g., `"give iron_ingot 10"`).
- `result`: Output message or error description.
- Returns `true` if the command was parsed and executed successfully; otherwise `false`.

**Usage Example (from a datapack ticker)**
```csharp
if (ChatService.TryExecuteSystemCommand("give iron_ingot 1", out var msg))
{
    Debug.Log($"[Datapack] {msg}");
}
else
{
    Debug.LogError($"[Datapack] Command failed: {msg}");
}
```

**Notes**
- Commands that rely on the sender (e.g., those that modify the issuer’s inventory) will use a dummy “system” player; if the command explicitly requires a real player, it will fail.
- This path re‑uses the same command parsers as the chat UI, ensuring consistent behavior.

---

## 4. Datapack Runtime
The datapack system allows server‑side JSON files to drive periodic command execution and event‑handler injection.

### Core Components
- `DatapackModels.cs` – Defines the JSON schema (`DatapackFile`, `Entry`, `Trigger`, `Action`).
- `DatapackRuntimeService.cs` – Loads `.json` files from `StreamingAssets/Datapacks/`, schedules ticks, and executes actions.

### Supported Features
1. **Periodic Command Execution**
   - `interval` (seconds) defines how often the action runs.
   - Optional `runImmediately` flag to execute once on load.
2. **Event‑Handler Injection**
   - Binds to `ScenarioEventIdentifierRegistry` to add/remove handlers for specific scenario events.
   - Supports backup/restore of existing handlers to allow clean removal when the datapack is disabled or reloaded.

### JSON Structure (example: DatapackSample.json)
```json
{
  "id": "example_datapack",
  "description": "Demo datapack that grants items and logs events.",
  "entries": [
    {
      "trigger": { "type": "interval", "seconds": 10 },
      "action": {
        "type": "command",
        "command": "give iron_ingot 1"
      }
    },
    {
      "trigger": { "type": "event", "eventId": "quest_started" },
      "action": {
        "type": "event_handler",
        "eventId": "quest_started",
        "register": true,
        "callback": "MyCustomHandler"
      }
    }
  ]
}
```

**Field Descriptions**
- `trigger.type`: `"interval"` for time‑based, `"event"` for scenario‑event based.
- `trigger.seconds`: Interval in seconds (only for `interval`).
- `trigger.eventId`: The scenario event ID to listen for (only for `event`).
- `action.type`: `"command"` runs a chat command; `"event_handler"` registers/unregisters an event handler.
- For `command`: `command` string is the exact command line (without leading slash).
- For `event_handler`: `eventId` matches the scenario event; `register` toggles add/remove; `callback` is the method name on a registered `MonoBehaviour` (must be public and parameter‑less or accept a single `string` argument).

### Loading & Enabling
Place `.json` files under `StreamingAssets/Datapacks/`. The `DatapackRuntimeService` auto‑discovers them on startup and begins processing.

To manually reload at runtime:
```csharp
DatapackRuntimeService.ReloadAll();
```

### Debugging
- The service logs each tick and action outcome to the console (prefixed with `[Datapack]`).
- If a JSON file fails to parse, an error is logged and the file is skipped.

---

## 5. Inventory Utility Extensions (for command support)
To support the new commands, `PlayerController.Inventory` received several helper methods:

| Method | Description |
|--------|-------------|
| `TryAddItemToInventory(Item item, int count, out int leftover)` | Attempts to add items; returns actual leftover that could not fit. |
| `ClearInventory()` | Removes all items from the inventory. |
| `RemoveItemFromInventory(string itemId, int count)` | Removes up to `count` of `itemId`; returns actual removed amount. |
| `RemoveAllOfItemFromInventory(string itemId)` | Removes every instance of `itemId`. |
| `CountItemInInventory(string itemId)` | Returns current count of `itemId`. |
| `TryDropItemInFront(Item item, int count, Vector3 offset)` | Spawns the given item(s) as a world drop in front of the player. |

Additional world‑drop helpers:
- `ItemSpawnUtility.TrySpawnDroppedItem(Item item, Vector3 position, Quaternion rotation)` – Creates a dropped item object.
- `Item.ApplyRuntimeItemData(Item item, ItemRuntimeData data)` – Applies runtime modifiers (e.g., durability, custom name) to an item before spawning.

---

## 6. Verification Checklist
- [ ] `/give` works with no arguments (defaults to 1 and issuer).
- [ ] `/give` respects player identifiers (`@s`, `fish:<id>`, raw ID).
- [ ] `/give` overflow creates a world drop.
- [ ] `/clean` with no args clears inventory.
- [ ] `/clean <item>` removes all of that item.
- [ ] `/clean <item> <n>` removes up to `n`.
- [ ] System command execution via `ChatService.TryExecuteSystemCommand` returns expected results.
- [ ] Datapack JSON files placed under `StreamingAssets/Datapacks/` are loaded and execute at the specified interval.
- [ ] Event‑handler bindings fire the correct methods when the associated scenario event occurs.
- [ ] Console logs show `[Datapack]` prefixes for traceability.

---

## 7. Related Files
- Command definitions: `CommandDefinition.Give.cs`, `CommandDefinition.Clean.cs`
- Command service: `CommandService.cs`
- Chat service: `ChatService.cs`
- Datapack models & runtime: `DatapackModels.cs`, `DatapackRuntimeService.cs`
- Inventory extensions: `PlayerController.Inventory.cs`
- World‑drop utilities: `ItemSpawnUtility.cs`, `Item.RuntimeData.cs`

