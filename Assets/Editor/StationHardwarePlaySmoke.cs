#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CourseProject;
using Oculus.Interaction;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using TMPro;
using Object = UnityEngine.Object;

namespace CourseSetup
{
    /// <summary>Real Play coroutines; synthetic core positioning, not controller input or Quest.</summary>
    [InitializeOnLoad]
    public static class StationHardwarePlaySmoke
    {
        private const string Key = "CourseSetup.HardwareSmoke.";
        private static bool launched;

        static StationHardwarePlaySmoke()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += ModeChanged;
            Application.logMessageReceived += RecordError;
        }

        public static void RunCli()
        {
            if (Application.isBatchMode || EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Use a windowed Windows Editor for Simulator verification.");
            XRGeneralSettingsPerBuildTarget store;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out store);
            SessionState.SetBool(Key+"Desktop", store.SettingsForBuildTarget(BuildTargetGroup.Standalone).InitManagerOnStart);
            SessionState.SetBool(Key+"Background", PlayerSettings.runInBackground);
            var feature = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature<ApiLayersFeature>();
            SessionState.SetBool(Key+"ApiLayers", feature.enabled);
            SessionState.SetInt(Key+"KnownErrors", 0);
            SessionState.SetInt(Key+"Errors", 0);
            SessionState.SetBool(Key+"Pending", true);
            SessionState.SetString(Key+"Started", DateTime.UtcNow.Ticks.ToString());
            CourseLauncher.OpenWeek06TeamUI();
            feature.enabled = false;
            EditorUtility.SetDirty(feature);
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            EditorApplication.isPlaying = true;
        }

        private static void RecordError(string message, string stack, LogType type)
        {
            if (!SessionState.GetBool(Key+"Pending", false) ||
                (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            var key = message.Contains("Illegal byte sequence") && stack.Contains("Meta.XR.MRUtilityKit.MRUK.OnSharedLibLog")
                ? "KnownErrors" : "Errors";
            SessionState.SetInt(Key+key, SessionState.GetInt(Key+key, 0)+1);
        }

        private static void Update()
        {
            if (!SessionState.GetBool(Key+"Pending",false)) return;
            var start = new DateTime(long.Parse(SessionState.GetString(Key+"Started","0")),DateTimeKind.Utc);
            if ((DateTime.UtcNow-start).TotalSeconds > 150)
            {
                Finish(new List<string>{"Smoke timed out"}, new List<string>(), 0);
                return;
            }
            if (EditorApplication.isPlaying && Time.time > 12 && !launched)
            {
                launched = true;
                new GameObject("HardwareSmokeRunner").AddComponent<HardwareSmokeRunner>();
            }
        }

        public static void Finish(List<string> failed, List<string> passedChecks, int frames)
        {
            int errors = SessionState.GetInt(Key+"Errors",0);
            if (errors > 0) failed.Add("Unexpected runtime errors: "+errors);
            var report = new HardwareReport {
                generatedUtc=DateTime.UtcNow.ToString("O"), passed=failed.Count==0,
                failedChecks=failed.ToArray(), passedChecks=passedChecks.ToArray(), videoFrames=frames,
                knownSdkLogDecodeExceptionCount=SessionState.GetInt(Key+"KnownErrors",0),
                unexpectedErrorCount=errors
            };
            File.WriteAllText(Path.Combine(StationHardwarePreview.Output,"Week07_Hardware_PlaySmoke.json"),
                JsonUtility.ToJson(report,true));
            SessionState.SetBool(Key+"Passed", report.passed);
            Debug.Log("WEEK07_HARDWARE_PLAY_SMOKE_"+(report.passed?"PASS":"FAIL"));
            EditorApplication.isPlaying=false;
        }

        private static void ModeChanged(PlayModeStateChange state)
        {
            if (state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key+"Pending",false)) return;
            CourseLauncher.SetDesktopStartup(SessionState.GetBool(Key+"Desktop",false));
            PlayerSettings.runInBackground=SessionState.GetBool(Key+"Background",false);
            var feature=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature<ApiLayersFeature>();
            feature.enabled=SessionState.GetBool(Key+"ApiLayers",false);
            EditorUtility.SetDirty(feature); AssetDatabase.SaveAssets();
            SessionState.SetBool(Key+"Pending",false);
            EditorApplication.Exit(SessionState.GetBool(Key+"Passed",false)?0:1);
        }

        [Serializable] private class HardwareReport
        {
            public string generatedUtc;
            public string verification="Windowed Unity Play with Meta XR Simulator. Test script positions core and sets hasBeenGrabbed; no controller grip, human test or Quest. Video is silent, 20 fps, fixed evidence camera.";
            public bool passed;
            public string[] passedChecks;
            public string[] failedChecks;
            public int videoFrames;
            public int knownSdkLogDecodeExceptionCount;
            public int unexpectedErrorCount;
        }
    }

