#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CourseProject;
using Oculus.Interaction;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using Object=UnityEngine.Object;

namespace CourseSetup
{
    // Captures production Play logic with synthetic input. Never labelled as physical Quest footage.
    [InitializeOnLoad]
    public static class StationRouteVideo
    {
        private const string Key="Course.RouteVideo.";
        private const string Request="Temp/StationRouteVideo.request";
        public static readonly string Output=Path.GetFullPath("CourseValidation/Runtime/RouteVideo_20261029");
        private static bool launched;
        static StationRouteVideo()
        {EditorApplication.update+=Update;EditorApplication.playModeStateChanged+=ModeChanged;Application.logMessageReceived+=Log;}
        [MenuItem("Course/Exploration Level/Record Route Walkthrough")]
        public static void Record()
        {
            StationRouteBuilder.RequireSavedEditScene();Directory.CreateDirectory(Output);
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out XRGeneralSettingsPerBuildTarget store);
            SessionState.SetBool(Key+"Desktop",store.SettingsForBuildTarget(BuildTargetGroup.Standalone).InitManagerOnStart);
            SessionState.SetBool(Key+"Background",PlayerSettings.runInBackground);
            var api=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature<ApiLayersFeature>();
            SessionState.SetBool(Key+"Api",api.enabled);
            CourseLauncher.OpenRecoveryRoute();api.enabled=false;EditorUtility.SetDirty(api);
            PlayerSettings.runInBackground=true;AssetDatabase.SaveAssets();
            SessionState.SetInt(Key+"Errors",0);SessionState.SetInt(Key+"Known",0);
            SessionState.SetString(Key+"Start",DateTime.UtcNow.Ticks.ToString());
            SessionState.SetBool(Key+"Pending",true);SessionState.SetBool(Key+"Finishing",false);
            launched=false;EditorApplication.isPlaying=true;
        }
        private static void Update()
        {
            if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&File.Exists(Request))
            {
                File.Delete(Request);
                try{Record();}catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"request_error.txt"),e.ToString());Debug.LogException(e);}
            }
            if(!SessionState.GetBool(Key+"Pending",false)||SessionState.GetBool(Key+"Finishing",false))return;
            if((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString(Key+"Start","0")),DateTimeKind.Utc)).TotalSeconds>420)
            {Finish(new VideoReport{error="Recording timed out"});return;}
            if(EditorApplication.isPlaying&&Time.time>10&&!launched)
            {launched=true;new GameObject("RouteVideoRunner").AddComponent<RouteVideoRunner>();}
        }
        private static void Log(string message,string stack,LogType type)
        {
            if(!SessionState.GetBool(Key+"Pending",false)||(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert))return;
            var k=message.Contains("Illegal byte sequence")&&stack.Contains("MRUK.OnSharedLibLog")?"Known":"Errors";
            SessionState.SetInt(Key+k,SessionState.GetInt(Key+k,0)+1);
        }
        public static void Finish(VideoReport report)
        {
            if(SessionState.GetBool(Key+"Finishing",false))return;
            SessionState.SetBool(Key+"Finishing",true);
            report.generatedUtc=DateTime.UtcNow.ToString("O");report.unexpectedErrors=SessionState.GetInt(Key+"Errors",0);
            report.knownSdkDecodeExceptions=SessionState.GetInt(Key+"Known",0);
            report.passed=string.IsNullOrEmpty(report.error)&&report.missionComplete&&report.unexpectedErrors==0;
            File.WriteAllText(Path.Combine(Output,"capture.json"),JsonUtility.ToJson(report,true));
            Debug.Log("STATION_ROUTE_VIDEO_"+(report.passed?"PASS":"FAIL")+" frames="+report.frames);
            EditorApplication.isPlaying=false;
        }
        private static void ModeChanged(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key+"Pending",false))return;
            CourseLauncher.SetDesktopStartup(SessionState.GetBool(Key+"Desktop",true));
            PlayerSettings.runInBackground=SessionState.GetBool(Key+"Background",false);
            var api=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature<ApiLayersFeature>();
            api.enabled=SessionState.GetBool(Key+"Api",false);EditorUtility.SetDirty(api);AssetDatabase.SaveAssets();
            SessionState.SetBool(Key+"Pending",false);SessionState.SetBool(Key+"Finishing",false);
        }
        [Serializable] public class Chapter { public float time; public string title; public string detail; }
        [Serializable] public class Sample { public float time; public float x,z; public bool carrying,powered; }
        [Serializable] public class VideoReport
        {
            public string generatedUtc;
            public string scope="Unity Play with Meta XR Simulator. Scripted stick input through production locomotor; synthetic Grabbable pointer input; separate eye-level evidence camera. Live game audio mix. Not human or physical Quest footage.";
            public bool passed,missionComplete;public string error;public int frames,width=1920,height=1080,fps=30,sampleRate;
            public long audioSampleFrames;public float audioPeak;public double audioRms;
            public int unexpectedErrors,knownSdkDecodeExceptions;public Chapter[] chapters;public Sample[] positions;
        }
    }

    [DefaultExecutionOrder(-40)]
    internal sealed class RouteVideoRunner:MonoBehaviour
    {
        private SpaceStationControls controls;private SpaceStationMission mission;private SpaceStationRoute route;
        private Transform head,socket;private Grabbable grab;private Rigidbody core;private Camera camera;
        private MediaEncoder encoder;private RenderTexture target;private Texture2D pixels;
        private readonly List<StationRouteVideo.Chapter> chapters=new List<StationRouteVideo.Chapter>();
        private readonly List<StationRouteVideo.Sample> positions=new List<StationRouteVideo.Sample>();
        private Vector3 moveWorld,lookAt;private bool recording,done,holding,autoCarry,audioStarted;
        private int frames,oldCaptureRate,sampleRate;private long sampleFrames,sampleCount;private float peak;private double squareSum;
        private Pose pointer;private IEnumerator flow;private object waiting;private float waitUntil;
        private readonly Stack<IEnumerator> routines=new Stack<IEnumerator>();
        private void Start()
        {
            try
            {
                controls=Object.FindFirstObjectByType<SpaceStationControls>();mission=Object.FindFirstObjectByType<SpaceStationMission>();
                route=Object.FindFirstObjectByType<SpaceStationRoute>();core=route.Core;grab=core.GetComponent<Grabbable>();
                head=(Transform)new SerializedObject(controls).FindProperty("head").objectReferenceValue;
                socket=GameObject.Find("EnergySocket_SnapAnchor").transform;
                controls.enabled=false;controls.ProcessInput(Vector2.zero,Vector2.zero,true);
                foreach(var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))listener.enabled=false;
                camera=StationHardwarePreview.CreateCamera();camera.name="RouteVideoCamera";camera.fieldOfView=75;
                camera.transform.SetPositionAndRotation(head.position,Quaternion.LookRotation(Vector3.right));
                camera.gameObject.AddComponent<AudioListener>();
                foreach(var xrCamera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))if(xrCamera!=camera)xrCamera.enabled=false;
                oldCaptureRate=Time.captureFramerate;Time.captureFramerate=30;sampleRate=AudioSettings.outputSampleRate;
                target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
                pixels=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
                encoder=new MediaEncoder(Path.Combine(StationRouteVideo.Output,"StationRoute_Walkthrough_Raw.mp4"),
                    new VideoTrackAttributes{frameRate=new MediaRational(30,1),width=1920,height=1080,includeAlpha=false},
                    new AudioTrackAttributes{channelCount=2,sampleRate=new MediaRational(sampleRate,1),language="und"});
                if(!AudioRenderer.Start())throw new InvalidOperationException("AudioRenderer could not enter recording mode");audioStarted=true;
                mission.ResetMission();lookAt=new Vector3(3.25f,1.6f,.75f);
                routines.Push(Flow());StartCoroutine(Capture());
            }
            catch(Exception e){Finish(e.ToString());}
        }
        private void Update()
        {
            if(done||controls==null)return;
            try
            {
                if(waiting!=null&&Time.time<waitUntil){}else
                {
                    waiting=null;
                    if(routines.Count>0)
                    {
                        var top=routines.Peek();
                        if(!top.MoveNext())routines.Pop();
                        else if(top.Current is IEnumerator nested)routines.Push(nested);
                        else if(top.Current is float seconds){waiting=true;waitUntil=Time.time+seconds;}
                    }
                    else{Finish(null);return;}
                }
                var forward=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;
                var right=Vector3.Cross(Vector3.up,forward);
                controls.ProcessInput(new Vector2(Vector3.Dot(moveWorld,right),Vector3.Dot(moveWorld,forward)),Vector2.zero,true);
            }
            catch(Exception e){Finish(e.ToString());}
        }
        private IEnumerator Flow()
        {
            yield return .5f;recording=true;
            Chapter("01  入口：确认目标舱门","先看远处的封闭舱门，再沿蓝色灯带寻找核心。 ");
            yield return Look(new Vector3(3.25f,1.6f,.75f),2.6f);
            yield return Walk(new Vector3(-8.8f,0,1.65f));
            Chapter("02  西侧缓坡：进入高架补给区","沿蓝灯上坡；平台比主地面高 0.6 米。");
            yield return Walk(new Vector3(-8.8f,.6f,6.55f));
            yield return Walk(new Vector3(-7.8f,.6f,6.55f));
            yield return Walk(new Vector3(-5.1f,.6f,6.05f));
            Chapter("03  补给台：抓取能量核心","按住 Grip 搬运核心；抓取后返程灯带变为琥珀色。");
            yield return Look(core.position,2f);
            pointer=new Pose(core.position,core.rotation);grab.ProcessPointerEvent(new PointerEvent(7701,PointerEventType.Hover,pointer));
            grab.ProcessPointerEvent(new PointerEvent(7701,PointerEventType.Select,pointer));holding=true;
            yield return MoveCoreToCarry();autoCarry=true;yield return 1.2f;
            if(!mission.HasAcquiredCore)throw new InvalidOperationException("Core acquisition not observed");
            Chapter("04  东侧缓坡：携带核心返回","从另一侧下坡，沿琥珀灯走回主通道。");
            yield return Walk(new Vector3(-3.35f,.6f,6.65f));
            // Clear the ramp parapet end with the full production capsule before turning south.
            yield return Walk(new Vector3(1.9f,0,6.65f));
            yield return Walk(new Vector3(1.9f,0,5f));
            Chapter("05  中央通道：返回原工作台","绕过中央设备，从工作台正面接近插槽。");
            yield return Walk(new Vector3(-.8f,0,5f));
            yield return Walk(new Vector3(-.8f,0,-.15f));
            yield return Walk(new Vector3(1.25f,0,.15f));
            Chapter("06  插入核心：恢复舱内供电","把核心放进插槽并松开 Grip，等待舱内照明依次恢复。");
            yield return Look(socket.position,1.3f);autoCarry=false;
            yield return MoveCore(socket.position,socket.rotation,2f);
            yield return .6f;
            grab.ProcessPointerEvent(new PointerEvent(7701,PointerEventType.Unselect,pointer));
            grab.ProcessPointerEvent(new PointerEvent(7701,PointerEventType.Unhover,pointer));holding=false;
            yield return .15f;
            if(!mission.IsInserted)throw new InvalidOperationException("Insertion failed at "+core.position);
            if (mission.RequiresLever)
            {
                yield return Look(mission.AirlockLever.Pivot.position, 2.7f);
                Chapter("07  手动解锁：拉下开门拉杆", "握住舱门旁的拉杆并拉到底，启动舱门开启动画。");
                yield return StationLeverDemo.Pull(mission.AirlockLever, 7702);
            }
            yield return Look(new Vector3(3.25f,1.4f,.75f),4.2f);
            if(!mission.IsHatchOpen)throw new InvalidOperationException("Hatch did not finish opening");
            Chapter("08  绿色出口：完成任务","确认门已完全打开，再通过舱门到达出口。");
            yield return Walk(new Vector3(2f,0,.75f));
            yield return Walk(new Vector3(4.45f,0,.75f));
            yield return .5f;if(!mission.IsEscaped)throw new InvalidOperationException("Exit did not complete mission");
            Chapter("路线完成 · MISSION COMPLETE","入口 → 上坡取芯 → 下坡返程 → 插入供电 → 开门撤离");
            yield return Look(new Vector3(2.1f,1.5f,1.3f),4f);
        }
        private void Chapter(string title,string detail)
        {chapters.Add(new StationRouteVideo.Chapter{time=frames/30f,title=title,detail=detail});}
        private IEnumerator Walk(Vector3 feet)
        {
            var delta=feet-head.position;delta.y=0;
            yield return Look(head.position+delta.normalized*5-Vector3.up*.18f,.65f);
            float timeout=Time.time+15;
            while(true)
            {
                delta=feet-head.position;delta.y=0;if(delta.magnitude<.12f)break;
                if(Time.time>timeout)throw new InvalidOperationException("Route blocked at "+head.position+" toward "+feet);
                moveWorld=delta.normalized;lookAt=head.position+delta.normalized*5-Vector3.up*.18f;yield return null;
            }
            moveWorld=Vector3.zero;yield return .25f;
        }
        private IEnumerator Look(Vector3 point,float duration)
        {moveWorld=Vector3.zero;lookAt=point;yield return duration;}
        private Pose CarryPose()=>new Pose(camera.transform.TransformPoint(new Vector3(.27f,-.38f,.62f)),Quaternion.identity);
        private IEnumerator MoveCoreToCarry()
        {var p=CarryPose();yield return MoveCore(p.position,p.rotation,1.2f);}
        private IEnumerator MoveCore(Vector3 point,Quaternion rotation,float duration)
        {
            var from=new Pose(core.position,core.rotation);float start=Time.time;
            while(Time.time-start<duration)
            {
                float t=Mathf.Clamp01((Time.time-start)/duration);t=t*t*t*(t*(6*t-15)+10);
                pointer=new Pose(Vector3.Lerp(from.position,point,t),Quaternion.Slerp(from.rotation,rotation,t));
                grab.ProcessPointerEvent(new PointerEvent(7701,PointerEventType.Move,pointer));yield return null;
            }
            pointer=new Pose(point,rotation);grab.ProcessPointerEvent(new PointerEvent(7701,PointerEventType.Move,pointer));
        }
        private IEnumerator Capture()
        {
            while(!done)
            {
                yield return new WaitForEndOfFrame();if(done)yield break;
                try
                {
                    camera.transform.position=head.position;
                    var direction=lookAt-camera.transform.position;
                    if(direction.sqrMagnitude>.01f)camera.transform.rotation=Quaternion.RotateTowards(camera.transform.rotation,Quaternion.LookRotation(direction),60f*Time.deltaTime);
                    if(holding&&autoCarry){pointer=CarryPose();grab.ProcessPointerEvent(new PointerEvent(7701,PointerEventType.Move,pointer));}
                    if(!recording)
                    {
                        int warmup=AudioRenderer.GetSampleCountForCaptureFrame();
                        using(var discarded=new NativeArray<float>(warmup*2,Allocator.Temp))AudioRenderer.Render(discarded);
                        continue;
                    }
                    var previous=RenderTexture.active;camera.Render();RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();RenderTexture.active=previous;
                    if(!encoder.AddFrame(pixels))throw new InvalidOperationException("Encoder rejected video frame");
                    int n=AudioRenderer.GetSampleCountForCaptureFrame();
                    using(var samples=new NativeArray<float>(n*2,Allocator.Temp))
                    {
                        if(!AudioRenderer.Render(samples)||!encoder.AddSamples(samples))throw new InvalidOperationException("Audio capture rejected");
                        for(int i=0;i<samples.Length;i++){float x=samples[i];peak=Mathf.Max(peak,Mathf.Abs(x));squareSum+=x*x;}
                        sampleCount+=samples.Length;sampleFrames+=n;
                    }
                    if(frames%6==0)positions.Add(new StationRouteVideo.Sample{time=frames/30f,x=head.position.x,z=head.position.z,carrying=holding,powered=mission.IsInserted});
                    frames++;
                    if(frames%30==0)File.WriteAllText(Path.Combine(StationRouteVideo.Output,"progress.txt"),frames/30f+" seconds / "+chapters[chapters.Count-1].title);
                }
                catch(Exception e){Finish(e.ToString());}
            }
        }
        private void Finish(string error)
        {
            if(done)return;done=true;
            if(string.IsNullOrEmpty(error)&&sampleRate>0&&Math.Abs(sampleFrames/(double)sampleRate-frames/30d)>.08)
                error="Audio and video durations differ by more than 80 ms";
            if(string.IsNullOrEmpty(error)&&peak<.00001f)error="Game audio capture is silent";
            Cleanup();
            StationRouteVideo.Finish(new StationRouteVideo.VideoReport{error=error,frames=frames,sampleRate=sampleRate,
                audioSampleFrames=sampleFrames,audioPeak=peak,audioRms=sampleCount>0?Math.Sqrt(squareSum/sampleCount):0,
                missionComplete=mission!=null&&mission.IsEscaped,chapters=chapters.ToArray(),positions=positions.ToArray()});
        }
        private void Cleanup()
        {
            if(audioStarted){AudioRenderer.Stop();audioStarted=false;}
            if(encoder!=null){encoder.Dispose();encoder=null;}
            Time.captureFramerate=oldCaptureRate;
        }
        private void OnDestroy(){Cleanup();if(target!=null)Destroy(target);if(pixels!=null)Destroy(pixels);}
    }
}
#endif
