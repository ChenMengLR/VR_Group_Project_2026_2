#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace CourseSetup
{
    /// <summary>
    /// Prepares the already-installed official Simulator and Core 205 Operator.
    /// Run in the windowed Editor, outside Play, after imports have completed.
    /// Uses official menu activation and public OpenXR settings APIs. It never
    /// downloads software, changes the system runtime, or writes environment vars.
    /// The official Simulator menu selects a runtime for this Editor process only.
    /// </summary>
    public static class Week03SimulatorSetup
    {
        private const string LayerName = "XR_APILAYER_METAX_operator";
        private const string SimulatorMenu = "Meta/Meta XR Simulator/Activate";
        private const string OperatorMenu = "Meta/Meta XR Operator/Activate";

        [Serializable]
        private sealed class SetupReport
        {
            public string generatedUtc;
            public string unityVersion;
            public string coreVersion;
            public string openxrVersion;
            public bool windowedEditor;
            public string operatorManifest;
            public string architecture;
            public bool simulatorMenuInvoked;
            public bool operatorMenuInvoked;
            public string processRuntimeBefore;
            public string processRuntimeAfter;
            public string processSelectedRuntime;
            public string processSimulatorConfig;
            public bool simulatorSelectedAndFilesPresent;
            public bool metaXRFeatureEnabled;
            public bool apiLayersFeatureEnabled;
            public bool operatorLayerEnabled;
            public bool readyForPlay;
            public string runtimeVerification;
            public string[] notes;
            public string[] errors;
        }

        [MenuItem("Course/Week 03/Prepare Simulator and Operator")]
        public static void Prepare()
        {
            var report = new SetupReport
            {
                generatedUtc = DateTime.UtcNow.ToString("o"),
                unityVersion = Application.unityVersion,
                architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                windowedEditor = !Application.isBatchMode,
                processRuntimeBefore = Environment.GetEnvironmentVariable("XR_RUNTIME_JSON"),
                runtimeVerification = "Pending: no Play session, input, screenshot, grab or teleport was tested by Prepare."
            };
            var notes = new List<string>();
            var errors = new List<string>();
            try
            {
                if (Application.platform != RuntimePlatform.WindowsEditor)
                    throw new InvalidOperationException("This preparation helper is for Windows Editor only.");
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Exit Play mode before preparing Simulator and Operator.");
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    throw new InvalidOperationException("Wait for script compilation and asset imports to finish, then run Prepare again.");
                if (Application.isBatchMode)
                    throw new InvalidOperationException("Use the windowed Editor for this visual test setup. Batch mode selects Simulator CI configuration.");

                var packages = PackageInfo.GetAllRegisteredPackages();
                var core = packages.FirstOrDefault(p => p.name == "com.meta.xr.sdk.core");
                var openxr = packages.FirstOrDefault(p => p.name == "com.unity.xr.openxr");
                report.coreVersion = core?.version;
                report.openxrVersion = openxr?.version;
                if (core == null || core.version != "205.0.0")
                    throw new InvalidOperationException("Expected installed com.meta.xr.sdk.core@205.0.0. No package was downloaded or changed.");
                if (openxr == null)
                    throw new InvalidOperationException("OpenXR Plugin is missing. Install stable 1.18.0 before using this helper.");

                // Both layouts are handled by the official Core 205 path utility.
                var layerDirs = new[]
                {
                    Path.Combine(core.resolvedPath, "Editor", "OculusInternal", "MetaXROperator", "x64"),
                    Path.Combine(core.resolvedPath, "Editor", "MetaXROperator", "x64")
                };
                string layerDir = layerDirs.FirstOrDefault(d =>
                    File.Exists(Path.Combine(d, "XrApiLayer_METAX_operator.json")) &&
                    File.Exists(Path.Combine(d, "XrApiLayer_METAX_operator.dll")));
                if (layerDir == null)
                    throw new FileNotFoundException("Bundled Operator x64 manifest/DLL is missing. Report this and repair Core SDK through Package Manager; no download or system configuration was attempted.");
                report.operatorManifest = Path.GetFullPath(Path.Combine(layerDir, "XrApiLayer_METAX_operator.json"));

                var group = BuildTargetGroup.Standalone;
                FeatureHelpers.RefreshFeatures(group);
                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                if (settings == null)
                    throw new InvalidOperationException("Standalone OpenXR settings are missing. Configure Week03 OpenXR first.");
                var apiLayers = settings.GetFeature<ApiLayersFeature>();
                var metaFeature = settings.GetFeatures<OpenXRFeature>()
                    .FirstOrDefault(f => f != null && f.GetType().FullName == "Meta.XR.MetaXRFeature");
                if (apiLayers == null || metaFeature == null)
                    throw new InvalidOperationException("Standalone API Layers or Meta XR Feature is missing. Verify installed packages and wait for feature discovery.");

                // Official menu validation prevents activation when Simulator is not installed.
                // False is also normal when it is already activated, so inspect real state below.
                report.simulatorMenuInvoked = EditorApplication.ExecuteMenuItem(SimulatorMenu);
                report.processRuntimeAfter = Environment.GetEnvironmentVariable("XR_RUNTIME_JSON");
                report.processSelectedRuntime = Environment.GetEnvironmentVariable("XR_SELECTED_RUNTIME_JSON");
                report.processSimulatorConfig = Environment.GetEnvironmentVariable("META_XRSIM_CONFIG_JSON");
                report.simulatorSelectedAndFilesPresent =
                    IsSimulatorManifest(report.processRuntimeAfter) &&
                    IsSimulatorManifest(report.processSelectedRuntime) &&
                    SamePath(report.processRuntimeAfter, report.processSelectedRuntime) &&
                    !string.IsNullOrEmpty(report.processSimulatorConfig) && File.Exists(report.processSimulatorConfig);
                if (!report.simulatorSelectedAndFilesPresent)
                    throw new InvalidOperationException("Official Simulator activation did not select an existing Meta Simulator runtime/config. Verify standalone Simulator installation and Core SDK detection. No manual/global environment override was applied.");

                metaFeature.enabled = true;
                EditorUtility.SetDirty(metaFeature);
                apiLayers.enabled = true;
                EditorUtility.SetDirty(apiLayers);
                report.operatorMenuInvoked = EditorApplication.ExecuteMenuItem(OperatorMenu);

                // Core205's PST fix explicitly re-enables after TryAdd. Reproduce that
                // public-API step because the activation menu alone can leave it disabled.
                var architecture = RuntimeInformation.ProcessArchitecture;
                // A target switch or Android build can leave the enabled layer in
                // settings while its staged Windows manifest has been removed.
                // Re-register through OpenXR so the manifest and DLL are copied.
                var stagedLayerDir = Path.Combine(Application.dataPath, "XR", "APILayers~",
                    "WindowsLayers", architecture.ToString().ToLowerInvariant());
                var stagedManifest = Path.Combine(stagedLayerDir, "XrApiLayer_METAX_operator.json");
                var stagedLibrary = Path.Combine(stagedLayerDir, "XrApiLayer_METAX_operator.dll");
                if (apiLayers.apiLayers.IsEnabled(LayerName, architecture) &&
                    (!File.Exists(stagedManifest) || !File.Exists(stagedLibrary)))
                {
                    if (!apiLayers.apiLayers.TryRemove(LayerName, architecture))
                        throw new InvalidOperationException("Could not remove an incomplete staged Operator layer.");
                }
                apiLayers.apiLayers.SetEnabled(LayerName, architecture, true);
                if (!apiLayers.apiLayers.IsEnabled(LayerName, architecture))
                {
                    if (!apiLayers.apiLayers.TryAdd(report.operatorManifest, architecture, group, out _))
                        throw new InvalidOperationException("OpenXR refused to register the bundled Operator API layer manifest.");
                    apiLayers.apiLayers.SetEnabled(LayerName, architecture, true);
                }
                EditorUtility.SetDirty(apiLayers);
                AssetDatabase.SaveAssets();

                report.metaXRFeatureEnabled = metaFeature.enabled;
                report.apiLayersFeatureEnabled = apiLayers.enabled;
                report.operatorLayerEnabled = apiLayers.apiLayers.IsEnabled(LayerName, architecture);
                report.readyForPlay = report.simulatorSelectedAndFilesPresent &&
                    report.metaXRFeatureEnabled && report.apiLayersFeatureEnabled && report.operatorLayerEnabled;
                if (!report.readyForPlay)
                    throw new InvalidOperationException("Preparation settings did not pass read-back verification.");
                notes.Add("Official Core205 menus activated Simulator and Operator; no system runtime or global environment variable was written by this helper.");
                notes.Add("Standalone Meta XR Feature is enabled so OVRManager can register unity_* Operator tools.");
                notes.Add("The direct local SSE client can connect to http://127.0.0.1:8720/sse after Play begins; no MCP proxy installation is needed for that supported connection method.");
                notes.Add("Scene content, Android configuration and course hand-tracking settings were not changed.");
                Debug.Log("Week03 Simulator/Operator settings verified. Enter Play, then inspect live Operator tools and XR session before testing interactions.");
            }
            catch (Exception ex)
            {
                errors.Add(ex.ToString());
                Debug.LogError("Week03 Simulator preparation failed: " + ex.Message);
                throw;
            }
            finally
            {
                report.notes = notes.ToArray();
                report.errors = errors.ToArray();
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string reportPath = Path.Combine(projectRoot, "Temp", "CourseSetup", "Week03_SimulatorSetup.json");
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
                Debug.Log("Simulator setup report: " + reportPath);
            }
        }

        private static bool IsSimulatorManifest(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path) &&
                string.Equals(Path.GetFileName(path), "meta_openxr_simulator.json", StringComparison.OrdinalIgnoreCase);
        }

        private static bool SamePath(string first, string second)
        {
            return string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
