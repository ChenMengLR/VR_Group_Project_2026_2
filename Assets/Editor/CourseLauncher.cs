#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

namespace CourseSetup
{
    public static class CourseLauncher
    {
        [MenuItem("Course/Open Week 02 Computer Practice", priority = 0)]
        public static void OpenWeek02()
        {
            Guard();
            EditorApplication.ExecuteMenuItem("Meta/Meta XR Simulator/Deactivate");
            SetDesktopStartup(false);
            EditorSceneManager.OpenScene(Week02Builder.ScenePath);
            Debug.Log("Week 02 ready: XRI keyboard/mouse simulator; desktop OpenXR startup is disabled for this practice.");
        }

        [MenuItem("Course/Open Week 03 Computer Practice", priority = 1)]
        public static void OpenWeek03()
        {
            Guard();
            SetDesktopStartup(true);
            EditorSceneManager.OpenScene(Week03Builder.ScenePath);
            EditorApplication.ExecuteMenuItem("Meta/Meta XR Simulator/Activate");
            Debug.Log("Week 03 ready: Meta XR Simulator; this is computer practice, not Quest hardware validation.");
        }

        [MenuItem("Course/Open Solo Space Station", priority = 2)]
        public static void OpenSoloStation()
        {
            Guard();
            SetDesktopStartup(true);
            EditorSceneManager.OpenScene(SoloStationBuilder.ScenePath);
            EditorApplication.ExecuteMenuItem("Meta/Meta XR Simulator/Activate");
            Debug.Log("Solo station ready in Meta XR Simulator. Use the settings menu for Teleport, grab the core, and insert it into the socket.");
        }

        public static void SetDesktopStartup(bool enabled)
        {
            XRGeneralSettingsPerBuildTarget store;
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out store) && store != null)
            {
                var general = store.SettingsForBuildTarget(BuildTargetGroup.Standalone);
                if (general != null)
                {
                    general.InitManagerOnStart = enabled;
                    EditorUtility.SetDirty(general);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private static void Guard()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop Play and wait for compilation before switching course scenes.");
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                if (EditorSceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save the current scene before switching. No changes were discarded.");
        }
    }
}
#endif
