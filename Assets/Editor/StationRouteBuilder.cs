#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourseProject;
using Oculus.Interaction.Locomotion;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CourseSetup
{
    public static class StationRouteBuilder
    {
        public const string ScenePath = "Assets/01_Scenes/Station_RecoveryRoute.unity";
        private const string Materials = "Assets/03_Materials/StationRoute";
        private static Transform root;
        private static Material hull, floor, trim, blue, amber, green, dim;
        private static readonly List<Renderer> outbound = new List<Renderer>();
        private static readonly List<Renderer> inbound = new List<Renderer>();

        [MenuItem("Course/Exploration Level/Build Recovery Route")]
        public static void Build()
        {
            RequireSavedEditScene();
            var scene = EditorSceneManager.OpenScene(Week06TeamUIBuilder.ScenePath);
            if (!EditorSceneManager.SaveScene(scene, ScenePath, true)) throw new IOException("Could not create recovery route scene.");
            scene = EditorSceneManager.OpenScene(ScenePath);
            var station = scene.GetRootGameObjects().Single(r=>r.name=="SoloStationRoot");
            root = new GameObject("RecoveryRoute_Environment").transform;
            SceneManager.MoveGameObjectToScene(root.gameObject, scene);
            outbound.Clear(); inbound.Clear();
            if (!AssetDatabase.IsValidFolder(Materials)) AssetDatabase.CreateFolder("Assets/03_Materials", "StationRoute");
            hull=Mat("Hull",new Color(.045f,.072f,.105f));
            floor=Mat("Deck",new Color(.15f,.20f,.25f));
            trim=Mat("Titanium",new Color(.29f,.39f,.47f));
            blue=Mat("GuideCyan",new Color(.07f,.78f,1),true);
            amber=Mat("GuideAmber",new Color(1,.49f,.08f),true);
            green=Mat("GuideGreen",new Color(.15f,1,.4f),true);
            dim=Mat("GuideOff",new Color(.055f,.12f,.15f),true);
            foreach(var name in new[]{"Floor_Collider","Ceiling","RearWall","FrontWall","StartEndWall",
                "StartGuideStrip","RepairGuideStrip","StartInstructions","ResetInstructions","SocketInstructions"})
                station.transform.Find(name)?.gameObject.SetActive(false);

            // Keep the original workbench, core coupler, animated hatch and exit intact.
            Box("MainDeck",new Vector3(-4.675f,-.1f,2.75f),new Vector3(15.65f,.2f,10.5f),floor,true);
            Box("ExitDeck",new Vector3(4.525f,-.1f,.75f),new Vector3(2.75f,.2f,6.5f),floor,true);
            Box("Ceiling",new Vector3(-4.675f,4.45f,2.75f),new Vector3(15.65f,.2f,10.5f),hull);
            Box("ExitCeiling",new Vector3(4.525f,3.05f,.75f),new Vector3(2.75f,.12f,6.5f),hull);
            Box("ExitSouthWall",new Vector3(4.525f,1.5f,-2.5f),new Vector3(2.75f,3,.18f),hull,true);
            Box("ExitNorthWall",new Vector3(4.525f,1.5f,4),new Vector3(2.75f,3,.18f),hull,true);
            Box("SouthHull",new Vector3(-4.675f,2.2f,-2.5f),new Vector3(15.65f,4.4f,.18f),hull,true);
            Box("NorthHull",new Vector3(-4.675f,2.2f,8),new Vector3(15.65f,4.4f,.18f),hull,true);
            Box("WestHull",new Vector3(-12.5f,2.2f,2.75f),new Vector3(.18f,4.4f,10.5f),hull,true);
            Box("EastExtensionSeal",new Vector3(3.25f,2.2f,6),new Vector3(.2f,4.4f,4),hull,true);
            Box("AirlockUpperSeal",new Vector3(3.25f,3.7f,.75f),new Vector3(.2f,1.4f,6.5f),hull,true);
            foreach(var name in new[]{"HatchPartition_Back","HatchPartition_Front","HatchLintel","SlidingHatch","ExitEndWall"})
                BlockTeleport(station.transform.Find(name).GetComponent<Collider>());

            // A gentle loop, with a machine island screening the direct approach to the core.
            Box("UpperBayDeck",new Vector3(-6.55f,.3f,6.65f),new Vector3(7.1f,.6f,2.3f),floor,true);
            Ramp("WestAscent",new Vector3(-8.8f,0,2.2f),new Vector3(-8.8f,.6f,5.6f),2.2f);
            Ramp("EastDescent",new Vector3(1f,0,6.65f),new Vector3(-3.05f,.6f,6.65f),2.2f);
            Box("ReactorIsland",new Vector3(-5.25f,1.15f,3.65f),new Vector3(3.5f,2.3f,2.35f),hull,true);
            for(int i=0;i<5;i++)
            {
                Box("ReactorFin_"+i,new Vector3(-6.65f+i*.7f,1.18f,2.44f),new Vector3(.08f,2.15f,.16f),trim,false,false);
                Box("ReactorStatus_"+i,new Vector3(-6.65f+i*.7f,1.57f,2.34f),new Vector3(.025f,.48f,.02f),blue,false,false);
            }
            Label("ReactorLabel","THERMAL ARRAY / 07",new Vector3(-5.25f,2.05f,2.33f),0,.15f,3.3f);
            // Solid low parapets keep dropped objects on the reachable deck.
            Rail("UpperWestRail",new Vector3(-10.05f,.6f,6.65f),new Vector3(.09f,.92f,2.3f));
            Rail("UpperFrontRail",new Vector3(-5.325f,.6f,5.51f),new Vector3(4.65f,.92f,.09f));
            Rail("UpperNorthRail",new Vector3(-6.55f,.6f,7.77f),new Vector3(7.1f,.92f,.09f));
            RampRails("West",new Vector3(-8.8f,0,2.2f),new Vector3(-8.8f,.6f,5.6f),2.2f);
            RampRails("East",new Vector3(1f,0,6.65f),new Vector3(-3.05f,.6f,6.65f),2.2f);
            Box("BaySupportA",new Vector3(-9.6f,.2f,7.3f),new Vector3(.3f,.4f,.3f),trim);
            Box("BaySupportB",new Vector3(-3.6f,.2f,7.3f),new Vector3(.3f,.4f,.3f),trim);

            var core=GameObject.Find("EnergyCore_GrabAndRelease").GetComponent<Rigidbody>();
            core.transform.SetPositionAndRotation(new Vector3(-5.1f,1.595f,6.9f),Quaternion.identity);
            PrefabUtility.RecordPrefabInstancePropertyModifications(core.transform);
            EditorUtility.SetDirty(core.transform);
            Box("CoreSupplyBench",new Vector3(-5.1f,1.39f,6.98f),new Vector3(1.5f,.12f,.75f),hull,true);
            Box("SupplyBenchBase",new Vector3(-5.1f,1f,6.98f),new Vector3(.75f,.8f,.52f),trim,true);
            Box("CoreCradleLight",new Vector3(-5.1f,1.458f,6.9f),new Vector3(.42f,.012f,.42f),blue,false,false);
            Label("SupplySign","E-07 / RESERVE CORE\nHOLD GRIP TO CARRY",new Vector3(-5.8f,2.65f,7.72f),0,.22f,3.8f);
            LightAt("SupplyLight",new Vector3(-5.1f,3.4f,6.5f),new Color(.35f,.7f,1),2.5f,5);
            // Observation window: a dark star field with frame and mullions, no transparent sorting cost.
            Box("ObservationWindow",new Vector3(-9.4f,2.1f,-2.385f),new Vector3(4.4f,2.15f,.035f),Mat("Space",new Color(.002f,.006f,.018f),true),false,false);
            for(int i=0;i<28;i++)
            {
                float x=-11.4f+((i*37)%101)/101f*3.95f;
                float y=1.15f+((i*23)%79)/79f*1.84f;
                Box("Star_"+i,new Vector3(x,y,-2.359f),Vector3.one*(i%4==0?.018f:.009f),blue,false,false);
            }
            Box("WindowUpperFrame",new Vector3(-9.4f,3.2f,-2.3f),new Vector3(4.6f,.12f,.18f),trim,false,false);
            Box("WindowLowerFrame",new Vector3(-9.4f,1f,-2.3f),new Vector3(4.6f,.16f,.18f),trim,false,false);
            for(int i=0;i<3;i++) Box("WindowMullion_"+i,new Vector3(-11.65f+i*2.25f,2.1f,-2.3f),new Vector3(.10f,2.2f,.18f),trim,false,false);
            Label("WindowMark","ORBITAL MAINTENANCE / SECTOR 07",new Vector3(-9.4f,.65f,-2.28f),180,.16f);

            // Repeated frames create foreground / middle / background planes along the entrance sightline.
            foreach(float x in new[]{-11.9f,-8f,-4.1f,-.2f})
            {
                Box("TrussSouth",new Vector3(x,2.15f,-2.29f),new Vector3(.15f,4.3f,.18f),trim,false,false);
                Box("OverheadRib",new Vector3(x,4.24f,2.75f),new Vector3(.16f,.18f,10.1f),trim,false,false);
                Box("OverheadLight",new Vector3(x,4.12f,.65f),new Vector3(.19f,.04f,2.6f),blue,false,false);
                LightAt("HallLight",new Vector3(x,3.55f,.65f),new Color(.47f,.68f,.9f),2.5f,6);
            }
            LightAt("UpperBayLight",new Vector3(-8,3.7f,6.1f),new Color(.45f,.66f,1),2.5f,5);
            LightAt("ReturnLight",new Vector3(.6f,3.4f,5.6f),new Color(1,.62f,.32f),2.5f,6);
            // Route ribbons remain clear of the player's centre line and are grouped for state feedback.
            Guide(outbound,new Vector3(-10.8f,.014f,1.72f),new Vector3(-8.8f,.014f,1.72f),blue);
            Guide(outbound,new Vector3(-9.72f,.015f,2.2f),new Vector3(-9.72f,.615f,5.6f),blue);
            Guide(outbound,new Vector3(-9.7f,.615f,7.45f),new Vector3(-5.1f,.615f,7.45f),blue);
            Guide(inbound,new Vector3(-4.35f,.615f,7.45f),new Vector3(-3.05f,.615f,7.45f),dim);
            Guide(inbound,new Vector3(-3.05f,.615f,7.55f),new Vector3(1f,.015f,7.55f),dim);
            Guide(inbound,new Vector3(1.4f,.015f,5.3f),new Vector3(-.45f,.015f,5.3f),dim);
            Guide(inbound,new Vector3(-.45f,.015f,5.3f),new Vector3(-.45f,.015f,-.45f),dim);
            Guide(inbound,new Vector3(-.45f,.015f,-.45f),new Vector3(1.85f,.015f,-.45f),dim);
            Label("AscentSign","01 / RESERVE BAY\nUP RAMP",new Vector3(-8.8f,3.05f,5.6f),0,.24f,2.8f);
            Label("ReturnSign","02 / POWER COUPLER\nDOWN RAMP  >",new Vector3(-1.4f,2.5f,7.8f),0,.20f,3.6f);
            Label("DockSign","02 / COUPLER\nRESTORE POWER",new Vector3(3.11f,2.65f,2.8f),90,.19f,2.3f);
            var missionStatus=station.transform.Find("MissionStatus").GetComponent<TMP_Text>();
            missionStatus.transform.SetPositionAndRotation(new Vector3(3.10f,1.85f,2.8f),Quaternion.Euler(0,90,0));
            missionStatus.transform.localScale=Vector3.one;
            missionStatus.rectTransform.sizeDelta=new Vector2(2.3f,.6f);missionStatus.fontSize=1.5f;
            missionStatus.text="POWER OFFLINE\nRETRIEVE CORE FROM UPPER BAY";
            Box("CouplerStatusBacking",new Vector3(3.16f,1.85f,2.8f),new Vector3(.05f,.64f,2.4f),hull,false,false);
            var routeSign=Label("RouteProgress","01 / RECOVER\nFOLLOW BLUE TO UPPER BAY",new Vector3(-6.8f,3.4f,1.95f),90,.25f);

            var rig=Object.FindFirstObjectByType<OVRCameraRig>();
            var spawn=new GameObject("Route_StartAnchor").transform;
            spawn.SetParent(root,false); spawn.SetPositionAndRotation(new Vector3(-10.8f,0,.75f),Quaternion.Euler(0,90,0));
            rig.transform.SetPositionAndRotation(spawn.position,spawn.rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rig.transform);
            var task=scene.GetRootGameObjects().Single(r=>r.name=="SoloStationTask");
            var startPad=task.transform.Find("StartPad_TeleportHotspot");
            var repairPad=task.transform.Find("RepairPad_TeleportHotspot");
            SetPad(startPad,new Vector3(-10.8f,0,.75f),90);
            SetPad(repairPad,new Vector3(1.25f,0,.15f),0);
            NewPad(repairPad,"Route_WestRampPad",new Vector3(-8.8f,0,1.75f),0);
            NewPad(repairPad,"Route_UpperBayPad",new Vector3(-7.8f,.6f,6.55f),90);
            NewPad(repairPad,"Route_CorePickupPad",new Vector3(-5.1f,.6f,6.05f),0);
            NewPad(repairPad,"Route_ReturnPad",new Vector3(1.35f,0,5f),180);
            // Move the world-space console into the entrance alcove, beside the route.
            var console=station.GetComponentInChildren<SpaceStationMissionConsole>(true);
            console.transform.SetPositionAndRotation(new Vector3(-11.7f,1.75f,2.8f),Quaternion.Euler(0,-90,0));
            ((RectTransform)console.transform).anchoredPosition3D=new Vector3(-11.7f,1.75f,2.8f);
            console.transform.localScale=Vector3.one*.003f;
            console.transform.Find("HelpText").GetComponent<TMP_Text>().text=
                "1  FOLLOW BLUE TO UPPER BAY; GRAB CORE\n2  CARRY DOWN RAMP TO AMBER COUPLER\n3  RELEASE CORE; WAIT FOR HATCH; REACH EXIT";
            PrefabUtility.RecordPrefabInstancePropertyModifications(console.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(console.transform.Find("HelpText").GetComponent<TMP_Text>());
            var exitVolume=new GameObject("Route_ExitVolume").AddComponent<BoxCollider>();
            exitVolume.transform.SetParent(root,false); exitVolume.transform.position=new Vector3(4.45f,1.6f,.75f);
            exitVolume.size=new Vector3(1.5f,2.8f,1.5f); exitVolume.isTrigger=true;
            var route=station.AddComponent<SpaceStationRoute>();
            route.Configure(station.GetComponent<SpaceStationMission>(),station.GetComponent<SpaceStationControls>(),core,
                spawn,exitVolume,outbound.ToArray(),inbound.ToArray(),blue,amber,green,dim,routeSign);
            StationCabinRefit.Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            StationCabinRefit.Bake(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("STATION_RECOVERY_ROUTE_BUILT: " + ScenePath);
        }

        public static void BuildCli()
        {
            Build();
            Capture();
            StationRouteValidation.Run();
        }

        [MenuItem("Course/Exploration Level/Capture Editor Previews")]
        public static void Capture()
        {
            RequireSavedEditScene();
            EditorSceneManager.OpenScene(ScenePath);
            var camera=StationHardwarePreview.CreateCamera();
            try
            {
                View(camera,"StationRoute_Entrance",new Vector3(-11.15f,1.68f,.35f),new Vector3(-1f,1.4f,2.7f),72);
                View(camera,"StationRoute_UpperBay",new Vector3(-8.9f,2.35f,5.9f),new Vector3(-4.5f,1.65f,6.8f),70);
                View(camera,"StationRoute_Return",new Vector3(-3.4f,2.25f,6.55f),new Vector3(1.9f,1.2f,2f),72);
                var shell=GameObject.Find("RecoveryRoute_Environment").transform;
                foreach(var n in new[]{"Ceiling","WestHull","SouthHull"})shell.Find(n).gameObject.SetActive(false);
                View(camera,"StationRoute_Overview",new Vector3(-13,11,-8),new Vector3(-4,0,3),60);
                foreach(var n in new[]{"Ceiling","WestHull","SouthHull"})shell.Find(n).gameObject.SetActive(true);
            }
            finally {Object.DestroyImmediate(camera.gameObject);EditorSceneManager.OpenScene(ScenePath);}
        }
        internal static void RequireSavedEditScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            for(int i=0;i<EditorSceneManager.sceneCount;i++)
                if(EditorSceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scenes first. No changes were discarded.");
        }
        private static void View(Camera c,string name,Vector3 position,Vector3 target,float fov)
        {StationHardwarePreview.Pose(c,position,target,fov); StationHardwarePreview.Save(c,Path.Combine(StationHardwarePreview.Output,name+".png"));}
        private static void SetPad(Transform pad,Vector3 feet,float yaw)
        {
            pad.SetPositionAndRotation(feet,Quaternion.Euler(0,yaw,0));pad.localScale=Vector3.one;
            PrefabUtility.RecordPrefabInstancePropertyModifications(pad);
            var disc=pad.Find("DiscVisual");
            if(disc!=null){var p=disc.localPosition;p.y=-.02f;disc.localPosition=p;PrefabUtility.RecordPrefabInstancePropertyModifications(disc);}
        }
        private static void NewPad(Transform original,string name,Vector3 feet,float yaw)
        {var copy=Object.Instantiate(original.gameObject,root);copy.name=name;SetPad(copy.transform,feet,yaw);}
        private static void Ramp(string name,Vector3 low,Vector3 high,float width)
        {
            // Closed wedge prevents dropped cores rolling into an inaccessible gap underneath.
            Vector3 delta=high-low;float length=new Vector2(delta.x,delta.z).magnitude;
            float w=width*.5f;
            var mesh=new Mesh {name=name+"_SolidWedge"};
            mesh.vertices=new[]{new Vector3(-w,-.12f,-.04f),new Vector3(w,-.12f,-.04f),
                new Vector3(-w,-.12f,length+.04f),new Vector3(w,-.12f,length+.04f),
                new Vector3(-w,0,-.04f),new Vector3(w,0,-.04f),
                new Vector3(-w,delta.y,length+.04f),new Vector3(w,delta.y,length+.04f)};
            mesh.triangles=new[]{4,6,5,5,6,7,0,1,2,1,3,2,0,4,1,1,4,5,
                2,3,6,3,7,6,0,2,4,2,6,4,1,5,3,3,5,7};
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Materials+"/"+name+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
            else {EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(low,Quaternion.LookRotation(new Vector3(delta.x,0,delta.z)));
            go.GetComponent<MeshFilter>().sharedMesh=saved;go.GetComponent<MeshRenderer>().sharedMaterial=floor;
            go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            go.GetComponent<MeshCollider>().sharedMesh=saved;
            BlockTeleport(go.GetComponent<MeshCollider>());
        }
        private static void RampRails(string name,Vector3 low,Vector3 high,float width)
        {
            var delta=high-low; var right=Vector3.Cross(Vector3.up,delta.normalized);
            foreach(int s in new[]{-1,1})
            {
                Vector3 centre=(low+high)*.5f+right*s*(width*.5f+.035f)+Vector3.up*.46f;
                var rail=Box(name+"RampParapet",centre,new Vector3(.08f,.92f,delta.magnitude),hull,true);
                rail.transform.rotation=Quaternion.LookRotation(delta,Vector3.up);
                var line=Box(name+"RampRailLight",centre+Vector3.up*.47f,new Vector3(.09f,.025f,delta.magnitude),blue,false,false);
                line.transform.rotation=rail.transform.rotation;
            }
        }
        private static void Rail(string name,Vector3 feet,Vector3 size)
        {Box(name,feet+Vector3.up*size.y*.5f,size,hull,true);Box(name+"Trim",feet+Vector3.up*(size.y+.015f),new Vector3(size.x+.01f,.03f,size.z+.01f),trim,false,false);}
        private static void Guide(List<Renderer> collection,Vector3 a,Vector3 b,Material mat)
        {var go=Box("RouteGuide",(a+b)*.5f,new Vector3(.055f,.012f,(b-a).magnitude),mat,false,false);go.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);collection.Add(go.GetComponent<Renderer>());}
        private static GameObject Box(string name,Vector3 position,Vector3 size,Material mat,bool teleportBlock=false,bool collision=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);
            go.transform.SetPositionAndRotation(position,Quaternion.identity);go.transform.localScale=size;
            var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());else if(teleportBlock)BlockTeleport(go.GetComponent<Collider>());
            return go;
        }
        private static void BlockTeleport(Collider collider)
        {
            var surface=collider.gameObject.AddComponent<ColliderSurface>();surface.InjectAllColliderSurface(collider);
            var blocker=collider.gameObject.AddComponent<TeleportInteractable>();blocker.InjectAllTeleportInteractable(surface);blocker.AllowTeleport=false;
        }
        private static TMP_Text Label(string name,string text,Vector3 position,float yaw,float height,float width=4.4f)
        {
            var go=new GameObject(name,typeof(TextMeshPro));go.transform.SetParent(root,false);
            go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            var label=go.GetComponent<TextMeshPro>();label.text=text;label.fontSize=height*10;label.alignment=TextAlignmentOptions.Center;
            float signHeight=text.Contains("\n")?.8f:.4f;
            label.fontStyle=FontStyles.Bold;label.color=new Color(.65f,.91f,1);label.rectTransform.sizeDelta=new Vector2(width,signHeight);
            var plate=Box(name+"_Backing",position+go.transform.forward*.035f,new Vector3(width+.15f,signHeight+.04f,.05f),hull,false,false);
            plate.transform.rotation=go.transform.rotation;
            return label;
        }
        private static void LightAt(string name,Vector3 pos,Color color,float intensity,float range)
        {var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=pos;var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;}
        private static Material Mat(string name,Color color,bool unlit=false)
        {
            string path=Materials+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.color=color;EditorUtility.SetDirty(mat);return mat;
        }
    }
}
#endif
