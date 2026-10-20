#if UNITY_EDITOR
using System.IO;
using CourseProject;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CourseSetup
{
    public static class StationHardwarePreview
    {
        public static string Output => Path.GetFullPath(Path.Combine(Application.dataPath,
            "..", "CourseValidation", "Runtime"));

        // Run in a graphics-enabled Editor. These are scene previews, not headset captures.
        public static void RebuildAndCapture()
        {
            Week06TeamUIBuilder.Build();
            Capture();
        }

        [MenuItem("Course/Week 07/Capture Hardware Editor Previews")]
        public static void Capture()
        {
            var scene = EditorSceneManager.OpenScene(Week06TeamUIBuilder.ScenePath);
            var hardware = Object.FindFirstObjectByType<SpaceStationHardware>();
            var core = GameObject.Find("EnergyCore_GrabAndRelease").transform;
            var corePosition = core.position;
            var coreRotation = core.rotation;
            var camera = CreateCamera();
            try
            {
                hardware.ResetHardware();
                Pose(camera, new Vector3(1.18f,1.55f,.45f), new Vector3(1.60f,1.00f,1.30f), 43);
                Save(camera, Path.Combine(Output, "Week07_CoreDock_EditorPreview.png"));
                Pose(camera, new Vector3(.45f,1.6f,.15f), new Vector3(3.25f,1.38f,.75f), 64);
                Save(camera, Path.Combine(Output, "Week07_AirlockClosed_EditorPreview.png"));
                Pose(camera, new Vector3(-.1f,1.95f,-.75f), new Vector3(2.05f,1.25f,1.1f), 64);
                Save(camera, Path.Combine(Output, "Week07_Station_EditorPreview.png"));
                core.SetPositionAndRotation(GameObject.Find("EnergySocket_SnapAnchor").transform.position, Quaternion.identity);
                hardware.ApplySequenceTime(SpaceStationHardware.SequenceDuration);
                Pose(camera, new Vector3(1.18f,1.55f,.45f), new Vector3(1.60f,1.00f,1.30f), 43);
                Save(camera, Path.Combine(Output, "Week07_CoreDockPowered_EditorPreview.png"));
                Pose(camera, new Vector3(.45f,1.6f,.15f), new Vector3(3.25f,1.38f,.75f), 64);
                Save(camera, Path.Combine(Output, "Week07_AirlockOpen_EditorPreview.png"));
            }
            finally
            {
                core.SetPositionAndRotation(corePosition, coreRotation);
                hardware.ResetHardware();
                Object.DestroyImmediate(camera.gameObject);
                // Discard temporary preview poses without saving the scene.
                EditorSceneManager.OpenScene(Week06TeamUIBuilder.ScenePath);
            }
            Debug.Log("WEEK07_HARDWARE_PREVIEWS_OK");
            EditorApplication.Exit(0);
        }

        public static Camera CreateCamera()
        {
            Directory.CreateDirectory(Output);
            var camera = new GameObject("HardwareEvidenceCamera").AddComponent<Camera>();
            camera.enabled = false;
            camera.nearClipPlane = .03f;
            camera.farClipPlane = 50;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().allowXRRendering = false;
            return camera;
        }

        public static void Pose(Camera camera, Vector3 position, Vector3 target, float fieldOfView)
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.fieldOfView = fieldOfView;
        }

        public static void Save(Camera camera, string path, int width = 1600, int height = 900)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                frame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
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
    }
}
#endif
