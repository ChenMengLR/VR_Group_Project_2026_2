# Quest 2 USB Link gameplay recording — 2026-10-21

## Delivered video

- [Edited gameplay video](../../Deliverables/Week07/Wang_VR_Week07_PCLink_Demo.mp4): 54 seconds, 1280 × 720, 30 fps, H.264 video and AAC 48 kHz stereo audio; 18,141,058 bytes.
- [Machine-readable provenance](Quest_PCLink_Demo_20261021.json) records the source and output hashes, exact trim, capture method and evidence limits.
- Local unedited source: `Logs/Recording_20261021/Quest_PC_Actual_Take01.mp4` (83.73 seconds). The raw video, separate WAV/MKV and recorder logs remain local under the ignored Logs directory.

## Method and visible results

The user wore a physical Meta Quest 2 and operated the controllers. The headset ran the Windows Unity project through **USB Link / PC VR**. Oculus Mirror was captured with the Realtek playback loopback. The user confirmed hearing game audio before the full recording. The Windows Oculus headphone endpoint was unavailable, so the recorder did not select it.

The final video is a single continuous excerpt of source seconds **18.5–72.5**, at its original speed. It includes the transition from the previous completed mission into a new run via Reset, the Help panel toggle, movement, grabbing and repositioning the core, insertion, green power feedback, the split airlock opening, entering the exit and the visible `MISSION COMPLETE` state. The first core repositioning attempt remains in the video.

Editing removed only footage before and after this excerpt. English title text is shown for four seconds and `Quest 2 | USB Link | PC VR` stays visible. Game audio was raised by 4 dB; no music or narration was added. The movement comfort vignette remains as rendered by the game.

## Verification and limits

- Source and final video decoded to the end without reported decoding errors.
- Final video contains 1,620 frames / 54.00 seconds. Its beginning, reset/help sequence, insertion, door animation and completion frames were visually inspected.
- Final audio is non-silent; decoded peak is −2.2 dBFS and mean level is −34.0 dBFS. These measurements establish signal and headroom, not an independent listening evaluation of every sound cue.
- This is **not a native Quest APK recording**, does not prove the supplied APK was installed, and does not complete the Week 07 requirement to record that APK in the headset Camera app.
- It does not establish haptic perception, exact snap-turn angle, all teleport gestures, or a classmate usability test/retest.
- Files are saved locally. No course email, Notion upload, Git commit or Git push was performed as part of recording/editing.

The user explicitly chose direct computer Link recording after the native-APK requirement had been explained. The submission checklist keeps the native-headset recording task open.
