# Week 07 — Wang midterm materials

**Lost Station: Restart the Hatch** — Wang Haobin, independent group.

Current materials prepared on **22 October 2026**:

- [English paper (DOCX)](Wang_VR_Week07_CreativeAward_Draft.docx)
- [Presentation (PPTX, six English slides with Chinese presenter notes)](Wang_VR_Week07_Midterm.pptx)
- [Quest APK 0.4.0 / version code 3](../../Deliverables/Week07/Wang_VR_Week07.apk)
- [54-second physical Quest 2 USB Link PC VR demonstration (MP4)](../../Deliverables/Week07/Wang_VR_Week07_PCLink_Demo.mp4)
- [All four files, README and SHA256 manifest (ZIP)](../../Deliverables/Week07/Wang_VR_Week07_CurrentProgress_20261022.zip)

For binary files on GitHub, use **Download raw file**. APK, MP4 and ZIP use Git LFS. A Unity checkout requires Git LFS: clone the repository, then run `git lfs pull`.

## Implemented flow and controls

Enter the station, read the English mission/help panel, move or teleport to the workbench, hold Grip to pick up the E-07 energy core, release it into the matching socket, wait for the animated split hatch, and reach the exit. Spatial game audio accompanies interaction and completion.

- Left stick: head-relative walking and strafing.
- Right stick: 30-degree snap turns; push forward and release for teleportation.
- Grip: hold to grab, release to drop; Trigger: UI selection.
- Left Menu: controls panel; right B: close panel; RESET MISSION: restart.

Open `Assets/01_Scenes/Week06_TeamUI_Wang.unity` in Unity 6000.3.24f1. For PC VR, connect Quest Link/Air Link, select the Meta desktop OpenXR runtime, switch the Unity build target to Windows, and use `Course > Open Station for Quest Air Link`, then Play. See the [repository instructions](../../README.md).

## Evidence and remaining work

The MP4 records the student's real Quest 2 headset/controller play on **21 October 2026 through USB Link and Windows Unity**. It retains game audio and shows reset, help, movement, grabbing/insertion, hatch animation, and mission completion. The continuous excerpt has no internal cuts or speed changes. [Recording provenance](../../CourseValidation/Runtime/Quest_PCLink_Demo_20261021.md).

**The required standalone APK installation, native Quest playthrough and headset Camera recording remain outstanding.** The approximately ten-minute classmate usability test, fixes and same-user retest remain Planned. The paper explicitly distinguishes completed PC VR evidence from these planned checks. Haptic perception, comfort and native-device performance are not established by the video.

APK SHA256: `B7CD05D4F7B81864A3DC31EE9D7D39E2B68AF4B34043693FB8545D1EAA51FA3D`.

Contribution: Wang Haobin is the sole group member responsible for project integration, headset demonstration and presentation; implementation and document preparation were AI-assisted. Instructor feedback will be recorded after it is received.

This is a current-progress submission after the 21 October advance deadline. Uploading files or registering the Notion card does not establish instructor acceptance of late/partial evidence. The ZIP's preparation-time README and report may describe the earlier local-only state; the repository and submission receipt record the subsequent upload.

## Course card

[Wang — Week 7 card](https://app.notion.com/p/End-to-End-Prototype-4367c00fe5ad82769000017b5699cdff) (Week property = 7). Detailed requirements and the unsent instructor email draft are in the [submission checklist](Submission_Checklist.md).
