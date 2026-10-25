using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ROM.EditorTools
{
    // All actions are manual. No scene saves, base generation, source edits or runtime scripts.
    public static class CiudadDelSolVoidAtmosphereGenerator
    {
        const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Base="Dungeon_Ciudad_del_Sol_Generated";
        const string Root="Dungeon_Void_Atmosphere";
        const string Signature="__ROM_VoidAtmosphere_251025_v1";
        const string Menu="ROM/Dungeon Ciudad del Sol/Atmosfera del Vacio/";
        const string Generated="Assets/ROM/Generated/VoidAtmosphere";
        const int Seed=251025, LightLimit=10;
        const float Clearance=5;
        static readonly string[] Paths=
        {
            "Assets/Standard Assets/Eternal Temple/Prefabs/Damaged/Column_Broken_01.prefab",
            "Assets/Standard Assets/Eternal Temple/Prefabs/Damaged/Column_Broken_02.prefab",
            "Assets/Standard Assets/Eternal Temple/Prefabs/Damaged/Broken_Wall_01.prefab",
            "Assets/Standard Assets/Eternal Temple/Prefabs/Bases/Thin/Base_Wall_Broken_03.prefab",
            "Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/SM_Stone_01.prefab",
            "Assets/LeartesStudios/The_Grand_Egyptian_Temple/Art/Prefabs/SM_Stone_03.prefab"
        };
        struct Edge { public Vector3 Point,Normal; }
        sealed class Source
        {
            public Transform Dungeon;
            public readonly List<Bounds> Geometry=new List<Bounds>();
            public readonly List<Bounds> Walking=new List<Bounds>();
            public readonly List<Edge> Edges=new List<Edge>();
        }
        sealed class Palette
        {
            public Material Violet,Magenta,Fog;
            public VolumeProfile Profile;
            public readonly List<Object> Temporary=new List<Object>();
            public void Dispose() { foreach(var item in Temporary) if(item) Object.DestroyImmediate(item); Temporary.Clear(); }
        }
        static bool Ready=>!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating;

        [MenuItem(Menu+"8. Retirar etapa rechazada y restaurar iluminacion %#&F2")]
        public static void RemoveRejectedStageAndRestore()
        {
            if(!Ready) return;
            int undo=-1; bool saved=false;
            try
            {
                ClosePreview();
                var scene=SceneManager.GetActiveScene(); var source=ReadSource(scene);
                var atmosphere=FindRoot(scene);
                var geometry=new GeometryGuard(scene,atmosphere?atmosphere.transform:null);
                string[] approved={"01 - Entrada Portico Solar/PorticoSolar_Architecture",
                    "02 - Jardines Dorados/JardinesDorados_Architecture","03 - Plaza del Eclipse/PlazaDelEclipse_Architecture"};
                foreach(string path in approved)
                    if(!source.Dungeon.Find(path)) throw new InvalidOperationException("Falta arquitectura aprobada: "+path);
                var materialChecks=new List<Action>();
                foreach(var renderer in source.Dungeon.GetComponentsInChildren<Renderer>(true))
                {
                    var original=renderer; string json=EditorJsonUtility.ToJson(renderer);
                    materialChecks.Add(()=>{ if(!original || EditorJsonUtility.ToJson(original)!=json) throw new InvalidOperationException("Renderer aprobado alterado."); });
                }
                int MissingScripts()=>source.Dungeon.GetComponentsInChildren<Transform>(true)
                    .Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                int missingBefore=MissingScripts();
                var cameras=scene.GetRootGameObjects().Where(g=>g.name=="Main Camera").Select(g=>g.GetComponent<Camera>()).Where(c=>c).ToArray();
                if(cameras.Length!=1) throw new InvalidOperationException("Se requiere una Main Camera original unica.");
                var camera=cameras[0]; var data=camera.GetComponent<UniversalAdditionalCameraData>();
                if(!data) throw new InvalidOperationException("Falta el componente URP original de Main Camera.");
                // Verified against Dungeon_Isla_del_Sol_Backup_PreIntegracion: HDR 1, PP 0, mask 1.
                Undo.IncrementCurrentGroup(); undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Retirar atmosfera del Vacio rechazada");
                Undo.RecordObject(camera,"Restaurar HDR anterior"); camera.allowHDR=true;
                Undo.RecordObject(data,"Restaurar postprocesado anterior"); data.renderPostProcessing=false; data.volumeLayerMask=1;
                int objects=atmosphere?atmosphere.GetComponentsInChildren<Transform>(true).Length:0;
                int systems=atmosphere?atmosphere.GetComponentsInChildren<ParticleSystem>(true).Length:0;
                int lights=atmosphere?atmosphere.GetComponentsInChildren<Light>(true).Length:0;
                int volumes=atmosphere?atmosphere.GetComponentsInChildren<Volume>(true).Length:0;
                if(atmosphere) Undo.DestroyObjectImmediate(atmosphere);
                Physics.SyncTransforms(); geometry.Verify();
                foreach(var check in materialChecks) check();
                ReadSource(scene);
                foreach(string path in approved)
                    if(!source.Dungeon.Find(path)) throw new InvalidOperationException("Arquitectura aprobada perdida: "+path);
                if(FindRoot(scene) || data.renderPostProcessing || data.volumeLayerMask.value!=1 || !camera.allowHDR)
                    throw new InvalidOperationException("Retirada o restauracion incompleta.");
                if(MissingScripts()!=missingBefore) throw new InvalidOperationException("Se alteraron las referencias a scripts originales.");
                var dependencies=EditorUtility.CollectDependencies(scene.GetRootGameObjects().Cast<Object>().ToArray());
                if(dependencies.Any(o=>o && AssetDatabase.GetAssetPath(o).StartsWith(Generated+"/",StringComparison.Ordinal)))
                    throw new InvalidOperationException("Quedan referencias de la escena a assets del Vacio; no se borraran.");
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,Target,false)) throw new InvalidOperationException("No se pudo guardar la escena limpia.");
                saved=true; Undo.CollapseUndoOperations(undo); undo=-1;
                // Only the known assets created by this stage, never source-package materials or meshes.
                string[] files={"AerialGlow.mat","LuminousMotes.mat","Violet.mat","Magenta.mat","Nebula.mat",
                    "Atmosphere.asset","VioletLightingEnhanced.asset","Nebula.asset","VioletEdgeEmitters.asset","VioletLowerEmitters.asset"};
                int removed=0;
                foreach(string file in files)
                {
                    string path=Generated+"/"+file;
                    if(!AssetDatabase.LoadMainAssetAtPath(path)) continue;
                    if(!AssetDatabase.DeleteAsset(path)) throw new InvalidOperationException("Escena limpia guardada; no se pudo eliminar "+path);
                    removed++;
                }
                if(AssetDatabase.IsValidFolder(Generated) && System.IO.Directory.GetFileSystemEntries(Generated).Length==0)
                    AssetDatabase.DeleteAsset(Generated);
                var view=SceneView.lastActiveSceneView;
                if(view) { view.sceneViewState.showImageEffects=false; view.Repaint(); }
                Debug.Log("VACIO RETIRADO Y ESCENA GUARDADA: "+objects+" objetos ("+systems+" sistemas de particulas, "+lights+" luces, "+volumes+
                    " volumenes). Eliminados "+removed+" assets exclusivos. Main Camera restaurada: HDR 1, postprocesado 0, mascara 1. Siete zonas, nueve rutas, colliders, materiales originales y tres arquitecturas aprobadas intactos. Scripts faltantes previos: "+missingBefore);
            }
            catch(Exception e)
            {
                if(!saved && undo>=0) Undo.RevertAllDownToGroup(undo);
                Debug.LogError("Retirada del Vacio: "+e.Message);
            }
        }
        [MenuItem(Menu+"7. Comprobar emisores sin modificar %#&F3")]
        public static void DiagnoseEmitters()
        {
            var root=FindRoot(SceneManager.GetActiveScene());
            if(!root) { Debug.LogError("No existe atmosfera para validar."); return; }
            var stage=root.transform.Find("VioletLighting_Enhanced");
            if(!stage) { Debug.LogError("No existe la etapa de iluminacion violeta."); return; }
            int total=0;
            foreach(var ps in stage.GetComponentsInChildren<ParticleSystem>(true))
            {
                var shape=ps.shape; var particles=new ParticleSystem.Particle[ps.particleCount];
                ps.GetParticles(particles);
                int distinct=particles.Select(p=>Key(p.position)).Distinct().Count();
                if(!ps.gameObject.activeInHierarchy || particles.Length==0 || distinct<10 ||
                    !shape.mesh || !shape.mesh.isReadable || shape.mesh.GetTopology(0)!=MeshTopology.Triangles)
                { Debug.LogError("Emisor no valido: "+ps.name+" / particulas="+particles.Length+" / posiciones="+distinct); return; }
                total+=particles.Length;
                Debug.Log("VOID_CHECK "+ps.name+": "+particles.Length+" particulas, "+distinct+" posiciones distintas, malla legible con triangulos.");
            }
            Debug.Log((EditorApplication.isPlaying?"VOID_RUNTIME_OK":"VOID_EDITOR_OK")+": tres emisores activos, "+total+" particulas. Comprobacion solo de lectura.");
        }
        [MenuItem(Menu+"6. Mejorar iluminacion violeta y guardar tras validar %#&F4")]
        public static void EnhanceVioletLighting()
        {
            if(!Ready) return;
            int undo=-1;
            var errors=new List<string>();
            Application.LogCallback capture=(message,stack,type)=>
            { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(message); };
            Application.logMessageReceived+=capture;
            try
            {
                ClosePreview();
                var scene=SceneManager.GetActiveScene(); var source=ReadSource(scene);
                var pipeline=UniversalRenderPipeline.asset;
                if(!pipeline || !pipeline.supportsHDR) throw new InvalidOperationException("El pipeline URP activo necesita HDR; no se modificara un asset de pipeline compartido automaticamente.");
                var geometry=new GeometryGuard(scene);
                Undo.IncrementCurrentGroup(); undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Mejora visual violeta Ciudad del Sol");
                var root=FindRoot(scene);
                if(!root)
                {
                    root=new GameObject(Root); SceneManager.MoveGameObjectToScene(root,scene);
                    Undo.RegisterCreatedObjectUndo(root,"Crear raiz de efectos visuales");
                    new GameObject(Signature).transform.SetParent(root.transform,false);
                }
                var stage=root.transform.Find("VioletLighting_Enhanced");
                if(!stage)
                {
                    var go=new GameObject("VioletLighting_Enhanced"); go.transform.SetParent(root.transform,false);
                    Undo.RegisterCreatedObjectUndo(go,"Crear efectos violetas"); stage=go.transform;
                }
                foreach(var system in stage.GetComponentsInChildren<ParticleSystem>())
                {
                    Undo.RecordObject(system,"Reparar emisores de halos");
                    system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                errors.Clear(); // The broken previous emitters are now stopped; validate this application only.
                var palette=CreatePalette(true);
                T Asset<T>(string name,Func<T> factory) where T:Object
                {
                    string path=Generated+"/"+name; var item=AssetDatabase.LoadAssetAtPath<T>(path);
                    if(item) return item;
                    if(AssetDatabase.LoadMainAssetAtPath(path)) throw new InvalidOperationException("Asset incompatible en "+path);
                    item=factory(); item.name="ROM_Void_"+name; AssetDatabase.CreateAsset(item,path); return item;
                }
                var glow=Asset("AerialGlow.mat",()=>
                {
                    var m=new Material(palette.Fog); m.SetColor("_BaseColor",new Color(3.2f,.10f,8,.7f));
                    m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend",(float)BlendMode.One);
                    m.SetFloat("_ZWrite",0); m.SetOverrideTag("RenderType","Transparent"); return m;
                });
                var motes=Asset("LuminousMotes.mat",()=>
                { var m=new Material(glow); m.SetColor("_BaseColor",new Color(5,.35f,12,.9f)); return m; });
                foreach(var material in new[]{glow,motes})
                {
                    Undo.RecordObject(material,"Corregir mezcla aditiva URP");
                    material.SetFloat("_Surface",1); material.SetFloat("_Blend",2);
                    material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend",(float)BlendMode.One);
                    material.SetFloat("_SrcBlendAlpha",(float)BlendMode.One); material.SetFloat("_DstBlendAlpha",(float)BlendMode.One);
                    material.SetFloat("_ZWrite",0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
                }
                var profile=Asset("VioletLightingEnhanced.asset",()=>
                {
                    var p=ScriptableObject.CreateInstance<VolumeProfile>();
                    var bloom=p.Add<Bloom>(); bloom.threshold.Override(1.35f); bloom.intensity.Override(1.65f);
                    bloom.scatter.Override(.82f); bloom.clamp.Override(22); bloom.highQualityFiltering.Override(true);
                    var color=p.Add<ColorAdjustments>(); color.postExposure.Override(.15f);
                    color.colorFilter.Override(new Color(.94f,.91f,1)); color.contrast.Override(6);
                    var tone=p.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
                    var vignette=p.Add<Vignette>(); vignette.intensity.Override(.12f);
                    return p;
                });
                foreach(var component in profile.components)
                    if(!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component,profile);
                EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
                // The emitter point cloud is invisible: no mesh renderer, collision, or changes to dungeon geometry.
                var ordered=source.Edges.OrderBy(e=>Key(e.Point).x).ThenBy(e=>Key(e.Point).z).ThenBy(e=>Key(e.Point).y).ToArray();
                int stride=Mathf.Max(1,Mathf.CeilToInt(ordered.Length/650f));
                var points=ordered.Where((e,i)=>i%stride==0).Select(e=>e.Point+e.Normal*2.5f-Vector3.up*2.8f).ToArray();
                var mesh=Asset("VioletEdgeEmitters.asset",()=>
                {
                    var m=new Mesh(); m.indexFormat=IndexFormat.UInt32; m.vertices=points;
                    m.SetIndices(Enumerable.Range(0,points.Length).ToArray(),MeshTopology.Points,0); m.RecalculateBounds(); return m;
                });
                var lower=Asset("VioletLowerEmitters.asset",()=>
                {
                    var m=new Mesh(); m.indexFormat=IndexFormat.UInt32;
                    m.vertices=points.Where((p,i)=>i%3==0).Select(p=>p-Vector3.up*10).ToArray();
                    m.SetIndices(Enumerable.Range(0,m.vertexCount).ToArray(),MeshTopology.Points,0); m.RecalculateBounds(); return m;
                });
                foreach(var emitter in new[]{mesh,lower})
                {
                    if(!emitter.isReadable || emitter.vertexCount<3) throw new InvalidOperationException("Emisor de particulas no legible o vacio.");
                    // Unity's Shape module requires triangle topology even when emitting from vertices.
                    // These meshes remain invisible; their vertex positions and the dungeon meshes never change.
                    Undo.RecordObject(emitter,"Corregir topologia del emisor invisible");
                    var indices=new int[(emitter.vertexCount-2)*3];
                    for(int i=1;i<emitter.vertexCount-1;i++)
                    { int j=(i-1)*3; indices[j]=0; indices[j+1]=i; indices[j+2]=i+1; }
                    emitter.indexFormat=IndexFormat.UInt16;
                    emitter.SetIndices(indices,MeshTopology.Triangles,0);
                    emitter.uv=Enumerable.Repeat(Vector2.zero,emitter.vertexCount).ToArray();
                    emitter.colors32=Enumerable.Repeat(new Color32(255,255,255,255),emitter.vertexCount).ToArray();
                    emitter.RecalculateNormals(); emitter.RecalculateBounds(); emitter.UploadMeshData(false);
                    EditorUtility.SetDirty(emitter); AssetDatabase.SaveAssetIfDirty(emitter);
                }
                GameObject Child(string name)
                {
                    var found=stage.Find(name); if(found) return found.gameObject;
                    var go=new GameObject(name); go.transform.SetParent(stage,false); Undo.RegisterCreatedObjectUndo(go,"Efecto "+name); return go;
                }
                void Glow(string name,Mesh emitter,float size,Material material,int max)
                {
                    var go=Child(name); var ps=go.GetComponent<ParticleSystem>();
                    if(!ps) ps=Undo.AddComponent<ParticleSystem>(go);
                    Undo.RecordObject(ps,"Configurar energia violeta"); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.useAutoRandomSeed=false; ps.randomSeed=(uint)(Seed+max);
                    var main=ps.main; main.duration=60; main.loop=true; main.prewarm=true; main.playOnAwake=true;
                    main.startLifetime=120; main.startDelay=0; main.simulationSpeed=1;
                    main.startSpeed=0; main.startSize=new ParticleSystem.MinMaxCurve(size*.8f,size*1.2f);
                    main.startColor=Color.white; main.maxParticles=max; main.simulationSpace=ParticleSystemSimulationSpace.Local;
                    main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
                    var shape=ps.shape; shape.enabled=true; shape.shapeType=ParticleSystemShapeType.Mesh;
                    shape.mesh=null; shape.mesh=emitter; shape.meshShapeType=ParticleSystemMeshShapeType.Vertex;
                    shape.meshSpawnMode=ParticleSystemShapeMultiModeValue.BurstSpread;
                    var emission=ps.emission; emission.enabled=true; emission.rateOverTime=0;
                    emission.SetBursts(new[]{new ParticleSystem.Burst(.02f,(short)max)});
                    var renderer=go.GetComponent<ParticleSystemRenderer>(); Undo.RecordObject(renderer,"Material HDR aditivo");
                    renderer.sharedMaterial=material; renderer.renderMode=ParticleSystemRenderMode.Billboard;
                    renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
                    renderer.maxParticleSize=.3f; ps.Simulate(1,true,true,false);
                    // Editor preview does not reliably process burst events during a synchronous menu callback.
                    if(ps.particleCount==0) ps.Emit(max);
                    ps.Pause();
                    var emitted=new ParticleSystem.Particle[ps.particleCount]; ps.GetParticles(emitted);
                    int distinct=emitted.Select(p=>Key(p.position)).Distinct().Count();
                    if(distinct<Mathf.Min(10,max)) throw new InvalidOperationException("Halos concentrados en un punto: "+name+" / "+distinct);
                    Debug.Log("VOID_EMISSION "+name+": "+ps.particleCount+" particulas, "+distinct+" posiciones distintas.");
                    if(ps.particleCount<max/2) throw new InvalidOperationException("No se emitieron suficientes halos: "+name+" / "+ps.particleCount);
                }
                Glow("Energia continua en bordes",mesh,10,glow,mesh.vertexCount);
                Glow("Halos inferiores de sustentacion",lower,24,glow,lower.vertexCount);
                var sparks=Child("Particulas luminosas flotantes"); var particle=sparks.GetComponent<ParticleSystem>();
                if(!particle) particle=Undo.AddComponent<ParticleSystem>(sparks);
                Undo.RecordObject(particle,"Motas violetas"); particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.useAutoRandomSeed=false; particle.randomSeed=Seed;
                var pm=particle.main; pm.duration=18; pm.loop=true; pm.prewarm=true; pm.playOnAwake=true;
                pm.startLifetime=new ParticleSystem.MinMaxCurve(10,18); pm.startSpeed=.4f; pm.maxParticles=300;
                pm.startSize=new ParticleSystem.MinMaxCurve(.7f,1.8f); pm.startColor=new Color(.7f,.35f,1,.8f);
                var pshape=particle.shape; pshape.enabled=true; pshape.shapeType=ParticleSystemShapeType.Mesh;
                pshape.mesh=lower; pshape.meshShapeType=ParticleSystemMeshShapeType.Vertex;
                var pe=particle.emission; pe.enabled=true; pe.rateOverTime=14;
                var pr=sparks.GetComponent<ParticleSystemRenderer>(); Undo.RecordObject(pr,"Material de motas");
                pr.sharedMaterial=motes; pr.shadowCastingMode=ShadowCastingMode.Off; pr.receiveShadows=false;
                pm.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
                particle.Simulate(12,true,true,true); particle.Pause();
                if(particle.particleCount==0) throw new InvalidOperationException("No se emitieron motas luminosas.");
                var zones=source.Dungeon.Cast<Transform>().Where(t=>t.name.Length>3&&t.name[2]==' '&&t.name[3]=='-').ToArray();
                var anchors=new List<Edge>();
                foreach(var zone in zones)
                    anchors.Add(source.Edges.OrderBy(e=>Mathf.Abs(e.Point.y-zone.position.y)*5+Vector3.Distance(e.Point,zone.position+new Vector3(8,0,-35))).First());
                foreach(var edge in source.Edges.OrderBy(e=>Key(e.Point).GetHashCode()))
                { if(anchors.Count>=10) break; if(anchors.All(a=>Vector3.Distance(a.Point,edge.Point)>60)) anchors.Add(edge); }
                // Upgrade existing atmosphere lights in place. Reuse them instead of increasing the light budget.
                var oldLights=root.GetComponentsInChildren<Light>(true).Where(l=>l.type==LightType.Point&&!l.transform.IsChildOf(stage)).ToArray();
                for(int i=0;i<anchors.Count;i++)
                {
                    Light light;
                    if(i<oldLights.Length) light=oldLights[i];
                    else { var go=Child("Luz magica "+i); light=go.GetComponent<Light>(); if(!light) light=Undo.AddComponent<Light>(go); go.transform.position=anchors[i].Point+anchors[i].Normal*3-Vector3.up*4; }
                    Undo.RecordObject(light,"Reflejos violetas intensos"); light.enabled=true; light.type=LightType.Point;
                    light.color=i%3==0?new Color(.72f,.035f,1):new Color(.37f,.09f,1);
                    light.intensity=48; light.range=48; light.shadows=LightShadows.None;
                }
                var fillGO=Child("Relleno ambiental legible"); var fill=fillGO.GetComponent<Light>(); if(!fill) fill=Undo.AddComponent<Light>(fillGO);
                Undo.RecordObject(fill,"Relleno ambiental"); fill.type=LightType.Directional; fill.intensity=.28f;
                fill.color=new Color(.75f,.71f,1); fill.shadows=LightShadows.None;
                fillGO.transform.rotation=Quaternion.Euler(52,155,0);
                var volumeGO=Child("Bloom HDR y exposicion equilibrada"); var volume=volumeGO.GetComponent<Volume>(); if(!volume) volume=Undo.AddComponent<Volume>(volumeGO);
                Undo.RecordObject(volume,"Bloom URP"); volume.isGlobal=true; volume.priority=100; volume.weight=1; volume.sharedProfile=profile;
                var cameras=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Where(c=>c.enabled).ToArray();
                if(cameras.Length==0) throw new InvalidOperationException("No hay camara activa para validar Bloom.");
                foreach(var camera in cameras)
                {
                    Undo.RecordObject(camera,"Habilitar HDR"); camera.allowHDR=true;
                    var data=camera.GetComponent<UniversalAdditionalCameraData>(); if(!data) data=Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
                    Undo.RecordObject(data,"Activar postprocesado de la escena"); data.renderPostProcessing=true; data.volumeLayerMask|=1;
                    if(data.scriptableRenderer==null) throw new InvalidOperationException("Renderer URP de camara no disponible.");
                }
                // Only fragments already present receive a private material clone. No new rocks are generated.
                var floating=root.transform.Find("Fragmentos flotantes - sin colision");
                if(floating) foreach(var renderer in floating.GetComponentsInChildren<MeshRenderer>())
                {
                    Undo.RecordObject(renderer,"Reflejos en fragmentos existentes");
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(original=>
                    {
                        if(!original || !original.HasProperty("_EmissionColor")) return original;
                        string suffix=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
                        return Asset("FragmentReflection_"+suffix+".mat",()=>
                        { var m=new Material(original); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",new Color(.16f,.025f,.65f)); return m; });
                    }).ToArray();
                }
                Physics.SyncTransforms(); geometry.Verify();
                if(root.GetComponentsInChildren<Light>().Count(l=>l.enabled&&l.type==LightType.Point)>10)
                    throw new InvalidOperationException("Mas de diez luces puntuales activas; no se guardara.");
                if(stage.GetComponentsInChildren<Collider>(true).Length!=0 || stage.GetComponentsInChildren<MeshFilter>(true).Length!=0)
                    throw new InvalidOperationException("Los efectos no deben agregar geometria arquitectonica ni colliders.");
                if(stage.GetComponentsInChildren<Light>().Any(l=>l.shadows!=LightShadows.None)) throw new InvalidOperationException("Sombras nuevas inesperadas.");
                foreach(var camera in cameras)
                    if(!camera.allowHDR || !camera.GetUniversalAdditionalCameraData().renderPostProcessing) throw new InvalidOperationException("Bloom no configurado.");
                if(errors.Count>0) throw new InvalidOperationException("Errores de Unity durante la aplicacion: "+string.Join("; ",errors.Distinct()));
                Undo.RegisterFullObjectHierarchyUndo(stage.gameObject,"Efectos violetas completos");
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,Target,false)) throw new InvalidOperationException("No se pudo guardar la escena.");
                Undo.CollapseUndoOperations(undo); undo=-1;
                var view=SceneView.lastActiveSceneView;
                if(view) { view.sceneLighting=true; view.sceneViewState.showImageEffects=true; view.Repaint(); }
                Debug.Log("ILUMINACION VIOLETA VALIDADA Y GUARDADA: "+mesh.vertexCount+" halos de borde, "+lower.vertexCount+" halos inferiores, maximo 300 motas y diez luces puntuales sin sombras; Bloom 1.65, ACES y HDR/postprocesado activos. Toda geometria, colliders, sectores y rutas previos intactos.",root);
            }
            catch(Exception e)
            { if(undo>=0) Undo.RevertAllDownToGroup(undo); Debug.LogError("Mejora violeta detenida, no se guardo: "+e.Message); }
            finally { Application.logMessageReceived-=capture; }
        }

        sealed class GeometryGuard
        {
            readonly List<Action> checks=new List<Action>();
            public GeometryGuard(Scene scene,Transform excluded=null)
            {
                foreach(var root in scene.GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if(excluded && (t==excluded || t.IsChildOf(excluded))) continue;
                    var original=t; Vector3 position=t.localPosition,scale=t.localScale; Quaternion rotation=t.localRotation;
                    var parent=t.parent; string name=t.name; bool active=t.gameObject.activeSelf;
                    checks.Add(()=>{ if(!original || original.parent!=parent || original.localPosition!=position || original.localRotation!=rotation || original.localScale!=scale || original.name!=name || original.gameObject.activeSelf!=active) throw new InvalidOperationException("Objeto previo movido/eliminado: "+name); });
                    foreach(var collider in t.GetComponents<Collider>())
                    { var captured=collider; string json=EditorJsonUtility.ToJson(collider); checks.Add(()=>{if(!captured||EditorJsonUtility.ToJson(captured)!=json)throw new InvalidOperationException("Collider previo alterado: "+name);}); }
                    foreach(var filter in t.GetComponents<MeshFilter>())
                    { var captured=filter; var mesh=filter.sharedMesh; checks.Add(()=>{if(!captured||captured.sharedMesh!=mesh)throw new InvalidOperationException("Malla previa alterada: "+name);}); }
                }
            }
            public void Verify() { foreach(var check in checks) check(); }
        }

        [MenuItem(Menu+"1. Previsualizar en copia temporal")]
        public static void Preview()
        {
            if(!Ready) return;
            try
            {
                ClosePreview();
                var scene=SceneManager.GetActiveScene();
                var source=ReadSource(scene); LoadPrefabs();
                var stage=ScriptableObject.CreateInstance<VoidPreview>(); stage.Source=source;
                StageUtility.GoToStage(stage,true);
            }
            catch(Exception e) { Debug.LogException(e); }
        }

        [MenuItem(Menu+"2. Integrar o regenerar SOLO tras aprobacion (sin guardar)")]
        public static void Integrate()
        {
            if(!Ready) return;
            int undo=-1; GameObject pending=null;
            try
            {
                if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())
                    throw new InvalidOperationException("Cierra la previsualizacion antes de integrar.");
                var scene=SceneManager.GetActiveScene(); var source=ReadSource(scene);
                var existing=FindRoot(scene);
                if(!EditorUtility.DisplayDialog("Aprobacion de atmosfera del Vacio",
                    "Continuar solo si aprobaste la previsualizacion. Se generara la atmosfera desde prefabs originales, sin copiar TEMP ni guardar la escena.",
                    "Integro la version aprobada","Cancelar")) return;
                var prefabs=LoadPrefabs(); var palette=CreatePalette(true);
                Undo.IncrementCurrentGroup(); undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Atmosfera del Vacio aprobada");
                pending=Build(source,scene,prefabs,palette,true);
                ValidateRoot(source,pending);
                if(existing) Undo.DestroyObjectImmediate(existing);
                pending.name=Root;
                Undo.RegisterFullObjectHierarchyUndo(pending,"Atmosfera completa");
                Undo.CollapseUndoOperations(undo); undo=-1;
                EditorSceneManager.MarkSceneDirty(scene);
                CameraWarnings(scene);
                Debug.Log("Atmosfera integrada/renovada sin duplicados. Dungeon intacto; revisa y guarda manualmente.",pending);
                pending=null;
            }
            catch(Exception e)
            {
                if(undo>=0) Undo.RevertAllDownToGroup(undo);
                else if(pending) Object.DestroyImmediate(pending);
                Debug.LogException(e);
            }
        }

        [MenuItem(Menu+"3. Validar distancias, colliders y luces")]
        public static void Validate()
        {
            try
            {
                var preview=StageUtility.GetCurrentStage() as VoidPreview;
                var source=preview?preview.PreviewSource:ReadSource(SceneManager.GetActiveScene());
                var root=FindRoot(source.Dungeon.gameObject.scene);
                if(!root) throw new InvalidOperationException("No existe atmosfera en este contexto.");
                ValidateRoot(source,root); CameraWarnings(source.Dungeon.gameObject.scene);
            }
            catch(Exception e) { Debug.LogException(e); }
        }

        [MenuItem(Menu+"4. Eliminar exclusivamente Dungeon_Void_Atmosphere")]
        public static void Remove()
        {
            if(!Ready) return;
            try
            {
                var preview=StageUtility.GetCurrentStage() as VoidPreview;
                var source=preview?preview.PreviewSource:ReadSource(SceneManager.GetActiveScene());
                var root=FindRoot(source.Dungeon.gameObject.scene);
                if(root) Undo.DestroyObjectImmediate(root);
                // Generated palette assets remain reusable; no scene or source assets are deleted/saved.
            }
            catch(Exception e) { Debug.LogException(e); }
        }

        [MenuItem(Menu+"5. Cerrar previsualizacion sin guardar")]
        public static void ClosePreview()
        { if(Ready && StageUtility.GetCurrentStage() is VoidPreview) StageUtility.GoToMainStage(); }

        static GameObject FindRoot(Scene scene)
        {
            var found=scene.GetRootGameObjects().Where(g=>g.name==Root).ToArray();
            if(found.Length>1 || (found.Length==1 && !found[0].transform.Find(Signature)))
                throw new InvalidOperationException("Raiz de atmosfera duplicada o ajena; no se modificara.");
            return found.FirstOrDefault();
        }
        static Source ReadSource(Scene scene,bool preview=false)
        {
            if(!preview && (scene.path!=Target || StageUtility.GetCurrentStage()!=StageUtility.GetMainStage()))
                throw new InvalidOperationException("Activa Dungeon_Isla_del_Sol en el escenario principal.");
            if(!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                throw new InvalidOperationException("Se requiere URP activo.");
            var roots=scene.GetRootGameObjects().Where(g=>g.name==Base).ToArray();
            if(roots.Length!=1 || !roots[0].transform.Find("__ROM_SolarStructure_12D9D67A_v1"))
                throw new InvalidOperationException("No se encontro una base unica y firmada. No se reconstruira.");
            var source=new Source{Dungeon=roots[0].transform};
            Physics.SyncTransforms();
            var children=source.Dungeon.Cast<Transform>().ToArray();
            if(children.Count(t=>t.name.Length>3 && t.name[2]==' ' && t.name[3]=='-')!=7 ||
               children.Count(t=>t.name.Length>5 && t.name[2]=='-' && char.IsDigit(t.name[3]) && t.name[5]==' ')!=9)
                throw new InvalidOperationException("Se requieren siete sectores y nueve recorridos existentes.");
            foreach(var renderer in roots[0].GetComponentsInChildren<Renderer>())
                if(renderer.enabled && renderer is MeshRenderer) source.Geometry.Add(renderer.bounds);
            foreach(var collider in roots[0].GetComponentsInChildren<Collider>())
                if(collider.enabled && !collider.isTrigger) source.Geometry.Add(collider.bounds);
            // Boundary of actual 6m floor modules, including bridge floors. No idealized island circles.
            var tiles=new Dictionary<Vector3Int,Bounds>();
            foreach(var filter in roots[0].GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(!renderer || !renderer.enabled || !filter.sharedMesh) continue;
                Transform t=filter.transform;
                bool module=false;
                for(;t && t!=source.Dungeon;t=t.parent) if(t.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)) { module=true; break; }
                Bounds b=renderer.bounds;
                if(!module || Mathf.Abs(b.size.x-6)>.1f || Mathf.Abs(b.size.z-6)>.1f) continue;
                var key=Key(new Vector3(b.center.x,b.max.y,b.center.z)); tiles[key]=b;
            }
            if(tiles.Count<100) throw new InvalidOperationException("No se reconocen los modulos transitables de 6m.");
            source.Walking.AddRange(tiles.Values);
            foreach(var pair in tiles.OrderBy(p=>p.Key.x).ThenBy(p=>p.Key.y).ThenBy(p=>p.Key.z))
                foreach(var normal in new[]{Vector3.left,Vector3.right,Vector3.back,Vector3.forward})
                {
                    Vector3 p=new Vector3(pair.Value.center.x,pair.Value.max.y,pair.Value.center.z);
                    if(!tiles.ContainsKey(Key(p+normal*6))) source.Edges.Add(new Edge{Point=p+normal*3,Normal=normal});
                }
            if(source.Edges.Count<20) throw new InvalidOperationException("Sin bordes suficientes para distribuir la atmosfera.");
            return source;
        }
        static Vector3Int Key(Vector3 v)=>new Vector3Int(Mathf.RoundToInt(v.x*10),Mathf.RoundToInt(v.y*10),Mathf.RoundToInt(v.z*10));
        static GameObject[] LoadPrefabs()
        { return Paths.Select(p=>AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? throw new InvalidOperationException("Falta prefab: "+p)).ToArray(); }

        static Palette CreatePalette(bool persistent)
        {
            var result=new Palette();
            if(persistent && !AssetDatabase.IsValidFolder(Generated))
            {
                if(!AssetDatabase.IsValidFolder("Assets/ROM/Generated")) AssetDatabase.CreateFolder("Assets/ROM","Generated");
                AssetDatabase.CreateFolder("Assets/ROM/Generated","VoidAtmosphere");
            }
            T Own<T>(string name,Func<T> create) where T:Object
            {
                string path=Generated+"/"+name;
                if(persistent)
                {
                    var existing=AssetDatabase.LoadAssetAtPath<T>(path);
                    if(existing)
                    {
                        // Unity normalizes names to the file stem when importing newly created assets.
                        string stem=System.IO.Path.GetFileNameWithoutExtension(name);
                        if(existing.name!=stem && !existing.name.StartsWith("ROM_Void_",StringComparison.Ordinal)) throw new InvalidOperationException("Asset ajeno en "+path);
                        return existing;
                    }
                    if(AssetDatabase.LoadMainAssetAtPath(path)) throw new InvalidOperationException("Asset ajeno en "+path);
                }
                var item=create(); item.name="ROM_Void_"+name;
                if(persistent) AssetDatabase.CreateAsset(item,path);
                else { item.hideFlags=HideFlags.HideAndDontSave; result.Temporary.Add(item); }
                return item;
            }
            Material Emissive(Color color)
            {
                var shader=Shader.Find("Universal Render Pipeline/Lit");
                if(!shader || !shader.isSupported) throw new InvalidOperationException("Shader URP Lit no disponible.");
                var m=new Material(shader); m.SetColor("_BaseColor",color*.3f); m.SetColor("_EmissionColor",color*5);
                m.EnableKeyword("_EMISSION"); m.SetFloat("_Smoothness",.6f); return m;
            }
            result.Violet=Own("Violet.mat",()=>Emissive(new Color(.38f,.025f,1)));
            result.Magenta=Own("Magenta.mat",()=>Emissive(new Color(.85f,.02f,.65f)));
            var texture=Own("Nebula.asset",()=>
            {
                var tex=new Texture2D(64,64,TextureFormat.RGBA32,false); tex.wrapMode=TextureWrapMode.Clamp;
                var pixels=new Color[4096];
                for(int y=0;y<64;y++) for(int x=0;x<64;x++)
                { float r=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude; pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),2)); }
                tex.SetPixels(pixels); tex.Apply(); return tex;
            });
            result.Fog=Own("Nebula.mat",()=>
            {
                var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if(!shader || !shader.isSupported) throw new InvalidOperationException("Shader URP Particles/Unlit no disponible.");
                var m=new Material(shader); m.SetTexture("_BaseMap",texture); m.SetColor("_BaseColor",new Color(.23f,.025f,.44f,.65f));
                m.SetFloat("_Surface",1); m.SetFloat("_ZWrite",0);
                m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue=(int)RenderQueue.Transparent; return m;
            });
            result.Profile=Own("Atmosphere.asset",()=>
            {
                var profile=ScriptableObject.CreateInstance<VolumeProfile>();
                var bloom=profile.Add<Bloom>(); bloom.threshold.Override(1); bloom.intensity.Override(.65f); bloom.scatter.Override(.75f);
                var color=profile.Add<ColorAdjustments>(); color.postExposure.Override(-.55f); color.colorFilter.Override(new Color(.83f,.72f,1));
                var vignette=profile.Add<Vignette>(); vignette.intensity.Override(.26f); vignette.color.Override(new Color(.055f,.005f,.1f));
                if(persistent) foreach(var component in profile.components) component.hideFlags=HideFlags.HideInHierarchy;
                else foreach(var component in profile.components) { component.hideFlags=HideFlags.HideAndDontSave; result.Temporary.Add(component); }
                return profile;
            });
            if(persistent)
            {
                foreach(var component in result.Profile.components)
                    if(!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component,result.Profile);
                EditorUtility.SetDirty(result.Profile); AssetDatabase.SaveAssetIfDirty(result.Profile);
            }
            return result;
        }

        static GameObject Build(Source source,Scene scene,GameObject[] prefabs,Palette palette,bool undo)
        {
            var root=new GameObject(Root+"__Building"); SceneManager.MoveGameObjectToScene(root,scene);
            if(undo) Undo.RegisterCreatedObjectUndo(root,"Crear atmosfera del Vacio");
            try
            {
                new GameObject(Signature).transform.SetParent(root.transform,false);
                Transform Group(string name) { var go=new GameObject(name); go.transform.SetParent(root.transform,false); return go.transform; }
                var fragments=Group("Fragmentos flotantes - sin colision"); var cores=Group("Nucleos emisivos URP");
                var lights=Group("Luces violetas - maximo 10 sin sombras"); var nebula=Group("Nebulosa bajo el mapa");
                var random=new System.Random(Seed); float Range(float a,float b)=>(float)(a+random.NextDouble()*(b-a));
                int count=0;
                for(int i=0;i<220;i++)
                {
                    bool core=i%5==0; var edge=source.Edges[random.Next(source.Edges.Count)];
                    bool placed=false;
                    for(int attempt=0;attempt<10 && !placed;attempt++)
                    {
                        int index=core?4:random.Next(prefabs.Length);
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[index],scene);
                        instance.transform.SetParent(core?cores:fragments,false);
                        instance.name=(core?"Nucleo_":"Ruina_")+i.ToString("D3")+"_"+prefabs[index].name;
                        instance.transform.position=edge.Point+edge.Normal*Range(12,42)+new Vector3(Range(-7,7),-Range(15,85),Range(-7,7));
                        instance.transform.rotation=Quaternion.Euler(Range(-35,35),Range(0,360),Range(-28,28));
                        instance.transform.localScale=prefabs[index].transform.localScale*(core?Range(.45f,1.1f):Range(.55f,i%7==0?2.4f:1.45f));
                        foreach(var script in instance.GetComponentsInChildren<MonoBehaviour>(true)) if(script) script.enabled=false;
                        foreach(var collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
                        foreach(var body in instance.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic=true; body.useGravity=false; body.detectCollisions=false; }
                        foreach(var audio in instance.GetComponentsInChildren<AudioSource>(true)) audio.enabled=false;
                        foreach(var light in instance.GetComponentsInChildren<Light>(true)) light.enabled=false;
                        foreach(var renderer in instance.GetComponentsInChildren<Renderer>())
                        {
                            renderer.shadowCastingMode=ShadowCastingMode.Off;
                            if(core) renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>i%2==0?palette.Violet:palette.Magenta).ToArray();
                        }
                        var bounds=Visible(instance);
                        if(!Safe(source,bounds,Clearance)) { Object.DestroyImmediate(instance); continue; }
                        placed=true; count++;
                    }
                }
                if(count<150) throw new InvalidOperationException("No hay espacio seguro para suficientes fragmentos: "+count);
                var anchors=new List<Edge>();
                // Seven zone accents and three separated bridge accents, chosen from their actual boundaries.
                foreach(Transform zone in source.Dungeon)
                {
                    if(zone.name.Length<4 || zone.name[2]!=' ' || zone.name[3]!='-') continue;
                    anchors.Add(source.Edges.OrderBy(e=>Mathf.Abs(e.Point.y-zone.position.y)*5+Vector3.Distance(e.Point,zone.position+new Vector3(12,0,-35))).First());
                }
                foreach(var edge in source.Edges.OrderBy(e=>random.Next()))
                { if(anchors.Count>=LightLimit) break; if(anchors.All(a=>Vector3.Distance(a.Point,edge.Point)>65)) anchors.Add(edge); }
                for(int i=0;i<anchors.Count;i++)
                {
                    var go=new GameObject("Luz_Vacio_"+i); go.transform.SetParent(lights,false);
                    go.transform.position=anchors[i].Point+anchors[i].Normal*4-Vector3.up*6;
                    var light=go.AddComponent<Light>(); light.type=LightType.Point; light.shadows=LightShadows.None;
                    light.color=i%3==0?new Color(.9f,.025f,.6f):new Color(.4f,.04f,1);
                    light.intensity=16; light.range=32; light.renderMode=LightRenderMode.Auto;
                }
                for(int i=0;i<18;i++)
                {
                    var edge=source.Edges[random.Next(source.Edges.Count)]; var go=new GameObject("Nebulosa estatica_"+i);
                    go.transform.SetParent(nebula,false); float size=Range(70,125);
                    go.transform.position=edge.Point+edge.Normal*Range(10,40)-Vector3.up*(size*.5f+30);
                    var ps=go.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.useAutoRandomSeed=false; ps.randomSeed=(uint)(Seed+i);
                    var main=ps.main; main.duration=60; main.loop=true; main.prewarm=true; main.startLifetime=60;
                    main.startSpeed=0; main.startSize=size; main.maxParticles=1; main.startColor=new Color(.65f,.2f,1,.65f);
                    var emission=ps.emission; emission.rateOverTime=0;
                    emission.SetBursts(new[]{new ParticleSystem.Burst(0,1)});
                    var shape=ps.shape; shape.enabled=false;
                    var renderer=go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=palette.Fog;
                    renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
                    ps.Play();
                }
                var volumeObject=new GameObject("Gradacion violeta y bloom URP"); volumeObject.transform.SetParent(root.transform,false);
                var volume=volumeObject.AddComponent<Volume>(); volume.isGlobal=true; volume.priority=15; volume.sharedProfile=palette.Profile;
                root.name=Root; ValidateRoot(source,root);
                Debug.Log("Vacio: "+count+" fragmentos/nucleos, "+anchors.Count+" luces sin sombras, 18 nubes. Semilla "+Seed+". Distancia minima "+Clearance+"m de la geometria y rutas.");
                return root;
            }
            catch { if(!undo) Object.DestroyImmediate(root); throw; }
        }
        static Bounds Visible(GameObject root)
        {
            var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
            if(renderers.Length==0) throw new InvalidOperationException("Fragmento sin malla visible: "+root.name);
            var bounds=renderers[0].bounds; foreach(var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        static bool Safe(Source source,Bounds bounds,float distance)
        {
            var expanded=bounds; expanded.Expand(distance*2);
            return !source.Geometry.Any(b=>b.Intersects(expanded)) && !source.Walking.Any(b=>b.Intersects(expanded));
        }
        static void ValidateRoot(Source source,GameObject root)
        {
            if(!root || !root.transform.Find(Signature)) throw new InvalidOperationException("Atmosfera sin firma.");
            Physics.SyncTransforms();
            foreach(string group in new[]{"Fragmentos flotantes - sin colision","Nucleos emisivos URP"})
            {
                var parent=root.transform.Find(group); if(!parent || parent.childCount==0) throw new InvalidOperationException("Falta grupo "+group);
                foreach(Transform child in parent)
                {
                    if(!Safe(source,Visible(child.gameObject),Clearance-.05f)) throw new InvalidOperationException("Fragmento demasiado cerca del mapa/ruta: "+child.name);
                    foreach(var renderer in child.GetComponentsInChildren<Renderer>())
                        if(renderer.sharedMaterials.Any(m=>!m || !m.shader)) throw new InvalidOperationException("Material faltante: "+child.name);
                }
            }
            if(root.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled)) throw new InvalidOperationException("La atmosfera no debe tener colliders activos.");
            if(root.GetComponentsInChildren<MonoBehaviour>(true).Any(m=>m && m.enabled && !(m is Volume)))
                throw new InvalidOperationException("Comportamiento importado activo.");
            var lights=root.GetComponentsInChildren<Light>().Where(l=>l.enabled).ToArray();
            if(lights.Length>LightLimit || lights.Any(l=>l.shadows!=LightShadows.None || l.type!=LightType.Point))
                throw new InvalidOperationException("Presupuesto de luces excedido.");
            foreach(var light in lights)
                if(source.Walking.Any(b=>b.Contains(light.transform.position))) throw new InvalidOperationException("Luz dentro de una superficie transitable.");
            Debug.Log("Atmosfera validada: separacion >=5m, colliders nuevos desactivados, sin gameplay y maximo diez luces sin sombras.");
        }
        static void CameraWarnings(Scene scene)
        {
            foreach(var camera in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Where(c=>c.enabled))
                if(!camera.TryGetComponent<UniversalAdditionalCameraData>(out var data) || !data.renderPostProcessing || (data.volumeLayerMask.value&1)==0)
                    Debug.LogWarning(camera.name+": bloom/gradacion requieren postprocesado y capa Default en la mascara de volumen. La herramienta no modifica esta camara; luces y materiales siguen visibles.");
            if(UniversalRenderPipeline.asset && !UniversalRenderPipeline.asset.supportsHDR)
                Debug.LogWarning("HDR desactivado en URP: resplandor limitado. No se modifico el pipeline.");
        }

        sealed class VoidPreview:PreviewSceneStage
        {
            public Source Source,PreviewSource; Palette palette;
            protected override GUIContent CreateHeaderContent()=>new GUIContent("Ciudad del Sol - Vacio (temporal)");
            protected override bool OnOpenStage()
            {
                if(Source==null || !Source.Dungeon || !base.OnOpenStage()) return false;
                try
                {
                    Scene temp=scene; temp.name="TEMP_CiudadDelSol_VoidAtmosphere";
                    var holder=new GameObject("Contexto temporal"); SceneManager.MoveGameObjectToScene(holder,scene); holder.SetActive(false);
                    var clone=Object.Instantiate(Source.Dungeon.gameObject,holder.transform);
                    clone.name=Base;
                    foreach(var script in clone.GetComponentsInChildren<MonoBehaviour>(true)) if(script) script.enabled=false;
                    foreach(var audio in clone.GetComponentsInChildren<AudioSource>(true)) audio.enabled=false;
                    clone.transform.SetParent(null,true);
                    clone.SetActive(true); PreviewSource=ReadSource(scene,true);
                    palette=CreatePalette(false); Build(PreviewSource,scene,LoadPrefabs(),palette,false);
                    var rig=new GameObject("Iluminacion y camara - solo copia temporal"); SceneManager.MoveGameObjectToScene(rig,scene);
                    var key=rig.AddComponent<Light>(); key.type=LightType.Directional; key.color=new Color(1,.83f,.65f); key.intensity=.9f;
                    rig.transform.rotation=Quaternion.Euler(48,-32,0);
                    var cameraGO=new GameObject("Camara Vacio"); cameraGO.transform.SetParent(rig.transform,false);
                    cameraGO.transform.position=new Vector3(420,380,-480); cameraGO.transform.LookAt(new Vector3(35,-10,45));
                    var camera=cameraGO.AddComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor;
                    camera.backgroundColor=new Color(.013f,.003f,.033f); camera.allowHDR=true; camera.farClipPlane=2000; camera.enabled=false;
                    camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
                    EditorApplication.delayCall+=()=>
                    {
                        if(!this || StageUtility.GetCurrentStage()!=this) return;
                        var view=SceneView.lastActiveSceneView;
                        if(view) { view.sceneLighting=true; view.sceneViewState.showFog=true; view.sceneViewState.showImageEffects=true; view.sceneViewState.showSkybox=false; view.LookAtDirect(new Vector3(35,-10,45),Quaternion.Euler(48,-28,0),330); view.Repaint(); }
                        Selection.activeGameObject=null;
                    };
                    Source=null; Debug.Log("Previsualizacion del Vacio lista; la escena original y sus arquitecturas siguen intactas."); return true;
                }
                catch(Exception e) { Debug.LogException(e); base.OnCloseStage(); palette?.Dispose(); return false; }
            }
            protected override void OnCloseStage() { base.OnCloseStage(); palette?.Dispose(); palette=null; }
        }
    }
}
