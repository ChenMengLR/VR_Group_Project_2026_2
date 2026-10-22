#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using CourseProject;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace CourseSetup
{
    [InitializeOnLoad]
    public static class StationControlsPlaySmoke
    {
        private const string Key="Course.ControlsSmoke.";
        static StationControlsPlaySmoke()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += ModeChanged;
            Application.logMessageReceived += Record;
        }
        public static void RunCli()
        {
            if(Application.isBatchMode) throw new InvalidOperationException("Use a windowed Editor for XR Play.");
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out XRGeneralSettingsPerBuildTarget store);
            SessionState.SetBool(Key+"Desktop",store.SettingsForBuildTarget(BuildTargetGroup.Standalone).InitManagerOnStart);
            SessionState.SetBool(Key+"Background",PlayerSettings.runInBackground);
            var feature=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature<ApiLayersFeature>();
            SessionState.SetBool(Key+"Api",feature.enabled);
            CourseLauncher.OpenWeek06TeamUI();
            feature.enabled=false; EditorUtility.SetDirty(feature);
            PlayerSettings.runInBackground=true; AssetDatabase.SaveAssets();
            SessionState.SetInt(Key+"Errors",0); SessionState.SetInt(Key+"Known",0);
            SessionState.SetInt(Key+"GroundWarnings",0); SessionState.SetBool(Key+"Launched",false);
            SessionState.SetString(Key+"Time",DateTime.UtcNow.Ticks.ToString());
            SessionState.SetBool(Key+"Pending",true);
            EditorApplication.isPlaying=true;
        }
        private static void Record(string message,string stack,LogType type)
        {
            if(!SessionState.GetBool(Key+"Pending",false))return;
            if(message.Contains("ground could not be found below the locomotor"))
                SessionState.SetInt(Key+"GroundWarnings",SessionState.GetInt(Key+"GroundWarnings",0)+1);
            if(type!=LogType.Error && type!=LogType.Exception && type!=LogType.Assert)return;
            string category=message.Contains("Illegal byte sequence") && stack.Contains("MRUK.OnSharedLibLog") ? "Known" : "Errors";
            SessionState.SetInt(Key+category,SessionState.GetInt(Key+category,0)+1);
        }
        private static void Update()
        {
            if(!SessionState.GetBool(Key+"Pending",false))return;
            var start=new DateTime(long.Parse(SessionState.GetString(Key+"Time","0")),DateTimeKind.Utc);
            if((DateTime.UtcNow-start).TotalSeconds>120){Finish(new List<string>(),"Play timeout");return;}
            if(EditorApplication.isPlaying && Time.time>10 && !SessionState.GetBool(Key+"Launched",false))
            {
                SessionState.SetBool(Key+"Launched",true);
                new GameObject("ControlsSmokeRunner").AddComponent<ControlsSmokeRunner>();
            }
        }
        public static void Finish(List<string> checks,string error)
        {
            int errors=SessionState.GetInt(Key+"Errors",0), ground=SessionState.GetInt(Key+"GroundWarnings",0);
            if(errors>0 || ground>0)error=(error??"")+$" Unexpected errors={errors}; ground warnings={ground}";
            var report=new Report{generatedUtc=DateTime.UtcNow.ToString("O"),passed=string.IsNullOrEmpty(error),
                checks=checks.ToArray(),error=error,unexpectedErrors=errors,groundWarnings=ground,
                knownMrukLogDecodeExceptions=SessionState.GetInt(Key+"Known",0)};
            Directory.CreateDirectory("CourseValidation/Runtime");
            File.WriteAllText("CourseValidation/Runtime/StationControls_PlaySmoke.json",JsonUtility.ToJson(report,true));
            SessionState.SetBool(Key+"Passed",report.passed);
            Debug.Log("STATION_CONTROLS_PLAY_"+(report.passed?"PASS":"FAIL"));
            EditorApplication.isPlaying=false;
        }
        private static void ModeChanged(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key+"Pending",false))return;
            EditorApplication.ExecuteMenuItem("Meta/Meta XR Simulator/Deactivate");
            CourseLauncher.SetDesktopStartup(SessionState.GetBool(Key+"Desktop",true));
            PlayerSettings.runInBackground=SessionState.GetBool(Key+"Background",false);
            var feature=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature<ApiLayersFeature>();
            feature.enabled=SessionState.GetBool(Key+"Api",false); EditorUtility.SetDirty(feature); AssetDatabase.SaveAssets();
            SessionState.SetBool(Key+"Pending",false);
            EditorApplication.Exit(SessionState.GetBool(Key+"Passed",false)?0:1);
        }
        [Serializable] private class Report
        {
            public string generatedUtc;
            public string scope="Windowed Unity Play using Meta XR Simulator; synthetic stick input through production controls, real lifecycle/frames/physics. No physical Quest input, ray-click acceptance or video recording.";
            public bool passed;
            public string[] checks;
            public string error;
            public int unexpectedErrors, groundWarnings, knownMrukLogDecodeExceptions;
        }
    }

    [DefaultExecutionOrder(-40)]
    internal sealed class ControlsSmokeRunner:MonoBehaviour
    {
        private readonly List<string> checks=new List<string>();
        private SpaceStationControls controls;
        private Transform origin;
        private Vector3 start;
        private float elapsed;
        private int phase;
        private bool done;
        private void Start()
        {
            try
            {
                controls=UnityEngine.Object.FindFirstObjectByType<SpaceStationControls>();
                origin=UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>().transform;
                Check(XRSettings.isDeviceActive,"Simulator XR device active");
                Check(!controls.Locomotor.IgnoringVelocity && controls.Locomotor.IsGrounded,"Actual Start finds station floor and enables locomotion");
                Check(UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>().centerEyeAnchor.position.y>1,"XR tracked head height initialized");
                controls.enabled=false; // Replace only hardware polling with synthetic test vectors.
                controls.ProcessInput(Vector2.zero,Vector2.zero,true);
                start=origin.position;
            }
            catch(Exception e){Finish(e.ToString());}
        }
        private void Update()
        {
            if(done)return;
            try
            {
                elapsed+=Time.deltaTime;
                if(phase==0)
                {
                    controls.ProcessInput(Vector2.up,Vector2.zero,true);
                    if(elapsed<1.3f)return;
                    Check(Vector3.Distance(start,origin.position)>1.4f,"Synthetic left stick moves rig in actual Play frames");
                    controls.ToggleMenu(); start=origin.position; phase=1; elapsed=0;
                }
                else if(phase==1)
                {
                    controls.ProcessInput(Vector2.up,Vector2.right,true);
                    if(elapsed<.5f)return;
                    Check(controls.MenuVisible && controls.MenuPanel.activeInHierarchy && !controls.RightTeleport.activeSelf &&
                        Vector3.Distance(start,origin.position)<.06f,"Visible controls menu suppresses locomotion during Play");
                    controls.CloseMenu(); controls.ProcessInput(Vector2.zero,Vector2.zero,true);
                    Check(controls.RightTeleport.activeInHierarchy,"Right teleport restores after closing menu and centering sticks");
                    Finish(null);
                }
            }
            catch(Exception e){Finish(e.ToString());}
        }
        private void Check(bool condition,string message)
        {if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        private void Finish(string error)
        {if(done)return;done=true;StationControlsPlaySmoke.Finish(checks,error);}
    }
}
#endif
