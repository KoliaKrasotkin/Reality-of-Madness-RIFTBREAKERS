using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ROM.Horde.EditorTools
{
    public static partial class FracturedForestBuilder
    {
        const string SunDemo="Assets/Standard Assets/Sun_Temple/Scenes/DemoScene.unity";
        const string SunRoot="Assets/ROM/Levels/SunTempleURP";
        static readonly Vector3 SunCenter=new Vector3(820,8,370);
        static readonly Vector3 SunCourt=new Vector3(867,48.7f,302);
        static Vector3 SunArrival => SunCenter-(SunCenter-SatelliteCenters[1]).normalized*252;

        static bool SunGeometry(MeshFilter filter)
        {
            var r=filter.GetComponent<MeshRenderer>();
            if(!r || !r.enabled || !filter.sharedMesh) return false;
            string name=(filter.name+" "+filter.sharedMesh.name).ToLowerInvariant();
            return !name.Contains("bgr_") && !name.Contains("background") && !name.Contains("sky") && !name.Contains("cloud");
        }

        public static void ProbeSunTempleFromCommandLine()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(SunDemo);
                var filters=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>()).Where(SunGeometry).ToArray();
                var bounds=filters[0].GetComponent<Renderer>().bounds;
                foreach(var f in filters) bounds.Encapsulate(f.GetComponent<Renderer>().bounds);
                var lines=new List<string>{"CORE "+filters.Length+" center="+bounds.center+" size="+bounds.size};
                foreach(var g in scene.GetRootGameObjects())
                {
                    var rr=g.GetComponentsInChildren<MeshRenderer>();
                    if(rr.Length<3) continue;
                    var b=rr[0].bounds; foreach(var r in rr) b.Encapsulate(r.bounds);
                    lines.Add(g.name+" | "+rr.Length+" | "+b);
                }
                foreach(var f in filters.OrderByDescending(f=>f.GetComponent<Renderer>().bounds.size.sqrMagnitude).Take(20))
                    lines.Add("BIG "+f.name+" | "+f.GetComponent<Renderer>().bounds);
                File.WriteAllLines("Documentation/suntemple-source-bounds.txt",lines);
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        [MenuItem("ROM/Horda/Convertir Sun Temple URP y agregar ciudad")]
        public static void SunFromMenu()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) BuildSunZone();
        }

        [MenuItem("ROM/Horda/Encontrar distrito urbano")]
        public static void FocusUrban() { FocusZone("Bosque Fracturado - Distrito urbano",UrbanCenter,200); }

        [MenuItem("ROM/Horda/Encontrar Ciudad del Sol")]
        public static void FocusSun() { FocusZone("Bosque Fracturado - Ciudad del Sol",SunCenter,280); }

        static void FocusZone(string name,Vector3 center,float radius)
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath)
            {
                if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }
            Selection.activeGameObject=GameObject.Find(name);
            if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.Frame(new Bounds(center,Vector3.one*radius*2),false);
        }

        public static void SunFromCommandLine()
        {
            try
            {
                BuildSunZone(); SunPreview();
                EditorSceneManager.OpenScene(SunDemo); SunPreview();
                EditorSceneManager.OpenScene(ScenePath);
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void RepairSunMaterials()
        {
            Directory.CreateDirectory(SunRoot+"/OriginalMaterials"); AssetDatabase.Refresh();
            int count=0;
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Standard Assets/Sun_Temple/Content"}))
            {
                string file=AssetDatabase.GUIDToAssetPath(guid);
                var source=AssetDatabase.LoadAssetAtPath<Material>(file);
                if(!source || source.name.StartsWith("Sky_")) continue;
                string backup=SunRoot+"/OriginalMaterials/"+guid+".mat";
                if(!File.Exists(backup)) AssetDatabase.CopyAsset(file,backup);
                // Use the untouched backup on every run: conversion remains idempotent.
                var original=AssetDatabase.LoadAssetAtPath<Material>(backup);
                string name=source.name;
                bool fx=name.StartsWith("FX_");
                bool glass=name.IndexOf("glass",StringComparison.OrdinalIgnoreCase)>=0;
                var converted=new Material(Shader.Find(fx?"Universal Render Pipeline/Particles/Unlit":"Universal Render Pipeline/Lit"));
                CopySavedTexture(original,converted,"_BaseMap",new[]{"_BaseMap","_BaseColorMap","_MainTex","_layer1Tex","_Diffuse","_Albedo"});
                CopySavedTexture(original,converted,"_BumpMap",new[]{"_BumpMap","_NormalMap","_layer1Norm"});
                if(converted.GetTexture("_BumpMap")) converted.EnableKeyword("_NORMALMAP");
                converted.SetColor("_BaseColor",glass?new Color(.8f,.92f,1,.3f):Color.white);
                converted.SetFloat("_Metallic",0); converted.SetFloat("_Smoothness",glass?.65f:.2f);
                converted.SetFloat("_Cull",0);
                float oldMode=SavedSunFloat(original,"_Mode",0);
                bool cutout=oldMode==1 || name.IndexOf("leaves",StringComparison.OrdinalIgnoreCase)>=0 || name.IndexOf("foliage",StringComparison.OrdinalIgnoreCase)>=0;
                if(cutout) { converted.SetFloat("_AlphaClip",1); converted.SetFloat("_Cutoff",.4f); converted.EnableKeyword("_ALPHATEST_ON"); converted.renderQueue=2450; }
                if(glass || fx || oldMode>=2)
                {
                    converted.SetFloat("_Surface",1); converted.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
                    converted.SetFloat("_DstBlend",(float)(fx?BlendMode.One:BlendMode.OneMinusSrcAlpha));
                    converted.SetFloat("_ZWrite",0); converted.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); converted.renderQueue=3000;
                }
                CopySavedTexture(original,converted,"_EmissionMap",new[]{"_EmissionMap"});
                if(converted.HasProperty("_EmissionMap") && converted.GetTexture("_EmissionMap"))
                { converted.EnableKeyword("_EMISSION"); converted.SetColor("_EmissionColor",Color.white); }
                converted.name=name;
                EditorUtility.CopySerialized(converted,source); source.name=name;
                EditorUtility.SetDirty(source); UnityEngine.Object.DestroyImmediate(converted); count++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[SunTemple] "+count+" materiales reparados en URP, originales respaldados.");
        }

        static float SavedSunFloat(Material material,string name,float fallback)
        {
            var values=new SerializedObject(material).FindProperty("m_SavedProperties.m_Floats");
            for(int i=0;i<values.arraySize;i++)
            {
                var item=values.GetArrayElementAtIndex(i);
                if(item.FindPropertyRelative("first").stringValue==name) return item.FindPropertyRelative("second").floatValue;
            }
            return fallback;
        }

        static bool CopySunRoot(GameObject go)
        {
            string name=go.name.ToLowerInvariant();
            if(name.Contains("camera") || name.Contains("exposure") || name.Contains("collisionbox") || name.Contains("fpscontroller") || name.Contains("postprocess") || name.Contains("volume")) return false;
            return go.GetComponentsInChildren<MeshFilter>().Any(SunGeometry);
        }

        static void BuildSunZone()
        {
            RepairSunMaterials();
            const string demoBackup=SunRoot+"/DemoScene_BeforeURP.unity";
            if(!File.Exists(demoBackup)) AssetDatabase.CopyAsset(SunDemo,demoBackup);
            var source=EditorSceneManager.OpenScene(SunDemo);
            var sourceTerrain=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).FirstOrDefault();
            if(sourceTerrain)
            {
                if(!sourceTerrain.terrainData) RestoreSunTerrain(sourceTerrain);
                string terrainMatPath=SunRoot+"/TerrainURP.mat";
                var terrainMaterial=AssetDatabase.LoadAssetAtPath<Material>(terrainMatPath);
                if(!terrainMaterial) { terrainMaterial=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")); AssetDatabase.CreateAsset(terrainMaterial,terrainMatPath); }
                sourceTerrain.materialTemplate=terrainMaterial;
            }
            // The old image effects are built-in-renderer effects and cannot run in URP.
            foreach(var script in source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)))
                if(script)
                {
                    if(script.GetType().Name=="DisableRenderer" && script.TryGetComponent<MeshRenderer>(out var helper)) helper.enabled=false;
                    if(script.GetType().Name.Contains("PostProcessing") || script.GetType().Name.Contains("Exposure")) script.enabled=false;
                }
            foreach(var light in source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>()))
                if(light.type==LightType.Directional) { light.intensity=1.2f; light.color=new Color(1,.94f,.83f); }
            EditorSceneManager.SaveScene(source);
            // Stash the authored root hierarchies; preserve their LODGroups and transforms.
            var copyRoots=source.GetRootGameObjects().Where(CopySunRoot).ToArray();
            var destination=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(destination);
            const string backup="Assets/ROM/Levels/map_rom_horde_before_suntemple.unity";
            if(!File.Exists(backup)) AssetDatabase.CopyAsset(ScenePath,backup);
            const string group="Bosque Fracturado - Ciudad del Sol";
            foreach(var old in destination.GetRootGameObjects().Where(g=>g.name==group)) UnityEngine.Object.DestroyImmediate(old);
            root=new GameObject(group).transform;
            soil=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/ForestSoil.mat");
            rock=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/FracturedCliff.mat");
            stone=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/WeatheredRuins.mat");
            var urban=GameObject.Find("Distrito urbano - ciudad destruida");
            if(urban)
            {
                // Older generated extras shared a mesh name. Give each region its own asset.
                var urbanMesh=SatelliteSurface(UrbanCenter,180,"Distrito urbano");
                urban.GetComponent<MeshFilter>().sharedMesh=urbanMesh; urban.GetComponent<MeshCollider>().sharedMesh=urbanMesh;
            }
            Satellite(SunCenter,280,"Isla de la Ciudad del Sol");
            if(sourceTerrain) CopySunGround(sourceTerrain);
            var authored=new GameObject("Sun Temple - composicion original").transform; authored.SetParent(root,false);
            Vector3 offset=SunCenter-new Vector3(93,0,-102);
            int objects=0;
            foreach(var original in copyRoots)
            {
                var clone=UnityEngine.Object.Instantiate(original,authored);
                clone.name=original.name;
                clone.transform.position=original.transform.position+offset;
                foreach(var script in clone.GetComponentsInChildren<MonoBehaviour>(true)) if(script)
                {
                    if(script.GetType().Name=="Door")
                    {
                        var door=new SerializedObject(script);
                        float angle=door.FindProperty("OpenRotationAmount").floatValue;
                        script.transform.Rotate(0,angle,0,Space.Self);
                    }
                    UnityEngine.Object.DestroyImmediate(script);
                }
                foreach(var camera in clone.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera);
                foreach(var light in clone.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(light);
                foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true)) { renderer.lightmapIndex=-1; renderer.lightmapScaleOffset=new Vector4(1,1,0,0); }
                foreach(var filter in clone.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(filter.name.IndexOf("CollisionBox",StringComparison.OrdinalIgnoreCase)>=0 || filter.name.IndexOf("ExposureArea",StringComparison.OrdinalIgnoreCase)>=0)
                    { var renderer=filter.GetComponent<Renderer>(); if(renderer) renderer.enabled=false; }
                    // Preserve the supplied colliders; add missing collision only on
                    // structural LOD0 meshes, rather than duplicating every LOD collider.
                    string name=filter.name.ToLowerInvariant();
                    if(filter.sharedMesh && !name.Contains("lod_") && (name.StartsWith("bld_") || name.StartsWith("struct_") || name.StartsWith("rock_")) && !filter.GetComponent<Collider>())
                        filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
                }
                foreach(var t in clone.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer=0; t.gameObject.isStatic=true; }
                objects++;
            }
            EditorSceneManager.CloseScene(source,true);
            var timber=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/BridgeTimber.mat");
            Vector3 direction=(SunCenter-SatelliteCenters[1]).normalized;
            Vector3 start=SatelliteCenters[1]+direction*(RegionRadii[1]-28),end=SunCenter-direction*252;
            Bridge(start,end,timber,"Puente hacia la Ciudad del Sol");
            // A broad arrival plaza makes the new zone readable from the crossing.
            var arrival=Primitive("Plaza de llegada - Ciudad del Sol",PrimitiveType.Cube,end+direction*12-Vector3.up*.15f,new Vector3(24,.3f,30),stone,true);
            arrival.transform.rotation=Quaternion.LookRotation(direction);
            Vector3 approach=end+direction*8;
            Vector3 landing=SunCourt-(SunCourt-approach).normalized*22; landing.y=SunCourt.y;
            Bridge(approach,landing,timber,"Paseo de acceso a los patios");
            Bridge(landing,SunCourt,timber,"Desembarco en el patio");
            ClearSunPassage(authored,approach,landing);
            ClearSunPassage(authored,landing,SunCourt);
            RepairRockCollisions(); ClearBridgeRockIntersections(); RepairVoidZone();
            var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>(); surface.BuildNavMesh();
            string navPath=SunRoot+"/NavMesh-SunTemple.asset";
            var nav=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if(nav) { EditorUtility.CopySerialized(surface.navMeshData,nav); surface.RemoveData(); surface.navMeshData=nav; surface.AddData(); EditorUtility.SetDirty(nav); }
            else AssetDatabase.CreateAsset(surface.navMeshData,navPath);
            EditorUtility.SetDirty(surface);
            VerifyPath(end+direction*8); VerifyPath((start+end)*.5f);
            VerifyPath(SunCourt);
            ValidateSunHeights();
            foreach(var center in SatelliteCenters) VerifyPath(center);
            VerifyPath(UrbanCenter); ValidateNavigation(); ValidateRockAndVoid();
            EditorSceneManager.SaveScene(destination); AssetDatabase.SaveAssets();
            File.WriteAllText("Documentation/zone-locations.txt","Mapa: "+ScenePath+"\nDistrito urbano: "+UrbanCenter+"\nCiudad del Sol: "+SunCenter+"\nSeleccionar el grupo en Hierarchy y pulsar F para enfocar.\n");
            Debug.Log("[SunTemple] Ciudad del Sol guardada; "+objects+" raices originales con LOD, isla de 560 m y puente navegable.");
        }

        static void RestoreSunTerrain(Terrain terrain)
        {
            const string raw="Documentation/sun-terrain-heights.bin";
            if(!File.Exists(raw)) throw new InvalidOperationException("La recuperacion del TerrainData antiguo todavia no produjo un mapa de alturas.");
            string file=SunRoot+"/RecoveredTerrain.asset";
            var data=AssetDatabase.LoadAssetAtPath<TerrainData>(file);
            if(!data)
            {
                using(var reader=new BinaryReader(File.OpenRead(raw)))
                {
                    int n=reader.ReadInt32(); var size=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                    var heights=new float[n,n]; for(int z=0;z<n;z++) for(int x=0;x<n;x++) heights[z,x]=reader.ReadSingle();
                    data=new TerrainData {heightmapResolution=n,size=size}; data.SetHeights(0,0,heights);
                    var layer=AssetDatabase.FindAssets("t:TerrainLayer",new[]{"Assets/_TerrainAutoUpgrade"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("ground_sediment")).Select(AssetDatabase.LoadAssetAtPath<TerrainLayer>).FirstOrDefault();
                    if(layer) data.terrainLayers=new[]{layer};
                    AssetDatabase.CreateAsset(data,file);
                }
            }
            terrain.terrainData=data;
            if(terrain.TryGetComponent<TerrainCollider>(out var collider)) collider.terrainData=data;
            EditorUtility.SetDirty(terrain);
            Debug.Log("[SunTemple] TerrainData reconstruido en formato Unity 6; relieve recuperado de forma aproximada.");
        }

        static void ClearSunPassage(Transform authored,Vector3 start,Vector3 end)
        {
            Physics.SyncTransforms();
            var removed=new HashSet<GameObject>(); var delta=end-start;
            var side=Vector3.Cross(Vector3.up,delta.normalized);
            foreach(float across in new[]{-2.5f,0,2.5f})
                foreach(float height in new[]{.4f,1.2f,2f})
                foreach(var hit in Physics.RaycastAll(start+Vector3.up*height+side*across,delta.normalized,delta.magnitude,1,QueryTriggerInteraction.Ignore))
                {
                    var node=hit.collider.transform;
                    if(!node.IsChildOf(authored)) continue;
                    while(node.parent && node.parent!=authored) node=node.parent;
                    if(node.parent==authored) removed.Add(node.gameObject);
                }
            foreach(var go in removed) UnityEngine.Object.DestroyImmediate(go);
            Debug.Log("[SunTemple] Acceso despejado: "+removed.Count+" piezas recolocadas fuera del recorrido.");
        }

        static void CopySunGround(Terrain original)
        {
            const int rings=128,segments=256;
            var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>();
            var data=original.terrainData; Vector3 pos=original.transform.position;
            for(int r=0;r<=rings;r++) for(int s=0;s<segments;s++)
            {
                float a=s*Mathf.PI*2/segments,rad=270*r/(float)rings;
                float x=Mathf.Sin(a)*rad,z=Mathf.Cos(a)*rad;
                float sourceX=93+x,sourceZ=-102+z;
                float u=(sourceX-pos.x)/data.size.x,v=(sourceZ-pos.z)/data.size.z;
                float y=u>=0 && u<=1 && v>=0 && v<=1?data.GetInterpolatedHeight(u,v)+pos.y:0;
                // Blend to the island edge without bringing the demo's surrounding world.
                y*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(235,270,rad));
                Vector3 start=SunArrival-SunCenter,finish=SunCourt-SunCenter,span=finish-start;
                var horizontal=new Vector3(span.x,0,span.z); var p=new Vector3(x,0,z);
                float t=Mathf.Clamp01(Vector3.Dot(p-new Vector3(start.x,0,start.z),horizontal)/horizontal.sqrMagnitude);
                Vector3 along=start+span*t;
                if(Vector2.Distance(new Vector2(x,z),new Vector2(along.x,along.z))<20)
                    y=Mathf.Min(y,along.y-.3f);
                vertices.Add(new Vector3(x,y+.02f,z)); uv.Add(new Vector2(sourceX,sourceZ)*.025f);
            }
            for(int r=0;r<rings;r++) for(int s=0;s<segments;s++)
            {
                int a=r*segments+s,b=r*segments+(s+1)%segments,c=a+segments,d=b+segments;
                triangles.AddRange(new[]{a,c,b,b,c,d});
            }
            var mesh=new Mesh{name="Sun Temple - terreno original recortado",vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=triangles.ToArray()};
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string file=SunRoot+"/SunTerrain.asset"; var old=AssetDatabase.LoadAssetAtPath<Mesh>(file);
            if(old) { EditorUtility.CopySerialized(mesh,old); mesh=old; EditorUtility.SetDirty(mesh); } else AssetDatabase.CreateAsset(mesh,file);
            var go=new GameObject("Relieve original de Sun Temple",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            go.transform.SetParent(root,false); go.transform.position=SunCenter; go.isStatic=true;
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshCollider>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=soil;
            Debug.Log("[SunTemple] Terreno original integrado: "+pos+" / "+data.size);
        }

        static void ValidateSunHeights()
        {
            var report=new List<string>();
            Vector3 offset=SunCenter-new Vector3(93,0,-102);
            foreach(var local in new[]{new Vector3(100,0,-26),new Vector3(140,40.5f,-150),new Vector3(140,40.5f,-170),new Vector3(165,40.5f,-185)})
            {
                var path=new NavMeshPath();
                bool sampled=NavMesh.SamplePosition(local+offset,out var target,8,NavMesh.AllAreas);
                bool connected=sampled && NavMesh.SamplePosition(new Vector3(0,0,-15),out var start,5,NavMesh.AllAreas) && NavMesh.CalculatePath(start.position,target.position,NavMesh.AllAreas,path) && path.status==NavMeshPathStatus.PathComplete;
                report.Add(local+" => "+(sampled?target.position.ToString():"sin superficie")+" connected="+connected);
            }
            File.WriteAllLines("Documentation/suntemple-navigation.txt",report);
            Debug.Log("[SunTemple] Navegacion interior: "+string.Join("; ",report));
        }

        public static void ProbeSunPathsFromCommandLine()
        {
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                var lines=new List<string>();
                Vector3 start=SunArrival+(SunCenter-SatelliteCenters[1]).normalized*8;
                NavMesh.SamplePosition(new Vector3(0,0,-15),out var origin,5,NavMesh.AllAreas);
                for(int i=0;i<=20;i++)
                {
                    var target=Vector3.Lerp(start,SunCourt,i/20f); var path=new NavMeshPath();
                    bool sampled=NavMesh.SamplePosition(target,out var hit,4,NavMesh.AllAreas);
                    if(sampled) NavMesh.CalculatePath(origin.position,hit.position,NavMesh.AllAreas,path);
                    lines.Add(i+" "+target+" nearest="+hit.position+" status="+path.status+" last="+(path.corners.Length>0?path.corners.Last().ToString():"none"));
                }
                File.WriteAllLines("Documentation/sun-path-debug.txt",lines);
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void SunPreview()
        {
            bool demo=SceneManager.GetActiveScene().path==SunDemo;
            var go=new GameObject("Sun Temple QA Camera",typeof(Camera)); var camera=go.GetComponent<Camera>();
            camera.transform.position=demo?new Vector3(-237,230,188):SunCenter+new Vector3(-330,230,290);
            camera.transform.LookAt(demo?new Vector3(93,35,-102):SunCenter+Vector3.up*35);
            camera.fieldOfView=50; camera.nearClipPlane=.3f; camera.farClipPlane=3000;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.11f,.16f,.22f);
            RenderSettings.fogDensity=.0007f;
            var lods=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
            foreach(var lod in lods) lod.ForceLOD(0);
            var target=new RenderTexture(1600,1200,24); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            var image=new Texture2D(1600,1200,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1600,1200),0,0); image.Apply();
            File.WriteAllBytes(demo?"Documentation/suntemple-demo-urp.png":"Documentation/suntemple-zone-overview.png",image.EncodeToPNG());
            foreach(var lod in lods) lod.ForceLOD(-1);
            RenderTexture.active=null; camera.targetTexture=null; target.Release();
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
