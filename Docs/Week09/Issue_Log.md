# Issue log and retest results · 5 November 2026

Scene: `Station_RecoveryRoute`. Each result below identifies where it was observed. No standalone Quest 2 session has been recorded for this revision.

## Visual issues observed and fixed

1. **The observation-window frame covered part of the sector label.** The label was moved to the front of the frame. The lower row of the [final paired screenshots](../../CourseValidation/Runtime/CabinPower_20261105/Cabin_Power_Comparison.jpg) shows the full sector label under emergency and restored lighting. Result: fixed in the Unity Play capture.
2. **Solid teleport discs competed visually with the floor route.** The six landing markers now use segmented rings. The original landing-target colliders were retained. The upper row of the same capture shows a ring on the entrance deck. Result: the visual change is visible in Unity Play. Physical thumbstick targeting accuracy is pending.

## Functional retest

- **Input and power.** Synthetic pointer select/release exercised the production grab interaction. Picking the core up kept the cabin on emergency power. Releasing it into the coupler secured it, started the three-circuit lighting sequence and allowed the hatch to open. The evacuation sign turned green.
- **Navigation and recovery.** The player started grounded. Entering the bounded exit completed the task. A reset returned player and core to their start locations. A second test reset during restoration and verified that the door stayed closed and emergency lighting returned. The simulator used scripted positioning for these checks.
- **Lighting and performance.** Two baked lightmaps and 78 light probes provide fixed lighting. Three local fill lights change during restoration. Matched editor captures verified the change in brightness at the entrance and dock. Standalone frame time and FPS are still to be measured.

The [simulator report](../../CourseValidation/Runtime/CabinPower_20261105/StationRoute_PlaySmoke.json) records 23 passed checks, zero unexpected errors and one known Meta MRUK log-decoding exception. The [editor report](../../CourseValidation/Runtime/CabinPower_20261105/CabinVisualQA.json) records the separate lighting measurements.

## Standalone Quest 2 retest to complete

Install this week's APK and launch from the headset. With physical controllers, walk and teleport on both ramps, grab and release the core at the socket, observe the whole lighting and hatch sequence, and finish at the exit. Repeat once with a mid-sequence reset. Record any missed inputs, navigation problems, and frame timing in both lighting states. For each reproduced issue, keep the steps, severity, fix and same-device retest result. Device-specific findings remain pending.
