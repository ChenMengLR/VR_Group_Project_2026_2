#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CourseProject;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace CourseSetup
{
    /// <summary>
    /// A short real Play session in the Windows Meta XR Simulator. This checks
    /// initialization and first-frame scene state; it does not simulate grab input.
    /// </summary>
    [InitializeOnLoad]
    public static class SoloStationPlaySmoke
    {
        private const string Prefix = "CourseSetup.SoloStationPlaySmoke.";

        [Serializable]
        private sealed class Report
        {
            public string generatedUtc;
            public string verification = "Windows Editor Play with Meta XR Simulator; no controller input or Quest hardware test";
            public bool simulatorSelected;
            public bool xrDeviceActive;
            public bool xrLoaderActive;
            public bool cameraPresent;
            public int gameFrames;
            public Vector3 rigPosition;
            public Vector3 headPosition;
            public Vector3 headForward;
            public bool eyeHeightPlausible;
            public bool missionPresent;
            public bool coreDynamic;
            public bool hatchClosed;
            public bool spatialHumPlaying;
            public bool screenshotSaved;
            public int errorCount;
            public string[] errors;
            public bool passed;
        }

        static SoloStationPlaySmoke()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            Application.logMessageReceived += RecordError;
        }

        [MenuItem("Course/Run Solo Space Station Play Smoke", priority = 24)]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Wait for imports, then start the Play smoke test outside Play Mode.");
            if (Application.isBatchMode)
                throw new InvalidOperationException("This Simulator smoke test needs a windowed Unity Editor.");
            CourseLauncher.OpenSoloStation();
            SessionState.SetBool(Prefix + "RunInBackground", PlayerSettings.runInBackground);
            PlayerSettings.runInBackground = true;
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            var apiLayers = settings == null ? null : settings.GetFeature<ApiLayersFeature>();
            if (apiLayers != null)
            {
                SessionState.SetBool(Prefix + "ApiWasEnabled", apiLayers.enabled);
                apiLayers.enabled = false;
                EditorUtility.SetDirty(apiLayers);
                AssetDatabase.SaveAssets();
            }
            SessionState.SetBool(Prefix + "Pending", true);
            SessionState.SetBool(Prefix + "Finished", false);
            SessionState.SetInt(Prefix + "ErrorCount", 0);
            SessionState.SetString(Prefix + "Started", DateTime.UtcNow.Ticks.ToString());
            EditorApplication.isPlaying = true;
        }

        public static void RunCli()
        {
            SessionState.SetBool(Prefix + "Cli", true);
            Run();
        }

        private static void RecordError(string message, string stack, LogType type)
        {
            if (!SessionState.GetBool(Prefix + "Pending", false)) return;
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                SessionState.SetInt(Prefix + "ErrorCount", SessionState.GetInt(Prefix + "ErrorCount", 0) + 1);
        }

        private static void Update()
        {
            if (!SessionState.GetBool(Prefix + "Pending", false)) return;
            if (!long.TryParse(SessionState.GetString(Prefix + "Started", "0"), out long ticks)) return;
            double elapsed = (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds;
            if (elapsed > 90)
            {
                FinishWithError("Play smoke timed out after 90 seconds.");
                return;
            }
            if (EditorApplication.isPlaying && elapsed >= 25 && !SessionState.GetBool(Prefix + "Finished", false))
                Evaluate();
        }

        private static void Evaluate()
        {
            SessionState.SetBool(Prefix + "Finished", true);
            var report = new Report { generatedUtc = DateTime.UtcNow.ToString("O") };
            try
            {
                var runtime = Environment.GetEnvironmentVariable("XR_RUNTIME_JSON");
                report.simulatorSelected = runtime != null &&
                    string.Equals(Path.GetFileName(runtime), "meta_openxr_simulator.json", StringComparison.OrdinalIgnoreCase);
                report.xrDeviceActive = XRSettings.isDeviceActive;
                report.xrLoaderActive = XRGeneralSettings.Instance != null &&
                    XRGeneralSettings.Instance.Manager != null &&
                    XRGeneralSettings.Instance.Manager.activeLoader != null;
                report.gameFrames = Time.frameCount;
                var rig = UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>();
                var camera = rig == null ? null : rig.centerEyeAnchor.GetComponent<Camera>();
                report.cameraPresent = camera != null && camera.isActiveAndEnabled;
                if (rig != null)
                {
                    report.rigPosition = rig.transform.position;
                    report.headPosition = rig.centerEyeAnchor.position;
                    report.headForward = rig.centerEyeAnchor.forward;
                    report.eyeHeightPlausible = report.headPosition.y > 1.0f && report.headPosition.y < 2.2f;
                }
                var mission = UnityEngine.Object.FindFirstObjectByType<SpaceStationMission>();
                report.missionPresent = mission != null && !mission.IsInserted;
                var core = GameObject.Find("EnergyCore_GrabAndRelease");
                var body = core == null ? null : core.GetComponent<Rigidbody>();
                report.coreDynamic = body != null && body.useGravity && !body.isKinematic;
                var hatch = GameObject.Find("SlidingHatch");
                report.hatchClosed = hatch != null && Mathf.Abs(hatch.transform.localPosition.y - 1.24f) < 0.01f;
                var hum = GameObject.Find("SocketSpatialAudio");
                report.spatialHumPlaying = hum != null &&
                    hum.GetComponents<AudioSource>().Any(a => a.isPlaying && a.spatialBlend > 0.99f);
                report.screenshotSaved = camera != null && SaveCamera(camera);
                report.errorCount = SessionState.GetInt(Prefix + "ErrorCount", 0);
                report.passed = report.simulatorSelected && report.xrDeviceActive && report.xrLoaderActive &&
                    report.cameraPresent && report.gameFrames >= 30 && report.eyeHeightPlausible && report.missionPresent && report.coreDynamic &&
                    report.hatchClosed && report.spatialHumPlaying && report.screenshotSaved && report.errorCount == 0;
                report.errors = report.passed ? Array.Empty<string>() : new[] { "One or more Play smoke checks failed; inspect the booleans and Editor log." };
            }
            catch (Exception ex)
            {
                report.errors = new[] { ex.ToString() };
                report.passed = false;
            }
            SaveReport(report);
            SessionState.SetBool(Prefix + "Passed", report.passed);
            Debug.Log("SOLO_STATION_PLAY_SMOKE_" + (report.passed ? "PASS" : "FAIL"));
            EditorApplication.isPlaying = false;
        }

        private static bool SaveCamera(Camera camera)
        {
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                var path = Path.Combine(OutputDirectory(), "SoloStation_PlaySmoke.png");
                File.WriteAllBytes(path, image.EncodeToPNG());
                return true;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void FinishWithError(string message)
        {
            if (SessionState.GetBool(Prefix + "Finished", false)) return;
            SessionState.SetBool(Prefix + "Finished", true);
            SessionState.SetBool(Prefix + "Passed", false);
            SaveReport(new Report { generatedUtc = DateTime.UtcNow.ToString("O"), errors = new[] { message }, passed = false });
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            else ExitCli(false);
        }

        private static void PlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Prefix + "Pending", false)) return;
            bool passed = SessionState.GetBool(Prefix + "Passed", false);
            SessionState.SetBool(Prefix + "Pending", false);
            RestoreApiLayers();
            PlayerSettings.runInBackground = SessionState.GetBool(Prefix + "RunInBackground", false);
            AssetDatabase.SaveAssets();
            ExitCli(passed);
        }

        private static void RestoreApiLayers()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            var apiLayers = settings == null ? null : settings.GetFeature<ApiLayersFeature>();
            if (apiLayers == null) return;
            apiLayers.enabled = SessionState.GetBool(Prefix + "ApiWasEnabled", false);
            EditorUtility.SetDirty(apiLayers);
            AssetDatabase.SaveAssets();
        }

        private static void ExitCli(bool passed)
        {
            if (!SessionState.GetBool(Prefix + "Cli", false)) return;
            SessionState.SetBool(Prefix + "Cli", false);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static string OutputDirectory()
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "CourseValidation", "Runtime"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void SaveReport(Report report)
        {
            File.WriteAllText(Path.Combine(OutputDirectory(), "SoloStation_PlaySmoke.json"), JsonUtility.ToJson(report, true));
        }
    }
}
#endif
