using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    /// <summary>
    /// Manual progressive integration. Integrate() never generates the base or copies previews.
    /// A separate guarded bootstrap command supports the user's explicit one-time authorization.
    /// Register future stages here only after their architecture has been approved.
    /// </summary>
    public static class CiudadDelSolApprovedStagesIntegrator
    {
        const string Target = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string RootName = "Dungeon_Ciudad_del_Sol_Generated";
        const string RootSignature = "__ROM_SolarStructure_12D9D67A_v1";
        const string Menu = "ROM/Dungeon Ciudad del Sol/Integracion/";

        sealed class ApprovedStage
        {
            public readonly string Zone, Name, Signature;
            public readonly Action Generate, Validate;
            public ApprovedStage(string zone,string name,string signature,Action generate,Action validate)
            { Zone=zone; Name=name; Signature=signature; Generate=generate; Validate=validate; }
        }

        // Existing architectures are preserved, including their manual edits. No regeneration here.
        static readonly ApprovedStage[] Approved =
        {
            new ApprovedStage("02 - Jardines Dorados","JardinesDorados_Architecture","__ROM_GoldenGardens_46F0C18E_v1",
                JardinesDoradosArchitectureGenerator.Generate,JardinesDoradosArchitectureGenerator.Validate),
            new ApprovedStage("03 - Plaza del Eclipse","PlazaDelEclipse_Architecture","__ROM_EclipseArchitecture_719A50BC_v1",
                PlazaDelEclipseArchitectureGenerator.Generate,PlazaDelEclipseArchitectureGenerator.Validate)
        };

        static readonly string[] ZoneNames =
        {
            "01 - Entrada Portico Solar","02 - Jardines Dorados","03 - Plaza del Eclipse",
            "04 - Archivos Solares","05 - Claustro del Eclipse","06 - Santuario Profano","07 - El Sol Corrompido"
        };
        static readonly Vector3[] Centers =
        {
            new Vector3(-126,0,-120),new Vector3(-102,4,-18),new Vector3(18,12,12),new Vector3(-108,20,108),
            new Vector3(18,28,144),new Vector3(150,4,-78),new Vector3(180,36,156)
        };
        static readonly string[] Routes =
        { "01-02 -","02-03 -","03-04 -","03-05 -","04-05 -","03-06 -","05-07 -","06-07 -","01-06 -" };

        [MenuItem(Menu + "3. Activar original ya cargada (sin abrir ni guardar) %#&F9")]
        public static void ActivateLoadedOriginal()
        {
            if (!CanRun()) return;
            if (StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())
            {
                Debug.LogError("Volve al escenario principal antes de activar la original.");
                return;
            }
            Scene scene=SceneManager.GetSceneByPath(Target);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("La original no esta cargada. No se abrira ni cerrara ninguna escena.");
                return;
            }
            if (!SceneManager.SetActiveScene(scene)) Debug.LogError("No se pudo activar la original.");
            else Debug.Log("Escena original activada; no se modificaron objetos ni se guardaron escenas.");
        }

        [MenuItem(Menu + "4. Crear base original SOLO si esta ausente (sin guardar) %#&F8")]
        public static void CreateAbsentOriginalBase()
        {
            if (!CanRun()) return;
            int group=-1;
            try
            {
                Scene scene=RequireTarget();
                if (scene.GetRootGameObjects().Any(g=>g.name==RootName))
                    throw new InvalidOperationException("La base ya existe. No se regenerara ni se reconstruira.");
                var before=new Snapshot(scene,null,Array.Empty<ApprovedStage>(),true);
                using (var log=new LogCapture())
                {
                    Undo.IncrementCurrentGroup(); group=Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Crear base ausente en original - autorizacion explicita");
                    SunDungeonStructureGenerator.Generate();
                    log.ThrowIfErrors("creacion inicial autorizada");
                    RequireBase(scene);
                    before.Verify();
                }
                Undo.CollapseUndoOperations(group);
                Debug.Log("Base creada en la original por autorizacion explicita. No se copio TEMP_* ni se guardo. Integrar arquitecturas antes de guardar.");
            }
            catch (Exception e)
            {
                if (group>=0) Undo.RevertAllDownToGroup(group);
                Debug.LogError("Creacion inicial detenida sin guardar: "+e.Message);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem(Menu + "1. Diagnosticar original (sin cambios) %#&F7")]
        public static void Diagnose()
        {
            if (!CanRun()) return;
            try
            {
                Scene scene=RequireTarget();
                Transform root=RequireBase(scene);
                foreach (var stage in Approved)
                    Debug.Log(stage.Zone+": "+(FindStage(root,stage) ? "arquitectura presente; se conservara":"arquitectura pendiente"));
                using (var log=new LogCapture())
                {
                    foreach (var stage in Approved) stage.Validate();
                    log.ThrowIfErrors("validacion previa");
                }
                CheckAccesses(root,false);
                Debug.Log("Diagnostico completado: escena original activa, 7 zonas y 9 rutas. No se modifico ni guardo nada.");
            }
            catch (Exception e) { Debug.LogError("Diagnostico Ciudad del Sol: "+e.Message); }
        }

        [MenuItem(Menu + "2. Integrar solo etapas faltantes y guardar tras validar %#&F10")]
        public static void Integrate()
        {
            if (!CanRun()) return;
            Scene scene=default;
            Snapshot before=null;
            int undoGroup=-1;
            bool saved=false;
            var added=new List<string>();
            var warnings=new List<string>();
            try
            {
                scene=RequireTarget();
                Transform root=RequireBase(scene); // NO fallback to base generation, even when absent.
                var missing=Approved.Where(stage=>!FindStage(root,stage)).ToArray();
                before=new Snapshot(scene,root,missing);
                using (var log=new LogCapture())
                {
                    // Generator methods catch their own exceptions: capture their errors explicitly.
                    foreach (var stage in Approved) stage.Validate();
                    log.ThrowIfErrors("validacion previa; no se genero nada");
                    Undo.IncrementCurrentGroup();
                    undoGroup=Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Integrar etapas aprobadas Ciudad del Sol");
                    foreach (var stage in missing)
                    {
                        stage.Generate();
                        log.ThrowIfErrors("generacion de "+stage.Name);
                        if (!FindStage(root,stage))
                            throw new InvalidOperationException(stage.Name+" no se creo; el generador pudo cancelarse. No se guardara.");
                        added.Add(stage.Name);
                    }
                    before.Verify();
                    RequireBase(scene);
                    foreach (var stage in Approved)
                    {
                        Transform instance=FindStage(root,stage);
                        if (!instance) throw new InvalidOperationException("Falta la etapa aprobada "+stage.Name);
                        CheckArchitecture(instance,warnings);
                        stage.Validate();
                    }
                    log.ThrowIfErrors("validacion final");
                    CheckAccesses(root,true);
                    before.Verify();
                    warnings.AddRange(log.Warnings);
                }
                Undo.CollapseUndoOperations(undoGroup);
                // This is the only save in this tool. Every structural/access check must have passed.
                if (scene.path!=Target || StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())
                    throw new InvalidOperationException("Cambio el contexto de edicion; no se guardara.");
                if (scene.isDirty && !EditorSceneManager.SaveScene(scene,Target,false))
                    throw new InvalidOperationException("Unity no pudo guardar la escena original.");
                saved=true;
                Debug.Log("Integracion verificada. Agregadas: "+(added.Count==0?"ninguna; ya estaban presentes":string.Join(", ",added))+
                    ". Arquitecturas existentes conservadas. Siete zonas, nueve rutas y objetos previos intactos. Escena original guardada/verificada.",root);
                foreach (string warning in warnings.Distinct()) Debug.LogWarning("Integracion Ciudad del Sol: "+warning);
            }
            catch (Exception e)
            {
                // Only this operation's new stages participate in its undo group; earlier work is preserved.
                if (!saved && undoGroup>=0)
                {
                    try { Undo.RevertAllDownToGroup(undoGroup); before?.Verify(); }
                    catch (Exception rollback) { Debug.LogError("Revisar Undo de integracion: "+rollback.Message); }
                }
                Debug.LogError("Integracion detenida. No se guardo la escena. "+e.Message);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        static bool CanRun() => !EditorApplication.isPlayingOrWillChangePlaymode &&
                                !EditorApplication.isCompiling && !EditorApplication.isUpdating;

        static Scene RequireTarget()
        {
            if (StageUtility.GetCurrentStage()!=StageUtility.GetMainStage())
                throw new InvalidOperationException("Hay una previsualizacion o prefab abierto. Volve al escenario principal sin copiar su contenido.");
            Scene scene=SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path!=Target || EditorSceneManager.IsPreviewScene(scene))
                throw new InvalidOperationException("La escena activa debe ser exactamente "+Target+". Esta herramienta no abre escenas.");
            return scene;
        }

        static Transform RequireBase(Scene scene)
        {
            var matches=scene.GetRootGameObjects().Where(g=>g.name==RootName).ToArray();
            if (matches.Length==0)
                throw new InvalidOperationException("La escena ORIGINAL no contiene "+RootName+
                    ". Una raiz en TEMP_* no cuenta. No se copiara la previsualizacion ni se ejecutara el generador base.");
            if (matches.Length!=1) throw new InvalidOperationException("Hay varias raices del dungeon. No se elegira ni eliminara ninguna.");
            Transform root=matches[0].transform;
            if (!root.Find(RootSignature) || !root.gameObject.activeInHierarchy ||
                root.position!=Vector3.zero || root.rotation!=Quaternion.identity || root.lossyScale!=Vector3.one)
                throw new InvalidOperationException("La estructura no coincide con la firma, posicion o escala originales.");
            for (int i=0;i<ZoneNames.Length;i++)
            {
                var zones=root.Cast<Transform>().Where(t=>t.name==ZoneNames[i]).ToArray();
                if (zones.Length!=1 || Vector3.Distance(zones[0].position,Centers[i])>.01f ||
                    Quaternion.Angle(zones[0].rotation,Quaternion.identity)>.01f ||
                    Vector3.Distance(zones[0].lossyScale,Vector3.one)>.001f)
                    throw new InvalidOperationException("Zona ausente, duplicada o transformada: "+ZoneNames[i]);
            }
            foreach (string prefix in Routes)
                if (root.Cast<Transform>().Count(t=>t.name.StartsWith(prefix,StringComparison.Ordinal))!=1)
                    throw new InvalidOperationException("Recorrido ausente o duplicado: "+prefix);
            return root;
        }

        static Transform FindStage(Transform root,ApprovedStage stage)
        {
            Transform zone=root.Find(stage.Zone);
            var stages=zone.Cast<Transform>().Where(t=>t.name==stage.Name).ToArray();
            if (stages.Length>1) throw new InvalidOperationException("Etapa duplicada: "+stage.Name+". No se borrara ninguna copia.");
            if (stages.Length==0) return null;
            if (!stages[0].Find(stage.Signature))
                throw new InvalidOperationException(stage.Name+" ya existe sin firma reconocida. Se conserva y se detiene la integracion.");
            return stages[0];
        }

        static void CheckArchitecture(Transform stage,List<string> warnings)
        {
            if (!stage.gameObject.activeInHierarchy || stage.localPosition!=Vector3.zero ||
                stage.localRotation!=Quaternion.identity || stage.localScale!=Vector3.one)
                throw new InvalidOperationException("La etapa esta desactivada o desplazada: "+stage.name);
            if (stage.GetComponentsInChildren<MeshRenderer>(true).Length==0)
                throw new InvalidOperationException("Etapa vacia: "+stage.name);
            foreach (var t in stage.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)
                    throw new InvalidOperationException("Script faltante en "+stage.name+"/"+t.name);
                if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                {
                    var prefab=PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                    if (!prefab || Vector3.Distance(t.localScale,prefab.transform.localScale)>.001f)
                        throw new InvalidOperationException("Prefab perdido o escala alterada: "+t.name);
                }
                foreach (var behaviour in t.GetComponents<MonoBehaviour>())
                    if (behaviour && behaviour.enabled)
                        throw new InvalidOperationException("Comportamiento activo inesperado en la arquitectura: "+t.name);
                var filter=t.GetComponent<MeshFilter>();
                if (filter && !filter.sharedMesh) throw new InvalidOperationException("Malla faltante: "+t.name);
                foreach (var r in t.GetComponents<MeshRenderer>())
                    foreach (var material in r.sharedMaterials)
                    {
                        if (!material || !material.shader || material.shader.name=="Hidden/InternalErrorShader")
                            throw new InvalidOperationException("Material o shader faltante: "+t.name);
                        if (!material.shader.isSupported) warnings.Add("Shader no soportado en "+t.name+": "+material.shader.name);
                    }
                foreach (var collider in t.GetComponents<MeshCollider>())
                    if (collider.enabled && !collider.sharedMesh)
                        throw new InvalidOperationException("MeshCollider sin malla: "+t.name);
            }
        }

        // Read-only conservative checks of ACTUAL enabled stage colliders, not a gameplay simulation.
        static void CheckAccesses(Transform root,bool requireBoth)
        {
            // Generators move newly instantiated colliders within the same menu invocation.
            // With autoSyncTransforms off, bounds can still describe their prefab-origin pose.
            Physics.SyncTransforms();
            var garden=FindStage(root,Approved[0]);
            var plaza=FindStage(root,Approved[1]);
            if (requireBoth && (!garden || !plaza)) throw new InvalidOperationException("Faltan arquitecturas aprobadas.");
            if (garden)
            {
                var center=Centers[1];
                float[,] segments={ {-12,-48,-12,-24},{-18,-24,24,-24},{24,-24,24,30},
                    {24,30,48,30},{-18,-24,-18,6},{-18,21,24,21} };
                foreach (var c in garden.GetComponentsInChildren<Collider>())
                {
                    if (!c.enabled || c.isTrigger) continue;
                    Bounds b=c.bounds;
                    for (int i=0;i<segments.GetLength(0);i++)
                    {
                        Vector3 a=center+new Vector3(segments[i,0],0,segments[i,1]);
                        Vector3 z=center+new Vector3(segments[i,2],0,segments[i,3]);
                        if (InAvenue(b,a,z,6,center.y))
                            throw new InvalidOperationException("Posible bloqueo de sendero de Jardines: "+c.name);
                    }
                    var landing=new Bounds(center+new Vector3(39,2.5f,17.3f),new Vector3(12,4.8f,9.4f));
                    if (landing.Intersects(b)) throw new InvalidOperationException("Posible bloqueo del descanso oriental: "+c.name);
                }
            }
            if (plaza)
            {
                var center=Centers[2];
                Vector3[] ports={ new Vector3(-60,0,0),new Vector3(-54,0,30),new Vector3(-30,0,48),new Vector3(60,0,0) };
                foreach (var c in plaza.GetComponentsInChildren<Collider>())
                {
                    if (!c.enabled || c.isTrigger) continue;
                    Bounds b=c.bounds;
                    for (int i=0;i<ports.Length;i++)
                    {
                        var direction=ports[i].normalized;
                        if (InAvenue(b,center+ports[i]+direction*6,center+direction*28,i==2?12:6,center.y))
                            throw new InvalidOperationException("Posible bloqueo de acceso a la Plaza: "+c.name);
                    }
                    if (b.max.y<=center.y+.03f || b.min.y>=center.y+4.5f) continue;
                    Vector2 nearest=new Vector2(Mathf.Clamp(center.x,b.min.x,b.max.x)-center.x,
                        Mathf.Clamp(center.z,b.min.z,b.max.z)-center.z);
                    float farthest=0;
                    foreach (float x in new[] {b.min.x,b.max.x}) foreach (float z in new[] {b.min.z,b.max.z})
                        farthest=Mathf.Max(farthest,Vector2.Distance(new Vector2(x,z),new Vector2(center.x,center.z)));
                    if (nearest.magnitude<36 && farthest>24)
                        throw new InvalidOperationException("Posible bloqueo del anillo de la Plaza: "+c.name+
                            " bounds="+b+" radio minimo="+nearest.magnitude+" maximo="+farthest);
                }
            }
        }

        static bool InAvenue(Bounds box,Vector3 from,Vector3 to,float halfWidth,float floor)
        {
            if (box.max.y<=floor+.03f || box.min.y>=floor+4.5f) return false;
            Vector2 a=new Vector2(from.x,from.z),b=new Vector2(to.x,to.z),axis=(b-a).normalized;
            Vector2 side=new Vector2(-axis.y,axis.x),delta=new Vector2(box.center.x,box.center.z)-(a+b)*.5f;
            Vector2 ext=new Vector2(box.extents.x,box.extents.z);
            float length=Vector2.Distance(a,b)*.5f;
            return Mathf.Abs(delta.x)<=ext.x+Mathf.Abs(axis.x)*length+Mathf.Abs(side.x)*halfWidth &&
                Mathf.Abs(delta.y)<=ext.y+Mathf.Abs(axis.y)*length+Mathf.Abs(side.y)*halfWidth &&
                Mathf.Abs(Vector2.Dot(delta,axis))<=length+Mathf.Abs(axis.x)*ext.x+Mathf.Abs(axis.y)*ext.y &&
                Mathf.Abs(Vector2.Dot(delta,side))<=halfWidth+Mathf.Abs(side.x)*ext.x+Mathf.Abs(side.y)*ext.y;
        }

        sealed class LogCapture : IDisposable
        {
            readonly List<string> errors=new List<string>();
            public readonly List<string> Warnings=new List<string>();
            public LogCapture() { Application.logMessageReceived+=OnLog; }
            void OnLog(string condition,string stack,LogType type)
            {
                if (type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(condition);
                else if (type==LogType.Warning || condition.Contains("piezas opcionales omitidas") || condition.Contains("piezas perimetrales omitidas"))
                    Warnings.Add(condition);
            }
            public void ThrowIfErrors(string phase)
            {
                if (errors.Count>0) throw new InvalidOperationException(phase+": "+string.Join(" | ",errors.Distinct()));
            }
            public void Dispose() { Application.logMessageReceived-=OnLog; }
        }

        sealed class Snapshot
        {
            readonly Scene scene;
            readonly int[] roots;
            readonly bool allowNewBase;
            readonly List<ObjectState> objects=new List<ObjectState>();
            public Snapshot(Scene scene,Transform dungeon,ApprovedStage[] missing,bool allowNewBase=false)
            {
                this.scene=scene;
                this.allowNewBase=allowNewBase;
                roots=scene.GetRootGameObjects().Select(g=>g.GetInstanceID()).ToArray();
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        var allowed=missing.Where(s=>dungeon.Find(s.Zone)==t).Select(s=>s.Name).ToArray();
                        objects.Add(new ObjectState(t,allowed));
                    }
            }
            public void Verify()
            {
                var currentRoots=scene.GetRootGameObjects();
                var existing=currentRoots.Where(g=>roots.Contains(g.GetInstanceID())).Select(g=>g.GetInstanceID());
                var extras=currentRoots.Where(g=>!roots.Contains(g.GetInstanceID())).ToArray();
                if (!roots.SequenceEqual(existing) || (!allowNewBase && extras.Length!=0) ||
                    (allowNewBase && extras.Any(g=>g.name!=RootName || !g.transform.Find(RootSignature))) ||
                    (allowNewBase && extras.Length>1))
                    throw new InvalidOperationException("Cambio la lista de objetos raiz de la escena.");
                foreach (var state in objects) state.Verify();
            }
        }

        sealed class ObjectState
        {
            readonly Transform target,parent;
            readonly Vector3 position,scale;
            readonly Quaternion rotation;
            readonly int sibling;
            readonly int[] children,components;
            readonly string[] allowed;
            readonly string name,tag;
            readonly bool active;
            readonly int layer;
            readonly StaticEditorFlags flags;
            readonly Dictionary<Component,Hash128> serialized=new Dictionary<Component,Hash128>();
            public ObjectState(Transform t,string[] allowed)
            {
                target=t; parent=t.parent; position=t.localPosition; rotation=t.localRotation; scale=t.localScale;
                sibling=t.GetSiblingIndex(); children=t.Cast<Transform>().Select(c=>c.GetInstanceID()).ToArray();
                this.allowed=allowed; name=t.name; tag=t.tag; active=t.gameObject.activeSelf; layer=t.gameObject.layer;
                flags=GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                var all=t.GetComponents<Component>();
                components=all.Select(c=>c?c.GetInstanceID():0).ToArray();
                foreach (var c in all) if (c && !(c is Transform)) serialized.Add(c,Hash128.Compute(EditorJsonUtility.ToJson(c)));
            }
            public void Verify()
            {
                if (!target || target.parent!=parent || target.localPosition!=position || target.localRotation!=rotation ||
                    target.localScale!=scale || target.GetSiblingIndex()!=sibling || target.name!=name || target.tag!=tag ||
                    target.gameObject.activeSelf!=active || target.gameObject.layer!=layer ||
                    GameObjectUtility.GetStaticEditorFlags(target.gameObject)!=flags)
                    throw new InvalidOperationException("Se altero un objeto preexistente: "+name);
                var now=target.Cast<Transform>().ToArray();
                if (!children.SequenceEqual(now.Where(t=>children.Contains(t.GetInstanceID())).Select(t=>t.GetInstanceID())))
                    throw new InvalidOperationException("Se eliminaron o reordenaron hijos preexistentes: "+name);
                if (now.Any(t=>!children.Contains(t.GetInstanceID()) && !allowed.Contains(t.name)))
                    throw new InvalidOperationException("Se agregaron hijos fuera de las etapas autorizadas: "+name);
                if (!components.SequenceEqual(target.GetComponents<Component>().Select(c=>c?c.GetInstanceID():0)))
                    throw new InvalidOperationException("Cambio la lista de componentes de "+name);
                foreach (var pair in serialized)
                    if (!pair.Key || Hash128.Compute(EditorJsonUtility.ToJson(pair.Key))!=pair.Value)
                        throw new InvalidOperationException("Cambio un componente, material o collider preexistente: "+name);
            }
        }
    }
}
