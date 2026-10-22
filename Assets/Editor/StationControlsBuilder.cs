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
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CourseSetup
{
    public static class StationControlsBuilder
    {
        public static void BuildAndValidate()
        {
            var scene = EditorSceneManager.OpenScene(Week06TeamUIBuilder.ScenePath);
            Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            CaptureMenuPreview(scene);
            StationControlsValidation.Run();
        }

        private static void CaptureMenuPreview(Scene scene)
        {
            var panel = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpaceStationControls>(true)).Single().MenuPanel;
            panel.SetActive(true);
            panel.transform.SetPositionAndRotation(new Vector3(0,10,0),Quaternion.identity);
            var go = new GameObject("ControlsPreviewCamera",typeof(Camera));
            var camera = go.GetComponent<Camera>();
            camera.transform.position = new Vector3(0,10,-1.6f);
            camera.orthographic = true; camera.orthographicSize = .57f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.015f,.025f,.045f);
            var target = new RenderTexture(1600,1000,24);
            var frame = new Texture2D(1600,1000,TextureFormat.RGB24,false);
            var previous = RenderTexture.active;
            try
            {
                Canvas.ForceUpdateCanvases();
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                frame.ReadPixels(new Rect(0,0,1600,1000),0,0); frame.Apply();
                Directory.CreateDirectory("CourseValidation/Runtime");
                File.WriteAllBytes("CourseValidation/Runtime/StationControls_Menu_EditorPreview.png",frame.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; Object.DestroyImmediate(frame); Object.DestroyImmediate(target); Object.DestroyImmediate(go);
            }
        }

        [MenuItem("Course/Controls/Apply Standard VR Controls")]
        public static void ApplyCurrent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var scene = EditorSceneManager.OpenScene(Week06TeamUIBuilder.ScenePath);
            Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("STATION_CONTROLS_BUILD_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void Apply(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var all = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var rig = roots.SelectMany(r => r.GetComponentsInChildren<OVRCameraRig>(true)).Single();
            var locomotor = rig.GetComponentInChildren<FirstPersonLocomotor>(true);
            if (locomotor == null) throw new InvalidOperationException("Meta FirstPersonLocomotor missing.");
            var station = roots.Single(r => r.name == "SoloStationRoot");
            var mission = station.GetComponent<SpaceStationMission>();
            var existing = station.transform.Find("StationControlsMenu");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var controls = station.GetComponent<SpaceStationControls>() ?? station.AddComponent<SpaceStationControls>();

            // Keep SDK input/interaction infrastructure; remove only competing sample locomotion inputs.
            foreach (var t in all)
            {
                if (t == null) continue;
                if (t.name == "ISDKExampleMenu" || t.name == "ControllerSlideInteractor" ||
                    t.name == "ControllerTurnerInteractor" || t.name == "ControllerLocomotionSlideActions" ||
                    t.name == "MicroGesturesLocomotionHandInteractorGroup") t.gameObject.SetActive(false);
                foreach (var behaviour in t.GetComponents<MonoBehaviour>())
                {
                    if (behaviour is MovingSetting || behaviour is TurningSetting || behaviour is MovementAimingSetting ||
                        behaviour is SlideLocomotionBroadcaster || behaviour is StepLocomotionBroadcaster ||
                        behaviour is TurnerEventBroadcaster || behaviour is TurnLocomotionBroadcaster ||
                        behaviour is LocomotionActionsBroadcaster)
                    {
                        behaviour.enabled = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                    }
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
            }
            var teleports = all.Where(t => t != null && t.name == "TeleportControllerInteractor").ToArray();
            var right = teleports.Single(t => PathOf(t).Contains("ComprehensiveInteractorsRight"));
            foreach (var t in teleports)
            {
                t.gameObject.SetActive(t == right);
                PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
            }
            locomotor.Acceleration = 5f;
            locomotor.GroundDamping = 40f;
            locomotor.SpeedFactor = 12f;
            locomotor.FlattenInputVelocity = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(locomotor);
            var character = rig.GetComponentInChildren<Oculus.Interaction.Locomotion.CharacterController>(true);
            // The imported scene overrides the sample mask to 6, excluding the station's Default layer.
            character.LayerMask = character.LayerMask.value | 1;
            PrefabUtility.RecordPrefabInstancePropertyModifications(character);

            var panel = new GameObject("StationControlsMenu", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            panel.transform.SetParent(station.transform, false);
            panel.transform.localScale = Vector3.one * .0015f;
            var rect = (RectTransform)panel.transform;
            rect.sizeDelta = new Vector2(1000, 650);
            panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            panel.GetComponent<Image>().color = new Color(.025f, .065f, .12f, .98f);
            panel.GetComponent<Image>().raycastTarget = false;
            Text(rect, "Title", "LOST STATION | CONTROLS", 270, 60, 38).color = new Color(.5f, .93f, 1f);
            Text(rect, "Controls", SpaceStationControls.Help, 65, 310, 27);
            var status = Text(rect, "MissionStatus", "STATION OFFLINE - GRAB THE CORE", -143, 72, 25);
            status.color = new Color(1f, .8f, .45f);
            var close = Button(rect, "Close", "RESUME", -230);
            var reset = Button(rect, "Reset", "RESET MISSION", 230);
            UnityEventTools.AddVoidPersistentListener(close.onClick, controls.CloseMenu);
            UnityEventTools.AddVoidPersistentListener(reset.onClick, controls.RequestReset);
            QuickActionsAPI.AddRayCanvasInteraction(panel);
            var data = new SerializedObject(controls);
            Assign(data, "locomotor", locomotor); Assign(data, "head", rig.centerEyeAnchor);
            Assign(data, "rightTeleport", right.gameObject); Assign(data, "menuPanel", panel);
            Assign(data, "menuStatus", status); Assign(data, "mission", mission);
            data.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);

            var console = station.GetComponentInChildren<SpaceStationMissionConsole>(true);
            var help = console.transform.Find("HelpText").GetComponent<TMP_Text>();
            help.text = "LEFT STICK: WALK  |  RIGHT: TURN / TELEPORT\nHOLD GRIP TO CARRY CORE; RELEASE IN SOCKET\nTRIGGER: UI  |  LEFT MENU: CONTROLS / RESET";
            help.fontSize = 29;
            PrefabUtility.RecordPrefabInstancePropertyModifications(help);
            var oldReset = station.transform.Find("ResetInstructions");
            if (oldReset != null) oldReset.GetComponent<TMP_Text>().text = "LEFT MENU: CONTROLS / RESET";
        }

        private static TextMeshProUGUI Text(RectTransform parent, string name, string caption, float y, float height, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.rectTransform.anchoredPosition = new Vector2(0, y);
            text.rectTransform.sizeDelta = new Vector2(940, height);
            text.text = caption; text.fontSize = size; text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            return text;
        }

        private static Button Button(RectTransform parent, string name, string caption, float x)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = new Vector2(x, -250); rect.sizeDelta = new Vector2(420, 86);
            var button = go.GetComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = name == "Reset" ? new Color(.55f,.28f,.1f) : new Color(.08f,.35f,.48f);
            colors.highlightedColor = new Color(.2f,.6f,.7f); colors.pressedColor = new Color(.08f,.2f,.3f);
            button.colors = colors;
            Text(rect, "Label", caption, 0, 80, 30).rectTransform.sizeDelta = new Vector2(400,80);
            return button;
        }

        private static void Assign(SerializedObject data, string field, Object value) => data.FindProperty(field).objectReferenceValue = value;

        public static void Inspect()
        {
            var scene = EditorSceneManager.OpenScene(Week06TeamUIBuilder.ScenePath);
            var lines = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name.Contains("Locomot") || t.name.Contains("Slide") || t.name.Contains("Turn") ||
                    t.name.Contains("Teleport") || t.name.Contains("Settings") || t.name.Contains("Menu") ||
                    t.name.Contains("Character"))
                .Select(t => PathOf(t) + " | active=" + t.gameObject.activeInHierarchy + " | " +
                    string.Join(", ", t.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name)));
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/StationControls_Hierarchy.txt", lines);
            Debug.Log("STATION_CONTROLS_INSPECT_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }
}
#endif
