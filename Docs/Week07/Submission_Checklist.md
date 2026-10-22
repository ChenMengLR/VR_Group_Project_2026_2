# Week 07 midterm delivery — Wang

> **Submission update — 22 October 2026:** Current artifacts were uploaded to the group GitHub in commit `3055ae921ff20d3858adfad7a0d840c6e8871039`. The Week 7 Notion card now has **Submitted**, submission date **2026-10-22**, a pinned submission-index link and the individual file links. All persisted after page reload. See the [submission receipt](Submission_Receipt_20261022.md). Earlier local-only and blank-card observations below describe the pre-upload state. Native APK recording and peer evaluation remain pending; the email draft has not been sent and instructor acceptance is unconfirmed.

Project: **Lost Station: Restart the Hatch** / 《失联空间站：重启舱门》
Group: **Wang Haobin, independent student group**
Course source: [Week 07 Midterm Project Presentation](https://app.notion.com/p/7-Midterm-Project-Presentation-3cf7c00fe5ad8131bac3ee94920ce56f)

## Local deliverables updated on 2026-10-22

| File | Purpose | Current verification |
| --- | --- | --- |
| `Wang_VR_Week07_CreativeAward_Draft.docx` | English HCI-form initial paper | Author affiliations, confirmed student email, Bora Youn's corresponding-author line, current controls, and 21 October PC VR evidence added. Two actual recording frames included. Both A4 pages rendered and visually inspected. Native APK and peer evaluation remain Planned. |
| `Wang_VR_Week07_Midterm.pptx` | Six-slide midterm talk, with Chinese presenter notes | Updated controls and evidence, actual PC VR frames, and remaining native/peer checks. All six slides rendered and inspected; package and layout checks passed. |
| `../../Deliverables/Week07/Wang_VR_Week07.apk` | Team-built Quest APK | Standard VR movement and controls added on 2026-10-21; APK size 106,982,987 bytes; `aapt` reads package `com.wanghaobin.loststation`, version `0.4.0` (version code 3), minimum Android SDK 32. SHA256: `B7CD05D4F7B81864A3DC31EE9D7D39E2B68AF4B34043693FB8545D1EAA51FA3D`. New APK installation and real Touch controls remain unverified. |
| `../../CourseValidation/Runtime/Week06_UI_Status_EditorPreview.png`, `Week06_UI_Help_EditorPreview.png` | English UI screenshots | Actual Unity Editor camera renders, visually inspected. These are not Quest screenshots. |

The [hardware upgrade notes](Hardware_Upgrade.md) include five Editor previews and a six-second silent automated Play animation. The core is positioned by the test script; it is not controller input or Quest footage. Fifteen Play checks passed, including a reset during opening. The updated source, report, PPT and APK are the current local deliverables; older Drive copies must be refreshed before sharing.

The current corrected DOCX, six-slide PPTX, exact local 0.4.0 APK, and supplementary PC VR video are also collected in `Deliverables/Week07/Wang_VR_Week07_CurrentProgress_20261022.zip`. Its README identifies the native recording and peer evaluation as outstanding; SHA256.json records the packaged files. This is a preparation bundle, not a claim that all requirements or submission actions are complete.

The prior `CourseValidation/Week05_Simulator_Audio_Demo.mp4` is **Meta XR Simulator** evidence of the earlier gameplay and sound flow. It does not meet the Week 07 requirement for a real-headset playthrough of the new APK.

The 2026-10-20 hardware version, scene, evidence, English DOCX, PPTX and version 0.3.0 Git LFS APK were pushed in [group-repository commit 238a69c](https://github.com/ChenMengLR/VR_Group_Project_2026_2/commit/238a69c). The [APK file page](https://github.com/ChenMengLR/VR_Group_Project_2026_2/blob/main/Deliverables/Week07/Wang_VR_Week07.apk) is an upload location. The 2026-10-21 controls update and version 0.4.0 APK are local deliverables at this stage; the remote file must be updated and checked before it is used as a link to the new version. Earlier DOCX and PPTX copies are saved in a private Drive folder; private Drive links are not submission links until the instructor can open them.

The [controls update](../StationControls_20261021.md) passed 28 deterministic scene/input checks and 6 actual Simulator Play checks. Quest 2 Air Link entry and head tracking were confirmed by the user on 2026-10-20. On 2026-10-21, the user completed a recorded physical Quest 2 **USB Link PC VR** playthrough including Reset, Help, movement, core grabbing/insertion, the airlock animation and exit completion. The [54-second edited video](../../Deliverables/Week07/Wang_VR_Week07_PCLink_Demo.mp4) and [provenance](../../CourseValidation/Runtime/Quest_PCLink_Demo_20261021.md) are saved locally. This is supplementary PC VR evidence: **native APK installation and recording in the headset Camera app remain unverified and required**. It does not establish every control gesture, haptic perception or peer testing.

## Course page checked on 2026-10-22

- The [Wang team page](https://app.notion.com/p/Wang-3f37c00fe5ad802e8a06d052dd994f9c) now exists. Its [Week 7 card](https://app.notion.com/p/End-to-End-Prototype-4367c00fe5ad82769000017b5699cdff) has Week = 7 but an older generic title, "End-to-End Prototype". Follow the detailed Week 07 course module for the actual midterm requirements. Submission Link, Submission Date, and Review Status were blank when checked; no submission was made during this document revision.
- Wang presentation: **22 October 2026, 15:45–15:55 KST**, followed by Q&A **15:55–16:00 KST**. Join by **15:40 KST**. China time is one hour earlier: join 14:40, present 14:45–14:55, Q&A 14:55–15:00.
- Required advance files: English DOCX, team Quest APK, and a recording of that APK installed and played on Quest using the headset Camera app. Email the instructor and attach the same files or accessible download links to the team Week 7 card.
- The advance deadline was **21 October 2026, 23:59 KST / 22:59 China time** and has passed. Late acceptance has not been confirmed. The existing PC VR recording may accompany an honest progress submission, with the native recording explicitly outstanding.
- Student author: **Wang Haobin**, Department of Artificial Intelligence, Dong-A University; **chenmenglr@gmail.com**, confirmed by the student on 22 October. Instructor: **Bora Youn**, course instructor at Dong-A University; **boracles@snu.ac.kr**. Required line: `Corresponding author: Bora Youn (boracles@snu.ac.kr).`
- The group remote was checked at commit `0872569`; its APK LFS hash was `ac6add7b4618882cf8c96d9dceb6209f1312b3ce270b55a0353fdd4d6eb7ceb2` (106,977,811 bytes), different from the current local 0.4.0 APK. Upload and verify the current local artifacts before using remote links.

## Still required for a complete Week 07 submission

1. Borrow a Quest, enable Developer Mode, connect it by USB, and accept the headset's USB debugging prompt. In PowerShell opened at the VR repository root, run:

   ```powershell
   $adbPath = 'D:\Unity\Editors\6000.3.24f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
   & $adbPath devices -l
   ```

   This command must be run again when preparing the native installation; earlier connection checks do not describe the current USB debugging state.
2. When the device is listed as `device`, install the exact APK from this repository root:

   ```powershell
   $apkPath = (Resolve-Path '.\Deliverables\Week07\Wang_VR_Week07.apk').Path
   (Get-FileHash -LiteralPath $apkPath -Algorithm SHA256).Hash
   & $adbPath install -r $apkPath
   ```

   The hash must match the one in the table above. Check the headset's app list for **Lost Station: Restart the Hatch** (usually under Unknown Sources), launch it, and test controller ray, Help, Reset, teleport, grip, core insertion, audio, hatch, and exit. Do not record a successful run until the actual device flow succeeds.
3. Record a continuous Quest headset playthrough with game audio, from start through core insertion and exit. Name it `Wang_VR_Week07_Demo.mp4`. Check video and audio by playing the saved file before sharing it. Record the headset model, app version, date, and any issues in the course validation notes.
4. Complete the Week 06 approximately ten-minute first-use test in [the peer test form](../Week06_UI_Peer_Test.md), fix the main issue, and retest with the same classmate. In the Week 07 paper, the course permits an unfinished evaluation to be marked **Planned**. Do not invent observations or treat this plan as a completed Week 06 test.
5. Send the **corrected English DOCX**, **current team APK**, and available clearly labeled evidence to `boracles@snu.ac.kr`, and attach the same files or accessible download links to the team Week 7 card. If the native recording is still missing, say so explicitly. The deadline has passed; teacher acceptance of a late or partial submission is unconfirmed. Record the actual submission date only after submission.

## Instructor email draft for the current partial evidence package

**To:** `boracles@snu.ac.kr`
**Subject:** `Wang | VR Week 07 midterm draft, Quest APK and demo`

> Dear Professor Youn,
> I am Wang Haobin, working as an independent group on *Lost Station: Restart the Hatch*. I apologize for the late submission. Attached or linked are my revised English paper draft, the team-built Quest APK version 0.4.0, and a supplementary 54-second Quest 2 USB Link PC VR demonstration. The video shows actual headset and controller play through the Windows Unity project, from reset and Help to core insertion, the airlock animation, and mission completion. Standalone APK installation and the required in-headset APK recording remain pending. The paper marks native-device checks and peer evaluation as Planned.
> Paper: [insert accessible DOCX link]
> APK: [insert accessible APK link]
> Supplementary PC VR demo: [insert accessible MP4 link]
> May I submit these current materials and provide the native APK recording when it is ready? I will present in the assigned Wang slot at 15:45 KST on 22 October.
> Thank you,
> Wang Haobin

Replace the link placeholders with verified accessible files before sending. This email is a draft, not a sent message. A local revision, Git upload, or prepared folder does not establish course submission or instructor acceptance.
