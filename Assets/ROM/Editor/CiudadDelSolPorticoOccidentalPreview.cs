using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    public static class CiudadDelSolPorticoOccidentalPreview
    {
        const string Target = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Menu = "ROM/Dungeon Ciudad del Sol/Portico occidental 18 m/";
        const string Wing = "Subsuelo_Ala_Occidental_Etapa03";
        const string StoneBeam = "Assets/Standard Assets/Eternal Temple/Prefabs/Bases/Ceiling/Ceiling_Beam_01.prefab";
        const float X = -78, Floor = -16;

        [MenuItem(Menu + "1. Abrir prueba temporal")]
        public static void Open()
        {
            if (StageUtility.GetCurrentStage() is PortalStage existing) { existing.Frame(); return; }
            var source = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || source.path != Target ||
                StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
                throw new InvalidOperationException("Abrir Dungeon_Isla_del_Sol en el escenario principal fuera de Play.");
            var stage = ScriptableObject.CreateInstance<PortalStage>();
            stage.Source = source;
            StageUtility.GoToStage(stage, true);
        }

        [MenuItem(Menu + "2. Encuadrar vano completo")]
        public static void Frame() { if (StageUtility.GetCurrentStage() is PortalStage stage) stage.Frame(); }

        [MenuItem(Menu + "3. Vista a altura del Warrior")]
        public static void PlayerView() { if (StageUtility.GetCurrentStage() is PortalStage stage) stage.Frame(true); }

        [MenuItem(Menu + "9. Cerrar sin guardar")]
        public static void Close() { if (StageUtility.GetCurrentStage() is PortalStage) StageUtility.GoToMainStage(); }

        public static void ValidateInBatch()
        {
            var source = EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);
            var preview = EditorSceneManager.NewPreviewScene();
            Mesh mesh = null;
            try { Build(source, preview, ref mesh); }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                if (mesh) Object.DestroyImmediate(mesh);
            }
        }

        sealed class PortalStage : PreviewSceneStage
        {
            public Scene Source;
            Mesh mesh;
            bool ready;
            protected override GUIContent CreateHeaderContent() => new GUIContent("Pórtico occidental · copia temporal");

            protected override bool OnOpenStage()
            {
                if (!base.OnOpenStage()) return false;
                try
                {
                    Build(Source, scene, ref mesh);
                    ready = true;
                    EditorApplication.delayCall += () => { if (this && StageUtility.GetCurrentStage() == this) Frame(); };
                    return true;
                }
                catch (Exception error)
                {
                    Debug.LogException(error);
                    return false;
                }
            }

            protected override void OnCloseStage()
            {
                base.OnCloseStage();
                if (mesh) Object.DestroyImmediate(mesh);
            }

            public void Frame(bool player = false)
            {
                if (!ready) return;
                var view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
                view.sceneLighting = false;
                if (player)
                    view.LookAt(new Vector3(-79, Floor+1.90f, -15),
                        Quaternion.LookRotation(new Vector3(1,0,.26f)), 10f, false, true);
                else
                    view.LookAt(new Vector3(-77, -12.10f, -15.5f),
                        Quaternion.LookRotation(new Vector3(1,-.12f,.38f)), 13.5f, true, true);
            }
        }

        static string Hash()
        {
            using var sha = SHA256.Create();
            using var file = File.OpenRead(Target);
            return BitConverter.ToString(sha.ComputeHash(file));
        }

        static void Check(bool pass, string message)
        {
            if (!pass) throw new InvalidOperationException(message);
        }

        static Bounds RenderBounds(GameObject obj) =>
            obj.GetComponentsInChildren<MeshRenderer>().Select(r => r.bounds)
                .Aggregate((a, b) => { a.Encapsulate(b); return a; });

        static void Build(Scene source, Scene preview, ref Mesh mesh)
        {
            Check(!source.isDirty, "La escena original tiene cambios sin guardar.");
            string hash = Hash();
            var root = source.GetRootGameObjects().Single(g => g.name == Wing);
            var holder = new GameObject("Contexto occidental temporal");
            SceneManager.MoveGameObjectToScene(holder, preview);
            holder.SetActive(false);
            var copy = Object.Instantiate(root, holder.transform);
            copy.name = root.name;
            foreach (var behaviour in copy.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
            holder.SetActive(true);

            var old = Find(237590366, new Vector3(X, -12.019f, -18));
            var end = Find(1714551338, new Vector3(X, -12.019f, -6));
            var originalEnd = RenderBounds(end.gameObject);
            var beams = copy.GetComponentsInChildren<BoxCollider>()
                .Where(c => c.enabled && c.name.StartsWith("Ceiling_Beam_01", StringComparison.Ordinal) &&
                    Mathf.Abs(c.bounds.center.x - X) < .04f &&
                    c.bounds.center.z > -24 && c.bounds.center.z < -6)
                .OrderBy(c => c.bounds.center.z).ToArray();
            var vaults = copy.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.enabled && r.name.StartsWith("Base_Ceiling_3m_01", StringComparison.Ordinal)).ToArray();
            Check(beams.Length == 3, "Se esperaban tres tramos de viga en el vano.");
            float top = beams[0].bounds.min.y, bottom = top - 1.20f;
            Check(beams.All(b => Mathf.Abs(b.bounds.min.y - top) < .015f), "Los tramos tienen cotas diferentes.");
            Check(Mathf.Abs(RenderBounds(old.gameObject).max.y - top) < .04f, "El encuentro original no coincide con la viga.");
            int vaultSeats = beams.Count(b => vaults.Any(r =>
                Mathf.Abs(r.bounds.min.y - b.bounds.max.y) < .06f &&
                Mathf.Min(r.bounds.max.z, b.bounds.max.z) - Mathf.Max(r.bounds.min.z, b.bounds.min.z) > 1 &&
                Mathf.Min(r.bounds.max.x, b.bounds.max.x) - Mathf.Max(r.bounds.min.x, b.bounds.min.x) > .1f));
            Check(vaultSeats == 3, "No se confirmaron los tres encuentros existentes viga-boveda.");

            var portal = new GameObject("Portico 18 m - solo prueba");
            SceneManager.MoveGameObjectToScene(portal, preview);
            var model = new StoneMesh();
            const float beamStart = -24.80f, beamEnd = -6.80f, legOffset = 1.30f;
            model.Profile(X, (beamStart + beamEnd) / 2, 18,
                (bottom,.96f), (bottom+.06f,1.08f), (bottom+.13f,1.08f),
                (bottom+.18f,.92f), (bottom+.24f,1.04f), (bottom+.29f,1.04f),
                (bottom+.32f,.90f), (top-.30f,.90f), (top-.24f,1.06f),
                (top-.18f,1.06f), (top-.12f,1.20f), (top,1.20f));
            // A shallow segmented relief reads as stonework without lowering the soffit.
            model.Frieze(X, -23.60f, -8.05f, bottom+.34f);
            foreach (float z in new[] {-24.19f,-7.40f})
                model.Profile(X,z,1.16f,
                    (bottom+.01f,1.12f),(bottom+.09f,1.18f),(bottom+.32f,1.12f),
                    (top-.31f,1.10f),(top-.20f,1.18f),(top-.13f,1.18f));
            AddBox(X, bottom, top, beamStart, beamEnd, 1.20f);
            Pillar(X, -24, 1.55f, 1.55f, bottom);
            const float seatZ = -7.25f, seatDepth = .70f;
            model.Profile(X, seatZ, seatDepth,
                (bottom-.48f,3.22f), (bottom-.35f,3.42f), (bottom-.11f,3.42f), (bottom,3.42f));
            AddBox(X, bottom-.48f, bottom, seatZ-seatDepth/2, seatZ+seatDepth/2, 3.42f);
            foreach (float side in new[] {-1f, 1f})
                Pillar(X+side*legOffset, -6, .80f, 2.46f, bottom-.48f);

            mesh = model.Create();
            portal.AddComponent<MeshFilter>().sharedMesh = mesh;
            portal.AddComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<GameObject>(StoneBeam).GetComponentInChildren<MeshRenderer>().sharedMaterial;
            var boxes = portal.GetComponents<BoxCollider>();
            Check(boxes.All(c => !c.bounds.Intersects(originalEnd)), "El portico invade el capitel conservado.");
            float Overlap(float aMin, float aMax, float bMin, float bMax) =>
                Mathf.Min(aMax,bMax)-Mathf.Max(aMin,bMin);
            for (int i = 1; i < boxes.Length; i++)
            {
                var lower = boxes[i].bounds;
                var upper = boxes[i >= 3 ? 2 : 0].bounds;
                Check(Mathf.Abs(lower.max.y-upper.min.y)<.01f &&
                    Overlap(lower.min.x,lower.max.x,upper.min.x,upper.max.x)>.12f &&
                    Overlap(lower.min.z,lower.max.z,upper.min.z,upper.max.z)>.12f,
                    "Falta asiento en la cadena de apoyo del portico.");
            }

            old.gameObject.SetActive(false);
            Physics.SyncTransforms();
            int contacts = 0;
            foreach (float z in new[] {-18f,-12f})
                foreach (float dz in new[] {-.18f,.18f})
                    foreach (float dx in new[] {-.24f,0,.24f})
                    {
                        var p = new Vector3(X+dx,top,z+dz);
                        Check(beams.Any(b => b.Raycast(new Ray(p-Vector3.up*.06f,Vector3.up), out var hit,.09f)
                            && Mathf.Abs(hit.point.y-top)<.025f) &&
                            boxes[0].Raycast(new Ray(p+Vector3.up*.06f,Vector3.down),out var seat,.09f) &&
                            Mathf.Abs(seat.point.y-top)<.025f, "Junta sin contacto de asiento en " + p);
                        contacts++;
                    }

            var physics = preview.GetPhysicsScene();
            var overlaps = new Collider[64];
            foreach (var box in boxes)
            {
                var b = box.bounds;
                int count = physics.OverlapBox(b.center, b.extents-Vector3.one*.025f, overlaps,
                    Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                var clashes = overlaps.Take(count).Where(c => c && c.gameObject != portal &&
                    c.bounds.max.y > Floor+.04f).Select(c => c.name).Distinct().ToArray();
                Check(count < overlaps.Length && clashes.Length == 0, "Interferencia con " + string.Join(", ", clashes));
                if (Mathf.Abs(b.min.y-Floor) > .02f) continue;
                foreach (float dx in new[] {-.7f,0,.7f})
                    foreach (float dz in new[] {-.7f,0,.7f})
                        Check(physics.Raycast(new Vector3(b.center.x+dx*b.extents.x,Floor-.025f,b.center.z+dz*b.extents.z)
                            + Vector3.up*.065f, Vector3.down,out var ground,.12f,~0,QueryTriggerInteraction.Ignore)
                            && ground.collider.gameObject != portal && Mathf.Abs(ground.point.y-Floor)<.035f,
                            "Piso irregular bajo un apoyo.");
            }
            // Four parallel Warrior-width lanes crossing the former column axis.
            float minHeadroom = float.PositiveInfinity;
            int samples = 0;
            foreach (float z in new[] {-17.85f,-16.95f,-16.05f,-15.15f})
                for (int i=0;i<=32;i++)
                {
                    var p = new Vector3(-82+i*.25f,Floor,z);
                    Check(physics.Raycast(p+Vector3.up*.20f,Vector3.down,out var ground,.35f,~0,QueryTriggerInteraction.Ignore)
                        && ground.normal.y>.9f && Mathf.Abs(ground.point.y-Floor)<.07f, "Piso discontinuo: " + p);
                    int count = physics.OverlapCapsule(ground.point+Vector3.up*.36f,ground.point+Vector3.up*2.13f,
                        .32f,overlaps,~0,QueryTriggerInteraction.Ignore);
                    Check(count==0,"Carril obstruido en " + p);
                    if (physics.Raycast(ground.point+Vector3.up*.10f,Vector3.up,out var ceiling,15,~0,QueryTriggerInteraction.Ignore))
                        minHeadroom=Mathf.Min(minHeadroom,ceiling.point.y-ground.point.y);
                    samples++;
                }
            Check(minHeadroom>=2.45f && !float.IsInfinity(minHeadroom),"Altura libre insuficiente o sin medida.");
            Check(minHeadroom>=6.77f && Mathf.Abs(mesh.bounds.min.y-Floor)<.02f &&
                mesh.bounds.max.y<=top+.002f,"El nuevo perfil invade la altura o la cubierta.");
            Check(!source.isDirty && Hash()==hash,"La escena original cambio.");
            // Section the copied foreground for both views. Keep all colliders and the far context.
            if (!Application.isBatchMode)
                foreach (var renderer in copy.GetComponentsInChildren<Renderer>())
                    if (renderer.bounds.max.x < -83)
                        SceneVisibilityManager.instance.Hide(renderer.gameObject, false);
            Debug.Log($"PORTICO_OCCIDENTAL_OK: dintel={boxes[0].bounds.size.z:F2} m; juntas={contacts}; " +
                $"viga-boveda={vaultSeats}; muestras={samples}; altura libre minima={minHeadroom:F2} m; " +
                $"triangulos={mesh.GetIndexCount(0)/3}; renderizadores=1; colliders={boxes.Length}; original intacta.");

            Transform Find(ulong instance, Vector3 at)
            {
                Check(GlobalObjectId.TryParse("GlobalObjectId_V1-2-e45696efca275a7419811d0359501b3d-159204-"+instance,out var id),
                    "Identidad invalida.");
                var original = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                Check(original && original.scene==source && original.activeInHierarchy &&
                    Vector3.Distance(original.transform.position,at)<.015f,"Identidad o posicion original distinta.");
                return copy.GetComponentsInChildren<Transform>().Single(t => t.name=="Column_Ceiling_02" &&
                    Vector3.Distance(t.position,original.transform.position)<.01f);
            }
            void AddBox(float x,float low,float high,float z0,float z1,float width)
            {
                var collider = portal.AddComponent<BoxCollider>();
                collider.center = new Vector3(x,(low+high)/2,(z0+z1)/2);
                collider.size = new Vector3(width,high-low,z1-z0);
            }
            void Pillar(float x,float z,float width,float depth,float head)
            {
                model.Profile(x,z,depth,(Floor,width),(Floor+.23f,width),
                    (Floor+.40f,width*.82f),(Floor+.56f,width*.77f),
                    (head-.53f,width*.77f),(head-.32f,width*.90f),(head-.20f,width),(head,width));
                AddBox(x,Floor,head,z-depth/2,z+depth/2,width);
            }
        }

        sealed class StoneMesh
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> indices = new List<int>();

            public void Profile(float x,float z,float depth,params (float Y,float Width)[] rings)
            {
                Vector3[] Ring((float Y,float Width) r) => new[]
                {
                    new Vector3(x-r.Width/2,r.Y,z-depth/2), new Vector3(x-r.Width/2,r.Y,z+depth/2),
                    new Vector3(x+r.Width/2,r.Y,z+depth/2), new Vector3(x+r.Width/2,r.Y,z-depth/2)
                };
                var previous=Ring(rings[0]);
                Quad(previous[3],previous[2],previous[1],previous[0]);
                foreach(var ring in rings.Skip(1))
                {
                    var next=Ring(ring);
                    for(int i=0;i<4;i++) Quad(previous[(i+1)%4],next[(i+1)%4],next[i],previous[i]);
                    previous=next;
                }
                Quad(previous[0],previous[1],previous[2],previous[3]);
            }

            public void Frieze(float x,float start,float end,float baseY)
            {
                const int stones = 13;
                float rise(float t) => .21f * 4f * t * (1f-t);
                for (int i=0;i<stones;i++)
                {
                    float t0=(i+.025f)/stones, t1=(i+.975f)/stones;
                    float z0=Mathf.Lerp(start,end,t0), z1=Mathf.Lerp(start,end,t1);
                    float y0=baseY+rise(t0), y1=baseY+rise(t1);
                    foreach (float sign in new[] {-1f,1f})
                    {
                        Vector3 P(float y,float z,float depth) => new Vector3(x+sign*depth,y,z);
                        var a=P(y0,z0,.445f);
                        var b=P(y0+.035f,z0+.055f,.566f);
                        var c=P(y0+.123f,z0+.055f,.566f);
                        var d=P(y0+.158f,z0,.445f);
                        var e=P(y1,z1,.445f);
                        var f=P(y1+.035f,z1-.055f,.566f);
                        var g=P(y1+.123f,z1-.055f,.566f);
                        var h=P(y1+.158f,z1,.445f);
                        void Face(Vector3 p,Vector3 q,Vector3 r,Vector3 s)
                        {
                            if(sign>0) Quad(p,q,r,s); else Quad(s,r,q,p);
                        }
                        Face(b,c,g,f);
                        Face(a,b,f,e);
                        Face(c,d,h,g);
                        Face(a,d,c,b);
                        Face(f,g,h,e);
                    }
                }
            }

            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                int offset=vertices.Count;
                vertices.AddRange(new[] {a,b,c,d});
                var u=(d-a).normalized;
                var v=(b-a).normalized;
                foreach(var point in new[] {a,b,c,d})
                    uv.Add(new Vector2(Vector3.Dot(point-a,u),Vector3.Dot(point-a,v))*.5f);
                indices.AddRange(new[] {offset,offset+1,offset+2,offset,offset+2,offset+3});
            }

            public Mesh Create()
            {
                var mesh=new Mesh { name="Dintel monumental y apoyos - malla temporal", hideFlags=HideFlags.DontSave };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0,uv);
                mesh.SetTriangles(indices,0);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                return mesh;
            }
        }
    }
}
