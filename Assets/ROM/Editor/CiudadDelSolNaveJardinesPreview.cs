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
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string Menu = "ROM/Dungeon Ciudad del Sol/Nave de Jardines Dorados/";
        const string Original = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Stone = "Assets/Standard Assets/Eternal Temple/Prefabs/Bases/Ceiling/Ceiling_Beam_01.prefab";
        const float Floor = -16, Spring = -7.244f, Thickness = .62f;
        const string Phase = "ROM/Dungeon Ciudad del Sol/Finalizacion subsuelo/";

        sealed class Sector
        {
            public readonly string Name, Root;
            public readonly float West, East, South, North, Crown, ProfilePower, MinimumPeak;
            public readonly float[] Ribs;
            public readonly ulong[] CapitalIds;
            public readonly bool EastBearing, TwoPiers;
            public readonly float CrownOffset, MinimumClearance;
            public readonly bool Adaptive;
            public Sector(string name,string root,float west,float east,float south,float north,float crown,
                float[] ribs,ulong[] capitalIds,bool eastBearing=false,bool twoPiers=false,
                float profilePower=2,float minimumPeak=16,float crownOffset=0,float minimumClearance=12,bool adaptive=false)
            {
                Name=name; Root=root; West=west; East=east; South=south; North=north; Crown=crown;
                Ribs=ribs; CapitalIds=capitalIds; EastBearing=eastBearing; TwoPiers=twoPiers;
                ProfilePower=profilePower; MinimumPeak=minimumPeak;
                CrownOffset=crownOffset; MinimumClearance=minimumClearance; Adaptive=adaptive;
            }
            public float Mid => (West+East)*.5f;
            public float CrownX => Mid+CrownOffset;
            public float Width => East-West;
            public float Length => North-South;
            public int Segments => Mathf.RoundToInt(Width*2);
            public float Soffit(float x)
            {
                float u=(x-CrownX)/(x<CrownX ? CrownX-West : East-CrownX);
                return Crown+(Spring-Crown)*(ProfilePower==2 ? u*u : Mathf.Pow(Mathf.Abs(u),ProfilePower));
            }
        }

        static readonly Sector Gardens = new Sector("Jardines Dorados", "Subsuelo_Ala_Occidental_Etapa03",
            -90,-72,-48,12,.65f,new[] {-42f,-18f,6f},new ulong[] {2063885918,385162568,237590366,1714551338,1643224509});
        static readonly Sector Sanctuary = new Sector("Santuario Profano", "Subsuelo_Central_Sureste_Etapa02",
            138,156,-96,-30,.65f,new[] {-84f,-60f,-36f},new ulong[] {1369524963,2101354107,62791430,756726172,1836594870});
        static readonly Sector Cloister = new Sector("Claustro del Eclipse", "Subsuelo_Ala_Norte_Etapa04",
            6,36,132,174,1.6f,new[] {144f,156f,168f},new ulong[] {1754331665,579078909,1565071413,850795091,626319685,98965296});
        static readonly Sector Sun = new Sector("El Sol Corrompido", "Subsuelo_Ala_Este_Cierre_Etapa05",
            180,198,144,180,.50f,new[] {150f,174f},new ulong[] {374710762,1491757317},true,true);
        static readonly Sector[] Sectors = { Gardens, Sanctuary, Cloister, Sun };

        [MenuItem(Phase + "1. Jardines Dorados")]
        static void OpenGardens() => Open(Gardens);
        [MenuItem(Phase + "2. Santuario Profano")]
        static void OpenSanctuary() => Open(Sanctuary);
        [MenuItem(Phase + "3. Claustro del Eclipse")]
        static void OpenCloister() => Open(Cloister);
        [MenuItem(Phase + "4. El Sol Corrompido")]
        static void OpenSun() => Open(Sun);
        [MenuItem(Phase + "5. Panoramica")]
        static void PhasePanorama() => Panorama();
        [MenuItem(Phase + "6. Vista a altura del Warrior")]
        static void PhaseWarrior() => Warrior();
        [MenuItem(Phase + "8. Panoramica con cubierta vecina")]
        static void PhaseContext() => FullContext();
        [MenuItem(Phase + "0. Vista conjunta de las cuatro salas")]
        static void OpenJoint() => Open(null);
        [MenuItem(Phase + "9. Cerrar sin guardar")]
        static void PhaseClose() => Close();

        [MenuItem(Menu + "1. Abrir prueba temporal")]
        public static void Open() => Open(Gardens);

        static void Open(Sector sector)
        {
            if (StageUtility.GetCurrentStage() is HallStage existing)
            {
                if (existing.Design == sector) { existing.Frame(false); return; }
                StageUtility.GoToMainStage();
            }
            var source = SceneManager.GetActiveScene();
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && source.path == Original &&
                StageUtility.GetCurrentStage() == StageUtility.GetMainStage(), "Abrir Dungeon_Isla_del_Sol fuera de Play.");
            var stage = ScriptableObject.CreateInstance<HallStage>();
            stage.Source = source;
            stage.Design = sector;
            StageUtility.GoToStage(stage, true);
        }

        [MenuItem(Menu + "2. Panoramica de la nave")]
        public static void Panorama() { if (StageUtility.GetCurrentStage() is HallStage stage) stage.Frame(false); }

        [MenuItem(Menu + "3. Vista a altura del Warrior")]
        public static void Warrior() { if (StageUtility.GetCurrentStage() is HallStage stage) stage.Frame(true); }

        [MenuItem(Menu + "5. Panoramica con cubierta vecina visible")]
        public static void FullContext()
        {
            if (StageUtility.GetCurrentStage() is HallStage stage) stage.Frame(false, false);
        }

        [MenuItem(Menu + "9. Cerrar sin guardar")]
        public static void Close() { if (StageUtility.GetCurrentStage() is HallStage) StageUtility.GoToMainStage(); }

        public static void ValidateInBatch()
        {
            Validate(EditorSceneManager.OpenScene(Original, OpenSceneMode.Single));
        }

        [MenuItem(Menu + "4. Validar copia temporal")]
        public static void ValidateCurrent()
        {
            var source = SceneManager.GetActiveScene();
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && source.path == Original,
                "Abrir Dungeon_Isla_del_Sol fuera de Play.");
            Validate(source);
        }

        public static void ValidatePhaseInBatch()
        {
            var source = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            ValidatePhase(source);
        }

        [MenuItem(Phase + "7. Validar los cuatro sectores")]
        static void ValidatePhaseFromMenu() => ValidatePhase(SceneManager.GetActiveScene());

        static void ValidatePhase(Scene source)
        {
            Require(source.path == Original && !EditorApplication.isPlayingOrWillChangePlaymode,
                "Abrir Dungeon_Isla_del_Sol fuera de Play.");
            var failures = new List<Exception>();
            foreach (var sector in Sectors)
                try { Validate(source,sector); }
                catch (Exception error)
                {
                    Debug.LogError("SECTOR_NO_VALIDADO " + sector.Name + ": " + error.Message);
                    failures.Add(error);
                }
            if (failures.Count>0) throw new AggregateException(failures);
        }

        public static void ValidateJointInBatch()
        {
            var source = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            var preview = EditorSceneManager.NewPreviewScene();
            var meshes = new List<Object>();
            try { BuildJoint(source, preview, meshes); }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                foreach (var mesh in meshes) Object.DestroyImmediate(mesh);
            }
        }

        static void Validate(Scene source, Sector sector = null)
        {
            var preview = EditorSceneManager.NewPreviewScene();
            var meshes = new List<Object>();
            try { Build(source, preview, meshes, sector ?? Gardens); }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                foreach (var mesh in meshes) Object.DestroyImmediate(mesh);
            }
        }

        sealed class HallStage : PreviewSceneStage
        {
            public Scene Source;
            public Sector Design;
            readonly List<Object> meshes = new List<Object>();
            GameObject context;
            MeshRenderer[] neighboringVaults;
            protected override GUIContent CreateHeaderContent() =>
                new GUIContent((Design?.Name ?? "Cuatro salas del subsuelo") + " · copia temporal");
            protected override bool OnOpenStage()
            {
                if (!base.OnOpenStage()) return false;
                try
                {
                    context = Design==null ? BuildJoint(Source, scene, meshes) : Build(Source, scene, meshes, Design);
                    neighboringVaults = context.GetComponentsInChildren<MeshRenderer>()
                        .Where(r => r.enabled && r.name.StartsWith("Base_Ceiling_3m_01", StringComparison.Ordinal)).ToArray();
                    EditorApplication.delayCall += () => { if (this && StageUtility.GetCurrentStage() == this) Frame(false); };
                    return true;
                }
                catch (Exception error) { Debug.LogException(error); return false; }
            }

            protected override void OnCloseStage()
            {
                base.OnCloseStage();
                foreach (var mesh in meshes) if (mesh) Object.DestroyImmediate(mesh);
            }

            public void Frame(bool player, bool section = true)
            {
                if (!context) return;
                if (Design==null)
                {
                    foreach (var roof in neighboringVaults) roof.enabled = !section;
                    var overview = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
                    overview.sceneLighting = false;
                    overview.LookAt(new Vector3(52,-10,40),
                        Quaternion.LookRotation(new Vector3(.24f,-1,.31f)), 225, true, true);
                    return;
                }
                foreach (var r in neighboringVaults)
                    r.enabled = !section || (player ? r.bounds.center.z >= Design.South : r.bounds.center.x >= Design.West);
                var view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
                view.sceneLighting = false;
                if (player)
                    view.LookAt(new Vector3(Design.Mid, Floor+1.90f, Design.South + Mathf.Min(23,Design.Length*.45f)), Quaternion.LookRotation(Vector3.forward), 13, false, true);
                else
                    view.LookAt(new Vector3(Design.Mid, -8, (Design.South+Design.North)*.5f), Quaternion.LookRotation(new Vector3(.85f,.04f,.48f)), Mathf.Max(24, Design.Length*.6f), true, true);
            }
        }

        static GameObject BuildJoint(Scene source, Scene preview, List<Object> meshes)
        {
            Require(Sectors.Select(s => s.Root).Distinct().Count()==Sectors.Length, "Raiz de sector repetida.");
            string hash = Hash();
            var group = new GameObject("Subsuelo completo - comparacion temporal");
            SceneManager.MoveGameObjectToScene(group, preview);
            foreach (var sector in Sectors)
                Build(source, preview, meshes, sector).transform.parent.SetParent(group.transform, true);
            group.SetActive(false);
            var copied = new HashSet<string>(Sectors.Select(s => s.Root));
            foreach (var root in source.GetRootGameObjects().Where(g => !copied.Contains(g.name) &&
                (g.name.StartsWith("Subsuelo_",StringComparison.Ordinal) || g.name=="Nucleo_Monumental_Conjunto")))
            {
                var context = Object.Instantiate(root, group.transform);
                context.name=root.name;
                foreach (var behaviour in context.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled=false;
            }
            group.SetActive(true);
            Physics.SyncTransforms();
            Require(meshes.OfType<Mesh>().Count()==8 && !source.isDirty && Hash()==hash,
                "La composicion altero la fuente o perdio mallas.");
            Debug.Log("SUBSUELO_CONJUNTO_OK: cuatro salas en una sola copia, ocho mallas, contexto subterraneo preservado; original intacta.");
            return group;
        }

        static void Require(bool pass, string reason)
        {
            if (!pass) throw new InvalidOperationException(reason);
        }

        static string Hash()
        {
            using var hash = SHA256.Create();
            using var stream = File.OpenRead(Original);
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "");
        }

        static GameObject Build(Scene source, Scene preview, List<Object> meshes, Sector design, List<MountPlan> mounts = null)
        {
            float westEdge=design.West, east=design.East, south=design.South, north=design.North;
            float Soffit(float x) => design.Soffit(x);
            bool Inside(float x,float z) => x>westEdge+.02f && x<east-.02f && z>south+.02f && z<north-.02f;
            bool Crosses(Bounds b) => b.max.x>westEdge+.02f && b.min.x<east-.02f &&
                b.max.z>south+.02f && b.min.z<north-.02f;
            Require(!source.isDirty, "La escena original tiene cambios sin guardar.");
            string hash = Hash();
            var roots = source.GetRootGameObjects();
            var west = roots.Single(g => g.name == design.Root);
            var protectedBounds = roots.Where(g => g != west)
                .SelectMany(g => g.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => (r.name,r.bounds))
                    .Concat(g.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).Select(c => (c.name,c.bounds))))
                .Where(v => Crosses(v.bounds) && v.bounds.max.y>Floor+.05f).ToArray();
            foreach (var v in protectedBounds)
            {
                float closest = Mathf.Clamp(design.CrownX, Mathf.Max(westEdge,v.bounds.min.x), Mathf.Min(east,v.bounds.max.x));
                Require(v.bounds.min.y >= Soffit(closest)+Thickness+.12f, "Interferencia superior: " + v.name + " " + v.bounds);
            }
            Require(design.Adaptive || protectedBounds.Length>0, "No se detecto la plataforma superior.");
            float minimumUpper = protectedBounds.Select(v => v.bounds.min.y).DefaultIfEmpty(float.PositiveInfinity).Min();
            float gap = protectedBounds.Select(v =>
                v.bounds.min.y-Soffit(Mathf.Clamp(design.CrownX,Mathf.Max(westEdge,v.bounds.min.x),Mathf.Min(east,v.bounds.max.x)))-Thickness)
                .DefaultIfEmpty(float.PositiveInfinity).Min();

            var holder = new GameObject("Contexto temporal - " + design.Name);
            SceneManager.MoveGameObjectToScene(holder, preview);
            holder.SetActive(false);
            var copy = Object.Instantiate(west,holder.transform);
            foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true)) if (b) b.enabled=false;
            holder.SetActive(true);
            var caps = copy.transform.Cast<Transform>().Single(t => t.name.StartsWith("04 - Columnas monumentales",StringComparison.Ordinal));
            var roof = copy.transform.Find("02 - Bovedas generales Y-4");
            var beams = copy.transform.Cast<Transform>().Single(t => t.name.StartsWith("03 - Vigas",StringComparison.Ordinal) && t.name.EndsWith("principales",StringComparison.Ordinal));
            Require(caps && roof && beams,"Jerarquia occidental distinta.");
            var removed = new HashSet<GameObject>();
            for (int i=0;i<design.CapitalIds.Length;i++)
            {
                Require(GlobalObjectId.TryParse("GlobalObjectId_V1-2-e45696efca275a7419811d0359501b3d-159204-"+design.CapitalIds[i],out var id),
                    "Identidad de capitel invalida.");
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                Require(obj && obj.scene==source && obj.activeInHierarchy && obj.transform.root==west.transform &&
                    Inside(obj.transform.position.x,obj.transform.position.z) && Mathf.Abs(obj.transform.position.y+12.019f)<.03f,
                    "Identidad o posicion de capitel no coincide.");
                var piece = caps.Cast<Transform>().Single(t => Vector3.Distance(t.position,obj.transform.position)<.02f);
                removed.Add(piece.gameObject);
            }
            var roofCuts = roof.Cast<Transform>().Where(t => Inside(t.position.x,t.position.z)).ToArray();
            var beamCuts = beams.Cast<Transform>().Where(t => Inside(t.position.x,t.position.z)).ToArray();
            int rows = Mathf.RoundToInt(design.Length/6), columns = Mathf.RoundToInt(design.Width/6);
            Require(roofCuts.Length==rows*columns && beamCuts.Length==rows*((columns-1)/2),"Cobertura modular distinta de la prevista.");
            var edgeBeamBoxes = beams.Cast<Transform>().Where(t =>
                Mathf.Abs(t.position.x-(design.EastBearing?east:westEdge))<.04f && t.position.z>south && t.position.z<north)
                .SelectMany(t => t.GetComponentsInChildren<BoxCollider>()).ToArray();
            Require(edgeBeamBoxes.Length==rows && edgeBeamBoxes.All(c => Mathf.Abs(c.bounds.max.y-Spring)<.04f),
                "La linea de vigas perimetral no recibe el arranque oeste.");
            Require(new[] {south-3,north+3}.All(z => roof.Cast<Transform>().Count(t =>
                Inside(t.position.x,(south+north)/2) && Mathf.Abs(t.position.z-z)<.04f) == columns),
                "Faltan modulos de encuentro en los extremos.");
            var guards = copy.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger &&
                Crosses(c.bounds) && c.bounds.max.y>Floor+.05f).ToArray();
            foreach (var t in roofCuts.Concat(beamCuts)) removed.Add(t.gameObject);
            bool EndCapital(Collider c)
            {
                var cap = c.transform;
                while (cap.parent && cap.parent!=caps && cap.parent!=copy.transform) cap=cap.parent;
                return cap.parent==caps && cap.name.StartsWith("Column_Ceiling_02",StringComparison.Ordinal) &&
                    (Mathf.Abs(cap.position.z-south)<.025f || Mathf.Abs(cap.position.z-north)<.025f) &&
                    c.bounds.max.y<Spring-.5f;
            }
            bool OnEdge(Bounds b) => (b.center.x<=westEdge+.05f && b.max.x<westEdge+.85f) ||
                (b.center.x>=east-.05f && b.min.x>east-.85f);
            Require(guards.All(c => removed.Any(g => c.transform==g.transform || c.transform.IsChildOf(g.transform)) ||
                OnEdge(c.bounds) || EndCapital(c)),
                "Hay un apoyo o collider adicional dentro de la nave: " +
                string.Join(", ",guards.Where(c => !removed.Any(g => c.transform.IsChildOf(g.transform)||c.gameObject==g) &&
                    !OnEdge(c.bounds) && !EndCapital(c)).Select(c => c.name + " " + c.bounds.center).Distinct()));

            var hall = new GameObject(design.Name + " - solo prueba");
            SceneManager.MoveGameObjectToScene(hall,preview);
            hall.transform.SetParent(holder.transform, true);
            var stone = AssetDatabase.LoadAssetAtPath<GameObject>(Stone).GetComponentInChildren<MeshRenderer>().sharedMaterial;
            var material = new Material(stone) { name = "Piedra opaca - copia temporal", hideFlags = HideFlags.DontSave };
            material.SetFloat("_AlphaClip",0);
            material.SetFloat("_AlphaToMask",0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType","Opaque");
            material.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Geometry;
            meshes.Add(material);
            var shell = new Masonry(design);
            shell.Barrel(south,north,0,Thickness);
            Add("Boveda longitudinal",shell);
            var detail = new Masonry(design);
            foreach (float z in design.Ribs)
            {
                detail.Barrel(z-.56f,z+.56f,-.33f,.02f);
                detail.Barrel(z-.66f,z-.56f,-.19f,.01f);
                detail.Barrel(z+.56f,z+.66f,-.19f,.01f);
                if (!design.EastBearing || design.TwoPiers) Pier(east,-1,z);
                if (design.EastBearing) Pier(westEdge,1,z);
            }
            // Keep the movement opening at its existing spring line. Set the masonry
            // back from the shared end plane to avoid coplanar faces at the junction.
            detail.End(south+.025f,south+.36f);
            detail.End(north-.36f,north-.025f);
            detail.EndTrim(south+.34f,south+.57f);
            detail.EndTrim(north-.57f,north-.34f);
            Add("Nervaduras, pilastras y transiciones",detail);
            foreach (var item in removed) item.SetActive(false);
            var endCapitals = guards.Where(EndCapital).Select(c => c.bounds).ToArray();
            var (low,peak,samples,seamSamples) = ValidateMounted(hall,design,endCapitals);
            if (mounts!=null)
            {
                var targets = removed.Select(clone =>
                {
                    var group = west.transform.Find(clone.transform.parent.name);
                    Require(group,"Falta el grupo original de " + clone.name);
                    var original = group.GetChild(clone.transform.GetSiblingIndex());
                    Require(original.name==clone.name && original.gameObject.activeInHierarchy &&
                        Vector3.Distance(original.position,clone.transform.position)<.001f &&
                        Quaternion.Angle(original.rotation,clone.transform.rotation)<.01f &&
                        Vector3.Distance(original.lossyScale,clone.transform.lossyScale)<.001f,
                        "La pieza original no coincide con la copia: " + clone.name);
                    return GlobalObjectId.GetGlobalObjectIdSlow(original.gameObject);
                }).ToArray();
                mounts.Add(new MountPlan(design,hall,targets,endCapitals));
            }
            Require(!source.isDirty && Hash()==hash,"La escena original cambio.");
            Debug.Log($"SUBSUELO_SECTOR_OK {design.Name}: {design.Width}x{design.Length} m; piso={Floor}; altura central minima={low:F2}; clave={peak:F2}; " +
                $"limite superior Y={minimumUpper:F2}; separacion minima sobre boveda={gap:F2}; muestras={samples}; juntas={seamSamples}; " +
                $"{roofCuts.Length} bovedas, {beamCuts.Length} vigas y {design.CapitalIds.Length} capiteles inactivos solo en copia; {meshes.OfType<Mesh>().Reverse().Take(2).Sum(m=>m.GetIndexCount(0)/3)} triangulos; " +
                "2 renderizadores, 2 colliders; original intacta.");
            return copy;

            void Pier(float edge, float inward, float z)
            {
                void Course(float width,float lower,float upper,float depth)
                {
                    float inner=edge+width*inward;
                    detail.Box(new Vector3(Mathf.Min(edge,inner),lower,z-depth/2),
                        new Vector3(Mathf.Max(edge,inner),upper,z+depth/2));
                }
                Course(.95f,Floor,Floor+.36f,1.52f);
                Course(.73f,Floor+.36f,-7.66f,1.10f);
                Course(.95f,-7.66f,-7.55f,1.38f);
                float inner=edge+.9f*inward;
                detail.Seat(Mathf.Min(edge,inner),Mathf.Max(edge,inner),z-.56f,z+.56f,-7.55f);
            }

            void Add(string label,Masonry part)
            {
                var go = new GameObject(label);
                go.transform.SetParent(hall.transform,false);
                var mesh = part.Create(label);
                meshes.Add(mesh);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial=material;
                go.AddComponent<MeshCollider>().sharedMesh=mesh;
            }
        }

        static (float Low,float Peak,int Samples,int Joints) ValidateMounted(GameObject hall,Sector design,Bounds[] endCapitals)
        {
            float westEdge=design.West, east=design.East, south=design.South, north=design.North;
            float Soffit(float x) => design.Soffit(x);
            Physics.SyncTransforms();
            var world = hall.scene.GetPhysicsScene();
            var hits = new Collider[32];
            int samples = 0;
            float low = float.PositiveInfinity, peak = 0;
            var capLanes = Enumerable.Range(-6,13).Select(i => design.Mid+i)
                .Where(x => !design.Adaptive || (x > westEdge+1.6f && x < east-1.6f))
                .Where(x => !endCapitals.Any(b => x>b.min.x-.36f && x<b.max.x+.36f)).ToArray();
            Require(capLanes.Any(x => Enumerable.Range(1,3).All(i => capLanes.Contains(x+i))),
                "Los accesos no ofrecen cuatro carriles contiguos.");
            foreach (float x in capLanes)
                for (float z=south-3;z<=north+3;z+=.5f)
                {
                    var p = new Vector3(x,Floor,z);
                    CheckWalk(p);
                    if (z>south+.5f && z<north-.5f)
                    {
                        Require(world.Raycast(p+Vector3.up*.1f,Vector3.up,out var head,18,~0,QueryTriggerInteraction.Ignore) &&
                            head.collider.transform.IsChildOf(hall.transform),"No se pudo medir la boveda en "+p);
                        float height = head.point.y-Floor;
                        low=Mathf.Min(low,height);
                        peak=Mathf.Max(peak,height);
                    }
                }
            if (design==Sun)
                foreach (float z in new[] {160.5f,161.5f,162.5f,163.5f})
                    for(float x=westEdge-3;x<=east+3;x+=.5f) CheckWalk(new Vector3(x,Floor,z));
            if (design==Cloister)
                for(float z=south-3;z<=north+3;z+=.5f) CheckWalk(new Vector3(24,Floor,z));
            Require(low>=design.MinimumClearance && peak>=design.MinimumPeak,
                $"Altura libre fuera del objetivo: mínimo {low:F2} m y clave {peak:F2} m.");
            int seamSamples = 0;
            foreach (float z in new[] {south+.04f,south+.35f,north-.35f,north-.04f}.Concat(design.Ribs))
                for (int joint=1;joint<design.Segments;joint++)
                    foreach (float offset in new[] {-.015f,.015f})
                    {
                        float x=westEdge+joint*.5f+offset;
                        if (x<westEdge+1 || x>east-1) continue;
                        Require(world.Raycast(new Vector3(x,Floor+.10f,z),Vector3.up,out var hit,18,~0,QueryTriggerInteraction.Ignore) &&
                            hit.collider.transform.IsChildOf(hall.transform) && hit.point.y<=Soffit(x)+.04f,
                            "Junta abierta o fuera de cota en X/Z="+x+"/"+z);
                        seamSamples++;
                    }
            foreach (float z in design.Ribs)
            {
                foreach (float x in design.TwoPiers ? new[] {westEdge+.48f,east-.48f} : new[] {east-.48f})
                Require(world.Raycast(new Vector3(x,Floor+.12f,z),Vector3.down,out var foot,.25f,~0,QueryTriggerInteraction.Ignore) &&
                    Mathf.Abs(foot.point.y-Floor)<.03f && !foot.collider.transform.IsChildOf(hall.transform),
                    "Pilastra sin piso en Z="+z);
            }

            return (low,peak,samples,seamSamples);

            void CheckWalk(Vector3 p)
            {
                Require(world.Raycast(p+Vector3.up*.2f,Vector3.down,out var floor,.35f,~0,QueryTriggerInteraction.Ignore) &&
                    Mathf.Abs(floor.point.y-Floor)<.05f && floor.normal.y>.9f,"Piso interrumpido en "+p);
                Require(world.OverlapCapsule(p+Vector3.up*.36f,p+Vector3.up*2.13f,.32f,hits,~0,QueryTriggerInteraction.Ignore)==0,
                    "Paso obstruido en "+p);
                samples++;
            }


        }

        sealed class Masonry
        {
            readonly Sector design;
            public Masonry(Sector design) { this.design=design; }
            readonly List<Vector3> points = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> faces = new List<int>();

            public void Barrel(float z0,float z1,float lower,float upper)
            {
                for (int i=0;i<design.Segments;i++)
                {
                    float a=Mathf.Lerp(design.West,design.East,i/(float)design.Segments), b=a+.5f;
                    Block(a,b,z0,z1,design.Soffit(a)+lower,design.Soffit(b)+lower,design.Soffit(a)+upper,design.Soffit(b)+upper,
                        i==0,i==design.Segments-1);
                }
            }

            public void End(float z0,float z1)
            {
                for (int i=0;i<design.Segments;i++)
                {
                    float a=Mathf.Lerp(design.West,design.East,i/(float)design.Segments), b=a+.5f;
                    Block(a,b,z0,z1,Spring,Spring,design.Soffit(a)+.035f,design.Soffit(b)+.035f,i==0,i==design.Segments-1);
                }
            }

            public void EndTrim(float z0,float z1)
            {
                for (int i=0;i<design.Segments;i++)
                {
                    float a=design.West+i*.5f, b=a+.5f;
                    Block(a,b,z0,z1,Mathf.Max(Spring,design.Soffit(a)-.15f),Mathf.Max(Spring,design.Soffit(b)-.15f),
                        design.Soffit(a)+.02f,design.Soffit(b)+.02f,i==0,i==design.Segments-1);
                }
            }

            public void Box(Vector3 min,Vector3 max) =>
                Block(min.x,max.x,min.z,max.z,min.y,min.y,max.y,max.y,true,true);

            public void Seat(float x0,float x1,float z0,float z1,float bottom) =>
                Block(x0,x1,z0,z1,bottom,bottom,design.Soffit(x0)-.30f,design.Soffit(x1)-.30f,true,true);

            void Block(float a,float b,float z0,float z1,float bottomA,float bottomB,float topA,float topB,
                bool west,bool east)
            {
                var p=new Vector3(a,bottomA,z0);var q=new Vector3(b,bottomB,z0);
                var r=new Vector3(b,bottomB,z1);var s=new Vector3(a,bottomA,z1);
                var t=new Vector3(a,topA,z0);var u=new Vector3(b,topB,z0);
                var v=new Vector3(b,topB,z1);var w=new Vector3(a,topA,z1);
                Quad(p,q,r,s); Quad(t,w,v,u); Quad(p,t,u,q); Quad(s,r,v,w);
                if(west) Quad(p,s,w,t);
                if(east) Quad(q,u,v,r);
            }

            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                int n=points.Count;
                points.AddRange(new[] {a,b,c,d});
                var normal=Vector3.Cross(b-a,c-a);
                foreach (var p in new[] {a,b,c,d})
                    uv.Add(Mathf.Abs(normal.z)>Mathf.Max(Mathf.Abs(normal.x),Mathf.Abs(normal.y)) ?
                        new Vector2(p.x,p.y)*.5f :
                        new Vector2(p.z,Mathf.Abs(normal.y)>Mathf.Abs(normal.x)?p.x:p.y)*.5f);
                faces.AddRange(new[] {n,n+1,n+2,n,n+2,n+3});
            }

            public Mesh Create(string label)
            {
                var mesh=new Mesh {name=label,hideFlags=HideFlags.DontSave};
                mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetTriangles(faces,0);
                mesh.RecalculateNormals();mesh.RecalculateTangents();
                return mesh;
            }
        }
    }
}
