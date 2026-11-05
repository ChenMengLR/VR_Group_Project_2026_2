#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourseProject;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CourseSetup
{
    public static class StationCabinRefit
    {
        private const string Textures="Assets/04_Images/ShipCabin";
        private const string Materials="Assets/03_Materials/ShipCabin";
        private const string Installation="FlightDeck_Systems";

        [MenuItem("Course/Exploration Level/Refit Cabin and Bake Lighting")]
        public static void RefitAndBake()
        {
            StationRouteBuilder.RequireSavedEditScene();
            var scene=EditorSceneManager.OpenScene(StationRouteBuilder.ScenePath);
            Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save refitted cabin.");
            Bake(scene);
        }

        public static void ApplyAndCheckCli()
        {
            try
            {
                RefitAndBake();
                StationCabinVisualQA.Capture();
                Debug.Log("CABIN_REFIT_AND_PREVIEW_OK");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        internal static void Apply(Scene scene)
        {
            if(scene.path!=StationRouteBuilder.ScenePath) throw new InvalidOperationException("Select the exploration scene.");
            EnsureFolder(Materials);
            EnsureFolder(CabinMeshAssets.Folder);
            var shell=scene.GetRootGameObjects().Single(x=>x.name=="RecoveryRoute_Environment").transform;
            var station=scene.GetRootGameObjects().Single(x=>x.name=="SoloStationRoot").transform;
            var oldInstallation=shell.Find(Installation);
            if(oldInstallation!=null) Object.DestroyImmediate(oldInstallation.gameObject);
            var equipment=new GameObject(Installation).transform;
            equipment.SetParent(shell,false);

            var hull=SurfaceMaterial("Hull",Color.white,.13f,.39f);
            var deck=SurfaceMaterial("Deck",new Color(.86f,.90f,.93f),.18f,.31f);
            var service=SurfaceMaterial("Service",Color.white,.25f,.38f);
            var ceiling=Finish("OverheadPanels",new Color(.66f,.71f,.75f),.12f,.35f);
            ceiling.CopyPropertiesFromMaterial(hull);
            ceiling.color=new Color(.68f,.73f,.78f);
            foreach(var n in new[]{"MainDeck","ExitDeck","UpperBayDeck","WestAscent","EastDescent"})
                CabinMeshAssets.Surface(shell.Find(n),deck,1.95f);
            foreach(var n in new[]{"SouthHull","NorthHull","WestHull","EastExtensionSeal","ExitSouthWall","ExitNorthWall","AirlockUpperSeal"})
                CabinMeshAssets.Surface(shell.Find(n),hull,2.2f);
            foreach(var n in new[]{"Ceiling","ExitCeiling"}) CabinMeshAssets.Surface(shell.Find(n),ceiling,2.55f);
            foreach(var n in new[]{"HatchPartition_Back","HatchPartition_Front","HatchLintel","ExitEndWall"})
                CabinMeshAssets.Surface(station.Find(n),hull,2.2f);
            CabinMeshAssets.Surface(shell.Find("ReactorIsland"),service,1.82f);
            CabinMeshAssets.Surface(shell.Find("SupplyBenchBase"),service,1.3f);
            foreach(Transform part in shell)
            {
                if(part.name=="OverheadLight" || part.name.StartsWith("Star_",StringComparison.Ordinal))
                    part.gameObject.SetActive(false);
            }
            var window=Finish("OrbitalView",new Color(.81f,.85f,.91f),0,0,true);
            window.mainTexture=Texture("OrbitalView.png",false,true);
            EditorUtility.SetDirty(window);
            shell.Find("ObservationWindow").GetComponent<Renderer>().sharedMaterial=window;
            shell.Find("WindowMark").position=new Vector3(-9.4f,.65f,-2.08f);
            shell.Find("WindowMark_Backing").position=new Vector3(-9.4f,.65f,-2.115f);
            var mark=CabinMeshAssets.LandingMark();
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    if(filter.name!="DiscVisual" || !filter.transform.parent.name.Contains("Pad"))continue;
                    // Keep the original full landing target; the split ring is a visible deck marking.
                    var target=filter.GetComponent<MeshCollider>();
                    var fullDisc=target.sharedMesh;
                    filter.sharedMesh=mark;
                    target.sharedMesh=fullDisc;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                }
            }

            // Replace the original overlapping room fill lights. Mission indicators still follow their hardware.
            foreach(var light in shell.GetComponentsInChildren<Light>(true)
                        .Concat(station.GetComponentsInChildren<Light>(true)))
            {
                if(light.name!="SocketLight" && light.name!="HatchStatusLight") light.enabled=false;
                else { light.shadows=LightShadows.None; light.range=Mathf.Min(light.range,2.8f); }
            }
            RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.20f,.23f,.27f);
            var trim=Finish("AnodisedFrame",new Color(.33f,.41f,.47f),.30f,.43f);
            var gasket=Finish("GraphiteGasket",new Color(.12f,.17f,.20f),.14f,.35f);
            var pipe=Finish("CoolantAlloy",new Color(.54f,.62f,.64f),.42f,.45f);
            var ochre=Finish("IsolationBands",new Color(.76f,.37f,.105f),.13f,.32f);
            var standby=Finish("EmergencyMarkers",new Color(.95f,.37f,.07f),0,0,true);
            var banks=new[]{Lens("Coupler"),Lens("Spine"),Lens("Reserve")};
            var fittings=new CabinMeshAssets.Fittings();
            BuildPressureFrames(fittings,trim,gasket);
            BuildServiceRuns(fittings,pipe,gasket,ochre);
            BuildLightingFixtures(fittings,gasket,trim,standby,banks);
            var renderers=fittings.Build(equipment);
            var fillPositions=new[]{new Vector3(.25f,3.30f,1.45f),new Vector3(-8.2f,3.2f,1.8f),new Vector3(-4.55f,3.25f,6.55f)};
            var circuits=new StationCabinPower.Bank[3];
            for(int i=0;i<circuits.Length;i++)
            {
                var fill=Lamp(equipment,"PowerCircuit_"+i,fillPositions[i],
                    new Color(.80f,.91f,1f),i==1?8.5f:7.5f,9f,LightmapBakeType.Realtime);
                circuits[i]=new StationCabinPower.Bank(fill,new[]{renderers[banks[i]]});
            }
            equipment.gameObject.AddComponent<StationCabinPower>().Configure(station.GetComponent<SpaceStationMission>(),
                circuits[0],circuits[1],circuits[2]);
            // Fixed emergency sconces baked into the structure, sampled by the handheld core via probes.
            foreach(float x in new[]{-10.5f,-5.3f,.15f})
                Lamp(equipment,"AuxiliaryWash",new Vector3(x,2.60f,-1.66f),new Color(1,.58f,.32f),2.6f,6f,LightmapBakeType.Baked);
            foreach(float x in new[]{-8.8f,-3.2f,1.35f})
                Lamp(equipment,"ReserveWash",new Vector3(x,2.74f,7.10f),new Color(.75f,.79f,.86f),2.0f,5.8f,LightmapBakeType.Baked);
            Lamp(equipment,"AirlockWash",new Vector3(4.3f,2.45f,1.9f),new Color(1,.68f,.44f),1.65f,4.3f,LightmapBakeType.Baked);

            var probes=new List<Vector3>();
            foreach(float x in new[]{-11.2f,-8.7f,-5.6f,-2.65f,.65f,2.55f,4.55f})
                foreach(float z in new[]{-1.15f,1.65f,4.9f,6.9f})
                {
                    if((x>3.25f && z>3.7f)||(x> -7f && x< -3.5f && z>2.4f && z<4.9f)) continue;
                    float raised=z>5.5f&&x<=-3f&&x>=-10.1f?.6f:0f;
                    foreach(float y in new[]{.42f,1.45f,2.55f}) probes.Add(new Vector3(x,y+raised,z));
                }
            equipment.gameObject.AddComponent<LightProbeGroup>().probePositions=probes.ToArray();
            EditorUtility.SetDirty(ceiling);
            AssetDatabase.SaveAssets();
        }

        private static void BuildPressureFrames(CabinMeshAssets.Fittings f, Material trim, Material black)
        {
            // Chamfered crown and load-bearing jambs break up the rectangular room silhouette.
            foreach(float x in new[]{-11.88f,-7.95f,-4.05f,-.18f,2.85f})
            {
                foreach(float side in new[]{-1f,1f})
                {
                    float z=2.75f+5.10f*side;
                    var foot=new Vector3(x,.12f,z);
                    var elbow=new Vector3(x,3.48f,z);
                    var crown=new Vector3(x,4.24f,z-.78f*side);
                    f.Box(black,(foot+elbow)/2,new Vector3(.27f,elbow.y-foot.y,.12f));
                    f.Box(trim,(foot+elbow)/2,new Vector3(.14f,elbow.y-foot.y,.24f));
                    f.Beam(black,elbow,crown,.32f,.24f);
                    f.Beam(trim,elbow,crown,.18f,.31f);
                }
                f.Box(trim,new Vector3(x,4.225f,2.75f),new Vector3(.24f,.20f,8.72f));
            }
            foreach(float z in new[]{-2.32f,7.83f})
                f.Box(black,new Vector3(-4.65f,.18f,z),new Vector3(15.35f,.30f,.13f));
        }

        private static void BuildServiceRuns(CabinMeshAssets.Fittings f,Material alloy,Material black,Material band)
        {
            // Double coolant manifold above the far wall. Couplings and hangers show how it is mounted.
            foreach(float y in new[]{3.35f,3.57f})
            {
                f.Pipe(alloy,new Vector3(-12.25f,y,7.62f),new Vector3(2.91f,y,7.62f),.062f);
                foreach(float x in new[]{-11.3f,-9.5f,-7.55f,-5.6f,-3.65f,-1.7f,.25f,2.2f})
                {
                    f.Pipe(black,new Vector3(x-.065f,y,7.62f),new Vector3(x+.065f,y,7.62f),.077f);
                    f.Pipe(band,new Vector3(x-.018f,y,7.62f),new Vector3(x+.018f,y,7.62f),.08f);
                }
            }
            foreach(float x in new[]{-10.5f,-6.55f,-2.6f,1.5f})
                f.Box(black,new Vector3(x,3.46f,7.74f),new Vector3(.13f,.50f,.27f));
            // Protective shrouds and side seals around the observation window.
            foreach(float x in new[]{-11.64f,-7.16f})
                f.Box(black,new Vector3(x,2.1f,-2.26f),new Vector3(.16f,2.38f,.20f));
            foreach(float y in new[]{.94f,3.26f})
                f.Box(alloy,new Vector3(-9.4f,y,-2.24f),new Vector3(4.58f,.11f,.23f));
        }

        private static void BuildLightingFixtures(CabinMeshAssets.Fittings f, Material housing,Material trim,Material marker,Material[] banks)
        {
            foreach(float x in new[]{-10.0f,-6.1f,-2.15f,1.35f})
            {
                foreach(float z in new[]{.55f,5.85f})
                {
                    int circuit=z>4f?2:(x< -4f?1:0);
                    f.Box(housing,new Vector3(x,4.23f,z),new Vector3(2.55f,.19f,.38f));
                    f.Box(trim,new Vector3(x,4.16f,z),new Vector3(2.45f,.12f,.33f));
                    foreach(float dx in new[]{-.58f,.58f})
                        f.Box(banks[circuit],new Vector3(x+dx,4.087f,z),new Vector3(1.05f,.045f,.225f));
                }
            }
            // Recessed wall luminaires and constant low emergency tabs stay in the player's field of view.
            foreach(float x in new[]{-11.0f,-6.3f,-2.3f,1.4f})
            {
                foreach(float z in new[]{-2.32f,7.82f})
                {
                    float inward=z<0?1f:-1f;
                    float y=z<0 && x< -7f?3.54f:2.08f;
                    int circuit=z>4f?2:(x< -4f?1:0);
                    f.Box(housing,new Vector3(x,y,z),new Vector3(.85f,.24f,.16f));
                    f.Box(banks[circuit],new Vector3(x,y,z+.09f*inward),new Vector3(.71f,.10f,.035f));
                    f.Box(marker,new Vector3(x,.42f,z+.03f*inward),new Vector3(.28f,.04f,.045f));
                }
            }
        }

        private static Light Lamp(Transform parent,string name,Vector3 position,Color color,float intensity,float range,LightmapBakeType mode)
        {
            var light=new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent,false);light.transform.position=position;
            light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;
            light.lightmapBakeType=mode;
            light.shadows=mode==LightmapBakeType.Baked?LightShadows.Soft:LightShadows.None;
            return light;
        }

        private static Material SurfaceMaterial(string name, Color tint,float metal,float gloss)
        {
            var material=Finish(name,tint,metal,gloss);
            material.SetTexture("_BaseMap",Texture(name+"_BaseColor.png"));
            material.SetTexture("_BumpMap",Texture(name+"_Normal.png",true));
            material.SetFloat("_BumpScale",.65f);
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Lens(string name)
        {
            var material=Finish(name+"Lens",new Color(.38f,.16f,.045f),0,.28f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",new Color(.42f,.12f,.025f));
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);return material;
        }

        private static Material Finish(string name,Color tint,float metal,float gloss,bool unlit=false)
        {
            string path=Materials+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material,path);
            }
            material.color=tint;
            if(!unlit){material.SetFloat("_Metallic",metal);material.SetFloat("_Smoothness",gloss);}
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D Texture(string file,bool normal=false,bool clamp=false)
        {
            string path=Textures+"/"+file;
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new FileNotFoundException("Missing cabin texture",path);
            var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            var wrap=clamp?TextureWrapMode.Clamp:TextureWrapMode.Repeat;
            int limit=clamp?2048:1024;
            if(importer.textureType!=type || importer.wrapMode!=wrap || !importer.mipmapEnabled ||
                importer.anisoLevel!=4 || importer.maxTextureSize!=limit || importer.filterMode!=FilterMode.Trilinear ||
                importer.sRGBTexture==normal || importer.npotScale!=TextureImporterNPOTScale.None)
            {
                importer.textureType=type;importer.sRGBTexture=!normal;
                importer.wrapMode=wrap;importer.mipmapEnabled=true;importer.anisoLevel=4;
                importer.maxTextureSize=limit;importer.filterMode=FilterMode.Trilinear;
                importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));
        }

        internal static void Bake(Scene scene)
        {
            const string path=Materials+"/CabinLighting.asset";
            var settings=AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if(settings==null){settings=new LightingSettings();AssetDatabase.CreateAsset(settings,path);}
            settings.realtimeGI=false;settings.bakedGI=true;
            settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;
            settings.lightmapResolution=14;settings.lightmapMaxSize=1024;
            settings.directSampleCount=32;settings.indirectSampleCount=64;settings.environmentSampleCount=32;
            settings.maxBounces=2;settings.ao=true;settings.aoMaxDistance=1.2f;
            settings.directionalityMode=LightmapsMode.NonDirectional;
            Lightmapping.lightingSettings=settings;
            EditorUtility.SetDirty(settings);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if(!Lightmapping.Bake())throw new InvalidOperationException("Unity could not finish the cabin light bake.");
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            var maps=LightmapSettings.lightmaps;
            int count=LightmapSettings.lightProbes!=null?LightmapSettings.lightProbes.count:0;
            if(maps.Length==0 || count<4)throw new InvalidOperationException("Missing baked lightmap or light probe data.");
            Debug.Log("CABIN_BAKE_OK lightmaps="+maps.Length+" probes="+count);
        }
    }
}
#endif
