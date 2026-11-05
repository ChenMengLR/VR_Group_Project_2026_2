# Week 9 · Quest Build and Issue Log

**Lost Station: Restart the Hatch** · Wang Haobin, independent group · 5 November 2026

## Materials

- [Download the Quest APK](../../Deliverables/Week09/Wang_LostStation_Week09_Quest.apk) · version 0.5.0 (4), ARM64, 112,247,043 bytes. On GitHub, select **Download raw file** to save the APK.
- [Package verification](../../CourseValidation/Runtime/CabinPower_20261105/Quest_Apk_Verification.json) includes the SHA-256 checksum, signing check and confirmation that the APK contains the recovery-route scene with all three power circuits.
- [Issue log, retest results and remaining headset checks](Issue_Log.md)
- [Before and after power restoration, captured in Unity Play with Meta XR Simulator](../../CourseValidation/Runtime/CabinPower_20261105/Cabin_Power_Comparison.jpg)
- [All 23 simulator checks](../../CourseValidation/Runtime/CabinPower_20261105/StationRoute_PlaySmoke.json) and [editor lighting checks](../../CourseValidation/Runtime/CabinPower_20261105/CabinVisualQA.json)
- [Unity scene](../../Assets/01_Scenes/Station_RecoveryRoute.unity)

## Play the level

Follow the blue route up the ramp. Hold Grip to carry the reserve core back to the coupler, then release it into the socket. Watch the lights come on from the dock through the main passage to the upper bay. Wait for the hatch to open, then follow green to the exit. Reset from the menu to replay.

To rebuild in Unity, switch the active build target to Android and choose `Course > Exploration Level > Build Week 09 Quest APK`.

## Contribution and test scope

Wang Haobin is the sole group member responsible for the level, cabin art, power sequence, Quest package and issue log. This submission includes the current Android build and editor/simulator retest evidence. Installation, physical controller input, navigation comfort and frame timing on a standalone Quest 2 are pending.
