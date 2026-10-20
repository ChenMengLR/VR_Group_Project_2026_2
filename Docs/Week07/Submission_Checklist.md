# Week 07 midterm delivery — Wang

Project: **Lost Station: Restart the Hatch** / 《失联空间站：重启舱门》
Group: **Wang Haobin, independent student group**
Course source: [Week 07 Midterm Project Presentation](https://app.notion.com/p/7-Midterm-Project-Presentation-3cf7c00fe5ad8131bac3ee94920ce56f)

## Files ready on 2026-10-20

| File | Purpose | Current verification |
| --- | --- | --- |
| `Wang_VR_Week07_CreativeAward_Draft.docx` | English HCI-form initial paper | Two A4 pages rendered and visually inspected; Quest and peer evaluation are explicitly Planned. |
| `Wang_VR_Week07_Midterm.pptx` | Six-slide midterm talk, with Chinese presenter notes | All slides rendered and inspected. About four minutes of paper explanation, then a six-minute prototype demo cue sheet. |
| `../../Deliverables/Week07/Wang_VR_Week07.apk` | Team-built Quest APK | Final UI scene rebuilt successfully on 2026-10-20; `aapt` reads package `com.wanghaobin.loststation`, version `0.2.0`, minimum Android SDK 32. SHA256: `CABFE917D8A1915BA79A858A3A1A5F789D0C44D232FB86BCCEAB7141F74BE9F2`. Device installation and runtime remain unverified until Quest is connected. |
| `../../CourseValidation/Runtime/Week06_UI_Status_EditorPreview.png`, `Week06_UI_Help_EditorPreview.png` | English UI screenshots | Actual Unity Editor camera renders, visually inspected. These are not Quest screenshots. |

The prior `CourseValidation/Week05_Simulator_Audio_Demo.mp4` is **Meta XR Simulator** evidence of the earlier gameplay and sound flow. It does not meet the Week 07 requirement for a real-headset playthrough of the new APK.

The code, scene, evidence and Git LFS APK were pushed in [group-repository commit 44dc15a](https://github.com/ChenMengLR/VR_Group_Project_2026_2/commit/44dc15a235d6e4b3a222c08ae6789f41cd856480). The [APK file page](https://github.com/ChenMengLR/VR_Group_Project_2026_2/blob/main/Deliverables/Week07/Wang_VR_Week07.apk) is an upload location; the instructor's access and download should still be checked before emailing it. The English DOCX and PPTX are also saved in a private Drive folder; private Drive links are not submission links until the instructor can open them.

## Still required for a complete Week 07 submission

1. Borrow a Quest, enable Developer Mode, connect it by USB, and accept the headset's USB debugging prompt. In PowerShell opened at the VR repository root, run:

   ```powershell
   $adbPath = 'D:\Unity\Editors\6000.3.24f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
   & $adbPath devices -l
   ```

   The current 2026-10-20 check showed no attached device.
2. When the device is listed as `device`, install the exact APK from this repository root:

   ```powershell
   $apkPath = (Resolve-Path '.\Deliverables\Week07\Wang_VR_Week07.apk').Path
   (Get-FileHash -LiteralPath $apkPath -Algorithm SHA256).Hash
   & $adbPath install -r $apkPath
   ```

   The hash must match the one in the table above. Check the headset's app list for **Lost Station: Restart the Hatch** (usually under Unknown Sources), launch it, and test controller ray, Help, Reset, teleport, grip, core insertion, audio, hatch, and exit. Do not record a successful run until the actual device flow succeeds.
3. Record a continuous Quest headset playthrough with game audio, from start through core insertion and exit. Name it `Wang_VR_Week07_Demo.mp4`. Check video and audio by playing the saved file before sharing it. Record the headset model, app version, date, and any issues in the course validation notes.
4. Ask one classmate to do the approximately ten-minute first-use task in [Week 06 peer test form](../Week06_UI_Peer_Test.md); record factual behavior and severity. Fix the highest-priority issue and have the same classmate repeat the same task. Do not label this as completed before their actual test and retest.
5. Send the **English DOCX**, **team APK**, and **real Quest video** to `boracles@snu.ac.kr` as files or accessible download links by **2026-10-21 23:59 Korea time** (2026-10-21 22:59 China time), and attach the same deliverables to the correct team Week 07 card. The new course module currently does not show a Wang card or a Wang presentation slot; confirm the team's assigned submission card and time with the instructor. The page body says **2026-10-22 15:00–15:45 Korea time**, while its date property shows a conflicting time.

## Instructor email draft (send after video exists)

**To:** `boracles@snu.ac.kr`
**Subject:** `Wang | VR Week 07 midterm draft, Quest APK and demo`

> Dear Professor Youn,
> I am Wang Haobin, working as an independent group on *Lost Station: Restart the Hatch*. Attached or linked are our English midterm paper draft, the team-built Quest APK, and a continuous Quest headset playthrough of that APK. The Week 06 team scene adds an English mission console with status, Help, and Reset controls; the paper distinguishes completed simulator checks from the headset and first-use findings.
> Paper: [insert accessible DOCX link]
> APK: [insert accessible APK link]
> Quest demo: [insert accessible MP4 link]
> Could you also confirm the correct Week 06/07 card and my group's presentation time? The current module list does not show Wang.
> Thank you,
> Wang Haobin

Revise the email's test sentence to match the actual Quest and classmate results before sending. The preparation of an email draft or Drive folder is not itself course submission.
