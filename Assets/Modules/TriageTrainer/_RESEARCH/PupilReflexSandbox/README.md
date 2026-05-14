# Direct Pupil Light Reflex Template (Unity 6000.1.8f1)

This sandbox is a self-contained template for direct pupil light reflex
implementation. It is designed to be merged later without touching existing
main-system scripts.

## Chosen Implementation Direction

Selected approach: **pure 3D module, separated from patient prefabs by default**.

Reasoning:

- Keeps risk low for the existing scenario system.
- Makes it easy to test reflex logic in isolation.
- Allows later integration by assigning anchors from real patient prefabs
  (`Scenario1_final`, `Scenario2Male_final`, `Scenario2Female_final`) without
  modifying those prefabs in this stage.

How this answers the decision points:

1. 3D rendering is used directly.
2. Integration path is component-based (assign face/eye anchors), not hard-coupled.
3. If not integrated into a model yet, cursor-based penlight still works in
   standalone sandbox mode.

## Implementation Summary (Reference)

- Rendering approach: pure 3D (not a 2D overlay, not 3D rendered to a 2D UI).
- Patient integration: separated by default; integration is component-based via
  assigned anchors, not hard-coupled to patient prefab scripts.
- If integrated into a patient model: control happens through
  `PupilReflexSandboxBootstrap` fields (anchors, anatomy, pathology, timing).
- If not integrated: enable/disable the sandbox GameObject for specific exam
  moments (start/stop the reflex check without touching patient prefabs).
- Penlight method: mouse cursor emits a ray and acts as the light source; light
  stimulus is sampled per eye via `PupilReflexMouseProbe`.

## Files

- `Scripts/PupilReflexSandboxBootstrap.cs`
- `Scripts/PupilReflexMouseProbe.cs`
- `Scripts/PupilReflexEye.cs`
- `Scripts/PupilReflexExamController.cs`
- `Scripts/Editor/PupilReflexSandboxPlayModePersist.cs`
- `Shaders/PupilReflexCorneaProjector.shader`

All code is under:

- `Assets/Modules/TriageTrainer/Tests/PupilReflexSandbox/`

## Quick Start

1. Create an empty GameObject in a scene.
2. Add `PupilReflexSandboxBootstrap`.
3. Press Play.

## Usage

### Standalone sandbox (default)

1. Create an empty GameObject in a scene.
2. Add `PupilReflexSandboxBootstrap`.
3. Keep `autoCreateSandboxPatient = true`.
4. Press Play; the eyes and penlight logic are auto-created.

### Patient prefab integration

1. Create a child GameObject under the patient prefab (e.g. `PupilReflexRig`).
2. Add `PupilReflexSandboxBootstrap`.
3. Set `autoCreateSandboxPatient = false`.
4. Assign `patientRoot`, `faceRoot`, `leftEyeAnchor`, `rightEyeAnchor`.
5. (Optional) Assign `faceCollider` if you need extra hit testing for other systems.
6. Toggle `autoPositionAnchors` off if you want to preserve manually tuned eye
   anchor positions.
7. (Optional) Disable the patient eye mesh during the exam to avoid overlap.

### Showing the reflex only in specific moments

- Enable the sandbox GameObject when the exam starts.
- Disable it after the exam finishes.
- Alternatively, keep it enabled but toggle `probeEnabled` on
  `PupilReflexMouseProbe` to control when the penlight can trigger responses.

Default runtime behavior:

- Creates two eyes.
- Eye spacing is 4 cm.
- Eyeball diameter is 2.5 cm.
- Iris diameter is 12 mm.
- Pupil baseline is 5 mm and constricts toward 2 mm.
- Mouse cursor acts as penlight.
- No FaceProxy is created. Any legacy `FaceProxy` object is removed at runtime.
- The patient/eye transforms are not auto-rotated toward the camera during play.

## Clinical and Visual Rules Implemented

### Eye anatomy

- Sclera: sphere.
- Iris: spherical annulus mesh (curvature matches eyeball).
- Pupil: spherical cap mesh (curvature matches eyeball).
- Cornea: transparent outer sphere.

Colors:

- Sclera: white.
- Iris: brown.
- Pupil: black.
- Cornea: transparent.

### Pupil reaction

- Constriction target: 5 mm -> 2 mm.
- Default constriction duration: 0.15 s.
- Constriction uses exponential deceleration:

  - `remaining = a * (1 - b)^x`
  - `a` = initial diameter delta
  - `b` = inspector-controlled decay parameter
  - `x` = normalized time domain

### Penlight / light source

- Cursor acts as light source.
- Probe radius: 10 mm (configurable).
- Impact is rendered by a shader projector (`PupilReflex/CorneaProjector`).
- The projector is aligned to the camera axis and placed on a camera-perpendicular plane at eye depth.
- The light footprint stays circular in screen space and fills the illuminated area (no hollow center).
- The projector is drawn on top of geometry to prevent occlusion artifacts.
- The shading uses Fresnel and specular terms to mimic cornea reflection.
- Two intensity zones:

  - 0 to 6 mm from center: 100% zone
  - 6 to 10 mm from center: 50% zone

- Stimulus starts when probe boundary first contacts pupil boundary.
- Inspector light intensity range: 1 to 100.

Projector tuning (Inspector, `PupilReflexMouseProbe`):

- `corneaIor`: controls Fresnel strength (default 1.376).
- `baseSpecular`: specular highlight strength.
- `stretchStrength`: ellipse stretch at grazing angles.
- `edgeSoftness`: falloff softness at the edge.

## Pathology Controls

Each eye can be configured independently:

- Reacts / does not react to light.
- Constriction strength (0 to 1).

Use cases:

- Non-reactive eye simulation.
- Partial response simulation.
- Left/right asymmetry simulation.

## Integration Guide (Later Main Merge)

`PupilReflexSandboxBootstrap` supports both modes.

1. Standalone test mode:

   - Keep `autoCreateSandboxPatient = true`.

2. Existing prefab integration mode:

   - Set `autoCreateSandboxPatient = false`.
   - Assign `patientRoot`, `faceRoot`, `leftEyeAnchor`, `rightEyeAnchor`.
   - Keep reflex logic and probe in this module.

This keeps merge impact low and avoids direct modifications to scenario-prefab
scripts at this stage.

## Notes

- `PupilReflexExamController` exposes UnityEvents for left/right/both checked.
- Use those events to connect scenario flow later.
- This template focuses on reflex feature implementation itself.
- Play mode persistence (optional): `PupilReflexSandboxPlayModePersist` can retain changes made during play.