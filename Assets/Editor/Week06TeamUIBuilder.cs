#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CourseProject;
using Oculus.Interaction;
using Oculus.Interaction.Editor.QuickActions;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CourseSetup
{
    /// <summary>
    /// Saves the Week 05 team scene as a Week 06 scene, then adds an English
    /// world-space UI through Meta's Ray Canvas quick action (the Canvas Wizard API).
    /// </summary>
    public static class Week06TeamUIBuilder
    {
        public const string ScenePath = "Assets/01_Scenes/Week06_TeamUI_Wang.unity";
        public const string PrefabPath = "Assets/03_Prefabs/Week06_MissionConsole.prefab";
        private const string ConsoleName = "Week06_MissionConsole_UIRoot";

        [MenuItem("Course/Week 06/Build Team UI Scene", priority = 60)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop Play and compilation before building Week 06.");
            if (!File.Exists(SoloStationBuilder.ScenePath))
                throw new FileNotFoundException("Build the Week 05 team scene first.", SoloStationBuilder.ScenePath);

            if (File.Exists(ScenePath))
            {
                var existing = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!existing.GetRootGameObjects().Any(go => go.name == "SoloStationRoot"))
                    throw new InvalidOperationException("Destination is not the generated team scene; preserving it.");
            }

            var source = EditorSceneManager.OpenScene(SoloStationBuilder.ScenePath, OpenSceneMode.Single);
            // The course calls for File > Save As from the Week 05 team scene.
            if (!EditorSceneManager.SaveScene(source, ScenePath, true))
                throw new IOException("Could not save a Week 06 copy of the Week 05 scene.");
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var station = scene.GetRootGameObjects().Single(go => go.name == "SoloStationRoot");
            var mission = station.GetComponent<SpaceStationMission>();
            if (mission == null) throw new InvalidOperationException("Space station mission is missing.");

            foreach (var oldName in new[] { "StartInstructions", "ResetInstructions" })
            {
                var oldLabel = station.transform.Find(oldName);
                if (oldLabel != null) oldLabel.gameObject.SetActive(false);
            }

            var canvasObject = new GameObject(ConsoleName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image),
                typeof(SpaceStationMissionConsole));
            canvasObject.transform.SetParent(station.transform, false);
            canvasObject.transform.position = new Vector3(-2f, 1.55f, 3.64f);
            // The panel is on the +Z wall; Unity UI's front is visible from -Z.
            canvasObject.transform.rotation = Quaternion.identity;
            // Fit the front wall and keep the text legible from the spawn area.
            canvasObject.transform.localScale = Vector3.one * 0.0038f;
            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = new Vector2(1000f, 650f);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20;
            var background = canvasObject.GetComponent<Image>();
            background.color = new Color(0.025f, 0.07f, 0.13f, 0.97f);
            background.raycastTarget = false;

            var title = AddText(rect, "Title", "LOST STATION | MISSION CONSOLE",
                new Vector2(0f, 269f), new Vector2(920f, 70f), 46f,
                new Color(0.49f, 0.93f, 1f));
            title.fontStyle = FontStyles.Bold;
            var statusLabel = AddText(rect, "StatusLabel", "CURRENT STATUS",
                new Vector2(0f, 198f), new Vector2(900f, 48f), 27f,
                new Color(0.54f, 0.75f, 0.84f));
            statusLabel.fontStyle = FontStyles.Bold;
            var status = AddText(rect, "StatusValue", "STATION OFFLINE\nGRAB THE CORE",
                new Vector2(0f, 97f), new Vector2(900f, 152f), 38f, Color.white);
            var help = AddText(rect, "HelpText",
                "1  TELEPORT TO THE BLUE REPAIR PAD\n2  HOLD GRIP. PUT CORE IN ORANGE SOCKET\n3  RELEASE CORE, THEN GO TO EXIT",
                new Vector2(0f, -85f), new Vector2(920f, 195f), 32f,
                new Color(0.79f, 0.94f, 1f));
            var helpButton = AddButton(rect, "HelpButton", "SHOW HELP",
                new Vector2(-225f, -256f), new Color(0.12f, 0.48f, 0.65f), out var helpButtonText);
            var resetButton = AddButton(rect, "ResetButton", "RESET MISSION",
                new Vector2(225f, -256f), new Color(0.59f, 0.31f, 0.10f), out _);

            var console = canvasObject.GetComponent<SpaceStationMissionConsole>();
            var serialized = new SerializedObject(console);
            Assign(serialized, "statusText", status);
            Assign(serialized, "helpText", help);
            Assign(serialized, "helpButtonText", helpButtonText);
            Assign(serialized, "helpButton", helpButton);
            Assign(serialized, "resetButton", resetButton);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddVoidPersistentListener(helpButton.onClick, console.ToggleHelp);
            UnityEventTools.AddVoidPersistentListener(resetButton.onClick, console.RequestReset);
            help.gameObject.SetActive(false);

            // Meta's public Quick Actions API performs the Canvas Wizard setup:
            // PointableCanvas, GraphicRaycaster and ray interactors on the existing rig.
            // The source scene already has one PointableCanvasModule/EventSystem.
            QuickActionsAPI.AddRayCanvasInteraction(canvasObject);

            EnsureFolder("Assets/03_Prefabs");
            PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObject, PrefabPath,
                InteractionMode.AutomatedAction);
            // Keep the scene-specific mission reference as a prefab-instance override.
            serialized = new SerializedObject(canvasObject.GetComponent<SpaceStationMissionConsole>());
            Assign(serialized, "mission", mission);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            StationHardwareBuilder.Apply(scene);
            StationControlsBuilder.Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save the Week 06 team scene.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Validate(scene);
            Selection.activeGameObject = canvasObject;
            Debug.Log("WEEK06_TEAM_UI_BUILD_OK: " + ScenePath + " | Meta Ray Canvas Wizard | English UI");
            // Meta's remote editor utilities can keep -quit waiting after the scene is saved.
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Course/Week 06/Validate Team UI Scene", priority = 61)]
        public static void ValidateCurrentScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Validate(scene);
            Debug.Log("WEEK06_TEAM_UI_VALIDATION_OK");
        }

        [MenuItem("Course/Week 07/Build Quest APK", priority = 70)]
        public static void BuildAndroidApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Switch Unity to Android before building the Week 07 APK.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Validate(scene);
            PlayerSettings.companyName = "WANG HAOBIN";
            PlayerSettings.productName = "Lost Station: Restart the Hatch";
            PlayerSettings.bundleVersion = "0.4.0";
            PlayerSettings.Android.bundleVersionCode = 3;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.wanghaobin.loststation");
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            Directory.CreateDirectory("Builds");
            const string output = "Builds/Wang_VR_Week07.apk";
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Week 07 Quest APK build failed: " + result.summary.result);
            Debug.Log("WEEK07_QUEST_APK_OK: " + output + " | " + result.summary.totalSize + " bytes");
            AssetDatabase.SaveAssets();
            // Meta editor background tasks can outlive a successful build and exhaust -quit's async timeout.
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Course/Week 06/Capture UI Previews", priority = 62)]
        public static void CaptureUiPreviews()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Validate(scene);
            var station = scene.GetRootGameObjects().Single(go => go.name == "SoloStationRoot");
            var console = station.GetComponentInChildren<SpaceStationMissionConsole>(true);
            var cameraObject = new GameObject("Temporary UI Preview Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(-2f, 1.55f, 0.9f);
            camera.transform.LookAt(new Vector3(-2f, 1.55f, 3.64f));
            camera.fieldOfView = 60f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.12f);
            try
            {
                Capture(camera, "CourseValidation/Runtime/Week06_UI_Status_EditorPreview.png");
                console.ToggleHelp();
                Capture(camera, "CourseValidation/Runtime/Week06_UI_Help_EditorPreview.png");
                console.ToggleHelp();
                Debug.Log("WEEK06_UI_PREVIEW_OK: status and help, Editor camera render (not Quest capture)");
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1600, 900, 24);
            var frame = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                frame.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                frame.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, frame.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(frame);
                Object.DestroyImmediate(target);
            }
        }

        private static void Validate(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var station = roots.SingleOrDefault(go => go.name == "SoloStationRoot");
            if (station == null) throw new InvalidOperationException("SoloStationRoot is missing.");
            var mission = station.GetComponent<SpaceStationMission>();
            var console = station.GetComponentInChildren<SpaceStationMissionConsole>(true);
            if (mission == null || console == null || console.Mission != mission ||
                console.HelpButton == null || console.ResetButton == null)
                throw new InvalidOperationException("Week 06 console is not connected to the mission.");
            if (console.HelpButton.onClick.GetPersistentEventCount() != 1 ||
                console.ResetButton.onClick.GetPersistentEventCount() != 1 ||
                console.HelpButton.onClick.GetPersistentTarget(0) != console ||
                console.ResetButton.onClick.GetPersistentTarget(0) != console ||
                console.HelpButton.onClick.GetPersistentMethodName(0) != nameof(SpaceStationMissionConsole.ToggleHelp) ||
                console.ResetButton.onClick.GetPersistentMethodName(0) != nameof(SpaceStationMissionConsole.RequestReset))
                throw new InvalidOperationException("Week 06 buttons have no live actions.");
            if (console.GetComponent<Canvas>()?.renderMode != RenderMode.WorldSpace ||
                console.GetComponent<GraphicRaycaster>() == null ||
                console.GetComponentInChildren<PointableCanvas>(true) == null)
                throw new InvalidOperationException("Meta Ray Canvas Wizard setup is incomplete.");
            if (roots.Sum(go => go.GetComponentsInChildren<EventSystem>(true).Length) != 1 ||
                roots.Sum(go => go.GetComponentsInChildren<PointableCanvasModule>(true).Length) != 1)
                throw new InvalidOperationException("The scene needs exactly one existing EventSystem and PointableCanvasModule.");
            var missing = roots.SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if (missing != 0) throw new InvalidOperationException("Week 06 scene has missing scripts: " + missing);
        }

        private static TextMeshProUGUI AddText(RectTransform parent, string name, string value,
            Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static Button AddButton(RectTransform parent, string name, string caption,
            Vector2 position, Color color, out TextMeshProUGUI captionText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400f, 88f);
            var image = go.GetComponent<Image>();
            // Button's ColorBlock applies its tint to this Image; keep the base white.
            image.color = Color.white;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.24f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.24f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            captionText = AddText(rect, name + "Label", caption, Vector2.zero,
                new Vector2(380f, 80f), 34f, Color.white);
            captionText.fontStyle = FontStyles.Bold;
            return button;
        }

        private static void Assign(SerializedObject serialized, string name, Object value)
        {
            var property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException("Console field missing: " + name);
            property.objectReferenceValue = value;
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
#endif
