# Moving Patient Bed and Patient Entity Documentation

## Overview
This document explains the interaction between the moving patient bed entity and the patient entity, focusing on the `IReposable` interface, the patient carry system, and the mechanics of placing a patient onto or lifting them from the bed.

## Key Components

### 1. IReposable Interface
- **File**: `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/IReposable.cs`
- **Purpose**: Defines an interface specification for objects that can be placed on or carried from a surface (e.g., a bed).
- **Members**:
  - `int Weight { get; }` – Returns the weight of the object. Used by the bed to calculate required personnel for movement.

### 2. Player Carry System (Reposable Carry)
- **File**: `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.ReposableCarry.cs`
- **Purpose**: Manages the player’s ability to pick up, carry, and drop `IReposable` objects.
- **Key Members**:
  - `bool IsCarryingReposable { get; }` – True when the player is currently carrying an reposable.
  - `IReposable CarriedReposable { get; }` – Reference to the carried object (null if none).
  - `bool TryPickUpReposable(IReposable target, out string failReason)` – Attempts to pick up the target.
  - `bool TryDropCarriedReposable(out string failReason)` – Attempts to drop the currently carried object.

### 3. MovingPatientBedController
- **File**: `Assets/Modules/TriageTrainer/Prefabs/Entity/MovingPatientBed/MovingPatientBedController.cs`
- **Purpose**: Represents a movable bed that can have a patient placed on it or lifted from it.
- **Implemented Interfaces**: `IInteractable`, `IInteract`
- **Core Mechanics**:
  - **Weight Calculation**: Total weight = bed’s own weight (`_weight`) + weight of the occupant (`ReposedTarget?.Weight ?? 0`).
  - **Movement Modes**:
    - **Toggle**: Simple on/off movement.
    - **Hold**: Requires continuous input to move.
  - **Attachment Visuals**: Maps item identifiers to visual objects via `_attachableItemVisualPairs` inspector list (runtime dictionary `_attachableVisualMap`). All visual objects are hidden at spawn (`HideAllAttachableVisuals()`); only items explicitly attached via `TryAttachItem()` become visible.
  - **Message Cooldown**: Prevents chat spam by enforcing a 3‑second cooldown on repeated interaction messages.
  - **Patient Transfer Methods**:
    - `ReposePatientOnBed(IPatient patient)` – Lays a patient onto the bed.
    - `LiftPatientFromBed()` – Lifts the patient off the bed and returns the patient reference.

### 4. PatientController
- **File**: `Assets/Modules/TriageTrainer/Prefabs/Entity/Patient/PatientController.cs`
- **Purpose**: Represents a patient that can lie on a bed and be carried by a player.
- **Implemented Interfaces**: `IInteractable`, `IInteract`, `IReposable`
- **Key Features**:
  - Default weight: `4`.
  - When lying on a bed and interacted with, the patient signals the bed to lift them (`Bed.LiftPatientFromBed()`), handing control to the player via the carry system.
  - Visual attachment: Uses `TreatmentDisplay` enum system (`PatientDisplayState` + `PatientTreatmentDisplayingChildGameObjects`) to manage 25 treatment display visuals. All visuals are hidden at spawn; only explicitly activated displays become visible via item use or scenario `EntityInit` commands.
  - Message Cooldown: Shares the 3‑second cooldown mechanism to avoid repetitive chat notifications.

## Interaction Flow

1. **Patient onto Bed**
   - Player interacts with a patient (while not carrying anything).
   - Patient calls `Interact` → requests the nearby bed to take the patient.
   - Bed verifies capacity, calls `ReposePatientOnBed(patient)`.
   - Patient becomes the bed’s `ReposedTarget`; visual attachment is shown.
   - Player is no longer carrying anything.

2. **Patient from Bed to Player**
   - Player interacts with the bed that has a patient.
   - Bed’s `Interact` → checks if player is free, then calls `LiftPatientFromBed()`.
   - Bed returns the patient instance; player’s carry system picks it up (`TryPickUpReposable`).
   - Bed’s `ReposedTarget` cleared; visual attachment removed.

3. **Patient from Player to Bed (reverse)**
   - Player carrying a patient interacts with a bed.
   - Bed’s `Interact` → attempts to accept the carried patient via `TryPickUpReposable`‑style logic (internally uses bed’s placement check).
   - If successful, patient is transferred to bed’s `ReposedTarget` and visually attached.

## Configuration & Setup

### Required Prefabs / Assets
- **Bed Prefab**: `Assets/Modules/TriageTrainer/Prefabs/Entity/MovingPatientBed/MovingPatientBed.prefab`
  - Must contain `MovingPatientBedController` component.
  - Must have a `UIDocument` for interaction prompts (if applicable).
- **Patient Prefab**: `Assets/Modules/TriageTrainer/Prefabs/Entity/Patient/PatientPrefab.prefab`
  - Must contain `PatientController` component.
- **Player Prefab**: Ensure `PlayerController.ReposableCarry` component is present on the player hierarchy.

### Registry Registration
- Both `MovingPatientBedController` and `PatientController` register themselves in the appropriate registries (`RegistryType.Entity`) during `Entity` during their `Awake` via `TTRegistryMonoBehaviourSupport` or equivalent.
- Ensure the identifier constants (`Identifier`) in each script match the prefab names used in Resources or addressable assets.

## Common Pitfalls
- **Missing IReposable Implementation**: If either bed or patient lacks proper `IReposable` weight implementation, the carrying system will reject pickups.
- **Missing Visual Mapping**: The identifier‑to‑visual mapping must exist in the bed/patient controller; otherwise the attached model will not appear/disappear correctly.
- **Registry Duplicates**: Registering the same entity type twice leads to warnings; ensure each prefab registers only once (typically via the centralized `TTRegistryMonoBehaviourSupport`).

## Verification Steps
1. Enter Play mode in a scene containing a bed and a patient prefab.
2. Select the patient and press the interact key (default `F`). Verify the patient lies onto the bed and disappears from the player’s hands.
3. Select the bed with the patient aboard and press interact. Verify the patient lifts onto the player’s hands and the bed becomes empty.
4. Attempt to pick up the patient from the bed while already carrying another item – should fail with appropriate feedback.
5. Check the console for any warnings regarding missing weights or missing visual mappings.

## Related Documentation
- [IReposable Interface](../multiplayerinfrastructure/IReposable.md)
- [Player Controller – Carry System](../multiplayerinfrastructure/player/playercontroller.md#reposable-carry)
- [Scenario Event Registry](../multiplayerinfrastructure/scenario/scenarioeventidentifierregistry.md)
- [Item Base Model SO](../triagetrainer/itembasemodelso.md)