    internal sealed class HardwareSmokeRunner : MonoBehaviour
    {
        private readonly List<string> passed = new List<string>();
        private readonly List<string> failed = new List<string>();
        private SpaceStationHardware hardware;
        private SpaceStationMission mission;
        private Rigidbody core;
        private Transform socket;
        private Vector3 start;
        private Vector3 leftClosed, rightClosed;
        private Camera camera;
        private RenderTexture target;
        private Texture2D frame;
        private MediaEncoder encoder;
        private int frameIndex, phase, oldCaptureRate;
        private float phaseStart;
        private bool finished;

        private void Start()
        {
            try
            {
                hardware=Object.FindFirstObjectByType<SpaceStationHardware>();
                mission=Object.FindFirstObjectByType<SpaceStationMission>();
                core=GameObject.Find("EnergyCore_GrabAndRelease").GetComponent<Rigidbody>();
                socket=GameObject.Find("EnergySocket_SnapAnchor").transform;
                start=core.position; leftClosed=hardware.LeftLeaf.localPosition; rightClosed=hardware.RightLeaf.localPosition;
                Check(hardware.IsConfigured,"Hardware references and mechanism clip assigned");
                Check(XRSettings.isDeviceActive,"Simulator XR device active");
                Check(core.GetComponent<CapsuleCollider>()!=null && core.GetComponent<SphereCollider>()==null,
                    "Core uses replacement capsule collision");
                Check(hardware.PassageBlocked && !mission.IsHatchOpen,"Initial closed gate blocks passage");
                Check(core.GetComponentInChildren<GrabInteractable>(true).enabled,"Meta controller grab target enabled");
                oldCaptureRate=Time.captureFramerate; Time.captureFramerate=20;
                camera=StationHardwarePreview.CreateCamera();
                StationHardwarePreview.Pose(camera,new Vector3(-.1f,1.8f,-.65f),new Vector3(2.05f,1.25f,1.1f),64);
                target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
                frame=new Texture2D(1280,720,TextureFormat.RGBA32,false);
                camera.targetTexture=target;
                AddCaption();
                encoder=new MediaEncoder(Path.Combine(StationHardwarePreview.Output,"Week07_Hardware_AutomatedPlay.mp4"),
                    new VideoTrackAttributes{frameRate=new MediaRational(20,1),width=1280,height=720,includeAlpha=false});
                core.isKinematic=true;
            }
            catch(Exception ex){failed.Add(ex.ToString());Finish();}
        }

