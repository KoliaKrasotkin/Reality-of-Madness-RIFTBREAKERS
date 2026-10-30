using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ROM.EditorTools
{
    // Disposable composition only. No integration, scene save, asset save or gameplay entry point.
    public static class CiudadDelSolNucleoConjuntoPreviewGenerator
    {
        const string Menu="ROM/Dungeon Ciudad del Sol/Nucleo monumental conjunto/";
        const string Target="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        static object Read(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
        static void Set(object o,string n,object value)=>o.GetType().GetField(n,Flags).SetValue(o,value);
        static object Call(object o,string n,params object[] args){try{return (o as Type??o.GetType()).GetMethod(n,Flags).Invoke(o is Type?null:o,args);}catch(TargetInvocationException e){throw e.InnerException??e;}}
        static object Guard(Scene source){var t=typeof(CiudadDelSolCentroOrientalPreviewGenerator).GetNestedType("Guard",Flags);return Activator.CreateInstance(t,Flags,null,new object[]{source},null);}
        static bool Root(GameObject g)=>g.name=="Dungeon_Ciudad_del_Sol_Generated"||g.name.StartsWith("Subsuelo_",StringComparison.Ordinal)||g.name=="Umbral_De_Las_Profundidades"||g.name=="Salida_Meridional_Generated";
        [MenuItem(Menu+"1. Crear copia conjunta temporal")]
        public static void Preview()
        {
            try
            {
                if(StageUtility.GetCurrentStage() is JointStage j&&j.Ready){j.View(0);return;}
                if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Cerrar la previsualizacion anterior sin guardar desde su menu9.");
                if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Esperar compilacion; fuera de Play.");
                var s=SceneManager.GetActiveScene();if(s.path!=Target)throw new InvalidOperationException("Abrir la escena original Dungeon_Isla_del_Sol.");
                var p=ScriptableObject.CreateInstance<JointStage>();p.Source=s;p.Original=Guard(s);StageUtility.GoToStage(p,true);
            }
            catch(Exception e){Debug.LogException(e);}
        }
        [MenuItem(Menu+"2. Vista general seccionada")] public static void General(){if(StageUtility.GetCurrentStage() is JointStage s&&s.Ready)s.View(0);}
        [MenuItem(Menu+"3. Vista union Z30 Y-34")] public static void Join(){if(StageUtility.GetCurrentStage() is JointStage s&&s.Ready)s.View(1);}
        [MenuItem(Menu+"4. Vista acceso superior V3")] public static void Upper(){if(StageUtility.GetCurrentStage() is JointStage s&&s.Ready)s.View(2);}
        [MenuItem(Menu+"5. Vista fondo V3")] public static void Bottom(){if(StageUtility.GetCurrentStage() is JointStage s&&s.Ready)s.View(3);}
        [MenuItem(Menu+"6. Validar copia conjunta")] public static void Validate(){try{if(StageUtility.GetCurrentStage() is JointStage s&&s.Ready)s.ValidateJoint();}catch(Exception e){Debug.LogException(e);}}
        [MenuItem(Menu+"7. Comprobar ruta superior sin generar")] public static void CheckUpper(){try{if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())throw new InvalidOperationException("Cerrar primero la copia temporal");var scene=SceneManager.GetActiveScene();if(scene.path!=Target)throw new InvalidOperationException("Escena original requerida");var probe=ScriptableObject.CreateInstance<JointStage>();try{probe.CheckUpper(scene);}finally{Object.DestroyImmediate(probe);}}catch(Exception e){Debug.LogException(e);}}
        [MenuItem(Menu+"9. Cerrar sin guardar")] public static void Close(){if(StageUtility.GetCurrentStage() is JointStage)StageUtility.GoToMainStage();}

        sealed class JointStage:PreviewSceneStage
        {
            public Scene Source;public object Original;public bool Ready;
            Transform context,generated;
            readonly List<ScriptableObject> builders=new List<ScriptableObject>();
            readonly Dictionary<GameObject,GameObject> identities=new Dictionary<GameObject,GameObject>();
            readonly Dictionary<GameObject,string> initial=new Dictionary<GameObject,string>();
            readonly HashSet<GameObject> approvedFloors=new HashSet<GameObject>();
            readonly List<GameObject> portal=new List<GameObject>();
            readonly List<GameObject> duplicateRails=new List<GameObject>();
            readonly List<GameObject> originalChanged=new List<GameObject>();
            readonly List<string> manifest=new List<string>();
            int samples;
            protected override GUIContent CreateHeaderContent()=>new GUIContent("NUCLEO CONJUNTO - COPIA TEMPORAL - NO INTEGRAR");
            static Bounds BoundsOf(GameObject g){var r=g.GetComponentsInChildren<Renderer>(true).Where(x=>x.enabled).ToArray();if(r.Length==0)throw new InvalidOperationException("Sin malla: "+g.name);var b=r[0].bounds;foreach(var x in r.Skip(1))b.Encapsulate(x.bounds);return b;}
            static string Snapshot(GameObject g)=>g.activeSelf+"|"+g.transform.position.ToString("F5")+g.transform.rotation.ToString("F5")+g.transform.localScale.ToString("F5")+"|"+string.Join(";",g.GetComponents<MeshFilter>().Select(f=>f.sharedMesh?f.sharedMesh.GetInstanceID():0))+"|"+string.Join(";",g.GetComponents<Renderer>().Select(r=>r.enabled+":"+string.Join(",",r.sharedMaterials.Select(m=>m?m.GetInstanceID():0))))+"|"+string.Join(";",g.GetComponents<Collider>().Select(c=>c.GetType().Name+":"+c.enabled+":"+c.isTrigger+":"+(c is MeshCollider mc&&mc.sharedMesh?mc.sharedMesh.GetInstanceID():0)));
            void Map(Transform original,Transform copy){identities.Add(copy.gameObject,original.gameObject);if(original.childCount!=copy.childCount)throw new InvalidOperationException("Copia no corresponde por identidad");for(int i=0;i<original.childCount;i++)Map(original.GetChild(i),copy.GetChild(i));}
            protected override bool OnOpenStage()
            {
                if(!base.OnOpenStage())return false;
                try
                {
                    var preview=scene;preview.name="TEMP_CiudadDelSol_NucleoConjunto";
                    foreach(var type in new[]{typeof(CiudadDelSolCentroOrientalPreviewGenerator),typeof(CiudadDelSolArticulacionSurestePreviewGenerator)})Call(type,"Audit",Source,Call(type,"Load"));
                    Call(typeof(CiudadDelSolSolInvertidoPreviewGenerator),"Audit",Source);
                    var h=new GameObject("Contexto_UNICO_por_identidad");SceneManager.MoveGameObjectToScene(h,scene);context=h.transform;h.SetActive(false);
                    foreach(var original in Source.GetRootGameObjects().Where(Root)){var c=Object.Instantiate(original,context);c.name=original.name;c.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);c.transform.localScale=original.transform.lossyScale;Map(original.transform,c.transform);}
                    Call(typeof(CiudadDelSolCentroOrientalPreviewGenerator),"Quiet",h);h.SetActive(true);Physics.SyncTransforms();
                    foreach(var copy in identities.Keys)initial.Add(copy,Snapshot(copy));
                    var root=new GameObject("Nucleo_Monumental_Conjunto_TEMPORAL");SceneManager.MoveGameObjectToScene(root,scene);generated=root.transform;
                    // Freeze the authorised floor identities before any generator reparents them.
                    foreach(var t in new[]{typeof(CiudadDelSolCentroOrientalPreviewGenerator),typeof(CiudadDelSolArticulacionSurestePreviewGenerator)})foreach(GameObject f in (IEnumerable)Call(t,"Floors",context.Cast<Transform>().Select(x=>x.gameObject)))if(!(bool)Call(t,"Gallery",f))if(!approvedFloors.Add(f))throw new InvalidOperationException("Piso solicitado por dos sectores: "+identities[f].GetInstanceID());
                    if(approvedFloors.Count!=194)throw new InvalidOperationException("Se esperaban194 pisos trasladables distintos.");
                    var sol=Builder(typeof(CiudadDelSolSolInvertidoPreviewGenerator),"SolPreview","03 - El Sol Invertido V3");
                    Call(sol,"OpenCopies");Call(sol,"Build");Physics.SyncTransforms();Call(sol,"ValidateGeometry");Call(sol,"MakeOpenVariant");Call(sol,"MakeThirdVariant");
                    var co=Builder(typeof(CiudadDelSolCentroOrientalPreviewGenerator),"Sector","01 - Centro Oriental aprobado");Call(co,"Build",Call(typeof(CiudadDelSolCentroOrientalPreviewGenerator),"Floors",context.Cast<Transform>().Select(x=>x.gameObject)));
                    var se=Builder(typeof(CiudadDelSolArticulacionSurestePreviewGenerator),"Sector","02 - Sureste arcadas aprobadas");Set(se,"Alternative",true);Call(se,"Build",Call(typeof(CiudadDelSolArticulacionSurestePreviewGenerator),"Floors",context.Cast<Transform>().Select(x=>x.gameObject)));Call(se,"BuildArcades");
                    // Closed-sector validation before applying the only approved border surgery.
                    Seal(co);Seal(se);Physics.SyncTransforms();Call(co,"Validate");Call(se,"Validate");
                    foreach(var b in new[]{co,se})
                    {
                        var r=(Transform)Read(b,"generated");var z=b==co?29.6f:30.4f;
                        foreach(Transform group in r)if(group.name.StartsWith("06 -",StringComparison.Ordinal))foreach(Transform piece in group)
                        {
                            if(!piece.name.StartsWith("Base_Wall_02",StringComparison.Ordinal))continue;var box=BoundsOf(piece.gameObject);
                            if(Mathf.Abs(box.center.z-z)<.03f&&(Mathf.Abs(box.center.x-93)<.02f||Mathf.Abs(box.center.x-99)<.02f))portal.Add(piece.gameObject);
                        }
                    }
                    if(portal.Count!=12||portal.Any(identities.ContainsKey))throw new InvalidOperationException("No son exactamente12 muros temporales autorizados.");
                    foreach(var wall in portal)wall.SetActive(false);
                    DeduplicateApprovedRails(co,se);
                    DeduplicateComposition();
                    AuditIdentities();ValidateJoint();Ready=true;EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this){Selection.activeGameObject=null;View(1);}};
                    Debug.Log("NUCLEO_CONJUNTO_LISTO: un contexto; CO aprobado, Sureste arcadas y V3 corregida.12 muros temporales y"+duplicateRails.Count+" duplicados exactos desactivados. No guardar ni integrar.");
                }
                catch(Exception e){Debug.LogException(e);Debug.LogError("NUCLEO_CONJUNTO_NO_VALIDADO: copia descartada; no integrar. Original protegido.");EditorApplication.delayCall+=()=>{if(this&&StageUtility.GetCurrentStage()==this)StageUtility.GoToMainStage();};}
                return true;
            }
            ScriptableObject Builder(Type outer,string nested,string label)
            {
                var b=ScriptableObject.CreateInstance(outer.GetNestedType(nested,Flags));builders.Add(b);Set(b,"JointScene",scene);Set(b,"Source",Source);Set(b,"Assets",Call(outer,"Load"));
                var guard=Activator.CreateInstance(outer.GetNestedType("Guard",Flags),Flags,null,new object[]{Source},null);Set(b,"Original",guard);Set(b,"context",context);
                var r=new GameObject(label);r.transform.SetParent(generated,false);Set(b,"generated",r.transform);return b;
            }
            void Seal(object b){Set(b,"preserved",Call(b,"Signature"));}
            void DeduplicateApprovedRails(object co,object se)
            {
                var a=(Transform)Read(co,"generated");var b=(Transform)Read(se,"generated");
                foreach(float x in new[]{81f,87f,93f,99f,105f,111f,129f})
                {
                    Func<Transform,GameObject[]> find=root=>root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Railing_01"&&Mathf.Abs(BoundsOf(t.gameObject).center.x-x)<.02f&&Mathf.Abs(BoundsOf(t.gameObject).center.z-29.45f)<.02f&&Mathf.Abs(BoundsOf(t.gameObject).max.y-(-14.75f))<.02f).Select(t=>t.gameObject).ToArray();
                    var left=find(a);var right=find(b);
                    if(left.Length!=1||right.Length!=1)throw new InvalidOperationException("PAREJA_BARANDA_NO_IDENTIFICADA: X="+x);
                    var keep=left[0];var omit=right[0];
                    if(identities.ContainsKey(keep)||identities.ContainsKey(omit)||ExactPiece(keep)!=ExactPiece(omit))throw new InvalidOperationException("BARANDAS_NO_COINCIDENTES: X="+x);
                    var km=keep.GetComponentsInChildren<MeshFilter>(true);var om=omit.GetComponentsInChildren<MeshFilter>(true);
                    if(km.Length==0||km.Length!=om.Length)throw new InvalidOperationException("BARANDAS_MALLAS_DISTINTAS: X="+x);
                    for(int i=0;i<km.Length;i++)if(km[i].sharedMesh!=om[i].sharedMesh||km[i].transform.localToWorldMatrix!=om[i].transform.localToWorldMatrix)throw new InvalidOperationException("BARANDAS_GEOMETRIA_DISTINTA: X="+x);
                    duplicateRails.Add(omit);
                }
                foreach(var rail in duplicateRails)rail.SetActive(false);
                Debug.Log("NUCLEO_BARANDAS_VERIFICADAS:7 parejas coincidentes por prefab, malla compartida, matriz mundial, materiales y colliders; conservar CO, omitir Sureste solo en copia conjunta. X81/87/93/99/105/111/129 Z29.45; nivel de galeria Y-16.");
            }
            static string ExactPiece(GameObject g)
            {
                string V(Vector3 v)=>v.ToString("R");
                string M(Matrix4x4 m)=>string.Join(",",Enumerable.Range(0,16).Select(i=>m[i].ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
                var path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g);
                if(string.IsNullOrEmpty(path))return "NO_PREFAB|"+g.GetInstanceID();
                var parts=new List<string>{path};
                foreach(var t in g.GetComponentsInChildren<Transform>(true))
                {
                    parts.Add(AnimationUtility.CalculateTransformPath(t,g.transform)+"|"+t.gameObject.activeSelf+"|"+t.gameObject.layer+"|"+t.gameObject.tag+"|"+M(t.localToWorldMatrix));
                    foreach(var c in t.GetComponents<Component>())
                    {
                        if(c is Transform)continue;
                        if(c is MeshFilter mf)parts.Add("Mesh|"+(mf.sharedMesh?mf.sharedMesh.GetInstanceID():0));
                        else if(c is Renderer r)parts.Add("Renderer|"+r.GetType()+"|"+r.enabled+"|"+r.shadowCastingMode+"|"+r.receiveShadows+"|"+string.Join(",",r.sharedMaterials.Select(m=>m?m.GetInstanceID():0)));
                        else if(c is Collider col)
                        {
                            var s="Collider|"+col.GetType()+"|"+col.enabled+"|"+col.isTrigger+"|"+col.contactOffset.ToString("R")+"|"+(col.sharedMaterial?col.sharedMaterial.GetInstanceID():0);
                            if(col is MeshCollider mc)s+="|"+(mc.sharedMesh?mc.sharedMesh.GetInstanceID():0)+"|"+mc.convex+"|"+mc.cookingOptions;
                            else if(col is BoxCollider bc)s+="|"+V(bc.center)+"|"+V(bc.size);
                            else if(col is CapsuleCollider cc)s+="|"+V(cc.center)+"|"+cc.radius.ToString("R")+"|"+cc.height.ToString("R")+"|"+cc.direction;
                            else if(col is SphereCollider sc)s+="|"+V(sc.center)+"|"+sc.radius.ToString("R");
                            else s+="|NO_COMPARABLE|"+col.GetInstanceID();
                            parts.Add(s);
                        }
                        else if(!(c is LODGroup))parts.Add("NO_COMPARABLE|"+(c?c.GetInstanceID():0));
                    }
                }
                return string.Join("\n",parts);
            }
            void DeduplicateComposition()
            {
                var seen=new Dictionary<string,(GameObject go,object owner)>();
                foreach(var b in builders)foreach(var p in (IEnumerable)Read(b,"pieces"))
                {
                    var g=(GameObject)Read(p,"Go");if(!g||!g.activeInHierarchy||identities.ContainsKey(g))continue;
                    var key=ExactPiece(g);
                    if(seen.TryGetValue(key,out var prior)&&!ReferenceEquals(prior.owner,b))
                    {
                        if(g.GetComponentsInChildren<Transform>(true).Any(t=>identities.ContainsKey(t.gameObject)))throw new InvalidOperationException("DUPLICADO_CON_ORIGINAL_PROTEGIDO: "+g.name);
                        duplicateRails.Add(g);g.SetActive(false);
                        Debug.Log("NUCLEO_DUPLICADO_EXACTO_OMITIDO: "+AnimationUtility.CalculateTransformPath(g.transform,generated)+" / "+g.transform.position+"; conservar="+AnimationUtility.CalculateTransformPath(prior.go.transform,generated));
                    }
                    else seen[key]=(g,b);
                }
                Debug.Log("NUCLEO_REVISION_GLOBAL_DUPLICADOS: todos los prefabs activos de los tres sectores examinados; omitidos="+duplicateRails.Count+"; originales protegidos excluidos; proximidad sola no autoriza omision.");
            }
            void AuditIdentities()
            {
                originalChanged.Clear();int moved=0,opened=0;
                foreach(var entry in initial)
                {
                    var g=entry.Key;if(!g)throw new InvalidOperationException("Objeto original copiado destruido.");if(Snapshot(g)==entry.Value)continue;
                    var source=identities[g];var p=source.transform.position;
                    bool floor=g.name.StartsWith("Platform_Base_01",StringComparison.Ordinal);
                    bool inner=p.x>=-18.03f&&p.x<=54.03f&&p.z>=-18.03f&&p.z<=54.03f;
                    if(approvedFloors.Contains(g)){if(!g.activeSelf||Mathf.Abs(BoundsOf(g).max.y+34)>.03f&&Mathf.Abs(BoundsOf(g).max.y+30)>.03f)throw new InvalidOperationException("Piso trasladado fuera de cotas aprobadas");moved++;}
                    else if(floor&&inner&&!g.activeSelf)opened++;
                    else if(g.name.StartsWith("Column_Ceiling_02",StringComparison.Ordinal)&&inner&&source.transform.root.name.StartsWith("Subsuelo_",StringComparison.Ordinal)){}
                    else throw new InvalidOperationException("CAMBIO_FUERA_DE_IDENTIDADES_APROBADAS: "+source.name+" "+p);
                    originalChanged.Add(g);
                }
                if(moved!=194||opened!=144)throw new InvalidOperationException("Identidades no coinciden: "+moved+" trasladados / "+opened+" abiertos.");
                Call(Original,"Verify");
            }
            static Vector3[] UpperRoute()=>new[]{new Vector3(156,-16,99),new Vector3(180,-16,99),new Vector3(180,-16,63),new Vector3(135,-16,63),new Vector3(135,-16,105),new Vector3(120,-16,105),new Vector3(120,-16,12),new Vector3(63,-16,12),new Vector3(63,-16,-15),new Vector3(57,-16,-15),new Vector3(45,-16,-15)};
            public void CheckUpper(Scene source){Physics.SyncTransforms();var all=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy).ToArray();var route=UpperRoute();for(int i=1;i<route.Length;i++)Segment(route[i-1],route[i],all,false,source.GetPhysicsScene());Debug.Log("NUCLEO_RUTA_SUPERIOR_OK: fuente solo lectura; abertura existente, rodeo Z63, G04/G03 y llegada V3 a Y-16; "+samples+" muestras. Sin geometria ni cambios de escena.");}
            void Segment(Vector3 a,Vector3 b,Collider[] colliders,bool stair=false,PhysicsScene? query=null)
            {
                var side=Vector3.Cross(Vector3.up,b-a).normalized;var physics=query??scene.GetPhysicsScene();var hits=new Collider[256];var allowed=new HashSet<Collider>(colliders);int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.4f);var near=colliders.Select(c=>(c,b:c.bounds)).ToArray();
                for(int i=0;i<=count;i++)foreach(float lane in new[]{-1.2f,-.4f,.4f,1.2f})
                {
                    var p=Vector3.Lerp(a,b,i/(float)count)+side*lane;var ray=new Ray(p+Vector3.up*1.15f,Vector3.down);var ground=new List<RaycastHit>();
                    foreach(var t in near)if(t.b.min.x<=p.x&&t.b.max.x>=p.x&&t.b.min.z<=p.z&&t.b.max.z>=p.z&&t.c.Raycast(ray,out var h,2.4f)&&h.normal.y>.5f)ground.Add(h);
                    if(ground.Count==0)throw new InvalidOperationException("CONJUNTO_SIN_SUELO: "+p);var foot=ground.OrderBy(h=>Mathf.Abs(h.point.y-p.y)).First().point;if(Mathf.Abs(foot.y-p.y)>(stair?.55f:.08f))throw new InvalidOperationException("CONJUNTO_DESNIVEL: "+p+" suelo="+foot);
                    int n=physics.OverlapCapsule(foot+Vector3.up*.69f,foot+Vector3.up*1.68f,.32f,hits,~0,QueryTriggerInteraction.Ignore);if(n==hits.Length)throw new InvalidOperationException("Consulta saturada");for(int k=0;k<n;k++)if(allowed.Contains(hits[k]))throw new InvalidOperationException("CONJUNTO_BLOQUEADO: "+foot+" / "+hits[k].name+" / "+hits[k].transform.position);
                    foreach(var t in near)if(t.b.min.x<=foot.x&&t.b.max.x>=foot.x&&t.b.min.z<=foot.z&&t.b.max.z>=foot.z&&t.c.Raycast(new Ray(foot+Vector3.up*.69f,Vector3.up),out var head,50)&&head.point.y-foot.y<2.1f)throw new InvalidOperationException("CONJUNTO_ALTURA_LIBRE: "+foot+" / "+t.c.name);
                    samples++;
                }
            }
            public void ValidateJoint()
            {
                Physics.SyncTransforms();AuditIdentities();samples=0;
                if(portal.Count!=12||portal.Any(w=>w.activeSelf))throw new InvalidOperationException("Paso Z30 no corresponde al plan autorizado.");
                // Preserve all original individual checks, including the closed retaining perimeter;
                // only the twelve approved preview walls are restored briefly for those checks.
                if(duplicateRails.Count<7||duplicateRails.Any(r=>r.activeSelf))throw new InvalidOperationException("Duplicados conjuntos fuera del ajuste aprobado.");
                try{foreach(var w in portal.Concat(duplicateRails))w.SetActive(true);Physics.SyncTransforms();foreach(var b in builders)Call(b,b.GetType().Name=="SolPreview"?"ValidateGeometry":"Validate");}
                finally{foreach(var w in portal.Concat(duplicateRails))w.SetActive(false);Physics.SyncTransforms();}
                var all=context.GetComponentsInChildren<Collider>().Concat(generated.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy).ToArray();
                foreach(var b in builders)foreach(var w in (IEnumerable)Read(b,"walks"))Segment((Vector3)Read(w,"A"),(Vector3)Read(w,"B"),all,(bool)Read(w,"Stair"));
                var routes=new[]{new[]{new Vector3(96,-34,27),new Vector3(96,-34,33),new Vector3(99,-34,39)},UpperRoute()};
                foreach(var route in routes)for(int i=1;i<route.Length;i++)Segment(route[i-1],route[i],all);
                // Include the complete original access, using its established sampled segments.
                var access=new[]{new Vector3(162,4,7),new Vector3(162,4,33.45f),new Vector3(150,4,33.45f),new Vector3(150,4,36)};
                for(int i=1;i<access.Length;i++)Segment(access[i-1],access[i],all);
                for(int i=0;i<5;i++){var a=new Vector3(150,4-i*4,36+i*12);var z=a+new Vector3(0,-4,6);Segment(a,z,all,true);if(i<4)Segment(z,z+Vector3.forward*6,all);}
                Segment(new Vector3(150,-16,90),new Vector3(150,-16,93),all);Segment(new Vector3(150,-16,93),new Vector3(156,-16,93),all);Segment(new Vector3(156,-16,93),new Vector3(156,-16,99),all);
                var seen=new Dictionary<string,MeshFilter>();int triangles=0;foreach(var f in context.GetComponentsInChildren<MeshFilter>().Concat(generated.GetComponentsInChildren<MeshFilter>()))
                {
                    if(!f.gameObject.activeInHierarchy||!f.sharedMesh)continue;var key=f.sharedMesh.GetInstanceID()+"|"+f.transform.localToWorldMatrix.ToString("F4");
                    if(seen.TryGetValue(key,out var old)&&(!identities.ContainsKey(f.gameObject)||!identities.ContainsKey(old.gameObject)))throw new InvalidOperationException("MALLA_DUPLICADA_NUEVA: "+AnimationUtility.CalculateTransformPath(f.transform,null)+" / "+AnimationUtility.CalculateTransformPath(old.transform,null)+" / "+f.transform.position);seen[key]=f;
                    if(f.transform.IsChildOf(generated))for(int n=0;n<f.sharedMesh.subMeshCount;n++)triangles+=(int)f.sharedMesh.GetIndexCount(n)/3;
                }
                if(context.childCount!=Source.GetRootGameObjects().Count(Root))throw new InvalidOperationException("Contexto duplicado o incompleto");
                var line="NUCLEO_CONJUNTO_VALIDACION_OK: contexto unico;194 pisos trasladados;144 abiertos V3;12 muros temporales desactivados;CO+Sureste arcadas+V3 corregida; muestras="+samples+"; renderers activos sectores="+generated.GetComponentsInChildren<Renderer>().Length+"; colliders activos sectores="+generated.GetComponentsInChildren<Collider>().Length+"; triangulos sectores="+triangles+". Acceso Y4/-16, G04/G03 y recorrido hasta V3 Y-38 comprobados estaticamente. Personaje real pendiente. L-E y contencion profunda V3 intactos. Sin guardar/integrar.";
                manifest.Clear();manifest.Add(line);foreach(var g in originalChanged){var o=identities[g];manifest.Add("ORIGINAL_COPIADO|"+GlobalObjectId.GetGlobalObjectIdSlow(o)+"|"+AnimationUtility.CalculateTransformPath(o.transform,null)+"|"+o.transform.position.ToString("F4")+" -> "+g.transform.position.ToString("F4")+"|activo="+g.activeSelf);}
                foreach(var w in portal)manifest.Add("MURO_TEMPORAL_RETIRADO|"+AnimationUtility.CalculateTransformPath(w.transform,generated)+"|"+w.transform.position.ToString("F4"));
                foreach(var r in duplicateRails)manifest.Add("BARANDA_DUPLICADA_OMITIDA|"+AnimationUtility.CalculateTransformPath(r.transform,generated)+"|"+r.transform.position.ToString("F4"));
                foreach(var f in generated.GetComponentsInChildren<MeshFilter>())if(f.gameObject.activeInHierarchy)manifest.Add("ACTIVO|"+(identities.ContainsKey(f.gameObject)?"REUTILIZADO":"NUEVO")+"|"+AnimationUtility.CalculateTransformPath(f.transform,generated)+"|"+f.transform.position.ToString("F4"));
                Directory.CreateDirectory("Temp/ROM_Reviews/NucleoConjunto_01");File.WriteAllLines("Temp/ROM_Reviews/NucleoConjunto_01/Validacion.txt",manifest);Call(Original,"Verify");Debug.Log(line);
            }
            public void View(int mode)
            {
                foreach(var r in context.GetComponentsInChildren<Renderer>())
                {
                    var b=r.bounds;bool hide=b.min.x>144||b.max.x< -36||b.min.z>114||b.max.z< -60||b.min.y> -4;
                    if(mode==0&&b.min.y> -9)hide=true;if(hide)SceneVisibilityManager.instance.Hide(r.gameObject,false);else SceneVisibilityManager.instance.Show(r.gameObject,false);
                }
                foreach(var r in generated.GetComponentsInChildren<Renderer>()){var b=r.bounds;bool cut=mode==0&&r.name.StartsWith("Base_Wall_02",StringComparison.Ordinal)&&(b.center.x>130||b.center.z< -23);if(cut)SceneVisibilityManager.instance.Hide(r.gameObject,false);else SceneVisibilityManager.instance.Show(r.gameObject,false);}
                var eye=mode==0?new Vector3(184,-9,-116):mode==1?new Vector3(96,-32.2f,21):mode==2?new Vector3(57,-14,-12):new Vector3(18,-36.2f,-12);
                var target=mode==0?new Vector3(54,-24,27):mode==1?new Vector3(99,-25,54):mode==2?new Vector3(27,-23,18):new Vector3(18,-15,18);
                var v=SceneView.lastActiveSceneView;if(!v)return;v.sceneLighting=false;v.sceneViewState.showImageEffects=false;v.cameraSettings.fieldOfView=mode==0?65:75;v.orthographic=false;var q=Quaternion.LookRotation(target-eye);v.LookAtDirect(eye+q*Vector3.forward*30,q,30);v.pivot=eye+q*Vector3.forward*v.cameraDistance;v.Repaint();
            }
            protected override void OnCloseStage(){try{if(Original!=null)Call(Original,"Verify");}finally{foreach(var b in builders)if(b)Object.DestroyImmediate(b);base.OnCloseStage();}}
        }
    }
}
