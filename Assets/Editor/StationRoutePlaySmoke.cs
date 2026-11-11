#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CourseProject;
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using Object=UnityEngine.Object;

namespace CourseSetup
{
    [InitializeOnLoad]
    public static class StationRoutePlaySmoke
    {
        private const string Key="CourseSetup.RouteSmoke.";
        private static bool launched;
        static StationRoutePlaySmoke()
        {EditorApplication.update+=Update;EditorApplication.playModeStateChanged+=ModeChanged;Application.logMessageReceived+=RecordError;}
        public static void RunCli()
        {
            if(Application.isBatchMode)throw new InvalidOperationException("Use a windowed Editor.");
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out XRGeneralSettingsPerBuildTarget store);
            SessionState.SetBool(Key+"Desktop",store.SettingsForBuildTarget(BuildTargetGroup.Standalone).InitManagerOnStart);
            SessionState.SetBool(Key+"Background",PlayerSettings.runInBackground);
            var feature=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature<ApiLayersFeature>();
            SessionState.SetBool(Key+"ApiLayers",feature.enabled);
            SessionState.SetInt(Key+"Errors",0);SessionState.SetInt(Key+"Known",0);
            SessionState.SetString(Key+"Started",DateTime.UtcNow.Ticks.ToString());
            SessionState.SetBool(Key+"Pending",true);
            CourseLauncher.OpenRecoveryRoute();feature.enabled=false;EditorUtility.SetDirty(feature);
            PlayerSettings.runInBackground=true;AssetDatabase.SaveAssets();EditorApplication.isPlaying=true;
        }
        private static void RecordError(string message,string stack,LogType type)
        {
            if(!SessionState.GetBool(Key+"Pending",false)||(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert))return;
            string kind=message.Contains("Illegal byte sequence")&&stack.Contains("MRUK.OnSharedLibLog")?"Known":"Errors";
            SessionState.SetInt(Key+kind,SessionState.GetInt(Key+kind,0)+1);
        }
        private static void Update()
        {
            if(!SessionState.GetBool(Key+"Pending",false))return;
            if((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString(Key+"Started","0")),DateTimeKind.Utc)).TotalSeconds>150)
            {Finish(new List<string>(),"Timed out");return;}
            if(EditorApplication.isPlaying&&Time.time>12&&!launched)
            {launched=true;new GameObject("RouteSmokeRunner").AddComponent<RouteSmokeRunner>();}
        }
        public static void Finish(List<string> checks,string error)
        {
            int errors=SessionState.GetInt(Key+"Errors",0);
            var result=new Report{generatedUtc=DateTime.UtcNow.ToString("O"),passed=error==null&&errors==0,
                checks=checks.ToArray(),error=error,unexpectedErrors=errors,knownMrukLogDecodeExceptions=SessionState.GetInt(Key+"Known",0)};
            Directory.CreateDirectory(StationCabinVisualQA.Output);
            File.WriteAllText(Path.Combine(StationCabinVisualQA.Output,"StationRoute_PlaySmoke.json"),JsonUtility.ToJson(result,true));
            SessionState.SetBool(Key+"Passed",result.passed);Debug.Log("STATION_ROUTE_PLAY_"+(result.passed?"PASS":"FAIL"));
            EditorApplication.isPlaying=false;
        }
        private static void ModeChanged(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key+"Pending",false))return;
            CourseLauncher.SetDesktopStartup(SessionState.GetBool(Key+"Desktop",false));
            PlayerSettings.runInBackground=SessionState.GetBool(Key+"Background",false);
            var feature=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature<ApiLayersFeature>();
            feature.enabled=SessionState.GetBool(Key+"ApiLayers",false);EditorUtility.SetDirty(feature);AssetDatabase.SaveAssets();
            SessionState.SetBool(Key+"Pending",false);EditorApplication.Exit(SessionState.GetBool(Key+"Passed",false)?0:1);
        }
        [Serializable]private class Report
        {
            public string generatedUtc;
            public string scope="Windowed Unity Play with Meta XR Simulator. Synthetic Grabbable pointer events and absolute locomotion; real lifecycle, physics and hatch coroutine. Not physical controller or Quest playtesting.";
            public bool passed;public string[] checks;public string error;public int unexpectedErrors,knownMrukLogDecodeExceptions;
        }
    }
    internal sealed class RouteSmokeRunner:MonoBehaviour
    {
        private readonly List<string> checks=new List<string>();
        private SpaceStationRoute route;private SpaceStationMission mission;private SpaceStationHardware hardware;
        private SpaceStationControls controls;private Grabbable grab;private Rigidbody core;private Transform socket,origin;
        private Pose pointer;
        private IEnumerator Start()
        {
            var steps=RunChecks();
            while(true)
            {
                bool next;object current=null;string error=null;
                try{next=steps.MoveNext();if(next)current=steps.Current;}
                catch(Exception e){next=false;error=e.ToString();}
                if(!next){StationRoutePlaySmoke.Finish(checks,error);yield break;}
                yield return current;
            }
        }
        private IEnumerator RunChecks()
        {
            route=Object.FindFirstObjectByType<SpaceStationRoute>();mission=Object.FindFirstObjectByType<SpaceStationMission>();
            hardware=Object.FindFirstObjectByType<SpaceStationHardware>();controls=Object.FindFirstObjectByType<SpaceStationControls>();
            core=route.Core;grab=core.GetComponent<Grabbable>();socket=GameObject.Find("EnergySocket_SnapAnchor").transform;
            origin=Object.FindFirstObjectByType<OVRCameraRig>().transform;
            Check(XRSettings.isDeviceActive,"Simulator XR device active");
            Check(controls.Locomotor.IsGrounded&&!controls.Locomotor.IgnoringVelocity,"Locomotor initializes grounded on new entrance deck");
            Check(Vector3.Distance(core.position,new Vector3(-5.1f,1.595f,6.9f))<.12f,"Core remains on elevated supply bench after twelve seconds of physics");
            Check(!mission.IsInserted&&hardware.PassageBlocked,"Initial mission has closed hatch");
            var cabin=Object.FindFirstObjectByType<StationCabinPower>();
            Check(cabin!=null&&cabin.Progress==0f,"Cabin begins on emergency power");
            StationCabinVisualQA.CaptureMissionState("Emergency");
            controls.enabled=false;
            MovePlayer(new Vector3(-5.1f,.6f,6.05f));yield return new WaitForSeconds(.3f);
            pointer=new Pose(core.position,core.rotation);
            grab.ProcessPointerEvent(new PointerEvent(9901,PointerEventType.Hover,pointer));
            grab.ProcessPointerEvent(new PointerEvent(9901,PointerEventType.Select,pointer));
            yield return new WaitForSeconds(.2f);
            Check(mission.HasAcquiredCore&&grab.GrabPoints.Count==1,"Synthetic pointer select acquires core through production Grabbable");
            Check(cabin.Progress==0f,"Picking up the core does not energize the cabin");
            Check(GameObject.Find("RouteProgress").GetComponent<TMPro.TMP_Text>().text.Contains("FOLLOW AMBER"),"Acquisition switches route sign to amber return route");
            mission.ResetMission();yield return new WaitForSeconds(.15f);
            Check(mission.CurrentStatus.Contains("RELEASE CORE")&&origin.position.x>-6,"Reset waits while core is held");
            grab.ProcessPointerEvent(new PointerEvent(9901,PointerEventType.Unselect,pointer));
            grab.ProcessPointerEvent(new PointerEvent(9901,PointerEventType.Unhover,pointer));yield return new WaitForSeconds(.4f);
            Check(Vector3.Distance(origin.position,route.StartAnchor.position)<.15f&&!mission.HasAcquiredCore,"Release completes deferred reset and returns player to entrance");
            core.position=new Vector3(-5,-2,6);core.linearVelocity=Vector3.zero;yield return new WaitForSeconds(.3f);
            Check(Vector3.Distance(core.position,new Vector3(-5.1f,1.595f,6.9f))<.15f&&mission.CurrentStatus.Contains("CORE RECOVERED"),"Core dropped outside map is restored to upper supply bench");
            yield return SelectMoveRelease();yield return new WaitForSeconds(.2f);
            Check(mission.IsInserted&&core.isKinematic&&hardware.PassageBlocked,"Release into existing coupler secures core before hatch opening; body="+core.position+", transform="+grab.Transform.position+", status="+mission.CurrentStatus);
            Check(cabin.Progress>0f&&cabin.Progress<1f&&cabin.GetBankPower(0)>cabin.GetBankPower(2),"Docking starts restoration at the coupler circuit");
            yield return new WaitForSeconds(3.1f);
            if (mission.RequiresLever)
            {
                Check(!mission.IsHatchOpen && mission.AirlockLever.IsAvailable, "Restored power awaits a manual lever pull");
                yield return StationLeverDemo.Pull(mission.AirlockLever, 9902);
                yield return new WaitForSeconds(3f);
            }
            Check(mission.IsHatchOpen&&hardware.IsOpen&&!hardware.PassageBlocked,"Real hatch coroutine completes and clears passage");
            Check(cabin.Progress==1f&&cabin.GetBankPower(2)==1f,"All three cabin circuits restored");
            StationCabinVisualQA.CaptureMissionState("Restored");
            Check(GameObject.Find("RouteProgress").GetComponent<TMPro.TMP_Text>().text.Contains("GREEN AIRLOCK"),"Powered route displays green evacuation stage");
            MovePlayer(new Vector3(4.45f,0,.75f));yield return new WaitForSeconds(.4f);
            Check(mission.IsEscaped,"Entering bounded exit completes mission");
            mission.ResetMission();yield return new WaitForSeconds(.4f);
            Check(Vector3.Distance(origin.position,route.StartAnchor.position)<.15f&&!mission.IsEscaped&&!mission.IsInserted&&hardware.PassageBlocked,"Reset from exit side returns player safely and reseals hatch");
            Check(!core.isKinematic&&grab.enabled&&Vector3.Distance(core.position,new Vector3(-5.1f,1.595f,6.9f))<.15f,"Reset restores reusable core at upper bench");
            Check(cabin.Progress==0f,"Reset from the exit returns the cabin to emergency power");
            yield return SelectMoveRelease();
            if (mission.RequiresLever)
            {
                yield return new WaitForSeconds(2.7f);
                yield return StationLeverDemo.Pull(mission.AirlockLever, 9903);
            }
            yield return new WaitForSeconds(1.5f);
            Check(hardware.OpenFraction>0&&hardware.OpenFraction<1,"Second cycle reaches animated intermediate state");
            mission.ResetMission();yield return new WaitForSeconds(3.2f);
            Check(!mission.IsHatchOpen&&!mission.IsInserted&&hardware.PassageBlocked,"Mid-animation reset cancels pending opening");
            Check(cabin.Progress==0f&&cabin.GetBankPower(0)==0f,"Mid-recovery reset cancels cabin restoration");
        }
        private void MovePlayer(Vector3 feet)
        {controls.Locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(),feet,LocomotionEvent.TranslationType.Absolute));}
        private IEnumerator SelectMoveRelease()
        {
            pointer=new Pose(core.position,core.rotation);
            // SDK Move updates only identifiers previously registered by Hover.
            grab.ProcessPointerEvent(new PointerEvent(9901,PointerEventType.Hover,pointer));
            grab.ProcessPointerEvent(new PointerEvent(9901,PointerEventType.Select,pointer));
            Debug.Log("ROUTE_POINTER_SELECT body="+core.position+" target="+grab.Transform.position+" points="+grab.GrabPoints.Count);
            yield return null;yield return null;
            var from=pointer;float started=Time.time;
            while(Time.time-started<2f)
            {
                float t=Mathf.Clamp01((Time.time-started)/2f);t=t*t*t*(t*(6*t-15)+10);
                pointer=new Pose(Vector3.Lerp(from.position,socket.position,t),Quaternion.Slerp(from.rotation,socket.rotation,t));
                grab.ProcessPointerEvent(new PointerEvent(9901,PointerEventType.Move,pointer));yield return null;
            }
            pointer=new Pose(socket.position,socket.rotation);
            // Settle after a gradual move rather than injecting a nine metre, single-frame throw.
            float settleUntil=Time.time+.5f;
            while(Time.time<settleUntil)
            {grab.ProcessPointerEvent(new PointerEvent(9901,PointerEventType.Move,pointer));yield return null;}
            Debug.Log("ROUTE_POINTER_MOVED body="+core.position+" target="+grab.Transform.position+" point="+grab.GrabPoints[0].position+" desired="+pointer.position+" kinematic="+core.isKinematic);
            grab.ProcessPointerEvent(new PointerEvent(9901,PointerEventType.Unselect,pointer));
            Debug.Log("ROUTE_POINTER_RELEASE body="+core.position+" target="+grab.Transform.position+" velocity="+core.linearVelocity);
            grab.ProcessPointerEvent(new PointerEvent(9901,PointerEventType.Unhover,pointer));
        }
        private void Check(bool condition,string message)
        {if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
    }
}
#endif
