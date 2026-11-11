#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CourseProject;
using Oculus.Interaction;
using Oculus.Interaction.Editor.QuickActions;
using Oculus.Interaction.Locomotion;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CourseSetup
{
    public static class StationGravityLeverBuilder
    {
        public const string Output = "CourseValidation/Runtime/GravityLever_20261109";
        private const string Installation = "GravityAndAirlockInteraction";

        [MenuItem("Course/Exploration Level/Apply Low Gravity and Airlock Lever")]
        public static void ApplyCurrent()
        {
            StationRouteBuilder.RequireSavedEditScene();
            var scene = EditorSceneManager.OpenScene(StationRouteBuilder.ScenePath);
            Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the updated recovery route.");
            AssetDatabase.SaveAssets();
            Debug.Log("STATION_GRAVITY_LEVER_SAVED");
        }

        public static void ApplyAndCheckCli()
        {
            try
            {
                ApplyCurrent();
                var scene = EditorSceneManager.OpenScene(StationRouteBuilder.ScenePath);
                var mission = Object.FindFirstObjectByType<SpaceStationMission>();
                var lever = Object.FindFirstObjectByType<StationAirlockLever>();
                if (lever == null || lever.Grab == null || !mission.RequiresLever)
                    throw new InvalidOperationException("The saved airlock must require its physical lever.");
                if (Object.FindFirstObjectByType<StationLowGravity>() == null ||
                    Object.FindObjectsByType<StationFloatingBody>(FindObjectsSortMode.None).Length != 3)
                    throw new InvalidOperationException("Low gravity and three floating-body bindings are required.");
                if (LightmapSettings.lightmaps.Length == 0 || LightmapSettings.lightProbes.count == 0)
                    throw new InvalidOperationException("Baked cabin lightmaps and probes must be retained.");
                var camera = StationHardwarePreview.CreateCamera();
                StationHardwarePreview.Pose(camera, new Vector3(1.46f, 1.58f, 2.88f),
                    new Vector3(3.04f, 1.32f, 2.31f), 52f);
                StationHardwarePreview.Save(camera, Path.Combine(Output, "Lever_EditorPreview.png"));
                Object.DestroyImmediate(camera.gameObject);
                EditorSceneManager.OpenScene(scene.path);
                Debug.Log("STATION_GRAVITY_LEVER_EDITOR_PASS");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static void Apply(Scene scene)
        {
            if (scene.path != StationRouteBuilder.ScenePath)
                throw new InvalidOperationException("Apply this feature to the recovery route.");
            var station = scene.GetRootGameObjects().Single(root => root.name == "SoloStationRoot");
            var mission = station.GetComponent<SpaceStationMission>();
            var controls = station.GetComponent<SpaceStationControls>();
            var route = station.GetComponent<SpaceStationRoute>();
            var rig = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<OVRCameraRig>(true)).Single();
            var character = rig.GetComponentInChildren<Oculus.Interaction.Locomotion.CharacterController>(true);
            var gravity = station.GetComponent<StationLowGravity>() ?? station.AddComponent<StationLowGravity>();
            gravity.Configure(controls.Locomotor, character);

            var old = station.transform.Find(Installation);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var installation = Group(Installation, station.transform, Vector3.zero);
            var graphite = Material("GraphiteAlloy");
            var metal = Material("MachinedTitanium");
            var amber = Material("StandbyAmber");
            var green = Material("ReadyGreen");
            var cyan = Material("EnergyCyan");

            var panel = Group("AirlockLeverPanel", installation, new Vector3(3.06f, 1.24f, 2.35f));
            panel.rotation = Quaternion.Euler(0f, -90f, 0f);
            Part("ControlHousing", panel, Vector3.zero, new Vector3(.55f, .82f, .14f), graphite);
            var panelCollider = panel.gameObject.AddComponent<BoxCollider>();
            panelCollider.size = new Vector3(.55f, .82f, .14f);
            Part("RecessedPlate", panel, new Vector3(0f, -.05f, .075f), new Vector3(.43f, .56f, .018f), metal);
            Part("LeverGuide", panel, new Vector3(0f, -.05f, .09f), new Vector3(.08f, .42f, .013f), graphite);
            var lamp = Part("InterlockLamp", panel, new Vector3(0f, .345f, .085f),
                new Vector3(.44f, .025f, .017f), amber).GetComponent<Renderer>();
            var label = Label("LeverInstructions", panel, new Vector3(0f, .245f, .095f),
                "POWER REQUIRED", new Vector2(.48f, .13f), .045f);
            Label("LatchMark", panel, new Vector3(0f, -.345f, .095f),
                "GRIP + PULL DOWN", new Vector2(.49f, .08f), .042f);

            var pivot = Group("LeverPivot", panel, new Vector3(0f, -.14f, .14f));
            Part("Axle", pivot, Vector3.zero, new Vector3(.24f, .07f, .07f), metal);
            var handle = Group("LeverHandle", pivot, Vector3.zero);
            Part("LeverArm", handle, new Vector3(0f, .14f, .02f), new Vector3(.048f, .28f, .045f), metal);
            Part("GripBar", handle, new Vector3(0f, .30f, .025f), new Vector3(.29f, .075f, .085f), amber);
            for (int i = -2; i <= 2; i++)
                Part("GripRib", handle, new Vector3(i * .047f, .30f, .025f), new Vector3(.012f, .081f, .091f), graphite);
            var handleCollider = handle.gameObject.AddComponent<BoxCollider>();
            handleCollider.center = new Vector3(0f, .30f, .025f);
            handleCollider.size = new Vector3(.34f, .12f, .13f);
            var body = handle.gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            QuickActionsAPI.AddGrabInteraction(handle.gameObject);
            var grab = handle.GetComponent<Grabbable>();
            grab.MaxGrabPoints = 1;
            grab.InjectOptionalRigidbody(body);
            var audio = panel.gameObject.AddComponent<AudioSource>();
            audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/07_Sounds/SFX/grab_core.wav");
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.volume = .5f;
            audio.minDistance = .3f;
            audio.maxDistance = 5f;
            var lever = panel.gameObject.AddComponent<StationAirlockLever>();
            lever.Configure(mission, grab, pivot, handle, label, lamp, amber, cyan, green, audio);
            mission.ConfigureAirlockLever(lever, Object.FindFirstObjectByType<StationCabinPower>());

            var floatingCore = route.Core.GetComponent<StationFloatingBody>() ?? route.Core.gameObject.AddComponent<StationFloatingBody>();
            floatingCore.Configure(route.Core, route.Core.GetComponent<Grabbable>(), true);
            MakeFloatingTool(installation, "FloatingSpanner", new Vector3(-9.5f, 1.45f, -1.45f), metal, graphite, true);
            MakeFloatingTool(installation, "FloatingBattery", new Vector3(-6f, 1.7f, 7.4f), graphite, cyan, false);
            Label("GravityInstructions", installation, new Vector3(-10.9f, 1.6f, -1.99f),
                "LOW GRAVITY ASSIST\nA  SHORT BOOST     X  HOLD TO BRAKE", new Vector2(2.2f, .33f), .11f);
            installation.Find("GravityInstructions").rotation = Quaternion.Euler(0f, 180f, 0f);
            UpdateInstructions(station);
            EditorUtility.SetDirty(mission);
            EditorUtility.SetDirty(gravity);
        }

        private static void MakeFloatingTool(Transform parent, string name, Vector3 position,
            Material primary, Material detail, bool spanner)
        {
            var tool = Group(name, parent, position);
            tool.rotation = Quaternion.Euler(10f, 20f, 25f);
            Part("Body", tool, Vector3.zero, new Vector3(.06f, .28f, .045f), primary);
            if (spanner)
            {
                Part("JawLeft", tool, new Vector3(-.045f, .145f, 0f), new Vector3(.035f, .09f, .05f), primary);
                Part("JawRight", tool, new Vector3(.045f, .145f, 0f), new Vector3(.035f, .09f, .05f), primary);
                Part("JawBase", tool, new Vector3(0f, .105f, 0f), new Vector3(.12f, .035f, .05f), primary);
            }
            else Part("ChargeStrip", tool, new Vector3(0f, 0f, -.027f), new Vector3(.02f, .19f, .01f), detail);
            var collider = tool.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(.13f, .38f, .06f);
            var body = tool.gameObject.AddComponent<Rigidbody>();
            body.mass = .12f;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            tool.gameObject.AddComponent<StationFloatingBody>().Configure(body, null, false);
        }

        private static void UpdateInstructions(GameObject station)
        {
            var console = station.GetComponentInChildren<SpaceStationMissionConsole>(true);
            var help = console.transform.Find("HelpText").GetComponent<TMP_Text>();
            help.text = "1  FOLLOW BLUE; GRIP TO CARRY CORE\n2  RELEASE IN SOCKET; RESTORE POWER\n3  GRIP AND PULL AIRLOCK LEVER; EXIT\nA  SHORT BOOST   X  HOLD TO BRAKE";
            help.fontSize = 24f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(help);
            station.transform.Find("StationControlsMenu/Controls").GetComponent<TMP_Text>().text =
                SpaceStationControls.LowGravityHelp;
        }

        private static Material Material(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Materials/StationHardware/" + name + ".mat") ??
            throw new FileNotFoundException("Missing station material " + name);

        private static Transform Group(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        private static Transform Part(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go.transform;
        }

        private static TextMeshPro Label(string name, Transform parent, Vector3 position, string caption, Vector2 size, float fontSize)
        {
            var label = Group(name, parent, position).gameObject.AddComponent<TextMeshPro>();
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            label.text = caption;
            label.fontSize = fontSize * 10f;
            label.rectTransform.sizeDelta = size;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(.65f, .90f, 1f);
            return label;
        }
    }
}
#endif
