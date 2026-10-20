# Week 07: E-07 core, coupler and split airlock

Updated 2026-10-20 for *Lost Station: Restart the Hatch*.

## Implemented in the current team scene

`Assets/01_Scenes/Week06_TeamUI_Wang.unity` now contains a coordinated set of
original lightweight sci-fi models:

- The sphere is replaced visually by an E-07 cartridge, with metal cage ribs,
  end caps, a cyan crystal and a rotating internal assembly. The existing Meta
  Grabbable, Rigidbody and select/release audio wiring are retained. One capsule
  collider replaces the sphere collider.
- The workbench socket has a recessed dark well, beveled metal rim, twelve
  indicator segments and three moving retention clamps. The workbench gains a
  metal edge, power track and equipment label.
- The airlock has two armored leaves, four retracting locks, a machined frame,
  status label and passage lights. Locks retract into the panels before the leaves
  slide behind the wall. Each leaf travels 1.02 m sideways.

The 2.65-second sequence is:

1. 0.00–0.45 s: clamps close and the socket segments charge.
2. 0.45–0.85 s: locks retract; the status reports `UNLOCKING HATCH`.
3. 0.85–1.05 s: panels unseal into the wall pocket.
4. 1.05–2.65 s: both panels slide apart. Only on completion do passage lights
   turn green and the doorway collider release.

The mission does not report `HATCH OPEN` or allow its exit completion check until
the animation completes. Reset stops the in-progress coroutine, stops mechanism
audio and restores the core, door leaves, locks, clamps, lamps and doorway collider.
Existing held-core and player-beyond-door reset guards remain in place.

## Sources and rebuilding

No external model/texture download was needed. Meshes and materials are authored
in `Assets/Editor/StationHardwareBuilder.cs` and saved under
`Assets/05_Models/StationHardware` and `Assets/03_Materials/StationHardware`.
The original mechanism WAV is generated with
`python scripts/Generate-Week07HardwareAudio.py`.

Runtime sequencing is in `Assets/02_Scripts/SpaceStationHardware.cs`; the mission
coordinates it in `SpaceStationMission.cs`. The Week 06 scene builder calls the
hardware builder, preserving the separate Week 05 scene. The latest APK version
is 0.3.0 (Android version code 2).

## Verified on this computer

- Unity scene build completed with no script compilation errors.
- Five actual Unity Editor camera renders were inspected: the core/socket,
  powered core/socket, closed door, open door and room overview. These images
  are **Editor previews**, not Quest captures.
- [Windowed Simulator Play report](../../CourseValidation/Runtime/Week07_Hardware_PlaySmoke.json):
  **15 checks passed, 0 unexpected runtime errors**. The report retains one
  known Meta MR Utility Kit native-log decoding exception.
- The Play test verified charge/slide timing, both completed leaf positions,
  the gate's collision state and delayed success status. It also inserted again,
  reset during partial opening, waited longer than the original sequence, and
  confirmed that the door stayed closed and the core became dynamic/grabbable.
- [Six-second animation clip](../../CourseValidation/Runtime/Week07_Hardware_AutomatedPlay.mp4):
  1280×720, 20 fps, 120 frames, silent. This records the actual mission coroutine
  in Play from a fixed evidence camera. The test script positions the core and
  sets the prior-grab flag; **it is not controller input, a human playthrough or
  a headset recording**. The caption is part of this evidence video only.

The new core's actual controller grip, Quest frame rate, hardware haptics, audio
balance and full exit flow still need testing on the borrowed Quest. Do not
replace the required Week 07 headset recording with this clip.

## Open and try the current version

After an Android build, switch the active Build Profile to Windows for the
computer Simulator. Open `Course > Open Week 06 Team UI`, then enter Play.
The older `Open Solo Space Station` menu opens the preserved Week 05 scene.

Grip the E-07 core, bring it to the lit coupler, then release it. Wait for the
clamps and door sequence to finish before moving to the exit. Use `RESET MISSION`
or the existing reset input to replay; return to the main room before resetting
after walking through the door.
