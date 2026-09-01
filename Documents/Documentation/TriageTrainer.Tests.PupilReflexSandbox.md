# <a id="TriageTrainer_Tests_PupilReflexSandbox"></a> Namespace TriageTrainer.Tests.PupilReflexSandbox

### Classes

 [PupilReflexExamController](TriageTrainer.Tests.PupilReflexSandbox.PupilReflexExamController.md)

Tracks bilateral direct light exam completion and exposes UnityEvents
that can be connected to downstream scenario logic.

 [PupilReflexEye](TriageTrainer.Tests.PupilReflexSandbox.PupilReflexEye.md)

Models a single eye with sclera, iris, pupil and cornea layers.
The iris and pupil are rendered as spherical patches that share
the same curvature as the eyeball.

 [PupilReflexMouseProbe](TriageTrainer.Tests.PupilReflexSandbox.PupilReflexMouseProbe.md)

Uses the mouse cursor as a penlight source and samples direct light
impact for each target eye.

 [PupilReflexSandboxBootstrap](TriageTrainer.Tests.PupilReflexSandbox.PupilReflexSandboxBootstrap.md)

Builds and configures a standalone direct pupil light reflex sandbox.
For production integration, disable auto sandbox creation and assign
anchors from an existing patient prefab.

