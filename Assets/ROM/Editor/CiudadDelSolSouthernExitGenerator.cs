using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    // Manual temporary review only: never opens, edits or saves the original scene.
    public static class CiudadDelSolSouthernExitGenerator
    {
        const string Target = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Dungeon = "Dungeon_Ciudad_del_Sol_Generated";
        const string Route = "01-06 - Ruta secundaria meridional";
        const string Root = "Salida_Meridional_Generated";
        const string Menu = "ROM/Dungeon Ciudad del Sol/Salida meridional/";
        const string ET = "Assets/Standard Assets/Eternal Temple/Prefabs/";
        static readonly Dictionary<string,string> Paths = new Dictionary<string,string>
        {
            {"Base", ET + "Pits and Platforms/Platform_Base_01.prefab"},
            {"Rail", ET + "Railings and Decors/Railing_01.prefab"},
            {"Column", ET + "Columns/Column_01.prefab"}
        };
        sealed class Asset { public GameObject Prefab; public Bounds Bounds; }
        sealed class Piece { public string Key; public GameObject Object; public Bounds Bounds; }
        sealed class Source
        {
            public Scene Scene;
            public Transform Root, Route;
            public string FileHash;
            public bool Dirty;
            public readonly List<Action> Checks = new List<Action>();
            public void Verify()
            {
                foreach(var check in Checks) check();
                if(Scene.isDirty != Dirty || FileHash != HashScene())
                    throw new InvalidOperationException("La escena original cambio durante la revision.");
            }
        }
        sealed class Plan
        {
            public Source Source;
            public Transform Root, Context;
            public readonly List<Piece> Pieces = new List<Piece>();
        }
        static bool Ready => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
        static string HashScene()
        {
            using(var sha = System.Security.Cryptography.SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(System.IO.File.ReadAllBytes(Target)));
        }

        [MenuItem(Menu + "1. Previsualizar salida inferior (copia temporal) %#&m")]
        public static void Preview()
        {
            if(!Ready) return;
            try
            {
                Close();
                var stage = ScriptableObject.CreateInstance<ExitPreview>();
                stage.Source = Inspect(); stage.Assets = LoadAssets();
                StageUtility.GoToStage(stage, true);
            }
            catch(Exception e) { Debug.LogException(e); }
        }
        [MenuItem(Menu + "2. Validar continuidad y colliders")]
        public static void Validate()
        {
            try
            {
                var stage = StageUtility.GetCurrentStage() as ExitPreview;
                if(!stage || stage.Plan == null) throw new InvalidOperationException("Abri primero la copia temporal de la salida.");
                ValidatePlan(stage.Plan);
            }
            catch(Exception e) { Debug.LogException(e); }
        }
        [MenuItem(Menu + "3. Cerrar previsualizacion sin guardar")]
        public static void Close()
        {
            if(Ready && StageUtility.GetCurrentStage() is ExitPreview) StageUtility.GoToMainStage();
        }
        static Source Inspect()
        {
            var scene = SceneManager.GetActiveScene();
            if(StageUtility.GetCurrentStage() != StageUtility.GetMainStage() || scene.path != Target)
                throw new InvalidOperationException("Activa Dungeon_Isla_del_Sol en el escenario principal.");
            var roots = scene.GetRootGameObjects().Where(g => g.name == Dungeon).ToArray();
            if(roots.Length != 1) throw new InvalidOperationException("Base ausente o duplicada. No se regenerara.");
            var root = roots[0].transform;
            if(root.position.sqrMagnitude > .0001f || Quaternion.Angle(root.rotation,Quaternion.identity) > .01f ||
                (root.lossyScale-Vector3.one).sqrMagnitude > .0001f)
                throw new InvalidOperationException("Transformacion de la base inesperada.");
            string[] routes = {"01-02 -","02-03 -","03-04 -","03-05 -","04-05 -","03-06 -","05-07 -","06-07 -","01-06 -"};
            foreach(var prefix in routes)
                if(root.Cast<Transform>().Count(t => t.name.StartsWith(prefix,StringComparison.Ordinal)) != 1)
                    throw new InvalidOperationException("Recorrido ausente o duplicado: " + prefix);
            string[] approved = {"01 - Entrada Portico Solar/PorticoSolar_Architecture","02 - Jardines Dorados/JardinesDorados_Architecture",
                "03 - Plaza del Eclipse/PlazaDelEclipse_Architecture","04 - Archivos Solares/ArchivosSolares_Architecture",
                "05 - Claustro del Eclipse/ClaustroDelEclipse_Architecture","06 - Santuario Profano/SantuarioProfano_Architecture",
                "07 - El Sol Corrompido/ElSolCorrompido_Architecture"};
            foreach(var path in approved) if(!root.Find(path)) throw new InvalidOperationException("Falta arquitectura aprobada: " + path);
            var source = new Source {Scene=scene, Root=root, Route=root.Cast<Transform>().Single(t=>t.name.StartsWith(Route,StringComparison.Ordinal)), FileHash=HashScene(), Dirty=scene.isDirty};
            if(!source.Route) throw new InvalidOperationException("No se encontro el puente inferior original.");
            // Compare every existing component, transform and child list after creating the temporary review.
            foreach(var originalRoot in scene.GetRootGameObjects())
                foreach(var t in originalRoot.GetComponentsInChildren<Transform>(true))
                {
                    var original = t; var parent=t.parent; var position=t.localPosition; var rotation=t.localRotation; var scale=t.localScale;
                    var children=t.Cast<Transform>().Select(c=>c.GetInstanceID()).ToArray();
                    var components=t.GetComponents<Component>().Select(c=>c?c.GetInstanceID():0).ToArray();
                    string name=t.name; bool active=t.gameObject.activeSelf; int sibling=t.GetSiblingIndex();
                    source.Checks.Add(() =>
                    {
                        if(!original || original.parent!=parent || original.localPosition!=position || original.localRotation!=rotation ||
                            original.localScale!=scale || original.name!=name || original.gameObject.activeSelf!=active || original.GetSiblingIndex()!=sibling ||
                            !children.SequenceEqual(original.Cast<Transform>().Select(c=>c.GetInstanceID())) ||
                            !components.SequenceEqual(original.GetComponents<Component>().Select(c=>c?c.GetInstanceID():0)))
                            throw new InvalidOperationException("Objeto original alterado: " + name);
                    });
                    foreach(var c in t.GetComponents<Component>())
                    {
                        if(!c || c is Transform) continue;
                        var component=c; string json=EditorJsonUtility.ToJson(c);
                        source.Checks.Add(() => { if(!component || EditorJsonUtility.ToJson(component)!=json) throw new InvalidOperationException("Componente original alterado: " + name); });
                    }
                }
            return source;
        }

        static Dictionary<string,Asset> LoadAssets()
        {
            var result = new Dictionary<string,Asset>();
            foreach(var path in Paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path.Value);
                if(!prefab) throw new InvalidOperationException("Falta prefab: " + path.Value);
                var normalize=Matrix4x4.TRS(Vector3.zero,prefab.transform.localRotation,prefab.transform.localScale)*prefab.transform.worldToLocalMatrix;
                Bounds bounds=default; bool found=false;
                foreach(var f in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r=f.GetComponent<MeshRenderer>();
                    if(!r || !r.enabled || !f.sharedMesh || r.name.EndsWith("_Shadow",StringComparison.OrdinalIgnoreCase)) continue;
                    bool active=true;
                    for(var t=f.transform;t;t=t.parent) { if(!t.gameObject.activeSelf) active=false; if(t==prefab.transform) break; }
                    if(!active) continue;
                    var b=TransformBounds(f.sharedMesh.bounds,normalize*f.transform.localToWorldMatrix);
                    if(found) bounds.Encapsulate(b); else { bounds=b; found=true; }
                }
                if(!found) throw new InvalidOperationException("Prefab sin malla: " + path.Value);
                result[path.Key]=new Asset{Prefab=prefab,Bounds=bounds};
            }
            if((result["Base"].Bounds.size-new Vector3(6,2,6)).sqrMagnitude>.01f)
                throw new InvalidOperationException("El modulo de piedra ya no mide 6 x 2 x 6.");
            return result;
        }
        static Plan Build(Source source, Transform context, Scene scene, Dictionary<string,Asset> assets)
        {
            var root=new GameObject(Root); SceneManager.MoveGameObjectToScene(root,scene);
            var plan=new Plan{Root=root.transform,Context=context,Source=source};
            var groups=new Dictionary<string,Transform>();
            GameObject P(string group,string key,float x,float z,float y,float yaw=0,bool top=false)
            {
                if(!groups.TryGetValue(group,out var parent))
                { var g=new GameObject(group); g.transform.SetParent(root.transform,false); groups[group]=parent=g.transform; }
                var asset=assets[key]; var q=Quaternion.Euler(0,yaw,0);
                var b=TransformBounds(asset.Bounds,Matrix4x4.Rotate(q));
                var pivot=new Vector3(x-b.center.x,y-(top?b.max.y:b.min.y),z-b.center.z); b.center+=pivot;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(asset.Prefab,scene);
                go.transform.SetParent(parent,true); go.transform.SetPositionAndRotation(pivot,q*asset.Prefab.transform.localRotation);
                go.transform.localScale=asset.Prefab.transform.localScale;
                DisableImportedBehaviour(go);
                if(!go.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&!c.isTrigger))
                    throw new InvalidOperationException("Prefab sin collider funcional: "+key);
                plan.Pieces.Add(new Piece{Key=key,Object=go,Bounds=b}); return go;
            }
            void Floor(string group,float x,float z,float y) => P(group,"Base",x,z,y,0,true);
            void Rails(string group,float left,float right,float z,float y)
            { P(group,"Rail",left+.53f,z,y); P(group,"Rail",right-.53f,z,y); }
            void Pier(float x,float z,float top)
            {
                float h=assets["Column"].Bounds.size.y;
                P("05 - Soportes arquitectonicos","Column",x,z,top,0,true);
                P("05 - Soportes arquitectonicos","Column",x,z,top-h,0,true);
            }
            // Native 6m modules meet the south edge of the original bridge at Z=-135.
            // Keep its railings: four capsules pass through the existing junction gap at X=31.5.
            foreach(float x in new[]{27f,33f}) Floor("01 - Conexion y plataforma",x,-138,0);
            foreach(float x in new[]{21f,27f,33f,39f}) foreach(float z in new[]{-144f,-150f})
                Floor("01 - Conexion y plataforma",x,z,0);
            Rails("04 - Pretiles",24,36,-138,0);
            foreach(float x in new[]{21f,39f}) P("04 - Pretiles","Rail",x,-141.53f,0,90);
            foreach(float z in new[]{-144f,-150f}) Rails("04 - Pretiles",18,42,z,0);

            // Eight broad 0.25m risers made from original stone platform modules at native scale.
            // Overlapped solid foundations expose 3m treads; first/last treads are 6m landings.
            for(int i=0;i<8;i++)
            {
                float z=i==7?-180:-156-i*3, y=-(i+1)*.25f;
                foreach(float x in new[]{21f,27f,33f,39f}) Floor("02 - Escalinata monumental - 8 peldanos",x,z,y);
            }
            float[] guardZ={-156,-162,-168,-174,-180};
            float[] guardY={-.25f,-.75f,-1.25f,-1.75f,-2};
            for(int i=0;i<guardZ.Length;i++) Rails("04 - Pretiles",18,42,guardZ[i],guardY[i]);
            foreach(float x in new[]{21f,27f,33f,39f}) foreach(float z in new[]{-186f,-192f})
                Floor("03 - Descanso y puente inferior",x,z,-2);
            foreach(float z in new[]{-186f,-192f}) Rails("04 - Pretiles",18,42,z,-2);
            foreach(float x in new[]{21f,39f}) P("04 - Pretiles","Rail",x,-194.47f,-2,90);
            for(int row=0;row<6;row++)
            {
                float z=-198-row*6;
                foreach(float x in new[]{24f,30f,36f}) Floor("03 - Descanso y puente inferior",x,z,-2);
                Rails("04 - Pretiles",21,39,z,-2);
                if(row%2==0) foreach(float x in new[]{24f,36f}) Pier(x,z,-4);
            }
            foreach(float x in new[]{24f,30f,36f}) P("06 - Cierre provisional sin portal","Rail",x,-230.47f,-2,90);
            foreach(float x in new[]{27f,33f}) Pier(x,-138,-2);
            foreach(float x in new[]{21f,39f})
            {
                foreach(float z in new[]{-144f,-150f}) Pier(x,z,-2);
                Pier(x,-162,-2.75f); Pier(x,-174,-3.75f);
                foreach(float z in new[]{-186f,-192f}) Pier(x,z,-4);
            }
            Physics.SyncTransforms(); return plan;
        }

        static void ValidatePlan(Plan plan)
        {
            Physics.SyncTransforms();
            var all=plan.Context.GetComponentsInChildren<Collider>().Concat(plan.Root.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger).ToArray();
            var hits=new Collider[64]; var physics=plan.Root.gameObject.scene.GetPhysicsScene();
            float Surface(float x,float z,float expected)
            {
                float y=float.NegativeInfinity;
                foreach(var c in all) if(c.Raycast(new Ray(new Vector3(x,expected+1.25f,z),Vector3.down),out var hit,1.9f)&&hit.normal.y>.5f) y=Mathf.Max(y,hit.point.y);
                if(float.IsNegativeInfinity(y)) throw new InvalidOperationException("Sin suelo continuo en "+new Vector3(x,expected,z));
                return y;
            }
            void Walk(Vector3 a,Vector3 b,float width)
            {
                var d=b-a; d.y=0; var side=Vector3.Cross(Vector3.up,d.normalized);
                int n=Mathf.Max(1,Mathf.CeilToInt(d.magnitude/.2f));
                for(int lane=0;lane<4;lane++) for(int i=0;i<=n;i++)
                {
                    var p=Vector3.Lerp(a,b,i/(float)n)+side*((lane-1.5f)*width/4);
                    float y=Surface(p.x,p.z,p.y);
                    int count=physics.OverlapCapsule(new Vector3(p.x,y+.65f,p.z),new Vector3(p.x,y+1.65f,p.z),.3f,hits,~0,QueryTriggerInteraction.Ignore);
                    if(count>0) throw new InvalidOperationException("Paso bloqueado en "+p+" / "+hits[0].name);
                }
            }
            foreach(var piece in plan.Pieces)
            {
                var original=PrefabUtility.GetCorrespondingObjectFromSource(piece.Object);
                if(!original||(piece.Object.transform.localScale-original.transform.localScale).sqrMagnitude>.0001f)
                    throw new InvalidOperationException("Prefab o escala original alterados.");
                foreach(var t in piece.Object.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new InvalidOperationException("Script faltante: "+t.name);
                foreach(var r in piece.Object.GetComponentsInChildren<MeshRenderer>())
                    if(r.sharedMaterials.Any(m=>!m||!m.shader)) throw new InvalidOperationException("Material faltante: "+r.name);
                if(piece.Key=="Column")
                {
                    var b=piece.Bounds;
                    if(!plan.Pieces.Any(other=>other!=piece && (other.Key=="Base"||other.Key=="Column") &&
                        Mathf.Abs(other.Bounds.min.y-b.max.y)<.04f && b.center.x>=other.Bounds.min.x && b.center.x<=other.Bounds.max.x &&
                        b.center.z>=other.Bounds.min.z && b.center.z<=other.Bounds.max.z))
                        throw new InvalidOperationException("Soporte sin contacto superior: "+piece.Object.name);
                }
            }
            Walk(new Vector3(18,0,-132),new Vector3(31.5f,0,-132),2.4f);
            Walk(new Vector3(31.5f,0,-132),new Vector3(31.5f,0,-144),2.4f);
            Walk(new Vector3(31.5f,0,-144),new Vector3(30,0,-150),8);
            Walk(new Vector3(30,0,-150),new Vector3(30,-2,-184),20);
            Walk(new Vector3(30,-2,-184),new Vector3(30,-2,-227),12);
            // The actual collider surface must have eight continuous, low risers, never jumps/gaps.
            float previous=Surface(30,-152,0); int drops=0;
            for(float z=-152.1f;z>=-184;z-=.1f)
            {
                float y=Surface(30,z,Mathf.Lerp(0,-2,Mathf.InverseLerp(-152,-184,z)));
                if(y<previous-.1f) { if(previous-y>.26f) throw new InvalidOperationException("Peldaño demasiado alto."); drops++; }
                if(y>previous+.05f) throw new InvalidOperationException("Escalinata con subida inesperada.");
                previous=y;
            }
            if(drops!=8 || Mathf.Abs(previous+2)>.02f) throw new InvalidOperationException("Se esperaban ocho peldanos y un descenso de 2m.");
            if(plan.Root.GetComponentsInChildren<Light>().Any(l=>l.enabled)||plan.Root.GetComponentsInChildren<ParticleSystem>().Any(p=>p.isPlaying))
                throw new InvalidOperationException("No se permiten luces ni efectos.");
            plan.Source.Verify();
            Debug.Log("SALIDA_SUR_VALIDACION_OK: continuidad original-plataforma-escalinata-puente comprobada con cuatro carriles; ocho risers de 0.25m; acceso por hueco original sin retirar barandas. Siete arquitecturas y nueve caminos intactos. Validacion geometrica, no gameplay/NavMesh.");
        }

        static Bounds TransformBounds(Bounds b,Matrix4x4 matrix)
        {
            var result=new Bounds(matrix.MultiplyPoint3x4(b.center),Vector3.zero);
            for(int i=0;i<8;i++) result.Encapsulate(matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,
                new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
            return result;
        }
        static void DisableImportedBehaviour(GameObject root)
        {
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true);
            var fallback=renderers.SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m);
            foreach(var r in renderers)
            {
                if(r.name.EndsWith("_Shadow",StringComparison.OrdinalIgnoreCase)) r.enabled=false;
                if(fallback&&r.sharedMaterials.Any(m=>!m)) r.sharedMaterials=r.sharedMaterials.Select(m=>m?m:fallback).ToArray();
                if(fallback&&r.sharedMaterials.Length==0) r.sharedMaterials=new[]{fallback};
            }
            foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true)) if(b) b.enabled=false;
            foreach(var a in root.GetComponentsInChildren<AudioSource>(true)) a.enabled=false;
            foreach(var l in root.GetComponentsInChildren<Light>(true)) l.enabled=false;
            foreach(var c in root.GetComponentsInChildren<Camera>(true)) c.enabled=false;
            foreach(var p in root.GetComponentsInChildren<ParticleSystem>(true)) { var m=p.main; m.playOnAwake=false; p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
            foreach(var b in root.GetComponentsInChildren<Rigidbody>(true)) { b.isKinematic=true; b.useGravity=false; }
        }
        sealed class ExitPreview : PreviewSceneStage
        {
            public Source Source; public Dictionary<string,Asset> Assets; public Plan Plan;
            protected override GUIContent CreateHeaderContent() => new GUIContent("Salida meridional - revision temporal");
            protected override bool OnOpenStage()
            {
                if(Source==null||Assets==null||!base.OnOpenStage()) return false;
                try
                {
                    Scene temporary=scene; temporary.name="TEMP_CiudadDelSol_Salida_Meridional";
                    var context=new GameObject("Contexto original - solo copias"); SceneManager.MoveGameObjectToScene(context,scene); context.SetActive(false);
                    void Copy(Transform original)
                    {
                        var copy=Object.Instantiate(original.gameObject,context.transform); copy.name=original.name;
                        copy.transform.SetPositionAndRotation(original.position,original.rotation); copy.transform.localScale=original.lossyScale;
                    }
                    Copy(Source.Route); Copy(Source.Root.Find("01 - Entrada Portico Solar")); Copy(Source.Root.Find("06 - Santuario Profano"));
                    var landmarks=Source.Root.Find("Arquitectura monumental - fuera de los pasos");
                    if(landmarks) foreach(Transform t in landmarks) if(t.position.z< -80) Copy(t);
                    DisableImportedBehaviour(context); context.SetActive(true);
                    Plan=Build(Source,context.transform,scene,Assets); ValidatePlan(Plan);
                    EditorApplication.delayCall+=() =>
                    {
                        if(!this||StageUtility.GetCurrentStage()!=this) return;
                        var view=SceneView.lastActiveSceneView;
                        if(view) { view.sceneLighting=false; view.sceneViewState.showImageEffects=false; view.orthographic=false;
                            view.LookAtDirect(new Vector3(30,-1,-169),Quaternion.Euler(48,-25,0),82); view.Repaint(); }
                        Selection.activeGameObject=null;
                    };
                    Debug.Log("SALIDA_SUR_PREVIEW: "+Plan.Pieces.Count+" prefabs originales; plataforma 24x12m, 8 peldanos anchos, puente inferior 18x36m a Y=-2. Entrada existente conservada. No se integro ni guardo la escena original.");
                    return true;
                }
                catch(Exception e) { Debug.LogException(e); base.OnCloseStage(); return false; }
            }
        }
    }
}
