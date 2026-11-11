#if UNITY_EDITOR
using System;
using System.IO;
using CourseProject;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CourseSetup
{
    public static class StationCabinVisualQA
    {
        public static string Output => StationGravityLeverBuilder.Output;

        [Serializable] private sealed class Report
        {
            public string scope = "Matched Unity Editor camera renders of the saved scene; lighting explicitly previewed at 0% and 100%.";
            public string generatedUtc, error;
            public bool passed;
            public string[] textureAssets = new string[3];
            public int powerBanks, fillLights, lensRenderers;
            public float[] entranceAndDockEmergency, entranceAndDockRestored;
        }

        [MenuItem("Course/Exploration Level/Capture Cabin Lighting Comparison")]
        public static void Capture()
        {
            StationRouteBuilder.RequireSavedEditScene();
            Directory.CreateDirectory(Output);
            var report = new Report { generatedUtc = DateTime.UtcNow.ToString("O") };
            EditorSceneManager.OpenScene(StationRouteBuilder.ScenePath);
            Camera camera = null;
            try
            {
                Transform shell = GameObject.Find("RecoveryRoute_Environment").transform;
                report.textureAssets[0] = SurfaceTexture(shell, "MainDeck");
                report.textureAssets[1] = SurfaceTexture(shell, "SouthHull");
                report.textureAssets[2] = SurfaceTexture(shell, "ReactorIsland");
                Require(report.textureAssets[0] != report.textureAssets[1], "The deck and pressure hull should visibly use different textures.");
                var power = Object.FindFirstObjectByType<StationCabinPower>();
                Require(power != null, "Saved scene is missing StationCabinPower.");
                CheckBindings(power, report);

                camera = StationHardwarePreview.CreateCamera();
                power.SetPreviewProgress(0f);
                report.entranceAndDockEmergency = CapturePair(camera, "Emergency");
                power.SetPreviewProgress(.5f);
                Require(power.GetBankPower(0) > power.GetBankPower(1) && power.GetBankPower(1) > power.GetBankPower(2),
                    "Power should travel from the coupler through the central deck to the upper bay.");
                power.SetPreviewProgress(1f);
                report.entranceAndDockRestored = CapturePair(camera, "Restored");
                for (int i = 0; i < 2; i++)
                    Require(report.entranceAndDockRestored[i] > report.entranceAndDockEmergency[i] + 1f,
                        "Mean picture brightness should rise after restoration at matched viewpoint " + i + ".");
                report.passed = true;
            }
            catch (Exception error)
            {
                report.error = error.ToString();
                throw;
            }
            finally
            {
                File.WriteAllText(Path.Combine(Output, "CabinVisualQA.json"), JsonUtility.ToJson(report, true));
                if (camera != null) Object.DestroyImmediate(camera.gameObject);
                // Discard all temporary lighting and camera changes, including failed captures.
                EditorSceneManager.OpenScene(StationRouteBuilder.ScenePath);
            }
        }

        private static string SurfaceTexture(Transform shell, string name)
        {
            Transform surface = shell.Find(name);
            Require(surface != null, "Saved scene is missing surface " + name);
            foreach (var renderer in surface.GetComponentsInChildren<MeshRenderer>())
            {
                Material material = renderer.sharedMaterial;
                if (material == null || !material.HasProperty("_BaseMap")) continue;
                Texture texture = material.GetTexture("_BaseMap");
                string path = AssetDatabase.GetAssetPath(texture);
                if (texture != null && texture.width >= 64 && !string.IsNullOrEmpty(path)) return path;
            }
            throw new InvalidOperationException(name + " has no authored base-color texture.");
        }

        private static void CheckBindings(StationCabinPower power, Report report)
        {
            var component = new SerializedObject(power);
            Require(component.FindProperty("mission").objectReferenceValue == Object.FindFirstObjectByType<SpaceStationMission>(),
                "Cabin power must be bound to the scene mission.");
            var banks = component.FindProperty("banks");
            report.powerBanks = banks.arraySize;
            Require(report.powerBanks == 3, "Three authored lighting circuits expected.");
            for (int i = 0; i < banks.arraySize; i++)
            {
                var bank = banks.GetArrayElementAtIndex(i);
                var fill = bank.FindPropertyRelative("fill").objectReferenceValue as Light;
                if (fill != null)
                {
                    Require(fill.shadows == LightShadows.None, "Circuit fill should not render dynamic shadows: " + fill.name);
                    report.fillLights++;
                }
                var lenses = bank.FindPropertyRelative("fixtures");
                Require(lenses.arraySize > 0, "Circuit " + i + " has no visible fixtures.");
                for (int k = 0; k < lenses.arraySize; k++)
                {
                    var renderer = lenses.GetArrayElementAtIndex(k).objectReferenceValue as Renderer;
                    Require(renderer != null && renderer.sharedMaterial != null, "A circuit fixture or its material is missing.");
                    report.lensRenderers++;
                }
            }
        }

        private static float[] CapturePair(Camera camera, string state)
        {
            StationHardwarePreview.Pose(camera, new Vector3(-10.95f, 1.62f, -.48f), new Vector3(-1.2f, 1.40f, 2.0f), 70);
            float entrance = SaveAndMeasure(camera, "Entrance_" + state);
            StationHardwarePreview.Pose(camera, new Vector3(-1.6f, 1.66f, -1.52f), new Vector3(2.63f, 1.48f, 1.60f), 70);
            return new[] { entrance, SaveAndMeasure(camera, "Dock_" + state) };
        }

        // Photograph the actual mission (door, core, signs and lighting together) during the Play check.
        internal static void CaptureMissionState(string state)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run the mission in Play mode.");
            var camera=StationHardwarePreview.CreateCamera();
            try
            {
                CapturePair(camera, "Play_"+state);
                StationHardwarePreview.Pose(camera,new Vector3(-5.95f,1.66f,.95f),new Vector3(-9.65f,1.88f,-2.32f),72);
                StationHardwarePreview.Save(camera,Path.Combine(Output,"Viewport_Play_"+state+".png"));
            }
            finally { Object.Destroy(camera.gameObject); }
        }

        // 0..255 mean encoded luma of the exported PNG: a reproducible picture check, not a photometric measurement.
        private static float SaveAndMeasure(Camera camera, string stem)
        {
            string path = Path.Combine(Output, stem + ".png");
            StationHardwarePreview.Save(camera, path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Require(texture.LoadImage(File.ReadAllBytes(path)), "Could not decode " + path);
                var pixels = texture.GetPixels32();
                double sum = 0;
                for (int i = 0; i < pixels.Length; i += 4)
                    sum += .2126 * pixels[i].r + .7152 * pixels[i].g + .0722 * pixels[i].b;
                return (float)(sum / ((pixels.Length + 3) / 4));
            }
            finally { Object.DestroyImmediate(texture); }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
