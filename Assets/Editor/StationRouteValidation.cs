#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CourseProject;
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using Oculus.Interaction.Surfaces;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
using MetaCharacter=Oculus.Interaction.Locomotion.CharacterController;

namespace CourseSetup
{
    public static class StationRouteValidation
    {
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        private static SpaceStationControls controls;
        private static FirstPersonLocomotor locomotor;
        private static MetaCharacter character;
        private static Transform origin,eyes,head;
        private static readonly List<string> checks=new List<string>();
        [MenuItem("Course/Exploration Level/Validate Recovery Route")]
        public static void Run()
        {
            StationRouteBuilder.RequireSavedEditScene();
            checks.Clear();string error=null;
            try
            {
                var scene=EditorSceneManager.OpenScene(StationRouteBuilder.ScenePath);
                Check(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true))
                    .Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"No missing scripts");
                var route=Object.FindFirstObjectByType<SpaceStationRoute>();
                controls=Object.FindFirstObjectByType<SpaceStationControls>();locomotor=controls.Locomotor;
                character=Get<MetaCharacter>(locomotor,"_characterController");origin=Get<Transform>(locomotor,"_playerOrigin");
                eyes=Get<Transform>(locomotor,"_playerEyes");head=Get<Transform>(controls,"head");
                Check(Vector3.Distance(route.Core.position,GameObject.Find("EnergySocket_SnapAnchor").transform.position)>8,
                    "Core and coupler separated by more than eight metres");
                Check(Mathf.Abs(route.Core.position.y-1.595f)<.05f,"Core starts on upper supply bench");
                Check(Vector3.Distance(Object.FindFirstObjectByType<SpaceStationMissionConsole>().transform.position,
                    new Vector3(-11.7f,1.75f,2.8f))<.02f,"Mission console stays in entrance alcove after scene reload");
                var targets=Object.FindObjectsByType<TeleportInteractable>(FindObjectsSortMode.None);
                Check(targets.Count(t=>t.AllowTeleport)==6,"Six enabled teleport hotspots");
                Check(targets.Where(t=>t.AllowTeleport).All(t=>t.transform.position.x<3),"No teleport target behind sealed hatch");
                Check(targets.Count(t=>!t.AllowTeleport)>=15,"Walls, railings and hatch expose blocking teleport surfaces");
                Check(GameObject.Find("SlidingHatch").GetComponent<TeleportInteractable>()?.AllowTeleport==false,"Closed hatch exposes a blocking teleport surface");
                ValidateTeleportCandidates(targets);
                Check(!route.IsInsideExit(new Vector3(4.45f,4.2f,.75f)) && route.IsInsideExit(new Vector3(4.45f,1.65f,.75f)),
                    "Exit uses a bounded volume including head height");
                Invoke(controls,"Awake");locomotor.SetDeltaTimeProvider(()=>1f/60f);locomotor.SetTimeProvider(()=>1f);
                Reset(new Vector3(-10.8f,0,.75f),0);
                Walk(new Vector3(-8.8f,0,1.65f));Walk(new Vector3(-8.8f,.6f,6.55f));
                Check(origin.position.y>.52f && origin.position.y<.72f,"Walk up west ramp onto upper bay");
                Walk(new Vector3(-7.8f,.6f,6.55f));Walk(new Vector3(-5.1f,.6f,6.05f));
                Check(Vector2.Distance(new Vector2(eyes.position.x,eyes.position.z),new Vector2(-5.1f,6.05f))<.2f,
                    "Reach core pickup position without crossing parapets");
                Walk(new Vector3(-3.35f,.6f,6.65f));Walk(new Vector3(1.35f,0,6.65f));
                Check(Mathf.Abs(origin.position.y)<.12f,"Walk down east ramp to main deck");
                Walk(new Vector3(1.35f,0,5f));Walk(new Vector3(-.8f,0,5f));Walk(new Vector3(-.8f,0,-.15f));Walk(new Vector3(1.25f,0,.15f));
                checks.Add("Continuous loop returns to front of original workbench");
                Reset(new Vector3(2,0,.75f),90);for(int i=0;i<120;i++)Tick(Vector2.up);
                Check(eyes.position.x<3.1f,"Sealed hatch blocks walking");
                var hardware=Object.FindFirstObjectByType<SpaceStationHardware>();hardware.ApplySequenceTime(SpaceStationHardware.SequenceDuration);
                for(int i=0;i<120;i++)Tick(Vector2.up);
                Check(eyes.position.x>3.8f,"Opened hatch permits walking to exit");hardware.ResetHardware();
                Reset(new Vector3(-8.8f,.6f,6.4f),180);Walk(new Vector3(-8.8f,0,1.65f));
                Check(Mathf.Abs(origin.position.y)<.12f,"West ramp also supports descent");
                Reset(new Vector3(1.35f,0,6.65f),-90);Walk(new Vector3(-3.4f,.6f,6.65f));
                Check(origin.position.y>.52f,"East ramp also supports ascent");
                Check(!route.IsCoreOutsideReachableArea(),"Initial core is inside recovery bounds");
            }
            catch(Exception e){error=e.ToString();Debug.LogError(error);}
            finally{EditorSceneManager.OpenScene(StationRouteBuilder.ScenePath);}
            Directory.CreateDirectory(StationGravityLeverBuilder.Output);
            File.WriteAllText(Path.Combine(StationGravityLeverBuilder.Output, "StationRoute_Validation.json"),JsonUtility.ToJson(new Report{
                generatedUtc=DateTime.UtcNow.ToString("O"),passed=error==null,checks=checks.ToArray(),error=error},true));
            Debug.Log("STATION_ROUTE_VALIDATION_"+(error==null?"PASS":"FAIL"));
            if(Application.isBatchMode)EditorApplication.Exit(error==null?0:1);
        }
        private static void Walk(Vector3 target)
        {
            for(int i=0;i<600;i++)
            {
                var delta=target-eyes.position;delta.y=0;
                if(delta.magnitude<.13f){for(int j=0;j<8;j++)Tick(Vector2.zero);return;}
                head.rotation=Quaternion.LookRotation(delta,Vector3.up);Tick(Vector2.up);
            }
            throw new InvalidOperationException("Path blocked toward "+target+"; eyes="+eyes.position+"; origin="+origin.position);
        }
        private static void ValidateTeleportCandidates(TeleportInteractable[] active)
        {
            var registry=TeleportInteractable.Registry;var prior=registry.List().ToList();
            var added=new List<TeleportInteractable>();var host=new GameObject("RouteTeleportValidation");
            try
            {
                foreach(var t in active)
                {
                    t.InjectSurface((ISurface)new SerializedObject(t).FindProperty("_surface").objectReferenceValue);
                    if(!prior.Contains(t)){registry.Register(t);added.Add(t);}
                }
                var computer=host.AddComponent<TeleportCandidateComputer>();
                var set=new InteractableRegistry<TeleportInteractor,TeleportInteractable>.InteractableSet(new HashSet<TeleportInteractable>(active),null);
                Physics.SyncTransforms();
                TeleportInteractable Probe(Vector3 from,Vector3 to,float lift)=>computer.ComputeCandidate(new ProbeArc(from,to,lift),in set,
                    (a,b)=>a.TieBreakerScore.CompareTo(b.TieBreakerScore),out var pose);
                TeleportInteractable Hotspot(string pad)=>GameObject.Find(pad).GetComponentsInChildren<TeleportInteractable>().Single(t=>t.AllowTeleport);
                foreach(var t in active.Where(t=>t.AllowTeleport))
                    Check(Probe(t.transform.position+Vector3.up*1.3f,t.transform.position-Vector3.up*.2f,0)==t,"SDK candidate lands on hotspot above ground blocker: "+t.transform.root.name+" / "+t.transform.position);
                Check(Probe(new Vector3(-10.8f,1.3f,.75f),new Vector3(-8.8f,-.1f,1.75f),.5f)==Hotspot("Route_WestRampPad"),"SDK arc: entrance to west ramp");
                Check(Probe(new Vector3(-8.8f,1.3f,1.75f),new Vector3(-7.8f,.5f,6.55f),1.1f)==Hotspot("Route_UpperBayPad"),"SDK arc: west ramp to upper bay");
                Check(Probe(new Vector3(-7.8f,1.9f,6.55f),new Vector3(-5.1f,.5f,6.05f),.5f)==Hotspot("Route_CorePickupPad"),"SDK arc: upper bay to core pickup");
                Check(Probe(new Vector3(1.35f,1.3f,5),new Vector3(1.25f,-.1f,.15f),1.6f)==Hotspot("RepairPad_TeleportHotspot"),"SDK arc: return landing to workbench");
                Check(Probe(new Vector3(2.5f,1.2f,.75f),new Vector3(4.5f,1.2f,.75f),0)==GameObject.Find("SlidingHatch").GetComponent<TeleportInteractable>(),"SDK candidate blocked by closed hatch");
                Check(Probe(new Vector3(-8.2f,1.2f,3.5f),new Vector3(-2.5f,1.2f,3.5f),0)==GameObject.Find("ReactorIsland").GetComponent<TeleportInteractable>(),"SDK candidate blocked by reactor island");
            }
            finally{foreach(var t in added)registry.Unregister(t);Object.DestroyImmediate(host);}
        }
        private sealed class ProbeArc:IPolyline
        {
            private readonly Vector3[] points=new Vector3[40];
            public int PointsCount=>points.Length;
            public Vector3 PointAtIndex(int i)=>points[i];
            public ProbeArc(Vector3 a,Vector3 b,float lift)
            {for(int i=0;i<points.Length;i++){float u=i/(points.Length-1f);points[i]=Vector3.Lerp(a,b,u)+Vector3.up*(4*lift*u*(1-u));}}
        }
        private static void Reset(Vector3 feet,float yaw)
        {
            controls.CloseMenu();origin.SetPositionAndRotation(feet,Quaternion.Euler(0,yaw,0));
            eyes.SetPositionAndRotation(feet+Vector3.up*1.65f,origin.rotation);if(head!=eyes)head.SetPositionAndRotation(eyes.position,eyes.rotation);
            character.SetPosition(feet+Vector3.up*(character.Height*.5f+character.SkinWidth));character.SetRotation(origin.rotation);
            locomotor.Velocity=Vector3.zero;typeof(FirstPersonLocomotor).GetField("_accumulatedDeltaFrame",Flags).SetValue(locomotor,Pose.identity);
            Physics.SyncTransforms();character.TryGround(1f);Tick(Vector2.zero);
        }
        private static void Tick(Vector2 input)
        {controls.ProcessInput(input,Vector2.zero,true);Invoke(locomotor,"Update");Invoke(locomotor,"LateUpdate");Invoke(locomotor,"LastUpdate");Physics.SyncTransforms();}
        private static T Get<T>(Object obj,string name) where T:Object=>(T)new SerializedObject(obj).FindProperty(name).objectReferenceValue;
        private static void Invoke(object obj,string name)=>obj.GetType().GetMethod(name,Flags).Invoke(obj,null);
        private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add(message);}
        [Serializable]private class Report{public string generatedUtc;public string scope="Editor deterministic input with Meta locomotor and saved scene collision. Not physical headset or human playtesting.";public bool passed;public string[] checks;public string error;}
    }
}
#endif
