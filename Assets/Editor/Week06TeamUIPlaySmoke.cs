#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourseProject;
using Oculus.Interaction;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace CourseSetup
{
    /// <summary>
    /// Runs the Week 06 team scene in a windowed Windows Editor with Meta XR
    /// Simulator. UI button events are invoked synthetically; this is not a
    /// controller-ray click, first-use study, or Quest hardware validation.
    /// </summary>
    [InitializeOnLoad]
    public static class Week06TeamUIPlaySmoke
    {
        private const string Prefix = "CourseSetup.Week06TeamUIPlaySmoke.";
        private const string InitialStatus = "STATION OFFLINE\nGRAB THE CORE";
        private const string ReportName = "Week06_TeamUI_PlaySmoke.json";
        private const string ScreenshotName = "Week06_TeamUI_PlaySmoke.png";

        [Serializable]
        private sealed class Report
        {
            public string generatedUtc;
            public string scene;
            public string verification = "Windowed Windows Editor Play with Meta XR Simulator. Buttons were invoked through Button.onClick.Invoke(), not clicked with a controller ray. No Quest or human first-use test.";
            public string runtimeManifest;
            public bool simulatorSelected;
            public bool xrLoaderActive;
            public bool xrDeviceActive;
            public int gameFrames;
            public bool gameCameraActive;
            public bool oneEventSystem;
            public bool onePointableCanvasModule;
            public bool oneActiveAudioListener;
            public int missingScriptCount;
            public bool worldSpaceCanvasReady;
            public bool uiInGameCameraView;
            public string initialMissionStatus;
            public string initialUiStatus;
            public bool initialStatusMatches;
            public bool helpButtonShowPassed;
            public bool helpButtonHidePassed;
            public bool resetButtonRestoredCore;
            public bool resetButtonRestoredStatus;
            public bool screenshotSaved;
            public int errorCount;
            public int knownSdkLogDecodeExceptionCount;
            public int unexpectedErrorCount;
            public string[] failedChecks;
            public bool passed;
        }

        static Week06TeamUIPlaySmoke()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            Application.logMessageReceived += RecordError;
        }

        [MenuItem("Course/Week 06/Run Team UI Play Smoke", priority = 63)]
        public static void Run()
        {
            if (Application.isBatchMode)
                throw new InvalidOperationException("Run this Meta XR Simulator smoke in a windowed Unity Editor.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Switch the active Build Profile to Windows x64 after building the Android APK.");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Stop Play and wait for script and asset imports before running the smoke.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Week06TeamUIBuilder.ScenePath) == null)
                throw new FileNotFoundException("Build the Week 06 team UI scene first.", Week06TeamUIBuilder.ScenePath);
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                if (EditorSceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save the current scene before switching to Week 06. No unsaved changes were discarded.");

            SessionState.SetBool(Prefix + "Pending", false);
            SessionState.SetBool(Prefix + "Finished", false);
            SessionState.SetBool(Prefix + "Passed", false);
            SessionState.SetInt(Prefix + "ErrorCount", 0);
            SessionState.SetInt(Prefix + "KnownSdkLogDecodeExceptionCount", 0);
            SessionState.SetInt(Prefix + "UnexpectedErrorCount", 0);
            CaptureSettings();
            try
            {
                CourseLauncher.OpenWeek06TeamUI();
                // Match the known working SoloStation Simulator smoke: the Meta
                // API layer can intercept desktop OpenXR startup in this scene.
                var apiLayers = GetApiLayers();
                if (apiLayers != null)
                {
                    apiLayers.enabled = false;
                    EditorUtility.SetDirty(apiLayers);
                    AssetDatabase.SaveAssets();
                }
                PlayerSettings.runInBackground = true;
                SessionState.SetString(Prefix + "Started", DateTime.UtcNow.Ticks.ToString());
                SessionState.SetBool(Prefix + "Pending", true);
                EditorApplication.isPlaying = true;
            }
            catch
            {
                SessionState.SetBool(Prefix + "Pending", false);
                RestoreSettings();
                throw;
            }
        }

        /// <summary>Use -executeMethod CourseSetup.Week06TeamUIPlaySmoke.RunCli without -batchmode or -quit.</summary>
        public static void RunCli()
        {
            SessionState.SetBool(Prefix + "Cli", true);
            try { Run(); }
            catch (Exception ex)
            {
                SaveReport(new Report
                {
                    generatedUtc = DateTime.UtcNow.ToString("O"),
                    scene = Week06TeamUIBuilder.ScenePath,
                    failedChecks = new[] { "Setup failed: " + ex },
                    passed = false
                });
                Debug.LogError("WEEK06_TEAM_UI_PLAY_SMOKE_SETUP_FAIL: " + ex);
                ExitCli(false);
            }
        }

        private static void CaptureSettings()
        {
            SessionState.SetBool(Prefix + "SettingsCaptured", false);
            XRGeneralSettingsPerBuildTarget store;
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out store) || store == null)
                throw new InvalidOperationException("XR general settings are missing for Windows Standalone.");
            var general = store.SettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (general == null)
                throw new InvalidOperationException("Windows Standalone XR settings are missing.");
            SessionState.SetBool(Prefix + "DesktopStartupWasEnabled", general.InitManagerOnStart);
            SessionState.SetBool(Prefix + "RunInBackgroundWasEnabled", PlayerSettings.runInBackground);
            var apiLayers = GetApiLayers();
            SessionState.SetBool(Prefix + "ApiLayersPresent", apiLayers != null);
            if (apiLayers != null)
                SessionState.SetBool(Prefix + "ApiLayersWereEnabled", apiLayers.enabled);
            SessionState.SetBool(Prefix + "SettingsCaptured", true);
        }

        private static ApiLayersFeature GetApiLayers()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            return settings == null ? null : settings.GetFeature<ApiLayersFeature>();
        }

        private static void RestoreSettings()
        {
            if (!SessionState.GetBool(Prefix + "SettingsCaptured", false)) return;
            CourseLauncher.SetDesktopStartup(SessionState.GetBool(Prefix + "DesktopStartupWasEnabled", false));
            PlayerSettings.runInBackground = SessionState.GetBool(Prefix + "RunInBackgroundWasEnabled", false);
            if (SessionState.GetBool(Prefix + "ApiLayersPresent", false))
            {
                var apiLayers = GetApiLayers();
                if (apiLayers != null)
                {
                    apiLayers.enabled = SessionState.GetBool(Prefix + "ApiLayersWereEnabled", false);
                    EditorUtility.SetDirty(apiLayers);
                }
            }
            AssetDatabase.SaveAssets();
            SessionState.SetBool(Prefix + "SettingsCaptured", false);
        }

        private static void RecordError(string message, string stack, LogType type)
        {
            if (!SessionState.GetBool(Prefix + "Pending", false)) return;
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                SessionState.SetInt(Prefix + "ErrorCount", SessionState.GetInt(Prefix + "ErrorCount", 0) + 1);
                // Simulator 207's MR Utility Kit can fail to decode its native
                // startup log. Keep the exception visible in the report while
                // distinguishing it from errors in the mission and UI logic.
                bool knownSdkLogDecode = message.Contains("Illegal byte sequence") &&
                    stack.Contains("Meta.XR.MRUtilityKit.MRUK.OnSharedLibLog");
                string key = knownSdkLogDecode ? "KnownSdkLogDecodeExceptionCount" : "UnexpectedErrorCount";
                SessionState.SetInt(Prefix + key, SessionState.GetInt(Prefix + key, 0) + 1);
            }
        }

        private static void Update()
        {
            if (!SessionState.GetBool(Prefix + "Pending", false) ||
                !long.TryParse(SessionState.GetString(Prefix + "Started", "0"), out long ticks)) return;
            double elapsed = (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds;
            if (elapsed > 90)
            {
                FinishWithError("Play smoke timed out after 90 seconds.");
                return;
            }
            if (EditorApplication.isPlaying && elapsed >= 20 && !SessionState.GetBool(Prefix + "Finished", false))
                Evaluate();
        }

        private static void Evaluate()
        {
            SessionState.SetBool(Prefix + "Finished", true);
            var report = new Report
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                scene = Week06TeamUIBuilder.ScenePath,
                runtimeManifest = Environment.GetEnvironmentVariable("XR_RUNTIME_JSON"),
                xrDeviceActive = XRSettings.isDeviceActive,
                xrLoaderActive = XRGeneralSettings.Instance != null &&
                    XRGeneralSettings.Instance.Manager != null &&
                    XRGeneralSettings.Instance.Manager.activeLoader != null,
                gameFrames = Time.frameCount,
                errorCount = SessionState.GetInt(Prefix + "ErrorCount", 0),
                knownSdkLogDecodeExceptionCount = SessionState.GetInt(Prefix + "KnownSdkLogDecodeExceptionCount", 0),
                unexpectedErrorCount = SessionState.GetInt(Prefix + "UnexpectedErrorCount", 0)
            };
            var failed = new List<string>();
            try
            {
                report.simulatorSelected = string.Equals(Path.GetFileName(report.runtimeManifest),
                    "meta_openxr_simulator.json", StringComparison.OrdinalIgnoreCase);
                var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                report.missingScriptCount = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Sum(transform => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject));
                var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                report.oneEventSystem = eventSystems.Length == 1 && eventSystems[0].isActiveAndEnabled;
                var canvasModules = UnityEngine.Object.FindObjectsByType<PointableCanvasModule>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                report.onePointableCanvasModule = canvasModules.Length == 1 && canvasModules[0].isActiveAndEnabled;
                report.oneActiveAudioListener = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)
                    .Count(listener => listener.isActiveAndEnabled) == 1;

                var rig = UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>();
                var camera = rig == null ? null : rig.centerEyeAnchor.GetComponent<Camera>();
                report.gameCameraActive = camera != null && camera.isActiveAndEnabled;
                var mission = UnityEngine.Object.FindFirstObjectByType<SpaceStationMission>();
                var console = UnityEngine.Object.FindFirstObjectByType<SpaceStationMissionConsole>();
                var canvas = console == null ? null : console.GetComponent<Canvas>();
                var raycaster = console == null ? null : console.GetComponent<GraphicRaycaster>();
                var rect = console == null ? null : console.GetComponent<RectTransform>();
                report.worldSpaceCanvasReady = console != null && console.gameObject.activeInHierarchy &&
                    canvas != null && canvas.isActiveAndEnabled && canvas.renderMode == RenderMode.WorldSpace &&
                    raycaster != null && raycaster.isActiveAndEnabled &&
                    console.GetComponentInChildren<PointableCanvas>(true) != null &&
                    console.HelpButton != null && console.ResetButton != null;
                report.uiInGameCameraView = report.gameCameraActive && report.worldSpaceCanvasReady &&
                    IsInCameraView(camera, rect);
                report.initialMissionStatus = mission == null ? null : mission.CurrentStatus;
                report.initialUiStatus = console == null ? null : console.VisibleStatus;
                report.initialStatusMatches = report.initialMissionStatus == InitialStatus &&
                    report.initialUiStatus == InitialStatus;

                if (console != null && console.HelpButton != null && console.ResetButton != null)
                {
                    var help = console.transform.Find("HelpText");
                    var caption = console.HelpButton.GetComponentInChildren<TMP_Text>(true);
                    bool helpInitiallyHidden = help != null && !help.gameObject.activeSelf && !console.HelpVisible;
                    console.HelpButton.onClick.Invoke();
                    report.helpButtonShowPassed = helpInitiallyHidden && console.HelpVisible &&
                        help.gameObject.activeSelf && caption != null && caption.text == "HIDE HELP";

                    var core = GameObject.Find("EnergyCore_GrabAndRelease");
                    var body = core == null ? null : core.GetComponent<Rigidbody>();
                    if (body != null && mission != null)
                    {
                        var displaced = body.position + new Vector3(0f, 0.5f, 0f);
                        body.position = displaced;
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                        Physics.SyncTransforms();
                        console.ResetButton.onClick.Invoke();
                        report.resetButtonRestoredCore = Vector3.Distance(body.position, displaced) > 0.2f &&
                            !body.isKinematic;
                        report.resetButtonRestoredStatus = mission.CurrentStatus == InitialStatus &&
                            console.VisibleStatus == InitialStatus && !mission.IsInserted;
                    }

                    // The Game camera capture deliberately shows the open help panel.
                    report.screenshotSaved = report.gameCameraActive && SaveCamera(camera);
                    console.HelpButton.onClick.Invoke();
                    report.helpButtonHidePassed = help != null && !console.HelpVisible && !help.gameObject.activeSelf &&
                        caption != null && caption.text == "SHOW HELP";
                }

                Require(report.simulatorSelected, "Meta XR Simulator runtime selected", failed);
                Require(report.xrLoaderActive, "OpenXR loader active", failed);
                Require(report.xrDeviceActive, "XR device active", failed);
                Require(report.gameFrames >= 30, "Game frames advanced", failed);
                Require(report.gameCameraActive, "OVRCameraRig Game camera active", failed);
                Require(report.oneEventSystem, "Exactly one active EventSystem", failed);
                Require(report.onePointableCanvasModule, "Exactly one active PointableCanvasModule", failed);
                Require(report.oneActiveAudioListener, "Exactly one active AudioListener", failed);
                Require(report.missingScriptCount == 0, "No missing scripts", failed);
                Require(report.worldSpaceCanvasReady, "World-space Meta ray Canvas ready", failed);
                Require(report.uiInGameCameraView, "World-space UI in Game camera view", failed);
                Require(report.initialStatusMatches, "Initial mission and UI status match", failed);
                Require(report.helpButtonShowPassed, "Synthetic Help button shows instructions", failed);
                Require(report.helpButtonHidePassed, "Synthetic Help button hides instructions", failed);
                Require(report.resetButtonRestoredCore, "Synthetic Reset button restores displaced core", failed);
                Require(report.resetButtonRestoredStatus, "Synthetic Reset button restores status", failed);
                Require(report.screenshotSaved, "Game camera screenshot saved", failed);
                Require(report.unexpectedErrorCount == 0, "No unexpected runtime errors", failed);
            }
            catch (Exception ex)
            {
                failed.Add("Evaluation exception: " + ex);
            }
            report.failedChecks = failed.ToArray();
            report.passed = failed.Count == 0;
            SaveReport(report);
            SessionState.SetBool(Prefix + "Passed", report.passed);
            Debug.Log("WEEK06_TEAM_UI_PLAY_SMOKE_" + (report.passed ? "PASS" : "FAIL"));
            EditorApplication.isPlaying = false;
        }

        private static void Require(bool ok, string check, List<string> failed)
        {
            if (!ok) failed.Add(check);
        }

        private static bool IsInCameraView(Camera camera, RectTransform rect)
        {
            if (camera == null || rect == null || (camera.cullingMask & (1 << rect.gameObject.layer)) == 0)
                return false;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var points = new[] { rect.TransformPoint(rect.rect.center), corners[0], corners[1], corners[2], corners[3] };
            return points.Any(point =>
            {
                var viewport = camera.WorldToViewportPoint(point);
                return viewport.z > 0 && viewport.x >= 0 && viewport.x <= 1 &&
                    viewport.y >= 0 && viewport.y <= 1;
            });
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
                File.WriteAllBytes(Path.Combine(OutputDirectory(), ScreenshotName), image.EncodeToPNG());
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
            SaveReport(new Report
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                scene = Week06TeamUIBuilder.ScenePath,
                failedChecks = new[] { message },
                passed = false
            });
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            else
            {
                SessionState.SetBool(Prefix + "Pending", false);
                RestoreSettings();
                ExitCli(false);
            }
        }

        private static void PlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode ||
                !SessionState.GetBool(Prefix + "Pending", false)) return;
            if (!SessionState.GetBool(Prefix + "Finished", false))
                FinishWithError("Play Mode exited before the 20-second evaluation.");
            bool passed = SessionState.GetBool(Prefix + "Passed", false);
            SessionState.SetBool(Prefix + "Pending", false);
            RestoreSettings();
            ExitCli(passed);
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
            File.WriteAllText(Path.Combine(OutputDirectory(), ReportName), JsonUtility.ToJson(report, true));
        }
    }
}
#endif