        private void LateUpdate()
        {
            if(finished)return;
            try
            {
                if(phase==0)
                {
                    if(frameIndex<40)
                    {
                        float t=Mathf.Clamp01((frameIndex-15)/24f);
                        core.position=Vector3.Lerp(start,socket.position,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.24f);
                    }
                    if(frameIndex==40)InsertSynthetically();
                    if(frameIndex==47)Check(mission.IsInserted && hardware.PassageBlocked && !mission.IsHatchOpen,
                        "Insertion locks core; passage remains blocked during charging");
                    if(frameIndex==70)Check(hardware.OpenFraction>0 && hardware.OpenFraction<1 && hardware.PassageBlocked,
                        "Split leaves move while passage collider remains enabled");
                    CaptureFrame();
                    frameIndex++;
                    if(frameIndex==120)
                    {
                        Check(hardware.IsOpen && mission.IsHatchOpen && !hardware.PassageBlocked,
                            "Full sequence opens both leaves and releases passage collider");
                        Check(mission.CurrentStatus=="HATCH OPEN\nPROCEED TO EXIT","Mission status waits for complete opening");
                        Check(Mathf.Abs(hardware.LeftLeaf.localPosition.z-leftClosed.z)>1 &&
                            Mathf.Abs(hardware.RightLeaf.localPosition.z-rightClosed.z)>1,"Both leaves complete 1.02 metre slide");
                        StationHardwarePreview.Save(camera,Path.Combine(StationHardwarePreview.Output,"Week07_Hardware_PlayOpen.png"));
                        encoder.Dispose();encoder=null;
                        mission.ResetMission(); phase=1;phaseStart=Time.time;
                    }
                    return;
                }
                if(phase==1 && Time.time-phaseStart>.3f)
                { InsertSynthetically(); phase=2;phaseStart=Time.time; }
                else if(phase==2 && Time.time-phaseStart>1.5f)
                {
                    Check(hardware.OpenFraction>0 && hardware.OpenFraction<1,"Second insertion reaches partial opening");
                    mission.ResetMission(); phase=3;phaseStart=Time.time;
                }
                else if(phase==3 && Time.time-phaseStart>3.2f)
                {
                    Check(!hardware.IsOpen && !mission.IsInserted && !mission.IsHatchOpen && hardware.PassageBlocked,
                        "Mid-animation reset remains closed after original sequence duration");
                    Check(Vector3.Distance(hardware.LeftLeaf.localPosition,leftClosed)<.001f &&
                        Vector3.Distance(hardware.RightLeaf.localPosition,rightClosed)<.001f,"Reset restores both leaf poses");
                    Check(!core.isKinematic && core.GetComponent<Grabbable>().enabled &&
                        core.GetComponentInChildren<GrabInteractable>(true).enabled,"Reset restores dynamic core and grab targets");
                    Check(mission.CurrentStatus=="STATION OFFLINE\nGRAB THE CORE","Reset restores mission status");
                    StationHardwarePreview.Save(camera,Path.Combine(StationHardwarePreview.Output,"Week07_Hardware_PlayReset.png"));
                    Finish();
                }
            }
            catch(Exception ex){failed.Add(ex.ToString());Finish();}
        }

        private void InsertSynthetically()
        {
            core.isKinematic=false;
            core.linearVelocity=Vector3.zero;core.angularVelocity=Vector3.zero;
            core.position=socket.position;core.rotation=socket.rotation;
            typeof(SpaceStationMission).GetField("hasBeenGrabbed",BindingFlags.NonPublic|BindingFlags.Instance)
                .SetValue(mission,true);
            Physics.SyncTransforms();
        }
        private void CaptureFrame()
        {
            var old=RenderTexture.active;
            camera.Render();RenderTexture.active=target;
            frame.ReadPixels(new Rect(0,0,1280,720),0,0);frame.Apply();
            if(!encoder.AddFrame(frame))throw new InvalidOperationException("MP4 frame rejected");
            RenderTexture.active=old;
        }
        private void AddCaption()
        {
            var canvas=new GameObject("EvidenceCaption",typeof(Canvas)).GetComponent<Canvas>();
            canvas.transform.SetParent(camera.transform,false);canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=100;
            var panel=new GameObject("CaptionPanel",typeof(RectTransform),typeof(Image));
            panel.transform.SetParent(canvas.transform,false);panel.GetComponent<Image>().color=new Color(0,0,0,.85f);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(1,.10f);
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            var text=new GameObject("Caption",typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(panel.transform,false);text.rectTransform.anchorMin=Vector2.zero;
            text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
            text.fontSize=19;text.alignment=TextAlignmentOptions.Center;text.color=Color.white;
            text.text="AUTOMATED UNITY PLAY  /  NOT QUEST\nScript-positioned core  |  Real hatch coroutine  |  Silent preview";
        }
        private void Check(bool condition,string message){(condition?passed:failed).Add(message);}
        private void Finish()
        {
            if(finished)return;finished=true;
            if(encoder!=null){encoder.Dispose();encoder=null;}
            Time.captureFramerate=oldCaptureRate;
            StationHardwarePlaySmoke.Finish(failed,passed,frameIndex);
        }
        private void OnDestroy()
        {
            if(encoder!=null)encoder.Dispose();
            Time.captureFramerate=oldCaptureRate;
            if(target!=null)Destroy(target);
            if(frame!=null)Destroy(frame);
        }
    }
}
#endif
