# Solo Space Station — simulated controller flow

Date: 2026-10-01. Unity 6000.3.24f1, Meta XR Simulator and Operator 205, Windows Editor, `Solo_SpaceStation.unity` opened through temporary `V:\` alias. This is an automated computer simulation, not a Quest device test or a student-operated demonstration.

## Continuous successful Play session

1. The official `MovingSetting.ControllerMovement` was set to `Teleport` for this runtime session. Operator injected the right controller grip orientation and Thumbstick Y=1. The live `TeleportInteractor` reported `Hover` on `RepairPad_TeleportHotspot` at `(1.500, 0.089, 0.750)`. Releasing Thumbstick Y to 0 moved the headset from `(0.000, 1.700, 0.000)` to `(1.500, 1.789, 0.846)`. The rig was not moved directly by the test.
2. Operator moved the right controller grip pose to the energy core. The live `GrabInteractor` entered `Hover`; Grip=1 changed it to `Select`, with `EnergyCore_GrabAndRelease` selected and `Grabbable.GrabPoints.Count=1`.
3. While Grip stayed pressed, Operator moved the controller grip pose from local-floor `(-0.350, 0.881, -0.4535)` to `(0.350, 0.921, -0.4535)`. The core followed to the socket at world `(1.8500, 1.0100, 1.3000)`; measured core/socket distance was approximately `1.69e-7` m.
4. Grip=0 produced `GrabPoints.Count=0`, `SpaceStationMission.IsInserted=true`, and opened `SlidingHatch` to local Y=`3.74` (closed Y=`1.24`). The core remained at the socket.
5. Operator then smoothly changed the simulated headset pose over two seconds, representing physical movement through the open passage. The head reached `(4.450, 1.789, 0.750)` and `SpaceStationMission.IsEscaped=true`; the status read `MISSION COMPLETE / B / R TO REPLAY`. This exit step used simulated headset position, not a controller locomotion input.

The final camera frame is [SoloStation_ControllerFlow_Final.png](SoloStation_ControllerFlow_Final.png). The corresponding machine-readable summary is [SoloStation_ControllerFlow_20261001.json](SoloStation_ControllerFlow_20261001.json).

## Test boundary

An earlier Play session confirmed the same repair-pad teleport but unexpectedly exited via Unity's toolbar Play toggle before grabbing. The Editor was restarted into Play once; the entire five-step flow above then completed in one uninterrupted session. No APK installation, Quest controls, comfort, performance, or headset passthrough was tested. This simulation does not establish whether a teacher accepts it as a substitute for a Quest recording.
