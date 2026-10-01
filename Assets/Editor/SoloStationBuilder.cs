#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CourseProject;
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CourseSetup
{
    /// <summary>
    /// Builds the solo group project from the validated Week 03 Meta interaction scene.
    /// The Week 03 scene and official Meta example remain untouched.
    /// This generated scene is intentionally rebuilt from source by this command.
    /// </summary>
    public static class SoloStationBuilder
    {
        public const string ScenePath = "Assets/01_Scenes/Solo_SpaceStation.unity";
        private const string RootName = "SoloStationRoot";
        private const string MaterialFolder = "Assets/03_Materials/SoloStation";

        [MenuItem("Course/Refresh Solo Space Station And Preview", priority = 19)]
        public static void BuildAndPreview()
        {
            Build();
            CapturePreview();
        }

        [MenuItem("Course/Build Solo Space Station", priority = 20)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop Play and wait for compilation before building.");
            if (!File.Exists(Week03Builder.ScenePath))
                throw new FileNotFoundException("Build Week 03 before the solo station scene.", Week03Builder.ScenePath);

            // The scene is generated. Refuse to overwrite another scene that happens to use this path.
            if (File.Exists(ScenePath))
            {
                var oldScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (!oldScene.GetRootGameObjects().Any(go => go.name == RootName))
                    throw new InvalidOperationException("Destination has no SoloStationRoot marker; scene preserved.");
            }
            EditorSceneManager.OpenScene(Week03Builder.ScenePath, OpenSceneMode.Single);
            // Replace the scene file while preserving its .meta GUID on rebuild.
            // Deleting and re-copying the asset leaves a stale GUID in Build Settings.
            File.Copy(Week03Builder.ScenePath, ScenePath, true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var rig = FindRoot(scene, "OVRCameraRig").GetComponent<OVRCameraRig>();
            var task = FindRoot(scene, "Week03_CourseTask");
            if (rig == null || task == null || FindRoot(scene, "ISDKExampleMenu") == null)
                throw new InvalidOperationException("The source scene is missing its Meta rig, course task or settings menu.");
            if (scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<TeleportInteractor>(true)).Count() == 0)
                throw new InvalidOperationException("The source scene has no Meta TeleportInteractor.");

            foreach (var rootName in new[] {
                "LocomotionEnvironment", "NavMeshPatches", "ExtraColliders", "NavMeshHotspotWalkable",
                "HotspotVoidFloor", "CollidersTeleportBlocker", "Hotspots", "MicrogestureTutorials"
            })
            {
                var old = FindRoot(scene, rootName);
                if (old != null) old.SetActive(false);
            }
            // Keep the official locomotion settings menu, but place its main panel
            // beside the starting pad so it cannot overlap the repair bench.
            FindRoot(scene, "ISDKExampleMenu").transform.position = new Vector3(-0.5f, 0f, 1.3f);

            task.name = "SoloStationTask";
            var startPad = task.transform.Find("StartZone");
            var repairPad = task.transform.Find("TargetZone");
            var core = task.transform.Find("MissionObject");
            if (startPad == null || repairPad == null || core == null)
                throw new InvalidOperationException("Source teleport pads or grabbable object are missing.");
            startPad.name = "StartPad_TeleportHotspot";
            repairPad.name = "RepairPad_TeleportHotspot";
            // After teleport the player faces the bench instead of the starting pad.
            repairPad.rotation = Quaternion.LookRotation(Vector3.forward);
            core.name = "EnergyCore_GrabAndRelease";
            core.position = new Vector3(1.15f, 1.02f, 1.30f);
            core.rotation = Quaternion.identity;
            var coreBody = core.GetComponent<Rigidbody>();
            var coreGrab = core.GetComponent<Grabbable>();
            if (coreBody == null || coreGrab == null)
                throw new InvalidOperationException("The source EnergyCore needs Rigidbody and Meta Grabbable.");
            coreBody.linearVelocity = Vector3.zero;
            coreBody.angularVelocity = Vector3.zero;

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            EnsureFolder(MaterialFolder);
            var hull = Material("Hull", new Color(0.055f, 0.09f, 0.16f));
            var floor = Material("Floor", new Color(0.12f, 0.17f, 0.23f));
            var trim = Material("Trim", new Color(0.28f, 0.40f, 0.52f));
            var desk = Material("Workbench", new Color(0.17f, 0.24f, 0.30f));
            var amber = Material("StandbyAmber", new Color(1f, 0.40f, 0.08f), true);
            var green = Material("PoweredGreen", new Color(0.10f, 1f, 0.33f), true);
            var cyan = Material("CoreCyan", new Color(0.05f, 0.74f, 1f), true);
            var door = Material("Hatch", new Color(0.18f, 0.31f, 0.44f));
            core.GetComponent<Renderer>().sharedMaterial = cyan;

            // Closed, compact room; the existing sample world is inactive in this copy.
            Cube("Floor_Collider", new Vector3(0.8f, -0.06f, 0.75f), new Vector3(10.2f, 0.12f, 6.5f), floor, root.transform);
            Cube("Ceiling", new Vector3(0.8f, 3.05f, 0.75f), new Vector3(10.2f, 0.12f, 6.5f), hull, root.transform);
            Cube("RearWall", new Vector3(0.8f, 1.5f, -2.5f), new Vector3(10.2f, 3f, 0.12f), hull, root.transform);
            Cube("FrontWall", new Vector3(0.8f, 1.5f, 4f), new Vector3(10.2f, 3f, 0.12f), hull, root.transform);
            Cube("StartEndWall", new Vector3(-4.3f, 1.5f, 0.75f), new Vector3(0.12f, 3f, 6.5f), hull, root.transform);
            Cube("ExitEndWall", new Vector3(5.9f, 1.5f, 0.75f), new Vector3(0.12f, 3f, 6.5f), hull, root.transform);
            Cube("HatchPartition_Back", new Vector3(3.25f, 1.5f, -1.32f), new Vector3(0.2f, 3f, 2.36f), hull, root.transform);
            Cube("HatchPartition_Front", new Vector3(3.25f, 1.5f, 2.82f), new Vector3(0.2f, 3f, 2.36f), hull, root.transform);
            Cube("HatchLintel", new Vector3(3.25f, 2.75f, 0.75f), new Vector3(0.22f, 0.5f, 1.82f), trim, root.transform);
            var hatch = Cube("SlidingHatch", new Vector3(3.25f, 1.24f, 0.75f), new Vector3(0.17f, 2.48f, 1.75f), door, root.transform);
            Cube("HatchWarningStrip", new Vector3(3.13f, 2.52f, 0.75f), new Vector3(0.04f, 0.05f, 1.55f), amber, root.transform);

            Cube("WorkbenchTop", new Vector3(1.5f, 0.79f, 1.45f), new Vector3(2.45f, 0.12f, 0.9f), desk, root.transform);
            Cube("WorkbenchLegLeft", new Vector3(0.5f, 0.39f, 1.45f), new Vector3(0.12f, 0.78f, 0.75f), trim, root.transform);
            Cube("WorkbenchLegRight", new Vector3(2.5f, 0.39f, 1.45f), new Vector3(0.12f, 0.78f, 0.75f), trim, root.transform);
            Cube("WorkbenchBack", new Vector3(1.5f, 1.15f, 1.90f), new Vector3(2.45f, 0.75f, 0.08f), hull, root.transform);

            var socket = new GameObject("EnergySocket_SnapAnchor").transform;
            socket.SetParent(root.transform, false);
            socket.position = new Vector3(1.85f, 1.01f, 1.30f);
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "SocketIndicator";
            ring.transform.SetParent(root.transform, false);
            ring.transform.position = new Vector3(1.85f, 0.88f, 1.30f);
            ring.transform.localScale = new Vector3(0.36f, 0.025f, 0.36f);
            ring.GetComponent<Renderer>().sharedMaterial = amber;
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            var exit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            exit.name = "ExitMarker";
            exit.transform.SetParent(root.transform, false);
            exit.transform.position = new Vector3(4.45f, 0.035f, 0.75f);
            exit.transform.localScale = new Vector3(0.9f, 0.025f, 0.9f);
            exit.GetComponent<Renderer>().sharedMaterial = green;
            Object.DestroyImmediate(exit.GetComponent<Collider>());

            Cube("StartGuideStrip", new Vector3(-1.5f, 0.045f, 2.35f), new Vector3(2.2f, 0.025f, 0.06f), cyan, root.transform);
            Cube("RepairGuideStrip", new Vector3(1.5f, 0.045f, 2.35f), new Vector3(2.2f, 0.025f, 0.06f), amber, root.transform);
            var socketLight = PointLight("SocketLight", socket.position + Vector3.up * 0.18f, new Color(1f, 0.46f, 0.10f), 1.5f, 2.5f, root.transform);
            var hatchLight = PointLight("HatchStatusLight", new Vector3(3.05f, 2.55f, 0.75f), new Color(0.12f, 0.40f, 1f), 2.2f, 3f, root.transform);
            PointLight("CeilingLight_Start", new Vector3(-1.7f, 2.65f, 0.75f), new Color(0.46f, 0.73f, 1f), 3f, 6f, root.transform);
            PointLight("CeilingLight_Repair", new Vector3(1.5f, 2.65f, 0.75f), new Color(0.46f, 0.73f, 1f), 3f, 6f, root.transform);
            PointLight("CeilingLight_Exit", new Vector3(4.6f, 2.65f, 0.75f), new Color(0.35f, 1f, 0.6f), 2f, 3f, root.transform);

            var status = Label("MissionStatus", "STATION OFFLINE\nGRAB THE CORE",
                new Vector3(1.5f, 2.0f, 1.85f), 1.0f, root.transform);
            Label("StartInstructions", "01  TELEPORT", new Vector3(-1.5f, 1.7f, 3.83f), 1.2f, root.transform);
            Label("SocketInstructions", "02  INSERT CORE", new Vector3(0.5f, 2.55f, 3.83f), 1.2f, root.transform);
            Label("ExitInstructions", "03  EXIT", new Vector3(4.5f, 1.7f, 3.83f), 1.2f, root.transform);
            Label("ResetInstructions", "B / R  RESET", new Vector3(-1.5f, 1.25f, 3.83f), 0.8f, root.transform);

            var audioRoot = new GameObject("SocketSpatialAudio");
            audioRoot.transform.SetParent(root.transform, false);
            audioRoot.transform.position = socket.position;
            var hum = audioRoot.AddComponent<AudioSource>();
            var cue = audioRoot.AddComponent<AudioSource>();
            var mission = root.AddComponent<SpaceStationMission>();
            var serialized = new SerializedObject(mission);
            Assign(serialized, "coreGrab", coreGrab);
            Assign(serialized, "coreBody", coreBody);
            Assign(serialized, "socket", socket);
            Assign(serialized, "playerHead", rig.centerEyeAnchor);
            Assign(serialized, "exitMarker", exit.transform);
            Assign(serialized, "hatch", hatch.transform);
            Assign(serialized, "socketLight", socketLight);
            Assign(serialized, "hatchLight", hatchLight);
            Assign(serialized, "socketIndicator", ring.GetComponent<Renderer>());
            Assign(serialized, "standbyMaterial", amber);
            Assign(serialized, "successMaterial", green);
            Assign(serialized, "statusText", status);
            Assign(serialized, "humSource", hum);
            Assign(serialized, "cueSource", cue);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.21f, 0.29f);
            RenderSettings.fog = false;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save solo station scene.");
            EditorBuildSettings.scenes = Array.Empty<EditorBuildSettingsScene>();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            var sceneGuid = AssetDatabase.AssetPathToGUID(ScenePath);
            if (!string.Equals(EditorBuildSettings.scenes[0].guid.ToString(), sceneGuid,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Build Settings scene GUID does not match the saved solo scene.");
            AssetDatabase.SaveAssets();
            Validate(scene);
            Selection.activeGameObject = root;
            Debug.Log("SOLO_STATION_BUILD_OK: " + ScenePath + " | one Meta rig, two teleport pads, one grabbable core, socket, hatch, spatial audio.");
        }

        [MenuItem("Course/Validate Solo Space Station", priority = 21)]
        public static void ValidateCurrentScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Validate(scene);
            Debug.Log("SOLO_STATION_VALIDATION_OK");
        }

        [MenuItem("Course/Capture Solo Space Station Preview", priority = 22)]
        public static void CapturePreview()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var cameraObject = new GameObject("Temporary Preview Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(-2.7f, 1.55f, -1.45f);
            camera.transform.LookAt(new Vector3(1.15f, 1.35f, 1.25f));
            camera.fieldOfView = 76f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.12f);
            var texture = new RenderTexture(1600, 900, 24);
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();
                const string path = "CourseValidation/Runtime/SoloStation_EditorPreview.png";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log("SOLO_STATION_PREVIEW_OK: " + path);
            }
            finally
            {
                RenderTexture.active = null;
                camera.targetTexture = null;
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [MenuItem("Course/Build Solo Space Station APK", priority = 23)]
        public static void BuildAndroidApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Switch Unity to Android first, or launch batchmode with -buildTarget Android.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Validate(scene);
            PlayerSettings.companyName = "WANG HAOBIN";
            PlayerSettings.productName = "Lost Station";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.wanghaobin.loststation");
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/SoloSpaceStation.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var result = BuildPipeline.BuildPlayer(options);
            if (result.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Solo APK build failed: " + result.summary.result);
            Debug.Log("SOLO_STATION_APK_OK: " + options.locationPathName + " | " + result.summary.totalSize + " bytes");
        }

        private static void Validate(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var root = roots.SingleOrDefault(go => go.name == RootName);
            var task = roots.SingleOrDefault(go => go.name == "SoloStationTask");
            if (root == null || task == null || roots.Count(go => go.GetComponent<OVRCameraRig>() != null) != 1)
                throw new InvalidOperationException("Solo scene needs its mission root, task and one Meta rig.");
            if (root.GetComponent<SpaceStationMission>() == null ||
                task.GetComponentsInChildren<Oculus.Interaction.Locomotion.TeleportInteractable>(true).Length < 2 ||
                task.GetComponentInChildren<Grabbable>(true) == null ||
                root.transform.Find("SlidingHatch") == null ||
                root.transform.Find("EnergySocket_SnapAnchor") == null)
                throw new InvalidOperationException("Solo mission interaction structure is incomplete.");
            int missing = roots.SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if (missing > 0) throw new InvalidOperationException("Scene contains " + missing + " missing scripts.");
        }

        private static GameObject FindRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static Light PointLight(string name, Vector3 position, Color color, float intensity, float range, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            return light;
        }

        private static TextMeshPro Label(string name, string text, Vector3 position, float size, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            go.transform.localScale = Vector3.one * 0.1f;
            var label = go.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = size * 12f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.70f, 0.94f, 1f);
            label.rectTransform.sizeDelta = new Vector2(32f, 8f);
            return label;
        }

        private static Material Material(string name, Color color, bool emission = false)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find(emission ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = Shader.Find(emission ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
            material.color = color;
            material.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Assign(SerializedObject serialized, string name, Object value)
        {
            var property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException("Mission field missing: " + name);
            property.objectReferenceValue = value;
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
#endif
