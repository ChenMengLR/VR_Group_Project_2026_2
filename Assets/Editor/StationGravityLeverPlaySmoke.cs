#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CourseProject;
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using TMPro;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using Object = UnityEngine.Object;

namespace CourseSetup
{
    [InitializeOnLoad]
    public static class StationGravityLeverPlaySmoke
    {
        private const string Key = "Course.GravityLeverSmoke.";
        public static readonly string Output = Path.GetFullPath("CourseValidation/Runtime/GravityLever_20261109");
        private static bool launched;

        static StationGravityLeverPlaySmoke()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += ModeChanged;
            Application.logMessageReceived += RecordError;
        }

        public static void RunCli()
        {
            if (Application.isBatchMode) throw new InvalidOperationException("Use a windowed Editor for Simulator Play.");
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget store);
            SessionState.SetBool(Key + "Desktop", store.SettingsForBuildTarget(BuildTargetGroup.Standalone).InitManagerOnStart);
            SessionState.SetBool(Key + "Background", PlayerSettings.runInBackground);
            var feature = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature<ApiLayersFeature>();
            SessionState.SetBool(Key + "ApiLayers", feature.enabled);
            SessionState.SetInt(Key + "Errors", 0);
            SessionState.SetInt(Key + "Known", 0);
            SessionState.SetString(Key + "Started", DateTime.UtcNow.Ticks.ToString());
            SessionState.SetBool(Key + "Pending", true);
            SessionState.SetBool(Key + "Finishing", false);
            launched = false;
            CourseLauncher.OpenRecoveryRoute();
            feature.enabled = false;
            EditorUtility.SetDirty(feature);
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            EditorApplication.isPlaying = true;
        }

        private static void RecordError(string message, string stack, LogType type)
        {
            if (!SessionState.GetBool(Key + "Pending", false) ||
                (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            string kind = message.Contains("Illegal byte sequence") && stack.Contains("MRUK.OnSharedLibLog") ? "Known" : "Errors";
            SessionState.SetInt(Key + kind, SessionState.GetInt(Key + kind, 0) + 1);
        }

        private static void Update()
        {
            if (!SessionState.GetBool(Key + "Pending", false) || SessionState.GetBool(Key + "Finishing", false)) return;
            long ticks = long.Parse(SessionState.GetString(Key + "Started", "0"));
            if ((DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds > 240)
            {
                Finish(new Report { error = "Simulator test exceeded 240 seconds." });
                return;
            }
            if (EditorApplication.isPlaying && Time.time > 12 && !launched)
            {
                launched = true;
                new GameObject("GravityLeverSmokeRunner").AddComponent<GravityLeverSmokeRunner>();
            }
        }

        public static void Finish(Report report)
        {
            if (SessionState.GetBool(Key + "Finishing", false)) return;
            SessionState.SetBool(Key + "Finishing", true);
            report.generatedUtc = DateTime.UtcNow.ToString("O");
            report.unexpectedErrors = SessionState.GetInt(Key + "Errors", 0);
            report.knownMrukLogDecodeExceptions = SessionState.GetInt(Key + "Known", 0);
            report.passed = string.IsNullOrEmpty(report.error) && report.unexpectedErrors == 0;
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "GravityLever_PlaySmoke.json"), JsonUtility.ToJson(report, true));
            SessionState.SetBool(Key + "Passed", report.passed);
            Debug.Log("STATION_GRAVITY_LEVER_PLAY_" + (report.passed ? "PASS" : "FAIL"));
            EditorApplication.isPlaying = false;
        }

        private static void ModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + "Pending", false)) return;
            CourseLauncher.SetDesktopStartup(SessionState.GetBool(Key + "Desktop", false));
            PlayerSettings.runInBackground = SessionState.GetBool(Key + "Background", false);
            var feature = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature<ApiLayersFeature>();
            feature.enabled = SessionState.GetBool(Key + "ApiLayers", false);
            EditorUtility.SetDirty(feature);
            AssetDatabase.SaveAssets();
            SessionState.SetBool(Key + "Pending", false);
            EditorApplication.Exit(SessionState.GetBool(Key + "Passed", false) ? 0 : 1);
        }

        [Serializable]
        public sealed class Report
        {
            public string generatedUtc;
            public string scope = "Unity Play with Meta XR Simulator. Scripted production control inputs, synthetic Grabbable pointer events, actual physics and hatch coroutine. Separate fixed evidence camera. Physical Quest controls and comfort require headset testing.";
            public bool passed;
            public string[] checks;
            public string error;
            public int unexpectedErrors, knownMrukLogDecodeExceptions;
            public float maximumMeasuredLift, measuredBrakePlanarSpeed;
            public int videoFrames;
            public string videoScope = "10-second silent Unity Play capture of the actual lever and hatch states, with synthetic grab input.";
        }
    }

    [DefaultExecutionOrder(-40)]
    internal sealed class GravityLeverSmokeRunner : MonoBehaviour
    {
        private readonly List<string> checks = new List<string>();
        private readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        private SpaceStationControls controls;
        private SpaceStationMission mission;
        private SpaceStationRoute route;
        private SpaceStationHardware hardware;
        private StationCabinPower cabin;
        private StationAirlockLever lever;
        private StationLowGravity gravity;
        private StationFloatingBody floatingCore;
        private Rigidbody core;
        private Grabbable coreGrab;
        private Transform origin, socket;
        private Camera camera;
        private TMP_Text caption;
        private MediaEncoder encoder;
        private RenderTexture videoTarget;
        private Texture2D videoFrame;
        private Pose corePointer, leverPointer;
        private Vector3 leverRestOffset;
        private Quaternion leverRestRotation;
        private Vector2 leftInput, rightInput;
        private bool focused = true, boostHeld, brakeHeld, done, recording;
        private float waitUntil, measureGroundY, maxLift, brakeSpeed;
        private bool measureLift;
        private int oldCaptureRate, videoFrames;

        private void Start()
        {
            try
            {
                Directory.CreateDirectory(StationGravityLeverPlaySmoke.Output);
                controls = Object.FindFirstObjectByType<SpaceStationControls>();
                mission = Object.FindFirstObjectByType<SpaceStationMission>();
                route = Object.FindFirstObjectByType<SpaceStationRoute>();
                hardware = Object.FindFirstObjectByType<SpaceStationHardware>();
                cabin = Object.FindFirstObjectByType<StationCabinPower>();
                lever = Object.FindFirstObjectByType<StationAirlockLever>();
                gravity = controls.LowGravity;
                core = route.Core;
                coreGrab = core.GetComponent<Grabbable>();
                floatingCore = core.GetComponent<StationFloatingBody>();
                socket = GameObject.Find("EnergySocket_SnapAnchor").transform;
                origin = Object.FindFirstObjectByType<OVRCameraRig>().transform;
                Check(XRSettings.isDeviceActive, "Simulator XR device is active");
                Check(gravity != null && lever != null && floatingCore != null, "Saved scene contains low gravity, floating core and physical lever");
                controls.enabled = false;
                oldCaptureRate = Time.captureFramerate;
                Time.captureFramerate = 30;
                camera = StationHardwarePreview.CreateCamera();
                StationHardwarePreview.Pose(camera, new Vector3(1.5f, 1.62f, 3.25f), new Vector3(3.1f, 1.4f, 1.35f), 70f);
                AddCaption();
                routines.Push(Flow());
                StartCoroutine(CaptureFrames());
            }
            catch (Exception error) { Finish(error.ToString()); }
        }

        private void Update()
        {
            if (done || controls == null) return;
            try
            {
                if (Time.time >= waitUntil)
                {
                    if (routines.Count == 0) { Finish(null); return; }
                    IEnumerator step = routines.Peek();
                    if (!step.MoveNext()) routines.Pop();
                    else if (step.Current is IEnumerator child) routines.Push(child);
                    else if (step.Current is float seconds) waitUntil = Time.time + seconds;
                }
                controls.ProcessInput(leftInput, rightInput, focused, boostHeld, brakeHeld);
                if (measureLift) maxLift = Mathf.Max(maxLift, origin.position.y - measureGroundY);
            }
            catch (Exception error) { Finish(error.ToString()); }
        }

        private IEnumerator Flow()
        {
            mission.ResetMission();
            yield return .5f;
            Check(controls.Locomotor.IsGrounded && !controls.Locomotor.IgnoringVelocity, "Player starts grounded with locomotion available");
            Check(!mission.IsInserted && !mission.IsHatchOpen && hardware.PassageBlocked && !lever.IsAvailable, "Power-off state locks the lever and hatch");
            Check(!floatingCore.IsFloating && core.useGravity, "Core begins resting at the supply bench");
            yield return PullLever(60f, .5f, true);
            Check(!lever.IsLatched && !mission.IsHatchOpen && lever.AngleDegrees < 1f, "Unpowered lever cannot activate the hatch");

            measureGroundY = origin.position.y;
            measureLift = true;
            int initialBoostCount = gravity.BoostCount;
            boostHeld = true;
            yield return .28f;
            Check(origin.position.y > measureGroundY + .025f, "A rising-edge boost produces measurable lift through the locomotor");
            Vector3 airborneStart = origin.position;
            leftInput = Vector2.up;
            yield return .22f;
            Check(PlanarDistance(origin.position, airborneStart) > .025f, "Left stick can steer while airborne");
            brakeHeld = true;
            leftInput = Vector2.zero;
            yield return .2f;
            brakeSpeed = Vector3.ProjectOnPlane(controls.Locomotor.Velocity, Vector3.up).magnitude;
            Check(brakeSpeed < .08f, "Holding X brakes planar drift");
            brakeHeld = false;
            yield return 1.5f;
            Check(gravity.BoostCount == initialBoostCount + 1, "Holding A never repeats the boost automatically");
            Check(maxLift <= gravity.MaximumLift + .065f, "Boost height remains within the authored small-lift bound");
            Check(controls.Locomotor.IsGrounded, "Boost settles back onto the original deck");
            boostHeld = false;
            measureLift = false;
            yield return .15f;

            measureGroundY = origin.position.y;
            measureLift = true;
            boostHeld = true;
            yield return 1.8f;
            Check(maxLift <= gravity.MaximumLift + .065f && controls.Locomotor.IsGrounded,
                "Unbraked boost reaches a bounded apex and lands without a repeat");
            measureLift = false;
            boostHeld = false;
            yield return .15f;

            leftInput = Vector2.up;
            yield return .25f;
            focused = false;
            yield return .15f;
            Check(Vector3.ProjectOnPlane(controls.Locomotor.Velocity, Vector3.up).magnitude < .03f, "Input focus loss stops horizontal movement");
            focused = true;
            leftInput = Vector2.zero;
            yield return .15f;
            boostHeld = true;
            yield return .2f;
            controls.ToggleMenu();
            yield return .25f;
            Check(controls.MenuVisible && !gravity.IsBoosting && !controls.RightTeleport.activeSelf, "Opening the menu cancels boost and teleport aiming");
            controls.CloseMenu();
            boostHeld = false;
            yield return .7f;
            rightInput = Vector2.up;
            yield return .15f;
            Check(Vector3.ProjectOnPlane(controls.Locomotor.Velocity, Vector3.up).magnitude < .03f && !gravity.IsBoosting, "Teleport aiming cancels drift and boost");
            rightInput = Vector2.zero;
            yield return .15f;
            boostHeld = true;
            yield return .2f;
            MovePlayer(route.StartAnchor.position);
            yield return .3f;
            Check(!gravity.IsBoosting && controls.Locomotor.IsGrounded && controls.Locomotor.Velocity.magnitude < .05f,
                "An absolute teleport during a boost clears the flight velocity");
            boostHeld = false;
            mission.ResetMission();
            yield return .35f;
            Check(Vector3.Distance(origin.position, route.StartAnchor.position) < .15f && !gravity.IsBoosting, "Mission reset returns the player to a stable starting position");

            yield return CarryCore(core.position + Vector3.up * .35f, Quaternion.identity, 1f);
            yield return .5f;
            Check(mission.HasAcquiredCore && floatingCore.IsFloating && !core.useGravity, "Releasing a previously grabbed core enables floating physics");
            float releasedHeight = core.position.y;
            yield return .6f;
            Check(Mathf.Abs(core.position.y - releasedHeight) < .3f, "Released core remains within a reachable height over time");
            yield return CarryCore(core.position + Vector3.right * .2f, core.rotation, .6f);
            yield return .3f;
            Check(floatingCore.IsFloating && !core.useGravity && coreGrab.GrabPoints.Count == 0,
                "A second grab and release re-enables bounded floating");
            mission.ResetMission();
            yield return .3f;
            Check(!floatingCore.IsFloating && core.useGravity && !mission.HasAcquiredCore, "Reset restores the resting core and clears floating state");

            yield return DockCore();
            yield return .2f;
            Check(mission.IsInserted && !mission.IsHatchOpen && hardware.PassageBlocked, "Core insertion restores power while retaining a closed hatch");
            Check(cabin.Progress > 0f && cabin.Progress < 1f, "Core insertion starts progressive cabin lighting");
            Check(!floatingCore.IsFloating && core.isKinematic, "Docking stops the floating core physics");
            yield return 3.2f;
            Check(mission.IsPowerReady && lever.IsAvailable && cabin.Progress == 1f, "All lighting circuits complete before the lever becomes available");
            Check(!mission.IsHatchOpen && hardware.PassageBlocked, "Powered hatch stays closed while awaiting a physical pull");
            caption.text = "UNITY PLAY / META XR SIMULATOR\nCORE DOCKED - PULL LEVER TO OPEN";
            Save("Powered_Lever_Closed.png");

            yield return PullLever(30f, .7f, true);
            yield return .8f;
            Check(!lever.IsLatched && lever.AngleDegrees < 1f && !mission.IsHatchOpen, "A partial pull returns to rest without opening the hatch");
            StartVideo();
            yield return 1.5f;
            caption.text = "UNITY PLAY / META XR SIMULATOR\nSYNTHETIC GRIP INPUT - PHYSICAL LEVER ROTATION";
            yield return PullLever(65f, 1.2f, false);
            Check(lever.IsLatched && mission.IsHatchOpening, "Pulling the handle through its threshold starts one hatch sequence");
            ReleaseLever();
            yield return 3.2f;
            Check(mission.IsHatchOpen && hardware.IsOpen && !hardware.PassageBlocked, "The existing double-door animation opens after lever activation");
            caption.text = "UNITY PLAY / META XR SIMULATOR\nAIRLOCK OPEN - EVACUATE";
            Save("Lever_Pulled_Hatch_Open.png");
            yield return 4.5f;
            Check(videoFrames == 300, "Ten-second lever sequence encoded as 300 frames");
            MovePlayer(new Vector3(4.45f, 0f, .75f));
            yield return .4f;
            Check(mission.IsEscaped, "Passing through the open airlock completes the mission");
            mission.ResetMission();
            yield return .4f;
            Check(!lever.IsLatched && lever.AngleDegrees < 1f && !mission.IsInserted && hardware.PassageBlocked, "Completed mission reset returns handle, core, lights and door to standby");

            yield return DockCore();
            yield return 2.7f;
            yield return PullLever(25f, .5f, false);
            Check(lever.IsHeld, "Lever is held during the reset regression");
            mission.ResetMission();
            yield return .3f;
            Check(!lever.IsHeld && lever.Grab.GrabPoints.Count == 0 && lever.AngleDegrees < 1f && !lever.IsAvailable, "Reset releases a held lever and restores its locked rest pose");
            ReleaseLever();
            yield return DockCore();
            yield return 2.7f;
            yield return PullLever(65f, .8f, true);
            yield return .7f;
            Check(mission.IsHatchOpening, "Third cycle reaches an intermediate hatch animation");
            mission.ResetMission();
            yield return 3.2f;
            Check(!mission.IsHatchOpen && !mission.IsInserted && hardware.PassageBlocked && cabin.Progress == 0f && !lever.IsLatched, "Reset during opening cancels the old sequence and returns emergency lighting");
            caption.text = "UNITY PLAY / META XR SIMULATOR\nRESET COMPLETE - POWER OFFLINE";
            Save("Reset_Locked.png");
        }

        private IEnumerator DockCore()
        {
            yield return CarryCore(socket.position, socket.rotation, 1.6f);
        }

        private IEnumerator CarryCore(Vector3 target, Quaternion rotation, float duration)
        {
            corePointer = new Pose(core.position, core.rotation);
            coreGrab.ProcessPointerEvent(new PointerEvent(9911, PointerEventType.Hover, corePointer));
            coreGrab.ProcessPointerEvent(new PointerEvent(9911, PointerEventType.Select, corePointer));
            yield return .1f;
            Pose from = corePointer;
            float start = Time.time;
            while (Time.time - start < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, (Time.time - start) / duration);
                corePointer = new Pose(Vector3.Lerp(from.position, target, t), Quaternion.Slerp(from.rotation, rotation, t));
                coreGrab.ProcessPointerEvent(new PointerEvent(9911, PointerEventType.Move, corePointer));
                yield return null;
            }
            corePointer = new Pose(target, rotation);
            float settleUntil = Time.time + .4f;
            while (Time.time < settleUntil)
            {
                coreGrab.ProcessPointerEvent(new PointerEvent(9911, PointerEventType.Move, corePointer));
                yield return null;
            }
            coreGrab.ProcessPointerEvent(new PointerEvent(9911, PointerEventType.Unselect, corePointer));
            coreGrab.ProcessPointerEvent(new PointerEvent(9911, PointerEventType.Unhover, corePointer));
        }

        private IEnumerator PullLever(float angle, float duration, bool release)
        {
            Vector3 gripCenter = lever.Handle.TransformPoint(lever.Handle.GetComponent<BoxCollider>().center);
            leverRestOffset = gripCenter - lever.Pivot.position;
            leverRestRotation = lever.Handle.rotation;
            leverPointer = new Pose(gripCenter, leverRestRotation);
            lever.Grab.ProcessPointerEvent(new PointerEvent(9921, PointerEventType.Hover, leverPointer));
            lever.Grab.ProcessPointerEvent(new PointerEvent(9921, PointerEventType.Select, leverPointer));
            yield return .1f;
            Vector3 axis = lever.Pivot.right;
            float start = Time.time;
            while (Time.time - start < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, (Time.time - start) / duration);
                Quaternion arc = Quaternion.AngleAxis(angle * t, axis);
                leverPointer = new Pose(lever.Pivot.position + arc * leverRestOffset, arc * leverRestRotation);
                lever.Grab.ProcessPointerEvent(new PointerEvent(9921, PointerEventType.Move, leverPointer));
                yield return null;
            }
            Quaternion full = Quaternion.AngleAxis(angle, axis);
            leverPointer = new Pose(lever.Pivot.position + full * leverRestOffset, full * leverRestRotation);
            lever.Grab.ProcessPointerEvent(new PointerEvent(9921, PointerEventType.Move, leverPointer));
            yield return .15f;
            if (release) ReleaseLever();
        }

        private void ReleaseLever()
        {
            if (lever.Grab.GrabPoints.Count > 0)
                lever.Grab.ProcessPointerEvent(new PointerEvent(9921, PointerEventType.Unselect, leverPointer));
            // Also clear synthetic hover sent while the physical interactables were locked.
            lever.Grab.ProcessPointerEvent(new PointerEvent(9921, PointerEventType.Unhover, leverPointer));
        }

        private void MovePlayer(Vector3 feet)
        {
            controls.Locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(), feet, LocomotionEvent.TranslationType.Absolute));
        }

        private static float PlanarDistance(Vector3 a, Vector3 b) => Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude;

        private void AddCaption()
        {
            var canvas = new GameObject("EvidenceCaption", typeof(Canvas)).GetComponent<Canvas>();
            canvas.transform.SetParent(camera.transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 100;
            var panel = new GameObject("CaptionPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, .85f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, .11f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            caption = new GameObject("Caption", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            caption.transform.SetParent(panel.transform, false);
            caption.rectTransform.anchorMin = Vector2.zero;
            caption.rectTransform.anchorMax = Vector2.one;
            caption.rectTransform.offsetMin = caption.rectTransform.offsetMax = Vector2.zero;
            caption.fontSize = 18f;
            caption.alignment = TextAlignmentOptions.Center;
            caption.color = Color.white;
            caption.text = "UNITY PLAY / META XR SIMULATOR\nAUTOMATED CONTROL AND LEVER TEST";
        }

        private void Save(string name)
        {
            StationHardwarePreview.Save(camera, Path.Combine(StationGravityLeverPlaySmoke.Output, name), 1280, 720);
        }

        private void StartVideo()
        {
            videoTarget = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            videoFrame = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            camera.targetTexture = videoTarget;
            encoder = new MediaEncoder(Path.Combine(StationGravityLeverPlaySmoke.Output, "Lever_Automated_Play.mp4"),
                new VideoTrackAttributes { frameRate = new MediaRational(30, 1), width = 1280, height = 720, includeAlpha = false });
            recording = true;
        }

        private IEnumerator CaptureFrames()
        {
            while (!done)
            {
                yield return new WaitForEndOfFrame();
                if (!recording || done) continue;
                try
                {
                    RenderTexture previous = RenderTexture.active;
                    try
                    {
                        camera.Render();
                        RenderTexture.active = videoTarget;
                        videoFrame.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                        videoFrame.Apply();
                        if (!encoder.AddFrame(videoFrame)) throw new InvalidOperationException("Encoder rejected a frame.");
                    }
                    finally { RenderTexture.active = previous; }
                    videoFrames++;
                    if (videoFrames == 300)
                    {
                        recording = false;
                        encoder.Dispose();
                        encoder = null;
                        camera.targetTexture = null;
                    }
                }
                catch (Exception error) { Finish(error.ToString()); }
            }
        }

        private void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks.Add(message);
            Debug.Log("GRAVITY_LEVER_CHECK " + message);
        }

        private void Finish(string error)
        {
            if (done) return;
            done = true;
            if (encoder != null) { encoder.Dispose(); encoder = null; }
            Time.captureFramerate = oldCaptureRate;
            StationGravityLeverPlaySmoke.Finish(new StationGravityLeverPlaySmoke.Report
            {
                checks = checks.ToArray(), error = error, maximumMeasuredLift = maxLift,
                measuredBrakePlanarSpeed = brakeSpeed, videoFrames = videoFrames
            });
        }

        private void OnDestroy()
        {
            if (encoder != null) { encoder.Dispose(); encoder = null; }
            Time.captureFramerate = oldCaptureRate;
            if (videoTarget != null) Destroy(videoTarget);
            if (videoFrame != null) Destroy(videoFrame);
            if (camera != null) Destroy(camera.gameObject);
        }
    }
}
#endif
