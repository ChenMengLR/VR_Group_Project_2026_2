# Week 10 · Spatial Interfaces and Accessibility

**Lost Station: Restart the Hatch** · Wang Haobin, independent group · 11 November 2026

This submission updates the recovery-route prototype with bounded low-gravity movement, a recoverable floating core, and a manual airlock lever. It also includes the revised four-page English Creative Award manuscript. The controls and status text expose the new sequence: retrieve the core, restore power, pull the lever, and leave through the hatch.

## Submitted materials

- [Quest APK 0.6.0 (5), ARM64](../../Deliverables/LowGravity/Wang_LostStation_0.6.0_Quest.apk) · 112,264,835 bytes. On GitHub, use **Download raw file**.
- [Before/after interaction and interface comparison](Before_After.md), with actual scene captures and the remaining accessibility checks.
- [Four-page English paper — PDF](../Paper_20261111/Wang_LostStation_CreativeAward_20261111.pdf)
- [Editable English paper — DOCX](../Paper_20261111/Wang_LostStation_CreativeAward_20261111.docx)
- [10-second lever and hatch demonstration](../../CourseValidation/Runtime/GravityLever_20261109/Lever_Automated_Play.mp4) · silent Unity Play / Meta XR Simulator capture with scripted input. This clip is supplementary functional evidence, not a physical-headset recording.
- [Low-gravity and lever checks: 37 passed](../../CourseValidation/Runtime/GravityLever_20261109/GravityLever_PlaySmoke.json)
- [Mission regression: 24 passed](../../CourseValidation/Runtime/GravityLever_20261109/StationRoute_PlaySmoke.json)
- [Editor route/collision checks: 30 passed](../../CourseValidation/Runtime/GravityLever_20261109/StationRoute_Validation.json)
- [APK signature, version and packaged-scene verification](../../CourseValidation/Runtime/GravityLever_20261109/Quest_Apk_Verification.json)
- [Unity scene](../../Assets/01_Scenes/Station_RecoveryRoute.unity)
- [File sizes and SHA-256 manifest](Submission_Manifest.json)

## Play and controls

Follow blue route cues to the upper supply bay. Hold Grip to carry the core; releasing it away from the socket enables its bounded drift, so it can be grabbed again. Return to the coupler and release the core into its socket. Wait for the three lighting circuits to recover. Grip the lever beside the hatch and pull downward until it latches. Enter the open hatch to finish. The menu provides help and mission reset.

- Left stick: move / strafe / steer while airborne.
- Right controller A: one short upward boost from the ground; holding A does not repeat it.
- Left controller X, held: brake planar drift and descend.
- Right stick left/right: snap turn 30 degrees. Forward: aim teleport; release: travel.
- Grip: hold the core or grab and pull the lever. Trigger: point and click UI.
- Left Menu: controls/reset panel. B: close panel. Simulator keyboard: Space for boost, Ctrl for braking.

Open in Unity 6000.3.24f1. For PC VR, connect Quest Link/Air Link, then choose `Course > Open Station for Quest Air Link`. For the simulator, use `Course > Exploration Level > Open Recovery Route - Simulator`. The Android package is a separate build; rebuild using `Course > Exploration Level > Build Low Gravity Quest APK`.

## Evaluation and limits

All 37 new-interaction checks, 24 mission-regression checks, and 30 deterministic Editor route checks passed in the saved evidence. These are software assertions, not participant counts. A scripted boost reached 0.1896 m; the planar speed at the brake checkpoint was zero.

On 11 November 2026, the author reported trying the updated version on Quest 2 through Link/Air Link without dizziness or other discomfort. This is an informal developer self-trial. Duration, exact trial date, standardized scores and full-sequence completion were not supplied. It does not establish comfort for other users or native APK execution.

Native Quest installation, standalone performance measurement, and a classmate first-use test/retest remain pending. The manuscript describes the proposed approximately ten-minute peer session as planned. Reach comfort and text readability for different users have not been measured.

## Paper and contribution

**Lost Station: Bounded Low Gravity and Staged Repair Interactions in VR** follows the HCI Korea 2027 English template. It includes three figure groups, four verified references, implementation details, evaluation scope, expected contribution and exhibition plans. Wang Haobin is the sole group member responsible for the prototype and course deliverables; Bora Youn is listed as course supervisor and corresponding author as required by the course.

The APK was inspected as the packaged artifact described above. This source submission excludes unrelated local IDE settings, imported sample assets, old speaking-practice drafts and temporary recordings. It is not a claim that the APK has been reproduced byte-for-byte from a clean clone.
