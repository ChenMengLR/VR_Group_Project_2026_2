#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourseProject;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CourseSetup
{
    /// <summary>Original low-poly hardware meshes; no third-party model dependency.</summary>
    public static class StationHardwareBuilder
    {
        private const string Models = "Assets/05_Models/StationHardware";
        private const string Materials = "Assets/03_Materials/StationHardware";

        public static void Apply(Scene scene)
        {
            Folder(Models);
            Folder(Materials);
            var station = scene.GetRootGameObjects().Single(x => x.name == "SoloStationRoot");
            var task = scene.GetRootGameObjects().Single(x => x.name == "SoloStationTask");
            var core = task.transform.Find("EnergyCore_GrabAndRelease");
            if (station.transform.Find("Hardware_Dock") != null)
                throw new InvalidOperationException("Hardware already exists; rebuild from the Week 05 scene.");

            var graphite = Mat("GraphiteAlloy", new Color(.12f, .17f, .22f), .55f, .48f);
            var titanium = Mat("MachinedTitanium", new Color(.46f, .57f, .65f), .72f, .55f);
            var ceramic = Mat("CeramicArmor", new Color(.60f, .69f, .73f), .18f, .42f);
            var rubber = Mat("Insulator", new Color(.018f, .027f, .038f), .05f, .25f);
            var cyan = Mat("EnergyCyan", new Color(.04f, .78f, 1f), 0, 0, true);
            var white = Mat("EnergyWhite", new Color(.58f, .95f, 1f), 0, 0, true);
            var amber = Mat("StandbyAmber", new Color(1f, .39f, .055f), 0, 0, true);
            var green = Mat("ReadyGreen", new Color(.12f, 1f, .44f), 0, 0, true);

            // Preserve the real Meta Rigidbody/Grabbable and its audio event wiring.
            core.GetComponent<Renderer>().enabled = false;
            core.localScale = Vector3.one;
            core.position = new Vector3(1.15f,.995f,1.30f);
            Object.DestroyImmediate(core.GetComponent<SphereCollider>());
            var collider = core.gameObject.AddComponent<CapsuleCollider>();
            collider.radius = .108f;
            collider.height = .28f;
            collider.direction = 1;
            var visuals = Group("Core_Cartridge_E07", core, Vector3.zero);
            var bodyMesh = Lathe("CoreContainment", new[] {
                V(0,-.108f),V(.038f,-.108f),V(.042f,-.09f),V(.042f,.09f),
                V(.038f,.108f),V(0,.108f) }, 12);
            Part("Containment", bodyMesh, visuals, Vector3.zero, rubber);
            var crystalMesh = Lathe("EnergyCrystal", new[] {
                V(0,-.098f),V(.048f,-.092f),V(.065f,-.062f),V(.065f,.062f),
                V(.048f,.092f),V(0,.098f) }, 6);
            var rotor = Group("InnerEnergyRotor", visuals, Vector3.zero);
            Part("HexagonalEnergyCell", crystalMesh, rotor, Vector3.zero, cyan);
            // Six bright conduits are visible between the surrounding cage ribs.
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + 30f) * Mathf.Deg2Rad;
                var conduit = Box("EnergyWindow_" + i, visuals,
                    new Vector3(Mathf.Cos(a) * .084f, 0, Mathf.Sin(a) * .084f),
                    new Vector3(.025f, .168f, .012f), cyan);
                conduit.localRotation = Quaternion.Euler(0, 90f - i * 60f - 30f, 0);
                var rib = Box("CageRib_" + i, visuals,
                    new Vector3(Mathf.Cos(i * Mathf.PI / 3) * .10f, 0, Mathf.Sin(i * Mathf.PI / 3) * .10f),
                    new Vector3(.021f, .19f, .025f), titanium);
                rib.localRotation = Quaternion.Euler(0, 90f - i * 60f, 0);
            }
            var capMesh = Lathe("CartridgeEndCap", new[] {
                V(0,-.017f),V(.087f,-.017f),V(.121f,-.008f),V(.121f,.009f),
                V(.103f,.021f),V(0,.021f) }, 12);
            Part("BottomArmor", capMesh, visuals, new Vector3(0,-.12f,0), graphite);
            Part("TopArmor", capMesh, visuals, new Vector3(0,.117f,0), titanium);
            var band = Ring("CartridgeLightBand", .099f, .119f, .006f, 48);
            Part("LowerEnergyBand", band, visuals, new Vector3(0,-.103f,0), cyan);
            Part("UpperEnergyBand", band, visuals, new Vector3(0,.103f,0), cyan);
            Part("TopInset", Ring("CapInsulator", .026f,.075f,.004f,24), visuals,
                new Vector3(0,.14f,0), rubber);
            Part("TopPowerGlyph", Ring("PowerGlyph", .038f,.044f,.003f,32,275f), rotor,
                new Vector3(0,.143f,0), white);
            Box("GlyphTick", rotor, new Vector3(0,.144f,.04f), new Vector3(.008f,.004f,.044f), white);

            // Coupler: actual dark recess, machined bevels, segmented lamps and clamps.
            var dock = Group("Hardware_Dock", station.transform, new Vector3(1.85f,.85f,1.30f));
            Box("BasePlinth", dock, new Vector3(0,.021f,0), new Vector3(.53f,.042f,.49f), graphite);
            Box("FrontIdentificationPlate", dock, new Vector3(0,.048f,-.225f),
                new Vector3(.35f,.014f,.035f), titanium);
            Part("RecessFloor", Lathe("DockWell", new[] {
                V(0,0),V(.137f,0),V(.137f,.012f),V(0,.012f) },48), dock, new Vector3(0,.035f,0), rubber);
            Part("SocketIndicator", Ring("CouplerBeveledRing",.137f,.202f,.034f,48), dock,
                new Vector3(0,.06f,0), titanium);
            Part("InnerDarkCollar", Ring("DockDarkCollar",.128f,.143f,.015f,48), dock,
                new Vector3(0,.055f,0), rubber);
            var segments = new List<Renderer>();
            var segmentMesh = Ring("DockLightSegment", .165f,.186f,.005f,5,22f);
            for (int i = 0; i < 12; i++)
            {
                var segment = Part("ChargeSegment_" + i, segmentMesh, dock, new Vector3(0,.080f,0), amber);
                segment.transform.localRotation = Quaternion.Euler(0,i*30f,0);
                segments.Add(segment);
            }
            var clamps = new List<Transform>();
            var directions = new List<Vector3>();
            for (int i = 0; i < 3; i++)
            {
                float a = (i*120f+30f)*Mathf.Deg2Rad;
                var radial = new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                var clamp = Group("RetentionClamp_" + i, dock, radial*.224f + Vector3.up*.07f);
                clamp.localRotation = Quaternion.Euler(0,90f-i*120f-30f,0);
                Box("ClampBase",clamp,Vector3.zero,new Vector3(.065f,.055f,.09f),graphite);
                Box("CeramicJaw",clamp,new Vector3(0,.026f,-.018f),new Vector3(.047f,.025f,.055f),ceramic);
                Box("ClampLight",clamp,new Vector3(0,.041f,0),new Vector3(.032f,.005f,.025f),cyan);
                clamps.Add(clamp);
                directions.Add(-radial);
            }
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                Part("BaseFastener", Lathe("HexFastener",new[]{V(0,0),V(.012f,0),V(.012f,.007f),V(0,.007f)},6),
                    dock,new Vector3(x*.237f,.044f,z*.217f),titanium);
            var oldIndicator = station.transform.Find("SocketIndicator");
            oldIndicator.GetComponent<Renderer>().enabled = false;
            var socket = station.transform.Find("EnergySocket_SnapAnchor");
            socket.position = new Vector3(1.85f,1.035f,1.30f);

            // Add surface construction to the existing bench, preserving its collision.
            station.transform.Find("WorkbenchTop").GetComponent<Renderer>().sharedMaterial = graphite;
            var bench = Group("Hardware_Workbench",station.transform,Vector3.zero);
            Box("FrontMetalEdge",bench,new Vector3(1.5f,.80f,.985f),new Vector3(2.45f,.075f,.04f),titanium);
            Box("EdgeLight",bench,new Vector3(1.5f,.824f,.96f),new Vector3(2.14f,.012f,.012f),cyan);
            for(int i=0;i<6;i++)
                Box("Vent_"+i,bench,new Vector3(.46f+i*.045f,.796f,.958f),new Vector3(.016f,.042f,.009f),rubber);
            Part("CoreRestPad",Ring("CoreRestPad",.13f,.18f,.004f,48),bench,new Vector3(1.15f,.856f,1.30f),titanium);
            Box("PowerBusTrack",bench,new Vector3(2.25f,.858f,1.70f),new Vector3(.75f,.009f,.018f),cyan);
            Box("PowerBusElbow",bench,new Vector3(1.89f,.858f,1.53f),new Vector3(.018f,.009f,.34f),cyan);
            Box("IdentificationPlate",bench,new Vector3(1.85f,.79f,.941f),new Vector3(.73f,.065f,.015f),graphite);
            Text("CouplerMark","E-07  /  CORE COUPLER",bench,new Vector3(1.85f,.79f,.931f),
                Quaternion.identity,3.8f, new Vector2(9f,1.1f));
            var benchLight = new GameObject("HardwareWorkbenchLight").AddComponent<Light>();
            benchLight.transform.SetParent(station.transform,false);
            benchLight.transform.position = new Vector3(1.4f,2.1f,.85f);
            benchLight.type=LightType.Point; benchLight.color=new Color(.73f,.85f,1);
            benchLight.intensity=2.5f; benchLight.range=2.3f; benchLight.shadows=LightShadows.None;

            var hatch = station.transform.Find("SlidingHatch");
            hatch.GetComponent<Renderer>().enabled=false;
            hatch.localScale=Vector3.one;
            var blocker=hatch.GetComponent<BoxCollider>();
            blocker.size=new Vector3(.17f,2.48f,1.75f);
            var frame=Group("Hardware_AirlockFrame",station.transform,new Vector3(3.25f,0,.75f));
            var doorLeds=new List<Renderer>();
            for(int side=-1;side<=1;side+=2)
            {
                Box("MachinedJamb",frame,new Vector3(-.065f,1.29f,side*.98f),new Vector3(.28f,2.58f,.21f),titanium);
                Box("JambInset",frame,new Vector3(-.216f,1.28f,side*.98f),new Vector3(.025f,2.28f,.12f),graphite);
                for(int j=0;j<5;j++)
                    doorLeds.Add(Box("JambLamp",frame,new Vector3(-.232f,.40f+j*.43f,side*.98f),
                        new Vector3(.013f,.24f,.035f),amber).GetComponent<Renderer>());
                Box("FloorRail",frame,new Vector3(-.04f,.025f,side*.46f),new Vector3(.34f,.04f,.92f),titanium);
            }
            Box("HeaderArmor",frame,new Vector3(-.04f,2.65f,0),new Vector3(.30f,.24f,2.18f),graphite);
            Box("HeaderLight",frame,new Vector3(-.20f,2.81f,0),new Vector3(.025f,.025f,1.88f),cyan);
            var hatchLabel=Text("AirlockStatus","AIRLOCK 07  /  SEALED",frame,
                new Vector3(-.205f,2.65f,0),Quaternion.Euler(0,90,0),11,new Vector2(19,1.8f));
            var leafMesh=Panel("AirlockLeaf",.16f,2.42f,.874f,.08f);
            var armorMesh=Panel("AirlockInsetArmor",.022f,1.84f,.65f,.09f);
            Transform left=null,right=null;
            var locks=new List<Transform>();
            for(int side=-1;side<=1;side+=2)
            {
                var leaf=Group(side<0?"DoorLeaf_Left":"DoorLeaf_Right",hatch,new Vector3(0,0,side*.435f));
                if(side<0)left=leaf;else right=leaf;
                Part("MainDoorPanel",leafMesh,leaf,Vector3.zero,graphite);
                Part("InsetCeramicArmor",armorMesh,leaf,new Vector3(-.092f,.02f,0),ceramic);
                Box("Spine",leaf,new Vector3(-.115f,.02f,side*.25f),new Vector3(.025f,1.72f,.055f),titanium);
                doorLeds.Add(Box("SealLamp",leaf,new Vector3(-.113f,0,-side*.397f),
                    new Vector3(.015f,2.10f,.016f),amber).GetComponent<Renderer>());
                for(int i=0;i<3;i++)
                    Box("ArmorGroove",leaf,new Vector3(-.11f,-.48f+i*.32f,0),
                        new Vector3(.01f,.025f,.53f),graphite);
                for(int vertical=-1;vertical<=1;vertical+=2)
                {
                    var latch=Group("RetractingLock",leaf,new Vector3(-.145f,vertical*.90f,-side*.28f));
                    Box("LockHousing",latch,Vector3.zero,new Vector3(.08f,.14f,.28f),titanium);
                    Box("LockLamp",latch,new Vector3(-.045f,0,0),new Vector3(.008f,.025f,.18f),amber);
                    locks.Add(latch);
                }
                for(int i=0;i<4;i++)
                    Box("LowerVent",leaf,new Vector3(-.09f,-1.055f,-.23f+i*.15f),
                        new Vector3(.014f,.055f,.05f),rubber);
            }
            // The lock sequence indexes alternating negative/positive Z motion.
            var alternatingLocks=new[]{locks[0],locks[2],locks[1],locks[3]};
            var audio=hatch.gameObject.AddComponent<AudioSource>();
            audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/07_Sounds/SFX/hatch_mechanism.wav");
            if(audio.clip==null)throw new FileNotFoundException("Generate the hatch_mechanism.wav first.");
            audio.playOnAwake=false;audio.loop=false;audio.volume=.55f;
            audio.spatialBlend=1;audio.spatialize=true;audio.rolloffMode=AudioRolloffMode.Linear;
            audio.minDistance=.7f;audio.maxDistance=9;audio.dopplerLevel=0;
            var hardware=hatch.gameObject.AddComponent<SpaceStationHardware>();
            hardware.Configure(left,right,alternatingLocks,clamps.ToArray(),directions.ToArray(),
                segments.ToArray(),doorLeds.ToArray(),amber,cyan,green,blocker,rotor,hatchLabel,audio);
            hardware.ResetHardware();
            var mission=new SerializedObject(station.GetComponent<SpaceStationMission>());
            mission.FindProperty("hardware").objectReferenceValue=hardware;
            mission.ApplyModifiedPropertiesWithoutUndo();
            if(!hardware.IsConfigured)throw new InvalidOperationException("Incomplete station hardware.");
            AssetDatabase.SaveAssets();
        }

        private static Vector2 V(float radius,float height)=>new Vector2(radius,height);
        private static Transform Group(string name,Transform parent,Vector3 position)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            return go.transform;
        }
        private static Transform Box(string name,Transform parent,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());
            return go.transform;
        }
        private static Renderer Part(string name,Mesh mesh,Transform parent,Vector3 position,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;return r;
        }
        private static TextMeshPro Text(string name,string text,Transform parent,Vector3 position,
            Quaternion rotation,float size,Vector2 rect)
        {
            var t=Group(name,parent,position);t.localRotation=rotation;t.localScale=Vector3.one*.1f;
            var label=t.gameObject.AddComponent<TextMeshPro>();label.text=text;label.fontSize=size;
            label.fontStyle=FontStyles.Bold;label.alignment=TextAlignmentOptions.Center;
            label.color=new Color(.64f,.86f,.94f);label.rectTransform.sizeDelta=rect;return label;
        }
        private static Material Mat(string name,Color color,float metal,float smooth,bool light=false)
        {
            string path=Materials+"/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find(light?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.name=name;m.SetColor("_BaseColor",color);
            if(!light){m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);}
            EditorUtility.SetDirty(m);return m;
        }
        private static Mesh Ring(string name,float inner,float outer,float height,int segments,float degrees=360)
        {
            float bevel=Mathf.Min(height*.3f,(outer-inner)*.15f);
            return Lathe(name,new[]{V(inner,-height/2),V(outer-bevel,-height/2),
                V(outer,-height/2+bevel),V(outer,height/2-bevel),V(outer-bevel,height/2),
                V(inner+bevel,height/2),V(inner,height/2-bevel)},segments,degrees);
        }
        private static Mesh Lathe(string name,Vector2[] profile,int segments,float degrees=360)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<segments;i++)
            for(int p=0;p<profile.Length;p++)
            {
                int n=(p+1)%profile.Length;
                float a=degrees*Mathf.Deg2Rad*i/segments,b=degrees*Mathf.Deg2Rad*(i+1)/segments;
                int v=vertices.Count;
                vertices.Add(new Vector3(Mathf.Cos(a)*profile[p].x,profile[p].y,Mathf.Sin(a)*profile[p].x));
                vertices.Add(new Vector3(Mathf.Cos(b)*profile[p].x,profile[p].y,Mathf.Sin(b)*profile[p].x));
                vertices.Add(new Vector3(Mathf.Cos(b)*profile[n].x,profile[n].y,Mathf.Sin(b)*profile[n].x));
                vertices.Add(new Vector3(Mathf.Cos(a)*profile[n].x,profile[n].y,Mathf.Sin(a)*profile[n].x));
                triangles.AddRange(new[]{v,v+2,v+1,v,v+3,v+2});
            }
            return SaveMesh(name,vertices,triangles);
        }
        private static Mesh Panel(string name,float depth,float height,float width,float bevel)
        {
            float y=height/2,z=width/2;
            var polygon=new[]{new Vector2(-y+bevel,-z),new Vector2(y-bevel,-z),
                new Vector2(y,-z+bevel),new Vector2(y,z-bevel),new Vector2(y-bevel,z),
                new Vector2(-y+bevel,z),new Vector2(-y,z-bevel),new Vector2(-y,-z+bevel)};
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int side=-1;side<=1;side+=2)
            {
                int center=vertices.Count;vertices.Add(new Vector3(side*depth/2,0,0));
                foreach(var p in polygon)vertices.Add(new Vector3(side*depth/2,p.x,p.y));
                for(int i=0;i<polygon.Length;i++)
                {
                    int a=center+1+i,b=center+1+(i+1)%polygon.Length;
                    triangles.AddRange(side<0?new[]{center,b,a}:new[]{center,a,b});
                }
            }
            for(int i=0;i<polygon.Length;i++)
            {
                var a=polygon[i];var b=polygon[(i+1)%polygon.Length];int v=vertices.Count;
                vertices.AddRange(new[]{new Vector3(-depth/2,a.x,a.y),new Vector3(depth/2,a.x,a.y),
                    new Vector3(depth/2,b.x,b.y),new Vector3(-depth/2,b.x,b.y)});
                triangles.AddRange(new[]{v,v+2,v+1,v,v+3,v+2});
            }
            return SaveMesh(name,vertices,triangles);
        }
        private static Mesh SaveMesh(string name,List<Vector3> vertices,List<int> triangles)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Models+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);return existing;
        }
        private static void Folder(string path)
        {
            var parts=path.Split('/');string current=parts[0];
            for(int i=1;i<parts.Length;i++){string next=current+"/"+parts[i];if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(current,parts[i]);current=next;}
        }
    }
}
#endif
